# APKPure Submission Quick Checklist

## ✅ Before You Start (One-Time Setup)

- [ ] Create release keystore (`keytool -genkey ...`)
- [ ] Save keystore password securely
- [ ] Create `android/key.properties` file
- [ ] Update `android/app/build.gradle.kts` for signing
- [ ] Add keystore files to `.gitignore`
- [ ] Host privacy policy at public URL

---

## 📱 Build Release APK

- [ ] Update version in `pubspec.yaml` (e.g., `1.0.0+1`)
- [ ] Run `flutter clean`
- [ ] Run `flutter pub get`
- [ ] Run `flutter build apk --release`
- [ ] Locate APK: `build/app/outputs/flutter-apk/app-release.apk`
- [ ] Verify APK size is under 100MB
- [ ] Install APK on test device
- [ ] Test all major features thoroughly
- [ ] Confirm no crashes or bugs

---

## 🎨 Prepare Graphics

- [ ] App Icon: 512x512 PNG (`assets/icon.png`)
- [ ] Feature Graphic: 1024x500 PNG/JPG (create new)
- [ ] Screenshots: 2-8 images at 1080x1920 (capture from app)
- [ ] Ensure all images are high quality
- [ ] Remove sensitive data from screenshots
- [ ] Verify graphics meet requirements

---

## 📝 Write Descriptions

- [ ] **App Name:** Black Eagle - Dog Management System
- [ ] **Short Description** (80 chars):
  ```
  Professional dog kennel management: breeding, health, training & business tracking
  ```
- [ ] **Full Description** (copy from `APKPURE_SUBMISSION_GUIDE.md`)
- [ ] **What's New** / Release Notes:
  ```
  Version 1.0.0 - Initial Release
  
  Features:
  • Complete dog profile management
  • Health tracking (vaccinations, deworming, medical records)
  • Breeding management and litter tracking
  • Business operations (income, expenses, analytics)
  • Handler assignment and deployment tracking
  • Daily care logging (feeding, training, grooming)
  • WhatsApp notifications
  • PDF certificate generation
  • Financial analytics and reports
  • Multi-user support with role-based access
  ```

---

## 📋 App Details

- [ ] **Package Name:** com.blackeagle.black_eagle (auto-filled from APK)
- [ ] **Category:** Business / Productivity
- [ ] **Content Rating:** Everyone
- [ ] **Price:** Free
- [ ] **Tags:** dog management, kennel, breeding, business, veterinary, dog care, handler, K9

---

## 🔒 Privacy & Legal

- [ ] **Privacy Policy URL:** https://www.dogsection.com/privacy
- [ ] Ensure privacy policy is publicly accessible
- [ ] **Contains Ads:** No (unless you have ads)
- [ ] **In-App Purchases:** No (unless you have IAP)
- [ ] **Target Audience:** Adults / Professional use
- [ ] **Sensitive Permissions Explanation:**
  - Camera: For capturing dog photos
  - Storage: For saving PDF certificates and photos

---

## 👤 Developer Information

- [ ] **Developer Name:** Telthemweb Solutions Pvt Ltd
- [ ] **Email:** support@dogsection.com
- [ ] **Website:** https://www.dogsection.com
- [ ] **Phone:** +263 77 3512690 (optional)

---

## 🚀 Upload & Submit

- [ ] Login to https://developer.apkpure.com/
- [ ] Click "Upload App"
- [ ] Upload APK file
- [ ] Upload app icon (512x512)
- [ ] Upload feature graphic (1024x500)
- [ ] Upload screenshots (2-8 images)
- [ ] Fill in all required fields
- [ ] Add app descriptions
- [ ] Provide privacy policy URL
- [ ] Add developer contact information
- [ ] Review all information carefully
- [ ] Click "Submit for Review"

---

## ⏱️ After Submission

- [ ] Check email for confirmation
- [ ] Monitor for approval (usually 1-3 days)
- [ ] If rejected, read feedback and fix issues
- [ ] Once approved, test download link
- [ ] Share app link with team/users
- [ ] Monitor reviews and ratings
- [ ] Plan future updates

---

## 📞 Emergency Contacts

**APKPure Support:**
- Email: developer@apkpure.com

**Your Support:**
- Email: support@dogsection.com
- Website: www.dogsection.com

---

## 🎯 Quick Commands

```bash
# Build release APK
cd mobile
flutter clean && flutter pub get && flutter build apk --release

# Check APK size (PowerShell)
Get-ChildItem build\app\outputs\flutter-apk\app-release.apk | Select-Object Name, @{Name="SizeMB";Expression={[math]::Round($_.Length / 1MB, 2)}}

# Install on device
adb install build/app/outputs/flutter-apk/app-release.apk

# Capture screenshot from device
adb shell screencap -p /sdcard/screenshot.png
adb pull /sdcard/screenshot.png
```

---

## 📁 Files You Need

1. **APK File:** `build/app/outputs/flutter-apk/app-release.apk`
2. **App Icon:** `assets/icon.png` (512x512)
3. **Feature Graphic:** Create 1024x500 image
4. **Screenshots:** 2-8 images at 1080x1920
5. **Privacy Policy:** Hosted at public URL
6. **Descriptions:** From `APKPURE_SUBMISSION_GUIDE.md`

---

## ✨ Pro Tips

1. **Double-check version number** - must be higher than previous release
2. **Test on real device** - emulator doesn't catch all issues
3. **Use consistent branding** - same colors, style across all graphics
4. **Clear screenshots** - show actual functionality, not empty states
5. **Proofread descriptions** - spelling and grammar matter
6. **Monitor email** - APKPure sends updates to your developer email
7. **Be patient** - review takes 1-3 days, sometimes longer
8. **Plan updates** - prepare for user feedback and bug fixes

---

## 🚨 Common Mistakes to Avoid

- ❌ Using debug APK instead of release APK
- ❌ Forgetting to update version number
- ❌ Privacy policy not publicly accessible
- ❌ Low-quality or blurry screenshots
- ❌ App name too long or generic
- ❌ Missing required permissions explanation
- ❌ Feature graphic with wrong dimensions
- ❌ APK not signed properly
- ❌ Sensitive data visible in screenshots
- ❌ Incomplete or misleading description

---

## 🎉 Success Criteria

Your submission is ready when:
- ✅ APK builds and installs without errors
- ✅ All features work as expected
- ✅ All graphics meet size requirements
- ✅ Descriptions are complete and accurate
- ✅ Privacy policy is accessible
- ✅ No crashes during testing
- ✅ Version number is correct
- ✅ Contact information is valid

---

**You're ready to submit! Good luck! 🚀**

---

Last Updated: September 7, 2026
Version: 1.0
