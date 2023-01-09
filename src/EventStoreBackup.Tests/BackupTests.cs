using System.IO.Compression;
using Xunit.Abstractions;

namespace EventStoreBackup.Tests;

public class BackupTests
{
    private readonly ITestOutputHelper _output;

    public BackupTests(ITestOutputHelper output) => _output = output;

    private async Task CreateBackupInternal(TempFile file, string host, string? compression)
    {
        await using var app = new WebApp(_output);
        var client = app.CreateClient();
        var @params = compression != null ? $"?compression={compression}" : null;
        var request = new HttpRequestMessage()
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri($"admin/backup{@params}", UriKind.Relative),
            Headers = {Host = $"{host}.esdb.local"}
        };
        using (var response = await client.SendAsync(request))
        {
            response.EnsureSuccessStatusCode();
            await using var stream = await response.Content.ReadAsStreamAsync();
            await using var fs = file.OpenWrite();
            await stream.CopyToAsync(fs);
        }
        Assert.True(file.Exists);
        Assert.NotEqual(0, file.Length);
    }

    [Fact]
    public async Task CreateBackup()
    {
        using var file = new TempFile();
        await CreateBackupInternal(file, "a", null);
        await using var fs = file.OpenRead();
        var files = await TarUtils.ListTarFiles(fs).ToListAsync();
        Assert.NotEmpty(files);
        Assert.Contains(files, name => name.StartsWith("metadata/a.esdb.local"));
        
        //The metadata/ folder should be first in the list
        Assert.StartsWith("metadata/", files[0]);
}

    [Fact]
    public async Task CreateBackupGzip()
    {
        using var file = new TempFile();
        await CreateBackupInternal(file, "b", "gzip");
        
        //Verify stream is valid gzip stream
        await using var fs = file.OpenRead();
        await using var gz = new GZipStream(fs, CompressionMode.Decompress, false);

        var buffer = new byte[1024];
        var length = await gz.ReadAsync(buffer);
        Assert.True(length > 0);
    }
    
    [Fact]
    public async Task CreateBackupBzip2()
    {
        using var file = new TempFile();
        await CreateBackupInternal(file, "c", "bzip2");
    }

    private class TempFile : IDisposable
    {
        private readonly FileInfo _info;
        private readonly string _filename;

        public TempFile()
        {
            _filename = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            _info = new FileInfo(_filename);
        }

        public bool Exists => _info.Exists;

        public long Length => _info.Length;

        public FileStream OpenWrite() => new (_filename, FileMode.Create, FileAccess.Write);

        public FileStream OpenRead() => new(_filename, FileMode.Open, FileAccess.Read);

        public void Dispose()
        {
            if (_info.Exists)
                _info.Delete();
        }
    }
}