using System.IO.Pipelines;
using System.Net;
using System.Text.Json;
using EventStoreBackup.K8s;
using k8s.Models;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

// ReSharper disable CommentTypo
// ReSharper disable StringLiteralTypo

namespace EventStoreBackup.Services;

public class BackupService
{
    private readonly K8sExec _exec;
    private readonly ILogger<BackupService> _logger;
    private readonly BackupOptions _options;

    public BackupService(K8sExec exec,
        ILogger<BackupService> logger,
        IOptions<BackupOptions> options)
    {
        _exec = exec;
        _logger = logger;
        _options = options.Value;
    }

    public async Task Process(string serverName,
        V1Pod pod,
        HttpResponse response,
        CompressionType? compression, 
        CancellationToken ct)
    {
        Task Exec(params string[] command) => _exec.Exec(pod, command, ct);
        
        var date = DateTime.UtcNow;

        var baseDir = ToUnixDirectory(_options.TempDirectory);
        
        baseDir = $"{baseDir}/{Guid.NewGuid()}";
        
        _logger.LogInformation("Creating backup in folder {BaseFolder} on pod {@Pod} with compression: {CompressionType}", 
            baseDir, pod, compression);

        await Exec("mkdir", baseDir);

        try
        {
            await WriteMetadata(pod, baseDir, serverName, date, ct);
            
            //We want to return the headers ASAP, so that clients block waiting to read the body
            response.StatusCode = (int) HttpStatusCode.OK;
            SetHeaders(response.Headers, serverName, date, compression);
            await response.StartAsync(ct);
            
            await PerformBackup(pod, baseDir, ct);

            //Write the TAR to the response body
            await CopyTar(pod, baseDir, response.BodyWriter, compression, ct);
        }
        finally
        {
            await response.CompleteAsync();
            
            //We do not pass any cancellationToken, because this operation should complete regardless of cancellation status
            await _exec.Exec(pod, new[] {"rm", "-rf", baseDir}, default);
        }
    }
    
    #region Setup
    
    private async Task WriteMetadata(V1Pod pod, 
        string baseDir, 
        string serverName, 
        DateTime date,
        CancellationToken ct)
    {
        var metadata = Path.Combine(baseDir, "metadata");
        metadata = metadata.Replace('\\', '/');

        Task Exec(params string[] commands) => _exec.Exec(pod, commands, ct);

        await Exec("mkdir", metadata);
        
        //To write to a file, we need to use shell redirection
        //Transfer using base64
        var metadataJson = JsonSerializer.SerializeToUtf8Bytes(new
            {
                date,
                server = serverName,
                pod = new
                {
                    Name = pod.Name(),
                    Namespace = pod.Namespace(),
                    containers = pod.Spec.Containers.Select(c => new { c.Name, c.Image })
                }
            },
            new JsonSerializerOptions() {PropertyNamingPolicy = JsonNamingPolicy.CamelCase});
        var base64Json = Convert.ToBase64String(metadataJson, Base64FormattingOptions.None);

        await Task.WhenAll(
            //Create informational files in the directory
            //Can be used to read metadata by listing files in tar archive
            //These filenames could cause problems on windows however
            Exec("touch", $"{metadata}/{date:yyyy-MM-dd}_{date:HH:mm:ss}"),
            Exec("touch", $"{metadata}/{serverName}"),
            
            Exec("/bin/sh", "-c", $"echo '{base64Json}' | base64 -d > '{metadata}/metadata.json' || exit $?"));
    }
    
    private static void SetHeaders(IHeaderDictionary headers, string serverName, DateTime date, CompressionType? compression)
    {
        var extension = compression switch
        {
            CompressionType.Gzip => "tar.gz",
            CompressionType.Bzip2 => "tar.bz2",
            _ => "tar"
        };
        headers[HeaderNames.ContentDisposition] = new ContentDispositionHeaderValue("attachment")
        {
            FileName = $"{serverName}_{date:yyyy-MM-dd}_{date:HH:mm:ss}.{extension}",
            CreationDate = new DateTimeOffset(date)
        }.ToString();
        headers[HeaderNames.ContentType] = compression switch
        {
            CompressionType.Gzip => "application/x-tar+gzip",
            CompressionType.Bzip2 => "application/x-tar+bzip2",
            _ => "application/x-tar"
        };
    }
    
    #endregion

    #region Backup
    
    //Actually do the backup, see https://developers.eventstore.com/server/v20.10/operations.html#simple-full-backup-restore
    private async Task PerformBackup(V1Pod pod, string baseDir, CancellationToken ct)
    {
        var dataDir = ToUnixDirectory(_options.DataDirectory);
        
        //tar will contain folder 'backup'
        var backupDir = $"{baseDir}/backup";
        await _exec.Exec(pod, new[] {"mkdir", backupDir}, ct);
        
        async Task Copy(string args)
        {
            //We will run through /bin/sh to use globbing
            
            var cmd = new[]
            {
                "/bin/sh",
                "-c",
                //Run all commands from 'dataDir'
                //All commands end with backup dir
                $"cd '{dataDir}' && cp -a {args} '{backupDir}' || exit $?"
            };
            await _exec.Exec(pod, cmd, ct);
        }

        //Because the default eventstore image does not include rsync, we have to get tricky
        //We will get the list of files and do our globbing manually here
        
        //Get files in index/
        var index = (await _exec.Exec(pod, 
            new[] {"/bin/sh", "-c", $"cd '{dataDir}' && find index/ -type f || exit $?"},
            ct)).StdOut?.Split('\n') ?? Array.Empty<string>();
        
        //We use --parents to preserve directory structure
        await Copy($"--parents {string.Join(" ", index.Where(f => f.EndsWith(".chk")))}");
        await Copy($"--parents {string.Join(" ", index.Where(f => !f.EndsWith(".chk")))}");
        
        await Copy("*.chk");
        await Copy("chunk-*.*");
    }
    
    private static string ToUnixDirectory(string directory)
    {
        directory = directory.Replace("\\", "/");
        if (!Path.IsPathRooted(directory))
            throw new InvalidOperationException($"Path '{directory}' is not rooted");
        //Remove trailing slash if present
        return directory.EndsWith('/') ? directory[..^1] : directory;
    }
    
    private async Task CopyTar(
        V1Pod pod,
        string baseDir,
        PipeWriter output, 
        CompressionType? compressionType,
        CancellationToken ct)
    {
        Task<K8sExecResponse> Exec(params string[] args) => _exec.Exec(pod, args, ct);
        
        //Set directories to 755 (read & execute)
        //Files to 644 (read-only)
        
        await Exec("/bin/sh", "-c", $"chmod 755 $(find '{baseDir}' -type d)");
        
        //Make files readonly
        await Exec("/bin/sh", "-c", $"chmod 644 $(find '{baseDir}' -type f)");
    
        var flags = compressionType switch
        {
            CompressionType.Gzip => "z",
            CompressionType.Bzip2 => "j",
            _ => null
        };
        
        //c (compress) command
        //-O to write to stdout (i.e. pipewriter)
        //-v to get the file entries below (to stderr)
        //Use -C flag to change directory to baseDir
        //Include backup/ & metadata/ folders
        var response = await _exec.Exec(pod,
            new[] {"tar", "c", $"-v{flags}OC", baseDir, "metadata/", "backup/"},
            output,
            ct);

        var entryCount = response.StdErr?.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length;
        _logger.LogInformation("Wrote TAR archive. {Entries} items", entryCount);
    }
    
    #endregion
}