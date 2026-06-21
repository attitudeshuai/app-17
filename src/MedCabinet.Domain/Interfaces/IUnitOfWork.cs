using MedCabinet.Domain.Entities;

namespace MedCabinet.Domain.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IRepository<User> Users { get; }
    IRepository<Household> Households { get; }
    IRepository<HouseholdMember> HouseholdMembers { get; }
    IRepository<Medicine> Medicines { get; }
    IRepository<MedUsage> MedUsages { get; }
    IRepository<MedAlert> MedAlerts { get; }
    IRepository<ProcurementSuggestion> ProcurementSuggestions { get; }

    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}
