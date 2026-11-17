using Dog.Api.Services;
using Dog.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Dog.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class WhatsAppController : ControllerBase
{
    private readonly IWhatsAppBotService _botService;
    private readonly ILogger<WhatsAppController> _logger;
    private readonly IErrorLoggingService _errorLog;
    private readonly string? _webhookToken;

    public WhatsAppController(IWhatsAppBotService botService, IConfiguration config, ILogger<WhatsAppController> logger, IErrorLoggingService errorLog)
    {
        _botService = botService;
        _logger = logger;
        _errorLog = errorLog;
        _webhookToken = config["WhatsApp:WebhookToken"];
    }

    /// <summary>Webhook for receiving WhatsApp messages from Meta API.</summary>
    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> ReceiveMessage([FromBody] JsonElement body)
    {
        try
        {
            _logger.LogInformation("🔔 WhatsApp webhook received");
            _logger.LogInformation("📦 Raw payload: {Payload}", body.ToString());

            var entry = body.GetProperty("entry");
            _logger.LogInformation("✅ Entry array found with {Count} items", entry.GetArrayLength());

            var firstEntry = entry[0];
            var changes = firstEntry.GetProperty("changes");
            _logger.LogInformation("✅ Changes array found with {Count} items", changes.GetArrayLength());

            var root = changes[0].GetProperty("value");
            _logger.LogInformation("✅ Value object extracted");

            // Verify it's a message (not status update)
            if (!root.TryGetProperty("messages", out var messages))
            {
                _logger.LogInformation("ℹ️ No messages property found (likely status update)");
                return Ok();
            }

            _logger.LogInformation("✅ Messages property found with {Count} messages", messages.GetArrayLength());

            var message = messages[0];
            var from = message.GetProperty("from").GetString();
            var text = message.GetProperty("text").GetProperty("body").GetString();

            _logger.LogInformation("📱 WhatsApp message received - From: {Phone}, Text: {Text}", from, text);

            // Process message
            _logger.LogInformation("🤖 Processing message with bot service...");
            var response = await _botService.ProcessMessageAsync(from, text);
            _logger.LogInformation("✅ Bot response generated: {Response}", response);

            // Send response
            _logger.LogInformation("📤 Sending response back to {Phone}", from);
            await _botService.SendMessageAsync(from, response);
            _logger.LogInformation("✅ Response sent successfully");

            return Ok(new { success = true });
        }
        catch (JsonException jsonEx)
        {
            _logger.LogError(jsonEx, "❌ JSON parsing error in WhatsApp webhook");
            await _errorLog.LogErrorAsync(jsonEx, "WhatsApp webhook JSON parsing failed");
            return Ok(new { success = false, error = "JSON parsing failed" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error processing WhatsApp message");
            await _errorLog.LogErrorAsync(ex, "WhatsApp webhook processing failed");
            return Ok(new { success = false, error = ex.Message });
        }
    }

    /// <summary>Webhook verification for Meta API.</summary>
    [HttpGet("webhook")]
    [AllowAnonymous]
    public IActionResult VerifyWebhook([FromQuery(Name = "hub.mode")] string mode, [FromQuery(Name = "hub.challenge")] string challenge, [FromQuery(Name = "hub.verify_token")] string verify_token)
    {
        if (mode == "subscribe" && verify_token == _webhookToken)
            return Ok(challenge);

        _logger.LogWarning("Webhook verification failed");
        return Unauthorized();
    }

    /// <summary>Send WhatsApp message to a user.</summary>
    [HttpPost("send")]
    public async Task<IActionResult> SendMessage([FromBody] SendWhatsAppRequest request)
    {
        try
        {
            await _botService.SendMessageAsync(request.PhoneNumber, request.Message);
            return Ok(new { success = true, message = "Message sent" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending WhatsApp message");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }
}

public class SendWhatsAppRequest
{
    public string PhoneNumber { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
