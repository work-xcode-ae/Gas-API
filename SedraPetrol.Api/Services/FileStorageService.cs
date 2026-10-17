using Microsoft.Extensions.Options;
using SedraPetrol.Api.Common;

namespace SedraPetrol.Api.Services;

public class StorageOptions
{
    /// <summary>Folder for uploaded files (relative paths are under the app content root).</summary>
    public string RootPath { get; set; } = "App_Data/uploads";
    public int MaxFileSizeMb { get; set; } = 20;
}

/// <summary>Stores uploaded files on disk. The DB only keeps the relative path (document.file_path, company.logo_path ...).</summary>
public class FileStorageService
{
    private static readonly HashSet<string> Allowed = new(StringComparer.OrdinalIgnoreCase)
        { ".pdf", ".png", ".jpg", ".jpeg", ".webp", ".gif", ".svg", ".doc", ".docx", ".xls", ".xlsx", ".csv", ".txt", ".zip" };

    private readonly string _root;
    private readonly StorageOptions _opt;

    public FileStorageService(IOptions<StorageOptions> opt, IWebHostEnvironment env)
    {
        _opt = opt.Value;
        _root = Path.IsPathRooted(_opt.RootPath) ? _opt.RootPath : Path.Combine(env.ContentRootPath, _opt.RootPath);
        Directory.CreateDirectory(_root);
    }

    public async Task<(string RelativePath, long Size)> SaveAsync(IFormFile file, string folder)
    {
        if (file.Length == 0) throw new AppException("The file is empty.");
        if (file.Length > _opt.MaxFileSizeMb * 1024L * 1024L) throw new AppException($"The file is larger than {_opt.MaxFileSizeMb} MB.");
        var ext = Path.GetExtension(file.FileName);
        if (!Allowed.Contains(ext)) throw new AppException($"File type '{ext}' is not allowed.");

        var safeFolder = string.Concat(folder.Where(ch => char.IsLetterOrDigit(ch) || ch is '_' or '-' or '/')).Trim('/');
        var relative = Path.Combine(safeFolder, DateTime.UtcNow.ToString("yyyy/MM"), $"{Guid.NewGuid():N}{ext.ToLowerInvariant()}").Replace('\\', '/');
        var full = Path.Combine(_root, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        await using var fs = File.Create(full);
        await file.CopyToAsync(fs);
        return (relative, file.Length);
    }

    public string? FullPath(string? relative)
    {
        if (string.IsNullOrWhiteSpace(relative)) return null;
        var full = Path.GetFullPath(Path.Combine(_root, relative));
        if (!full.StartsWith(Path.GetFullPath(_root), StringComparison.OrdinalIgnoreCase)) return null; // path traversal guard
        return File.Exists(full) ? full : null;
    }
}
