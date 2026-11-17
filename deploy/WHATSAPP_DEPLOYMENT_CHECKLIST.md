# 🚀 WhatsApp Bot Deployment Checklist

**Project:** Dog Section WhatsApp Bot  
**Date:** _______________  
**Deployed By:** _______________  
**Approval:** _______________

---

## 📋 PRE-DEPLOYMENT (Week Before)

### Infrastructure Preparation
- [ ] Server capacity verified (CPU, RAM, disk space)
- [ ] HTTPS certificate installed and valid
- [ ] DNS domain configured and resolving
- [ ] Firewall rules updated for incoming traffic
- [ ] Database backup scheduled
- [ ] Rollback plan documented

### Team Preparation
- [ ] Team briefed on deployment
- [ ] Support team trained on troubleshooting
- [ ] User documentation ready to share
- [ ] FAQ document prepared
- [ ] Escalation contacts identified

### Credential Preparation
- [ ] Meta Business Account created
- [ ] WhatsApp Business API access approved
- [ ] Business phone number obtained
- [ ] Phone number verified with SMS code
- [ ] AccessToken generated and saved securely: `_______________________`
- [ ] PhoneNumberId noted: `_______________________`
- [ ] **WebhookToken created (ANY custom string you want):** `_______________________`
  - ℹ️ Can be ANY name/password you choose (e.g., MyDogBotToken2024)
  - Must be 8+ characters for security
  - Example: DogSection_WebhookSecret_ABC123

---

## 🔧 DEPLOYMENT DAY

### Step 1: Configuration
**Time: 15 minutes**

**Server Configuration:**
- [ ] appsettings.json updated with credentials
  - [ ] AccessToken (from Meta): `_______________________`
  - [ ] PhoneNumberId (from Meta): `_______________________`
  - [ ] WebhookToken (YOUR custom string): `_______________________`
    - ℹ️ This is any custom name/password YOU create
    - Must match EXACTLY in Meta dashboard
- [ ] Environment variables set (recommended for security)
- [ ] Credentials verified:
  - [ ] No typos, no extra spaces
  - [ ] WebhookToken matches in both appsettings.json AND Meta dashboard
- [ ] Old credentials backed up

**File Changes:**
- [ ] Dog.Api/Program.cs reviewed
- [ ] Dog.Api/Controllers/WhatsAppController.cs deployed
- [ ] Dog.Api/Services/WhatsAppBotService.cs deployed

### Step 2: Service Deployment
**Time: 10 minutes**

**API Deployment:**
- [ ] API service stopped cleanly
- [ ] New code deployed
- [ ] Database migrations run (if needed)
- [ ] API service started
- [ ] Service status verified (running)

**Verification:**
- [ ] API is accessible via HTTPS
- [ ] Logs show no startup errors
- [ ] Database connection confirmed
- [ ] Port is correct and accessible

### Step 3: Webhook Configuration
**Time: 20 minutes**

**Meta Dashboard Setup:**
- [ ] Logged into Meta Business Suite
- [ ] Webhook URL entered: `https://___________/api/WhatsApp/webhook`
- [ ] Verify Token entered (matches WebhookToken exactly)
- [ ] Webhook verified successfully (test passed)
- [ ] Subscriptions enabled:
  - [ ] messages ✓
  - [ ] message_template_status_update ✓
  - [ ] message_template_quality_update ✓

**Verification Tests:**
```bash
[ ] Curl test: 
curl -X GET "https://[domain]/api/WhatsApp/webhook?mode=subscribe&challenge=test123&verify_token=[token]"
Result: test123 ✓

[ ] Response time: _____ ms (should be <1 second)
```

### Step 4: Initial Testing
**Time: 30 minutes**

**Manual Tests:**

| Test | Command | Expected | Result | ✓/✗ |
|------|---------|----------|--------|-----|
| Start | Any text | Menu appears | | |
| Help | help | Help menu | | |
| Branches | 1 | Branches menu | | |
| Dogs | 2 | Dogs menu | | |
| Users | 3 | Users menu | | |
| Matings | 4 | Matings menu | | |
| Family | 5 | Family menu | | |
| Create User | 6 | Step 1 form | | |
| Logout | 8 | Confirmation | | |

**Performance Checks:**
- [ ] Response time <2 sec (normal)
- [ ] Response time <5 sec (acceptable)
- [ ] No timeout errors
- [ ] No database errors
- [ ] No API errors

**Error Checks:**
- [ ] No errors in API logs
- [ ] No errors in WhatsApp webhook logs
- [ ] All 9 commands working
- [ ] Navigation working correctly

### Step 5: Advanced Testing
**Time: 30 minutes**

**Feature Tests:**

**Branches Feature:**
- [ ] List branches works
- [ ] View branch details works
- [ ] View dogs in branch works
- [ ] View users in branch works

**Dogs Feature:**
- [ ] List all dogs works
- [ ] View dog details works
- [ ] View dog vaccinations works
- [ ] View dog assignments works
- [ ] View dog family tree works

**Users Feature:**
- [ ] List all users works
- [ ] View user details works
- [ ] View user's dogs works
- [ ] View user activities works

**User Creation Feature:**
- [ ] Step 1 (first name) works
- [ ] Step 2 (last name) works
- [ ] Step 3 (username) works
- [ ] Step 4 (email) works
- [ ] Step 5 (phone optional) works
- [ ] Step 6 (role selection) works
- [ ] Step 7 (branch selection) works
- [ ] Step 8 (confirmation) works
- [ ] Confirmation "yes" creates user
- [ ] Confirmation "no" cancels creation

**Logout Feature:**
- [ ] Logout command works
- [ ] Session cleared
- [ ] Form data erased
- [ ] Back/forward navigation works

**Session Handling:**
- [ ] Session persists across messages
- [ ] Back navigation works
- [ ] Menu command works
- [ ] Logout clears session

---

## 📊 POST-DEPLOYMENT

### First 24 Hours
**Monitoring Tasks:**

- [ ] Monitor logs every 2 hours
- [ ] Check error count: `_____` (should be 0)
- [ ] Monitor message volume: `_____` messages
- [ ] Response times: avg `_____` ms
- [ ] No timeout errors
- [ ] Database connections healthy

**Support Tasks:**
- [ ] Support team on standby
- [ ] Emergency contact list accessible
- [ ] Escalation procedure tested
- [ ] Rollback plan verified ready

### First Week
**Daily Tasks:**

- [ ] Day 1: Monitor closely, test all features
- [ ] Day 2: Check user feedback
- [ ] Day 3: Review error patterns
- [ ] Day 4: Verify token expiration not approaching
- [ ] Day 5: Run full feature test
- [ ] Day 6-7: Monitor for issues

**End of Week Report:**
```
Total Messages Processed: _______
Errors Encountered: _______
Features Tested: 9/9 ✓
User Feedback: _______________________
Issues Found: _______________________
```

### First Month
**Weekly Tasks:**

- [ ] Review logs and metrics
- [ ] Test all commands once
- [ ] Verify credentials not expired
- [ ] Check database performance
- [ ] Document any issues found
- [ ] Update runbooks if needed

---

## 🔐 Security Verification

### Credentials
- [ ] AccessToken stored securely (env var, not file)
- [ ] WebhookToken stored securely
- [ ] No credentials in git repo
- [ ] No credentials in logs
- [ ] Credentials rotated every 90 days ✓

### API Security
- [ ] HTTPS enforced (no HTTP)
- [ ] Webhook validation enabled
- [ ] API logs don't contain sensitive data
- [ ] Database connection encrypted
- [ ] API accessible only via HTTPS

### Access Control
- [ ] Only authorized IPs can access (if applicable)
- [ ] Rate limiting configured
- [ ] Session timeout set (15 minutes)
- [ ] Logout properly clears data

---

## ✅ Success Criteria

**Deployment is successful when ALL are true:**

✅ API running without errors  
✅ All 9 commands return correct responses  
✅ Response time <5 seconds  
✅ Webhook verified in Meta  
✅ User creation flow completes  
✅ Session management working  
✅ Logout clears session  
✅ No errors in logs (24 hours)  
✅ Support team trained  
✅ Documentation shared with users  

**Current Status: [ ] Ready / [ ] In Progress / [ ] Issues Found**

---

## ⚠️ Issues Found During Deployment

| Issue | Severity | Cause | Resolution | Status |
|-------|----------|-------|-----------|--------|
| | | | | |
| | | | | |
| | | | | |

---

## 🔄 Rollback Decision

**IF Critical Issues Found:**

- [ ] Stop API service
- [ ] Restore previous configuration
- [ ] Restore previous code
- [ ] Restart API service
- [ ] Verify rollback successful
- [ ] Document issue for investigation

**Rollback Reason:** ___________________  
**Rolled Back By:** ___________________  
**Time:** ___________________  

---

## 📝 Sign-Off

**Deployment Completed:**
- [ ] All tests passed
- [ ] No critical issues
- [ ] Users notified
- [ ] Support team briefed
- [ ] Monitoring activated

**Approved For Production:**

| Role | Name | Signature | Date |
|------|------|-----------|------|
| Technical Lead | | | |
| Manager | | | |
| Admin | | | |

---

## 📞 Emergency Contacts

| Role | Name | Phone | Email |
|------|------|-------|-------|
| On-Call Support | | | |
| Technical Lead | | | |
| Manager | | | |
| Meta Support | | | [Meta Support Email] |

---

## 📅 Follow-Up Tasks

**After Deployment:**

- [ ] Schedule weekly review (every Monday)
- [ ] Monitor token expiration (90 days out)
- [ ] Plan monthly feature updates
- [ ] Gather user feedback monthly
- [ ] Performance optimization review (quarterly)

**Scheduled Reviews:**
- [ ] Day 7: `_______________`
- [ ] Day 30: `_______________`
- [ ] Day 90: `_______________`
- [ ] Token Rotation: `_______________`

---

## 📊 Metrics to Track

**Daily:**
```
Messages Processed: ______
Errors: ______
Avg Response Time: ______ ms
Uptime: ______ %
```

**Weekly:**
```
Active Users: ______
Features Used:
  - Branches: ______
  - Dogs: ______
  - Users: ______
  - Matings: ______
  - User Creation: ______
```

---

**Deployment Completed: [ ] Yes [ ] No**

**Date:** _______________  
**Time:** _______________  
**Deployed By:** _______________  
**Verified By:** _______________  

---

*Print and keep this checklist for future deployments*
