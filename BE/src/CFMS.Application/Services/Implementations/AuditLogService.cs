using AutoMapper;
using CFMS.Application.Common.Models;
using CFMS.Application.DTOs.AuditLogs;
using CFMS.Application.Services.Interfaces;
using CFMS.Domain.Entities;
using CFMS.Application.Common.Interfaces;
using CFMS.Domain.Enums;

namespace CFMS.Application.Services.Implementations;

public class AuditLogService : IAuditLogService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public AuditLogService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<PagedResult<AuditLogDto>> GetAuditLogsAsync(AuditLogFilterRequest filter, CancellationToken ct = default)
    {
        var (items, totalCount) = await _unitOfWork.AuditLogs.GetPagedAsync(
            filter.Page,
            filter.PageSize,
            filter.UserId,
            filter.EntityType,
            filter.EntityId,
            filter.Action,
            filter.FromDate,
            filter.ToDate,
            ct);

        var dtos = _mapper.Map<IEnumerable<AuditLogDto>>(items);
        return PagedResult<AuditLogDto>.Create(dtos, filter.Page, filter.PageSize, totalCount);
    }

    public async Task LogAsync(
        Guid? userId,
        AuditAction action,
        string entityType,
        Guid? entityId,
        string? oldValues = null,
        string? newValues = null,
        string? ipAddress = null,
        CancellationToken ct = default)
    {
        var log = new AuditLog
        {
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            OldValues = oldValues,
            NewValues = newValues,
            IpAddress = ipAddress
        };

        await _unitOfWork.AuditLogs.AddAsync(log, ct);
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
