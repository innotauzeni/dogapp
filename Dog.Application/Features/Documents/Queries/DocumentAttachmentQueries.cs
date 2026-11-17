using Dog.Application.Common;
using Dog.Application.Features.Documents.DTOs;
using MediatR;

namespace Dog.Application.Features.Documents.Queries
{
    public record GetDocumentsByEntityQuery(string EntityType, int EntityId) : IRequest<ApiResponse<List<DocumentAttachmentDto>>>;
}
