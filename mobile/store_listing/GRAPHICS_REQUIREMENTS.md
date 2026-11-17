# Graphics Requirements for APKPure Submission

## 📐 Required Graphics Specifications

### 1. App Icon
**Dimensions:** 512 x 512 pixels  
**Format:** PNG (24-bit, no alpha/transparency)  
**File Size:** Under 1MB  
**Current Location:** `mobile/assets/icon.png`

**Design Guidelines:**
- Square shape (APKPure will apply rounding)
- Clear, recognizable design
- High contrast for visibility
- No text or should be minimal
- Represents dog management/business theme
- Professional appearance

**Tips:**
- Use your brand colors
- Simple, memorable design
- Works well at small sizes (e.g., 48x48)
- Test on light and dark backgrounds

---

### 2. Feature Graphic
**Dimensions:** 1024 x 500 pixels  
**Format:** PNG or JPG  
**File Size:** Under 1MB  
**Aspect Ratio:** 2.048:1

**Design Guidelines:**
- Showcase app name and key features
- Professional, eye-catching design
- Readable text (if any)
- Consistent with app branding
- High quality, no pixelation

**Suggested Content:**
- App logo/icon on left
- App name: "Black Eagle"
- Tagline: "Professional Dog Management"
- Key feature icons (dog, health, business)
- Clean, modern design

**Color Palette Suggestions:**
- Primary: #1976D2 (Blue - professional, trustworthy)
- Secondary: #FFA000 (Amber - energetic, warm)
- Accent: #4CAF50 (Green - health, growth)
- Background: White or gradient
- Text: Dark gray or black for readability

---

### 3. Screenshots (Phone)
**Minimum Required:** 2 screenshots  
**Recommended:** 4-8 screenshots  
**Dimensions:** 1080 x 1920 pixels (portrait) or larger  
**Format:** PNG or JPG  
**Aspect Ratio:** 16:9 or 9:16  
**File Size:** Under 8MB each

**Best Practices:**
- Use actual app screenshots (not mockups)
- High resolution, crisp quality
- Show key features and functionality
- Logical order (onboarding → main features → advanced)
- Consistent status bar and navigation
- Remove or anonymize sensitive data
- Use devices with clean UI (no operator logos)

**Recommended Screenshot Sequence:**

#### Screenshot 1: Dashboard/Home
- **Purpose:** First impression, overview
- **Content:** Main dashboard with statistics
- **Shows:** Total dogs, active dogs, financial summary
- **Screen:** Dashboard with cards and metrics

#### Screenshot 2: Dog Profile
- **Purpose:** Core functionality
- **Content:** Individual dog details page
- **Shows:** Dog photo, breed, health status, key info
- **Screen:** Dog profile with avatar, info rows, tabs

#### Screenshot 3: Health Tracking
- **Purpose:** Medical management capability
- **Content:** Vaccination records or health history
- **Shows:** Vaccination list, dates, next due
- **Screen:** Health tracking screen with timeline

#### Screenshot 4: Breeding Management
- **Purpose:** Specialized feature
- **Content:** Mating records or litter management
- **Shows:** Breeding cycles, partner info, puppies
- **Screen:** Breeding screen with records

#### Screenshot 5: Business Analytics
- **Purpose:** Financial tracking
- **Content:** Financial dashboard or reports
- **Shows:** Income, expenses, profit charts
- **Screen:** Analytics with graphs and KPIs

#### Screenshot 6: Dog List/Management
- **Purpose:** Organization capability
- **Content:** List of all dogs with filters
- **Shows:** Multiple dog cards, search, filter options
- **Screen:** Dogs list screen

#### Screenshot 7: Care Record Entry
- **Purpose:** Daily operations
- **Content:** Add feeding, training, or care record
- **Shows:** Form with fields, date picker, notes
- **Screen:** Care record form

#### Screenshot 8: Reports/Certificate
- **Purpose:** Professional output
- **Content:** Generated PDF certificate or report
- **Shows:** Formatted document, professional look
- **Screen:** PDF viewer or certificate preview

---

### 4. Optional: Video Preview
**Length:** 30 seconds to 2 minutes  
**Format:** MP4 (H.264)  
**Dimensions:** 1280 x 720 (16:9) or 1920 x 1080  
**File Size:** Under 100MB

**Video Content Ideas:**
- Quick app tour (15-30 seconds per feature)
- Problem → Solution approach
- Key features highlight
- User testimonial (optional)
- Call to action

---

## 🎨 How to Capture Screenshots

### Method 1: Android Device
1. Open your app on an Android device
2. Navigate to the screen you want to capture
3. Press **Power + Volume Down** simultaneously
4. Screenshots saved to `/Pictures/Screenshots/`
5. Transfer to PC via USB or cloud

### Method 2: Android Emulator
1. Open Android Studio or Flutter emulator
2. Launch your app (`flutter run`)
3. Navigate to desired screen
4. Click camera icon in emulator toolbar
5. Or use **Ctrl + S** (Windows) or **Cmd + S** (Mac)

### Method 3: ADB (Best Quality)
```bash
# Connect device via USB with debugging enabled
adb shell screencap -p /sdcard/screenshot.png
adb pull /sdcard/screenshot.png
```

### Recommended Devices for Screenshots
- **Pixel 5** (1080 x 2340) - Clean Android UI
- **Pixel 6** (1080 x 2400) - Modern design
- **Samsung Galaxy S21** (1080 x 2400) - Popular
- Use emulator with Google Pixel skin for consistency

---

## 🖼️ Image Editing Tools

### Free Tools
- **Canva** (https://canva.com) - Feature graphic, banners
- **Figma** (https://figma.com) - Professional design
- **GIMP** (https://gimp.org) - Free Photoshop alternative
- **Photopea** (https://photopea.com) - Online Photoshop
- **Inkscape** - Vector graphics

### Screenshot Frame Tools
- **Screely** (https://screely.com) - Add device frames online
- **MockUPhone** (https://mockuphone.com) - Device mockups
- **Shotsnapp** (https://shotsnapp.com) - Beautiful screenshots
- **AppMockUp** - Device frame generator

### Optimization Tools
- **TinyPNG** (https://tinypng.com) - Compress PNG/JPG
- **Squoosh** (https://squoosh.app) - Google's image optimizer
- **ImageOptim** - Mac image compression

---

## 📝 Screenshot Annotation Guidelines

### What to Highlight
- Key features with subtle arrows or circles
- Important buttons or actions
- Unique selling points
- Data visualizations

### What to Avoid
- Too much text overlay
- Blocking core UI elements
- Low contrast annotations
- Overly promotional text
- Clutter or busy designs

---

## ✅ Pre-Upload Checklist

Before uploading graphics:

- [ ] App icon is exactly 512x512 PNG
- [ ] Feature graphic is exactly 1024x500 PNG/JPG
- [ ] At least 2 screenshots are 1080x1920 or larger
- [ ] All images are high resolution (not blurry)
- [ ] No sensitive user data visible in screenshots
- [ ] Colors are consistent with branding
- [ ] Text is readable on all graphics
- [ ] File sizes are under limits
- [ ] All images are in correct format (PNG/JPG)
- [ ] Screenshots show real app functionality
- [ ] Graphics look professional and polished

---

## 🎨 Feature Graphic Template Ideas

### Layout Option 1: Classic
```
┌─────────────────────────────────────────────┐
│  [App Icon]  BLACK EAGLE                    │
│     512x512  Professional Dog Management     │
│              System                          │
│                                              │
│              [Dog Icon] [Health Icon]        │
│              [Business Icon]                 │
└─────────────────────────────────────────────┘
```

### Layout Option 2: Modern
```
┌─────────────────────────────────────────────┐
│                                              │
│  BLACK EAGLE    [Key Feature Icons]         │
│  Dog Management  🐕 💉 📊                    │
│                                              │
│  Manage • Track • Grow                      │
└─────────────────────────────────────────────┘
```

### Layout Option 3: Showcase
```
┌─────────────────────────────────────────────┐
│  [App Screenshots Montage]                  │
│                                              │
│  BLACK EAGLE - Professional Dog Management  │
│  Health • Breeding • Business • Analytics    │
└─────────────────────────────────────────────┘
```

---

## 🚀 Tools to Create Feature Graphic

### Canva Template (Recommended)
1. Go to https://canva.com
2. Create custom size: 1024 x 500 px
3. Choose "Presentation" or "Banner" template
4. Customize with:
   - App icon
   - App name text
   - Key feature icons
   - Background gradient or color
   - Call to action
5. Download as PNG

### Figma Template
1. Create new design (1024 x 500)
2. Add layers:
   - Background rectangle (full size)
   - App icon (import PNG)
   - Text layers (app name, tagline)
   - Feature icons (from Icons8 or Font Awesome)
3. Export as PNG at 2x resolution

---

## 📐 Quick Reference Dimensions

| Asset | Width | Height | Format | Notes |
|-------|-------|--------|--------|-------|
| App Icon | 512 | 512 | PNG | No transparency |
| Feature Graphic | 1024 | 500 | PNG/JPG | Main banner |
| Phone Screenshot | 1080 | 1920 | PNG/JPG | Portrait |
| Tablet Screenshot | 1920 | 1200 | PNG/JPG | Landscape |
| Video | 1280 | 720 | MP4 | Optional |

---

## 💡 Pro Tips

1. **Consistency:** Use the same status bar style across all screenshots
2. **Clean Data:** Use demo data that looks realistic but not sensitive
3. **Timing:** Capture screenshots at 10:00 AM with full battery (looks professional)
4. **Locale:** Use English for international appeal
5. **Density:** Use xxhdpi or xxxhdpi devices for sharp screenshots
6. **Orientation:** Stick to portrait for phone screenshots (more intuitive)
7. **Context:** Show meaningful data, not empty states
8. **Updates:** Update screenshots when UI changes significantly

---

## 🎯 Design Resources

### Icon Libraries
- **Material Icons** (https://fonts.google.com/icons)
- **Font Awesome** (https://fontawesome.com)
- **Icons8** (https://icons8.com)
- **Flaticon** (https://flaticon.com)

### Stock Photos (Dog Images)
- **Unsplash** (https://unsplash.com/s/photos/dog)
- **Pexels** (https://pexels.com/search/dog)
- **Pixabay** (https://pixabay.com/images/search/dog)

### Color Palettes
- **Coolors** (https://coolors.co)
- **Adobe Color** (https://color.adobe.com)
- **Material Palette** (https://materialpalette.com)

---

**Need Help?**
Contact: support@dogsection.com

**Graphics Design Service:**
If you need professional graphics created, consider hiring a designer on:
- Fiverr (https://fiverr.com)
- Upwork (https://upwork.com)
- 99designs (https://99designs.com)

Budget: $50-200 for complete app store graphics package
