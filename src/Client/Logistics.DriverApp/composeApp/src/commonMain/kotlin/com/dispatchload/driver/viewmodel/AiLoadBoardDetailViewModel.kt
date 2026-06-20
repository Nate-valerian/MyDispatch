package com.dispatchload.driver.viewmodel

import com.dispatchload.driver.api.DriverApi
import com.dispatchload.driver.api.ApiException
import com.dispatchload.driver.api.LoadBoardApi
import com.dispatchload.driver.api.bodyOrThrow
import com.dispatchload.driver.api.models.LoadBoardBookingRequest
import com.dispatchload.driver.api.models.LoadBoardBookingResultDto
import com.dispatchload.driver.api.models.RouteLoadBoardListingDto
import com.dispatchload.driver.service.PreferencesManager
import com.dispatchload.driver.viewmodel.base.BaseViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update

data class AiLoadBoardDetailUiState(
    val listing: RouteLoadBoardListingDto? = null,
    val isBooking: Boolean = false,
    val bookingResult: LoadBoardBookingResultDto? = null,
    val errorMessage: String? = null
)

class AiLoadBoardDetailViewModel(
    private val loadBoardApi: LoadBoardApi,
    private val driverApi: DriverApi,
    private val preferencesManager: PreferencesManager,
    selectionStore: AiLoadFinderSelectionStore,
    private val listingKey: String
) : BaseViewModel() {

    private val _uiState = MutableStateFlow(
        AiLoadBoardDetailUiState(
            listing = selectionStore.get(listingKey),
            errorMessage = if (selectionStore.get(listingKey) == null) {
                "Load details expired. Search again to reopen this load."
            } else {
                null
            }
        )
    )
    val uiState: StateFlow<AiLoadBoardDetailUiState> = _uiState.asStateFlow()

    fun bookLoad() {
        val routeListing = _uiState.value.listing ?: return
        val listingId = routeListing.listing.id
        if (listingId.isNullOrBlank()) {
            _uiState.update { it.copy(errorMessage = "Search again before booking this load.") }
            return
        }

        launchSafely(onError = { e ->
            _uiState.update {
                it.copy(
                    isBooking = false,
                    errorMessage = when {
                        e is ApiException && e.statusCode == 403 ->
                            "Booking requires load-board booking permission. Call or email the broker, then have dispatch book it."
                        else -> e.message ?: "Booking failed"
                    }
                )
            }
        }) {
            _uiState.update { it.copy(isBooking = true, errorMessage = null, bookingResult = null) }

            val userId = preferencesManager.getUserId()
            val dispatcherId = userId
                ?.takeIf { it.isNotBlank() }
                ?.let { driverApi.getDriverByUserId(it).bodyOrThrow().id }
            val truckId = preferencesManager.getTruckId()?.takeIf { it.isNotBlank() }

            if (truckId.isNullOrBlank()) {
                error("Truck context missing. Open the dashboard once, then try booking again.")
            }

            val booking = loadBoardApi.bookLoadBoardListing(
                listingId = listingId,
                loadBoardBookingRequest = LoadBoardBookingRequest(
                    truckId = truckId,
                    dispatcherId = dispatcherId,
                    customerName = routeListing.listing.brokerName,
                    notes = "Driver requested booking from AI Load Finder after broker confirmation."
                )
            ).bodyOrThrow()

            _uiState.update {
                it.copy(
                    isBooking = false,
                    bookingResult = booking,
                    errorMessage = if (booking.success == true) {
                        null
                    } else {
                        booking.errorMessage ?: "Booking failed"
                    }
                )
            }
        }
    }
}
