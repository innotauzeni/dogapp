using Dog.Application.Common;
using Dog.Application.Contracts;
using Dog.Application.Features.DogTraining.Commands;
using Dog.Application.Features.DogTraining.DTOs;
using Dog.Application.Features.DogTraining.Queries;
using Dog.Application.Features.Documents.DTOs;
using Dog.Core.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Dog.Application.Features.DogTraining.Handlers
{
    internal static class TrainingAttachments
    {
        public static readonly string[] EntityTypes = { HealthMapper.DocumentEntityType, LogMapper.DocumentEntityType, "TrainingReport" };

        /// <summary>Finds the enrolment that owns the record, so the caller's access can be checked.</summary>
        public static async Task<DogTrainingEnrollment?> OwnerAsync(IUnitOfWork uow, string entityType, int entityId, CancellationToken ct)
        {
            int? enrollmentId = entityType switch
            {
                "TrainingHealth" => (await uow.Repository<DogHealthAssessment>().TableNoTracking.Where(a => a.Id == entityId).Select(a => (int?)a.EnrollmentId).FirstOrDefaultAsync(ct)),
                "TrainingLog" => (await uow.Repository<DogTrainingDailyLog>().TableNoTracking.Where(a => a.Id == entityId).Select(a => (int?)a.EnrollmentId).FirstOrDefaultAsync(ct)),
                "TrainingReport" => (await uow.Repository<DogTrainingProgressReport>().TableNoTracking.Where(a => a.Id == entityId).Select(a => (int?)a.EnrollmentId).FirstOrDefaultAsync(ct)),
                _ => null
            };
            return enrollmentId == null ? null
                : await uow.Repository<DogTrainingEnrollment>().TableNoTracking.FirstOrDefaultAsync(e => e.Id == enrollmentId.Value, ct);
        }
    }

    public class GetTrainingAttachmentsHandler : IRequestHandler<GetTrainingAttachmentsQuery, ApiResponse<List<DocumentAttachmentDto>>>
    {
        private readonly IUnitOfWork _uow;
        public GetTrainingAttachmentsHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<List<DocumentAttachmentDto>>> Handle(GetTrainingAttachmentsQuery request, CancellationToken ct)
        {
            if (!TrainingAttachments.EntityTypes.Contains(request.EntityType)) return ApiResponse<List<DocumentAttachmentDto>>.Fail("Unknown attachment type.");
            var owner = await TrainingAttachments.OwnerAsync(_uow, request.EntityType, request.EntityId, ct);
            if (owner == null || !TrainingSupport.CanAccess(request.Actor, owner)) return ApiResponse<List<DocumentAttachmentDto>>.Fail("You do not have access to this record.");

            var list = await _uow.Repository<DocumentAttachment>().TableNoTracking
                .Where(d => d.EntityType == request.EntityType && d.EntityId == request.EntityId).OrderBy(d => d.CreatedAt).ToListAsync(ct);
            return ApiResponse<List<DocumentAttachmentDto>>.Ok(list.Select(d => new DocumentAttachmentDto
            {
                Id = d.Id, EntityType = d.EntityType, EntityId = d.EntityId, FileName = d.FileName, FileUrl = d.FileUrl,
                ContentType = d.ContentType, Notes = d.Notes, CreatedAt = d.CreatedAt
            }).ToList());
        }
    }

    public class AddTrainingAttachmentHandler : IRequestHandler<AddTrainingAttachmentCommand, ApiResponse<bool>>
    {
        private readonly IUnitOfWork _uow;
        public AddTrainingAttachmentHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<bool>> Handle(AddTrainingAttachmentCommand request, CancellationToken ct)
        {
            if (!TrainingAttachments.EntityTypes.Contains(request.EntityType)) return ApiResponse<bool>.Fail("Unknown attachment type.");
            var owner = await TrainingAttachments.OwnerAsync(_uow, request.EntityType, request.EntityId, ct);
            if (owner == null || !TrainingSupport.CanAccess(request.Actor, owner)) return ApiResponse<bool>.Fail("You do not have access to this record.");

            await _uow.Repository<DocumentAttachment>().AddAsync(new DocumentAttachment
            {
                CompanyId = owner.CompanyId, EntityType = request.EntityType, EntityId = request.EntityId,
                FileName = request.FileName, FileUrl = request.FileUrl, ContentType = request.ContentType,
                Notes = request.Notes, UploadedByUserId = request.Actor.UserId,
            }, ct);
            return ApiResponse<bool>.Ok(true, "File attached.");
        }
    }

    public class DeleteTrainingAttachmentHandler : IRequestHandler<DeleteTrainingAttachmentCommand, ApiResponse<bool>>
    {
        private readonly IUnitOfWork _uow;
        public DeleteTrainingAttachmentHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<bool>> Handle(DeleteTrainingAttachmentCommand request, CancellationToken ct)
        {
            if (!TrainingAttachments.EntityTypes.Contains(request.EntityType)) return ApiResponse<bool>.Fail("Unknown attachment type.");
            var owner = await TrainingAttachments.OwnerAsync(_uow, request.EntityType, request.EntityId, ct);
            if (owner == null || !TrainingSupport.CanAccess(request.Actor, owner)) return ApiResponse<bool>.Fail("You do not have access to this record.");

            var doc = await _uow.Repository<DocumentAttachment>().Table.AsTracking()
                .FirstOrDefaultAsync(d => d.Id == request.AttachmentId && d.EntityType == request.EntityType && d.EntityId == request.EntityId, ct);
            if (doc == null) return ApiResponse<bool>.Fail("File not found.");
            await _uow.Repository<DocumentAttachment>().DeleteAsync(doc, ct);
            return ApiResponse<bool>.Ok(true, "File removed.");
        }
    }
}
