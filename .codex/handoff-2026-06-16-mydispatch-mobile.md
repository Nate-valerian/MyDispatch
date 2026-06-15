# MyDispatch Mobile/Postgres Handoff - 2026-06-16

## Current Goal

Make the MyDispatch mobile app build locally on Windows, with Android first, without using the DOM project's Postgres container.

## Done Today

- Confirmed Docker/Postgres inside Ubuntu belonged to DOM and should not be reused for MyDispatch.
- Removed the temporary MyDispatch databases and role from the DOM Postgres container.
- Created a separate MyDispatch-only PostgreSQL cluster on Windows, using PostgreSQL 18 binaries but an isolated data directory and port.
- Fixed the first cluster attempt by recreating it with UTF-8 encoding so seeded names and route text with Unicode characters save correctly.
- Ran `Logistics.DbMigrator` successfully against the new database and seeded master, US tenant, and EU tenant data.
- Started `Logistics.API` on `http://localhost:7000` and verified Swagger returns a real OpenAPI spec.
- Built the Android driver app successfully with OpenAPI generation from the live API.
- Patched local-dev blockers in the repo:
  - Firebase Gradle plugins are skipped when `google-services.json` is absent.
  - Stripe seeders skip placeholder keys instead of trying to call Stripe.
  - iPhone runbook and iOS permission descriptions were added for the earlier iPhone path.

## Next

- Decide whether to keep the dedicated Windows Postgres flow as the default local dev path or wire it into repo scripts/docs.
- Run the Android APK on an emulator/device and verify login, tenant selection, load list, documents/photos, tracking, and offline behavior.
- Point the mobile app to the correct API base URL for emulator/device testing (`10.0.2.2` for Android emulator, LAN IP for physical phone).
- Add `google-services.json` only when Firebase push/crash reporting is ready for this environment.
- Later: revisit iPhone build on macOS/Xcode or CI/TestFlight. Windows can prepare the shared KMP code but cannot produce the iOS app binary.

## What Changed

- Created a dedicated local PostgreSQL cluster for MyDispatch:
  - Data: `C:\Users\nate-\AppData\Local\PostgreSQL\my-dispatch-data`
  - Port: `55432`
  - User: `postgres`
  - Password: `Test12345#`
  - Databases: `master_dispatchload`, `us_dispatchload`, `eu_dispatchload`
- Seeded the databases successfully with `Logistics.DbMigrator`.
- Started the API on `http://localhost:7000` and verified Swagger:
  - `http://localhost:7000/swagger/v1/swagger.json`
- Built Android successfully:
  - `src\Client\Logistics.DriverApp\androidApp\build\outputs\apk\dev\debug\androidApp-dev-debug.apk`
- Kept DOM Postgres clean. MyDispatch databases were removed from the DOM container.

## Code Changes

- Driver app README now documents local Android/OpenAPI/Firebase behavior.
- Android Gradle build now skips Firebase Google Services/Crashlytics plugins when `google-services.json` is missing.
- iOS `Info.plist` has location and photo-library descriptions.
- Added iPhone runbook.
- Stripe seeders now skip blank or placeholder Stripe keys like `<Stripe secret key>`, so local seeding works without real Stripe credentials.

## Archive

Saved under `.codex/archives/`:

- `2026-06-16-mydispatch-mobile-postgres-tracked.patch`
- `2026-06-16-mydispatch-mobile-postgres-files.zip`
- `2026-06-16-mydispatch-mobile-postgres-status.txt`

## Restart Tomorrow

Start MyDispatch Postgres:

```powershell
& "C:\Program Files\PostgreSQL\18\bin\pg_ctl.exe" -D "$env:LOCALAPPDATA\PostgreSQL\my-dispatch-data" -l "$env:LOCALAPPDATA\PostgreSQL\my-dispatch-postgres.log" -o "-p 55432 -c listen_addresses=127.0.0.1" start
```

Start API against the dedicated DB:

```powershell
$env:ConnectionStrings__MasterDatabase='Host=127.0.0.1; Port=55432; Database=master_dispatchload; Username=postgres; Password=Test12345#; Include Error Detail=true'
$env:TenantDatabaseDefaults__Host='127.0.0.1'
$env:TenantDatabaseDefaults__Port='55432'
$env:TenantDatabaseDefaults__UserId='postgres'
$env:TenantDatabaseDefaults__Password='Test12345#'
dotnet run --project src\Presentation\Logistics.API\Logistics.API.csproj --urls http://localhost:7000
```

Build Android:

```powershell
cd C:\Users\nate-\Desktop\projects\my-dispatch\src\Client\Logistics.DriverApp
.\gradlew.bat :androidApp:assembleDevDebug
```
