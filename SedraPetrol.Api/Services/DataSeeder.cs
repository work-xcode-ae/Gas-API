using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SedraPetrol.Api.Data;
using SedraPetrol.Api.Domain.Entities;
using SedraPetrol.Api.Security;

namespace SedraPetrol.Api.Services;

/// <summary>
/// Idempotent seed of the master data the API relies on: lookups, currency, UAE locations,
/// company + branches, gas types, roles/permissions, admin user, a starter chart of accounts,
/// account mappings, fiscal periods and checklist templates. Only inserts what is missing.
/// </summary>
public class DataSeeder
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _cfg;
    private readonly ILogger<DataSeeder> _log;

    public DataSeeder(AppDbContext db, IConfiguration cfg, ILogger<DataSeeder> log)
    {
        _db = db; _cfg = cfg; _log = log;
    }

    // type code, en, ar, values (code, en, ar)
    public static readonly (string Type, string En, string Ar, (string Code, string En, string Ar)[] Values)[] Lookups =
    {
        ("BUILDING_TYPE", "Building type", "نوع المبنى", new[] { ("residential", "Residential", "سكني"), ("commercial", "Commercial", "تجاري"), ("mixed", "Mixed use", "مختلط") }),
        ("BUILDING_STATUS", "Building status", "حالة المبنى", new[] { ("active", "Active", "نشط"), ("inactive", "Inactive", "غير نشط") }),
        ("UNIT_TYPE", "Unit type", "نوع الوحدة", new[] { ("apartment", "Apartment", "شقة"), ("penthouse", "Penthouse", "بنتهاوس"), ("studio", "Studio", "استوديو"), ("duplex", "Duplex", "دوبلكس"), ("villa", "Villa", "فيلا"), ("commercial", "Commercial", "تجاري") }),
        ("UNIT_STATUS", "Unit status", "حالة الوحدة", new[] { ("vacant", "Vacant", "شاغرة"), ("occupied", "Occupied", "مشغولة"), ("reserved", "Reserved", "محجوزة"), ("maintenance", "Under maintenance", "تحت الصيانة") }),
        ("METER_TYPE", "Meter type", "نوع العداد", new[] { ("smart", "Smart", "ذكي"), ("standard", "Standard", "عادي"), ("prepaid", "Prepaid", "مسبق الدفع") }),
        ("METER_STATUS", "Meter status", "حالة العداد", new[] { ("active", "Active", "نشط"), ("fault", "Fault", "عطل"), ("disconnected", "Disconnected", "مفصول"), ("maintenance", "Maintenance", "صيانة") }),
        ("METER_SERVICE_TYPE", "Meter service type", "نوع خدمة العداد", new[] { ("connect", "Connect", "توصيل"), ("disconnect", "Disconnect", "فصل"), ("replace", "Replace", "استبدال"), ("report_issue", "Report issue", "إبلاغ عن مشكلة") }),
        ("METER_ISSUE_TYPE", "Meter issue type", "نوع مشكلة العداد", new[] { ("leak", "Gas leak", "تسرب غاز"), ("no_reading", "No reading", "لا توجد قراءة"), ("damaged", "Damaged", "تالف"), ("low_pressure", "Low pressure", "ضغط منخفض"), ("display", "Display fault", "عطل الشاشة"), ("other", "Other", "أخرى") }),
        ("SERVICE_STATUS", "Service status", "حالة الخدمة", new[] { ("pending", "Pending", "معلق"), ("in_progress", "In progress", "قيد التنفيذ"), ("completed", "Completed", "مكتمل"), ("cancelled", "Cancelled", "ملغي") }),
        ("READING_METHOD", "Reading method", "طريقة القراءة", new[] { ("manual", "Manual", "يدوي"), ("remote", "Remote", "عن بعد"), ("customer", "Customer", "العميل"), ("photo", "Photo", "صورة") }),
        ("CONTRACT_STATUS", "Contract status", "حالة العقد", new[] { ("draft", "Draft", "مسودة"), ("active", "Active", "نشط"), ("pending", "Pending", "معلق"), ("expired", "Expired", "منتهي"), ("terminated", "Terminated", "ملغي") }),
        ("FEE_TYPE", "Fee type", "نوع الرسوم", new[] { ("gas_price", "Gas price (per m³)", "سعر الغاز (لكل م³)"), ("maintenance", "Maintenance", "صيانة"), ("connection", "Connection", "توصيل"), ("disconnection", "Disconnection", "فصل"), ("inspection", "Inspection", "فحص"), ("meter_rent", "Meter rent", "إيجار العداد"), ("insurance", "Insurance", "تأمين"), ("deposit", "Deposit", "تأمين نقدي"), ("gas_purchase", "Gas purchase", "شراء غاز"), ("other", "Other", "أخرى") }),
        ("FEE_CYCLE", "Fee cycle", "دورة الرسوم", new[] { ("monthly", "Monthly", "شهري"), ("yearly", "Yearly", "سنوي"), ("one_time", "One time", "مرة واحدة"), ("per_m3", "Per m³", "لكل م³") }),
        ("INVOICE_TYPE", "Invoice type", "نوع الفاتورة", new[] { ("gas_consumption", "Gas consumption", "استهلاك غاز"), ("maintenance", "Maintenance", "صيانة"), ("deposit", "Deposit", "تأمين"), ("connection", "Connection", "توصيل"), ("other", "Other", "أخرى") }),
        ("INVOICE_STATUS", "Invoice status", "حالة الفاتورة", new[] { ("draft", "Draft", "مسودة"), ("pending", "Pending", "معلقة"), ("partial", "Partially paid", "مسددة جزئياً"), ("paid", "Paid", "مسددة"), ("overdue", "Overdue", "متأخرة"), ("cancelled", "Cancelled", "ملغاة") }),
        ("PAYMENT_METHOD", "Payment method", "طريقة الدفع", new[] { ("cash", "Cash", "نقداً"), ("bank_transfer", "Bank transfer", "تحويل بنكي"), ("cheque", "Cheque", "شيك"), ("online", "Online", "إلكتروني"), ("card", "Card", "بطاقة") }),
        ("PAYMENT_STATUS", "Payment status", "حالة الدفعة", new[] { ("completed", "Completed", "مكتملة"), ("pending", "Pending clearance", "قيد التحصيل"), ("bounced", "Bounced", "مرتجع"), ("cancelled", "Cancelled", "ملغاة") }),
        ("PARTY_TYPE", "Party type", "نوع الطرف", new[] { ("individual", "Individual", "فرد"), ("company", "Company", "شركة") }),
        ("CONTACT_TYPE", "Contact type", "نوع الاتصال", new[] { ("phone", "Phone", "هاتف"), ("mobile", "Mobile", "جوال"), ("email", "Email", "بريد إلكتروني") }),
        ("VENDOR_TYPE", "Vendor type", "نوع المورد", new[] { ("gas_supplier", "Gas supplier", "مورد غاز"), ("maintenance_vendor", "Maintenance vendor", "مقاول صيانة"), ("service_vendor", "Service vendor", "مورد خدمات"), ("other", "Other", "أخرى") }),
        ("DOCUMENT_TYPE", "Document type", "نوع المستند", new[] { ("emirates_id", "Emirates ID", "الهوية الإماراتية"), ("passport", "Passport", "جواز السفر"), ("trade_license", "Trade license", "الرخصة التجارية"), ("title_deed", "Title deed", "سند الملكية"), ("contract", "Contract", "عقد"), ("certificate", "Certificate", "شهادة"), ("report", "Report", "تقرير"), ("photo", "Photo", "صورة"), ("invoice", "Invoice", "فاتورة"), ("other", "Other", "أخرى") }),
        ("TANK_STATUS", "Tank status", "حالة الخزان", new[] { ("ok", "OK", "جيد"), ("low", "Low", "منخفض"), ("maintenance", "Maintenance", "صيانة"), ("out_of_service", "Out of service", "خارج الخدمة") }),
        ("TANK_EVENT_TYPE", "Tank event type", "نوع حدث الخزان", new[] { ("refill", "Refill", "تعبئة"), ("manual_reading", "Manual reading", "قراءة يدوية"), ("sensor", "Sensor", "حساس"), ("adjustment", "Adjustment", "تسوية") }),
        ("REFILL_STAGE", "Refill stage", "مرحلة التعبئة", new[] { ("0", "Request", "طلب التعبئة"), ("1", "Purchase order", "أمر الشراء"), ("2", "Supplier payment", "الدفع للمورد"), ("3", "Collection", "استلام الغاز"), ("4", "Purchase invoice", "فاتورة الشراء"), ("5", "Delivery schedule", "جدولة التوصيل"), ("6", "Tank refill", "تعبئة الخزان"), ("7", "Management approval", "موافقة الإدارة"), ("8", "Completed", "مكتمل") }),
        ("REFILL_STATUS", "Refill status", "حالة التعبئة", new[] { ("requested", "Requested", "مطلوب"), ("po_issued", "PO issued", "تم إصدار أمر الشراء"), ("paid", "Paid", "تم الدفع"), ("collecting", "Collected", "تم الاستلام"), ("invoiced", "Invoiced", "تمت الفوترة"), ("scheduled", "Scheduled", "مجدول"), ("delivering", "Delivered", "تم التوصيل"), ("pending_approval", "Pending approval", "بانتظار الموافقة"), ("completed", "Completed", "مكتمل"), ("cancelled", "Cancelled", "ملغي") }),
        ("PRIORITY", "Priority", "الأولوية", new[] { ("low", "Low", "منخفضة"), ("medium", "Medium", "متوسطة"), ("high", "High", "عالية"), ("urgent", "Urgent", "عاجلة") }),
        ("SR_STATUS", "Service request status", "حالة طلب الخدمة", new[] { ("pending", "Pending", "معلق"), ("in_progress", "In progress", "قيد التنفيذ"), ("completed", "Completed", "مكتمل"), ("cancelled", "Cancelled", "ملغي") }),
        ("VISIT_TYPE", "Visit type", "نوع الزيارة", new[] { ("preventive", "Preventive", "وقائية"), ("corrective", "Corrective", "تصحيحية"), ("emergency", "Emergency", "طارئة"), ("inspection", "Inspection", "فحص") }),
        ("VISIT_STATUS", "Visit status", "حالة الزيارة", new[] { ("scheduled", "Scheduled", "مجدولة"), ("in_progress", "In progress", "قيد التنفيذ"), ("completed", "Completed", "مكتملة"), ("cancelled", "Cancelled", "ملغاة") }),
        ("VISIT_FREQUENCY", "Visit frequency", "تكرار الزيارات", new[] { ("monthly", "Monthly", "شهري"), ("quarterly", "Quarterly", "ربع سنوي"), ("semi_annual", "Semi-annual", "نصف سنوي"), ("annual", "Annual", "سنوي") }),
        ("SEVERITY", "Severity", "الخطورة", new[] { ("info", "Info", "معلومة"), ("low", "Low", "منخفضة"), ("medium", "Medium", "متوسطة"), ("high", "High", "عالية"), ("critical", "Critical", "حرجة") }),
        ("AMC_STATUS", "AMC status", "حالة عقد الصيانة", new[] { ("pending", "Pending", "معلق"), ("active", "Active", "نشط"), ("expired", "Expired", "منتهي"), ("terminated", "Terminated", "ملغي") }),
        ("AMC_TERM_TYPE", "AMC term category", "فئة بند العقد", new[] { ("general", "General", "عام"), ("payment", "Payment", "الدفع"), ("sla", "Service level", "مستوى الخدمة"), ("liability", "Liability", "المسؤولية"), ("termination", "Termination", "الإنهاء"), ("other", "Other", "أخرى") }),
        ("AMC_PAYMENT_STATUS", "AMC payment status", "حالة دفعة العقد", new[] { ("pending", "Pending", "معلقة"), ("billed", "Billed", "مفوترة"), ("paid", "Paid", "مدفوعة") }),
        ("SCOPE_ITEM_TYPE", "Scope item type", "نوع بند النطاق", new[] { ("included", "Included", "مشمول"), ("excluded", "Excluded", "غير مشمول") }),
        ("CD_CERT_TYPE", "Civil Defense certificate type", "نوع شهادة الدفاع المدني", new[] { ("gas_installation", "Gas installation", "تمديدات الغاز"), ("fire_safety", "Fire safety", "السلامة من الحريق"), ("completion", "Completion", "إنجاز"), ("other", "Other", "أخرى") }),
        ("CD_STATUS", "Civil Defense status", "حالة الدفاع المدني", new[] { ("valid", "Valid", "سارية"), ("expired", "Expired", "منتهية"), ("renewal_in_progress", "Renewal in progress", "قيد التجديد") }),
        ("CD_RENEWAL_STATUS", "CD renewal status", "حالة التجديد", new[] { ("in_progress", "In progress", "قيد التنفيذ"), ("completed", "Completed", "مكتمل"), ("cancelled", "Cancelled", "ملغي") }),
        ("ACCOUNT_TYPE", "Account type", "نوع الحساب", new[] { ("asset", "Asset", "أصول"), ("liability", "Liability", "خصوم"), ("equity", "Equity", "حقوق ملكية"), ("revenue", "Revenue", "إيرادات"), ("expense", "Expense", "مصروفات") }),
        ("JOURNAL_STATUS", "Journal status", "حالة القيد", new[] { ("draft", "Draft", "مسودة"), ("posted", "Posted", "مرحل"), ("reversed", "Reversed", "معكوس") }),
        ("BILL_STATUS", "Vendor bill status", "حالة فاتورة المورد", new[] { ("draft", "Draft", "مسودة"), ("pending", "Pending", "معلقة"), ("partial", "Partially paid", "مسددة جزئياً"), ("paid", "Paid", "مسددة"), ("cancelled", "Cancelled", "ملغاة") }),
        ("NATIONALITY", "Nationality", "الجنسية", new[] { ("AE", "Emirati", "إماراتي"), ("SA", "Saudi", "سعودي"), ("EG", "Egyptian", "مصري"), ("JO", "Jordanian", "أردني"), ("SY", "Syrian", "سوري"), ("LB", "Lebanese", "لبناني"), ("IN", "Indian", "هندي"), ("PK", "Pakistani", "باكستاني"), ("PH", "Filipino", "فلبيني"), ("GB", "British", "بريطاني"), ("US", "American", "أمريكي"), ("OTHER", "Other", "أخرى") }),
    };

    public async Task SeedAsync()
    {
        try
        {
            if (!await _db.Database.CanConnectAsync())
            {
                _log.LogWarning("Seeder skipped: cannot connect to the database. Run sedra_schema_v2.2_sqlserver.sql first and check ConnectionStrings:SedraPetrol.");
                return;
            }
            await SeedLookupsAsync();
            var (companyId, branchIds) = await SeedOrganizationAsync();
            await SeedGasTypesAsync();
            await SeedSecurityAsync(companyId, branchIds);
            await SeedAccountingAsync(companyId);
            await SeedChecklistsAsync();
            _log.LogInformation("Seed completed.");
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Seeding failed.");
        }
    }

    private async Task SeedLookupsAsync()
    {
        var types = await _db.LookupTypes.IgnoreQueryFilters().ToListAsync();
        var values = await _db.LookupValues.IgnoreQueryFilters().ToListAsync();
        foreach (var (typeCode, en, ar, vals) in Lookups)
        {
            var type = types.FirstOrDefault(t => t.Code == typeCode);
            if (type == null)
            {
                type = new LookupType { Code = typeCode, NameEn = en, NameAr = ar, IsActive = true };
                _db.LookupTypes.Add(type);
                await _db.SaveChangesAsync();
            }
            var order = 0;
            foreach (var (code, ven, var) in vals)
            {
                order += 10;
                if (values.Any(v => v.LookupTypeId == type.Id && v.Code == code)) continue;
                _db.LookupValues.Add(new LookupValue { LookupTypeId = type.Id, Code = code, NameEn = ven, NameAr = var, SortOrder = order, IsActive = true });
            }
        }
        await _db.SaveChangesAsync();
    }

    private async Task<long> LookupId(string type, string code) =>
        await (from v in _db.LookupValues join t in _db.LookupTypes on v.LookupTypeId equals t.Id
               where t.Code == type && v.Code == code select v.Id).FirstAsync();

    private async Task<(long CompanyId, List<long> BranchIds)> SeedOrganizationAsync()
    {
        var aed = await _db.Currencies.FirstOrDefaultAsync(c => c.Code == "AED");
        if (aed == null)
        {
            aed = new Currency { Code = "AED", NameEn = "UAE Dirham", NameAr = "درهم إماراتي", Symbol = "د.إ", DecimalPlaces = 2, IsActive = true };
            _db.Currencies.Add(aed);
            _db.Currencies.Add(new Currency { Code = "USD", NameEn = "US Dollar", NameAr = "دولار أمريكي", Symbol = "$", DecimalPlaces = 2, IsActive = true });
            await _db.SaveChangesAsync();
        }

        var uae = await _db.Countries.FirstOrDefaultAsync(c => c.Code == "AE");
        if (uae == null)
        {
            uae = new Country { Code = "AE", NameEn = "United Arab Emirates", NameAr = "الإمارات العربية المتحدة", IsActive = true };
            _db.Countries.Add(uae);
            await _db.SaveChangesAsync();
        }

        var emirates = new (string Code, string En, string Ar, (string Code, string En, string Ar)[] Cities)[]
        {
            ("AUH", "Abu Dhabi", "أبوظبي", new[] { ("AUH", "Abu Dhabi", "أبوظبي"), ("AAN", "Al Ain", "العين") }),
            ("DXB", "Dubai", "دبي", new[] { ("DXB", "Dubai", "دبي") }),
            ("SHJ", "Sharjah", "الشارقة", new[] { ("SHJ", "Sharjah", "الشارقة") }),
            ("AJM", "Ajman", "عجمان", new[] { ("AJM", "Ajman", "عجمان") }),
            ("UAQ", "Umm Al Quwain", "أم القيوين", new[] { ("UAQ", "Umm Al Quwain", "أم القيوين") }),
            ("RAK", "Ras Al Khaimah", "رأس الخيمة", new[] { ("RAK", "Ras Al Khaimah", "رأس الخيمة") }),
            ("FUJ", "Fujairah", "الفجيرة", new[] { ("FUJ", "Fujairah", "الفجيرة") }),
        };
        foreach (var e in emirates)
        {
            var em = await _db.Emirates.FirstOrDefaultAsync(x => x.Code == e.Code);
            if (em == null)
            {
                em = new Emirate { CountryId = uae.Id, Code = e.Code, NameEn = e.En, NameAr = e.Ar };
                _db.Emirates.Add(em);
                await _db.SaveChangesAsync();
            }
            foreach (var c in e.Cities)
                if (!await _db.Cities.AnyAsync(x => x.Code == c.Code))
                    _db.Cities.Add(new City { EmirateId = em.Id, Code = c.Code, NameEn = c.En, NameAr = c.Ar, IsActive = true });
        }
        await _db.SaveChangesAsync();

        var company = await _db.Companies.FirstOrDefaultAsync();
        if (company == null)
        {
            company = new Company { Code = "SEDRA", NameEn = "Sedra Petrol", NameAr = "سدرة للبترول", BaseCurrencyId = aed.Id, IsActive = true };
            _db.Companies.Add(company);
            await _db.SaveChangesAsync();
        }

        if (!await _db.Branches.AnyAsync())
        {
            foreach (var code in new[] { "AUH", "DXB", "SHJ", "AJM", "FUJ" })
            {
                var city = await _db.Cities.FirstAsync(c => c.Code == code);
                _db.Branches.Add(new Branch { CompanyId = company.Id, CityId = city.Id, Code = code, NameEn = city.NameEn, NameAr = city.NameAr, IsActive = true });
            }
            await _db.SaveChangesAsync();
        }
        return (company.Id, await _db.Branches.Select(b => b.Id).ToListAsync());
    }

    private async Task SeedGasTypesAsync()
    {
        if (!await _db.GasTypes.AnyAsync(g => g.Code == "LPG"))
            _db.GasTypes.Add(new GasType { Code = "LPG", NameEn = "Liquefied petroleum gas", NameAr = "غاز البترول المسال", LitreToM3 = 0.27m, IsActive = true });
        if (!await _db.GasTypes.AnyAsync(g => g.Code == "NG"))
            _db.GasTypes.Add(new GasType { Code = "NG", NameEn = "Natural gas", NameAr = "الغاز الطبيعي", LitreToM3 = 1m, IsActive = true });
        await _db.SaveChangesAsync();
    }

    private async Task SeedSecurityAsync(long companyId, List<long> branchIds)
    {
        var existing = await _db.Permissions.Select(p => p.Code).ToListAsync();
        foreach (var p in PermissionCatalog.All().Where(p => !existing.Contains(p.Code)))
            _db.Permissions.Add(new Permission { Code = p.Code, Module = p.Module, NameEn = p.NameEn, NameAr = p.NameAr, IsActive = true });
        await _db.SaveChangesAsync();

        var roles = new (string Code, string En, string Ar, Func<string, bool> Grants)[]
        {
            ("ADMIN", "Administrator", "مدير النظام", _ => true),
            ("MANAGER", "Operations manager", "مدير العمليات", c => !c.StartsWith("users.") && !c.StartsWith("accounting.close")),
            ("ACCOUNTANT", "Accountant", "محاسب", c => c.EndsWith(".view") || c.StartsWith("invoices") || c.StartsWith("payments") || c.StartsWith("payables") || c.StartsWith("accounting")),
            ("TECHNICIAN", "Technician", "فني", c => c.EndsWith(".view") && !c.StartsWith("accounting") || c is "meters.manage" or "maintenance.manage" or "service_requests.manage" or "tanks.manage"),
            ("READER", "Meter reader", "قارئ عدادات", c => c is "dashboard.view" or "buildings.view" or "meters.view" or "meters.manage" or "tanks.view"),
        };
        var perms = await _db.Permissions.ToListAsync();
        foreach (var (code, en, ar, grants) in roles)
        {
            var role = await _db.Roles.FirstOrDefaultAsync(r => r.Code == code);
            if (role != null) continue; // never overwrite customised roles
            role = new Role { Code = code, NameEn = en, NameAr = ar };
            _db.Roles.Add(role);
            await _db.SaveChangesAsync();
            _db.RolePermissions.AddRange(perms.Where(p => grants(p.Code)).Select(p => new RolePermission { RoleId = role.Id, PermissionId = p.Id }));
            await _db.SaveChangesAsync();
        }

        var email = _cfg["Seed:AdminEmail"] ?? "admin@sedrapetrol.ae";
        if (!await _db.AppUsers.IgnoreQueryFilters().AnyAsync(u => u.Email == email))
        {
            var admin = new AppUser { CompanyId = companyId, BranchId = branchIds.FirstOrDefault(), NameEn = "System Administrator", NameAr = "مدير النظام", Email = email, IsActive = true };
            admin.PasswordHash = new PasswordHasher<AppUser>().HashPassword(admin, _cfg["Seed:AdminPassword"] ?? "Admin@12345");
            _db.AppUsers.Add(admin);
            await _db.SaveChangesAsync();
            var adminRole = await _db.Roles.FirstAsync(r => r.Code == "ADMIN");
            _db.UserRoles.Add(new UserRole { UserId = admin.Id, RoleId = adminRole.Id });
            _db.UserBranches.AddRange(branchIds.Select(b => new UserBranch { UserId = admin.Id, BranchId = b }));
            await _db.SaveChangesAsync();
            _log.LogWarning("Created admin user {Email}. Change the password after the first login.", email);
        }
    }

    private async Task SeedAccountingAsync(long companyId)
    {
        if (!await _db.Accounts.AnyAsync(a => a.CompanyId == companyId))
        {
            var aed = await _db.Currencies.Where(c => c.Code == "AED").Select(c => (long?)c.Id).FirstAsync();
            var chart = new (string Code, string? Parent, string Type, string En, string Ar, bool Group)[]
            {
                ("1", null, "asset", "Assets", "الأصول", true),
                ("11", "1", "asset", "Cash & banks", "النقدية والبنوك", true),
                ("1101", "11", "asset", "Cash on hand", "الصندوق", false),
                ("1102", "11", "asset", "Bank account", "الحساب البنكي", false),
                ("1103", "11", "asset", "Cheques under collection", "شيكات تحت التحصيل", false),
                ("12", "1", "asset", "Receivables", "الذمم المدينة", true),
                ("1201", "12", "asset", "Tenants receivable", "ذمم المستأجرين", false),
                ("1301", "1", "asset", "VAT recoverable (input)", "ضريبة القيمة المضافة المدخلات", false),
                ("1401", "1", "asset", "Gas inventory", "مخزون الغاز", false),
                ("2", null, "liability", "Liabilities", "الخصوم", true),
                ("2101", "2", "liability", "Vendors payable", "ذمم الموردين", false),
                ("2201", "2", "liability", "VAT payable (output)", "ضريبة القيمة المضافة المخرجات", false),
                ("2301", "2", "liability", "Customer deposits", "تأمينات العملاء", false),
                ("3", null, "equity", "Equity", "حقوق الملكية", true),
                ("3101", "3", "equity", "Capital", "رأس المال", false),
                ("3201", "3", "equity", "Retained earnings", "الأرباح المحتجزة", false),
                ("4", null, "revenue", "Revenue", "الإيرادات", true),
                ("4101", "4", "revenue", "Gas sales", "مبيعات الغاز", false),
                ("4102", "4", "revenue", "Maintenance revenue", "إيرادات الصيانة", false),
                ("4103", "4", "revenue", "Connection & service fees", "رسوم التوصيل والخدمات", false),
                ("4104", "4", "revenue", "Other revenue", "إيرادات أخرى", false),
                ("5", null, "expense", "Expenses", "المصروفات", true),
                ("5101", "5", "expense", "Gas purchases", "مشتريات الغاز", false),
                ("5102", "5", "expense", "Maintenance contracts (AMC)", "عقود الصيانة", false),
                ("5103", "5", "expense", "Transport", "النقل", false),
                ("5104", "5", "expense", "Other expenses", "مصروفات أخرى", false),
            };
            var created = new Dictionary<string, Account>();
            foreach (var a in chart)
            {
                var acc = new Account
                {
                    CompanyId = companyId, AccountCode = a.Code, NameEn = a.En, NameAr = a.Ar,
                    AccountTypeId = await LookupId("ACCOUNT_TYPE", a.Type), ParentAccountId = a.Parent == null ? null : created[a.Parent].Id,
                    Level = (short)(a.Parent == null ? 1 : created[a.Parent].Level!.Value + 1), IsGroup = a.Group, IsPostable = !a.Group, CurrencyId = aed, IsActive = true,
                };
                _db.Accounts.Add(acc);
                await _db.SaveChangesAsync();
                created[a.Code] = acc;
            }

            long Acc(string code) => created[code].Id;
            async Task Map(string source, string? keyType, string? keyCode, string? debit, string? credit) =>
                _db.AccountMappings.Add(new AccountMapping
                {
                    CompanyId = companyId, SourceType = source, IsActive = true,
                    SourceKeyId = keyType == null ? null : await LookupId(keyType, keyCode!),
                    DebitAccountId = debit == null ? null : Acc(debit), CreditAccountId = credit == null ? null : Acc(credit),
                });
            await Map(MappingSources.Receivable, null, null, "1201", "1201");
            await Map(MappingSources.Vat, null, null, null, "2201");
            await Map(MappingSources.VatInput, null, null, "1301", null);
            await Map(MappingSources.Payable, null, null, "2101", "2101");
            await Map(MappingSources.Deposit, null, null, null, "2301");
            await Map(MappingSources.InvoiceItem, null, null, null, "4104");                       // fallback
            await Map(MappingSources.InvoiceItem, "FEE_TYPE", "gas_price", null, "4101");
            await Map(MappingSources.InvoiceItem, "FEE_TYPE", "maintenance", null, "4102");
            await Map(MappingSources.InvoiceItem, "FEE_TYPE", "connection", null, "4103");
            await Map(MappingSources.InvoiceItem, "FEE_TYPE", "disconnection", null, "4103");
            await Map(MappingSources.InvoiceItem, "FEE_TYPE", "inspection", null, "4103");
            await Map(MappingSources.InvoiceItem, "FEE_TYPE", "meter_rent", null, "4103");
            await Map(MappingSources.InvoiceItem, "FEE_TYPE", "deposit", null, "2301");
            await Map(MappingSources.VendorBillItem, null, null, "5104", null);                    // fallback
            await Map(MappingSources.VendorBillItem, "FEE_TYPE", "gas_purchase", "5101", null);
            await Map(MappingSources.VendorBillItem, "FEE_TYPE", "gas_price", "5101", null);
            await Map(MappingSources.VendorBillItem, "FEE_TYPE", "maintenance", "5102", null);
            foreach (var (method, acc) in new[] { ("cash", "1101"), ("bank_transfer", "1102"), ("online", "1102"), ("card", "1102"), ("cheque", "1103") })
            {
                await Map(MappingSources.Payment, "PAYMENT_METHOD", method, acc, null);
                await Map(MappingSources.VendorPayment, "PAYMENT_METHOD", method, null, method == "cheque" ? "1102" : acc);
            }
            await _db.SaveChangesAsync();
        }

        var year = (short)DateTime.Today.Year;
        foreach (var y in new[] { year, (short)(year + 1) })
        {
            if (await _db.FiscalPeriods.AnyAsync(p => p.CompanyId == companyId && p.Year == y)) continue;
            for (short m = 1; m <= 12; m++)
            {
                var start = new DateOnly(y, m, 1);
                _db.FiscalPeriods.Add(new FiscalPeriod { CompanyId = companyId, Year = y, PeriodNo = m, StartDate = start, EndDate = start.AddMonths(1).AddDays(-1), IsClosed = false });
            }
        }
        await _db.SaveChangesAsync();
    }

    private async Task SeedChecklistsAsync()
    {
        if (await _db.ChecklistTemplates.AnyAsync()) return;
        var tpl = new ChecklistTemplate { VisitTypeId = await LookupId("VISIT_TYPE", "preventive"), NameEn = "Preventive gas system visit", NameAr = "زيارة صيانة وقائية لنظام الغاز", IsActive = true };
        _db.ChecklistTemplates.Add(tpl);
        await _db.SaveChangesAsync();
        var items = new (string En, string Ar)[]
        {
            ("Inspect tank and valves for leaks", "فحص الخزان والصمامات للتأكد من عدم وجود تسرب"),
            ("Check pressure regulators", "فحص منظمات الضغط"),
            ("Test gas leak detectors and alarms", "اختبار أجهزة كشف التسرب والإنذار"),
            ("Inspect piping, joints and supports", "فحص الأنابيب والوصلات والدعامات"),
            ("Check emergency shut-off valves", "فحص صمامات الإغلاق الطارئ"),
            ("Inspect meters and seals", "فحص العدادات والأختام"),
            ("Verify fire extinguishers and signage", "التحقق من طفايات الحريق واللوحات الإرشادية"),
        };
        var order = 0;
        _db.ChecklistTemplateItems.AddRange(items.Select(i => new ChecklistTemplateItem { TemplateId = tpl.Id, ItemTextEn = i.En, ItemTextAr = i.Ar, SortOrder = order += 10 }));
        await _db.SaveChangesAsync();
    }
}
