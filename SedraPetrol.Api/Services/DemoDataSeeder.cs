using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Services;

/// <summary>
/// Demo data for presenting the system: two residential buildings (5 floors × 6 flats), tenants with gas supply contracts,
/// meters, monthly readings and invoices for the past months, customer payments (some still unpaid / overdue) and an AMC contract
/// per building. Turned on with <c>Seed:DemoData = true</c>; runs once after the API has started (skipped when the demo buildings
/// exist) and goes through the API itself, so every invoice, receipt and journal entry is created exactly as the portal would.
/// Remove it later with the delete-all-data script.
/// </summary>
public static class DemoDataSeeder
{
    private record BuildingSpec(string NameEn, string NameAr, string Branch, string City, string Street, string Zone,
        string OwnerEn, string OwnerAr, string OwnerPhone, decimal GasPrice, decimal AmcValue);

    private static readonly BuildingSpec[] Buildings =
    {
        new("Al Reem Residence", "ريم ريزيدنس", "AUH", "AUH", "Shams Boutik Street", "Al Reem Island",
            "Saeed Rashid Al Mansoori", "سعيد راشد المنصوري", "0501234567", 15.00m, 24000m),
        new("Corniche View Building", "بناية إطلالة الكورنيش", "AJM", "AJM", "Corniche Street", "Al Rashidiya 1",
            "Mohammed Hamad Al Nuaimi", "محمد حمد النعيمي", "0559876543", 14.50m, 18000m),
    };

    private static readonly (string En, string Ar, string Nat)[] People =
    {
        ("Ahmed Khalid Al Hammadi", "أحمد خالد الحمادي", "AE"), ("Rajesh Kumar Nair", "راجيش كومار ناير", "IN"),
        ("Mohamed Adel Hassan", "محمد عادل حسن", "EG"), ("Maria Cristina Santos", "ماريا كريستينا سانتوس", "PH"),
        ("Omar Yousef Haddad", "عمر يوسف حداد", "JO"), ("Fatima Noor Al Zaabi", "فاطمة نور الزعابي", "AE"),
        ("Imran Ali Sheikh", "عمران علي شيخ", "PK"), ("Rami Georges Khoury", "رامي جورج خوري", "LB"),
        ("Hala Mahmoud Darwish", "هالة محمود درويش", "SY"), ("Suresh Babu Menon", "سوريش بابو مينون", "IN"),
        ("Khaled Abdullah Al Qahtani", "خالد عبدالله القحطاني", "SA"), ("John David Mitchell", "جون ديفيد ميتشل", "GB"),
        ("Aisha Salem Al Ketbi", "عائشة سالم الكتبي", "AE"), ("Tariq Mehmood Butt", "طارق محمود بت", "PK"),
        ("Nour Eldin Farouk", "نور الدين فاروق", "EG"), ("Anil Joseph Thomas", "أنيل جوزيف توماس", "IN"),
        ("Lina Samir Abboud", "لينا سمير عبود", "LB"), ("Hamad Saif Al Shamsi", "حمد سيف الشامسي", "AE"),
        ("Jose Miguel Reyes", "خوسيه ميغيل رييس", "PH"), ("Yazan Khalil Nasser", "يزن خليل ناصر", "JO"),
        ("Priya Ramesh Iyer", "بريا راميش آيير", "IN"), ("Mostafa Ibrahim Saleh", "مصطفى إبراهيم صالح", "EG"),
        ("Bilal Ahmed Qureshi", "بلال أحمد قريشي", "PK"), ("Sara Waleed Al Masri", "سارة وليد المصري", "SY"),
        ("Michael Robert Hayes", "مايكل روبرت هايز", "US"), ("Mariam Obaid Al Suwaidi", "مريم عبيد السويدي", "AE"),
        ("Vikram Singh Rathore", "فيكرام سينغ راثور", "IN"), ("Ziad Fouad Haidar", "زياد فؤاد حيدر", "LB"),
        ("Reem Hassan Al Najjar", "ريم حسن النجار", "JO"), ("Faisal Nasser Al Otaibi", "فيصل ناصر العتيبي", "SA"),
        ("Grace Ann Villanueva", "غريس آن فيلانويفا", "PH"), ("Hesham Tarek Mansour", "هشام طارق منصور", "EG"),
        ("Deepak Chandra Pillai", "ديباك شاندرا بيلاي", "IN"), ("Usman Farooq Malik", "عثمان فاروق مالك", "PK"),
        ("Rana Adnan Shaheen", "رنا عدنان شاهين", "SY"), ("Abdulla Mohammed Al Blooshi", "عبدالله محمد البلوشي", "AE"),
        ("Samuel James Carter", "صموئيل جيمس كارتر", "GB"), ("Nadia Karim Bitar", "نادية كريم بيطار", "LB"),
        ("Arjun Prakash Rao", "أرجون براكاش راو", "IN"), ("Waleed Saad Al Harbi", "وليد سعد الحربي", "SA"),
        ("Joanna Marie Cruz", "جوانا ماري كروز", "PH"), ("Karim Samy Abdelaziz", "كريم سامي عبدالعزيز", "EG"),
        ("Hassan Raza Chaudhry", "حسن رضا تشودري", "PK"), ("Dana Fadi Saleh", "دانة فادي صالح", "JO"),
        ("Rashid Juma Al Muhairi", "راشد جمعة المهيري", "AE"), ("Kiran Lakshmi Das", "كيران لاكشمي داس", "IN"),
        ("Elias Antoine Haddad", "إلياس أنطوان حداد", "LB"), ("Majed Fahad Al Dosari", "ماجد فهد الدوسري", "SA"),
    };

    private static readonly string[] ServiceTypes = { "walk_in", "walk_in", "contract_delivery", "vip", "night_shift" };

    public static async Task RunAsync(WebApplication app)
    {
        var log = app.Logger;
        try
        {
            var cfg = app.Configuration;
            var baseUrl = app.Urls.FirstOrDefault(u => u.StartsWith("http://")) ?? app.Urls.FirstOrDefault() ?? "http://localhost:5080";
            baseUrl = baseUrl.Replace("://*", "://localhost").Replace("://+", "://localhost").Replace("://0.0.0.0", "://localhost");
            using var handler = new HttpClientHandler { ServerCertificateCustomValidationCallback = (_, _, _, _) => true };
            using var http = new HttpClient(handler) { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromMinutes(2) };

            var login = await Send(http, HttpMethod.Post, "/api/auth/login", new { email = cfg["Seed:AdminEmail"], password = cfg["Seed:AdminPassword"] });
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!["token"]!.GetValue<string>());

            log.LogInformation("Demo data: checking / creating 2 buildings, 60 flats, tenants, invoices, payments …");
            // the history goes back up to 15 months: make sure those fiscal periods exist, otherwise no journal entries are posted
            for (var y = DateTime.Today.Year - 2; y <= DateTime.Today.Year; y++)
                await Send(http, HttpMethod.Post, $"/api/fiscal-periods/generate/{y}");
            var branches = (await Send(http, HttpMethod.Get, "/api/branches?pageSize=0"))!["items"]!.AsArray();
            var cities = (await Send(http, HttpMethod.Get, "/api/cities?pageSize=0"))!["items"]!.AsArray();
            long IdOf(JsonArray list, string code) => list.First(x => x!["code"]!.GetValue<string>() == code)!["id"]!.GetValue<long>();
            var termOptions = (await Send(http, HttpMethod.Get, "/api/contract-term-templates/options?contractType=supply"))!.AsArray();
            var defaultTerms = termOptions.Where(t => t!["isDefault"]!.GetValue<bool>())
                .Select(t => new { templateId = t!["id"]!.GetValue<long>(), category = t["category"]!.GetValue<string>(), text = t["textAr"]!.GetValue<string>() }).ToList();

            var today = DateOnly.FromDateTime(DateTime.Today);
            var tenantsMade = 0;
            var failures = 0;

            for (var bi = 0; bi < Buildings.Length; bi++)
            {
                var spec = Buildings[bi];
                var branchId = IdOf(branches, spec.Branch);
                var cityId = IdOf(cities, spec.City);

                // resume: reuse the building when an earlier run created it
                var found = (await Send(http, HttpMethod.Get, $"/api/buildings?pageSize=0&search={Uri.EscapeDataString(spec.NameEn)}"))?["items"]?.AsArray()
                    .FirstOrDefault(x => (x?["nameEn"]?.GetValue<string>() ?? x?["name"]?.GetValue<string>()) == spec.NameEn);
                long buildingId;
                if (found != null) buildingId = found["id"]!.GetValue<long>();
                else
                {
                    var units = new List<object>();
                    for (var floor = 1; floor <= 5; floor++)
                        for (var n = 1; n <= 6; n++)
                            units.Add(new { no = $"{floor}{n:00}", floor, type = floor == 1 && n <= 2 ? "commercial" : "residential",
                                            area = 85 + (n % 3) * 35, bedrooms = (short)(1 + n % 3), bathrooms = (short)(1 + n % 2) });
                    var b = await Send(http, HttpMethod.Post, "/api/buildings", new
                    {
                        branchId, nameEn = spec.NameEn, nameAr = spec.NameAr, cityId, street = spec.Street, zone = spec.Zone, totalFloors = 5, status = "active",
                        owners = new[] { new { party = new { partyType = "individual", nameEn = spec.OwnerEn, nameAr = spec.OwnerAr, phone = spec.OwnerPhone, cityId, poBox = $"{4100 + bi * 731}" },
                                               isPrimary = true, ownershipPercentage = 100m } },
                        tanks = new[] { new { tankNo = "T-1", gasType = "LPG", capacityLitres = 4000m, currentLevelLitres = 2600m - bi * 900m, serialNo = $"TNK-{spec.Branch}-{2019 + bi}-01",
                                              manufacturer = "Emirates Gas Equipment", installDate = new DateOnly(2019 + bi, 3, 15) } },
                        units,
                    });
                    buildingId = b!["id"]!.GetValue<long>();
                }
                var unitRows = (await Send(http, HttpMethod.Get, $"/api/units?buildingId={buildingId}&pageSize=0"))!["items"]!.AsArray()
                    .Select(u => (Id: u!["id"]!.GetValue<long>(), No: u["no"]!.GetValue<string>(), Commercial: u["type"]?.GetValue<string>() == "commercial"))
                    .OrderBy(u => u.No).ToList();
                var meters = (await Send(http, HttpMethod.Get, $"/api/meters?buildingId={buildingId}&pageSize=0"))!["items"]!.AsArray()
                    .ToDictionary(m => m!["meterNo"]!.GetValue<string>(), m => m!["id"]!.GetValue<long>());
                var contracts = (await Send(http, HttpMethod.Get, $"/api/supply-contracts?buildingId={buildingId}&pageSize=0"))!["items"]!.AsArray()
                    .Where(c => c?["unit"] != null).GroupBy(c => c!["unit"]!.GetValue<string>()).ToDictionary(g => g.Key, g => g.First()!);

                var occupiedIndex = 0;
                foreach (var u in unitRows)
                {
                    var vacant = u.No is "205" or "402" or "506" or "303";          // 4 of 30 flats stay vacant
                    var rnd = new Random(2026 * (bi + 1) + int.Parse(u.No));        // same data on every run (resume-safe)
                    var personIndex = bi * 26 + (vacant ? 0 : occupiedIndex++);
                    try
                    {
                        var meterNo = $"GM-{spec.Branch}-{u.No}";
                        if (!meters.TryGetValue(meterNo, out var meterId))
                        {
                            var meter = await Send(http, HttpMethod.Post, "/api/meters", new
                            {
                                branchId, meterNo, serial = $"ITR{2400000 + bi * 1000 + int.Parse(u.No)}", unitId = u.Id, type = "smart",
                                brand = "Itron", model = "G4 RF1", installDate = new DateOnly(2024, 1, 10), initialReading = 0m, calibrationDate = new DateOnly(2024, 1, 5),
                            });
                            meterId = meter!["id"]!.GetValue<long>();
                        }
                        if (vacant) continue;

                        var p = People[personIndex % People.Length];
                        var monthsAgo = rnd.Next(3, 15);
                        var start = new DateOnly(today.Year, today.Month, 1).AddMonths(-monthsAgo).AddDays(rnd.Next(0, 20));
                        var opening = Math.Round((decimal)(rnd.NextDouble() * 400 + 50), 1);
                        var deposit = u.Commercial ? 1500m : 600m;
                        long tenantId, contractId;
                        if (contracts.TryGetValue(u.No, out var existingContract))
                        {
                            tenantId = existingContract["tenantId"]!.GetValue<long>();
                            contractId = existingContract["id"]!.GetValue<long>();
                            var invCount = (await Send(http, HttpMethod.Get, $"/api/invoices?pageSize=1&tenantId={tenantId}&from=2000-01-01"))?["total"]?.GetValue<int>() ?? 0;
                            if (invCount > 0) continue;                                  // already complete
                            var detail = await Send(http, HttpMethod.Get, $"/api/supply-contracts/{contractId}");
                            start = DateOnly.Parse(detail!["contract"]!["startDate"]!.GetValue<string>());
                            opening = detail["contract"]!["openingReading"]?.GetValue<decimal>() ?? opening;
                        }
                        else
                        {
                            var phone = $"05{new[] { 0, 2, 5, 6, 8 }[rnd.Next(5)]}{rnd.Next(1000000, 9999999)}";
                            var email = $"{p.En.Split(' ')[0].ToLowerInvariant()}.{p.En.Split(' ')[^1].ToLowerInvariant()}{rnd.Next(10, 99)}@gmail.com";
                            var t = await Send(http, HttpMethod.Post, "/api/tenants", new
                            {
                                branchId, nameEn = p.En, nameAr = p.Ar, phone, email, nationality = p.Nat, cityId,
                                emiratesId = $"784-{rnd.Next(1970, 2001)}-{rnd.Next(1000000, 9999999)}-{rnd.Next(1, 9)}", unitId = u.Id,
                                contract = new
                                {
                                    unitId = u.Id, startDate = start, durationMonths = 24, autoRenew = true, renewalPeriodMonths = (short)12,
                                    openingReading = opening, openingReadingDate = new DateTimeOffset(start.ToDateTime(new TimeOnly(10, 0)), TimeSpan.FromHours(4)),
                                    depositAmount = deposit, status = "active",
                                    serviceType = ServiceTypes[rnd.Next(ServiceTypes.Length)], usageType = u.Commercial ? "commercial" : "residential",
                                    billByEmail = true, billByPost = rnd.Next(5) == 0, billingCycleMonths = (short)1,
                                    fees = new object[]
                                    {
                                        new { type = "gas_price", cycle = "per_m3", amount = spec.GasPrice, effectiveFrom = start },
                                        new { type = "service", cycle = "monthly", amount = u.Commercial ? 25m : 12m, effectiveFrom = start },
                                        new { type = "connection", cycle = "one_time", amount = 100m, effectiveFrom = start },
                                    },
                                    termItems = defaultTerms,
                                },
                            });
                            tenantId = t!["id"]!.GetValue<long>();
                            contractId = t["contractId"]!.GetValue<long>();
                        }
                        tenantsMade++;
                        await HistoryAsync(http, rnd, today, buildingId, tenantId, contractId, meterId, start, opening, deposit, u.Commercial);
                    }
                    catch (Exception ex)
                    {
                        failures++;
                        log.LogWarning("Demo data: flat {Unit} of {Building} skipped – {Error}", u.No, spec.NameEn, ex.Message);
                    }
                }

                // annual maintenance contract with the owner, paid in two instalments
                var hasAmc = (await Send(http, HttpMethod.Get, $"/api/amc-contracts?buildingId={buildingId}&pageSize=1"))?["total"]?.GetValue<int>() > 0;
                if (!hasAmc)
                {
                    var amcStart = new DateOnly(today.Year, 1, 1);
                    try
                    {
                        await Send(http, HttpMethod.Post, "/api/amc-contracts", new
                        {
                            buildingId, startDate = amcStart, endDate = amcStart.AddYears(1).AddDays(-1), annualValue = spec.AmcValue, visitFrequency = "quarterly",
                            responseTimeHours = (short)4, emergencyIncluded = true, status = "active",
                            scope = new[] { "فحص الخزان والتجهيزات", "فحص لوحة التوزيع الرئيسية وخطوط التوزيع", "فحص التسرب", "فحص نظام كشف التسرب والكواشف" },
                            excludedScope = new[] { "استبدال قطع الغيار الرئيسية" },
                            paymentSchedule = new[]
                            {
                                new { label = "First payment", dueDate = amcStart.AddMonths(6).AddDays(-1), amount = spec.AmcValue / 2, notes = (string?)null },
                                new { label = "Second payment", dueDate = amcStart.AddYears(1).AddDays(-1), amount = spec.AmcValue / 2, notes = (string?)null },
                            },
                            generateVisits = true, autoPaymentSchedule = false,
                        });
                    }
                    catch (Exception ex) { failures++; log.LogWarning("Demo data: AMC of {Building} skipped – {Error}", spec.NameEn, ex.Message); }
                }
                log.LogInformation("Demo building {Name} ready.", spec.NameEn);
            }
            if (failures > 0) log.LogWarning("Demo data: {Count} item(s) failed – restart the API to complete them.", failures);
            await PostMissingJournalsAsync(app);
            log.LogInformation("Demo data ready: {Buildings} buildings, {Tenants} tenants.", Buildings.Length, tenantsMade);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Demo data was not completed.");
        }
    }

    /// <summary>
    /// Invoices and receipts saved while their fiscal period did not exist yet have no journal entry: post them now
    /// (same posting rules as when they were created), so the ledger, trial balance and reports include them.
    /// </summary>
    private static async Task PostMissingJournalsAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SedraPetrol.Api.Data.AppDbContext>();
        var posting = scope.ServiceProvider.GetRequiredService<PostingService>();
        var billing = scope.ServiceProvider.GetRequiredService<BillingService>();
        var frozen = await billing.InvoiceStatusIdsAsync("draft", "cancelled");
        var dead = await billing.DeadPaymentStatusIdsAsync();
        var invoices = await db.Invoices.Where(i => i.JournalEntryId == null && (i.StatusId == null || !frozen.Contains(i.StatusId.Value)))
            .OrderBy(i => i.IssueDate).ThenBy(i => i.Id).ToListAsync();
        var posted = 0;
        foreach (var inv in invoices)
        {
            var items = await db.InvoiceItems.Where(x => x.InvoiceId == inv.Id).ToListAsync();
            inv.JournalEntryId = (await posting.PostInvoiceAsync(inv, items))?.Id;
            if (inv.JournalEntryId != null) posted++;
            await db.SaveChangesAsync();
        }
        var payments = await db.Payments.Where(p => p.JournalEntryId == null && (p.StatusId == null || !dead.Contains(p.StatusId.Value)))
            .OrderBy(p => p.PaymentDate).ThenBy(p => p.Id).ToListAsync();
        foreach (var pay in payments)
        {
            pay.JournalEntryId = (await posting.PostPaymentAsync(pay))?.Id;
            if (pay.JournalEntryId != null) posted++;
            await db.SaveChangesAsync();
        }
        if (posted > 0) app.Logger.LogInformation("Demo data: {Count} missing journal entries posted.", posted);
    }

    /// <summary>Move-in invoice (paid), then a reading at every month end with that month's invoice and, mostly, its payment.</summary>
    private static async Task HistoryAsync(HttpClient http, Random rnd, DateOnly today, long buildingId, long tenantId, long contractId, long meterId,
        DateOnly start, decimal opening, decimal deposit, bool commercial)
    {
        var payer = rnd.Next(10);          // 0 = late payer: the last 3 bills stay unpaid
        var method = new[] { "cash", "bank_transfer", "bank_transfer", "card", "online" }[rnd.Next(5)];

        var moveIn = await Send(http, HttpMethod.Post, "/api/invoices", new
        {
            tenantId, buildingId, contractId, type = "connection", issueDate = start, dueDate = start, issue = true,
            notes = "Move-in: gas connection and refundable deposit",
            items = new object[]
            {
                new { itemType = "connection", description = "Gas connection to the kitchen", quantity = 1m, unitPrice = 100m, vatRate = 5m },
                new { itemType = "deposit", description = "Refundable gas deposit", quantity = 1m, unitPrice = deposit, vatRate = 0m },
            },
        });
        await Send(http, HttpMethod.Post, "/api/payments", new
        {
            tenantId, amount = moveIn!["totalAmount"]!.GetValue<decimal>(), date = start, method = "cash",
            invoiceId = moveIn["id"]!.GetValue<long>(), notes = "Move-in payment",
        });

        var reading = opening;
        var lastMonth = new DateOnly(today.Year, today.Month, 1).AddMonths(-1);
        for (var m = new DateOnly(start.Year, start.Month, 1); m <= lastMonth; m = m.AddMonths(1))
        {
            var end = m.AddMonths(1).AddDays(-1);
            var use = commercial ? rnd.Next(25, 70) + Math.Round((decimal)rnd.NextDouble(), 1) : rnd.Next(3, 15) + Math.Round((decimal)rnd.NextDouble(), 1);
            if (m.Month is 12 or 1 or 2) use = Math.Round(use * 1.25m, 1);   // more hot water in winter
            reading += use;
            await Send(http, HttpMethod.Post, $"/api/meters/{meterId}/readings", new
            {
                readingValue = reading, readingDate = new DateTimeOffset(end.ToDateTime(new TimeOnly(11, 0)), TimeSpan.FromHours(4)), method = "manual",
            });
            var issueDate = end.AddDays(1);
            if (issueDate > today) continue;
            var inv = await Send(http, HttpMethod.Post, $"/api/supply-contracts/{contractId}/generate-invoice", new
            {
                periodStart = m < start ? start : m, periodEnd = end, issue = true, issueDate,
            });
            if (inv?["created"]?.GetValue<bool>() != true) continue;

            var monthsOld = (today.DayNumber - issueDate.DayNumber) / 30;
            var unpaid = monthsOld == 0 ? rnd.Next(10) < 7      // last month's bill: mostly still open
                       : payer == 0 ? monthsOld <= 3              // late payers
                       : rnd.Next(20) == 0;                        // a few forgotten bills
            if (unpaid) continue;
            var payDate = issueDate.AddDays(rnd.Next(2, 26));
            if (payDate > today) payDate = today;
            await Send(http, HttpMethod.Post, "/api/payments", new
            {
                tenantId, amount = inv["totalAmount"]!.GetValue<decimal>(), date = payDate, method,
                invoiceId = inv["id"]!.GetValue<long>(), reference = method == "bank_transfer" ? $"FT{rnd.Next(100000000, 999999999)}" : null,
            });
        }
        // this month's reading, not billed yet (for half of the flats) — shows "reading → invoice" live in the demo
        if (rnd.Next(2) == 0 && today.Day > 3)
            await Send(http, HttpMethod.Post, $"/api/meters/{meterId}/readings", new
            {
                readingValue = reading + rnd.Next(2, 8), readingDate = new DateTimeOffset(today.AddDays(-2).ToDateTime(new TimeOnly(10, 30)), TimeSpan.FromHours(4)),
                method = "manual",
            });
    }

    /// <summary>Calls the API; a conflict or server error (e.g. two documents taking the same number at once) is retried a few times.</summary>
    private static async Task<JsonNode?> Send(HttpClient http, HttpMethod method, string url, object? body = null)
    {
        for (var attempt = 1; ; attempt++)
        {
            using var req = new HttpRequestMessage(method, url);
            if (body != null) req.Content = JsonContent.Create(body, options: new JsonSerializerOptions(JsonSerializerDefaults.Web));
            using var res = await http.SendAsync(req);
            var text = await res.Content.ReadAsStringAsync();
            if (res.IsSuccessStatusCode) return string.IsNullOrWhiteSpace(text) ? null : JsonNode.Parse(text);
            var retry = (int)res.StatusCode >= 500 || (res.StatusCode == System.Net.HttpStatusCode.Conflict && text.Contains("duplicate"));
            if (!retry || attempt >= 4) throw new InvalidOperationException($"{method} {url} → {(int)res.StatusCode}: {text}");
            await Task.Delay(1500 * attempt);
        }
    }
}
