using MedCabinet.Domain.Entities;
using MedCabinet.Domain.Interfaces;
using MedCabinet.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace MedCabinet.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private IDbContextTransaction? _transaction;

    public IRepository<User> Users { get; }
    public IRepository<Household> Households { get; }
    public IRepository<HouseholdMember> HouseholdMembers { get; }
    public IRepository<Medicine> Medicines { get; }
    public IRepository<MedUsage> MedUsages { get; }
    public IRepository<MedAlert> MedAlerts { get; }
    public IRepository<ProcurementSuggestion> ProcurementSuggestions { get; }

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
        Users = new Repository<User>(context);
        Households = new Repository<Household>(context);
        HouseholdMembers = new Repository<HouseholdMember>(context);
        Medicines = new Repository<Medicine>(context);
        MedUsages = new Repository<MedUsage>(context);
        MedAlerts = new Repository<MedAlert>(context);
        ProcurementSuggestions = new Repository<ProcurementSuggestion>(context);
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public async Task BeginTransactionAsync()
    {
        _transaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        if (_transaction == null)
        {
            throw new InvalidOperationException("No transaction to commit.");
        }
        await _transaction.CommitAsync();
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction == null)
        {
            throw new InvalidOperationException("No transaction to rollback.");
        }
        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}
