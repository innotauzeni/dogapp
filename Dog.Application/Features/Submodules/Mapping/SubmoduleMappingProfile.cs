using AutoMapper;
using Dog.Application.Features.Submodules.DTOs;
using Dog.Core.Entities;

namespace Dog.Application.Features.Submodules.Mapping
{
    public class SubmoduleMappingProfile : Profile
    {
        public SubmoduleMappingProfile()
        {
            CreateMap<Submodule, SubmoduleDetailDto>();
            CreateMap<CreateSubmoduleDto, Submodule>();
            CreateMap<UpdateSubmoduleDto, Submodule>();
        }
    }
}
