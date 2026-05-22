package com.dispatchload.driver

import android.content.Context
import androidx.activity.ComponentActivity
import com.dispatchload.driver.config.AppConfig
import com.dispatchload.driver.config.messagingHubUrl
import com.dispatchload.driver.config.signalRHubUrl
import com.dispatchload.driver.service.AndroidLocaleManager
import com.dispatchload.driver.service.LocaleManager
import com.dispatchload.driver.service.LocationService
import com.dispatchload.driver.service.AndroidNetworkMonitor
import com.dispatchload.driver.service.NetworkMonitor
import com.dispatchload.driver.service.auth.AuthService
import com.dispatchload.driver.service.createAndroidDataStore
import com.dispatchload.driver.service.messaging.MessagingService
import com.dispatchload.driver.service.realtime.SignalRService
import com.dispatchload.driver.util.BarcodeScannerLauncher
import com.dispatchload.driver.util.CameraLauncher
import org.koin.android.ext.koin.androidContext
import org.koin.android.ext.koin.androidLogger
import org.koin.core.context.startKoin
import org.koin.core.module.dsl.singleOf
import org.koin.dsl.module

private var koinInitialized = false

fun initKoin(activity: ComponentActivity) {
    if (koinInitialized) {
        return
    }

    // Create launchers before Koin initialization (must happen in onCreate before setContent)
    val cameraLauncher = CameraLauncher(activity)
    val barcodeScannerLauncher = BarcodeScannerLauncher(activity)

    startKoin {
        androidLogger()
        androidContext(activity)
        modules(
            // Android-specific module (must be loaded first to provide PreferencesManager)
            androidModule(cameraLauncher, barcodeScannerLauncher),
            commonModule()
        )
    }

    koinInitialized = true
}

private fun androidModule(
    cameraLauncher: CameraLauncher,
    barcodeScannerLauncher: BarcodeScannerLauncher
) = module {
    single { createAndroidDataStore(get<Context>()) }
    single { AuthService(AppConfig.identityServerUrl, get()) }
    single { SignalRService(AppConfig.signalRHubUrl, get()) }
    single { MessagingService(AppConfig.messagingHubUrl, get()) }
    singleOf(::LocationService)
    single<NetworkMonitor> { AndroidNetworkMonitor(get()) }
    single<LocaleManager> { AndroidLocaleManager() }

    // Platform-specific launchers
    single { cameraLauncher }
    single { barcodeScannerLauncher }
}
