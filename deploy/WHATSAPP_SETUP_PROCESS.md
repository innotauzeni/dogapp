# 📱 WhatsApp Bot Integration - Complete Setup & Usage Process

---

## 📋 Table of Contents
1. [Prerequisites](#prerequisites)
2. [Step-by-Step Setup](#step-by-step-setup)
3. [Configuration](#configuration)
4. [Testing](#testing)
5. [Workflow Examples](#workflow-examples)
6. [Troubleshooting](#troubleshooting)
7. [Best Practices](#best-practices)
8. [Feature Reference](#feature-reference)

---

## 📌 Prerequisites

### What You'll Need:
- [ ] Meta Business Account (free at business.facebook.com)
- [ ] WhatsApp Business API access (from Meta)
- [ ] A business phone number
- [ ] API server (Dog Section API)
- [ ] Access to appsettings.json on your server
- [ ] Basic understanding of API webhooks

### Estimated Setup Time:
- **Basic Setup:** 30 minutes
- **Full Configuration:** 1-2 hours
- **Testing:** 15 minutes

---

## 🚀 Step-by-Step Setup Process

### **STEP 1: Get Meta Business Account & Credentials**

#### 1.1 Create Meta Business Account
```
1. Go to: https://business.facebook.com
2. Click "Create Account"
3. Enter business details:
   - Business Name: "Dog Section"
   - Your Name
   - Email
   - Phone Number
4. Click "Create Account"
```

#### 1.2 Get WhatsApp Business API Access
```
1. Log into Meta Business Suite
2. Go to "Settings" → "Apps and websites"
3. Click "Add Apps"
4. Search for "WhatsApp"
5. Click "Set Up" on WhatsApp Business Platform
6. Follow the setup wizard
7. Save your credentials:
   - Access Token (keep this SECRET!)
   - Phone Number ID
   - Business Account ID
```

#### 1.3 Create Business Phone Number
```
1. In Meta Business Suite
2. Go to "WhatsApp" → "Phone Numbers"
3. Click "Add Number"
4. Choose:
   - Existing number (if you have one)
   - OR request a new number
5. Verify the number (via SMS code)
6. Note the Phone Number ID
```

### **STEP 2: Configure Your API Server**

#### 2.1 Update appsettings.json
```json
{
  "WhatsApp": {
    "AccessToken": "YOUR_ACCESS_TOKEN_HERE",
    "PhoneNumberId": "YOUR_PHONE_ID_HERE",
    "WebhookToken": "YOUR_SECRET_VERIFY_TOKEN"
  }
}
```

**Where to find values:**
- **AccessToken**: Meta Business Suite → Settings → Tokens
- **PhoneNumberId**: Meta Business Suite → WhatsApp → Phone Numbers
- **WebhookToken**: Create a random string (e.g., "super_secret_token_12345")

#### 2.2 Set Environment Variables (Production)
```bash
# On your server (not in appsettings.json)
export WhatsApp__AccessToken=your_token_here
export WhatsApp__PhoneNumberId=your_phone_id_here
export WhatsApp__WebhookToken=your_webhook_token_here
```

**Or on Windows:**
```powershell
$env:WhatsApp__AccessToken = "your_token_here"
$env:WhatsApp__PhoneNumberId = "your_phone_id_here"
$env:WhatsApp__WebhookToken = "your_webhook_token_here"
```

#### 2.3 Restart API Service
```bash
# If running as service
net stop "DogSectionAPI"
net start "DogSectionAPI"

# Or if running directly
# Stop the running process and restart
```

### **STEP 3: Configure Webhook in Meta**

#### 3.1 Set Webhook URL
```
1. Meta Business Suite
2. Settings → WhatsApp → Configuration
3. Scroll to "Webhooks"
4. Click "Edit Subscriptions"
5. Enter:
   - Callback URL: https://your-api-domain.com/api/WhatsApp/webhook
   - Verify Token: Your_Webhook_Token (same as appsettings.json)
6. Click "Verify"
```

**Important:**
- Must use HTTPS (not HTTP)
- Firewall must allow incoming requests
- URL must be publicly accessible

#### 3.2 Subscribe to Message Webhooks
```
1. Still in "Edit Subscriptions"
2. Check these fields:
   ✅ messages
   ✅ message_template_status_update
   ✅ message_template_quality_update
3. Click "Save"
```

### **STEP 4: Test Webhook Connection**

#### 4.1 Manual Test (in Meta)
```
1. Meta Business Suite
2. Settings → WhatsApp → Webhooks
3. Click "Test Webhook"
4. Check console logs on your server
5. Verify you see:
   "Webhook verification request received"
```

#### 4.2 Send Test Message
```
1. Get the bot's WhatsApp number from Meta
2. Message the number: "help"
3. Should receive:
   🐾 Dog Management Bot Commands:
   help - Show this menu
   dogs - List all your dogs
   ...
```

---

## ⚙️ Configuration Details

### WhatsAppBotService.cs Setup
```csharp
// File: Dog.Api/Services/WhatsAppBotService.cs
// Automatically reads from appsettings.json
// Constructor:
public WhatsAppBotService(
    HttpClient httpClient, 
    IConfiguration config, 
    ILogger<WhatsAppBotService> logger)
{
    _whatsAppToken = config["WhatsApp:AccessToken"];
    _phoneNumberId = config["WhatsApp:PhoneNumberId"];
}
```

### Dependency Injection (Program.cs)
```csharp
// Already added:
builder.Services.AddScoped<IWhatsAppBotService, WhatsAppBotService>();
builder.Services.AddHttpClient<IWhatsAppBotService>();
```

### API Endpoints Active
```
POST   /api/WhatsApp/webhook    (receive messages)
GET    /api/WhatsApp/webhook    (verify webhook)
POST   /api/WhatsApp/send       (send messages manually)
```

---

## ✅ Testing Checklist

### Pre-Launch Tests
- [ ] Webhook URL is valid HTTPS
- [ ] Credentials in appsettings.json are correct
- [ ] API server is running and accessible
- [ ] Firewall allows incoming requests
- [ ] Meta webhook verification passes
- [ ] Test message "help" gets response

### Message Tests
```
TEST 1: Help command
  Send: help
  Expect: Bot shows help menu ✅

TEST 2: List dogs
  Send: dogs
  Expect: Bot shows dogs list ✅

TEST 3: View dog details
  Send: view 1
  Expect: Bot shows dog details ✅

TEST 4: Record activity
  Send: record 1 feeding
  Expect: Activity recorded confirmation ✅

TEST 5: Vaccine check
  Send: vaccine
  Expect: Dogs needing vaccines ✅
```

---

## 💬 Workflow Examples

### **Workflow 1: Daily Health Check**
```
Step 1: Get dog list
  User: dogs
  Bot:  Shows all dogs with IDs

Step 2: Check specific dog
  User: view 1
  Bot:  Shows complete dog details
        (including vaccinations, health status)

Step 3: Record observation
  User: record 1 feeding Good appetite
  Bot:  ✅ Activity recorded

Step 4: Quick stats
  User: status
  Bot:  Shows dashboard overview
```

### **Workflow 2: Vaccination Management**
```
Step 1: Check due vaccines
  User: vaccine
  Bot:  Shows dogs needing vaccines

Step 2: View vaccine history
  User: view 1
  Bot:  Shows all vaccines for dog #1

Step 3: Record vaccination
  User: record 1 vaccine Rabies booster given
  Bot:  ✅ Vaccination recorded

Step 4: Confirm update
  User: view 1
  Bot:  Shows updated vaccine info
```

### **Workflow 3: Breeding Management**
```
Step 1: Check breeding status
  User: breeding
  Bot:  Shows active matings and litters

Step 2: View breeding dog
  User: view 5
  Bot:  Shows dog with offspring info

Step 3: Record mating
  User: record 5 mating Mated with dog #8
  Bot:  ✅ Mating recorded

Step 4: Monitor puppies
  User: view [puppyId]
  Bot:  Shows puppy health status
```

---

## 🔧 Troubleshooting Guide

### **Issue 1: Bot Not Responding**

**Symptoms:**
- Send message but no response
- Nothing in API logs

**Solutions:**
1. Check webhook URL is correct in Meta
2. Verify HTTPS is used (not HTTP)
3. Test webhook verification:
   ```
   curl -X GET "https://your-api/api/WhatsApp/webhook?mode=subscribe&challenge=test_challenge&verify_token=YOUR_TOKEN"
   ```
4. Check firewall allows incoming requests
5. Verify appsettings.json credentials

### **Issue 2: Webhook Verification Fails**

**Symptoms:**
- Error: "Webhook verification failed"
- In Meta: "Webhook could not be verified"

**Solutions:**
1. Verify token must EXACTLY match between:
   - appsettings.json: WhatsApp:WebhookToken
   - Meta: Callback Verify Token
2. Example:
   ```json
   // appsettings.json
   "WhatsApp": {
     "WebhookToken": "my_secret_token_123"
   }
   // Meta → set to: my_secret_token_123
   ```
3. Restart API after changing token

### **Issue 3: Credentials Error**

**Symptoms:**
- Error: "Invalid access token"
- Error: "Invalid phone number ID"

**Solutions:**
1. Verify token is current:
   - Tokens expire after 60 days
   - Get new one from Meta if needed
2. Check phone number ID:
   - Should be numbers only (e.g., "123456789")
   - Found in Meta → WhatsApp → Phone Numbers
3. Ensure no extra spaces in credentials

### **Issue 4: Messages Not Saving**

**Symptoms:**
- Command works but activity not recorded
- Database error in logs

**Solutions:**
1. Check database connection string
2. Verify user is mapped to company
3. Ensure dog ID exists
4. Check database logs for errors

---

## 📚 Best Practices

### **Security**
- [ ] Never share AccessToken
- [ ] Rotate webhook token periodically
- [ ] Use environment variables in production
- [ ] Keep appsettings.json out of version control
- [ ] Use HTTPS only (never HTTP)

### **Operations**
- [ ] Monitor bot logs daily
- [ ] Set up alerts for webhook failures
- [ ] Keep Meta credentials updated
- [ ] Test new features before deployment
- [ ] Document custom commands

### **User Communication**
- [ ] Send "help" message to new users
- [ ] Provide quick reference card
- [ ] Schedule regular reminders
- [ ] Collect feedback on bot features

### **Maintenance**
```
Daily:
  - Check webhook logs
  - Monitor for errors
  
Weekly:
  - Review message volume
  - Check token expiry (if needed)
  
Monthly:
  - Test all commands
  - Update documentation
  - Review user feedback
```

---

## 🎯 Feature Reference

### **All Available Commands**

```
COMMAND         USAGE                    RESPONSE
────────────────────────────────────────────────────
help            help                     Show command menu
dogs            dogs                     List all dogs
view            view [dogId]             Dog details + vaccines + family
detail          detail [dogId]           Same as view
add             add [name] [breed]       Register new dog
record          record [id] [activity]   Log care/vaccine/mating
vaccine         vaccine                  Dogs needing vaccines
breeding        breeding                 Breeding status & litters
status          status                   Dashboard overview
```

### **Activity Types for Record Command**
```
feeding, vaccine, illness, injured, bite, 
heat, mating, birth, other
```

### **Example Commands**
```
view 1
detail 2
record 1 feeding Good appetite
record 2 vaccine Rabies booster
record 3 mating Mated with dog #5
vaccine
breeding
status
add Buddy Labrador
```

---

## 🚀 Deployment Checklist

Before going live:

- [ ] API endpoint implemented and tested
- [ ] WhatsApp credentials obtained from Meta
- [ ] appsettings.json updated with credentials
- [ ] Webhook URL configured in Meta
- [ ] Webhook verification successful
- [ ] Test messages working
- [ ] Error logging configured
- [ ] Database connection verified
- [ ] Security review completed
- [ ] User documentation ready
- [ ] Support team trained

---

## 📞 Support Workflow

### **If Something Goes Wrong**

1. **Check Logs**
   ```
   Location: Dog.Api/logs/whatsapp.log
   Look for: "Error", "Failed", "Exception"
   ```

2. **Test Webhook**
   ```
   curl -X POST https://your-api/api/WhatsApp/webhook \
   -H "Content-Type: application/json" \
   -d '{"entry":[{"changes":[{"value":{"messages":[{"from":"1234567890","text":{"body":"test"}}]}}]}]}'
   ```

3. **Verify Credentials**
   ```
   - Check AccessToken in Meta (Settings → Tokens)
   - Verify PhoneNumberId in Meta (WhatsApp → Phone Numbers)
   - Confirm WebhookToken matches
   ```

4. **Restart Service**
   ```
   Stop API → Update config → Restart API
   ```

---

## 📊 Monitoring & Analytics

### **Key Metrics to Track**
- Messages received per day
- Commands used (most popular)
- Error rate
- Response time
- User retention

### **Logging Setup**
```csharp
// Already configured:
private readonly ILogger<WhatsAppBotService> _logger;

// Logs include:
// - Incoming messages
// - Command processing
// - Errors and exceptions
// - API responses
```

---

## ✨ Future Enhancements

Possible features to add:
- [ ] Photo/media sharing
- [ ] Automated reminders
- [ ] Two-way media (upload dog photos)
- [ ] Multi-language support
- [ ] Custom command scripting
- [ ] Message templates
- [ ] Bulk messaging

---

## 🎓 Training Guide for Team

### **For Business Users**
1. Share WHATSAPP_BOT_GUIDE.md
2. Send practice commands to test number
3. Create quick reference poster
4. Schedule weekly training

### **For Technical Support**
1. Share this document
2. Share troubleshooting guide
3. Set up log monitoring
4. Create runbook for common issues

### **For Mobile Users**
1. Save bot number in contacts as "🐾 Dog Bot"
2. Share guide via email
3. Provide quick reference card
4. Weekly tips on Slack

---

## ✅ SUCCESS CRITERIA

After following this process, you should be able to:

- ✅ Send "help" to bot and receive menu
- ✅ Use "dogs" to list all dogs
- ✅ Use "view [id]" to see dog details
- ✅ Record activities via "record" command
- ✅ Check vaccines via "vaccine" command
- ✅ Monitor breeding via "breeding" command
- ✅ Receive error-free responses
- ✅ Access bot 24/7/365

---

**🎉 WhatsApp Bot is Now Live!**

Your team can now manage dogs and breeding operations via WhatsApp messages.

For questions or issues, refer to the troubleshooting section or check API logs.

---

*Last Updated: 2026-07-18*
*Build Status: ✅ Complete (0 Errors)*
