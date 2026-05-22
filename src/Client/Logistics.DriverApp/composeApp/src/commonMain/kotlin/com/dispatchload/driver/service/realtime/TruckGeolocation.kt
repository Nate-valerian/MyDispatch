package com.dispatchload.driver.service.realtime

import com.dispatchload.driver.api.models.Address
import com.dispatchload.driver.api.models.GeoPoint
import kotlinx.serialization.Serializable

/**
 * Location data to send to the server.
 */
@Serializable
data class TruckGeolocation(
    val truckId: String,
    val tenantId: String,
    val currentLocation: GeoPoint,
    val currentAddress: Address? = null,
    val truckNumber: String? = null,
    val driversName: String? = null
)
