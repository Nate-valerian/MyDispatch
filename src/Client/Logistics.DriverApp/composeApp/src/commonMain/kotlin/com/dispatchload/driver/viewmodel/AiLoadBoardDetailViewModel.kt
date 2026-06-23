package com.dispatchload.driver.viewmodel

import com.dispatchload.driver.api.EmployeeApi
import com.dispatchload.driver.api.MessageApi
import com.dispatchload.driver.api.bodyOrThrow
import com.dispatchload.driver.api.models.CreateConversationRequest
import com.dispatchload.driver.api.models.RouteLoadBoardListingDto
import com.dispatchload.driver.api.models.SendMessageRequest
import com.dispatchload.driver.model.toDisplayString
import com.dispatchload.driver.service.PreferencesManager
import com.dispatchload.driver.viewmodel.base.BaseViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlin.math.roundToInt

data class AiLoadBoardDetailUiState(
    val listing: RouteLoadBoardListingDto? = null,
    val isDispatcher: Boolean = false,
    val isRequestingDispatch: Boolean = false,
    val dispatchRequestSent: Boolean = false,
    val dispatchConversationId: String? = null,
    val errorMessage: String? = null
)

class AiLoadBoardDetailViewModel(
    private val messageApi: MessageApi,
    private val employeeApi: EmployeeApi,
    private val preferencesManager: PreferencesManager,
    selectionStore: AiLoadFinderSelectionStore,
    listingKey: String
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

    init {
        launchSafely {
            val role = preferencesManager.getUserRole()
            if (role == "Dispatcher") {
                _uiState.update { it.copy(isDispatcher = true) }
            }
        }
    }

    fun requestDispatchBooking() {
        val routeListing = _uiState.value.listing ?: return

        launchSafely(onError = { e ->
            _uiState.update {
                it.copy(
                    isRequestingDispatch = false,
                    errorMessage = e.message ?: "Could not send the request to dispatch"
                )
            }
        }) {
            _uiState.update {
                it.copy(
                    isRequestingDispatch = true,
                    dispatchRequestSent = false,
                    dispatchConversationId = null,
                    errorMessage = null
                )
            }

            val userId = preferencesManager.getUserId()
                ?.takeIf { it.isNotBlank() }
                ?: error("Driver profile is missing. Sign in again, then try the request.")
            val dispatchers = employeeApi.getEmployees(role = "Dispatcher", pageSize = 20)
                .bodyOrThrow()
                .items
                ?.filter { it.id != userId }
                ?.takeIf { it.isNotEmpty() }
                ?: error("No dispatcher is available to receive this request.")
            val dispatcherId = dispatchers.random().id ?: error("Dispatcher profile is missing.")

            val conversation = messageApi.createConversation(
                CreateConversationRequest(
                    participantIds = listOf(userId, dispatcherId),
                    name = routeListing.conversationName()
                )
            ).bodyOrThrow()
            val conversationId = conversation.id ?: error("Could not open a dispatch conversation.")

            messageApi.sendMessage(
                SendMessageRequest(
                    conversationId = conversationId,
                    content = routeListing.toDispatchRequestMessage()
                )
            ).bodyOrThrow()

            _uiState.update {
                it.copy(
                    isRequestingDispatch = false,
                    dispatchRequestSent = true,
                    dispatchConversationId = conversationId,
                    errorMessage = null
                )
            }
        }
    }
}

private fun RouteLoadBoardListingDto.conversationName(): String {
    val origin = listing.originAddress.let { listOfNotNull(it.city, it.state).joinToString(", ") }.takeIf { it.isNotBlank() }
    val dest = listing.destinationAddress.let { listOfNotNull(it.city, it.state).joinToString(", ") }.takeIf { it.isNotBlank() }
    return if (origin != null && dest != null) "Book: $origin → $dest" else "Load board booking request"
}

private fun RouteLoadBoardListingDto.toDispatchRequestMessage(): String {
    val listing = listing

    return listOf(
        "Please book this load after broker confirmation.",
        "Route: ${listing.originAddress.toDisplayString()} to ${listing.destinationAddress.toDisplayString()}",
        "Broker: ${listing.brokerName ?: "-"}",
        "Phone: ${listing.brokerPhone ?: "-"}",
        "Email: ${listing.brokerEmail ?: "-"}",
        "MC: ${listing.brokerMcNumber ?: "-"}",
        "Rate: ${listing.totalRate.formatDollars()}",
        "Rate per mile: ${listing.ratePerMile.formatRatePerMile()}",
        "Off route: ${distanceFromRoute?.roundToInt()?.let { "$it mi" } ?: "-"}",
        "Equipment: ${listing.equipmentType ?: "-"}",
        "Commodity: ${listing.commodity ?: "-"}",
        "Pickup: ${listing.pickupDateStart?.toString()?.take(10) ?: "-"}",
        "Delivery: ${listing.deliveryDateStart?.toString()?.take(10) ?: "-"}",
        "Listing: ${listing.externalListingId ?: listing.id ?: "-"}"
    ).joinToString("\n")
}

private fun Double?.formatDollars(): String =
    this?.let { "$${it.roundToInt()}" } ?: "-"

private fun Double?.formatRatePerMile(): String =
    this?.let { "${it.formatCurrency()}/mi" } ?: "-"

private fun Double.formatCurrency(): String {
    val cents = (this * 100).roundToInt()
    val dollars = cents / 100
    val remainder = cents % 100
    return "$$dollars.${remainder.toString().padStart(2, '0')}"
}
