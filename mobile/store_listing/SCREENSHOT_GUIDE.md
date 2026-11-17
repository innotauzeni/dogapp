# Screenshot Capture Guide for APKPure

## 📸 Requirements

- **Minimum:** 2 screenshots
- **Recommended:** 4-8 screenshots
- **Dimensions:** 1080 x 1920 pixels (portrait) minimum
- **Format:** PNG or JPG
- **File Size:** Under 8MB each
- **Orientation:** Portrait (vertical) recommended

---

## 📱 Recommended Screens to Capture

### 1. Dashboard (Home Screen) - **REQUIRED**
**Purpose:** Show overview and first impression

**What to display:**
- Total dogs count
- Active dogs
- Deployed dogs
- Financial summary (income, expenses)
- Recent activities
- Quick action buttons

**Why:** Shows comprehensive functionality at a glance

---

### 2. Dog Profile/Details - **REQUIRED**
**Purpose:** Core feature showcase

**What to display:**
- Dog photo (use attractive dog image)
- Name, breed, registration number
- Key information (gender, DOB, microchip)
- Health status indicators
- Action buttons (Edit, Certificate, etc.)
- Tabs: Details, Health, Breeding, Care Records

**Why:** Most important feature of the app

---

### 3. Health Tracking - **HIGHLY RECOMMENDED**
**Purpose:** Medical management capability

**What to display:**
- Vaccination list with dates
- Next due dates
- Deworming records
- Medical history timeline
- Status badges (Vaccinated, Due Soon, Overdue)

**Why:** Critical feature for dog management

---

### 4. Dog List with Multiple Dogs
**Purpose:** Shows scale and organization

**What to display:**
- List of 5-8 dogs
- Dog avatars/photos
- Quick info (breed, status, age)
- Search bar
- Filter/sort options
- Add button

**Why:** Demonstrates ability to manage multiple dogs

---

### 5. Breeding Management (Optional)
**Purpose:** Specialized functionality

**What to display:**
- Mating records
- Heat cycle tracking
- Pregnancy/litter information
- Partner dog details

**Why:** Unique feature for professional breeders

---

### 6. Business Analytics (Optional)
**Purpose:** Business value proposition

**What to display:**
- Financial charts (bar, line graphs)
- Income vs Expenses
- Profit/Loss summary
- Date range selector
- KPI cards

**Why:** Appeals to business-minded users

---

### 7. Care Record Entry (Optional)
**Purpose:** Daily operations ease-of-use

**What to display:**
- Form to add care record
- Activity type selector (Feeding, Training, etc.)
- Date/time picker
- Notes field
- Photo upload option
- Save button

**Why:** Shows user-friendly interface

---

### 8. Certificate/PDF Preview (Optional)
**Purpose:** Professional output

**What to display:**
- Generated PDF certificate
- Dog registration details
- Company branding
- Professional formatting

**Why:** Demonstrates professional-grade features

---

## 🎬 Step-by-Step Capture Process

### Option A: Real Android Device (Best Quality)

#### Step 1: Prepare Demo Data

Before capturing, ensure you have:
- At least 3-5 dogs with photos added
- Vaccination records for showcase
- Some financial data (income/expenses)
- Care records (feeding, training logs)
- Use realistic names, not test data

**Pro Tip:** Use attractive dog photos (from Unsplash/Pexels) for better visual appeal

#### Step 2: Clean Device UI

- Set time to **10:00 AM** (looks professional)
- Charge battery to **100%** or at least 90%
- Enable Wi-Fi icon (disable mobile data)
- Clear all notifications
- Close background apps
- Use light theme (or dark if app supports)

#### Step 3: Capture Screenshots

**On Android:**
1. Navigate to screen you want to capture
2. Press **Power button + Volume Down** simultaneously
3. Hold for 1 second until you see animation
4. Screenshot saved to `/Pictures/Screenshots/`

**Tips:**
- Take multiple shots of each screen (choose best later)
- Ensure no loading spinners visible
- Show populated data, not empty states
- Avoid error messages or warnings

#### Step 4: Transfer to PC

**Via USB:**
```bash
# Connect device via USB
# Enable File Transfer mode
# Navigate to Internal Storage/Pictures/Screenshots/
# Copy files to PC
```

**Via Cloud:**
- Upload to Google Photos
- Download to PC
- Or use Google Drive, Dropbox

---

### Option B: Android Emulator

#### Step 1: Set Up Emulator

```bash
# Open Android Studio
# Tools → Device Manager → Create Virtual Device

# Recommended device: Pixel 5
# System Image: Android 12 (API 31) or higher
# RAM: 2GB minimum
# Internal Storage: 2GB
```

#### Step 2: Configure Emulator

- Resolution: 1080 x 2340 (or similar 9:16 ratio)
- Density: 440 dpi
- Time: Set to 10:00 AM
- Battery: Full
- Network: Wi-Fi enabled

#### Step 3: Run App

```bash
flutter run
# App will launch in emulator
```

#### Step 4: Capture Screenshots

**Method 1: Emulator Toolbar**
- Click camera icon in emulator toolbar (right side)
- Screenshot saved to desktop or documents folder

**Method 2: Keyboard Shortcut**
- Windows: **Ctrl + S**
- Mac: **Cmd + S**

**Method 3: Manual**
- Right-click emulator
- "Save screenshot"
- Choose location

---

### Option C: ADB Command (Pro Method)

```bash
# Connect device via USB with USB debugging enabled

# Capture screenshot
adb shell screencap -p /sdcard/screenshot.png

# Pull to PC
adb pull /sdcard/screenshot.png ./screenshot.png

# Or combine in one command (PowerShell):
adb shell screencap -p | Out-File -Encoding Default screenshot.png
```

**Batch capture script (PowerShell):**

```powershell
# Create script: capture-screenshots.ps1

$screens = @("dashboard", "dog-profile", "health", "dog-list", "breeding", "analytics")

foreach ($screen in $screens) {
    Write-Host "Capturing $screen - Navigate to the screen and press Enter"
    Read-Host
    
    adb shell screencap -p /sdcard/$screen.png
    adb pull /sdcard/$screen.png ./$screen.png
    adb shell rm /sdcard/$screen.png
    
    Write-Host "$screen captured successfully!"
    Start-Sleep -Seconds 1
}

Write-Host "All screenshots captured!"
```

**Usage:**
```bash
# Run Flutter app on device
flutter run

# In another terminal:
.\capture-screenshots.ps1

# Follow prompts to navigate and capture
```

---

## ✂️ Editing Screenshots

### Recommended Tools

1. **Windows:**
   - Snipping Tool (built-in)
   - Paint 3D (built-in)
   - GIMP (free)
   - Photoshop (professional)

2. **Online:**
   - Photopea (https://photopea.com) - Free Photoshop alternative
   - Canva (https://canva.com) - Easy editing
   - Pixlr (https://pixlr.com) - Quick edits

3. **Mobile:**
   - Snapseed (Google, free)
   - Adobe Photoshop Express (free)

### Basic Editing Steps

#### 1. Crop/Resize (if needed)

**Ensure 9:16 aspect ratio (1080 x 1920)**

```bash
# Using ImageMagick (command line)
magick convert input.png -resize 1080x1920^ -gravity center -extent 1080x1920 output.png
```

**In GIMP:**
1. Open screenshot
2. Image → Scale Image
3. Width: 1080, Height: 1920
4. Interpolation: Cubic (best quality)
5. Scale

#### 2. Remove Sensitive Data (if any)

- Blur out phone numbers
- Hide email addresses
- Anonymize personal names (use demo names instead)
- Cover sensitive financial amounts (if needed)

**In GIMP:**
1. Select area with Rectangle Select tool
2. Filters → Blur → Pixelize or Gaussian Blur
3. Or use paintbrush tool to cover

#### 3. Enhance Quality

**Brightness/Contrast:**
- Slightly increase brightness if screen is dark
- Increase contrast for better visibility
- Don't overdo it (keep natural look)

**In GIMP:**
- Colors → Brightness-Contrast
- Brightness: +5 to +10
- Contrast: +5 to +15

**Sharpen (optional):**
- Filters → Enhance → Sharpen (Unsharp Mask)
- Amount: 0.5-1.0
- Radius: 1.0

#### 4. Add Device Frame (optional)

**Using MockUPhone:**
1. Go to https://mockuphone.com
2. Choose device: Google Pixel 5 or similar
3. Upload your screenshot
4. Download result

**Pros:** Looks professional
**Cons:** Reduces visible area, APKPure may not require

---

## 🎨 Screenshot Optimization

### Compress for Web

**Before upload:**
- Original: 2-5 MB per screenshot
- Optimized: 500 KB - 1 MB per screenshot

**Using TinyPNG:**
1. Go to https://tinypng.com
2. Upload screenshots (up to 20 at once)
3. Download compressed versions
4. Quality loss minimal, size reduced 50-70%

**Using ImageMagick:**
```bash
# Compress PNG
magick convert input.png -quality 85 output.png

# Convert to JPG (smaller size)
magick convert input.png -quality 90 output.jpg
```

### Batch Processing

**PowerShell script to optimize all screenshots:**

```powershell
# optimize-screenshots.ps1

$screenshots = Get-ChildItem -Path . -Filter *.png

foreach ($screenshot in $screenshots) {
    $output = $screenshot.Name -replace '.png', '-optimized.png'
    
    # Using ImageMagick
    magick convert $screenshot.FullName -resize 1080x1920^ -quality 85 $output
    
    Write-Host "Optimized: $output"
}

Write-Host "All screenshots optimized!"
```

---

## 📋 Screenshot Checklist

Before finalizing:

### Technical
- [ ] Resolution is 1080 x 1920 (or higher)
- [ ] Aspect ratio is 9:16 (portrait)
- [ ] Format is PNG or JPG
- [ ] File size under 8MB (ideally under 1MB)
- [ ] Image is sharp, not blurry
- [ ] No visible compression artifacts

### Content
- [ ] Shows actual app functionality
- [ ] No loading spinners or errors
- [ ] Data looks realistic (not obviously test data)
- [ ] No sensitive information visible
- [ ] Status bar looks clean (time, battery, signal)
- [ ] No notification banners visible

### Aesthetics
- [ ] Good visual appeal
- [ ] Colors are vibrant and true
- [ ] Text is readable at thumbnail size
- [ ] Layout looks balanced
- [ ] Dog photos (if any) are attractive
- [ ] No UI glitches or overlaps

### Branding
- [ ] App branding visible (logo, colors)
- [ ] Consistent look across all screenshots
- [ ] Professional appearance
- [ ] Matches app icon style

---

## 🎯 Screenshot Sequence (Recommended Order)

1. **Dashboard** - Start with overview
2. **Dog Profile** - Core functionality
3. **Health Tracking** - Key feature
4. **Dog List** - Organization capability
5. **Business Analytics** - Value proposition
6. **Breeding Management** - Specialized feature
7. **Care Record Form** - Ease of use
8. **Certificate Preview** - Professional output

**Logic:** General → Specific → Advanced

---

## 💡 Pro Tips for Great Screenshots

### 1. Use Real-Looking Data
- Dog names: Max, Bella, Charlie, Luna, Cooper
- Breeds: German Shepherd, Labrador, Golden Retriever
- Realistic dates and numbers
- Actual-looking notes and descriptions

### 2. Show Success States
- ✅ "Record saved successfully"
- ✅ Checkmarks and green indicators
- ✅ Completed tasks
- ❌ Avoid error messages or warnings

### 3. Populate Lists
- Show 3-5 items minimum in lists
- Don't show empty states
- Use varied data (different breeds, dates)
- Include profile pictures for dogs

### 4. Timing Matters
- Capture at 10:00 AM (psychological: start of workday)
- Battery 100% or high (shows quality app doesn't drain)
- Full Wi-Fi signal (shows connectivity)

### 5. Consistent Theming
- Use same theme (light or dark) across all screenshots
- Keep status bar style consistent
- Same navigation bar across screens

### 6. Highlight Key Features
- If possible, subtly highlight important UI elements
- Use app's built-in highlights, don't overlay graphics
- Keep annotations minimal

### 7. Test Thumbnail View
- View screenshots at 25% size
- Ensure key elements still visible
- Text should be readable (somewhat)
- Overall screen should be identifiable

---

## 🚨 Common Mistakes to Avoid

### ❌ Don't Do This:
- Empty screens with "No data" messages
- Loading spinners or progress bars
- Error messages or red warnings
- Debug text or developer tools visible
- Test user names like "Test User" or "John Doe"
- Lorem ipsum text
- Fake/obvious stock photos
- Low resolution or blurry images
- Cropped text or cut-off elements
- Visible personal information
- Outdated UI (ensure you're running latest build)
- Mixed orientations (some portrait, some landscape)

### ✅ Do This Instead:
- Populated screens with realistic data
- Completed states with success indicators
- Real dog names and breeds
- Actual content and descriptions
- High-resolution, sharp images
- Full UI elements clearly visible
- Anonymized but realistic data
- Current app version
- Consistent portrait orientation
- Professional, polished appearance

---

## 📁 Organization

Create a folder structure:

```
store_listing/
├── screenshots/
│   ├── raw/                    # Original screenshots from device
│   │   ├── 01_dashboard.png
│   │   ├── 02_dog_profile.png
│   │   ├── 03_health.png
│   │   └── ...
│   ├── edited/                 # After editing/optimization
│   │   ├── 01_dashboard_final.png
│   │   ├── 02_dog_profile_final.png
│   │   └── ...
│   └── with_frames/            # With device frames (optional)
│       └── ...
```

---

## ✅ Final Verification

Before uploading to APKPure:

**Test on Different Displays:**
- [ ] View on large monitor (check quality)
- [ ] View on mobile device (check readability)
- [ ] View as thumbnails (check recognizability)

**Compare with Competitors:**
- [ ] Search similar apps on APKPure
- [ ] Check their screenshot quality
- [ ] Ensure yours are comparable or better

**Get Feedback:**
- [ ] Show screenshots to colleague/friend
- [ ] Ask: "What does this app do?" (should be clear)
- [ ] Ask: "Would you download this?" (gauge appeal)

---

## 📞 Need Help?

**Screenshot Issues:**
- Flutter Discord: https://discord.gg/flutter
- Stack Overflow: https://stackoverflow.com/questions/tagged/flutter

**Image Editing Help:**
- GIMP Forum: https://www.gimp.org/tutorials/
- Canva Help: https://www.canva.com/help/

**Professional Screenshots:**
- Hire on Fiverr: Search "app store screenshots"
- Budget: $20-50 for set of 8 screenshots

---

**Ready to capture? Follow this guide and you'll have professional screenshots ready! 📸**

Last Updated: September 7, 2026
