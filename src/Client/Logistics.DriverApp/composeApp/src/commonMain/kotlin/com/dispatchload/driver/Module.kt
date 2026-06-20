package com.dispatchload.driver

import com.dispatchload.driver.api.ApiFactory
import com.dispatchload.driver.api.CustomerApi
import com.dispatchload.driver.config.AppConfig
import com.dispatchload.driver.api.DocumentApi
import com.dispatchload.driver.api.DriverApi
import com.dispatchload.driver.api.DvirApi
import com.dispatchload.driver.api.EmployeeApi
import com.dispatchload.driver.api.InspectionsApi
import com.dispatchload.driver.api.LoadApi
import com.dispatchload.driver.api.LoadBoardApi
import com.dispatchload.driver.api.MessageApi
import com.dispatchload.driver.api.PrivacyApi
import com.dispatchload.driver.api.ReportApi
import com.dispatchload.driver.api.StatApi
import com.dispatchload.driver.api.TripApi
import com.dispatchload.driver.api.TruckApi
import com.dispatchload.driver.api.UserApi
import com.dispatchload.driver.api.VinsApi
import com.dispatchload.driver.api.models.DvirType
import com.dispatchload.driver.api.models.InspectionType
import com.dispatchload.driver.service.DutyStatusManager
import com.dispatchload.driver.service.LoadProximityWatcher
import com.dispatchload.driver.service.PreferencesManager
import com.dispatchload.driver.service.messaging.ConversationStateManager
import com.dispatchload.driver.viewmodel.AccountViewModel
import com.dispatchload.driver.viewmodel.AiLoadBoardDetailViewModel
import com.dispatchload.driver.viewmodel.AiLoadFinderSelectionStore
import com.dispatchload.driver.viewmodel.AiLoadFinderViewModel
import com.dispatchload.driver.viewmodel.ChatViewModel
import com.dispatchload.driver.viewmodel.ConditionReportViewModel
import com.dispatchload.driver.viewmodel.ConversationListViewModel
import com.dispatchload.driver.viewmodel.DashboardViewModel
import com.dispatchload.driver.viewmodel.DocumentCaptureType
import com.dispatchload.driver.viewmodel.DvirFormViewModel
import com.dispatchload.driver.viewmodel.EmployeeSelectViewModel
import com.dispatchload.driver.viewmodel.LoadDetailViewModel
import com.dispatchload.driver.viewmodel.LoginViewModel
import com.dispatchload.driver.viewmodel.MyLicensesViewModel
import com.dispatchload.driver.viewmodel.PastLoadsViewModel
import com.dispatchload.driver.viewmodel.PodCaptureViewModel
import com.dispatchload.driver.viewmodel.PrivacyViewModel
import com.dispatchload.driver.viewmodel.SettingsViewModel
import com.dispatchload.driver.viewmodel.StatsViewModel
import com.dispatchload.driver.viewmodel.TripDetailViewModel
import com.dispatchload.driver.viewmodel.TripsViewModel
import io.ktor.client.HttpClient
import org.koin.core.module.dsl.singleOf
import org.koin.core.module.dsl.viewModelOf
import org.koin.dsl.module

fun commonModule() = module {
    singleOf(::PreferencesManager)

    // Register ApiFactory as a singleton
    single { ApiFactory(AppConfig.apiBaseUrl, get(), get()) }

    // HttpClient for file upload operations
    single<HttpClient> { get<ApiFactory>().httpClient }

    // Generated API instances from ApiFactory
    single<CustomerApi> { get<ApiFactory>().customerApi }
    single<DocumentApi> { get<ApiFactory>().documentApi }
    single<DriverApi> { get<ApiFactory>().driverApi }
    single<DvirApi> { get<ApiFactory>().dvirApi }
    single<EmployeeApi> { get<ApiFactory>().employeeApi }
    single<InspectionsApi> { get<ApiFactory>().inspectionsApi }
    single<LoadApi> { get<ApiFactory>().loadApi }
    single<LoadBoardApi> { get<ApiFactory>().loadBoardApi }
    single<MessageApi> { get<ApiFactory>().messageApi }
    single<PrivacyApi> { get<ApiFactory>().privacyApi }
    single<ReportApi> { get<ApiFactory>().reportApi }
    single<StatApi> { get<ApiFactory>().statApi }
    single<TripApi> { get<ApiFactory>().tripApi }
    single<TruckApi> { get<ApiFactory>().truckApi }
    single<UserApi> { get<ApiFactory>().userApi }
    single<VinsApi> { get<ApiFactory>().vinsApi }

    // ConversationStateManager service for shared messaging state
    singleOf(::ConversationStateManager)

    // Duty status + proximity watcher (drives location tracking lifecycle)
    singleOf(::LoadProximityWatcher)
    singleOf(::DutyStatusManager)
    singleOf(::AiLoadFinderSelectionStore)

    viewModelOf(::DashboardViewModel)
    viewModelOf(::AccountViewModel)
    viewModelOf(::AiLoadFinderViewModel)
    viewModelOf(::AiLoadBoardDetailViewModel)
    viewModelOf(::LoadDetailViewModel)
    viewModelOf(::PastLoadsViewModel)
    viewModelOf(::MyLicensesViewModel)
    viewModelOf(::StatsViewModel)
    viewModelOf(::LoginViewModel)
    viewModelOf(::SettingsViewModel)
    viewModelOf(::ConversationListViewModel)
    viewModelOf(::EmployeeSelectViewModel)
    viewModelOf(::TripsViewModel)
    viewModelOf(::TripDetailViewModel)
    viewModelOf(::ChatViewModel)
    viewModelOf(::PodCaptureViewModel)
    viewModelOf(::ConditionReportViewModel)
    viewModelOf(::DvirFormViewModel)
    viewModelOf(::PrivacyViewModel)
}
