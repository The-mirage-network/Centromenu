using UnityEngine;

namespace CentroFeatures.ui;





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
