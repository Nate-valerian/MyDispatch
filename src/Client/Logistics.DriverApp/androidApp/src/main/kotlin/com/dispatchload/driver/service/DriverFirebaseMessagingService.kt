package com.dispatchload.driver.service

import android.app.NotificationChannel
import android.app.NotificationManager
import android.app.PendingIntent
import android.content.Intent
import androidx.core.app.NotificationCompat
import com.google.firebase.messaging.FirebaseMessagingService
import com.google.firebase.messaging.RemoteMessage
import com.dispatchload.driver.MainActivity
import com.dispatchload.driver.R
import com.dispatchload.driver.api.DriverApi
import com.dispatchload.driver.api.models.SetDriverDeviceTokenCommand
import com.dispatchload.driver.util.Logger
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.SupervisorJob
import kotlinx.coroutines.launch
import org.koin.android.ext.android.inject

class DriverFirebaseMessagingService : FirebaseMessagingService() {
    private val driverApi: DriverApi by inject()
    private val preferencesManager: PreferencesManager by inject()

    private val serviceScope = CoroutineScope(Dispatchers.IO + SupervisorJob())

    companion object {
        private const val CHANNEL_ID = "dispatchload_driver_channel"
        private const val NOTIFICATION_ID = 2001
    }

    override fun onNewToken(token: String) {
        super.onNewToken(token)
        Logger.d("New FCM token: $token")

        // Send token to server
        serviceScope.launch {
            try {
                val userId = preferencesManager.getUserId()
                if (userId != null) {
                    driverApi.setDriverDeviceToken(
                        userId,
                        SetDriverDeviceTokenCommand(userId, token)
                    )
                    Logger.d("Device token sent to server")
                } else {
                    Logger.w("User ID not available, cannot send device token")
                }
            } catch (e: Exception) {
                Logger.e("Failed to send device token", e)
            }
        }
    }

    override fun onMessageReceived(message: RemoteMessage) {
        super.onMessageReceived(message)

        Logger.d("FCM message received from: ${message.from}")

        val conversationId = message.data["conversationId"]

        // Handle notification payload (sent by backend for all push types)
        message.notification?.let { notification ->
            val title = notification.title ?: "DispatchLoad Driver"
            val body = notification.body ?: "New notification"
            showNotification(title, body, conversationId)
        }

        // Handle data-only payloads (app in foreground or no notification block)
        if (message.notification == null && message.data.isNotEmpty()) {
            Logger.d("Message data payload: ${message.data}")
            handleDataPayload(message.data)
        }
    }

    private fun handleDataPayload(data: Map<String, String>) {
        when (data["type"]) {
            "load_update" -> {
                Logger.d("Load update notification received")
            }

            "new_load" -> {
                showNotification(
                    title = "New Load Assigned",
                    body = "You have been assigned a new load"
                )
            }

            "message" -> {
                val conversationId = data["conversationId"]
                // Notification body already shown via message.notification payload in onMessageReceived;
                // this branch handles data-only messages as a fallback.
                if (conversationId != null) {
                    showNotification(
                        title = "New Message",
                        body = "You have a new message",
                        conversationId = conversationId
                    )
                }
            }

            else -> {
                Logger.d("Unknown notification type: ${data["type"]}")
            }
        }
    }

    private fun showNotification(title: String, body: String, conversationId: String? = null) {
        createNotificationChannel()

        val intent = Intent(this, MainActivity::class.java).apply {
            flags = Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_ACTIVITY_CLEAR_TASK
            conversationId?.let { putExtra(MainActivity.EXTRA_CONVERSATION_ID, it) }
        }

        val notificationId = conversationId?.hashCode() ?: NOTIFICATION_ID

        val pendingIntent = PendingIntent.getActivity(
            this,
            notificationId,
            intent,
            PendingIntent.FLAG_IMMUTABLE or PendingIntent.FLAG_UPDATE_CURRENT
        )

        val notification = NotificationCompat.Builder(this, CHANNEL_ID)
            .setSmallIcon(R.drawable.ic_launcher_foreground)
            .setContentTitle(title)
            .setContentText(body)
            .setPriority(NotificationCompat.PRIORITY_HIGH)
            .setAutoCancel(true)
            .setContentIntent(pendingIntent)
            .build()

        val notificationManager = getSystemService(NotificationManager::class.java)
        notificationManager.notify(notificationId, notification)
    }

    private fun createNotificationChannel() {
        val channel = NotificationChannel(
            CHANNEL_ID,
            "DispatchLoad Notifications",
            NotificationManager.IMPORTANCE_HIGH
        ).apply {
            description = "Notifications for load updates and assignments"
        }

        val notificationManager = getSystemService(NotificationManager::class.java)
        notificationManager.createNotificationChannel(channel)
    }
}
