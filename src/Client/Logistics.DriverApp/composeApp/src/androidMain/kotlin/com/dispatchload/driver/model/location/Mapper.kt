package com.dispatchload.driver.model.location

import android.location.Address
import android.location.Location
import com.dispatchload.driver.api.models.GeoPoint
import com.dispatchload.driver.api.models.Address as AddressDto

fun Address?.toAddressDto(): AddressDto = AddressDto(
    line1 = this?.thoroughfare ?: "",
    line2 = this?.subThoroughfare ?: "",
    city = this?.locality ?: "",
    zipCode = this?.postalCode ?: "",
    state = this?.adminArea ?: "",
    country = this?.countryName ?: "",
)

fun Location.toGeoPoint(): GeoPoint = GeoPoint(
    latitude = this.latitude,
    longitude = this.longitude
)
