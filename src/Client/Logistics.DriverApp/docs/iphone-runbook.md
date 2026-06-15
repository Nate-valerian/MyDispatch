# iPhone Runbook

This guide is for running the DispatchLoad Driver App on iPhone through the iOS target in `iosApp/`.

## What Already Exists

- Kotlin Multiplatform shared app in `composeApp/`
- SwiftUI host app in `iosApp/`
- API base URL and Identity Server URL injected through `Info.plist`
- Dev and production xcconfig files
- Camera/photo capture for POD, BOL, DVIR, and inspection workflows
- Barcode scanner implementation using AVFoundation
- Location tracking using CoreLocation
- SignalR-based tracking and messaging services

## Main iPhone Blockers

### 1. Real Device Cannot Use `localhost`

The current dev config is for the iOS simulator:

```xcconfig
API_BASE_URL = http:/$()/localhost:7000
IDENTITY_SERVER_URL = http:/$()/localhost:7001
```

That works only when the backend is running on the Mac that hosts the simulator.

For a physical iPhone, use one of these:

- Production/staging HTTPS domains:

```xcconfig
API_BASE_URL = https:/$()/api.dispatchload.app
IDENTITY_SERVER_URL = https:/$()/id.dispatchload.app
```

- A temporary HTTPS tunnel:

```text
Cloudflare Tunnel, ngrok, or another HTTPS tunnel
```

Example:

```xcconfig
API_BASE_URL = https:/$()/your-api-tunnel.trycloudflare.com
IDENTITY_SERVER_URL = https:/$()/your-identity-tunnel.trycloudflare.com
```

Prefer HTTPS for real devices. Avoid adding broad App Transport Security exceptions unless there is no other option.

### 2. Apple Signing Must Be Configured

Open `iosApp/iosApp.xcodeproj` in Xcode and verify:

- Team: your Apple Developer team
- Bundle identifier: `com.dispatchload.driver` or a unique development variant
- Signing: automatic signing is usually easiest for device testing
- Device: connected iPhone trusted by the Mac

If using TestFlight, create the matching App ID and provisioning profile in Apple Developer.

### 3. Backend Must Support Mobile Driver Login

The mobile app currently calls the Identity Server token endpoint with password credentials. For testing this can work, but for App Store-quality mobile auth, prefer:

```text
Authorization Code + PKCE
```

Minimum testing requirements:

- Identity Server reachable from the phone
- Driver account exists
- Access token includes tenant information
- Refresh token works
- API accepts `Authorization: Bearer ...`
- API accepts `X-Tenant`

### 4. iOS Permissions Must Match Real Behavior

The app uses:

- Camera for photos and barcode scanning
- Photo library fallback for choosing existing images
- Location while On Duty
- Background location mode for active driver tracking

The required `Info.plist` usage descriptions are present. If background location is used in production, keep the in-app explanation clear and only track while the driver is On Duty.

### 5. Push Notifications Are Not Complete On iOS Yet

Android has Firebase wiring. For iPhone production push notifications, decide the path:

- APNs directly
- Firebase Cloud Messaging for iOS

Required pieces:

- Apple Push Notification capability in Xcode
- APNs key/certificate
- Firebase iOS app and `GoogleService-Info.plist` if using FCM
- Device token registration endpoint in the backend
- Driver notification preferences and logout cleanup

## Recommended iPhone MVP Test

Run this flow on a physical iPhone:

1. Login as a driver.
2. Confirm the app resolves the correct tenant.
3. Open Dashboard.
4. Open current trip.
5. Open a load.
6. Capture a POD/BOL photo.
7. Submit pickup or delivery action.
8. Toggle On Duty.
9. Verify location update reaches the backend.
10. Send a message to dispatch.
11. Logout and confirm tokens/location tracking stop.

## Suggested Staging Setup

Create staging URLs before TestFlight:

```text
https://staging-api.dispatchload.app
https://staging-id.dispatchload.app
```

Then either:

- add a staging xcconfig and scheme in Xcode, or
- temporarily point `Dev.xcconfig` at the staging URLs for device testing.

Do not ship a TestFlight build pointing at `localhost`.

## Store/TestFlight Checklist

- App icon and launch screen are final
- Privacy policy URL exists
- Camera, photo library, and location descriptions are accurate
- Background location review explanation is ready
- Test driver tenant and credentials exist
- Production HTTPS API and Identity Server certificates are valid
- Push notification strategy is decided
- No dev tunnel URLs in release builds
- Version and build numbers are incremented

