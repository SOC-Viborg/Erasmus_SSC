using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Erasmus_SSC.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NewsController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetNews()
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data", "news.json");
            if (!System.IO.File.Exists(filePath))
                return NotFound();

            var json = System.IO.File.ReadAllText(filePath);
            var data = JsonSerializer.Deserialize<object>(json);
            return Ok(data);
        }
    }
}
