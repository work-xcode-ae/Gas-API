using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using SedraPetrol.Api.Common;

namespace SedraPetrol.Api.Security;

/// <summary>
/// Permission catalog. Every module has "{module}.view" (GET) and "{module}.manage" (POST/PUT/DELETE).
/// A few actions need an extra, more specific permission.
/// </summary>
public static class PermissionCatalog
{
    public static readonly (string Module, string NameEn, string NameAr)[] Modules =
    {
        ("dashboard", "Dashboard", "لوحة التحكم"),
        ("settings", "Settings & master data", "الإعدادات والبيانات الأساسية"),
        ("users", "Users & roles", "المستخدمون والصلاحيات"),
        ("parties", "Owners / parties", "الملاك"),
        ("vendors", "Vendors, drivers & vehicles", "الموردون والسائقون والمركبات"),
        ("buildings", "Buildings & units", "المباني والوحدات"),
        ("tenants", "Tenants", "المستأجرون"),
        ("contracts", "Gas supply contracts", "عقود توريد الغاز"),
        ("meters", "Meters & readings", "العدادات والقراءات"),
        ("tanks", "Tanks & refill", "الخزانات والتعبئة"),
        ("amc", "Maintenance (AMC) contracts", "عقود الصيانة"),
        ("maintenance", "Maintenance visits", "زيارات الصيانة"),
        ("service_requests", "Service requests", "طلبات الخدمة"),
        ("civil_defense", "Civil Defense", "الدفاع المدني"),
        ("invoices", "Tenant invoices", "فواتير المستأجرين"),
        ("payments", "Tenant payments", "مدفوعات المستأجرين"),
        ("payables", "Vendor bills & payments", "فواتير ومدفوعات الموردين"),
        ("accounting", "Accounting", "المحاسبة"),
        ("documents", "Documents", "المستندات"),
        ("reports", "Reports", "التقارير"),
    };

    public static readonly (string Code, string Module, string NameEn, string NameAr)[] Special =
    {
        ("invoices.post", "invoices", "Issue / post invoices", "إصدار الفواتير"),
        ("invoices.cancel", "invoices", "Cancel invoices", "إلغاء الفواتير"),
        ("payments.cancel", "payments", "Cancel payments", "إلغاء المدفوعات"),
        ("tanks.approve_refill", "tanks", "Approve refills (stage 7)", "اعتماد التعبئة"),
        ("accounting.post", "accounting", "Post / reverse journal entries", "ترحيل القيود"),
        ("accounting.close_period", "accounting", "Close fiscal periods", "إقفال الفترات"),
    };

    public static IEnumerable<(string Code, string Module, string NameEn, string NameAr)> All()
    {
        foreach (var m in Modules)
        {
            yield return ($"{m.Module}.view", m.Module, $"View {m.NameEn}", $"عرض {m.NameAr}");
            yield return ($"{m.Module}.manage", m.Module, $"Manage {m.NameEn}", $"إدارة {m.NameAr}");
        }
        foreach (var s in Special) yield return s;
    }
}

/// <summary>Puts a controller in a permission module: GET needs "{module}.view", writes need "{module}.manage".</summary>
[AttributeUsage(AttributeTargets.Class, Inherited = true)]
public class PermissionModuleAttribute : Attribute
{
    public string Module { get; }
    public PermissionModuleAttribute(string module) => Module = module;
}

/// <summary>Require a specific permission code on an action (overrides the module default).</summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public class RequirePermissionAttribute : Attribute
{
    public string Code { get; }
    public RequirePermissionAttribute(string code) => Code = code;
}

/// <summary>
/// Global filter: GET needs "{module}.view", anything else needs "{module}.manage",
/// unless the action carries [RequirePermission]. Users in the ADMIN role pass everything.
/// </summary>
public class PermissionFilter : IAsyncAuthorizationFilter
{
    public Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var endpoint = context.HttpContext.GetEndpoint();
        if (endpoint?.Metadata.GetMetadata<IAllowAnonymous>() != null) return Task.CompletedTask;

        var user = context.HttpContext.RequestServices.GetRequiredService<ICurrentUser>();
        if (!user.IsAuthenticated) return Task.CompletedTask; // [Authorize] returns 401
        if (user.IsAdmin) return Task.CompletedTask;

        var explicitCodes = endpoint?.Metadata.GetOrderedMetadata<RequirePermissionAttribute>().Select(a => a.Code).ToList() ?? new();
        var required = new List<string>(explicitCodes);
        if (required.Count == 0
            && context.ActionDescriptor is ControllerActionDescriptor cad
            && cad.ControllerTypeInfo.GetCustomAttributes(typeof(PermissionModuleAttribute), true).FirstOrDefault() is PermissionModuleAttribute mod)
        {
            var isRead = HttpMethods.IsGet(context.HttpContext.Request.Method) || HttpMethods.IsHead(context.HttpContext.Request.Method);
            required.Add($"{mod.Module}.{(isRead ? "view" : "manage")}");
        }

        // "x.manage" also grants "x.view"
        bool Has(string code) => user.HasPermission(code) || (code.EndsWith(".view") && user.HasPermission(code[..^5] + ".manage"));
        if (required.Any(code => !Has(code)))
        {
            context.Result = new ObjectResult(new ProblemDetails
            {
                Status = 403, Title = "Forbidden",
                Detail = $"Missing permission: {string.Join(", ", required.Where(c => !Has(c)))}"
            }) { StatusCode = 403 };
        }
        return Task.CompletedTask;
    }
}
