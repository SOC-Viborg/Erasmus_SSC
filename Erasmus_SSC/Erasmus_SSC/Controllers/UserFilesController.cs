using System.Security.Claims;
using Erasmus_SSC.Data;
using Erasmus_SSC.Dtos.UserFiles;
using Erasmus_SSC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Erasmus_SSC.Controllers;

[ApiController]
[Route("api/user-files")]
[Authorize]
public sealed class UserFilesController : ControllerBase
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".doc", ".docx", ".txt",
        ".png", ".jpg", ".jpeg", ".webp",
        ".zip", ".rar",
        ".xlsx", ".xls", ".ppt", ".pptx"
    };

    private const long MaxFileBytes = 10 * 1024 * 1024; // 10 MB

    private readonly ApplicationDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<UserFilesController> _logger;

    public UserFilesController(
        ApplicationDbContext db,
        IWebHostEnvironment env,
        ILogger<UserFilesController> logger)
    {
        _db = db;
        _env = env;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<UserFileItemDto>>> GetAll(CancellationToken ct)
    {
        var items = await _db.UserFiles
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .Include(x => x.OwnerUser)
            .OrderByDescending(x => x.UploadedAt)
            .Select(x => new UserFileItemDto
            {
                Id = x.Id,
                FileName = x.FileName,
                FileType = x.FileType,
                SizeBytes = x.SizeBytes,
                UploadedAt = x.UploadedAt,
                OwnerUserId = x.OwnerUserId,
                OwnerUserName = x.OwnerUser.UserName
            })
            .ToListAsync(ct);

        return Ok(items);
    }

    [HttpPost("upload")]
    [RequestSizeLimit(MaxFileBytes + 200_000)]
    public async Task<ActionResult<UserFileItemDto>> Upload([FromForm] UploadUserFileForm form, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        if (form.File is null)
            return BadRequest(new { message = "File is missing." });

        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized(new { message = "User id not found in token." });

        var file = form.File;

        if (file.Length <= 0)
            return BadRequest(new { message = "Empty file." });

        if (file.Length > MaxFileBytes)
            return BadRequest(new { message = "File is too large. Max 10 MB." });

        var originalFileName = Path.GetFileName(file.FileName);
        var ext = Path.GetExtension(originalFileName);

        if (string.IsNullOrWhiteSpace(ext) || !AllowedExtensions.Contains(ext))
            return BadRequest(new { message = "Unsupported file type." });

        var storageFolder = Path.Combine(_env.ContentRootPath, "App_Data", "user-files");
        Directory.CreateDirectory(storageFolder);

        var storedFileName = $"{Guid.NewGuid():N}{ext}";
        var absolutePath = Path.Combine(storageFolder, storedFileName);

        await using (var fs = System.IO.File.Create(absolutePath))
        {
            await file.CopyToAsync(fs, ct);
        }

        var entity = new UserFile
        {
            FileName = originalFileName,
            StoredPath = storedFileName,
            FileType = string.IsNullOrWhiteSpace(file.ContentType)
                ? "application/octet-stream"
                : file.ContentType,
            SizeBytes = checked((int)file.Length),
            UploadedAt = DateTime.UtcNow,
            OwnerUserId = userId,
            IsDeleted = false
        };

        _db.UserFiles.Add(entity);
        await _db.SaveChangesAsync(ct);

        var ownerName = await _db.Users
            .Where(u => u.Id == userId)
            .Select(u => u.UserName)
            .FirstOrDefaultAsync(ct) ?? "Unknown";

        return Ok(new UserFileItemDto
        {
            Id = entity.Id,
            FileName = entity.FileName,
            FileType = entity.FileType,
            SizeBytes = entity.SizeBytes,
            UploadedAt = entity.UploadedAt,
            OwnerUserId = entity.OwnerUserId,
            OwnerUserName = ownerName
        });
    }

    [HttpGet("{id:int}/download")]
    public async Task<IActionResult> Download(int id, CancellationToken ct)
    {
        var entity = await _db.UserFiles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);

        if (entity is null)
            return NotFound(new { message = "File not found." });

        var absolutePath = Path.Combine(_env.ContentRootPath, "App_Data", "user-files", entity.StoredPath);

        if (!System.IO.File.Exists(absolutePath))
            return NotFound(new { message = "Stored file not found on disk." });

        var stream = System.IO.File.OpenRead(absolutePath);
        return File(stream, entity.FileType, entity.FileName);
    }

    private bool TryGetCurrentUserId(out int userId)
    {
        userId = 0;

        var raw =
            User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            User.FindFirstValue("nameid") ??
            User.FindFirstValue("sub");

        return int.TryParse(raw, out userId);
    }
}