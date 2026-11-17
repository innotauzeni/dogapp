# WhatsApp Notifications Troubleshooting Guide

## ✅ Your Configuration Looks Good

From your production appsettings.json:
- ✅ WhatsApp AccessToken is set
- ✅ PhoneNumberId is set
- ✅ ApiVersion is set to v25.0
- ✅ NotifyWhatsapp numbers are configured: 263773512690, 263774914150, 263772539121, 263785764731

## 🔍 Diagnostic Steps

### Step 1: Check Application Logs

Look for these log messages in your application logs:

**Good signs (notifications working):**
```
📤 SendMessage called - Phone: 263773512690
🌐 WhatsApp API URL: https://graph.facebook.com/v25.0/1245355721991877/messages
📱 Normalized phone: 263773512690
📦 Sending payload: {...}
🚀 Posting to WhatsApp API...
✅ WhatsApp message sent successfully to 263773512690
```

**Problem signs:**
```
⚠️ WhatsApp credentials not configured
❌ WhatsApp message send failed - StatusCode: XXX
⚠️ WhatsApp send skipped — credentials not configured
```

### Step 2: Test if Activities Are Being Recorded

1. Add a new dog via web portal or mobile app
2. Check the database - is the dog added?
3. Record a feeding - is it saved?

If activities are NOT being saved, the notification code never runs.

### Step 3: Check if Background Task is Running

The notifications run in background tasks using `Task.Run()`. Check if:
- The main operation completes successfully
- No exceptions in error logs

### Step 4: Test WhatsApp API Directly

Run this command to test if your WhatsApp credentials work:

```bash
curl -X POST \
  "https://graph.facebook.com/v25.0/1245355721991877/messages" \
  -H "Authorization: Bearer EAAUKkNjfG0ABR49Wa2GiVLisRNnDZAlMVbh3nWRNxw4ykYL7qH3GXDl3qMlHUogpQc48T7YqYbsGNcIcK4XCZCC32PKZCzysNQo6r78WZBe3TiMn0SCMS2As8ZCANky4hhWOi3LZBilUNuVkO7H7kEZCMkoSZC77pAYS6nF07AMnoR2Rx6JvOJbZC8M5jdryXxwZDZD" \
  -H "Content-Type: application/json" \
  -d '{
    "messaging_product": "whatsapp",
    "recipient_type": "individual",
    "to": "263773512690",
    "type": "text",
    "text": {
      "preview_url": false,
      "body": "Test message from Dog Management System"
    }
  }'
```

**Expected response (success):**
```json
{
  "messaging_product": "whatsapp",
  "contacts": [{
    "input": "263773512690",
    "wa_id": "263773512690"
  }],
  "messages": [{
    "id": "wamid.XXX"
  }]
}
```

**Error responses:**
```json
{
  "error": {
    "message": "Invalid OAuth access token",
    "type": "OAuthException",
    "code": 190
  }
}
```

## 🐛 Common Issues & Fixes

### Issue 1: Access Token Expired
**Symptom:** Error 190 or "Invalid OAuth access token"

**Fix:**
1. Go to Meta Business Suite (business.facebook.com)
2. Navigate to WhatsApp > API Setup
3. Generate a new access token
4. Update appsettings.json with new token
5. Restart the API service

### Issue 2: Phone Number Not Registered
**Symptom:** Error "Recipient phone number not engaged"

**Fix:**
The phone numbers must be opted-in to receive messages from your WhatsApp Business Account:
1. Each number must send a message to your WhatsApp Business number first
2. Or you must have sent a template message to them within 24 hours

### Issue 3: Rate Limiting
**Symptom:** Some messages send, others don't

**Fix:**
- Meta has rate limits (varies by tier)
- Add delays between messages
- Check your WhatsApp Business tier limits

### Issue 4: Notifications Running but Silent Failures
**Symptom:** No errors in logs, no messages received

**Cause:** Notifications use `Task.Run()` with silent catch blocks

**Fix:** Check error logs table in database:
```sql
SELECT * FROM ka_sytemerrorlogs 
WHERE Source LIKE '%WhatsApp%' 
ORDER BY CreatedAt DESC 
LIMIT 50;
```

### Issue 5: Service Not Registered
**Symptom:** NullReferenceException in handlers

**Fix:** Verify Program.cs has:
```csharp
builder.Services.AddScoped<Dog.Application.Contracts.IWhatsAppNotificationService, Dog.Api.Services.WhatsAppNotificationService>();
```

### Issue 6: Numbers Format Issue
**Symptom:** Messages fail with "invalid phone number"

**Current format:** `263773512690` ✅ (Correct)
**Wrong formats:** 
- `+263 77 351 2690` ❌
- `0773512690` ❌
- `263 773512690` ❌

## 📊 Enhanced Logging (Temporary Debug)

To get more detailed logs, temporarily modify `WhatsAppNotificationService.cs`:

Add at the start of `SendToAllNotificationNumbers`:
```csharp
private async Task SendToAllNotificationNumbers(string message)
{
    Console.WriteLine($"🔔 NOTIFICATION: Attempting to send to {_notificationNumbers.Count} numbers");
    
    foreach (var number in _notificationNumbers)
    {
        Console.WriteLine($"📱 Sending to: {number}");
        try
        {
            await _whatsAppBot.SendMessageAsync(number, message);
            Console.WriteLine($"✅ Sent to: {number}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Failed to send to {number}: {ex.Message}");
        }
    }
}
```

Check console/log output for these messages.

## 🧪 Manual Test Script

Create a test endpoint to verify notifications work:

**Add to AnimalController.cs:**
```csharp
[HttpPost("test-notification")]
public async Task<IActionResult> TestNotification([FromServices] IWhatsAppNotificationService notifyService)
{
    try
    {
        var testDog = new Dogtbl 
        { 
            Name = "Test Dog", 
            Breed = "Test Breed",
            Gender = "Male",
            DateOfBirth = DateTime.Now.AddYears(-1)
        };
        
        var testRecord = new DogCareRecord 
        { 
            OccurredOn = DateTime.Now,
            QuantityOrOutcome = "2 cups",
            Details = "Test feeding"
        };
        
        await notifyService.NotifyDogFedAsync(testDog, testRecord, "Test Handler");
        
        return Ok(new { message = "Notification sent! Check WhatsApp." });
    }
    catch (Exception ex)
    {
        return BadRequest(new { error = ex.Message, stackTrace = ex.StackTrace });
    }
}
```

**Test:**
```bash
curl -X POST "http://your-server:9001/api/Animal/test-notification" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

## 📝 Verification Checklist

Run through this checklist:

- [ ] Application deployed and running (check: `curl http://your-server:9001/health`)
- [ ] appsettings.json has correct WhatsApp credentials
- [ ] NotifyWhatsapp section has 4 phone numbers
- [ ] Service registered in Program.cs
- [ ] No compilation errors (check deployment logs)
- [ ] Add a dog through web portal - does it save?
- [ ] Check application logs for "📤 SendMessage called"
- [ ] Check application logs for "✅ WhatsApp message sent"
- [ ] Check error logs for WhatsApp-related errors
- [ ] Test WhatsApp API directly with curl (see Step 4)
- [ ] Verify phone numbers are opted-in/engaged

## 🔧 Quick Fixes to Try

### Fix 1: Restart the API Service
```bash
sudo systemctl restart dog-api
# or
systemctl restart dog-api.service
```

### Fix 2: Check Service Status
```bash
sudo systemctl status dog-api
sudo journalctl -u dog-api -n 100 --no-pager
```

### Fix 3: Verify Deployment
```bash
# Check if new files deployed
ls -la /var/www/dog-api/Services/WhatsAppNotificationService.dll
# Should exist and have recent timestamp
```

### Fix 4: Force Rebuild and Redeploy
```bash
# On development machine
cd "c:\Users\Admin\Documents\Visual Studio 18\l\csharpdata\Dog"
dotnet clean
dotnet build --configuration Release
dotnet publish Dog.Api/Dog.Api.csproj --configuration Release --output ./publish

# Copy to server and restart
```

## 📞 Getting Help

If still not working, collect this info:

1. **Application Logs** (last 100 lines):
   ```bash
   sudo journalctl -u dog-api -n 100
   ```

2. **Recent error logs from database:**
   ```sql
   SELECT * FROM ka_sytemerrorlogs 
   WHERE CreatedAt > NOW() - INTERVAL 1 HOUR
   ORDER BY CreatedAt DESC;
   ```

3. **Test curl result** (from Step 4)

4. **Deployment verification:**
   ```bash
   ls -la /var/www/dog-api/Services/
   ```

Share these and I can help diagnose further!
