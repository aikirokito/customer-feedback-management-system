using CFMS.Application.Common.Models;
using CFMS.Application.DTOs.AuditLogs;

namespace CFMS.Application.Services.Interfaces;

public interface IAuditLogService
{
    Task<PagedResult<AuditLogDto>> GetAuditLogsAsync(AuditLogFilterRequest filter, CancellationToken ct = default);
    Task LogAsync(Guid? userId, Domain.Enums.AuditAction action, string entityType, Guid? entityId, string? oldValues = null, string? newValues = null, string? ipAddress = null, CancellationToken ct = default);
}
