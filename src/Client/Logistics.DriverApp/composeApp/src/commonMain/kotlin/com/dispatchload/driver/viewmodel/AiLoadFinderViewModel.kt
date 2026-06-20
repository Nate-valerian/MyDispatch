package com.dispatchload.driver.viewmodel

import com.dispatchload.driver.api.LoadBoardApi
import com.dispatchload.driver.api.bodyOrThrow
import com.dispatchload.driver.api.models.RouteLoadBoardListingDto
import com.dispatchload.driver.api.models.SearchRouteLoadBoardCommand
import com.dispatchload.driver.model.DistanceUnit
import com.dispatchload.driver.viewmodel.base.BaseViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import com.dispatchload.driver.api.models.DistanceUnit as ApiDistanceUnit

class AiLoadFinderSelectionStore {
    private val listings = mutableMapOf<String, RouteLoadBoardListingDto>()

    fun put(listing: RouteLoadBoardListingDto): String {
        val key = listing.listing.id
            ?: listing.listing.externalListingId
            ?: "load-board-${listing.hashCode()}"
        listings[key] = listing
        return key
    }

    fun get(key: String): RouteLoadBoardListingDto? = listings[key]
}

data class AiLoadFinderUiState(
    val assistantName: String = "Mira",
    val assistantVoice: String = "Mira voice",
    val originText: String = "",
    val destinationText: String = "",
    val radiusText: String = "50",
    val distanceUnit: DistanceUnit = DistanceUnit.MILES,
    val isSearching: Boolean = false,
    val hasSearched: Boolean = false,
    val listings: List<RouteLoadBoardListingDto> = emptyList(),
    val errorMessage: String? = null
)

class AiLoadFinderViewModel(
    private val loadBoardApi: LoadBoardApi,
    private val selectionStore: AiLoadFinderSelectionStore
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

            val result = loadBoardApi.searchRouteLoadBoard(
                SearchRouteLoadBoardCommand(
                    origin = state.originText.trim(),
                    destination = state.destinationText.trim(),
                    radius = radius,
                    distanceUnit = state.distanceUnit.toApiDistanceUnit(),
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

    fun selectListing(listing: RouteLoadBoardListingDto): String = selectionStore.put(listing)
}

private fun DistanceUnit.toApiDistanceUnit(): ApiDistanceUnit {
    return when (this) {
        DistanceUnit.MILES -> ApiDistanceUnit.MILES
        DistanceUnit.KILOMETERS -> ApiDistanceUnit.KILOMETERS
    }
}
