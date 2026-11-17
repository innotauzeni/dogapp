using Dog.Application.Authorization;
using Dog.Application.Common;
using Dog.Application.Contracts;
using Dog.Application.Features.Documents.Commands;
using Dog.Application.Features.Documents.DTOs;
using Dog.Application.Features.Documents.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Dog.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DocumentController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IFileStorageService _storage;
        public DocumentController(IMediator mediator, IFileStorageService storage)
        {
            _mediator = mediator;
            _storage = storage;
        }

        private static readonly string[] AllowedTypes =
        {
            "image/jpeg", "image/png", "image/gif", "image/webp", "application/pdf",
            "application/msword", "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        };
        private const long MaxBytes = 10 * 1024 * 1024; // 10 MB

        [HttpGet]
        [HasPermission("document.view")]
        public async Task<IActionResult> GetByEntity(string entityType, int entityId, CancellationToken ct)
            => Ok(await _mediator.Send(new GetDocumentsByEntityQuery(entityType, entityId), ct));

        /// <summary>Uploads a file to object storage and records it against an entity, in one call.</summary>
        [HttpPost("Upload")]
        [HasPermission("document.create")]
        public async Task<IActionResult> Upload(IFormFile file, [FromForm] int companyId, [FromForm] string entityType,
            [FromForm] int entityId, [FromForm] string? notes, CancellationToken ct)
        {
            if (!_storage.IsEnabled)
                return Ok(ApiResponse<DocumentAttachmentDto>.Fail("File storage is not configured on the server."));
            if (file == null || file.Length == 0)
                return Ok(ApiResponse<DocumentAttachmentDto>.Fail("No file uploaded."));
            if (file.Length > MaxBytes)
                return Ok(ApiResponse<DocumentAttachmentDto>.Fail("File must be 10 MB or smaller."));
            if (!AllowedTypes.Contains(file.ContentType))
                return Ok(ApiResponse<DocumentAttachmentDto>.Fail("Only images, PDF and Word documents are allowed."));

            await using var stream = file.OpenReadStream();
            var url = await _storage.UploadAsync(stream, file.FileName, file.ContentType, "documents", ct);

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var resp = await _mediator.Send(new CreateDocumentAttachmentCommand(new CreateDocumentAttachmentDto
            {
                CompanyId = companyId,
                EntityType = entityType,
                EntityId = entityId,
                FileName = file.FileName,
                FileUrl = url,
                ContentType = file.ContentType,
                Notes = notes,
                UploadedByUserId = userId,
            }), ct);
            return Ok(resp);
        }

        /// <summary>Records a document that was already uploaded elsewhere (e.g. the Portal,
        /// which has its own direct access to object storage) — metadata only, no file bytes.</summary>
        [HttpPost("Create")]
        [HasPermission("document.create")]
        public async Task<IActionResult> Create([FromBody] CreateDocumentAttachmentDto dto, CancellationToken ct)
            => Ok(await _mediator.Send(new CreateDocumentAttachmentCommand(dto), ct));

        [HttpDelete("{id:int}")]
        [HasPermission("document.delete")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
            => Ok(await _mediator.Send(new DeleteDocumentAttachmentCommand(id), ct));
    }
}
