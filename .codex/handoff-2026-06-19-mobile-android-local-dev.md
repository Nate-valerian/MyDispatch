# Mobile Android Local Dev Handoff - 2026-06-19

## What changed

- Aspire local dev now passes the same tenant database host, port, username, and password into API and migrator resources.
- Local dev Postgres still exposes the proxy on `localhost:5433`; the migrator now receives explicit seeded tenant connection strings for `us_dispatchload` and `eu_dispatchload`.
- The migrator waits for the master database before running.
- Android dev builds can override generated `BuildConfig` backend URLs with Gradle properties:
  - `driverApiBaseUrl`
  - `driverIdentityServerUrl`
- `src/Client/Logistics.DriverApp/README.md` documents the local override command.

## Current local state

- Aspire dashboard: `http://localhost:7100`
- Aspire-managed Postgres proxy: `localhost:5433`
- Published Identity workaround: `http://localhost:17001`
- Published API workaround: `http://localhost:17000`
- Verified endpoints:
  - `http://localhost:17001/.well-known/openid-configuration`
  - `http://localhost:17000/swagger/v1/swagger.json`
- Rebuilt APK:
  - `src/Client/Logistics.DriverApp/androidApp/build/outputs/apk/dev/debug/androidApp-dev-debug.apk`
- The APK was installed and launched on emulator `emulator-5554`.

## Smart App Control blocker

Normal Aspire-launched API and Identity resources on ports `7000` and `7001` were blocked by Windows Smart App Control / Code Integrity. Logs showed:

`System.IO.FileLoadException ... An Application Control policy has blocked this file. (0x800711C7)`

The blocked files included unsigned local build DLLs such as:

- `Logistics.Infrastructure.Integrations.Eld.dll`
- `Logistics.IdentityServer.dll`

Cleaning and rebuilding did not resolve it. Direct apphost launch did not resolve it either because the process still loads local DLLs.

## Workaround used

Publish API and Identity as single-file framework-dependent apphosts, then run them from `.codex/publish` on alternate ports while Aspire keeps Postgres running:

```powershell
dotnet publish src\Presentation\Logistics.IdentityServer\Logistics.IdentityServer.csproj -c Debug -o .codex\publish\identity /p:UseAppHost=true /p:SelfContained=false
dotnet publish src\Presentation\Logistics.API\Logistics.API.csproj -c Debug -o .codex\publish\api /p:UseAppHost=true /p:SelfContained=false
```

Build Android against those workaround ports:

```powershell
cd src\Client\Logistics.DriverApp
.\gradlew.bat :androidApp:assembleDevDebug -PopenApiSpecUrl=http://localhost:17000/swagger/v1/swagger.json -PdriverApiBaseUrl=http://10.0.2.2:17000 -PdriverIdentityServerUrl=http://10.0.2.2:17001
```

Install the APK:

```powershell
adb install -r androidApp\build\outputs\apk\dev\debug\androidApp-dev-debug.apk
adb shell am start -n com.dispatchload.driver/.MainActivity
```

## Remaining caveat

The app has been installed and launched, but driver login has not yet been manually verified in the emulator UI.
