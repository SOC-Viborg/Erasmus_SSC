    using Erasmus_SSC.Data;
    using Erasmus_SSC.Dtos.News;       
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using System.Text.Json;

    namespace Erasmus_SSC.Controllers
    {
        [ApiController]
        [Route("api/[controller]")]
        public class NewsController : ControllerBase
        {
            private readonly ApplicationDbContext _db;

            public NewsController(ApplicationDbContext db)
            {
                _db = db;
            }

            [HttpGet]
            public async Task<ActionResult<IReadOnlyList<PublicNewsDto>>> GetNews(CancellationToken ct)
            {
                
                var fromDb = await _db.News
                    .AsNoTracking()
                    .OrderByDescending(n => n.PublishedAt)
                    .Select(n => new PublicNewsDto
                    {
                        Title = n.Title,
                        Description = n.Description,
                        ImageUrl = n.ImageUrl ?? string.Empty,
                        Date = n.PublishedAt
                    })
                    .ToListAsync(ct);

                if (fromDb.Count > 0)
                    return Ok(fromDb);

                
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data", "news.json");
                if (!System.IO.File.Exists(filePath))
                    return Ok(Array.Empty<PublicNewsDto>());

                var json = await System.IO.File.ReadAllTextAsync(filePath, ct);
                var fromFile = JsonSerializer.Deserialize<List<PublicNewsDto>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new List<PublicNewsDto>();

                return Ok(fromFile);
            }
        }
    

    //[ApiController]
    //[Route("api/[controller]")]
    //public class NewsController : ControllerBase
    //{
    //    [HttpGet]
    //    public IActionResult GetNews()
    //    {
    //        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data", "news.json");
    //        if (!System.IO.File.Exists(filePath))
    //            return NotFound();

    //        var json = System.IO.File.ReadAllText(filePath);
    //        var data = JsonSerializer.Deserialize<object>(json);
    //        return Ok(data);
    //    }
    //}
}
