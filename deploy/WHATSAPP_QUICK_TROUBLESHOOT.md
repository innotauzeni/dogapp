# 🚨 WhatsApp Bot - Quick Troubleshooting Guide

## Quick Fixes (Try First)

### 1. **Bot Not Responding**
```
❌ Problem: Send message, no response
✅ Fix: 
   1. Restart API service
   2. Verify appsettings.json has credentials
   3. Check firewall allows incoming connections
   4. Send "help" again
```

### 2. **"Webhook verification failed" in Meta**
```
❌ Problem: Meta says webhook can't be verified
✅ Fix:
   1. Copy exact WebhookToken from appsettings.json
   2. Paste into Meta webhook verification token field
   3. Make sure there are NO extra spaces
   4. Click "Verify" again
```

### 3. **"Invalid access token" Error**
```
❌ Problem: Error in logs about invalid token
✅ Fix:
   1. Get new token from Meta Business Suite
   2. Replace in appsettings.json
   3. Restart API
   4. Test with "help" command
```

### 4. **Nothing Appears in Bot Logs**
```
❌ Problem: Messages sent but no logs
✅ Fix:
   1. Verify webhook URL is correct in Meta
   2. Check URL uses HTTPS (not HTTP)
   3. Run: curl test to verify endpoint responds
   4. Restart API after any config change
```

---

## Check These First

### **Configuration Checklist**
- [ ] AccessToken is NOT empty
- [ ] PhoneNumberId is NOT empty
- [ ] WebhookToken is NOT empty
- [ ] appsettings.json is valid JSON
- [ ] No extra spaces in values
- [ ] API service is running
- [ ] Port is accessible (8080, 443, etc.)

### **Meta Configuration Checklist**
- [ ] Business Account created
- [ ] WhatsApp Business API access approved
- [ ] Phone number verified
- [ ] Webhook URL entered (HTTPS only)
- [ ] Verify Token matches appsettings.json exactly
- [ ] Message webhooks subscribed (✅ messages)
- [ ] Phone number not blocked

---

## Testing Commands

### **Test 1: Is API Running?**
```bash
curl https://your-api-domain.com/api/WhatsApp/webhook?mode=subscribe&challenge=test123&verify_token=YOUR_TOKEN

Expected: test123 (echo challenge back)
```

### **Test 2: Can Bot Send Messages?**
```bash
curl -X POST https://your-api-domain.com/api/WhatsApp/send \
  -H "Content-Type: application/json" \
  -d '{
    "phoneNumber": "YOUR_PHONE_NUMBER",
    "message": "Test message from API"
  }'

Expected: {"success": true}
```

### **Test 3: Send Message in WhatsApp**
```
Text to bot number: help
Expected: Menu appears with all commands
```

---

## Common Error Messages

### **"Webhook could not be verified"**
| Cause | Solution |
|-------|----------|
| Wrong verify token | Copy exact token from appsettings.json |
| URL not HTTPS | Use HTTPS only, not HTTP |
| API not running | Start the API service |
| Firewall blocking | Allow incoming traffic on port |

### **"Invalid access token"**
| Cause | Solution |
|-------|----------|
| Token expired | Get new one from Meta |
| Token is wrong | Verify in Meta Business Suite |
| Spaces in token | Remove extra spaces |

### **"Invalid phone number ID"**
| Cause | Solution |
|-------|----------|
| Wrong ID | Check Meta → WhatsApp → Phone Numbers |
| Phone not verified | Verify number in Meta |
| ID has spaces | Remove spaces |

### **"Message not sent"**
| Cause | Solution |
|-------|----------|
| Invalid recipient number | Verify phone number format |
| Rate limiting | Wait 5 minutes, try again |
| Phone banned | Check Meta for violations |

---

## Command Fails/Returns Error

### **Scenario 1: "dogs" shows error**
```
Check:
1. Database connection (appsettings.json)
2. User has access to company
3. Dogs exist in database
4. Run: SELECT * FROM ka_dogs LIMIT 1;
```

### **Scenario 2: "view 1" shows "not found"**
```
Check:
1. Dog ID 1 exists: SELECT * FROM ka_dogs WHERE Id = 1;
2. User's company is correct
3. Dog isn't deleted (check IsActive)
```

### **Scenario 3: "record 1 feeding" fails**
```
Check:
1. Dog ID 1 exists
2. Activity type is valid (feeding, vaccine, etc.)
3. Database write permission
4. Check logs for database error
```

---

## Step-by-Step Debugging

### **Level 1: Quick Checks (2 min)**
```
[ ] Is API running? (check logs)
[ ] Do credentials exist in appsettings.json?
[ ] Is Meta webhook URL correct?
[ ] Send "help" — does bot respond?
```

### **Level 2: Verify Setup (5 min)**
```
[ ] Run webhook verification test
[ ] Check API logs for errors
[ ] Verify database connection
[ ] Test manual send via API endpoint
```

### **Level 3: Deep Dive (10 min)**
```
[ ] Check firewall/network
[ ] Review detailed API logs
[ ] Verify database permissions
[ ] Test with different commands
```

### **Level 4: Escalate (if needed)**
```
If still failing after Level 3:
1. Contact Meta support (for credentials/webhook issues)
2. Check API logs for specific error codes
3. Verify database schema hasn't changed
4. Check API version compatibility
```

---

## Recovery Procedures

### **If Bot Stops Responding**
```
1. Stop API service
2. Check error logs for why it crashed
3. Fix the issue (usually DB connection or config)
4. Restart API
5. Test with "help" command
6. Monitor logs for 5 minutes
```

### **If Webhook Breaks**
```
1. Check webhook URL still exists and is HTTPS
2. Verify firewall still allows traffic
3. Check Meta dashboard for webhook errors
4. Re-verify webhook in Meta settings
5. Test bot message
```

### **If Credentials Expired**
```
1. Go to Meta Business Suite
2. Get new AccessToken
3. Update appsettings.json
4. Restart API
5. Verify webhook still works
```

---

## Performance Issues

### **Bot Responds Slowly (>5 sec)**
```
Check:
1. API load/CPU usage
2. Database query performance
3. Network latency
4. Message size (keep <4KB)

Fix:
1. Optimize database queries
2. Add caching for dogs list
3. Reduce data in responses
```

### **High Error Rate**
```
Check:
1. API error logs
2. Database connection pool
3. API memory usage
4. Network timeouts

Fix:
1. Restart API
2. Check database resources
3. Increase timeout values
```

---

## Monitoring Setup

### **Daily Checks**
```
Every morning:
1. Check if API is running
2. Send "help" — confirm response
3. Send "dogs" — confirm list
4. Send "vaccine" — confirm alerts work
```

### **Weekly Checks**
```
Every week:
1. Review error logs
2. Check message volume
3. Verify credentials still valid
4. Test all 9 commands
```

### **Alert Triggers**
```
Set alerts for:
1. API down (no heartbeat)
2. Webhook errors (100+ in 1 hour)
3. Database errors (any database connection error)
4. Rate limiting (Meta responding with 429)
```

---

## Test Commands Checklist

```
Send these to verify everything works:

[ ] help          → Shows menu
[ ] dogs          → Shows dog list
[ ] view 1        → Shows dog details
[ ] detail 1      → Shows dog details
[ ] record 1 feeding hello   → Logs activity
[ ] vaccine       → Shows due vaccines
[ ] breeding      → Shows breeding status
[ ] status        → Shows dashboard
[ ] add Test Labrador  → Adds dog
```

---

## When to Restart

```
After changing:
- Appsettings.json ✅ RESTART REQUIRED
- Environment variables ✅ RESTART REQUIRED
- Database schema ✅ RESTART REQUIRED
- Code (if deployed) ✅ RESTART REQUIRED

Not required for:
- Meta webhook settings (takes effect immediately)
- Database data (queries latest automatically)
```

---

## Getting Help

### **Still Stuck?**

1. **Collect Information**
   ```
   1. Full error message (from logs or Meta)
   2. Command that failed (exact text sent)
   3. What should have happened (expected result)
   4. API log excerpt around error time
   5. Credentials (DO NOT share actual tokens!)
   ```

2. **Check Logs**
   ```
   Location: Dog.Api/logs/whatsapp.log
   
   Useful commands:
   tail -f logs/whatsapp.log        (watch live)
   grep "Error" logs/whatsapp.log   (find errors)
   grep "webhook" logs/whatsapp.log (webhook issues)
   ```

3. **Contact Support**
   ```
   Include:
   - Error message
   - Steps to reproduce
   - Screenshots of issue
   - API logs from time of error
   ```

---

## Quick Reference - Most Common Fixes

| Issue | Fix | Time |
|-------|-----|------|
| No response | Restart API | 30s |
| Wrong token | Copy from Meta | 1m |
| HTTPS error | Check webhook URL | 2m |
| DB error | Check connection | 3m |
| Dog not found | Verify ID exists | 2m |
| Webhook fails | Re-verify in Meta | 3m |

---

## Status Check Command

```bash
# Run this to check overall system health
curl -X GET "https://your-api-domain.com/api/WhatsApp/webhook?mode=subscribe&challenge=health&verify_token=YOUR_TOKEN"

Expected output: health

This confirms:
✅ API is running
✅ HTTPS works
✅ Webhook token correct
✅ Endpoint accessible
```

---

**Print this guide and keep it near your desk!** 📋

Questions? Check WHATSAPP_SETUP_PROCESS.md for detailed documentation.

---

*Last Updated: 2026-07-18*
*Build: ✅ Production Ready*
