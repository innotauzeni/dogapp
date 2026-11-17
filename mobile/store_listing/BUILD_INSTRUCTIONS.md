# Build Instructions for APKPure Release

## 🔧 Prerequisites

Before building the release APK, ensure you have:

- [ ] Flutter SDK installed (run `flutter doctor`)
- [ ] Android SDK installed
- [ ] Java JDK 17 or higher
- [ ] All dependencies updated (`flutter pub get`)
- [ ] App tested on real device or emulator
- [ ] No critical bugs or crashes
- [ ] Release signing key created

---

## 🔐 Step 1: Create Release Signing Key (First Time Only)

### Generate Keystore

```bash
# Navigate to android/app directory
cd mobile/android/app

# Generate keystore (run this once)
keytool -genkey -v -keystore black-eagle-release.jks -keyalg RSA -keysize 2048 -validity 10000 -alias black-eagle
```

**You will be prompted for:**
- Keystore password (create strong password)
- Key password (can be same as keystore password)
- First and last name: Telthemweb Solutions
- Organizational unit: Development
- Organization: Telthemweb Solutions Pvt Ltd
- City: Harare
- State: Harare
- Country code: ZW

**IMPORTANT:**
- Save the keystore file (`black-eagle-release.jks`) securely
- Save passwords securely (use password manager)
- **NEVER** commit keystore to Git
- **NEVER** share keystore publicly
- Keep backup of keystore (if lost, you can't update app)

### Store Credentials Securely

Create a file `android/key.properties` (Git-ignored):

```properties
storePassword=YOUR_KEYSTORE_PASSWORD
keyPassword=YOUR_KEY_PASSWORD
keyAlias=black-eagle
storeFile=black-eagle-release.jks
```

---

## ⚙️ Step 2: Configure Build for Release Signing

### Update `android/app/build.gradle.kts`

Replace the entire file with:

```kotlin
plugins {
    id("com.android.application")
    id("kotlin-android")
    id("dev.flutter.flutter-gradle-plugin")
}

// Load keystore properties
val keystorePropertiesFile = rootProject.file("key.properties")
val keystoreProperties = java.util.Properties()
if (keystorePropertiesFile.exists()) {
    keystoreProperties.load(java.io.FileInputStream(keystorePropertiesFile))
}

android {
    namespace = "com.blackeagle.black_eagle"
    compileSdk = 34

    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_17
        targetCompatibility = JavaVersion.VERSION_17
    }

    kotlinOptions {
        jvmTarget = JavaVersion.VERSION_17.toString()
    }

    defaultConfig {
        applicationId = "com.blackeagle.black_eagle"
        minSdk = 21
        targetSdk = 34
        versionCode = flutter.versionCode
        versionName = flutter.versionName
    }

    signingConfigs {
        create("release") {
            if (keystorePropertiesFile.exists()) {
                storeFile = file(keystoreProperties["storeFile"] as String)
                storePassword = keystoreProperties["storePassword"] as String
                keyAlias = keystoreProperties["keyAlias"] as String
                keyPassword = keystoreProperties["keyPassword"] as String
            }
        }
    }

    buildTypes {
        release {
            signingConfig = signingConfigs.getByName("release")
            
            // Enable code shrinking and obfuscation
            isMinifyEnabled = true
            isShrinkResources = true
            
            proguardFiles(
                getDefaultProguardFile("proguard-android-optimize.txt"),
                "proguard-rules.pro"
            )
        }
        
        debug {
            signingConfig = signingConfigs.getByName("debug")
        }
    }
}

flutter {
    source = "../.."
}

dependencies {
    // Add any additional dependencies here if needed
}
```

### Create ProGuard Rules (Optional but Recommended)

Create `android/app/proguard-rules.pro`:

```proguard
# Flutter wrapper
-keep class io.flutter.app.** { *; }
-keep class io.flutter.plugin.**  { *; }
-keep class io.flutter.util.**  { *; }
-keep class io.flutter.view.**  { *; }
-keep class io.flutter.**  { *; }
-keep class io.flutter.plugins.**  { *; }

# Keep application classes
-keep class com.blackeagle.black_eagle.** { *; }

# Gson (if used)
-keepattributes Signature
-keepattributes *Annotation*
-dontwarn sun.misc.**
-keep class com.google.gson.** { *; }

# Keep R class
-keepclassmembers class **.R$* {
    public static <fields>;
}
```

### Update `.gitignore`

Add to `android/.gitignore`:

```
# Keystore files - NEVER commit these!
*.jks
*.keystore
key.properties
```

---

## 🏗️ Step 3: Update Version Number

### Edit `pubspec.yaml`

```yaml
version: 1.0.0+1
# Format: MAJOR.MINOR.PATCH+BUILD_NUMBER
# Example: 1.0.0+1 means Version 1.0.0, Build 1
```

**Version Naming Convention:**
- **MAJOR**: Incompatible API changes or major redesign
- **MINOR**: New features, backwards-compatible
- **PATCH**: Bug fixes, minor improvements
- **BUILD_NUMBER**: Increment for each release (must always increase)

**Examples:**
- First release: `1.0.0+1`
- Bug fix: `1.0.1+2`
- New feature: `1.1.0+3`
- Major update: `2.0.0+4`

---

## 🧹 Step 4: Clean Previous Builds

```bash
cd mobile

# Clean previous builds
flutter clean

# Get dependencies
flutter pub get

# Verify no issues
flutter doctor -v
```

---

## 🔨 Step 5: Build Release APK

### Standard Release APK

```bash
flutter build apk --release
```

**Output location:**
```
mobile/build/app/outputs/flutter-apk/app-release.apk
```

### Build with Specific Options

```bash
# Build with verbose logging
flutter build apk --release --verbose

# Build with specific target platform
flutter build apk --release --target-platform android-arm64

# Build app bundle (AAB) for Google Play
flutter build appbundle --release

# Build split APKs by ABI (smaller size)
flutter build apk --split-per-abi --release
```

**Split APKs Output:**
- `app-armeabi-v7a-release.apk` (32-bit ARM)
- `app-arm64-v8a-release.apk` (64-bit ARM, most common)
- `app-x86_64-release.apk` (64-bit Intel, for emulators)

---

## 📦 Step 6: Verify APK

### Check APK Size

```bash
# Windows PowerShell
Get-ChildItem build\app\outputs\flutter-apk\app-release.apk | Select-Object Name, @{Name="SizeMB";Expression={[math]::Round($_.Length / 1MB, 2)}}

# Linux/Mac
ls -lh build/app/outputs/flutter-apk/app-release.apk
```

**Expected Size:** 15-50 MB (depends on assets and dependencies)  
**APKPure Limit:** 100 MB

### Install on Test Device

```bash
# Install via ADB
adb install build/app/outputs/flutter-apk/app-release.apk

# Or install manually:
# 1. Copy APK to device
# 2. Enable "Install from Unknown Sources"
# 3. Tap APK file to install
```

### Test Thoroughly

**Critical Tests:**
- [ ] App launches without crashes
- [ ] Login/authentication works
- [ ] All main features functional
- [ ] Camera permission works (dog photos)
- [ ] Storage permission works (certificates)
- [ ] WhatsApp notifications send
- [ ] PDF generation works
- [ ] Navigation flows correctly
- [ ] No crashes on back button
- [ ] App handles network errors gracefully
- [ ] Images load correctly
- [ ] Forms validate properly
- [ ] Data saves and persists

---

## 🔍 Step 7: Analyze APK (Optional)

### Using Android Studio

1. Open Android Studio
2. Build → Analyze APK
3. Select `app-release.apk`
4. Review:
   - APK size breakdown
   - Unused resources
   - Method count
   - Permissions

### Using Command Line

```bash
# Inspect APK contents
unzip -l build/app/outputs/flutter-apk/app-release.apk

# Check APK info
aapt dump badging build/app/outputs/flutter-apk/app-release.apk

# Verify signing
jarsigner -verify -verbose -certs build/app/outputs/flutter-apk/app-release.apk
```

---

## 📱 Step 8: Test on Multiple Devices

**Recommended Testing:**
- [ ] Real device (your daily phone)
- [ ] Emulator with different screen size
- [ ] Tablet (if supporting tablets)
- [ ] Old Android version (Android 5.0 / API 21)
- [ ] Latest Android version (Android 14+)
- [ ] Low-end device (2GB RAM)
- [ ] High-end device

---

## 📝 Step 9: Prepare Release Notes

Create `RELEASE_NOTES.txt`:

```
Version 1.0.0 - Initial Release

🎉 FEATURES
• Complete dog profile management with photos
• Health tracking: vaccinations, deworming, medical records
• Breeding management: heat cycles, mating, litter tracking
• Business operations: income, expenses, financial analytics
• Handler assignment and deployment tracking
• Daily care logging: feeding, training, grooming
• WhatsApp integration for instant notifications
• PDF certificate generation
• Multi-user support with role-based access
• Real-time data synchronization

🐕 DOG MANAGEMENT
• Add unlimited dogs with detailed profiles
• Track breed, color, microchip, registration numbers
• Photo galleries for each dog
• Age calculation from date of birth
• Active/inactive status management

💉 HEALTH & MEDICAL
• Vaccination schedule with reminders
• Deworming records and next-due dates
• Illness and injury reporting
• Treatment notes and outcomes
• Complete medical history

🔬 BREEDING
• Heat cycle tracking
• Mating records with partner information
• Pregnancy monitoring
• Litter management with puppy count
• Breeding analytics

📊 BUSINESS
• Income tracking from multiple sources
• Expense management by category
• Bank account integration
• Profit/loss calculations
• Financial reports and dashboards

🔔 NOTIFICATIONS
• WhatsApp notifications for all activities
• Real-time updates
• Customizable notification preferences

🔒 SECURITY
• Secure user authentication
• Role-based access control
• Encrypted data transmission
• Company/organization isolation

This is our first public release. We welcome your feedback!
Contact: support@dogsection.com
```

---

## ✅ Pre-Submission Checklist

Before uploading to APKPure:

### Technical
- [ ] APK builds successfully
- [ ] APK is signed with release key
- [ ] APK size under 100MB
- [ ] Version number is correct
- [ ] Package name is correct (com.blackeagle.black_eagle)
- [ ] minSdkVersion is 21 or higher
- [ ] targetSdkVersion is 33+ (latest)

### Testing
- [ ] Tested on real device
- [ ] No crashes or ANR (App Not Responding)
- [ ] All permissions work correctly
- [ ] Network requests succeed
- [ ] Offline functionality works
- [ ] Back button navigation correct
- [ ] No memory leaks
- [ ] Battery usage is reasonable

### Legal & Compliance
- [ ] Privacy policy created and hosted
- [ ] Terms of service prepared (if needed)
- [ ] Permissions are necessary and documented
- [ ] No copyright violations in assets
- [ ] App complies with store policies

### Assets
- [ ] App icon (512x512) ready
- [ ] Feature graphic (1024x500) ready
- [ ] Screenshots (2-8) captured and edited
- [ ] Descriptions written
- [ ] Release notes prepared

---

## 🚀 Step 10: Upload to APKPure

1. **Login:** https://developer.apkpure.com/
2. **Upload APK:** Click "Upload App" button
3. **Fill Details:** Use information from `APKPURE_SUBMISSION_GUIDE.md`
4. **Upload Graphics:** Icon, feature graphic, screenshots
5. **Add Descriptions:** Short and full descriptions
6. **Set Category:** Business / Productivity
7. **Privacy Policy:** Provide URL
8. **Review:** Double-check everything
9. **Submit:** Click "Submit for Review"
10. **Wait:** Usually 1-3 business days for approval

---

## 🔄 Future Updates

### Increment Version

For next release:

```yaml
# pubspec.yaml
version: 1.0.1+2  # Version 1.0.1, Build 2
```

### Build Update

```bash
flutter clean
flutter pub get
flutter build apk --release
```

### Upload Update

- Upload new APK with higher version code
- Update "What's New" with changelog
- APKPure will replace old version

---

## 🐛 Troubleshooting

### Build Fails

**Error: Gradle build failed**
```bash
cd android
./gradlew clean
cd ..
flutter clean
flutter pub get
flutter build apk --release --verbose
```

**Error: Keystore not found**
- Check `key.properties` path is correct
- Ensure `black-eagle-release.jks` exists
- Verify file path uses forward slashes

**Error: Out of memory**
```bash
# Increase Gradle memory
# Edit android/gradle.properties
org.gradle.jvmargs=-Xmx4096m -XX:MaxPermSize=1024m
```

### APK Won't Install

**Error: App not installed**
- Uninstall debug version first
- Check APK is not corrupted
- Enable "Install from Unknown Sources"
- Verify device has enough storage

**Error: INSTALL_FAILED_UPDATE_INCOMPATIBLE**
- Uninstall existing version
- Clear app data
- Try again

### APK Too Large

**Reduce size:**
- Use split APKs: `flutter build apk --split-per-abi`
- Enable obfuscation (already in config)
- Remove unused assets
- Compress images
- Use vector graphics instead of PNGs

---

## 📞 Support

**Build Issues:**
- Flutter Discord: https://discord.gg/flutter
- Stack Overflow: https://stackoverflow.com/questions/tagged/flutter

**APKPure Support:**
- Email: developer@apkpure.com
- Help: https://developer.apkpure.com/help

**Black Eagle Support:**
- Email: support@dogsection.com
- Website: www.dogsection.com

---

## 📚 Additional Resources

- **Flutter Build Docs:** https://docs.flutter.dev/deployment/android
- **App Signing:** https://developer.android.com/studio/publish/app-signing
- **ProGuard:** https://www.guardsquare.com/manual/home
- **APKPure Guidelines:** https://developer.apkpure.com/guidelines

---

**Good luck with your build! 🚀**

Last Updated: September 7, 2026
