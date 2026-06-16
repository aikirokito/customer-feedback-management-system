using AutoMapper;
using CFMS.Application.DTOs.Feedback;
using CFMS.Domain.Entities;

namespace CFMS.Application.Mappings;

public class FeedbackMappingProfile : Profile
{
    public FeedbackMappingProfile()
    {
        CreateMap<Feedback, FeedbackListItemDto>()
            .ForMember(dest => dest.SubmittedByUserName,
                opt => opt.MapFrom(src => $"{src.SubmittedByUser.FirstName} {src.SubmittedByUser.LastName}"))
            .ForMember(dest => dest.AssignedToUserName,
                opt => opt.MapFrom(src => src.AssignedToUser != null
                    ? $"{src.AssignedToUser.FirstName} {src.AssignedToUser.LastName}"
                    : null));

        CreateMap<Feedback, FeedbackDetailDto>()
            .IncludeBase<Feedback, FeedbackListItemDto>();

        CreateMap<FeedbackAttachment, FeedbackAttachmentDto>()
            .ForMember(dest => dest.UploadedAtUtc, opt => opt.MapFrom(src => src.CreatedAtUtc))
            .ForMember(dest => dest.PublicUrl, opt => opt.Ignore()); // resolved by service

        CreateMap<FeedbackResponse, FeedbackResponseDto>()
            .ForMember(dest => dest.RespondedByUserName,
                opt => opt.MapFrom(src => $"{src.RespondedByUser.FirstName} {src.RespondedByUser.LastName}"));

        CreateMap<FeedbackStatusHistory, FeedbackStatusHistoryDto>()
            .ForMember(dest => dest.ChangedAtUtc, opt => opt.MapFrom(src => src.CreatedAtUtc))
            .ForMember(dest => dest.ChangedByUserName,
                opt => opt.MapFrom(src => $"{src.ChangedByUser.FirstName} {src.ChangedByUser.LastName}"));
    }
}
