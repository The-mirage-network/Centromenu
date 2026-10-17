using CentroFeatures.modules;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CentroFeatures.ui.sections
{
	internal class SpooferSection : Section
	{
		public SpooferSection() : base("Spoofer") { }

		public readonly Dictionary<string, int> versions = new Dictionary<string, int>()
		{
			// Current version at runtime
			// VersionShower::Start uses ReferenceDataManager.Refdata.userFacingVersion to get version strings such as "17.1"
			// however that doesn't seem to before the game fully loads, so we have to use Constants::AddressablesVersion to get a less human-understandable version string
			{ $"{Constants.AddressablesVersion} (Current)", Constants.GetBroadcastVersion() },
			{ "16.1.0", 50632950 },
			{ "17.1", 50643450 },
			{ "17.1.2", 50647000 },
			{ "17.2", 50645050 },
			{ "17.2.1", 50652900 },
			{ "17.2.2", 50653700 },
			{ "17.3", 50652400 },
			{ "17.4", 50656300 },
			{ "18.0", 50663350 },
			{ "18.0 (build 7348)", 50663400 },
			{ "19.0 (build 7488)", 50663650 }, // iOS
			{ "19.0 (build 7489)", 50663600 } // PC
		};

		private int versionSelection = 0;

		public override void Render()
		{
			GUILayout.Label("Version Spoofer:");
			ModuleManager.spoofVersion.Enabled = GUILayout.Toggle(ModuleManager.spoofVersion.Enabled, "Enable Version Spoofing");

			GUILayout.Label($"Spoofed Version: {versions.ElementAt(versionSelection).Key} ({ModuleManager.spoofVersion.spoofedVersion})");
			versionSelection = (int)GUILayout.HorizontalSlider(versionSelection, 0, versions.Count - 1);
			ModuleManager.spoofVersion.spoofedVersion = versions.ElementAt(versionSelection).Value;

			ModuleManager.spoofVersion.UseModdedProtocol = GUILayout.Toggle(ModuleManager.spoofVersion.UseModdedProtocol, "Use Modded Protocol");
		}

		private void ClampSelectedLevel(uint newLevel)
		{
			// Do we really need to have an upper bounds on the level value?
			// I doubt anyone will press the +100 that much anyway
			uint maxLevel = Utilities.IsAnticheatPresent() ? 100001 : uint.MaxValue - 1;

			ModuleManager.spoofLevel.SpoofedLevel = Math.Clamp(newLevel, 0, maxLevel);
		}
	}
}
