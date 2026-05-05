using AutoMapper;
using Dog.Application.Features.Permissions.DTOs;
using Dog.Core.Entities;

namespace Dog.Application.Features.Permissions.Mapping
{
    public class PermissionMappingProfile : Profile
    {
        public PermissionMappingProfile()
        {
            CreateMap<Permission, PermissionDto>();
            CreateMap<CreatePermissionDto, Permission>();
            CreateMap<UpdatePermissionDto, Permission>();
        }
    }
}
