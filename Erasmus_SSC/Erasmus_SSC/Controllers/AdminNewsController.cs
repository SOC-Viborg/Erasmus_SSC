using Erasmus_SSC.Data;
using Erasmus_SSC.Dtos.News;
using Erasmus_SSC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Erasmus_SSC.Controllers;

[ApiController]
[Route("api/admin/news")]
[Authorize(Roles = "Admin")]
public sealed class AdminNewsController : ControllerBase
{
    private static readonly HashSet<string> AllowedExt = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".webp"
    };

    private const long MaxImageBytes = 5 * 1024 * 1024; // 5MB

    private readonly ApplicationDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<AdminNewsController> _logger;

    public AdminNewsController(ApplicationDbContext db, IWebHostEnvironment env, ILogger<AdminNewsController> logger)
    {
        _db = db;
        _env = env;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<AdminNewsDto>>> GetAll(CancellationToken ct)
    {
        var items = await _db.News
            .AsNoTracking()
            .OrderByDescending(n => n.PublishedAt)
            .Select(n => new AdminNewsDto
            {
                Id = n.Id,
                Title = n.Title,
                Description = n.Description,
                ImageUrl = n.ImageUrl ?? string.Empty,
                Date = n.PublishedAt
            })
            .ToListAsync(ct);

        return Ok(items);
    }

    [HttpPost]
    [RequestSizeLimit(MaxImageBytes + 200_000)]
    public async Task<ActionResult<AdminNewsDto>> Create([FromForm] UpsertNewsForm form, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var entity = new News
        {
            Title = form.Title.Trim(),
            Description = form.Description,
            PublishedAt = EnsureUtc(form.Date)
        };

        if (form.Image is not null)
        {
            var imageUrl = await SaveImageAsync(form.Image, ct);
            entity.ImageUrl = imageUrl;
        }

        _db.News.Add(entity);
        await _db.SaveChangesAsync(ct);

        return Created($"/api/admin/news/{entity.Id}", new AdminNewsDto
        {
            Id = entity.Id,
            Title = entity.Title,
            Description = entity.Description,
            ImageUrl = entity.ImageUrl ?? string.Empty,
            Date = entity.PublishedAt
        });
    }

    [HttpPut("{id:int}")]
    [RequestSizeLimit(MaxImageBytes + 200_000)]
    public async Task<ActionResult<AdminNewsDto>> Update(int id, [FromForm] UpsertNewsForm form, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var entity = await _db.News.FirstOrDefaultAsync(n => n.Id == id, ct);
        if (entity is null) return NotFound(new { message = "News not found." });

        entity.Title = form.Title.Trim();
        entity.Description = form.Description;
        entity.PublishedAt = EnsureUtc(form.Date);

        if (form.Image is not null)
        {
            TryDeleteLocalImage(entity.ImageUrl);

            var imageUrl = await SaveImageAsync(form.Image, ct);
            entity.ImageUrl = imageUrl;
        }

        await _db.SaveChangesAsync(ct);

        return Ok(new AdminNewsDto
        {
            Id = entity.Id,
            Title = entity.Title,
            Description = entity.Description,
            ImageUrl = entity.ImageUrl ?? string.Empty,
            Date = entity.PublishedAt
        });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var entity = await _db.News.FirstOrDefaultAsync(n => n.Id == id, ct);
        if (entity is null) return NotFound(new { message = "News not found." });

        TryDeleteLocalImage(entity.ImageUrl);

        _db.News.Remove(entity);
        await _db.SaveChangesAsync(ct);

        return NoContent();
    }

    private async Task<string> SaveImageAsync(IFormFile file, CancellationToken ct)
    {
        if (file.Length <= 0)
            throw new InvalidOperationException("Empty image file.");

        if (file.Length > MaxImageBytes)
            throw new InvalidOperationException("Image is too large (max 5MB).");

        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(ext) || !AllowedExt.Contains(ext))
            throw new InvalidOperationException("Unsupported image format. Use png/jpg/jpeg/webp.");

        if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("File is not an image.");

        var folder = Path.Combine(_env.WebRootPath, "uploads", "news");
        Directory.CreateDirectory(folder);

        var fileName = $"{Guid.NewGuid():N}{ext}";
        var absPath = Path.Combine(folder, fileName);

        await using (var fs = System.IO.File.Create(absPath))
        {
            await file.CopyToAsync(fs, ct);
        }

        // URL for browser
        return $"/uploads/news/{fileName}";
    }

    private void TryDeleteLocalImage(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            return;

       
        if (!imageUrl.StartsWith("/uploads/news/", StringComparison.OrdinalIgnoreCase))
            return;

        try
        {
            var rel = imageUrl.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString());
            var abs = Path.Combine(_env.WebRootPath, rel);

            if (System.IO.File.Exists(abs))
                System.IO.File.Delete(abs);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete old news image {ImageUrl}", imageUrl);
        }
    }

    private static DateTime EnsureUtc(DateTime dt)
    {
        if (dt.Kind == DateTimeKind.Utc) return dt;
        if (dt.Kind == DateTimeKind.Unspecified) return DateTime.SpecifyKind(dt, DateTimeKind.Utc);
        return dt.ToUniversalTime();
    }
}
