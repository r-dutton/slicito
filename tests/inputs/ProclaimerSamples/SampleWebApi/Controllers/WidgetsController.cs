using Microsoft.AspNetCore.Mvc;

namespace SampleWebApi.Controllers;

[ApiController]
[Route("api/widgets")]
public class WidgetsController : ControllerBase
{
    [HttpGet("{id}")]
    public ActionResult<string> GetWidget(int id)
    {
        return Ok($"widget-{id}");
    }

    [HttpPost("create")]
    public IActionResult CreateWidget([FromBody] string name)
    {
        return Created($"/api/widgets/{name}", name);
    }
}
