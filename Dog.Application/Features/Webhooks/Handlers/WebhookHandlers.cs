using MediatR;
using Microsoft.EntityFrameworkCore;
using Dog.Application.Common;
using Dog.Application.Contracts;
using Dog.Application.Features.Webhooks.Commands;
using Dog.Application.Features.Webhooks.DTOs;
using Dog.Application.Features.Webhooks.Queries;
using Dog.Core.Entities;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Dog.Application.Features.Webhooks.Handlers
{
    public class GetWebhooksByCompanyHandler : IRequestHandler<GetWebhooksByCompanyQuery, ApiResponse<List<WebhookDto>>>
    {
        private readonly IUnitOfWork _uow;
        public GetWebhooksByCompanyHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<List<WebhookDto>>> Handle(GetWebhooksByCompanyQuery request, CancellationToken ct)
        {
            // Fetch webhooks without Include to avoid EF alias conflict with audit columns
            var webhooks = await _uow.Repository<Webhook>()
                .TableNoTracking
                .Where(w => w.CompanyId == request.CompanyId)
                .OrderBy(w => w.Name)
                .ToListAsync(ct);

            // Fetch log counts separately to avoid the JOIN alias issue
            var webhookIds = webhooks.Select(w => w.Id).ToList();
            var logStats = await _uow.Repository<WebhookLog>()
                .TableNoTracking
                .Where(l => webhookIds.Contains(l.WebhookId))
                .GroupBy(l => l.WebhookId)
                .Select(g => new
                {
                    WebhookId = g.Key,
                    Total = g.Count(),
                    Successful = g.Count(l => l.Success)
                })
                .ToListAsync(ct);

            var result = webhooks.Select(w =>
            {
                var stats = logStats.FirstOrDefault(s => s.WebhookId == w.Id);
                var dto = WebhookMapper.Map(w);
                dto.TotalDeliveries = stats?.Total ?? 0;
                dto.SuccessfulDeliveries = stats?.Successful ?? 0;
                return dto;
            }).ToList();

            return ApiResponse<List<WebhookDto>>.Ok(result);
        }
    }

    public class GetWebhookByIdHandler : IRequestHandler<GetWebhookByIdQuery, ApiResponse<WebhookDto>>
    {
        private readonly IUnitOfWork _uow;
        public GetWebhookByIdHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<WebhookDto>> Handle(GetWebhookByIdQuery request, CancellationToken ct)
        {
            var w = await _uow.Repository<Webhook>().TableNoTracking
                .FirstOrDefaultAsync(x => x.Id == request.Id, ct);
            if (w == null) return ApiResponse<WebhookDto>.Fail("Webhook not found.");

            var stats = await _uow.Repository<WebhookLog>().TableNoTracking
                .Where(l => l.WebhookId == w.Id)
                .GroupBy(l => l.WebhookId)
                .Select(g => new { Total = g.Count(), Successful = g.Count(l => l.Success) })
                .FirstOrDefaultAsync(ct);

            var dto = WebhookMapper.Map(w);
            dto.TotalDeliveries = stats?.Total ?? 0;
            dto.SuccessfulDeliveries = stats?.Successful ?? 0;
            return ApiResponse<WebhookDto>.Ok(dto);
        }
    }

    public class GetWebhookLogsHandler : IRequestHandler<GetWebhookLogsQuery, ApiResponse<WebhookLogPageDto>>
    {
        private readonly IUnitOfWork _uow;
        public GetWebhookLogsHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<WebhookLogPageDto>> Handle(GetWebhookLogsQuery request, CancellationToken ct)
        {
            var query = _uow.Repository<WebhookLog>().TableNoTracking
                .Include(l => l.Webhook);

            var filtered = request.WebhookId.HasValue
                ? query.Where(l => l.WebhookId == request.WebhookId.Value)
                : query;

            var total = await filtered.CountAsync(ct);
            var data  = await filtered
                .OrderByDescending(l => l.CreatedAt)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(l => new WebhookLogDto
                {
                    Id            = l.Id,
                    WebhookId     = l.WebhookId,
                    WebhookName   = l.Webhook!.Name,
                    Event         = l.Event,
                    Payload       = l.Payload,
                    StatusCode    = l.StatusCode,
                    ResponseBody  = l.ResponseBody,
                    Success       = l.Success,
                    AttemptNumber = l.AttemptNumber,
                    ErrorMessage  = l.ErrorMessage,
                    DurationMs    = l.DurationMs,
                    NextRetryAt   = l.NextRetryAt,
                    CreatedAt     = l.CreatedAt,
                })
                .ToListAsync(ct);

            return ApiResponse<WebhookLogPageDto>.Ok(new WebhookLogPageDto
            {
                Total = total, Page = request.Page, PageSize = request.PageSize, Data = data
            });
        }
    }

    public class CreateWebhookHandler : IRequestHandler<CreateWebhookCommand, ApiResponse<WebhookDto>>
    {
        private readonly IUnitOfWork _uow;
        public CreateWebhookHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<WebhookDto>> Handle(CreateWebhookCommand request, CancellationToken ct)
        {
            var d = request.Dto;
            var entity = new Webhook
            {
                Name        = d.Name,
                Url         = d.Url,
                Secret      = d.Secret,
                Events      = string.Join(",", d.Events),
                IsActive    = d.IsActive,
                Description = d.Description,
                RetryCount  = d.RetryCount,
                CompanyId   = d.CompanyId,
            };
            await _uow.Repository<Webhook>().AddAsync(entity, ct);
            return ApiResponse<WebhookDto>.Ok(WebhookMapper.Map(entity), "Webhook created.");
        }
    }

    public class UpdateWebhookHandler : IRequestHandler<UpdateWebhookCommand, ApiResponse<WebhookDto>>
    {
        private readonly IUnitOfWork _uow;
        public UpdateWebhookHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<WebhookDto>> Handle(UpdateWebhookCommand request, CancellationToken ct)
        {
            var repo   = _uow.Repository<Webhook>();
            var entity = await repo.GetByIdAsync(request.Dto.Id, ct);
            if (entity == null) return ApiResponse<WebhookDto>.Fail("Webhook not found.");
            var d = request.Dto;
            entity.Name        = d.Name;
            entity.Url         = d.Url;
            entity.Secret      = d.Secret;
            entity.Events      = string.Join(",", d.Events);
            entity.IsActive    = d.IsActive;
            entity.Description = d.Description;
            entity.RetryCount  = d.RetryCount;
            await repo.UpdateAsync(entity, ct);
            return ApiResponse<WebhookDto>.Ok(WebhookMapper.Map(entity), "Webhook updated.");
        }
    }

    public class DeleteWebhookHandler : IRequestHandler<DeleteWebhookCommand, ApiResponse<bool>>
    {
        private readonly IUnitOfWork _uow;
        public DeleteWebhookHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<bool>> Handle(DeleteWebhookCommand request, CancellationToken ct)
        {
            var repo   = _uow.Repository<Webhook>();
            var entity = await repo.GetByIdAsync(request.Id, ct);
            if (entity == null) return ApiResponse<bool>.Fail("Webhook not found.");
            await repo.DeleteAsync(entity, ct);
            return ApiResponse<bool>.Ok(true, "Webhook deleted.");
        }
    }

    public class ToggleWebhookHandler : IRequestHandler<ToggleWebhookCommand, ApiResponse<bool>>
    {
        private readonly IUnitOfWork _uow;
        public ToggleWebhookHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ApiResponse<bool>> Handle(ToggleWebhookCommand request, CancellationToken ct)
        {
            var repo   = _uow.Repository<Webhook>();
            var entity = await repo.GetByIdAsync(request.Id, ct);
            if (entity == null) return ApiResponse<bool>.Fail("Webhook not found.");
            entity.IsActive = !entity.IsActive;
            await repo.UpdateAsync(entity, ct);
            return ApiResponse<bool>.Ok(true, $"Webhook {(entity.IsActive ? "activated" : "deactivated")}.");
        }
    }

    /// <summary>
    /// Delivers a webhook event to all active subscribers for that event.
    /// Signs the payload with HMAC-SHA256 if a secret is configured.
    /// </summary>
    public class DeliverWebhookHandler : IRequestHandler<DeliverWebhookCommand, ApiResponse<bool>>
    {
        private readonly IUnitOfWork _uow;
        private readonly IHttpClientFactory _httpClientFactory;

        public DeliverWebhookHandler(IUnitOfWork uow, IHttpClientFactory httpClientFactory)
        {
            _uow = uow;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<ApiResponse<bool>> Handle(DeliverWebhookCommand request, CancellationToken ct)
        {
            var d = request.Dto;

            // Build the envelope — d.Payload is already a JSON string
            var payloadJson = JsonSerializer.Serialize(new
            {
                @event    = d.Event,
                timestamp = DateTime.UtcNow,
                data      = d.Payload,
            });

            // When WebhookId > 0 deliver to that specific webhook,
            // otherwise fan-out to all active webhooks subscribed to the event
            List<Webhook> webhooks;
            if (d.WebhookId > 0)
            {
                var wh = await _uow.Repository<Webhook>()
                    .TableNoTracking
                    .FirstOrDefaultAsync(w => w.Id == d.WebhookId && w.IsActive, ct);
                webhooks = wh != null ? new List<Webhook> { wh } : new List<Webhook>();
            }
            else
            {
                webhooks = await _uow.Repository<Webhook>()
                    .TableNoTracking
                    .Where(w => w.IsActive && w.Events.Contains(d.Event))
                    .ToListAsync(ct);
            }

            if (!webhooks.Any())
                return ApiResponse<bool>.Ok(false, "No active webhooks found for this event.");

            foreach (var webhook in webhooks)
                await DeliverAsync(webhook, d.Event, payloadJson, 1, ct);

            return ApiResponse<bool>.Ok(true, $"Delivered to {webhooks.Count} webhook(s).");
        }

        private async Task DeliverAsync(Webhook webhook, string eventName, string payloadJson, int attempt, CancellationToken ct)
        {
            var log = new WebhookLog
            {
                WebhookId     = webhook.Id,
                Event         = eventName,
                Payload       = payloadJson,
                AttemptNumber = attempt,
            };

            var sw = Stopwatch.StartNew();
            try
            {
                var client  = _httpClientFactory.CreateClient("WebhookClient");
                var content = new StringContent(payloadJson, Encoding.UTF8, "application/json");

                // HMAC-SHA256 signature header
                if (!string.IsNullOrEmpty(webhook.Secret))
                {
                    var sig = ComputeHmac(payloadJson, webhook.Secret);
                    content.Headers.Add("X-Webhook-Signature", $"sha256={sig}");
                }

                content.Headers.Add("X-Webhook-Event", eventName);
                content.Headers.Add("X-Webhook-Id", webhook.Id.ToString());

                var response = await client.PostAsync(webhook.Url, content, ct);
                sw.Stop();

                log.StatusCode   = (int)response.StatusCode;
                log.ResponseBody = await response.Content.ReadAsStringAsync(ct);
                log.Success      = response.IsSuccessStatusCode;
                log.DurationMs   = sw.ElapsedMilliseconds;

                if (!log.Success && attempt < webhook.RetryCount)
                    log.NextRetryAt = DateTime.UtcNow.AddMinutes(Math.Pow(2, attempt)); // exponential backoff
            }
            catch (Exception ex)
            {
                sw.Stop();
                log.Success      = false;
                log.ErrorMessage = ex.Message;
                log.DurationMs   = sw.ElapsedMilliseconds;
                if (attempt < webhook.RetryCount)
                    log.NextRetryAt = DateTime.UtcNow.AddMinutes(Math.Pow(2, attempt));
            }

            await _uow.Repository<WebhookLog>().AddAsync(log, ct);
        }

        private static string ComputeHmac(string payload, string secret)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            return Convert.ToHexString(hash).ToLower();
        }
    }

    public class RetryWebhookLogHandler : IRequestHandler<RetryWebhookLogCommand, ApiResponse<bool>>
    {
        private readonly IUnitOfWork _uow;
        private readonly IMediator _mediator;

        public RetryWebhookLogHandler(IUnitOfWork uow, IMediator mediator)
        {
            _uow = uow;
            _mediator = mediator;
        }

        public async Task<ApiResponse<bool>> Handle(RetryWebhookLogCommand request, CancellationToken ct)
        {
            var log = await _uow.Repository<WebhookLog>()
                .TableNoTracking
                .Include(l => l.Webhook)
                .FirstOrDefaultAsync(l => l.Id == request.LogId, ct);

            if (log == null) return ApiResponse<bool>.Fail("Log not found.");
            if (log.Success)  return ApiResponse<bool>.Fail("Already succeeded — no retry needed.");

            return await _mediator.Send(new DeliverWebhookCommand(new WebhookDeliverDto
            {
                WebhookId = log.WebhookId,
                Event     = log.Event,
                Payload   = log.Payload ?? "{}",
            }), ct);
        }
    }

    internal static class WebhookMapper
    {
        public static WebhookDto Map(Webhook w, int totalDeliveries = 0, int successfulDeliveries = 0) => new()
        {
            Id                   = w.Id,
            Name                 = w.Name,
            Url                  = w.Url,
            Events               = w.Events,
            IsActive             = w.IsActive,
            Description          = w.Description,
            RetryCount           = w.RetryCount,
            CompanyId            = w.CompanyId,
            CreatedAt            = w.CreatedAt,
            TotalDeliveries      = totalDeliveries,
            SuccessfulDeliveries = successfulDeliveries,
        };
    }
}
