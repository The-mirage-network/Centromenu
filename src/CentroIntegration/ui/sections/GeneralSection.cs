using CentroFeatures.modules;
using UnityEngine;

namespace CentroFeatures.ui.sections
{
	internal class GeneralSection : Section
	{
		public GeneralSection() : base("General") { }

		public override void Render() {
			GUILayout.Label("Additional utility and moderation features imported into CentroMenu. Features that CentroMenu already provides remain in their original tabs and are not duplicated here.");

			ModuleManager.chatLogger.Enabled = GUILayout.Toggle(ModuleManager.chatLogger.Enabled, "Log chat messages to console");
			Centro.notifications.disableNotifications = GUILayout.Toggle(Centro.notifications.disableNotifications, "Disable Notifications");

			if(GUILayout.Button("Clear Notifications"))
			{
				Centro.notifications.ClearNotifications();
				Centro.notifications.Send("Notifications", "All notifications have been cleared.", 5);
			}

			if(GUILayout.Button("Test Notification"))
			{
				Centro.notifications.Send("CentroMenu", "Notifications are working.", 5f);
			}
		}
	}
}
