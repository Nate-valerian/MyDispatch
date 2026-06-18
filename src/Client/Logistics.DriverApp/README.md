# DispatchLoad Driver App - Kotlin Multiplatform

A cross-platform driver application built with **Kotlin Multiplatform (KMP)** and **Compose Multiplatform**, running on both **Android** and **iOS** from a single codebase.

> **Cross-Platform**: Share ~90% of code between Android and iOS
> **Native Performance**: Compiled to native code for each platform
> **Modern UI**: Compose Multiplatform for consistent UX

**Migrated from**: .NET MAUI → **KMP (Android + iOS)**

## Features

- **Authentication**: Login and secure session management using ROPC flow
- **Dashboard**: View truck information and active loads
- **Load Management**: View load details, confirm pickups and deliveries
- **Statistics**: Driver performance metrics with interactive charts
- **Past Loads**: Historical load data for the past 90 days
- **Account Management**: Update user profile information
- **Real-time Updates**: SignalR for live load status updates
- **Real-time Location Tracking**: Background location service with proximity detection
- **Push Notifications**: Firebase Cloud Messaging for load updates
- **Maps Integration**: Google Maps for route visualization

## Report

The complete project report can be found in the [report.pdf](docs/report.pdf) file.

## Getting Started

### Prerequisites

- **Android Studio**: Latest version with Kotlin and Compose support
- **Xcode**: Version 15.0+ for iOS development
- **Kotlin Multiplatform Mobile Plugin**: Installed in Android Studio
- **CocoaPods**: For iOS dependency management
- **Simulator/Device**: Android device/emulator (API 26+) and iOS device/simulator (iOS 15.0+)
- **Docker Desktop**: Required for the Aspire backend stack used by local mobile development

### Local Development

Start Docker Desktop, then run the backend from the repository root first. The mobile app needs both
the API and Identity Server:

```bash
dotnet run --project src/Aspire/Logistics.Aspire.AppHost
```

Service URLs used by local mobile builds:

| Target                       | API                                | Identity Server                    |
| ---------------------------- | ---------------------------------- | ---------------------------------- |
| Android emulator, dev flavor | `http://10.0.2.2:7000`             | `http://10.0.2.2:7001`             |
| iOS simulator, dev config    | `http://localhost:7000`            | `http://localhost:7001`            |
| Physical phone               | Use HTTPS staging/prod or a tunnel | Use HTTPS staging/prod or a tunnel |

From `src/Client/Logistics.DriverApp`, build the Android dev app with:

```bash
./gradlew :androidApp:assembleDevDebug
```

On Windows PowerShell:

```powershell
.\gradlew.bat :androidApp:assembleDevDebug
```

The build regenerates the KMP API client from Swagger before Kotlin compilation. By default it
reads:

```text
http://localhost:7000/swagger/v1/swagger.json
```

If the API is available somewhere else, pass the Swagger URL explicitly:

```bash
./gradlew :androidApp:assembleDevDebug -PopenApiSpecUrl=https://your-api-host/swagger/v1/swagger.json
```

Firebase config is optional for local Android builds. If `google-services.json` is missing, the
Google Services and Crashlytics Gradle plugins are skipped.

For iOS simulator work, open `iosApp/iosApp.xcodeproj` in Xcode and run the dev configuration. For
physical iPhone setup, staging URLs, signing, permissions, and TestFlight notes, see the
[iPhone runbook](docs/iphone-runbook.md).

## Tech Stack

### Core Technologies

- Kotlin Multiplatform (KMP)
- Compose Multiplatform (UI)
- Ktor for networking and HTTP clients
- Koin for dependency injection
- Kotlinx Serialization for JSON serialization and deserialization
- Kotlinx Coroutines for asynchronous programming
- Material 3 design system
- JetBrains Navigation Compose for navigation
- OpenAPI Generator for API client generation

### Android Platform

- **Min SDK**: 26 (Android 8.0)
- **Target SDK**: 36 (Android 16)

### iOS Platform

- **iOS Version**: 15.0+
- **Xcode**: 15.0+
- **Swift Interop**: Native iOS integration

## Project Structure

```text
Logistics.DriverApp/
├── composeApp/                     # KMP Application Module
│   ├── src/
│   │   ├── commonMain/             # Shared code (~90%)
│   │   │   └── kotlin/com/dispatchload/driver/
│   │   │       ├── api/            # API clients & networking
│   │   │       ├── model/          # Domain models
│   │   │       ├── navigation/     # Navigation routes & graphs
│   │   │       ├── permission/     # Permission handling
│   │   │       ├── service/        # Business services
│   │   │       ├── ui/             # Compose UI screens & components
│   │   │       ├── util/           # Utilities & extensions
│   │   │       ├── viewmodel/      # ViewModels (MVVM)
│   │   │       └── Module.kt       # Koin DI module
│   │   │
│   │   ├── androidMain/            # Android-specific code
│   │   │   └── kotlin/             # Platform implementations
│   │   │
│   │   ├── iosMain/                # iOS-specific code
│   │   │   └── kotlin/             # Platform implementations
│   │   │
│   │   └── openapi/                # OpenAPI spec for code generation
│   │       └── api-spec.json
│   │
│   └── build.gradle.kts
│
├── iosApp/                         # iOS Application
│   ├── iosApp.xcodeproj/
│   ├── iosApp/
│   └── Configuration/
│
├── docs/                           # Documentation
│   ├── project-proposal.pdf
│   └── uml/
│
├── gradle/
│   └── libs.versions.toml          # Centralized dependency versions
│
├── build.gradle.kts
└── settings.gradle.kts
```

## Architecture

The app follows **Clean Architecture** with **MVVM** pattern:

- **UI Layer**: Compose Multiplatform screens with Material 3
- **ViewModel Layer**: State management with JetBrains Lifecycle ViewModel
- **Service Layer**: Business logic and data orchestration
- **API Layer**: Ktor-based HTTP clients with OpenAPI-generated models
- **Navigation**: JetBrains Navigation Compose for type-safe navigation

### Dependency Injection

Koin is used for multiplatform DI, configured in `Module.kt` with platform-specific modules in
`androidMain` and `iosMain`.

### API Generation

API clients and models are auto-generated from OpenAPI spec using the OpenAPI Generator Gradle
plugin.

#### Automatic Generation (Build-Time)

The API client is automatically regenerated during the build process. When you run any build
command (e.g., `./gradlew assembleDebug`), the `openApiGenerate` task runs automatically before
compilation.

```bash
# Build triggers automatic API generation
./gradlew assembleDebug
```

For local Android builds, the Logistics API must be running because the generated client is created
from Swagger. See [Local Development](#local-development) for the default URL and override command.

#### Manual Generation

To manually regenerate the API client (e.g., after updating the OpenAPI spec):

```bash
# Regenerate API client only
./gradlew openApiGenerate

# Force regeneration and rebuild
./gradlew openApiGenerate --rerun-tasks
./gradlew compileDebugKotlinAndroid
```

#### Generated Files Location

Generated API clients and models are output to:

```text
composeApp/build/generated/openapi/src/main/kotlin/com/dispatchload/driver/api/
```

> **Note**: Generated files are in the `build/` directory and should not be committed to version control.
