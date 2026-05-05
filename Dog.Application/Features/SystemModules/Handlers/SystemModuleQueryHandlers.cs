using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Dog.Application.Common;
using Dog.Application.Contracts;
using Dog.Application.Features.SystemModules.DTOs;
using Dog.Application.Features.SystemModules.Queries;
using Dog.Core.Entities;

namespace Dog.Application.Features.SystemModules.Handlers
{
    public class GetAllSystemModulesHandler : IRequestHandler<GetAllSystemModulesQuery, ApiResponse<List<SystemModuleDto>>>
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;

        public GetAllSystemModulesHandler(IUnitOfWork uow, IMapper mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }

        public async Task<ApiResponse<List<SystemModuleDto>>> Handle(GetAllSystemModulesQuery request, CancellationToken cancellationToken)
        {
            var modules = await _uow.Repository<SystemModule>()
                .TableNoTracking
                .Include(m => m.childrens)
                .OrderBy(m => m.SortOrder)
                .ToListAsync(cancellationToken);

            return ApiResponse<List<SystemModuleDto>>.Ok(_mapper.Map<List<SystemModuleDto>>(modules));
        }
    }

    public class GetSystemModuleByIdHandler : IRequestHandler<GetSystemModuleByIdQuery, ApiResponse<SystemModuleDto>>
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;

        public GetSystemModuleByIdHandler(IUnitOfWork uow, IMapper mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }

        public async Task<ApiResponse<SystemModuleDto>> Handle(GetSystemModuleByIdQuery request, CancellationToken cancellationToken)
        {
            var module = await _uow.Repository<SystemModule>()
                .TableNoTracking
                .Include(m => m.childrens)
                .FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken);

            if (module == null)
                return ApiResponse<SystemModuleDto>.Fail($"SystemModule with id {request.Id} not found.");

            return ApiResponse<SystemModuleDto>.Ok(_mapper.Map<SystemModuleDto>(module));
        }
    }
}
