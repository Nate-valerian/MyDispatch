# Handoff: Mobile Android Local Dev

Date: 2026-06-18 23:43 +03:00

## What Changed In Repo

- `src/Aspire/Logistics.Aspire.AppHost/Properties/launchSettings.json`
  - Added `ASPIRE_ALLOW_UNSECURED_TRANSPORT=true` so the Aspire dashboard can run on `http://localhost:7100`.
- `src/Client/Logistics.DriverApp/README.md`
  - Replaced the generic setup section with concrete local mobile dev instructions.
  - Documented Docker Desktop as required for the Aspire backend stack.
  - Documented Android emulator URLs:
    - API: `http://10.0.2.2:7000`
    - Identity Server: `http://10.0.2.2:7001`
  - Documented Android build command:
    - `.\gradlew.bat :androidApp:assembleDevDebug`
  - Documented OpenAPI Swagger source and override flag.
  - Documented Firebase config as optional for local Android builds.

## Machine Setup Completed

- Docker Desktop for Windows installed.
- Docker engine verified from Windows:
  - Client/server `29.5.3`
- Android Studio installed:
  - Android Studio Quail 1 `2026.1.1 Patch 2`
- Android SDK wired through:
  - `src/Client/Logistics.DriverApp/local.properties`
  - `sdk.dir=C:/Users/nate-/AppData/Local/Android/Sdk`
- Android emulator created and booted:
  - `Pixel 8 Pro API 36.1`
  - `emulator-5554`
- Driver APK built successfully:
  - `.\gradlew.bat :androidApp:assembleDevDebug`
- Driver APK installed successfully:
  - `androidApp\build\outputs\apk\dev\debug\androidApp-dev-debug.apk`
- Driver app launched:
  - Package: `com.dispatchload.driver`
  - Activity: `com.dispatchload.driver/.MainActivity`

## Environment Issues Solved

- Aspire initially failed because `ASPIRE_ALLOW_UNSECURED_TRANSPORT` was missing.
- Docker was installed only inside Ubuntu WSL, while Aspire was being run from Windows PowerShell.
- Docker Desktop install initially failed because `C:\ProgramData\DockerDesktop` was owned by the user instead of Administrators.
  - Fixed by setting owner to Administrators in elevated PowerShell.
- Aspire DCP initially hit `SocketException 10013` on IPv6 loopback `::1`.
  - Fixed by adjusting Windows IPv6 prefix policy so IPv4-mapped addresses are preferred:
    - `netsh interface ipv6 set prefixpolicy ::ffff:0:0/96 60 4`
    - `ipconfig /flushdns`
- Docker CLI path may still require a new terminal or temporary session path:
  - `$env:Path += ";C:\Program Files\Docker\Docker\resources\bin"`

## Current Runtime Notes

- Aspire dashboard was confirmed running:
  - `http://localhost:7100`
- Docker containers observed running:
  - `postgres-ypfbmyxs`
  - `pgadmin-hnckhtdf`
- API and Identity ports were listening through Aspire at one point:
  - `7000`
  - `7001`
- Earlier API/Identity HTTP calls timed out while startup/migration was still settling.
- Postgres logs showed missing tenant databases before migrator completion:
  - `master_dispatchload`
  - `us_dispatchload`
  - `eu_dispatchload`

## Test Credentials

From `docs/getting-started/test-credentials.md`:

- Super Admin:
  - `admin@test.com`
  - `Test12345#`
  - Admin portal
- Owner:
  - `owner@test.com`
  - `Test12345#`
  - TMS/Office portal
- Driver:
  - `driver1@test.com`
  - `Test12345#`
  - Driver mobile app

Local portal URLs:

- Aspire dashboard: `http://localhost:7100`
- Identity Server: `http://localhost:7001`
- API Swagger: `http://localhost:7000/swagger/v1/swagger.json`
- Admin portal: `http://localhost:7002`
- TMS/Office portal: `http://localhost:7003`

## Tomorrow Checklist

1. Start Docker Desktop.
2. Open a new PowerShell and verify:
   - `docker version`
3. Start Aspire from repo root:
   - `dotnet run --project src\Aspire\Logistics.Aspire.AppHost`
4. Open Aspire dashboard:
   - `http://localhost:7100`
5. Check resource statuses:
   - `postgres` running
   - `migrator` completed successfully
   - `identity-server` running
   - `api` running
6. Verify endpoints:
   - `http://localhost:7000/swagger/v1/swagger.json`
   - `http://localhost:7001/.well-known/openid-configuration`
7. Start Android emulator if not already running.
8. Reinstall/relaunch driver app if needed:
   - `cd src\Client\Logistics.DriverApp`
   - `.\gradlew.bat :androidApp:assembleDevDebug`
   - `adb install -r androidApp\build\outputs\apk\dev\debug\androidApp-dev-debug.apk`
   - `adb shell am start -n com.dispatchload.driver/.MainActivity`
9. Try driver login:
   - `driver1@test.com`
   - `Test12345#`
10. If login fails, inspect:
    - mobile logcat
    - Aspire `api` logs
    - Aspire `identity-server` logs
    - whether migrator completed and seeded users

## Git Working Tree At Handoff

Tracked changes:

- `.codex/archives/2026-06-16-mydispatch-mobile-postgres-status.txt`
- `src/Aspire/Logistics.Aspire.AppHost/Properties/launchSettings.json`
- `src/Client/Logistics.DriverApp/README.md`

Untracked files:

- `.codex/russia-market-notes-2026-06-16.md`
- `docs/store-assets/linkedin-driver-composite.png`

Suggested commit later:

- Commit only the AppHost launch profile and mobile README docs as a focused local-dev setup update.
- Leave unrelated `.codex` market notes and store asset alone unless intentionally part of another task.
