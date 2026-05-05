using AutoMapper;
using Dog.Application.Features.SystemModules.DTOs;
using Dog.Core.Entities;

namespace Dog.Application.Features.SystemModules.Mapping
{
    public class SystemModuleMappingProfile : Profile
    {
        public SystemModuleMappingProfile()
        {
            CreateMap<SystemModule, SystemModuleDto>()
                .ForMember(d => d.Children, o => o.MapFrom(s => s.childrens));

            CreateMap<Submodule, SubmoduleDto>();

            CreateMap<CreateSystemModuleDto, SystemModule>();
            CreateMap<UpdateSystemModuleDto, SystemModule>();
        }
    }
}
