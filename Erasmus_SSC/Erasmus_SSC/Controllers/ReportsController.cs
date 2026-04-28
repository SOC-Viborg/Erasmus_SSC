using Erasmus_SSC.Data;
using Erasmus_SSC.Dtos.Reports;
using Erasmus_SSC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Erasmus_SSC.Controllers;

[ApiController]
[Route("api/reports")]
public sealed class ReportsController : ControllerBase
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".doc", ".docx", ".exe"
    };

    private const long MaxFileBytes = 20 * 1024 * 1024; // 20 MB

    private readonly ApplicationDbContext _db;
    private readonly IWebHostEnvironment _env;

    public ReportsController(ApplicationDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    [HttpGet("languages")]
    [AllowAnonymous]
    public async Task<ActionResult<List<ReportLanguageDto>>> GetLanguages(CancellationToken ct)
    {
        var items = await _db.ReportLanguages
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .Select(x => new ReportLanguageDto
            {
                Id = x.Id,
                Name = x.Name,
                Code = x.Code
            })
            .ToListAsync(ct);

        return Ok(items);
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<ReportItemDto>>> GetAll(CancellationToken ct)
    {
        var items = await _db.Reports
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .OrderByDescending(x => x.UploadedAt)
            .Select(x => new ReportItemDto
            {
                Id = x.Id,
                Title = x.Title,
                FileName = x.FileName,
                FileType = x.FileType,
                SizeBytes = x.SizeBytes,
                UploadedAt = x.UploadedAt,
                LanguageId = x.LanguageId,
                LanguageName = x.Language.Name,
                LanguageCode = x.Language.Code
            })
            .ToListAsync(ct);

        return Ok(items);
    }

    [HttpGet("{id:int}/download")]
    [AllowAnonymous]
    public async Task<IActionResult> Download(int id, CancellationToken ct)
    {
        var entity = await _db.Reports
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);

        if (entity is null)
            return NotFound(new { message = "Report not found." });

        var absolutePath = Path.Combine(_env.ContentRootPath, "App_Data", "reports", entity.StoredPath);

        if (!System.IO.File.Exists(absolutePath))
            return NotFound(new { message = "Stored file not found on disk." });

        var stream = System.IO.File.OpenRead(absolutePath);
        return File(stream, entity.FileType, entity.FileName);
    }

    [HttpPost("upload")]
    [Authorize(Roles = "Admin")]
    [RequestSizeLimit(MaxFileBytes + 200_000)]
    public async Task<ActionResult<ReportItemDto>> Upload([FromForm] UploadReportForm form, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        if (form.File is null)
            return BadRequest(new { message = "File is missing." });

        if (string.IsNullOrWhiteSpace(form.Title))
            return BadRequest(new { message = "Title is required." });

        var language = await _db.ReportLanguages
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == form.LanguageId, ct);

        if (language is null)
            return BadRequest(new { message = "Selected language does not exist." });

        var file = form.File;

        if (file.Length <= 0)
            return BadRequest(new { message = "Empty file." });

        if (file.Length > MaxFileBytes)
            return BadRequest(new { message = "File is too large. Max 20 MB." });

        var originalFileName = Path.GetFileName(file.FileName);
        var ext = Path.GetExtension(originalFileName);

        if (string.IsNullOrWhiteSpace(ext) || !AllowedExtensions.Contains(ext))
            return BadRequest(new { message = "Unsupported file type." });

        var storageFolder = Path.Combine(_env.ContentRootPath, "App_Data", "reports");
        Directory.CreateDirectory(storageFolder);

        var storedFileName = $"{Guid.NewGuid():N}{ext}";
        var absolutePath = Path.Combine(storageFolder, storedFileName);

        await using (var fs = System.IO.File.Create(absolutePath))
        {
            await file.CopyToAsync(fs, ct);
        }

        var entity = new Report
        {
            Title = form.Title.Trim(),
            FileName = originalFileName,
            StoredPath = storedFileName,
            FileType = string.IsNullOrWhiteSpace(file.ContentType)
                ? "application/octet-stream"
                : file.ContentType,
            SizeBytes = checked((int)file.Length),
            UploadedAt = DateTime.UtcNow,
            LanguageId = language.Id,
            IsDeleted = false
        };

        _db.Reports.Add(entity);
        await _db.SaveChangesAsync(ct);

        return Ok(new ReportItemDto
        {
            Id = entity.Id,
            Title = entity.Title,
            FileName = entity.FileName,
            FileType = entity.FileType,
            SizeBytes = entity.SizeBytes,
            UploadedAt = entity.UploadedAt,
            LanguageId = language.Id,
            LanguageName = language.Name,
            LanguageCode = language.Code
        });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var entity = await _db.Reports
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);

        if (entity is null)
            return NotFound(new { message = "Report not found." });

        entity.IsDeleted = true;
        await _db.SaveChangesAsync(ct);

        return NoContent();
    }
}
