// TEMPORARY TEST ENDPOINT - Add this to your AnimalController.cs
// This will help test if WhatsApp notifications work

using Microsoft.AspNetCore.Mvc;
using Dog.Application.Contracts;
using Dog.Core.Entities;

// Add this method to your AnimalController class:

/// <summary>
/// TEST ENDPOINT - Sends a test WhatsApp notification to verify it's working
/// DELETE THIS AFTER TESTING
/// </summary>
[HttpPost("test-whatsapp-notification")]
[AllowAnonymous] // Remove this line if you want to keep auth
public async Task<IActionResult> TestWhatsAppNotification(
    [FromServices] IWhatsAppNotificationService notificationService)
{
    try
    {
        // Create test dog
        var testDog = new Dogtbl
        {
            Id = 999,
            Name = "Test Dog Max",
            Breed = "German Shepherd",
            Gender = "Male",
            DateOfBirth = DateTime.Now.AddYears(-2),
            MicrochipNumber = "TEST123456"
        };

        // Create test care record
        var testRecord = new DogCareRecord
        {
            Id = 999,
            DogId = 999,
            EventType = "Feeding",
            OccurredOn = DateTime.Now,
            QuantityOrOutcome = "2 cups of premium dog food",
            Details = "Test feeding - this is a test notification",
            RequiresAttention = false
        };

        // Send notification
        await notificationService.NotifyDogFedAsync(testDog, testRecord, "System Test");

        return Ok(new
        {
            success = true,
            message = "Test notification sent successfully!",
            sentTo = new[] {
                "263773512690",
                "263774914150",
                "263772539121",
                "263785764731"
            },
            instructions = "Check WhatsApp on these 4 numbers. If you receive a message about 'Test Dog Max' being fed, notifications are working!"
        });
    }
    catch (Exception ex)
    {
        return BadRequest(new
        {
            success = false,
            error = ex.Message,
            stackTrace = ex.StackTrace,
            innerException = ex.InnerException?.Message
        });
    }
}

/* 
HOW TO USE THIS TEST ENDPOINT:

1. Add this method to your AnimalController.cs

2. Rebuild and redeploy your API

3. Test with curl:
   curl -X POST http://your-server:9001/api/Animal/test-whatsapp-notification

4. Or test in browser:
   http://your-server:9001/api/Animal/test-whatsapp-notification

5. Check the response:
   - If success = true: Check your WhatsApp for the test message
   - If success = false: The error message will tell you what's wrong

6. Once confirmed working, DELETE this endpoint for security

EXPECTED WHATSAPP MESSAGE:
🍖 DOG FEEDING RECORDED

🐕 Dog: Test Dog Max
🕐 Time: Jan 25, 2026 · 14:30
🥘 Quantity: 2 cups of premium dog food
👤 Fed by: System Test
📝 Notes: Test feeding - this is a test notification

✅ Feeding recorded successfully.
*/
