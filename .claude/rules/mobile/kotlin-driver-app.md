---
paths:
  - "src/Client/Logistics.DriverApp/**/*.kt"
---

# Kotlin Driver App Conventions

Kotlin Multiplatform mobile app for truck drivers using Compose Multiplatform.

## Project Structure

```text
composeApp/src/commonMain/kotlin/com/dispatchload/driver/
├── api/           # ApiFactory and generated API clients
├── model/         # Domain models, extensions, settings
├── navigation/    # Routes, Navigator, entry provider
├── service/       # Services (auth, location, messaging)
├── ui/components/ # Reusable UI components
├── ui/screens/    # Screen composables
├── ui/theme/      # Colors, typography, theme
├── util/          # Extension functions, utilities
├── viewmodel/     # ViewModels with UI state
└── Module.kt      # Koin DI module
```

Platform-specific: `androidMain/`, `iosMain/` for expect/actual implementations.

## Tech Stack

| Category | Library |
|----------|---------|
| UI | Compose Multiplatform (Material3) |
| Navigation | Navigation 3 (type-safe, `@Serializable` routes) |
| DI | Koin (`singleOf`, `viewModelOf`, `koinViewModel()`, `koinInject()`) |
| Networking | Ktor Client |
| Serialization | kotlinx.serialization |
| State | StateFlow + collectAsState() |
| ViewModel | JetBrains Lifecycle ViewModel |
| Storage | DataStore Preferences |
| API | OpenAPI Generator (auto-generated from swagger.json) |

## API Layer

- Generated from backend swagger.json: `./gradlew openApiGenerate`
- Package: `com.dispatchload.driver.api` (clients), `com.dispatchload.driver.api.models` (DTOs)
- APIs accessed via `ApiFactory` (registered in Koin as singletons)
- APIs return `Response<T>` — use `.body()` to get data
- Include `X-Tenant` header via PreferencesManager
- Handle 401 via AuthEventBus for automatic logout
- OrderBy: `-PropertyName` for descending, `PropertyName` for ascending

## DI (Koin)

- Register in `Module.kt`: `singleOf(::Service)`, `viewModelOf(::ViewModel)`
- Parameterized VMs: `viewModel { params -> VM(get(), params.get<String>()) }`
- In composables: `koinViewModel()` (auto-wired), `koinInject()` (services)

## Navigation (Navigation 3)

- Routes: `@Serializable data object XRoute : NavKey` or `data class XRoute(val id: String) : NavKey`
- Top-level routes defined in `topLevelRoutes` set for bottom nav
- Entry provider maps routes to composables via `entry<XRoute> { ... }`
- Actions: `navigator.navigate()`, `goBack()`, `clearAndNavigate()`, `navigateAndClear()`

## ViewModel Pattern

- Extend `ViewModel()`, use `MutableStateFlow<UiState>` + `asStateFlow()`
- Sealed class for UI states: `Loading`, `Success(data)`, `Error(message)`
- Load data in `init {}`, expose `refresh()` for pull-to-refresh
- Use `viewModelScope.launch {}` for coroutines

## UI Conventions

- Screen composables: navigation callbacks as parameters, ViewModel as last param
- Use `Scaffold` + `AppTopBar` for screen structure
- `when (state)` for UiState rendering: Loading → `LoadingIndicator()`, Error → `ErrorView()`
- Reusable components: `CardContainer`, `SectionCard`, `DetailRow`, `EmptyStateView`
- Access user settings via `LocalUserSettings.current`
- DTO extensions in `model/DtoExtensions.kt`
- Platform-specific formatting via expect/actual (`formatCurrency()`, `formatDistance()`)

## Build

```bash
./gradlew assembleDebug          # Android
./gradlew openApiGenerate        # Regenerate API clients
./gradlew clean build            # Clean build
```
