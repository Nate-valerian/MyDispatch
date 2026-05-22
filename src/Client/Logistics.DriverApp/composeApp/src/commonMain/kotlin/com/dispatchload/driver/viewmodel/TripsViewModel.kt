package com.dispatchload.driver.viewmodel

import com.dispatchload.driver.api.TripApi
import com.dispatchload.driver.api.bodyOrThrow
import com.dispatchload.driver.api.models.TripDto
import com.dispatchload.driver.service.PreferencesManager
import com.dispatchload.driver.viewmodel.base.BaseViewModel
import com.dispatchload.driver.viewmodel.base.UiState
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow

class TripsViewModel(
    private val tripApi: TripApi,
    private val preferencesManager: PreferencesManager
) : BaseViewModel() {

    private val _uiState = MutableStateFlow<UiState<List<TripDto>>>(UiState.Loading)
    val uiState: StateFlow<UiState<List<TripDto>>> = _uiState.asStateFlow()

    init {
        loadTrips()
    }

    private fun loadTrips() {
        launchWithState(_uiState) {
            val truckId = preferencesManager.getTruckId()
            val response = tripApi.getTrips(truckId = truckId, orderBy = "-CreatedAt").bodyOrThrow()
            response.items ?: emptyList()
        }
    }

    fun refresh() {
        loadTrips()
    }
}
