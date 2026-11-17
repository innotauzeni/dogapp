# WhatsApp Notifications - Quick Fix Guide

## 🚨 The Problem

You're only receiving notifications on the registered Meta test number, not on the 4 numbers in your config.

**Why?** Meta WhatsApp Business API restricts messages to:
- ✅ Registered test numbers only
- ✅ Users who messaged you first (24h window)
- ✅ Pre-approved template messages

**You cannot send regular text to random numbers** - It's Meta's anti-spam policy.

---

## ✅ Quick Fix (Choose One)

### Option 1: Register Test Numbers (5 minutes)

**Best for:** Immediate testing, small team

**Steps:**
1. Go to https://business.facebook.com/
2. WhatsApp Manager → Settings → Phone numbers
3. Click "Add phone number" → "Test number"
4. Add each of your 4 numbers:
   - 263773512690
   - 263774914150
   - 263772539121
   - 263785764731
5. Verify each with code sent via WhatsApp
6. Done! Test immediately.

**Limit:** Maximum 5 test numbers total

---

### Option 2: Create Template Message (24-48 hours)

**Best for:** Production use, unlimited numbers

**Steps:**

#### 1. Create Template
Go to: https://business.facebook.com/ → WhatsApp Manager → Message templates

**Click "Create template"**

**Fill in:**
- **Template name:** `dog_notification`
- **Category:** UTILITY
- **Language:** English
- **Body text:** `{{1}}`
  *(This placeholder will be replaced with your notification text)*

**Optional fields:**
- **Header:** `Dog Management Alert`
- **Footer:** `Dog Section System`

#### 2. Submit for Review
Click "Submit" - Meta reviews in 24-48 hours

#### 3. Wait for Approval
Check status in Message templates section

#### 4. Test After Approval
Once approved, notifications will work for ALL numbers (no limit)

---

## 🔧 Code Already Fixed

I've updated your code to:
- ✅ Try regular message first (works for test numbers)
- ✅ Auto-fallback to template if needed
- ✅ Detailed logging for debugging

**File modified:** `Dog.Api/Services/WhatsAppBotService.cs`

---

## 🧪 How to Test

### After registering test numbers:
```bash
# Add a dog or record feeding
# Check logs for:
✅ WhatsApp message sent successfully to 263773512690
```

### After template approval:
```bash
# Add a dog or record feeding
# Check logs for:
⚠️ Phone requires template message
✅ WhatsApp template message sent successfully to 263773512690
```

---

## 📋 Verification

Check your logs at: `Dog.Api/Logs/`

**Success:**
```
✅ WhatsApp message sent successfully to 263773512690
✅ WhatsApp message sent successfully to 263774914150
✅ WhatsApp message sent successfully to 263772539121
✅ WhatsApp message sent successfully to 263785764731
```

**Template fallback:**
```
⚠️ Text message failed for 263773512690, attempting template message fallback
✅ WhatsApp template message sent successfully to 263773512690
```

---

## 🎯 My Recommendation

**For your case (4 specific numbers):**

### Short-term (Today):
✅ Register all 4 numbers as test numbers (5 min)
✅ Test immediately
✅ Verify all receive notifications

### Long-term (Production):
✅ Create `dog_notification` template
✅ Submit for approval
✅ Deploy in 2-3 days once approved
✅ Can add unlimited numbers later

---

## 📱 Alternative: User Opt-In

Want users to opt-in themselves?

**Add this link to your portal:**
```
https://wa.me/1245355721991877?text=Hi
```

**Or generate QR code:**
Go to: https://www.qr-code-generator.com/
Enter: `https://wa.me/1245355721991877?text=Hi`

**User flow:**
1. User clicks link or scans QR
2. Opens WhatsApp
3. Sends "Hi" message
4. Can receive regular messages for 24 hours
5. After 24h, needs template

---

## ⚠️ Common Errors

| Error | Meaning | Fix |
|-------|---------|-----|
| "user's number is part of an experiment" | Not a test number | Add as test number |
| "Receiver is not on allowed list" | Not registered | Add as test number |
| Error 131047 / 131026 | Not opted in | Use template message |
| "Template not found" | Template not created | Create in Meta Business Manager |

---

## 🚀 Action Plan

**Right now (5 minutes):**
1. Open https://business.facebook.com/
2. Go to WhatsApp Manager
3. Add 4 numbers as test numbers
4. Test notification

**Today (1 hour):**
1. Create `dog_notification` template
2. Submit for approval
3. Wait for approval

**Tomorrow (after approval):**
1. Test template messages
2. Deploy to production
3. All numbers receive notifications! 🎉

---

## 📞 Need Help?

**Meta Business Support:**
https://business.facebook.com/help/

**Your Support:**
support@dogsection.com

---

## 📚 Detailed Guide

For complete explanation and advanced options, see:
`deploy/WHATSAPP_NOTIFICATION_SETUP_GUIDE.md`

---

**Status:** Code is fixed and ready. Now you need to either:
1. ✅ Register test numbers (quick), OR
2. ✅ Get template approved (production)

**Estimated time to fix:** 5 minutes (test numbers) or 2 days (template approval)

Good luck! 🚀
