using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Dog.Application.Common;
using Dog.Application.Contracts;
using Dog.Application.Features.Submodules.Commands;
using Dog.Application.Features.Submodules.DTOs;
using Dog.Application.Features.Submodules.Queries;
using Dog.Core.Entities;

namespace Dog.Application.Features.Submodules.Handlers
{
    public class GetSubmodulesByModuleHandler : IRequestHandler<GetSubmodulesByModuleQuery, ApiResponse<List<SubmoduleDetailDto>>>
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;

        public GetSubmodulesByModuleHandler(IUnitOfWork uow, IMapper mapper) { _uow = uow; _mapper = mapper; }

        public async Task<ApiResponse<List<SubmoduleDetailDto>>> Handle(GetSubmodulesByModuleQuery request, CancellationToken cancellationToken)
        {
            var items = await _uow.Repository<Submodule>()
                .TableNoTracking
                .Where(s => s.ModuleId == request.ModuleId)
                .OrderBy(s => s.Order)
                .ToListAsync(cancellationToken);

            return ApiResponse<List<SubmoduleDetailDto>>.Ok(_mapper.Map<List<SubmoduleDetailDto>>(items));
        }
    }

    public class GetSubmoduleByIdHandler : IRequestHandler<GetSubmoduleByIdQuery, ApiResponse<SubmoduleDetailDto>>
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;

        public GetSubmoduleByIdHandler(IUnitOfWork uow, IMapper mapper) { _uow = uow; _mapper = mapper; }

        public async Task<ApiResponse<SubmoduleDetailDto>> Handle(GetSubmoduleByIdQuery request, CancellationToken cancellationToken)
        {
            var entity = await _uow.Repository<Submodule>().GetByIdAsync(request.Id, cancellationToken);
            if (entity == null)
                return ApiResponse<SubmoduleDetailDto>.Fail($"Submodule with id {request.Id} not found.");

            return ApiResponse<SubmoduleDetailDto>.Ok(_mapper.Map<SubmoduleDetailDto>(entity));
        }
    }

    public class CreateSubmoduleHandler : IRequestHandler<CreateSubmoduleCommand, ApiResponse<SubmoduleDetailDto>>
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;

        public CreateSubmoduleHandler(IUnitOfWork uow, IMapper mapper) { _uow = uow; _mapper = mapper; }

        public async Task<ApiResponse<SubmoduleDetailDto>> Handle(CreateSubmoduleCommand request, CancellationToken cancellationToken)
        {
            var entity = _mapper.Map<Submodule>(request.Dto);
            await _uow.Repository<Submodule>().AddAsync(entity, cancellationToken);
            return ApiResponse<SubmoduleDetailDto>.Ok(_mapper.Map<SubmoduleDetailDto>(entity), "Submodule created successfully.");
        }
    }

    public class UpdateSubmoduleHandler : IRequestHandler<UpdateSubmoduleCommand, ApiResponse<SubmoduleDetailDto>>
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;

        public UpdateSubmoduleHandler(IUnitOfWork uow, IMapper mapper) { _uow = uow; _mapper = mapper; }

        public async Task<ApiResponse<SubmoduleDetailDto>> Handle(UpdateSubmoduleCommand request, CancellationToken cancellationToken)
        {
            var repo = _uow.Repository<Submodule>();
            var entity = await repo.GetByIdAsync(request.Dto.Id, cancellationToken);
            if (entity == null)
                return ApiResponse<SubmoduleDetailDto>.Fail($"Submodule with id {request.Dto.Id} not found.");

            _mapper.Map(request.Dto, entity);
            await repo.UpdateAsync(entity, cancellationToken);
            return ApiResponse<SubmoduleDetailDto>.Ok(_mapper.Map<SubmoduleDetailDto>(entity), "Submodule updated successfully.");
        }
    }

    public class DeleteSubmoduleHandler : IRequestHandler<DeleteSubmoduleCommand, ApiResponse<bool>>
    {
        private readonly IUnitOfWork _uow;

        public DeleteSubmoduleHandler(IUnitOfWork uow) { _uow = uow; }

        public async Task<ApiResponse<bool>> Handle(DeleteSubmoduleCommand request, CancellationToken cancellationToken)
        {
            var repo = _uow.Repository<Submodule>();
            var entity = await repo.GetByIdAsync(request.Id, cancellationToken);
            if (entity == null)
                return ApiResponse<bool>.Fail($"Submodule with id {request.Id} not found.");

            await repo.DeleteAsync(entity, cancellationToken);
            return ApiResponse<bool>.Ok(true, "Submodule deleted successfully.");
        }
    }
}
