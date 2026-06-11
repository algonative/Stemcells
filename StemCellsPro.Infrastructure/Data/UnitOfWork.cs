using System.Data;

namespace StemCellsPro.Infrastructure.Data;

public class UnitOfWork : StemCellsPro.Application.Interfaces.IUnitOfWork
{
    private readonly DapperContext _context;
    private IDbConnection? _connection;
    private IDbTransaction? _transaction;

    public UnitOfWork(DapperContext context)
    {
        _context = context;
    }

    public void BeginTransaction()
    {
        _connection = _context.CreateConnection();
        _connection.Open();
        _transaction = _connection.BeginTransaction();
    }

    public void Commit()
    {
        try
        {
            _transaction?.Commit();
        }
        catch
        {
            _transaction?.Rollback();
            throw;
        }
        finally
        {
            _transaction?.Dispose();
            _connection?.Dispose();
        }
    }

    public void Rollback()
    {
        _transaction?.Rollback();
        _transaction?.Dispose();
        _connection?.Dispose();
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _connection?.Dispose();
    }
}
