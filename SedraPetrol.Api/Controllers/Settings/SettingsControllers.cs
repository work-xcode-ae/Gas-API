using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SedraPetrol.Api.Common;
using SedraPetrol.Api.Domain.Entities;
using SedraPetrol.Api.Security;
using SedraPetrol.Api.Services;

namespace SedraPetrol.Api.Controllers;

/// <summary>Company profile and branding (logo, dark logo, stamp).</summary>
[Route("api/companies")]
[PermissionModule("settings")]
public class CompaniesController : CrudController<Company>
{
    /// <summary>Upload branding: kind = logo | logo_dark | stamp. Stores the file and saves its path on the company.</summary>
    [HttpPost("{id:long}/branding/{kind}")]
    [RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> UploadBranding(long id, string kind, [FromForm] FileForm form)
    {
        var file = form.File ?? throw new AppException("file is required.");
        var c = await Db.Companies.FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("Company", id);
        var (path, _) = await HttpContext.RequestServices.GetRequiredService<FileStorageService>().SaveAsync(file, "branding");
        switch (kind)
        {
            case "logo": c.LogoPath = path; break;
            case "logo_dark": c.LogoDarkPath = path; break;
            case "stamp": c.StampPath = path; break;
            default: throw new AppException("kind must be logo, logo_dark or stamp.");
        }
        await Db.SaveChangesAsync();
        return Ok(new { path, url = $"/api/files/{path}" });
    }
}

public class FileForm
{
    public IFormFile? File { get; set; }
}

[Route("api/branches")]
[PermissionModule("settings")]
public class BranchesController : CrudController<Branch> { }

[Route("api/countries")]
[PermissionModule("settings")]
public class CountriesController : CrudController<Country> { }

[Route("api/emirates")]
[PermissionModule("settings")]
public class EmiratesController : CrudController<Emirate> { }

[Route("api/cities")]
[PermissionModule("settings")]
public class CitiesController : CrudController<City> { }

[Route("api/currencies")]
[PermissionModule("settings")]
public class CurrenciesController : CrudController<Currency> { }

/// <summary>Business number sequences (read, and adjust the next value when migrating data).</summary>
[Route("api/number-sequences")]
[PermissionModule("settings")]
public class NumberSequencesController : CrudController<NumberSequence> { }
