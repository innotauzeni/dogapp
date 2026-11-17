using Dog.Application.Common;
using Dog.Application.Contracts;
using Dog.Application.Features.DogCertifications.Commands;
using Dog.Application.Features.DogCertifications.DTOs;
using Dog.Application.Features.DogCertifications.Queries;
using Dog.Core.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Dog.Application.Features.DogCertifications.Handlers
{
    public class GetCertificationsByDogHandler : IRequestHandler<GetCertificationsByDogQuery, ApiResponse<List<DogCertificationDto>>>
    {
        private readonly IUnitOfWork _uow;
        public GetCertificationsByDogHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<List<DogCertificationDto>>> Handle(GetCertificationsByDogQuery request, CancellationToken ct)
        {
            var list = await _uow.Repository<DogCertification>()
                .TableNoTracking
                .Include(c => c.Dog)
                .Where(c => c.DogId == request.DogId)
                .OrderByDescending(c => c.IssueDate)
                .ToListAsync(ct);

            return ApiResponse<List<DogCertificationDto>>.Ok(list.Select(DogCertificationMapper.Map).ToList());
        }
    }

    public class GetDogCertificationByIdHandler : IRequestHandler<GetDogCertificationByIdQuery, ApiResponse<DogCertificationDto>>
    {
        private readonly IUnitOfWork _uow;
        public GetDogCertificationByIdHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<DogCertificationDto>> Handle(GetDogCertificationByIdQuery request, CancellationToken ct)
        {
            var cert = await _uow.Repository<DogCertification>()
                .TableNoTracking
                .Include(c => c.Dog)
                .FirstOrDefaultAsync(c => c.Id == request.Id, ct);

            return cert == null
                ? ApiResponse<DogCertificationDto>.Fail("Certification not found.")
                : ApiResponse<DogCertificationDto>.Ok(DogCertificationMapper.Map(cert));
        }
    }

    public class CreateDogCertificationHandler : IRequestHandler<CreateDogCertificationCommand, ApiResponse<DogCertificationDto>>
    {
        private readonly IUnitOfWork _uow;
        public CreateDogCertificationHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<DogCertificationDto>> Handle(CreateDogCertificationCommand request, CancellationToken ct)
        {
            var d = request.Dto;
            var dogExists = await _uow.Repository<Dogtbl>().TableNoTracking.AnyAsync(dog => dog.Id == d.DogId, ct);
            if (!dogExists) return ApiResponse<DogCertificationDto>.Fail("Dog not found.");

            var entity = new DogCertification
            {
                DogId = d.DogId,
                CompanyId = d.CompanyId,
                CertificationName = d.CertificationName,
                IssuingBody = d.IssuingBody,
                IssueDate = d.IssueDate,
                ExpiryDate = d.ExpiryDate,
                TrainerName = d.TrainerName,
                Notes = d.Notes,
                CertificateFileUrl = d.CertificateFileUrl,
            };
            await _uow.Repository<DogCertification>().AddAsync(entity, ct);
            return ApiResponse<DogCertificationDto>.Ok(DogCertificationMapper.Map(entity), "Certification recorded.");
        }
    }

    public class UpdateDogCertificationHandler : IRequestHandler<UpdateDogCertificationCommand, ApiResponse<DogCertificationDto>>
    {
        private readonly IUnitOfWork _uow;
        public UpdateDogCertificationHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<DogCertificationDto>> Handle(UpdateDogCertificationCommand request, CancellationToken ct)
        {
            var repo = _uow.Repository<DogCertification>();
            var entity = await repo.GetByIdAsync(request.Id, ct);
            if (entity == null) return ApiResponse<DogCertificationDto>.Fail("Certification not found.");

            var d = request.Dto;
            entity.CertificationName = d.CertificationName;
            entity.IssuingBody = d.IssuingBody;
            entity.IssueDate = d.IssueDate;
            entity.ExpiryDate = d.ExpiryDate;
            entity.TrainerName = d.TrainerName;
            entity.Notes = d.Notes;
            entity.CertificateFileUrl = d.CertificateFileUrl;
            await repo.UpdateAsync(entity, ct);
            return ApiResponse<DogCertificationDto>.Ok(DogCertificationMapper.Map(entity), "Certification updated.");
        }
    }

    public class DeleteDogCertificationHandler : IRequestHandler<DeleteDogCertificationCommand, ApiResponse<bool>>
    {
        private readonly IUnitOfWork _uow;
        public DeleteDogCertificationHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<bool>> Handle(DeleteDogCertificationCommand request, CancellationToken ct)
        {
            var repo = _uow.Repository<DogCertification>();
            var entity = await repo.GetByIdAsync(request.Id, ct);
            if (entity == null) return ApiResponse<bool>.Fail("Certification not found.");
            await repo.DeleteAsync(entity, ct);
            return ApiResponse<bool>.Ok(true, "Certification deleted.");
        }
    }

    internal static class DogCertificationMapper
    {
        public static DogCertificationDto Map(DogCertification c) => new()
        {
            Id = c.Id,
            DogId = c.DogId,
            DogName = c.Dog?.Name,
            CertificationName = c.CertificationName,
            IssuingBody = c.IssuingBody,
            IssueDate = c.IssueDate,
            ExpiryDate = c.ExpiryDate,
            TrainerName = c.TrainerName,
            Notes = c.Notes,
            CertificateFileUrl = c.CertificateFileUrl,
        };
    }
}
