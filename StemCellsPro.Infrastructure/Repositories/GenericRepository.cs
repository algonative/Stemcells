using Dapper;
using Dapper.Contrib.Extensions;
using StemCellsPro.Application.Interfaces;
using StemCellsPro.Infrastructure.Data;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace StemCellsPro.Infrastructure.Repositories;

public class GenericRepository<T> : IGenericRepository<T> where T : class
{
    private readonly DapperContext _context;

    public GenericRepository(DapperContext context)
    {
        _context = context;
    }

    private string GetTableName()
    {
        var type = typeof(T);
        var tableAttr = type.GetCustomAttribute<System.ComponentModel.DataAnnotations.Schema.TableAttribute>();
        return tableAttr != null ? tableAttr.Name : $"{type.Name}s";
    }

    public async Task<int> AddAsync(T entity)
    {
        using var connection = _context.CreateConnection();
        return await connection.InsertAsync(entity);
    }

    public async Task<int> DeleteAsync(int id)
    {
        using var connection = _context.CreateConnection();
        // Create a dummy entity with just the ID to delete it using Dapper.Contrib
        var entity = Activator.CreateInstance<T>();
        var property = typeof(T).GetProperty("Id");
        if (property != null)
        {
            property.SetValue(entity, id);
        }
        var success = await connection.DeleteAsync(entity);
        return success ? 1 : 0;
    }

    public async Task<IReadOnlyList<T>> GetAllAsync()
    {
        using var connection = _context.CreateConnection();
        var result = await connection.GetAllAsync<T>();
        return result.ToList();
    }

    public async Task<T?> GetByIdAsync(int id)
    {
        using var connection = _context.CreateConnection();
        return await connection.GetAsync<T>(id);
    }

    public async Task<int> UpdateAsync(T entity)
    {
        using var connection = _context.CreateConnection();
        var success = await connection.UpdateAsync(entity);
        return success ? 1 : 0;
    }
}
