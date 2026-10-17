using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using SedraPetrol.Api.Common;
using SedraPetrol.Api.Domain.Entities;
using SedraPetrol.Api.Security;
using SedraPetrol.Api.Services;

namespace SedraPetrol.Api.Controllers;

/// <summary>Attachments for any record (entity_type + entity_id): Emirates IDs, contracts, certificates, reading photos ...</summary>
[Route("api/documents")]
[PermissionModule("documents")]
public class DocumentsController : ApiControllerBase
{
    private FileStorageService Files => HttpContext.RequestServices.GetRequiredService<FileStorageService>();

    /// <summary>Documents of a record, e.g. ?entityType=tenant&amp;entityId=5. Or expiringInDays for alerts.</summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? entityType, [FromQuery] long? entityId, [FromQuery] int? expiringInDays)
    {
        await LoadLookups();
        var q = Db.Documents.AsNoTracking();
        if (entityType != null) q = q.Where(d => d.EntityType == entityType);
        if (entityId != null) q = q.Where(d => d.EntityId == entityId);
        if (expiringInDays != null) { var limit = Today.AddDays(expiringInDays.Value); q = q.Where(d => d.ExpiryDate != null && d.ExpiryDate <= limit); }
        var docs = await q.OrderByDescending(d => d.Id).Take(500).ToListAsync();
        return Ok(docs.Select(d => new { d.Id, d.EntityType, d.EntityId, type = Code(d.DocumentTypeId), typeLabel = Label(d.DocumentTypeId), d.FileName, d.MimeType, d.SizeBytes, d.ExpiryDate, d.CreatedAt, url = $"/api/documents/{d.Id}/download" }));
    }

    /// <summary>Upload a file (multipart/form-data: file, entityType, entityId, documentType, expiryDate).</summary>
    [HttpPost]
    [RequestSizeLimit(50_000_000)]
    public async Task<IActionResult> Upload([FromForm] UploadForm form)
    {
        var file = form.File ?? throw new AppException("file is required.");
        var (path, size) = await Files.SaveAsync(file, form.EntityType);
        var d = new Document
        {
            BranchId = form.BranchId, EntityType = form.EntityType, EntityId = form.EntityId, FileName = Path.GetFileName(file.FileName), FilePath = path, MimeType = file.ContentType,
            SizeBytes = size, ExpiryDate = form.ExpiryDate, DocumentTypeId = await Lk(LookupTypes.DocumentType, null, form.DocumentType ?? "other", field: "documentType"),
        };
        Db.Documents.Add(d);
        await Db.SaveChangesAsync();
        return Ok(new { d.Id, url = $"/api/documents/{d.Id}/download" });
    }

    public class UploadForm
    {
        public IFormFile? File { get; set; }
        public string EntityType { get; set; } = "";
        public long EntityId { get; set; }
        /// <summary>DOCUMENT_TYPE code (emirates_id, contract, certificate, photo ...)</summary>
        public string? DocumentType { get; set; }
        public DateOnly? ExpiryDate { get; set; }
        public long? BranchId { get; set; }
    }

    [HttpGet("{id:long}/download")]
    public async Task<IActionResult> Download(long id)
    {
        var d = await Db.Documents.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("Document", id);
        var full = Files.FullPath(d.FilePath) ?? throw new NotFoundException("File", d.FileName ?? id.ToString());
        return PhysicalFile(full, d.MimeType ?? "application/octet-stream", d.FileName);
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        var d = await Db.Documents.FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("Document", id);
        Db.Documents.Remove(d); // soft delete; the file stays on disk
        await Db.SaveChangesAsync();
        return NoContent();
    }
}

/// <summary>Serves stored files by path (company logos etc.). Any signed-in user.</summary>
[Route("api/files")]
public class FilesController : ApiControllerBase
{
    [HttpGet("{**path}")]
    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    public IActionResult Get(string path)
    {
        // Only branding files are public (used on login page / printed invoices).
        if (!path.StartsWith("branding/", StringComparison.OrdinalIgnoreCase) && !(User.Identity?.IsAuthenticated ?? false)) return Unauthorized();
        var full = HttpContext.RequestServices.GetRequiredService<FileStorageService>().FullPath(path) ?? throw new NotFoundException("File", path);
        new FileExtensionContentTypeProvider().TryGetContentType(full, out var type);
        return PhysicalFile(full, type ?? "application/octet-stream");
    }
}

/// <summary>Activity log (audit trail) across the system.</summary>
[Route("api/activity")]
[PermissionModule("dashboard")]
public class ActivityController : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] PageQuery q, [FromQuery] string? entityType, [FromQuery] long? entityId, [FromQuery] long? buildingId, [FromQuery] long? userId)
    {
        var query = Db.ActivityLogs.AsNoTracking().Where(a => Me.IsAdmin || Me.BranchIds.Count == 0 || a.BranchId == null || Me.BranchIds.Contains(a.BranchId.Value));
        if (entityType != null) query = query.Where(a => a.EntityType == entityType);
        if (entityId != null) query = query.Where(a => a.EntityId == entityId);
        if (buildingId != null) query = query.Where(a => a.BuildingId == buildingId);
        if (userId != null) query = query.Where(a => a.UserId == userId);
        return Ok(await query.OrderByDescending(a => a.OccurredAt)
            .Select(a => new { a.Id, a.EntityType, a.EntityId, a.Action, a.Description, a.BuildingId, a.OccurredAt, a.UserId, user = a.User!.NameAr ?? a.User.NameEn })
            .ToPagedAsync(q));
    }
}
