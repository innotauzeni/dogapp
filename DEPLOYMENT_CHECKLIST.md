# WhatsApp Notifications - Deployment Checklist

## ✅ Pre-Deployment Checklist

### 1. **Files to Deploy**

Deploy these projects (they reference each other):

```
✓ Dog.Api/          (Contains WhatsAppNotificationService implementation)
✓ Dog.Application/  (Contains IWhatsAppNotificationService interface and modified handlers)
✓ Dog.Core/         (Entity definitions - no changes but referenced)
✓ Dog.Infrastructure/ (Database context - no changes but referenced)
```

### 2. **Modified Files Summary**

**New Files:**
- ✓ `Dog.Api/Services/WhatsAppNotificationService.cs`
- ✓ `Dog.Application/Contracts/IWhatsAppNotificationService.cs`

**Modified Files:**
- ✓ `Dog.Api/Program.cs` (Added service registration)
- ✓ `Dog.Application/Features/Animals/Handlers/AnimalHandlers.cs` (Added notification calls)

### 3. **Configuration Check**

Ensure your **production** `appsettings.json` has:

```json
{
  "WhatsApp": {
    "AccessToken": "EAAUKKNjfG0ABR5ZA5TLA5N7V0ScIZBJCzA9XgHxEtdc9fpP0",
    "PhoneNumberId": "1264452783409286",
    "WebhookToken": "dogerp_wa_verify_2026"
  },
  "NotifyWhatsapp": {
    "WhatsAppNumbers": "263773512690,263774914150,263772539121,263785764731"
  }
}
```

⚠️ **IMPORTANT:** Make sure these are your PRODUCTION credentials, not development ones!

### 4. **Build Commands**

```bash
# Clean previous builds
dotnet clean

# Restore dependencies
dotnet restore

# Build in Release mode
dotnet build --configuration Release

# Publish the API
dotnet publish Dog.Api/Dog.Api.csproj --configuration Release --output ./publish
```

### 5. **Deployment Steps**

#### Option A: Manual Deployment
1. Build the solution in Release mode
2. Stop the API service on your server
3. Copy the published files to your server
4. Update `appsettings.json` with production values
5. Restart the API service

#### Option B: Using your existing deployment script
1. Commit changes to git
2. Push to your repository
3. Your CI/CD should handle the rest (if configured)

#### Option C: GitHub Actions (if using .github/workflows/deploy.yml)
1. Commit and push changes
2. GitHub Actions will automatically deploy

## 🧪 Post-Deployment Testing

### Test 1: Dog Creation (via Web Portal)
```
1. Log into web portal
2. Navigate to "Add New Dog"
3. Fill in dog details
4. Submit
5. ✓ Check if WhatsApp notification arrives at all 4 numbers
```

### Test 2: Dog Feeding (via Mobile App)
```
1. Open mobile app
2. Select a dog
3. Record feeding activity
4. Submit
5. ✓ Check if WhatsApp notification arrives at all 4 numbers
```

### Test 3: Vaccination (via WhatsApp Bot)
```
1. Send vaccination command to WhatsApp bot
2. Complete the vaccination record
3. ✓ Check if notification arrives at all 4 numbers
```

### Test 4: Multiple Activities
```
Test each activity type:
- ✓ Feeding
- ✓ Vaccination
- ✓ Mating
- ✓ Illness
- ✓ Injury
- ✓ Bite incident
- ✓ Deworming
- ✓ Litter birth

Each should send a notification!
```

## 🔍 Troubleshooting

### Issue: Notifications not being sent

**Check 1: Configuration**
```bash
# Verify appsettings.json has correct values
cat appsettings.json | grep -A 5 "NotifyWhatsapp"
```

**Check 2: WhatsApp Credentials**
```bash
# Verify WhatsApp section exists
cat appsettings.json | grep -A 5 "WhatsApp"
```

**Check 3: Service Registration**
Look for this line in Program.cs:
```csharp
builder.Services.AddScoped<Dog.Api.Services.IWhatsAppNotificationService, Dog.Api.Services.WhatsAppNotificationService>();
```

**Check 4: Logs**
Check application logs for:
- "📤 SendMessage called" - indicates notification attempted
- "✅ WhatsApp message sent successfully" - indicates success
- "❌ WhatsApp message send failed" - indicates failure with details

### Issue: Compilation errors

**Fix 1: Restore packages**
```bash
dotnet restore
```

**Fix 2: Clean and rebuild**
```bash
dotnet clean
dotnet build
```

**Fix 3: Check namespace references**
Make sure `Dog.Application.Contracts` is referenced in both:
- Dog.Api project
- Dog.Application project

### Issue: Notifications sent but not received

**Check 1: Phone numbers format**
- Must include country code (263 for Zimbabwe)
- No spaces or special characters
- Example: `263773512690` ✓
- NOT: `+263 77 351 2690` ✗

**Check 2: WhatsApp Business API status**
- Verify your WhatsApp Business API account is active
- Check Meta Business Suite for any restrictions

**Check 3: Rate limits**
- Meta has rate limits on WhatsApp messages
- If sending many notifications quickly, some may be throttled

## 🚀 Deployment Command Summary

```bash
# 1. Navigate to project directory
cd "c:\Users\Admin\Documents\Visual Studio 18\l\csharpdata\Dog"

# 2. Clean and restore
dotnet clean
dotnet restore

# 3. Build
dotnet build --configuration Release

# 4. Run tests (if you have any)
dotnet test

# 5. Publish API
dotnet publish Dog.Api/Dog.Api.csproj --configuration Release --output ./publish

# 6. The ./publish folder now contains everything needed for deployment
```

## 📋 Server Deployment Steps

### If using systemd (Linux):
```bash
# 1. Stop the service
sudo systemctl stop dog-api

# 2. Backup current version
sudo cp -r /var/www/dog-api /var/www/dog-api.backup

# 3. Copy new files
sudo cp -r ./publish/* /var/www/dog-api/

# 4. Update appsettings.json
sudo nano /var/www/dog-api/appsettings.json

# 5. Set permissions
sudo chown -R www-data:www-data /var/www/dog-api

# 6. Start the service
sudo systemctl start dog-api

# 7. Check status
sudo systemctl status dog-api

# 8. Check logs
sudo journalctl -u dog-api -f
```

### If using IIS (Windows):
```powershell
# 1. Stop IIS site
Stop-Website -Name "DogAPI"

# 2. Backup current version
Copy-Item "C:\inetpub\wwwroot\DogAPI" "C:\inetpub\wwwroot\DogAPI.backup" -Recurse

# 3. Copy new files
Copy-Item ".\publish\*" "C:\inetpub\wwwroot\DogAPI\" -Recurse -Force

# 4. Update appsettings.json
notepad "C:\inetpub\wwwroot\DogAPI\appsettings.json"

# 5. Start IIS site
Start-Website -Name "DogAPI"
```

## ✅ Verification Checklist

After deployment, verify:

- [ ] API starts without errors
- [ ] Existing functionality still works (mobile app, web portal, WhatsApp bot)
- [ ] Can add a new dog (test from any platform)
- [ ] WhatsApp notification received for dog creation
- [ ] Can record feeding (test from any platform)
- [ ] WhatsApp notification received for feeding
- [ ] All 4 numbers receive notifications
- [ ] Notification messages are readable and formatted correctly
- [ ] Check logs for any errors

## 🎉 Success Criteria

**Deployment is successful when:**
1. ✅ API starts without errors
2. ✅ All existing features work (no breaking changes)
3. ✅ Adding a dog sends WhatsApp notification
4. ✅ Recording activities sends WhatsApp notifications
5. ✅ All 4 configured numbers receive messages
6. ✅ Messages are formatted correctly and readable

## 📞 Support Numbers Receiving Notifications

After successful deployment, these numbers will receive notifications:
- 263773512690
- 263774914150
- 263772539121
- 263785764731

## 🔄 Rollback Plan (If Issues Occur)

If something goes wrong:

```bash
# Linux (systemd)
sudo systemctl stop dog-api
sudo rm -rf /var/www/dog-api
sudo mv /var/www/dog-api.backup /var/www/dog-api
sudo systemctl start dog-api

# Windows (IIS)
Stop-Website -Name "DogAPI"
Remove-Item "C:\inetpub\wwwroot\DogAPI" -Recurse -Force
Rename-Item "C:\inetpub\wwwroot\DogAPI.backup" "DogAPI"
Start-Website -Name "DogAPI"
```

## 📝 Notes

- Notification failures will NOT break the application
- If WhatsApp credentials are missing, warnings are logged but app continues
- If notification numbers are not configured, notifications are skipped silently
- Each platform (mobile, web, WhatsApp) will trigger notifications independently
- No changes needed to mobile app or web portal - they work automatically!
