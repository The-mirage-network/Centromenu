using BepInEx;
using CentroFeatures.anticheat;
using CentroFeatures.ui;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace CentroFeatures.modules
{
	internal class ConfigManager
	{
		public readonly string CONFIG_PATH = Path.Combine(Paths.ConfigPath, "Centromenu", "ImportedFeatures");

		public readonly List<string> configList = new List<string>();
		public string currentConfig = "Imported Features";

		public class ConfigData
		{
			public MainUI.MainUIConfig Menu { get; set; }
			public Dictionary<string, Dictionary<string, JsonElement>> Modules { get; set; }
			public Dictionary<string, Dictionary<string, JsonElement>> Routines { get; set; }
			public Anticheat.AnticheatConfigData Anticheat { get; set; }
		}

		public void Initialize()
		{
			if(!Directory.Exists(CONFIG_PATH))
			{
				Centro.Log.LogInfo("No config folder was found, creating...");
				Directory.CreateDirectory(CONFIG_PATH);

				configList.Add(currentConfig);
				SaveConfig(currentConfig);
				return;
			}

			string[] configFiles = Directory.GetFiles(CONFIG_PATH, "*.json");
			Centro.Log.LogInfo($"Discovered {configFiles.Length} config files");

			foreach(string file in configFiles)
			{
				configList.Add(Path.GetFileNameWithoutExtension(file));
			}

			// Keep the imported settings isolated from standalone Centro installs.
			if(!configList.Contains(currentConfig))
			{
				configList.Add(currentConfig);
				SaveConfig(currentConfig);
				return;
			}

			// Load the default config
			LoadConfig(currentConfig);
		}

		public string GetConfigPath(string configName)
		{
			return Path.Combine(CONFIG_PATH, configName + ".json");
		}

		public void LoadConfig(string configName)
		{
			string configLocation = GetConfigPath(configName);
			if(!File.Exists(configLocation))
			{
				Centro.Log.LogWarning($"Tried to load config {configName} when no such config exists");
				// Let's just carry on with our current config
				return;
			}

			string configString = File.ReadAllText(configLocation);

			ConfigData configData;
			try
			{
				configData = JsonSerializer.Deserialize<ConfigData>(configString);
			}
			catch
			{
				Centro.Log.LogError($"Failed to load config at {configLocation}");
				return;
			}

			Centro.mainUI.LoadConfigData(configData.Menu);
			Centro.modules.LoadConfigData(configData.Modules);
			Centro.routines.LoadConfigData(configData.Routines);
			Anticheat.LoadConfigData(configData.Anticheat);

			currentConfig = configName;
			Centro.Log.LogInfo($"Loaded config {configName}");
		}

		public void SaveConfig(string configName)
		{
			string configLocation = GetConfigPath(configName);

			ConfigData configData = new ConfigData();
			configData.Menu = Centro.mainUI.GetConfigData();
			configData.Modules = Centro.modules.GetConfigData();
			configData.Routines = Centro.routines.GetConfigData();
			configData.Anticheat = Anticheat.GetConfigData();

			JsonSerializerOptions serializerOptions = new JsonSerializerOptions();
			serializerOptions.WriteIndented = true;

			string configString = JsonSerializer.Serialize(configData, serializerOptions);
			File.WriteAllText(configLocation, configString);

			Centro.Log.LogInfo($"Config {configName} has been saved to {configLocation}");
		}

		public string GetUnusedConfigName()
		{
			HashSet<string> configHashList = configList.ToHashSet();
			string match = null;

			// https://stackoverflow.com/a/27289807
			char[] digits = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
			string baseConfigName = currentConfig.TrimEnd(digits).Trim();

			for(int i = 1; i < 255; i++)
			{
				string configName = baseConfigName + " " + i;
				if(configHashList.Contains(configName)) continue;

				match = configName;
				break;
			}

			return match;
		}

		public void CreateNewConfig(string configName)
		{
			// First save our old config
			SaveConfig(currentConfig);

			// Then create our new config
			configList.Add(configName);
			SaveConfig(configName);
			currentConfig = configName;
		}
	}
}
