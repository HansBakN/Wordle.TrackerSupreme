using Microsoft.AspNetCore.Mvc;

namespace Wordle.TrackerSupreme.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StatusController : ControllerBase
{
    [HttpGet]
    public IActionResult GetStatus()
    {
        return Ok(new { application = "Wordle.TrackerSupreme", status = "ready" });
    }
}
