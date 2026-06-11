namespace StemCellsPro.Application.Interfaces;

public interface IAuditService
{
    Task LogActionAsync(string action, string entityName, string entityId, string details);
}
