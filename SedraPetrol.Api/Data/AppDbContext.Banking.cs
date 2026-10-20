using Microsoft.EntityFrameworkCore;
using SedraPetrol.Api.Domain.Entities;

namespace SedraPetrol.Api.Data;

/// <summary>Cash &amp; bank tables (BRD §15): accounts, transfers, statement lines, reconciliations.</summary>
public partial class AppDbContext
{
    public DbSet<BankAccount> BankAccounts => Set<BankAccount>();
    public DbSet<BankTransfer> BankTransfers => Set<BankTransfer>();
    public DbSet<BankStatementLine> BankStatementLines => Set<BankStatementLine>();
    public DbSet<BankReconciliation> BankReconciliations => Set<BankReconciliation>();
}
