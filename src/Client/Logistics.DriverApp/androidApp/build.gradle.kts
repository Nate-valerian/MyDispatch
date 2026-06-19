plugins {
    alias(libs.plugins.androidApplication)
    alias(libs.plugins.composeCompiler)
}

val googleServicesCandidates = listOf(
    "google-services.json",
    "src/dev/google-services.json",
    "src/debug/google-services.json",
    "src/devDebug/google-services.json",
    "src/prod/google-services.json",
    "src/release/google-services.json",
    "src/prodRelease/google-services.json"
).map { file(it) }

val hasGoogleServicesConfig = googleServicesCandidates.any { it.exists() }
val devApiBaseUrl = providers.gradleProperty("driverApiBaseUrl")
    .orElse("http://10.0.2.2:7000")
    .get()
val devIdentityServerUrl = providers.gradleProperty("driverIdentityServerUrl")
    .orElse("http://10.0.2.2:7001")
    .get()

if (hasGoogleServicesConfig) {
    apply(plugin = "com.google.gms.google-services")
    apply(plugin = "com.google.firebase.crashlytics")
} else {
    logger.lifecycle(
        "google-services.json not found; Firebase Google Services and Crashlytics plugins are skipped for this build."
    )
}

android {
    namespace = "com.dispatchload.driver"
    compileSdk = 37

    defaultConfig {
        applicationId = "com.dispatchload.driver"
        minSdk = 26
        targetSdk = 36
        versionCode = 4
        versionName = "1.0.0"

        vectorDrawables {
            useSupportLibrary = true
        }
    }

    flavorDimensions += "environment"
    productFlavors {
        create("dev") {
            dimension = "environment"
            versionNameSuffix = "-dev"
            buildConfigField("String", "API_BASE_URL", "\"$devApiBaseUrl\"")
            buildConfigField("String", "IDENTITY_SERVER_URL", "\"$devIdentityServerUrl\"")
            manifestPlaceholders["allowCleartext"] = "true"
        }
        create("prod") {
            dimension = "environment"
            buildConfigField("String", "API_BASE_URL", "\"https://api.dispatchload.app\"")
            buildConfigField("String", "IDENTITY_SERVER_URL", "\"https://id.dispatchload.app\"")
            manifestPlaceholders["allowCleartext"] = "false"
        }
    }

    signingConfigs {
        create("release") {
            storeFile = rootProject.file("release-keystore.jks")
            storePassword = System.getenv("KEYSTORE_PASSWORD") ?: "dispatchload"
            keyAlias = "release"
            keyPassword = System.getenv("KEY_PASSWORD") ?: "dispatchload"
        }
    }

    buildTypes {
        release {
            isMinifyEnabled = true
            isShrinkResources = true
            signingConfig = signingConfigs.getByName("release")
            proguardFiles(
                getDefaultProguardFile("proguard-android-optimize.txt"),
                "proguard-rules.pro"
            )
            ndk {
                debugSymbolLevel = "FULL"
            }
        }
        debug {
            isMinifyEnabled = false
        }
    }

    buildFeatures {
        compose = true
        buildConfig = true
    }

    packaging {
        resources {
            excludes += "/META-INF/{AL2.0,LGPL2.1}"
        }
    }
}

dependencies {
    // Depend on the shared KMP library module
    implementation(project(":composeApp"))

    // Android Compose
    implementation(libs.androidx.activity.compose)

    // AndroidX Core
    implementation(libs.androidx.core.ktx)

    // Koin Android
    implementation(platform(libs.koin.bom))
    implementation(libs.koin.core)
    implementation(libs.koin.android)
    implementation(libs.koin.androidx.compose)

    // Firebase
    implementation(platform(libs.firebase.bom))
    implementation(libs.firebase.crashlytics)
    implementation(libs.firebase.messaging)

    // Google Play Services & Maps
    implementation(libs.play.services.location)
    implementation(libs.bundles.maps)
}
