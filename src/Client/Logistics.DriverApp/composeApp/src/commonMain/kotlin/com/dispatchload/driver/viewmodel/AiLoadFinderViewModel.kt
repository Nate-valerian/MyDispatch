package com.dispatchload.driver.viewmodel

import com.dispatchload.driver.api.LoadBoardApi
import com.dispatchload.driver.api.bodyOrThrow
import com.dispatchload.driver.api.models.Address
import com.dispatchload.driver.api.models.LoadBoardListingDto
import com.dispatchload.driver.api.models.SearchLoadBoardCommand
import com.dispatchload.driver.model.DistanceUnit
import com.dispatchload.driver.viewmodel.base.BaseViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlin.math.roundToInt

data class AiLoadFinderUiState(
    val assistantName: String = "Mira",
    val assistantVoice: String = "Mira voice",
    val originText: String = "",
    val destinationText: String = "",
    val radiusText: String = "50",
    val distanceUnit: DistanceUnit = DistanceUnit.MILES,
    val isSearching: Boolean = false,
    val hasSearched: Boolean = false,
    val listings: List<LoadBoardListingDto> = emptyList(),
    val errorMessage: String? = null
)

class AiLoadFinderViewModel(
    private val loadBoardApi: LoadBoardApi
) : BaseViewModel() {

    private val _uiState = MutableStateFlow(AiLoadFinderUiState())
    val uiState: StateFlow<AiLoadFinderUiState> = _uiState.asStateFlow()

    fun updateOrigin(value: String) {
        _uiState.update { it.copy(originText = value, errorMessage = null) }
    }

    fun updateDestination(value: String) {
        _uiState.update { it.copy(destinationText = value, errorMessage = null) }
    }

    fun updateRadius(value: String) {
        _uiState.update { it.copy(radiusText = value.filter(Char::isDigit), errorMessage = null) }
    }

    fun updateDistanceUnit(unit: DistanceUnit) {
        _uiState.update { it.copy(distanceUnit = unit, errorMessage = null) }
    }

    fun searchLoads() {
        val state = _uiState.value
        val radius = state.radiusText.toIntOrNull()

        when {
            state.originText.isBlank() -> {
                _uiState.update { it.copy(errorMessage = "Enter a route start") }
                return
            }

            state.destinationText.isBlank() -> {
                _uiState.update { it.copy(errorMessage = "Enter a route end") }
                return
            }

            radius == null || radius <= 0 -> {
                _uiState.update { it.copy(errorMessage = "Enter a search radius") }
                return
            }
        }

        launchSafely(onError = { error ->
            _uiState.update {
                it.copy(
                    isSearching = false,
                    hasSearched = true,
                    errorMessage = error.message ?: "Load search failed"
                )
            }
        }) {
            _uiState.update { it.copy(isSearching = true, errorMessage = null) }

            val radiusMiles = radius.toBackendMiles(state.distanceUnit)
            val result = loadBoardApi.searchLoadBoard(
                SearchLoadBoardCommand(
                    originAddress = state.originText.toAddress(),
                    originRadius = radiusMiles,
                    destinationAddress = state.destinationText.toAddress(),
                    destinationRadius = radiusMiles,
                    maxResults = 25
                )
            ).bodyOrThrow()

            _uiState.update {
                it.copy(
                    isSearching = false,
                    hasSearched = true,
                    listings = result.listings.orEmpty(),
                    errorMessage = null
                )
            }
        }
    }
}

private fun Int.toBackendMiles(unit: DistanceUnit): Int {
    return when (unit) {
        DistanceUnit.MILES -> this
        DistanceUnit.KILOMETERS -> (this * 0.621371).roundToInt()
    }.coerceAtLeast(1)
}

private fun String.toAddress(): Address {
    val parts = split(",").map { it.trim() }.filter { it.isNotBlank() }
    val city = parts.firstOrNull().orEmpty()
    val state = parts.getOrNull(1)
        ?.split(" ")
        ?.firstOrNull()
        ?.uppercase()
        .orEmpty()

    return Address(
        line1 = null,
        city = city,
        zipCode = null,
        state = state,
        country = "US"
    )
}
