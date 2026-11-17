# Black Eagle — Mobile App

Flutter mobile client for the **Black Eagle Surveillance** kennel & operations
platform. It talks to the live API at
`https://api.blackeaglesurveillance.co.zw/api/`.

Android is the primary target; the iOS project folder is kept intact and builds,
but has not been the focus yet.

## Responsive by design

The #1 goal of this app is to **fit every Android phone** — small budget devices
and large flagships alike — without anything looking cramped or stretched.

How it's achieved (no hard-coded pixel sizes anywhere):

- **[flutter_screenutil](https://pub.dev/packages/flutter_screenutil)** — every
  dimension is written against a 375×812 design baseline using `.w` / `.h` /
  `.sp` / `.r`. At runtime these resolve against the *actual* device size, so the
  same layout scales proportionally on any screen. Baseline is configured in
  [`lib/core/responsive/responsive.dart`](lib/core/responsive/responsive.dart)
  and applied in [`lib/main.dart`](lib/main.dart) via `ScreenUtilInit`.
- **Width breakpoints** (`context.isTablet`, `context.gridColumns(...)`) switch
  *layout* where it helps — e.g. the dashboard stat grid adds columns on wider
  screens and the home shell swaps its bottom bar for a `NavigationRail` on
  tablets/landscape.
- **Text-scale clamping** so very large OS accessibility font settings can't
  break the layout (clamped to 0.85–1.3 in `main.dart`).
- Scrollable forms, `FittedBox` on numbers, and `Flexible`/`Expanded` everywhere
  to avoid overflow on the narrowest devices.

## Architecture

```
lib/
  core/            config, theme, responsive helpers, dio client, secure storage, router
  features/
    auth/          login + session (JWT, auto-refresh)
    home/          bottom-nav / rail shell
    dashboard/     at-a-glance stats
    dogs/          list, profile, add/edit, vaccinations
    breeding/      mating records, litters
    business/      invoices (read), customers (list + add)
    profile/       account + sign out
  shared/widgets/  reusable, responsive UI building blocks
```

- **State**: `provider` (`AuthProvider` holds the session & permissions).
- **Networking**: `dio` wrapper in
  [`lib/core/network/api_client.dart`](lib/core/network/api_client.dart) —
  attaches the bearer token, unwraps the `{ success, message, data, errors }`
  envelope, and transparently refreshes an expired token once on `401`.
- **Auth storage**: `flutter_secure_storage` (encrypted).
- **Permissions**: the API returns the user's permission list at login;
  create/edit actions (FABs, edit buttons) are gated with `auth.can('pet.create')`
  etc., matching the backend `[HasPermission(...)]` attributes.

## Running

```bash
cd mobile
flutter pub get
flutter run            # on a connected Android device/emulator
```

Build artifacts:

```bash
flutter build apk            # release APK
flutter build appbundle      # Play Store bundle
```

Sign in with a real platform account (email / username / phone + password).

## Configuration

API base URL and timeouts live in
[`lib/core/config/app_config.dart`](lib/core/config/app_config.dart). Point
`apiBaseUrl` at a different environment if needed.

## Notes / next steps

- Invoices are currently read-only on mobile (creation with line items stays on
  the web portal for now); customers can be listed and created.
- iOS: project is present and should build, but UI has only been verified on
  Android so far.
