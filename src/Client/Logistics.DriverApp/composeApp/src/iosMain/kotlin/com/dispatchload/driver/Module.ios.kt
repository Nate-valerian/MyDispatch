package com.dispatchload.driver

import com.dispatchload.driver.config.AppConfig
import com.dispatchload.driver.config.messagingHubUrl
import com.dispatchload.driver.config.signalRHubUrl
import com.dispatchload.driver.service.IosLocaleManager
import com.dispatchload.driver.service.LocaleManager
import com.dispatchload.driver.service.LocationService
import com.dispatchload.driver.service.IosNetworkMonitor
import com.dispatchload.driver.service.NetworkMonitor
import com.dispatchload.driver.service.auth.AuthService
import com.dispatchload.driver.service.createIosDataStore
import com.dispatchload.driver.service.messaging.MessagingService
import com.dispatchload.driver.service.realtime.SignalRService
import org.koin.core.context.startKoin
import org.koin.core.module.dsl.singleOf
import org.koin.dsl.module

private var koinInitialized = false

fun initKoin() {
    if (koinInitialized) {
        return
    }

    startKoin {
        modules(
            iosModule,
            commonModule()
        )
    }
    koinInitialized = true
}

/**
 * Koin module for iOS-specific dependencies
 */
val iosModule = module {
    single { createIosDataStore() }
    single { AuthService(AppConfig.identityServerUrl, get()) }
    single<SignalRService> { SignalRService(AppConfig.signalRHubUrl, get()) }
    single { MessagingService(AppConfig.messagingHubUrl, get()) }
    singleOf(::LocationService)
    single<NetworkMonitor> { IosNetworkMonitor() }
    single<LocaleManager> { IosLocaleManager() }
}
