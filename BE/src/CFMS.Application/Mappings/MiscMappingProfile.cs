using AutoMapper;
using CFMS.Application.DTOs.Assignments;
using CFMS.Application.DTOs.Comments;
using CFMS.Application.DTOs.Notifications;
using CFMS.Application.DTOs.AuditLogs;
using CFMS.Domain.Entities;

namespace CFMS.Application.Mappings;

public class MiscMappingProfile : Profile
{
    public MiscMappingProfile()
    {
        // Comments
        CreateMap<FeedbackComment, CommentDto>()
            .ForMember(dest => dest.AuthorName,
                opt => opt.MapFrom(src => $"{src.AuthorUser.FirstName} {src.AuthorUser.LastName}"));

        // Assignments
        CreateMap<FeedbackAssignment, AssignmentDto>()
            .ForMember(dest => dest.AssignedToUserName,
                opt => opt.MapFrom(src => $"{src.AssignedToUser.FirstName} {src.AssignedToUser.LastName}"))
            .ForMember(dest => dest.AssignedByUserName,
                opt => opt.MapFrom(src => $"{src.AssignedByUser.FirstName} {src.AssignedByUser.LastName}"))
            .ForMember(dest => dest.AssignedAtUtc, opt => opt.MapFrom(src => src.CreatedAtUtc));

        // Notifications
        CreateMap<Notification, NotificationDto>();

        // AuditLogs
        CreateMap<AuditLog, AuditLogDto>()
            .ForMember(dest => dest.UserFullName,
                opt => opt.MapFrom(src => src.User != null
                    ? $"{src.User.FirstName} {src.User.LastName}"
                    : null));
    }
}
