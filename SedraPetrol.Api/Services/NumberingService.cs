using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SedraPetrol.Api.Common;
using SedraPetrol.Api.Data;

namespace SedraPetrol.Api.Services;

/// <summary>Business number prefixes stored in number_sequence.prefix.</summary>
public static class NumberPrefixes
{
    public const string Building = "B", Tenant = "T", SupplyContract = "GSC", Invoice = "INV", Receipt = "RCP",
        Refill = "RFR", ServiceRequest = "SR", Amc = "AMC", Journal = "JE", PurchaseOrder = "PO",
        VendorPayment = "VP", MeterService = "MS", Visit = "MV", Meter = "M", Vendor = "V";
}

/// <summary>
/// Generates business numbers (B-001, GSC-2026-0001 ...) from number_sequence,
/// using an UPDLOCK so two concurrent requests never get the same number.
/// </summary>
public class NumberingService
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _user;

    public NumberingService(AppDbContext db, ICurrentUser user)
    {
        _db = db; _user = user;
    }

    /// <param name="prefix">e.g. "GSC"</param>
    /// <param name="yearly">true → PREFIX-YYYY-NNNN, false → PREFIX-NNN</param>
    /// <param name="padding">digits used when the sequence row is created</param>
    /// <param name="date">document date (decides the year part)</param>
    public async Task<string> NextAsync(string prefix, bool yearly = true, short padding = 4, DateOnly? date = null)
    {
        var companyId = _user.CompanyId ?? await _db.Companies.Select(c => (long?)c.Id).FirstOrDefaultAsync()
                        ?? throw new AppException("No company exists. Seed the database first.");
        short? year = yearly ? (short)(date ?? DateOnly.FromDateTime(DateTime.Today)).Year : null;

        var conn = _db.Database.GetDbConnection();
        var openedHere = conn.State != ConnectionState.Open;
        if (openedHere) await conn.OpenAsync();
        try
        {
            for (var attempt = 0; attempt < 3; attempt++)
            {
                await using var cmd = conn.CreateCommand();
                cmd.Transaction = _db.Database.CurrentTransaction?.GetDbTransaction();
                cmd.CommandText = @"
UPDATE number_sequence WITH (UPDLOCK, HOLDLOCK)
   SET last_value = ISNULL(last_value, 0) + 1, updated_at = SYSDATETIMEOFFSET()
OUTPUT inserted.last_value, inserted.padding
 WHERE company_id = @c AND prefix = @p AND branch_id IS NULL AND is_deleted = 0
   AND ((@y IS NULL AND [year] IS NULL) OR [year] = @y);";
                Add(cmd, "@c", companyId);
                Add(cmd, "@p", prefix);
                Add(cmd, "@y", (object?)year ?? DBNull.Value);
                await using (var r = await cmd.ExecuteReaderAsync())
                {
                    if (await r.ReadAsync())
                    {
                        var value = r.GetInt32(0);
                        var pad = r.IsDBNull(1) ? padding : r.GetInt16(1);
                        return Format(prefix, year, value, pad);
                    }
                }

                try
                {
                    await using var ins = conn.CreateCommand();
                    ins.Transaction = cmd.Transaction;
                    ins.CommandText = @"INSERT INTO number_sequence (company_id, branch_id, prefix, [year], last_value, padding, created_at, is_deleted)
                                        VALUES (@c, NULL, @p, @y, 1, @pad, SYSDATETIMEOFFSET(), 0);";
                    Add(ins, "@c", companyId);
                    Add(ins, "@p", prefix);
                    Add(ins, "@y", (object?)year ?? DBNull.Value);
                    Add(ins, "@pad", padding);
                    await ins.ExecuteNonQueryAsync();
                    return Format(prefix, year, 1, padding);
                }
                catch (DbException) when (attempt < 2)
                {
                    // Another request created the row at the same time; loop and UPDATE it.
                }
            }
            throw new AppException($"Could not generate a number for prefix {prefix}.");
        }
        finally
        {
            if (openedHere) await conn.CloseAsync();
        }
    }

    private static string Format(string prefix, short? year, int value, short padding) =>
        year.HasValue ? $"{prefix}-{year}-{value.ToString().PadLeft(padding, '0')}"
                      : $"{prefix}-{value.ToString().PadLeft(padding, '0')}";

    private static void Add(DbCommand cmd, string name, object value)
    {
        var p = cmd.CreateParameter();
        p.ParameterName = name;
        p.Value = value;
        cmd.Parameters.Add(p);
    }
}
