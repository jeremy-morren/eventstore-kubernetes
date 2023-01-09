using System.Net;
using EventStoreBackup.Services;
using Microsoft.AspNetCore.Mvc;

namespace EventStoreBackup;

public class BackupActionResult : IActionResult
{
    private readonly CompressionType? _compressionType;

    public BackupActionResult(CompressionType? compressionType) => _compressionType = compressionType;

    public async Task ExecuteResultAsync(ActionContext context)
    {
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<BackupService>>();
        var svc = context.HttpContext.RequestServices.GetRequiredService<BackupService>();

        var response = context.HttpContext.Response;
        var ct = context.HttpContext.RequestAborted;
        try
        {
            var pod = await context.HttpContext.GetPodAsync(ct);
            response.StatusCode = (int) HttpStatusCode.OK;
            await svc.Process(context.HttpContext.Request.Host.ToString(), pod, response, _compressionType, ct);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error processing backup");
            if (!response.HasStarted)
                response.StatusCode = (int) HttpStatusCode.InternalServerError;
        }
    }
}