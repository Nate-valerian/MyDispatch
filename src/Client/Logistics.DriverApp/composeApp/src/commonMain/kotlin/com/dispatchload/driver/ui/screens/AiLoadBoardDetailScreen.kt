package com.dispatchload.driver.ui.screens

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.filled.Call
import androidx.compose.material.icons.filled.CheckCircle
import androidx.compose.material.icons.filled.Email
import androidx.compose.material.icons.filled.LocalShipping
import androidx.compose.material3.Button
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import com.dispatchload.driver.api.models.LoadBoardListingDto
import com.dispatchload.driver.api.models.RouteLoadBoardListingDto
import com.dispatchload.driver.model.toDisplayString
import com.dispatchload.driver.ui.components.AppTopBar
import com.dispatchload.driver.ui.components.CardContainer
import com.dispatchload.driver.ui.components.DetailRow
import com.dispatchload.driver.viewmodel.AiLoadBoardDetailViewModel
import org.koin.compose.viewmodel.koinViewModel
import kotlin.math.roundToInt

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun AiLoadBoardDetailScreen(
    onNavigateBack: () -> Unit,
    onOpenUrl: (String) -> Unit,
    onOpenConversation: (String) -> Unit = {},
    viewModel: AiLoadBoardDetailViewModel = koinViewModel()
) {
    val uiState by viewModel.uiState.collectAsState()
    val routeListing = uiState.listing

    Scaffold(
        topBar = {
            AppTopBar(
                title = "Load Board Load",
                navigationIcon = {
                    IconButton(onClick = onNavigateBack) {
                        Icon(Icons.AutoMirrored.Filled.ArrowBack, contentDescription = "Back")
                    }
                }
            )
        }
    ) { paddingValues ->
        LazyColumn(
            modifier = Modifier
                .fillMaxSize()
                .padding(paddingValues),
            contentPadding = PaddingValues(16.dp),
            verticalArrangement = Arrangement.spacedBy(16.dp)
        ) {
            if (routeListing == null) {
                item {
                    CardContainer {
                        Text(
                            text = uiState.errorMessage ?: "Load details are not available.",
                            modifier = Modifier.padding(16.dp),
                            color = MaterialTheme.colorScheme.error
                        )
                    }
                }
                return@LazyColumn
            }

            item { LoadBoardHeaderCard(routeListing) }
            item {
                BrokerContactCard(
                    listing = routeListing.listing,
                    onCallBroker = { phone -> onOpenUrl("tel:${phone.toTelNumber()}") },
                    onEmailBroker = { email ->
                        onOpenUrl(
                            "mailto:$email?subject=${urlPart("Load ${routeListing.listing.externalListingId}")}"
                        )
                    }
                )
            }
            item { LoadBoardDetailsCard(routeListing) }

            uiState.errorMessage?.let { message ->
                item {
                    Text(
                        text = message,
                        color = MaterialTheme.colorScheme.error,
                        style = MaterialTheme.typography.bodyMedium
                    )
                }
            }

            if (uiState.dispatchRequestSent) {
                item {
                    CardContainer {
                        Column(modifier = Modifier.padding(16.dp)) {
                            Row(horizontalArrangement = Arrangement.spacedBy(12.dp)) {
                                Icon(
                                    imageVector = Icons.Default.CheckCircle,
                                    contentDescription = null,
                                    tint = MaterialTheme.colorScheme.primary
                                )
                                Column(modifier = Modifier.weight(1f)) {
                                    Text(
                                        text = "Sent to dispatch",
                                        style = MaterialTheme.typography.titleMedium,
                                        fontWeight = FontWeight.Bold
                                    )
                                    Text(
                                        text = "Dispatch has the load details and broker contact info.",
                                        style = MaterialTheme.typography.bodyMedium,
                                        color = MaterialTheme.colorScheme.onSurfaceVariant
                                    )
                                }
                            }
                            uiState.dispatchConversationId?.let { conversationId ->
                                TextButton(
                                    onClick = { onOpenConversation(conversationId) },
                                    modifier = Modifier.align(androidx.compose.ui.Alignment.End)
                                ) {
                                    Text("Open conversation")
                                }
                            }
                        }
                    }
                }
            }

            item {
                Button(
                    onClick = viewModel::requestDispatchBooking,
                    enabled = !uiState.isRequestingDispatch && !uiState.dispatchRequestSent,
                    modifier = Modifier.fillMaxWidth()
                ) {
                    if (uiState.isRequestingDispatch) {
                        CircularProgressIndicator(
                            modifier = Modifier
                                .height(18.dp)
                                .width(18.dp),
                            strokeWidth = 2.dp
                        )
                        Spacer(modifier = Modifier.width(8.dp))
                    }
                    Text(
                        when {
                            uiState.isRequestingDispatch -> "Sending request"
                            uiState.dispatchRequestSent -> "Request sent"
                            else -> "Request dispatch to book"
                        }
                    )
                }
            }
        }
    }
}

@Composable
private fun LoadBoardHeaderCard(routeListing: RouteLoadBoardListingDto) {
    val listing = routeListing.listing

    CardContainer {
        Column(
            modifier = Modifier.padding(16.dp),
            verticalArrangement = Arrangement.spacedBy(8.dp)
        ) {
            Row(horizontalArrangement = Arrangement.spacedBy(12.dp)) {
                Icon(
                    imageVector = Icons.Default.LocalShipping,
                    contentDescription = null,
                    tint = MaterialTheme.colorScheme.primary
                )
                Column(modifier = Modifier.weight(1f)) {
                    Text(
                        text = "${listing.originAddress.toDisplayString()} to ${listing.destinationAddress.toDisplayString()}",
                        style = MaterialTheme.typography.titleMedium,
                        fontWeight = FontWeight.Bold
                    )
                    Text(
                        text = listing.brokerName ?: listing.providerName ?: "Load board",
                        style = MaterialTheme.typography.bodyMedium,
                        color = MaterialTheme.colorScheme.onSurfaceVariant
                    )
                }
            }

            DetailRow("Fit", "${routeListing.fitScore} fit")
            DetailRow("Off Route", routeListing.distanceFromRoute?.let { "${it.roundToInt()} mi" } ?: "-")
            DetailRow("Rate", listing.totalRate?.let(::formatWholeDollars) ?: "-")
            DetailRow("Rate Per Mile", listing.ratePerMile?.let { "${formatRate(it)}/mi" } ?: "-")
        }
    }
}

@Composable
private fun BrokerContactCard(
    listing: LoadBoardListingDto,
    onCallBroker: (String) -> Unit,
    onEmailBroker: (String) -> Unit
) {
    val phone = listing.brokerPhone?.takeIf { it.isNotBlank() }
    val email = listing.brokerEmail?.takeIf { it.isNotBlank() }

    CardContainer {
        Column(
            modifier = Modifier.padding(16.dp),
            verticalArrangement = Arrangement.spacedBy(12.dp)
        ) {
            Text(
                text = "Broker",
                style = MaterialTheme.typography.titleMedium,
                fontWeight = FontWeight.Bold
            )
            DetailRow("Name", listing.brokerName ?: "-")
            DetailRow("Phone", phone ?: "-")
            DetailRow("Email", email ?: "-")
            DetailRow("MC", listing.brokerMcNumber ?: "-")

            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.spacedBy(8.dp)
            ) {
                Button(
                    onClick = { phone?.let(onCallBroker) },
                    enabled = phone != null,
                    modifier = Modifier.weight(1f)
                ) {
                    Icon(Icons.Default.Call, contentDescription = null)
                    Spacer(modifier = Modifier.width(8.dp))
                    Text("Call")
                }
                OutlinedButton(
                    onClick = { email?.let(onEmailBroker) },
                    enabled = email != null,
                    modifier = Modifier.weight(1f)
                ) {
                    Icon(Icons.Default.Email, contentDescription = null)
                    Spacer(modifier = Modifier.width(8.dp))
                    Text("Email")
                }
            }
        }
    }
}

@Composable
private fun LoadBoardDetailsCard(routeListing: RouteLoadBoardListingDto) {
    val listing = routeListing.listing

    CardContainer {
        Column(
            modifier = Modifier.padding(16.dp),
            verticalArrangement = Arrangement.spacedBy(8.dp)
        ) {
            Text(
                text = "Load Details",
                style = MaterialTheme.typography.titleMedium,
                fontWeight = FontWeight.Bold
            )
            DetailRow("Equipment", listing.equipmentType ?: "-")
            DetailRow("Commodity", listing.commodity ?: "-")
            DetailRow("Distance", listing.distance?.let { "${it.roundToInt()} mi" } ?: "-")
            DetailRow("Weight", listing.weight?.let { "$it lb" } ?: "-")
            DetailRow("Length", listing.length?.let { "$it ft" } ?: "-")
            DetailRow("Pickup", listing.pickupDateStart?.toString()?.take(10) ?: "-")
            DetailRow("Delivery", listing.deliveryDateStart?.toString()?.take(10) ?: "-")
            DetailRow("Listing", listing.externalListingId ?: "-")
        }
    }
}

private fun String.toTelNumber(): String =
    filter { it.isDigit() || it == '+' }

private fun urlPart(value: String): String =
    value.replace(" ", "%20").replace("#", "%23")

private fun formatWholeDollars(value: Double): String = "$${value.roundToInt()}"

private fun formatRate(value: Double): String {
    val cents = (value * 100).roundToInt()
    val dollars = cents / 100
    val remainder = cents % 100
    return "$$dollars.${remainder.toString().padStart(2, '0')}"
}
