# APKPure – paste-ready submission (Black Eagle v1.1.0)

APK to upload: `mobile/release/BlackEagle-1.1.0-universal.apk` (37 MB)
(one universal file with 64-bit and 32-bit ARM code - installs on every Android 7.0+ phone and tablet; signed with the release key `black-eagle-release.jks`. A 58 MB variant that also contains x86_64 for emulators is `BlackEagle-1.1.0-universal-with-x86.apk`.)

## Basic information

| Field | Paste this |
|---|---|
| App name | Black Eagle – Dog Management |
| Package name | com.blackeagle.black_eagle |
| Version name / code | 1.1.0 / 2 |
| Category | Business (alternative: Productivity) |
| Price | Free |
| Content rating | Everyone |
| Minimum Android | 7.0 (API 24) |
| Developer / company | Black Eagle Surveillance |
| Contact email | support@blackeaglesurveillance.co.zw |
| Website | https://dogweb.blackeaglesurveillance.co.zw |
| Privacy policy URL | https://dogweb.blackeaglesurveillance.co.zw/Home/Privacy *(deploy the new Portal first so this page exists)* |
| Language | English |

## Short description (under 80 characters)

```
Dog kennel, breeding, training and invoicing – all in one business app.
```

## Full description

```
Black Eagle is the mobile app for kennels, security-dog companies, breeders and dog trainers. Run your whole dog operation from your phone: dogs, health, breeding, dog training, customers and money.

DOG MANAGEMENT
• Complete dog profiles with photos, breed, colour, microchip and registration details
• Photo gallery per dog with full-screen zoom
• Dog certificates as PDF with your company logo and details
• Search your dogs, customers and invoices from one place

HEALTH & CARE
• Vaccination and deworming records with next-due dates
• Illness, injury and vet-visit history
• Daily care log: feeding, exercise, grooming and behaviour notes

BREEDING
• Heat cycles, mating records and expected dates
• Pregnancy and litter tracking, puppy records and age
• Litter totals update automatically when a puppy dies

DOG TRAINING
• See the dogs allocated to you and where each one is in its 21-day programme
• Record today's training: rate every activity, minutes trained and notes
• Weekly progress reports for management review

CUSTOMERS & INVOICES
• Customers linked to their company, with job title and contact details
• Create invoices for the customer or company
• Invoice detail with company VAT details, items, totals and payment history
• Record payments by Cash, EcoCash, OneMoney, InnBucks, bank transfer, card or cheque – with payer, reference and receiving account so every dollar can be traced
• Bank transfers and cheques stay pending until finance confirms them
• Download or share the professional invoice PDF

BUSINESS
• Track income and expenses by category
• Dashboards and analytics for your company
• Multiple branches: head office sees everything, branches see their own

TEAM & SECURITY
• Handlers and staff with role-based permissions – people only see and do what their role allows
• Secure sign-in; your data is sent over HTTPS
• Works with the Black Eagle web portal and WhatsApp bot, so everything stays in sync

Requires a Black Eagle account from your organisation. Contact support@blackeaglesurveillance.co.zw to get started.
```

## What's new (version 1.1.0)

```
• New: record payments from the invoice screen (cash, EcoCash, bank transfer, card, cheque) with full trace details
• New: invoice screen shows the billed company, VAT number and the payment history
• Dog training: handler logbook and weekly reports
• Smaller, faster app and improved stability
• Signed release build that installs on all Android 7.0+ phones and tablets
```

## Tags / keywords

```
dog management, kennel, breeding, dog training, k9, security dogs, veterinary records, invoicing, business, zimbabwe
```

## Screenshots to upload (take them from the installed app, phone portrait)

1. Dashboard / home
2. Dog profile with photo
3. Health & vaccination records
4. Breeding / litter screen
5. Training – today's logbook
6. Invoice detail with payments
7. Record payment sheet
8. Customers list

APKPure needs at least 2 screenshots (1080×1920 recommended), the 512×512 icon (`mobile/assets/icon.png`) and, optionally, a 1024×500 banner.

## Why the earlier upload failed ("Publishment Failed")

The previous APK was signed with Flutter's **debug** key and the project was missing two libraries (`photo_view`, `image_cropper`) that the code uses. Both are fixed in this build. Always upload APKs signed with the same release key (`black-eagle-release.jks`) – APKPure rejects updates signed with a different key.
