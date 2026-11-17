using Dog.Application.Common;
using Dog.Application.Features.Documents.DTOs;
using MediatR;

namespace Dog.Application.Features.Documents.Commands
{
    public record CreateDocumentAttachmentCommand(CreateDocumentAttachmentDto Dto) : IRequest<ApiResponse<DocumentAttachmentDto>>;
    public record DeleteDocumentAttachmentCommand(int Id) : IRequest<ApiResponse<bool>>;
}
