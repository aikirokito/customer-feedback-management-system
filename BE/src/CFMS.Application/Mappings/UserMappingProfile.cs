using AutoMapper;
using CFMS.Application.DTOs.Auth;
using CFMS.Application.DTOs.Users;
using CFMS.Domain.Entities;
using CFMS.Domain.Enums;

namespace CFMS.Application.Mappings;

public class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        // User → UserDto (used in AuthResponse)
        CreateMap<User, UserDto>();

        // User → UserListItemDto / UserDetailDto (used in admin endpoints, Sprint 2)
        CreateMap<User, UserListItemDto>()
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.Status == UserStatus.Active))
            .ForMember(dest => dest.DepartmentName,
                opt => opt.MapFrom(src => src.Department != null ? src.Department.Name : null));

        CreateMap<User, UserDetailDto>()
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.Status == UserStatus.Active))
            .ForMember(dest => dest.DepartmentName,
                opt => opt.MapFrom(src => src.Department != null ? src.Department.Name : null));

        // UpdateUserRequest → User (only profile fields; email and role are ignored)
        CreateMap<UpdateUserRequest, User>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Email, opt => opt.Ignore())
            .ForMember(dest => dest.Role, opt => opt.Ignore())
            .ForMember(dest => dest.Status, opt => opt.Ignore())
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore());
    }
}
