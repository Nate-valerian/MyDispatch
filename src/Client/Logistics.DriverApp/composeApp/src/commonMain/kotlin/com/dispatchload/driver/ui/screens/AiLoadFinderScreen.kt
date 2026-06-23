package com.dispatchload.driver.ui.screens

import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.ExperimentalLayoutApi
import androidx.compose.foundation.layout.FlowRow
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.filled.LocalShipping
import androidx.compose.material.icons.filled.Route
import androidx.compose.material.icons.filled.Search
import androidx.compose.material3.AssistChip
import androidx.compose.material3.Button
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.FilterChip
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.material3.pulltorefresh.PullToRefreshBox
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.unit.dp
import com.dispatchload.driver.api.models.RouteLoadBoardListingDto
import com.dispatchload.driver.model.DistanceUnit
import com.dispatchload.driver.model.toDisplayString
import com.dispatchload.driver.ui.components.AppTopBar
import com.dispatchload.driver.ui.components.CardContainer
import com.dispatchload.driver.viewmodel.AiLoadFinderViewModel
import com.dispatchload.driver.viewmodel.AiLoadFinderUiState
import org.koin.compose.viewmodel.koinViewModel
import kotlin.math.roundToInt

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun AiLoadFinderScreen(
    onNavigateBack: () -> Unit,
    onLoadBoardListingClick: (String) -> Unit,
    viewModel: AiLoadFinderViewModel = koinViewModel()
) {
    val uiState by viewModel.uiState.collectAsState()

    Scaffold(
        topBar = {
            AppTopBar(
                title = "AI Load Finder",
                navigationIcon = {
                    IconButton(onClick = onNavigateBack) {
                        Icon(Icons.AutoMirrored.Filled.ArrowBack, contentDescription = "Back")
                    }
                }
            )
        }
    ) { paddingValues ->
        PullToRefreshBox(
            isRefreshing = uiState.isSearching && uiState.hasSearched,
            onRefresh = viewModel::searchLoads,
            modifier = Modifier
                .fillMaxSize()
                .padding(paddingValues)
        ) {
        LazyColumn(
            modifier = Modifier.fillMaxSize(),
            contentPadding = PaddingValues(16.dp),
            verticalArrangement = Arrangement.spacedBy(16.dp)
        ) {
            item {
                AiAssistantCard(uiState)
            }

            item {
                RouteSearchCard(
                    uiState = uiState,
                    onOriginChange = viewModel::updateOrigin,
                    onDestinationChange = viewModel::updateDestination,
                    onRadiusChange = viewModel::updateRadius,
                    onDistanceUnitChange = viewModel::updateDistanceUnit,
                    onSearch = viewModel::searchLoads
                )
            }

            uiState.errorMessage?.let { message ->
                item {
                    Text(
                        text = message,
                        color = MaterialTheme.colorScheme.error,
                        style = MaterialTheme.typography.bodyMedium
                    )
                }
            }

            if (uiState.hasSearched && !uiState.isSearching) {
                item {
                    Text(
                        text = "${uiState.listings.size} matching loads",
                        style = MaterialTheme.typography.titleLarge,
                        fontWeight = FontWeight.Bold
                    )
                }
            }

            if (uiState.hasSearched && !uiState.isSearching && uiState.listings.isEmpty()) {
                item {
                    CardContainer {
                        Text(
                            text = "No load-board matches found",
                            style = MaterialTheme.typography.bodyLarge,
                            color = MaterialTheme.colorScheme.onSurfaceVariant,
                            modifier = Modifier
                                .fillMaxWidth()
                                .padding(32.dp)
                        )
                    }
                }
            }

            items(uiState.listings) { listing ->
                RouteLoadBoardListingCard(
                    routeListing = listing,
                    distanceUnit = uiState.distanceUnit,
                    showBrokerInfo = uiState.showBrokerInfo,
                    onClick = {
                        onLoadBoardListingClick(viewModel.selectListing(listing))
                    }
                )
            }
        }
        } // PullToRefreshBox
    }
}

@Composable
private fun AiAssistantCard(uiState: AiLoadFinderUiState) {
    CardContainer {
        Column(
            modifier = Modifier.padding(16.dp),
            verticalArrangement = Arrangement.spacedBy(12.dp)
        ) {
            Row(horizontalArrangement = Arrangement.spacedBy(12.dp)) {
                Icon(
                    imageVector = Icons.Default.Route,
                    contentDescription = null,
                    tint = MaterialTheme.colorScheme.primary
                )
                Column {
                    Text(
                        text = uiState.assistantName,
                        style = MaterialTheme.typography.titleLarge,
                        fontWeight = FontWeight.Bold
                    )
                    Text(
                        text = "Voice: ${uiState.assistantVoice}",
                        style = MaterialTheme.typography.bodyMedium,
                        color = MaterialTheme.colorScheme.onSurfaceVariant
                    )
                }
            }
            Text(
                text = "Searching load boards around your route",
                style = MaterialTheme.typography.bodyMedium,
                color = MaterialTheme.colorScheme.onSurfaceVariant
            )
        }
    }
}

@Composable
private fun RouteSearchCard(
    uiState: AiLoadFinderUiState,
    onOriginChange: (String) -> Unit,
    onDestinationChange: (String) -> Unit,
    onRadiusChange: (String) -> Unit,
    onDistanceUnitChange: (DistanceUnit) -> Unit,
    onSearch: () -> Unit
) {
    CardContainer {
        Column(
            modifier = Modifier.padding(16.dp),
            verticalArrangement = Arrangement.spacedBy(12.dp)
        ) {
            OutlinedTextField(
                value = uiState.originText,
                onValueChange = onOriginChange,
                label = { Text("Route start") },
                placeholder = { Text("Dallas, TX") },
                modifier = Modifier.fillMaxWidth(),
                singleLine = true
            )

            OutlinedTextField(
                value = uiState.destinationText,
                onValueChange = onDestinationChange,
                label = { Text("Route end") },
                placeholder = { Text("Atlanta, GA") },
                modifier = Modifier.fillMaxWidth(),
                singleLine = true
            )

            Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                OutlinedTextField(
                    value = uiState.radiusText,
                    onValueChange = onRadiusChange,
                    label = { Text("Radius") },
                    keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Number),
                    modifier = Modifier.weight(1f),
                    singleLine = true
                )

                FilterChip(
                    selected = uiState.distanceUnit == DistanceUnit.MILES,
                    onClick = { onDistanceUnitChange(DistanceUnit.MILES) },
                    label = { Text("mi") }
                )

                FilterChip(
                    selected = uiState.distanceUnit == DistanceUnit.KILOMETERS,
                    onClick = { onDistanceUnitChange(DistanceUnit.KILOMETERS) },
                    label = { Text("km") }
                )
            }

            Button(
                onClick = onSearch,
                enabled = !uiState.isSearching,
                modifier = Modifier.fillMaxWidth()
            ) {
                if (uiState.isSearching) {
                    CircularProgressIndicator(
                        modifier = Modifier
                            .height(18.dp)
                            .width(18.dp),
                        strokeWidth = 2.dp
                    )
                } else {
                    Icon(Icons.Default.Search, contentDescription = null)
                }
                Spacer(modifier = Modifier.width(8.dp))
                Text(if (uiState.isSearching) "Searching" else "Find loads")
            }
        }
    }
}

@OptIn(ExperimentalLayoutApi::class)
@Composable
private fun RouteLoadBoardListingCard(
    routeListing: RouteLoadBoardListingDto,
    distanceUnit: DistanceUnit,
    showBrokerInfo: Boolean,
    onClick: () -> Unit
) {
    val listing = routeListing.listing

    CardContainer(modifier = Modifier.clickable(onClick = onClick)) {
        Column(
            modifier = Modifier.padding(16.dp),
            verticalArrangement = Arrangement.spacedBy(10.dp)
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
                    if (showBrokerInfo) {
                        Text(
                            text = listing.brokerName ?: listing.providerName ?: "Load board",
                            style = MaterialTheme.typography.bodyMedium,
                            color = MaterialTheme.colorScheme.onSurfaceVariant
                        )
                    }
                }
            }

            FlowRow(
                horizontalArrangement = Arrangement.spacedBy(8.dp),
                verticalArrangement = Arrangement.spacedBy(8.dp)
            ) {
                routeListing.fitScore?.let {
                    ListingMetricChip("$it fit")
                }
                routeListing.distanceFromRoute?.let {
                    ListingMetricChip("${it.roundToInt()} ${distanceUnit.abbreviation} off route")
                }
                listing.totalRate?.let {
                    ListingMetricChip(formatWholeDollars(it))
                }
                listing.ratePerMile?.let {
                    ListingMetricChip("${formatRate(it)}/mi")
                }
                listing.distance?.let {
                    ListingMetricChip("${it.roundToInt()} mi")
                }
            }

            Text(
                text = listOfNotNull(
                    listing.equipmentType,
                    listing.commodity,
                    listing.pickupDateStart?.toString()?.take(10)
                ).joinToString(" | ").ifBlank { "Details pending" },
                style = MaterialTheme.typography.bodyMedium,
                color = MaterialTheme.colorScheme.onSurfaceVariant
            )
        }
    }
}

@Composable
private fun ListingMetricChip(text: String) {
    AssistChip(
        onClick = {},
        label = {
            Text(
                text = text,
                maxLines = 1,
                overflow = TextOverflow.Clip,
                softWrap = false
            )
        }
    )
}

private fun formatWholeDollars(value: Double): String = "$${value.roundToInt()}"

private fun formatRate(value: Double): String {
    val cents = (value * 100).roundToInt()
    val dollars = cents / 100
    val remainder = cents % 100
    return "$$dollars.${remainder.toString().padStart(2, '0')}"
}
