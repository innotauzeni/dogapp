using Dog.Application.Common;
using Dog.Application.Contracts;
using Dog.Application.Features.Documents.Commands;
using Dog.Application.Features.Documents.DTOs;
using Dog.Application.Features.Documents.Queries;
using Dog.Core.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Dog.Application.Features.Documents.Handlers
{
    public class GetDocumentsByEntityHandler : IRequestHandler<GetDocumentsByEntityQuery, ApiResponse<List<DocumentAttachmentDto>>>
    {
        private readonly IUnitOfWork _uow;
        public GetDocumentsByEntityHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<List<DocumentAttachmentDto>>> Handle(GetDocumentsByEntityQuery request, CancellationToken ct)
        {
            var list = await _uow.Repository<DocumentAttachment>()
                .TableNoTracking
                .Where(d => d.EntityType == request.EntityType && d.EntityId == request.EntityId)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync(ct);

            return ApiResponse<List<DocumentAttachmentDto>>.Ok(list.Select(DocumentAttachmentMapper.Map).ToList());
        }
    }

    public class CreateDocumentAttachmentHandler : IRequestHandler<CreateDocumentAttachmentCommand, ApiResponse<DocumentAttachmentDto>>
    {
        private readonly IUnitOfWork _uow;
        public CreateDocumentAttachmentHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<DocumentAttachmentDto>> Handle(CreateDocumentAttachmentCommand request, CancellationToken ct)
        {
            var d = request.Dto;
            var entity = new DocumentAttachment
            {
                CompanyId = d.CompanyId,
                EntityType = d.EntityType,
                EntityId = d.EntityId,
                FileName = d.FileName,
                FileUrl = d.FileUrl,
                ContentType = d.ContentType,
                Notes = d.Notes,
                UploadedByUserId = d.UploadedByUserId,
            };
            await _uow.Repository<DocumentAttachment>().AddAsync(entity, ct);
            return ApiResponse<DocumentAttachmentDto>.Ok(DocumentAttachmentMapper.Map(entity), "Document attached.");
        }
    }

    public class DeleteDocumentAttachmentHandler : IRequestHandler<DeleteDocumentAttachmentCommand, ApiResponse<bool>>
    {
        private readonly IUnitOfWork _uow;
        public DeleteDocumentAttachmentHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<bool>> Handle(DeleteDocumentAttachmentCommand request, CancellationToken ct)
        {
            var repo = _uow.Repository<DocumentAttachment>();
            var entity = await repo.GetByIdAsync(request.Id, ct);
            if (entity == null) return ApiResponse<bool>.Fail("Document not found.");
            await repo.DeleteAsync(entity, ct);
            return ApiResponse<bool>.Ok(true, "Document deleted.");
        }
    }

    internal static class DocumentAttachmentMapper
    {
        public static DocumentAttachmentDto Map(DocumentAttachment d) => new()
        {
            Id = d.Id,
            EntityType = d.EntityType,
            EntityId = d.EntityId,
            FileName = d.FileName,
            FileUrl = d.FileUrl,
            ContentType = d.ContentType,
            Notes = d.Notes,
            CreatedAt = d.CreatedAt,
        };
    }
}
