using Logistics.Application.Abstractions.Notifications;
using Logistics.Domain.Primitives.Enums;

namespace Logistics.TelegramBot.Services;

internal sealed class NoOpTelegramNotificationService : ITelegramNotificationService
{
    public Task SendNotificationAsync(
        Guid tenantId,
        string title,
        string message,
        TelegramChatRole? targetRole = null,
        CancellationToken ct = default) =>
        Task.CompletedTask;
}
