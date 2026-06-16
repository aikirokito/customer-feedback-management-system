using CFMS.Application.DTOs.AuditLogs;
using CFMS.Application.Services.Interfaces;
using CFMS.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CFMS.Api.Controllers;

/// <summary>
/// Audit log query API. Admin only.
/// </summary>
[Authorize(Roles = RoleNames.SystemAdmin)]
[Tags("Audit Logs")]
public class AuditLogsController : BaseController
{
    private readonly IAuditLogService _auditLogService;

    public AuditLogsController(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    [HttpGet]
    [Route("~/api/admin/audit-logs")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> GetAuditLogs([FromQuery] AuditLogFilterRequest filter, CancellationToken ct)
    {
        var result = await _auditLogService.GetAuditLogsAsync(filter, ct);
        return OkResponse(result);
    }
}
