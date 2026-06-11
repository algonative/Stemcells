namespace StemCellsPro.Application.Interfaces;

public interface IUnitOfWork : IDisposable
{
    // Expose repositories here if needed, or use them directly via DI
    void BeginTransaction();
    void Commit();
    void Rollback();
}
