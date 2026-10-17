using UnityEngine;

namespace CentroFeatures.ui;

/// <summary>
/// Compatibility adapter that routes imported Centro notifications through
/// CentroMenu's single notification renderer.
/// </summary>
internal class NotificationManager : MonoBehaviour
{
    public bool disableNotifications;

    public void Send(string title, string message, float ttl = 10f)
    {
        Centro.Log?.LogMessage($"[Notification] [{title}] {message}");
        if (disableNotifications)
            return;

        Centromenu.CentroAPI.Notification(title, message, ttl);
    }

    public void ClearNotifications() => Centromenu.CentroAPI.ClearNotifications();
}
