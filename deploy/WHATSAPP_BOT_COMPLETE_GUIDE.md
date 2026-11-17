# 🤖 WhatsApp Bot - Complete User & Deployment Guide

## 📋 Table of Contents

1. [Overview](#overview)
2. [Features](#features)
3. [How to Use](#how-to-use)
4. [Deployment Guide](#deployment-guide)
5. [Configuration](#configuration)
6. [Troubleshooting](#troubleshooting)
7. [Maintenance](#maintenance)

---

## 🎯 Overview

The Dog Section WhatsApp Bot is an interactive system that allows users to manage dogs, users, branches, mating records, and family trees directly through WhatsApp messages. No special app required—just WhatsApp!

**Key Benefits:**
- ✅ Access dog management on-the-go
- ✅ Interactive menu navigation
- ✅ Create new users via WhatsApp
- ✅ View complete dog profiles
- ✅ Track mating records and family trees
- ✅ No additional software needed

---

## 🚀 Features

### **1. Interactive Menu System**
- Hierarchical navigation
- Session-based state tracking
- Back/forward navigation
- Quick access via numbers

### **2. Branch Management**
- View all branches
- See dogs assigned to branches
- View users in branches
- Branch details and statistics

### **3. User Management**
- List all users
- View user details
- See dogs assigned to users
- Track user activities

### **4. Dog Management**
- View all dogs
- Complete dog profiles (info, vaccines, assignments)
- Dog family trees (sire, dam, siblings, offspring)
- Vaccination tracking

### **5. Breeding Management**
- View active and historical matings
- Track mating dates and status
- Monitor expected puppies

### **6. User Creation** ⭐ NEW
- 7-step interactive form
- Role selection from 6 roles
- Branch selection from 5 branches
- Validation at each step
- Summary confirmation

### **7. Logout**
- Secure session clearing
- Resets all form data
- One-command logout

---

## 📱 How to Use

### **Getting Started**

**Step 1: Get the Bot Number**
- Ask your administrator for the WhatsApp bot number
- Save it in your contacts (e.g., "🐾 Dog Bot")

**Step 2: Send First Message**
- Open WhatsApp
- Message the bot number with any text
- Bot responds with main menu

### **Main Menu Options**

```
🐾 DOG MANAGEMENT SYSTEM

1️⃣  View Branches
2️⃣  View Dogs
3️⃣  View Users & Handlers
4️⃣  View Mating Records
5️⃣  View Dog Family Tree
6️⃣  Create New User
7️⃣  Show Help
8️⃣  Logout

Reply with number (1-8)
```

---

## 🏢 Using Branches

### **View All Branches**
```
User: 1
Bot:  Shows all branches list
User: 1
Bot:  Shows Branch #1 menu
```

**Branch Menu Options:**
- 1️⃣ View branch details
- 2️⃣ View dogs in branch
- 3️⃣ View users in branch
- 4️⃣ Back to menu

**Example Output:**
```
🏢 BRANCH #1 DETAILS

Name: Head Office - Main Branch
Type: Head Office
Location: Central Area
Manager: John Smith
Users: 5
Dogs: 12
Active Matings: 2
```

---

## 🐕 Using Dogs

### **View Dogs**
```
User: 2
Bot:  Shows dogs menu
User: 1
Bot:  Lists all dogs
User: [dog-id]
Bot:  Shows dog detail menu
```

**Dog Detail Options:**
- 1️⃣ View dog info
- 2️⃣ View vaccinations
- 3️⃣ View assignments
- 4️⃣ View family tree
- 5️⃣ Back to menu

**Example Dog Info:**
```
🐕 DOG #1 INFORMATION

Name: Buddy
Breed: Labrador Retriever
Gender: Male
DOB: 2020-03-15 (4 years old)
Microchip: 981000123456789900
Color: Golden

Status: Active ✅
Owner: John Smith
Handler: Sarah Johnson
Branch: Main Office
```

### **View Vaccinations**
```
💉 DOG #1 VACCINATIONS

✅ Rabies - 2024-12-10 (Due: 2025-12-10)
✅ DHPP - 2024-12-10 (Due: 2025-12-10)
✅ Bordetella - 2024-12-10
⚠️  Leptospirosis - Due in 15 days

Last Vaccination: Dec 10, 2024
Next Due: Jan 10, 2025
```

### **View Family Tree**
```
👨‍👩‍👧‍👦 DOG #1 FAMILY TREE

👨 Father (Sire):
   Champion Duke (ID: 5)
   └─ Labrador | Active | 6 offspring

👩 Mother (Dam):
   Lady Luna (ID: 8)
   └─ Labrador | Active | 12 offspring

👶 Siblings: 6
👪 Offspring: 3
```

---

## 👥 Managing Users

### **View Users**
```
User: 3
Bot:  Shows users menu
User: 1
Bot:  Lists all users
User: [user-id]
Bot:  Shows user detail menu
```

**User Detail Options:**
- 1️⃣ View user details
- 2️⃣ View dogs assigned
- 3️⃣ View user's activities
- 4️⃣ Back to menu

**Example User Info:**
```
👤 USER #1 DETAILS

Name: John Smith
Role: Manager
Branch: Head Office
Phone: +1234567890
Email: john@dogsection.com
Status: Active ✅

Dogs Assigned: 3
  1. Buddy (Labrador)
  2. Luna (Golden Retriever)
  3. Max (German Shepherd)
```

---

## ➕ Creating a New User

### **Start User Creation**
```
User: 6
Bot:  Shows Step 1 form
```

### **Step-by-Step Process**

**Step 1: First Name**
```
👤 CREATE NEW USER
Step 1 of 7: Enter first name
Example: John
```
→ User types: `John`

**Step 2: Last Name**
```
Step 2 of 7: Enter last name
Example: Doe
```
→ User types: `Doe`

**Step 3: Username**
```
Step 3 of 7: Enter username
Example: johndoe
```
→ User types: `johndoe`

**Step 4: Email**
```
Step 4 of 7: Enter email address
Example: john@example.com
```
→ User types: `john@example.com`

**Step 5: Phone (Optional)**
```
Step 5 of 7: Enter phone number (optional)
Example: +1 234 567 8900
Type phone or type skip to continue
```
→ User types: `skip` (or phone number)

**Step 6: Select Role** ⭐
```
Step 6 of 7: Select role

1️⃣  Administrator
2️⃣  Manager
3️⃣  Handler
4️⃣  Veterinarian
5️⃣  Breeder
6️⃣  Support Staff

Reply with role number (1-6)
```
→ User types: `3` (Handler)

**Step 7: Select Branch** ⭐
```
Step 7 of 7: Select branch / company

1️⃣  Head Office - Main Branch
2️⃣  North Branch - Handler Training
3️⃣  South Branch - Breeding Center
4️⃣  East Branch - Clinic
5️⃣  West Branch - Storage

Reply with branch number (1-5)
```
→ User types: `2` (North Branch)

**Step 8: Confirmation**
```
✅ USER CREATION SUMMARY

👤 Name: John Doe
👤 Username: johndoe
📧 Email: john@example.com
📞 Phone: (not provided)
🔐 Role: Handler
🏢 Branch: North Branch - Handler Training

Note: A temporary password will be sent via email.

Reply YES to confirm or NO to cancel
```
→ User types: `yes`

**Result:**
```
✅ USER CREATED SUCCESSFULLY

User Details:
• Name: John Doe
• Username: johndoe
• Email: john@example.com
• Role: Handler
• Branch: North Branch - Handler Training

A temporary password has been sent to the email address.

Returning to main menu...
```

---

## ❤️ Viewing Mating Records

### **Access Mating Records**
```
User: 4
Bot:  Shows mating menu
User: 1
Bot:  Lists all matings
```

**Example Output:**
```
❤️ MATING RECORDS

🟢 Active Matings (1):
  1. Buddy (M) × Luna (F)
     └─ Date: 2024-11-20 | Status: Confirmed
     └─ Expected puppies: Dec 20, 2024

🟡 Past Matings (3):
  2. Max (M) × Princess (F) - Completed
  3. Rex (M) × Daisy (F) - Completed
  4. Champion (M) × Queen (F) - Completed
```

---

## 👨‍👩‍👧‍👦 Viewing Family Trees

### **Access Family Trees**
```
User: 5
Bot:  Shows family tree menu
User: 1
Bot:  Shows multi-generation family tree
```

**Example Output:**
```
👨‍👩‍👧‍👦 DOG FAMILY TREES

Generation 1 (Founders):
  1. Champion Max (ID: 5) - 15 offspring
  2. Lady Luna (ID: 8) - 12 offspring

Generation 2 (Current):
  3. Buddy (ID: 1) - Son of Max & Luna
  4. Princess (ID: 2) - Daughter of Max & Luna
```

---

## 🚪 Logout

### **Log Out Safely**
```
User: 8
Bot:  Confirms logout

or

User: logout
Bot:  Confirms logout
```

**What Happens:**
- ✅ Session cleared
- ✅ All form data erased
- ✅ Resets to login state
- ✅ Can log back in anytime

---

## 🔄 Navigation Tips

| Command | Action |
|---------|--------|
| `back` | Return to previous menu |
| `menu` | Go to main menu |
| `main` | Go to main menu |
| `0` | Go to main menu |
| `exit` | Go to main menu |
| `quit` | Go to main menu |
| `logout` | Logout and clear session |
| `help` | Show help menu |
| Numbers `1-8` | Select menu option |

---

---

# 🚀 DEPLOYMENT GUIDE

## ⚠️ Prerequisites

Before deployment, ensure you have:

- ✅ Meta/Facebook Business Account
- ✅ WhatsApp Business API access
- ✅ Business phone number verified
- ✅ API server (Dog.Api running)
- ✅ HTTPS certificate (required by Meta)
- ✅ Access to appsettings.json
- ✅ Access to DNS/domain settings

**Estimated Time:** 1-2 hours

---

## 📋 Pre-Deployment Checklist

- [ ] API is running on production server
- [ ] HTTPS certificate is valid
- [ ] Database is accessible from API
- [ ] Meta credentials obtained
- [ ] WhatsApp phone number ready
- [ ] Webhook URL planned
- [ ] Team trained on usage

---

## 🔧 Step 1: Get Meta Credentials

### **1.1 Create Meta Business Account**

1. Go to https://business.facebook.com
2. Click "Create Account"
3. Enter:
   - Business Name: "Dog Section"
   - Your Name
   - Business Email
   - Phone Number
4. Click "Create Account"

### **1.2 Get WhatsApp Business API Access**

1. Log into Meta Business Suite
2. Click "Settings" → "Apps and websites"
3. Click "Add Apps"
4. Search for "WhatsApp"
5. Click "Set Up" on WhatsApp Business Platform
6. Follow setup wizard

### **1.3 Create Business Phone Number**

1. In Meta Business Suite
2. Go to "WhatsApp" → "Phone Numbers"
3. Click "Add Number"
4. Choose existing or request new number
5. Verify phone number (via SMS code)
6. **Save:** Phone Number ID

### **1.4 Generate Access Token**

1. Go to "Settings" → "User Tokens"
2. Click "Generate Token"
3. Select WhatsApp Business app
4. Set expiration (90 days recommended)
5. Copy and save the token
6. **Save:** AccessToken

### **1.5 Create Webhook Token**

1. Choose a secure random string
   ```
   Example: your_super_secret_webhook_token_123
   ```
2. **Save:** WebhookToken (you'll need this)

---

## ⚙️ Step 2: Configure API Server

### **2.1 Update appsettings.json**

Navigate to your production server and update:
```
Dog.Api/appsettings.json
```

Add or update the WhatsApp section:

```json
{
  "WhatsApp": {
    "AccessToken": "YOUR_ACCESS_TOKEN_HERE",
    "PhoneNumberId": "YOUR_PHONE_ID_HERE",
    "WebhookToken": "YOUR_WEBHOOK_VERIFY_TOKEN"
  }
}
```

**Important:** Replace with actual values from Step 1

### **2.2 Set Environment Variables (Recommended)**

**For better security, use environment variables instead:**

**Windows Server:**
```powershell
# Run as Administrator
$env:WhatsApp__AccessToken = "your_token_here"
$env:WhatsApp__PhoneNumberId = "your_phone_id_here"
$env:WhatsApp__WebhookToken = "your_webhook_token_here"
```

**Linux/Docker:**
```bash
export WhatsApp__AccessToken="your_token_here"
export WhatsApp__PhoneNumberId="your_phone_id_here"
export WhatsApp__WebhookToken="your_webhook_token_here"
```

### **2.3 Restart API Service**

**If running as Windows Service:**
```powershell
# Stop service
net stop "DogSectionAPI"

# Start service
net start "DogSectionAPI"
```

**If running directly:**
1. Stop the running process
2. Restart with updated configuration

---

## 🔌 Step 3: Configure Webhook in Meta

### **3.1 Get Your Webhook URL**

Your webhook URL should be:
```
https://your-api-domain.com/api/WhatsApp/webhook
```

**Requirements:**
- Must use HTTPS (not HTTP)
- Must be publicly accessible
- Must be resolvable via DNS

### **3.2 Set Webhook in Meta Dashboard**

1. Log into Meta Business Suite
2. Go to "Settings" → "WhatsApp" → "Configuration"
3. Scroll to "Webhooks"
4. Click "Edit Subscriptions"
5. Enter:
   - **Callback URL:** `https://your-api-domain.com/api/WhatsApp/webhook`
   - **Verify Token:** (the WebhookToken from appsettings.json)
6. Click "Verify and Save"

### **3.3 Subscribe to Webhooks**

Still in "Edit Subscriptions", check these fields:
- ✅ `messages`
- ✅ `message_template_status_update`
- ✅ `message_template_quality_update`

Click "Save"

---

## ✅ Step 4: Verify Setup

### **4.1 Test Webhook Connection**

```bash
# From your terminal/command line:
curl -X GET "https://your-api-domain.com/api/WhatsApp/webhook?mode=subscribe&challenge=test123&verify_token=YOUR_WEBHOOK_TOKEN"
```

Expected response: `test123`

If you get an error, check:
- Webhook URL is correct
- API is running
- Firewall allows incoming traffic
- Webhook token matches exactly

### **4.2 Send Test Message**

1. Open WhatsApp
2. Message your business WhatsApp number with: `help`
3. Bot should respond with menu

**If bot doesn't respond:**
- Check API logs for errors
- Verify credentials in appsettings.json
- Ensure webhook is verified in Meta
- Wait 5 minutes and try again

### **4.3 Manual Testing**

Test all major features:

```
Test 1: Menu
Send: help
Expect: Menu appears

Test 2: Branches
Send: 1
Expect: Branches menu

Test 3: Dogs
Send: 2
Expect: Dogs menu

Test 4: Users
Send: 3
Expect: Users menu

Test 5: Create User
Send: 6
Expect: User creation step 1

Test 6: Logout
Send: 8
Expect: Logout confirmation
```

---

## 📊 Step 5: Monitor & Maintain

### **5.1 Check Logs**

**Windows:**
```powershell
# View recent API logs
Get-Content "C:\path\to\logs\whatsapp.log" -Tail 100
```

**Linux:**
```bash
# View recent API logs
tail -100 /var/log/dogsection/whatsapp.log
```

### **5.2 Monitor Key Metrics**

Daily:
- ✅ API is running
- ✅ "help" command works
- ✅ No error logs

Weekly:
- ✅ Test all commands
- ✅ Verify credentials not expired
- ✅ Check message volume

Monthly:
- ✅ Review error logs
- ✅ Rotate tokens if needed
- ✅ Update documentation

### **5.3 Set Up Alerts**

Configure alerts for:
- API down/crashes
- Webhook failures (100+ errors/hour)
- Database connection errors
- Authentication failures

---

## 🔐 Security Best Practices

### **Do's:**
✅ Use HTTPS only (never HTTP)
✅ Store credentials in environment variables
✅ Rotate tokens every 90 days
✅ Monitor webhook logs
✅ Restrict API access to trusted IPs
✅ Use strong webhook tokens (32+ chars)
✅ Enable HTTPS certificate auto-renewal

### **Don'ts:**
❌ Never share access tokens
❌ Don't commit credentials to git
❌ Don't use HTTP webhooks
❌ Don't log full credentials
❌ Don't expose token in URLs
❌ Don't use simple webhook tokens

---

## 🛠️ Troubleshooting

### **Problem: "Webhook verification failed"**

**Solution:**
1. Verify token matches exactly (case-sensitive)
2. Check for extra spaces
3. Confirm API is running
4. Test with curl command
5. Restart API service

### **Problem: "Invalid access token"**

**Solution:**
1. Get new token from Meta (tokens expire)
2. Verify token format (no extra spaces)
3. Ensure correct phone number ID
4. Restart API after updating

### **Problem: Bot doesn't respond**

**Solution:**
1. Verify webhook URL is correct in Meta
2. Check API logs for errors
3. Test webhook with curl
4. Verify firewall allows incoming traffic
5. Confirm credentials in appsettings.json

### **Problem: "Phone number not verified"**

**Solution:**
1. Go to Meta WhatsApp settings
2. Re-verify phone number
3. Check SMS code wasn't expired
4. Try verification again

---

## 📈 Scaling & Optimization

### **For High Volume**

- Implement message queuing (Azure Service Bus, RabbitMQ)
- Add caching for frequently accessed data
- Use CDN for any static content
- Monitor database query performance
- Set up load balancing

### **Performance Optimization**

```
Current Setup:
- Message processing: <2 seconds
- Data fetching: <3 seconds
- Total response time: <5 seconds

Optimization:
- Cache branch/role lists (hourly refresh)
- Implement session timeout (15 mins)
- Archive old mating records
- Index frequently queried fields
```

---

## 📞 Support & Escalation

### **If Issues Persist**

1. **Collect Information:**
   - Full error message
   - Exact command that failed
   - API logs (last 50 lines)
   - WhatsApp number used
   - Time issue occurred

2. **Check These Resources:**
   - Meta WhatsApp API docs
   - API logs for errors
   - Database connection status
   - Webhook delivery logs in Meta

3. **Contact Support:**
   - Include error logs
   - Describe reproduction steps
   - Share appsettings structure (no values)
   - Provide timestamps

---

## ✅ Deployment Checklist

Before going live:

- [ ] All credentials obtained from Meta
- [ ] appsettings.json configured
- [ ] Environment variables set (if using)
- [ ] API service running
- [ ] HTTPS certificate valid
- [ ] Webhook URL configured in Meta
- [ ] Webhook verified successfully
- [ ] Test message received
- [ ] All 6 commands tested
- [ ] Logs monitored for errors
- [ ] Team trained on usage
- [ ] Backup and recovery plan documented
- [ ] Monitoring/alerts configured
- [ ] Security checklist completed

---

## 📅 Post-Deployment Tasks

### **Week 1**
- Monitor messages hourly
- Check for errors daily
- Verify all features work
- Gather user feedback

### **Month 1**
- Review usage patterns
- Optimize slow queries
- Document common issues
- Train additional users

### **Ongoing**
- Monitor performance
- Rotate credentials monthly
- Keep documentation updated
- Gather feedback for improvements

---

## 🎉 Success Criteria

Your deployment is successful when:

✅ Users can send "help" and receive menu
✅ All 8 menu options work correctly
✅ User creation flow completes successfully
✅ Role and branch selection display properly
✅ Logout clears session correctly
✅ Response times under 5 seconds
✅ No errors in logs for 24 hours
✅ Users report positive experience

---

## 📚 Additional Resources

- **Meta WhatsApp API Docs:** https://developers.facebook.com/docs/whatsapp
- **Setup Process:** WHATSAPP_SETUP_PROCESS.md
- **User Guide:** WHATSAPP_BOT_GUIDE.md
- **Interactive Menu:** WHATSAPP_INTERACTIVE_MENU.md
- **Troubleshooting:** WHATSAPP_QUICK_TROUBLESHOOT.md

---

## 🎓 User Training

### **Admin/Manager Training (30 min)**
1. Overview of all 8 menu options
2. Managing users via WhatsApp
3. Monitoring bot activity
4. Troubleshooting basic issues

### **Handler Training (15 min)**
1. How to start bot conversation
2. View assigned dogs
3. View user profile
4. Navigation basics

### **New User Setup (5 min)**
1. Save bot number in contacts
2. Send first message
3. Navigate to needed section
4. Bookmark help screen

---

## 📞 Support Contact

For deployment or usage issues:
- Contact: [Your Admin Email]
- Hours: Business hours
- Response Time: 24 hours
- Escalation: [Your Manager]

---

**🎉 Congratulations! Your WhatsApp Bot is now live!**

Users can now manage the entire dog breeding operation directly from WhatsApp.

---

*Last Updated: 2026-07-18*
*Version: 1.0*
*Build Status: ✅ Production Ready*
