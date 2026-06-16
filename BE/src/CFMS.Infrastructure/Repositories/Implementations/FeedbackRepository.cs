using CFMS.Domain.Entities;
using CFMS.Domain.Enums;
using CFMS.Infrastructure.Persistence;
using CFMS.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CFMS.Infrastructure.Repositories.Implementations;

public class FeedbackRepository : Repository<Feedback>, IFeedbackRepository
{
    public FeedbackRepository(AppDbContext context) : base(context) { }

    public async Task<(IEnumerable<Feedback> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        FeedbackStatus? status = null,
        FeedbackCategory? category = null,
        FeedbackPriority? priority = null,
        Guid? submittedByUserId = null,
        Guid? assignedToUserId = null,
        string? searchTerm = null,
        CancellationToken ct = default)
    {
        var query = _dbSet.AsQueryable();

        if (status.HasValue) query = query.Where(f => f.Status == status.Value);
        if (category.HasValue) query = query.Where(f => f.Category == category.Value);
        if (priority.HasValue) query = query.Where(f => f.Priority == priority.Value);
        if (submittedByUserId.HasValue) query = query.Where(f => f.SubmittedByUserId == submittedByUserId.Value);
        if (assignedToUserId.HasValue) query = query.Where(f => f.AssignedToUserId == assignedToUserId.Value);
        if (!string.IsNullOrWhiteSpace(searchTerm))
            query = query.Where(f => f.Title.Contains(searchTerm) || f.Description.Contains(searchTerm));

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(f => f.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(f => f.SubmittedByUser)
            .Include(f => f.AssignedToUser)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<Feedback?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default)
        => await _dbSet
            .Include(f => f.SubmittedByUser)
            .Include(f => f.AssignedToUser)
            .Include(f => f.Attachments)
            .Include(f => f.Responses).ThenInclude(r => r.RespondedByUser)
            .Include(f => f.Comments).ThenInclude(c => c.AuthorUser)
            .Include(f => f.StatusHistory).ThenInclude(sh => sh.ChangedByUser)
            .Include(f => f.AssignmentHistory).ThenInclude(a => a.AssignedToUser)
            .Include(f => f.AssignmentHistory).ThenInclude(a => a.AssignedByUser)
            .FirstOrDefaultAsync(f => f.Id == id, ct);


    public async Task<FeedbackAttachment?> GetAttachmentByIdAsync(Guid attachmentId, CancellationToken ct = default)
        => await _context.Set<FeedbackAttachment>()
            .Include(a => a.Feedback)
            .FirstOrDefaultAsync(a => a.Id == attachmentId, ct);

    public async Task<FeedbackResponse?> GetResponseByIdAsync(Guid responseId, CancellationToken ct = default)
        => await _context.Set<FeedbackResponse>()
            .Include(r => r.Feedback)
            .Include(r => r.RespondedByUser)
            .FirstOrDefaultAsync(r => r.Id == responseId, ct);

    public async Task<FeedbackComment?> GetCommentByIdAsync(Guid commentId, CancellationToken ct = default)
        => await _context.Set<FeedbackComment>()
            .Include(c => c.Feedback)
            .Include(c => c.AuthorUser)
            .Include(c => c.Replies)
                .ThenInclude(r => r.AuthorUser)
            .FirstOrDefaultAsync(c => c.Id == commentId, ct);

    public void RemoveAttachment(FeedbackAttachment attachment)
        => _context.Set<FeedbackAttachment>().Remove(attachment);

    public async Task<IEnumerable<Feedback>> GetReportFeedbacksAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        FeedbackCategory? category = null,
        FeedbackStatus? status = null,
        Guid? assignedToUserId = null,
        CancellationToken ct = default)
    {
        var query = _dbSet.AsQueryable();

        if (fromDate.HasValue) query = query.Where(f => f.CreatedAtUtc >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(f => f.CreatedAtUtc <= toDate.Value);
        if (category.HasValue) query = query.Where(f => f.Category == category.Value);
        if (status.HasValue) query = query.Where(f => f.Status == status.Value);
        if (assignedToUserId.HasValue) query = query.Where(f => f.AssignedToUserId == assignedToUserId.Value);

        return await query
            .Include(f => f.AssignedToUser)
            .ToListAsync(ct);
    }
}
