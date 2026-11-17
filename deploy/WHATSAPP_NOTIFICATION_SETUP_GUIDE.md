# WhatsApp Notification Setup Guide

## 🚨 Important: Meta WhatsApp Business API Limitations

**The issue:** By default, Meta WhatsApp Business API **ONLY** allows sending messages to:

1. ✅ **Test phone numbers** registered in Meta Business Manager
2. ✅ **Users who messaged you first** within last 24 hours (active conversation)
3. ✅ **Template messages** (requires pre-approval from Meta)

**You CANNOT send regular text messages to random phone numbers** - This is Meta's anti-spam policy.

---

## 🎯 Solution Options

### Option 1: Register Test Numbers (Quick, Testing Only)

**Best for:** Testing, development, small team notifications

**Steps:**
1. Go to Meta Business Manager: https://business.facebook.com/
2. Navigate to **WhatsApp Manager**
3. Go to **Settings** → **Phone numbers**
4. Click **Add phone number** → Add as **Test Number**
5. Add all 4 numbers:
   - 263773512690
   - 263774914150
   - 263772539121
   - 263785764731

**Limitations:**
- Maximum 5 test numbers
- Only for testing, not production use
- Test numbers can only be used for testing, not real business

---

### Option 2: Use Template Messages (Recommended for Production)

**Best for:** Production, unlimited phone numbers, business use

WhatsApp **requires** pre-approved message templates for notifications to users who haven't opted in.

#### Step 1: Create Message Template

1. Go to **Meta Business Manager**: https://business.facebook.com/
2. Navigate to **WhatsApp Manager**
3. Go to **Message templates**
4. Click **Create template**

#### Step 2: Template Configuration

**Template Name:** `dog_notification`

**Category:** UTILITY (for transactional notifications)

**Language:** English

**Header (Optional):** Text
```
Dog Management Alert
```

**Body:** (Use {{1}} as variable placeholder)
```
{{1}}
```

**Footer (Optional):**
```
Dog Section Management System
```

**Buttons (Optional):**
- Reply button: "View Details"
- Reply button: "Contact Support"

#### Step 3: Submit for Review

- Click **Submit**
- Meta reviews within 24-48 hours
- Once approved, template is active

#### Step 4: Update Configuration

The code I just fixed will automatically use template messages as fallback.

**Template structure:**
```json
{
  "name": "dog_notification",
  "language": {"code": "en"},
  "components": [{
    "type": "body",
    "parameters": [{
      "type": "text",
      "text": "Your actual notification message here"
    }]
  }]
}
```

---

### Option 3: Opt-In Flow (Best User Experience)

**Best for:** Users who want notifications, legal compliance, high engagement

#### How It Works:
1. User sends any message to your WhatsApp Business number
2. They're now "opted in" for 24 hours
3. You can send regular text messages within that window
4. After 24 hours, must use templates again

#### Implementation:
1. Add QR code or "Click to Chat" link on your website
2. Users scan/click to initiate conversation
3. They send "Hi" or any message
4. Now they can receive notifications for 24 hours
5. After 24h, use template messages

**Click to Chat Link:**
```
https://wa.me/263788937675?text=Hi,%20I%20want%20to%20receive%20dog%20notifications
```

---

## 🛠️ Current Implementation (Already Fixed)

I've updated the code to:

1. ✅ Try sending regular text message first (works for test numbers & active conversations)
2. ✅ If that fails, automatically try template message fallback
3. ✅ Logs detailed error information
4. ✅ Handles opt-in requirements gracefully

**Files Modified:**
- `Dog.Api/Services/WhatsAppBotService.cs` - Added template message support

---

## 📋 Step-by-Step Setup (Recommended Approach)

### For Production Use:

#### 1. Create Template in Meta Business Manager

**Go to:** https://business.facebook.com/ → WhatsApp Manager → Message templates

**Create template:**
- **Name:** `dog_notification`
- **Category:** UTILITY
- **Language:** English
- **Body:** `{{1}}` (this will be your dynamic notification text)

**Submit for approval** - Takes 24-48 hours

#### 2. Update appsettings.json (Optional Template Name)

If you want to use a different template name:

```json
{
  "WhatsApp": {
    "AccessToken": "your_token",
    "PhoneNumberId": "your_phone_id",
    "TemplateName": "dog_notification",
    "TemplateLanguage": "en"
  }
}
```

#### 3. Test After Approval

Once template is approved:
```bash
# Test notification
curl -X POST https://yourdomain.com/api/test-whatsapp \
  -H "Content-Type: application/json" \
  -d '{"phoneNumber": "263773512690", "message": "Test dog notification"}'
```

#### 4. Add All Numbers to Notifications

Your current config is correct:
```json
{
  "NotifyWhatsapp": {
    "WhatsAppNumbers": "263773512690,263774914150,263772539121,263785764731"
  }
}
```

All these numbers will receive notifications via template messages.

---

## 🚀 Alternative: Register Test Numbers (Quick Setup for Testing)

**For immediate testing without waiting for template approval:**

### Step 1: Go to Meta Business Manager
https://business.facebook.com/

### Step 2: Navigate to WhatsApp Manager
Settings → Phone numbers → Add phone number

### Step 3: Register Each Test Number

Add these as **test numbers** (not production):
1. 263773512690
2. 263774914150
3. 263772539121
4. 263785764731

### Step 4: Verify Each Number

Meta will send verification code to each number via WhatsApp

### Step 5: Test Immediately

Once registered, messages will work instantly (no template needed for test numbers)

**Limitations:**
- Maximum 5 test numbers total
- Only for testing/development
- Not suitable for production

---

## 🔍 How to Check Current Status

### View Logs

Check your application logs for:

**Success:**
```
✅ WhatsApp message sent successfully to 263773512690
```

**Opted-in required:**
```
⚠️ Phone 263773512690 requires template message (not opted in)
📋 Attempting template message fallback
```

**Template sent:**
```
✅ WhatsApp template message sent successfully to 263773512690
```

### Check Meta Business Manager

1. Go to https://business.facebook.com/
2. WhatsApp Manager → Insights
3. View message delivery status
4. Check for errors or failures

---

## 📱 User Opt-In Implementation (Optional)

To allow users to opt in for regular text messages:

### Add QR Code to Your Portal

Generate QR code for:
```
https://wa.me/1245355721991877?text=Hi,%20I%20want%20dog%20notifications
```

**Generate QR Code:** https://www.qr-code-generator.com/

### Add "Click to Chat" Button

```html
<a href="https://wa.me/1245355721991877?text=Hi" target="_blank">
  Enable WhatsApp Notifications
</a>
```

### User Flow:
1. User clicks link or scans QR
2. Opens WhatsApp with pre-filled message
3. User sends message
4. Bot responds with confirmation
5. User is now opted in for 24 hours
6. Can receive regular text notifications

---

## ⚠️ Common Errors & Solutions

### Error: "user's number is part of an experiment"
**Solution:** Number needs to be registered as test number OR use template message

### Error: "Receiver is not on allowed list"
**Solution:** Add number as test number in Meta Business Manager

### Error: "Template not found"
**Solution:** Create template in Meta Business Manager and wait for approval

### Error: "Invalid parameter"
**Solution:** Check template name matches exactly (case-sensitive)

### Error: 131047 or 131026
**Solution:** User hasn't opted in - use template message instead

---

## 📊 Notification Flow Diagram

```
User Activity (Dog Fed, Dog Added, etc.)
            ↓
  WhatsAppNotificationService
            ↓
    Try Regular Text Message
            ↓
         Success? ────── YES ──→ ✅ Message Delivered
            │
            NO
            ↓
    Try Template Message
            ↓
         Success? ────── YES ──→ ✅ Message Delivered
            │
            NO
            ↓
    Log Error & Continue
```

---

## 🎯 Recommended Setup for Your Case

**For production use with your 4 numbers:**

### Option A: Template Messages (Best for Production)

1. ✅ Create `dog_notification` template in Meta Business Manager
2. ✅ Submit for approval (24-48 hours)
3. ✅ Once approved, all 4 numbers will receive notifications
4. ✅ No limits, works for any phone number

**Pros:**
- Works for unlimited numbers
- Production-ready
- Complies with Meta policies
- Professional

**Cons:**
- Requires approval (1-2 days wait)
- Less flexible message format

### Option B: Test Numbers (Quick Testing)

1. ✅ Register all 4 numbers as test numbers
2. ✅ Works immediately
3. ✅ Can send any message format

**Pros:**
- Immediate setup
- Full message flexibility
- No approval needed

**Cons:**
- Only 5 numbers max
- Testing only, not production
- Against Meta TOS for production use

---

## 📞 Meta Business Support

**Need help?**
- Meta Business Help: https://business.facebook.com/help/
- WhatsApp Business API Docs: https://developers.facebook.com/docs/whatsapp/
- Support: https://business.facebook.com/business/help/

---

## ✅ Verification Checklist

After setup, verify:

- [ ] Template created in Meta Business Manager (if using templates)
- [ ] Template approved (check status)
- [ ] Template name matches code (`dog_notification`)
- [ ] All 4 phone numbers in NotifyWhatsapp config
- [ ] WhatsApp AccessToken and PhoneNumberId configured
- [ ] Test notification sent successfully
- [ ] Check logs for success messages
- [ ] Check Meta Business Manager for delivery status

---

## 🚀 Next Steps

### Immediate (Testing):
1. Register 4 numbers as test numbers
2. Test notifications
3. Verify delivery

### Production (Best Practice):
1. Create `dog_notification` template
2. Submit for approval
3. Wait 24-48 hours
4. Deploy updated code
5. Test template notifications
6. Go live!

### Optional (Enhanced UX):
1. Add QR code to portal for opt-in
2. Add "Enable Notifications" button
3. Send welcome message on opt-in
4. Track opt-in status

---

## 📧 Support

**Questions?**
- Email: support@dogsection.com
- Meta Business Support: https://business.facebook.com/help/

---

**Current Status:** Code updated to support template message fallback. Waiting for you to either:
1. Register test numbers (quick), OR
2. Create and get template approved (production-ready)

Choose based on your timeline and needs! 🚀
