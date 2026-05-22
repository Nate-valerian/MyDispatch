package com.dispatchload.driver.viewmodel

import com.dispatchload.driver.api.EmployeeApi
import com.dispatchload.driver.api.bodyOrThrow
import com.dispatchload.driver.api.models.DriverLicenseDto
import com.dispatchload.driver.service.PreferencesManager
import com.dispatchload.driver.viewmodel.base.BaseViewModel
import com.dispatchload.driver.viewmodel.base.UiState
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow

class MyLicensesViewModel(
    private val employeeApi: EmployeeApi,
    private val preferencesManager: PreferencesManager
) : BaseViewModel() {

    private val _uiState = MutableStateFlow<UiState<List<DriverLicenseDto>>>(UiState.Loading)
    val uiState: StateFlow<UiState<List<DriverLicenseDto>>> = _uiState.asStateFlow()

    init {
        loadLicenses()
    }

    private fun loadLicenses() {
        launchWithState(_uiState) {
            val userId = preferencesManager.getUserId()
            if (userId.isNullOrEmpty()) {
                throw IllegalStateException("User ID not available")
            }
            // Show revoked entries on the driver app too — drivers see their own
            // history. Sorting: soonest expiry first so warnings surface up top.
            val licenses = employeeApi
                .getDriverLicenses(userId, includeRevoked = true)
                .bodyOrThrow()
            licenses.sortedBy { it.expiresAt }
        }
    }

    fun refresh() {
        loadLicenses()
    }
}
