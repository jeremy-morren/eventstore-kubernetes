using Microsoft.AspNetCore.Mvc;

namespace EventStoreBackup;

[ApiController]
[Route("/admin/[controller]")]
public class BackupController : Controller
{
    [HttpPost]
    public IActionResult Index([FromQuery] CompressionType? compression) => new BackupActionResult(compression);
}