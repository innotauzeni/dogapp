using AutoMapper;
using MediatR;
using Dog.Application.Common;
using Dog.Application.Contracts;
using Dog.Application.Features.SystemModules.Commands;
using Dog.Application.Features.SystemModules.DTOs;
using Dog.Core.Entities;

namespace Dog.Application.Features.SystemModules.Handlers
{
    public class CreateSystemModuleHandler : IRequestHandler<CreateSystemModuleCommand, ApiResponse<SystemModuleDto>>
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;

        public CreateSystemModuleHandler(IUnitOfWork uow, IMapper mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }

        public async Task<ApiResponse<SystemModuleDto>> Handle(CreateSystemModuleCommand request, CancellationToken cancellationToken)
        {
            var entity = _mapper.Map<SystemModule>(request.Dto);
            await _uow.Repository<SystemModule>().AddAsync(entity, cancellationToken);
            return ApiResponse<SystemModuleDto>.Ok(_mapper.Map<SystemModuleDto>(entity), "SystemModule created successfully.");
        }
    }

    public class UpdateSystemModuleHandler : IRequestHandler<UpdateSystemModuleCommand, ApiResponse<SystemModuleDto>>
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;

        public UpdateSystemModuleHandler(IUnitOfWork uow, IMapper mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }

        public async Task<ApiResponse<SystemModuleDto>> Handle(UpdateSystemModuleCommand request, CancellationToken cancellationToken)
        {
            var repo = _uow.Repository<SystemModule>();
            var entity = await repo.GetByIdAsync(request.Dto.Id, cancellationToken);
            if (entity == null)
                return ApiResponse<SystemModuleDto>.Fail($"SystemModule with id {request.Dto.Id} not found.");

            _mapper.Map(request.Dto, entity);
            await repo.UpdateAsync(entity, cancellationToken);
            return ApiResponse<SystemModuleDto>.Ok(_mapper.Map<SystemModuleDto>(entity), "SystemModule updated successfully.");
        }
    }

    public class DeleteSystemModuleHandler : IRequestHandler<DeleteSystemModuleCommand, ApiResponse<bool>>
    {
        private readonly IUnitOfWork _uow;

        public DeleteSystemModuleHandler(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<ApiResponse<bool>> Handle(DeleteSystemModuleCommand request, CancellationToken cancellationToken)
        {
            var repo = _uow.Repository<SystemModule>();
            var entity = await repo.GetByIdAsync(request.Id, cancellationToken);
            if (entity == null)
                return ApiResponse<bool>.Fail($"SystemModule with id {request.Id} not found.");

            await repo.DeleteAsync(entity, cancellationToken);
            return ApiResponse<bool>.Ok(true, "SystemModule deleted successfully.");
        }
    }
}
