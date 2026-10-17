using CentroFeatures.network;
using Il2CppInterop.Runtime;
using System.Collections.Generic;

namespace CentroFeatures
{
	internal class Sabotage
	{






		public static bool UpdateSystemsDirectly { get; set; } = true;

		public static readonly Dictionary<string, SystemTypes> skeldSabotages = new Dictionary<string, SystemTypes>()
		{
			{ "Reactor", SystemTypes.Reactor },
			{ "Oxygen", SystemTypes.LifeSupp },
			{ "Lights", SystemTypes.Electrical },
			{ "Communications", SystemTypes.Comms }
		};

		public static readonly Dictionary<string, SystemTypes> skeldDoors = new Dictionary<string, SystemTypes>()
		{
			{ "Cafeteria", SystemTypes.Cafeteria },
			{ "Storage", SystemTypes.Storage },
			{ "Medbay", SystemTypes.MedBay },
			{ "Security", SystemTypes.Security },
			{ "Upper Engine", SystemTypes.UpperEngine },
			{ "Lower Engine", SystemTypes.LowerEngine },
			{ "Electrical", SystemTypes.Electrical }
		};

		public static readonly Dictionary<string, SystemTypes> miraSabotages = new Dictionary<string, SystemTypes>()
		{
			{ "Reactor", SystemTypes.Reactor },
			{ "Oxygen", SystemTypes.LifeSupp },
			{ "Lights", SystemTypes.Electrical },
			{ "Communications", SystemTypes.Comms }
		};

		public static readonly Dictionary<string, SystemTypes> polusSabotages = new Dictionary<string, SystemTypes>()
		{
			{ "Reactor", SystemTypes.Laboratory },
			{ "Lights", SystemTypes.Electrical },
			{ "Communications", SystemTypes.Comms }
		};

		public static readonly Dictionary<string, SystemTypes> polusDoors = new Dictionary<string, SystemTypes>()
		{
			{ "Office", SystemTypes.Office },
			{ "Communications", SystemTypes.Comms },
			{ "Laboratory", SystemTypes.Laboratory },
			{ "Decontamination", SystemTypes.Decontamination },
			{ "Electrical", SystemTypes.Electrical },
			{ "Oxygen", SystemTypes.LifeSupp },
			{ "Weapons", SystemTypes.Weapons },
			{ "Storage", SystemTypes.Storage }
		};

		public static readonly Dictionary<string, SystemTypes> airshipSabotages = new Dictionary<string, SystemTypes>()
		{
			{ "Reactor", SystemTypes.HeliSabotage },
			{ "Lights", SystemTypes.Electrical },
			{ "Communications", SystemTypes.Comms }
		};

		public static readonly Dictionary<string, SystemTypes> airshipDoors = new Dictionary<string, SystemTypes>()
		{
			{ "Brig", SystemTypes.Brig },
			{ "Records", SystemTypes.Records },
			{ "Communications", SystemTypes.Comms },
			{ "Main Hall", SystemTypes.MainHall },
			{ "Kitchen", SystemTypes.Kitchen },
			{ "Medical", SystemTypes.Medical },
			{ "Lounge", SystemTypes.Lounge }
		};

		public static readonly Dictionary<string, SystemTypes> fungleSabotages = new Dictionary<string, SystemTypes>()
		{
			{ "Reactor", SystemTypes.Reactor },
			{ "Communications", SystemTypes.Comms },
			{ "Mushroom Mixup", SystemTypes.MushroomMixupSabotage }
		};

		public static readonly Dictionary<string, SystemTypes> fungleDoors = new Dictionary<string, SystemTypes>()
		{
			{ "Storage", SystemTypes.Storage },
			{ "Kitchen", SystemTypes.Kitchen },
			{ "Laboratory", SystemTypes.Laboratory },
			{ "Lookout", SystemTypes.Lookout },
			{ "Mining Pit", SystemTypes.MiningPit },
			{ "Communications", SystemTypes.Comms },
			{ "Reactor", SystemTypes.Reactor }
		};

		public static Dictionary<string, SystemTypes> GetSabotages()
		{
			MapNames map = Utilities.GetCurrentMap();
			return map switch
			{
				MapNames.Skeld or MapNames.Dleks => skeldSabotages,
				MapNames.MiraHQ => miraSabotages,
				MapNames.Polus => polusSabotages,
				MapNames.Airship => airshipSabotages,
				MapNames.Fungle => fungleSabotages,

				_ => skeldSabotages,
			};
		}

		public static Dictionary<string, SystemTypes> GetDoors()
		{
			MapNames map = Utilities.GetCurrentMap();
			return map switch
			{
				MapNames.Skeld or MapNames.Dleks => skeldDoors,

				MapNames.MiraHQ => [],
				MapNames.Polus => polusDoors,
				MapNames.Airship => airshipDoors,
				MapNames.Fungle => fungleDoors,

				_ => skeldDoors,
			};
		}



		public static bool CanUnlockDoors()
		{
			MapNames map = Utilities.GetCurrentMap();
			return AmongUsClient.Instance.AmHost || map == MapNames.Polus || map == MapNames.Airship || map == MapNames.Fungle;
		}

		public static void SabotageSystem(SystemTypes system, int targetClientId = -2)
		{
			if(!UpdateSystemsDirectly)
			{
				ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Sabotage, (byte)system);
				return;
			}

			BatchedMessage batch = new BatchedMessage(targetClientId == -2 ? AmongUsClient.Instance.HostId : targetClientId);

			switch(system)
			{
				case SystemTypes.Reactor:
				case SystemTypes.Laboratory:
				case SystemTypes.HeliSabotage:
				case SystemTypes.LifeSupp:
				case SystemTypes.Comms:
					batch.QueueUpdateSystem(PlayerControl.LocalPlayer, system, 128);
					break;



				case SystemTypes.Electrical:
					byte amount = 4;

					for(byte i = 0; i < 5; i++)
					{
						if(BoolRange.Next(0.5f))
						{
							amount |= (byte)(1 << i);
						}
					}

					batch.QueueUpdateSystem(PlayerControl.LocalPlayer, SystemTypes.Electrical, (byte)(amount | 128));
					break;

				case SystemTypes.MushroomMixupSabotage:
					batch.QueueUpdateSystem(PlayerControl.LocalPlayer, system, (byte)MushroomMixupSabotageSystem.Operation.TriggerSabotage);
					break;

				default:
					Centro.Log.LogError($"Attempted to sabotage unknown system {system}");
					break;
			}

			batch.FinishBatch();
		}

		public static void FixSabotage(SystemTypes system, int targetClientId = -2)
		{
			BatchedMessage batch = new BatchedMessage(targetClientId == -2 ? AmongUsClient.Instance.HostId : targetClientId);

			switch(system)
			{


				case SystemTypes.Reactor:
				case SystemTypes.Laboratory:
				case SystemTypes.LifeSupp:
					batch.QueueUpdateSystem(PlayerControl.LocalPlayer, system, 16);
					break;


				case SystemTypes.Comms:
				case SystemTypes.HeliSabotage:
					batch.QueueUpdateSystem(PlayerControl.LocalPlayer, system, 16 | 0);
					batch.QueueUpdateSystem(PlayerControl.LocalPlayer, system, 16 | 1);
					break;

				case SystemTypes.Electrical:
					SwitchSystem switches = ShipStatus.Instance.Systems[SystemTypes.Electrical].Cast<SwitchSystem>();


					int amount = switches.ActualSwitches ^ switches.ExpectedSwitches;

					if(amount == 0)
					{
						Centro.Log.LogInfo($"Attempted to fix lights, XOR operation is 0 so that means we have nothing to fix");
						break;
					}





					batch.QueueUpdateSystem(PlayerControl.LocalPlayer, SystemTypes.Electrical, (byte)(amount | 128));
					break;

				case SystemTypes.MushroomMixupSabotage:
					if(!AmongUsClient.Instance.AmHost)
					{
						Centro.Log.LogInfo("Attempted to fix Mushroom Mixup, we are not the host so nothing can be done");
						break;
					}

					MushroomMixupSabotageSystem mixupSystem = ShipStatus.Instance.Systems[SystemTypes.MushroomMixupSabotage].Cast<MushroomMixupSabotageSystem>();

					if(!mixupSystem.IsActive)
					{
						Centro.Log.LogInfo("Attempted to fix Mushroom Mixup, the sabotage is not enabled so we have nothing to fix");
						break;
					}

					Centro.Log.LogInfo("Attempted to fix Mushroom Mixup, we are the host so it can be fixed");

					mixupSystem.currentSecondsUntilHeal = 0.1f;
					mixupSystem.IsDirty = true;
					break;

				default:
					Centro.Log.LogError($"Attempted to fix unknown sabotage system {system}");
					break;
			}

			batch.FinishBatch();
		}

		public static bool IsSabotageActive(SystemTypes system)
		{
			ShipStatus.Instance.Systems.TryGetValue(system, out ISystemType systemType);
			if(systemType == null) return false;

			IActivatable activableSystem = systemType.TryCast<IActivatable>();
			if(activableSystem == null)
			{
				Centro.Log.LogError($"All sabotage types should extend from IActivatable, but yet {system} doesn't");
				return false;
			}

			return activableSystem.IsActive;
		}

		public static void LockDoor(SystemTypes door)
		{
			ShipStatus.Instance.RpcCloseDoorsOfType(door);
		}

		public static void UnlockDoor(SystemTypes system)
		{
			for(byte i = 0; i < ShipStatus.Instance.AllDoors.Count; i++)
			{
				OpenableDoor door = ShipStatus.Instance.AllDoors[i];
				if(door.Room != system) continue;

				UnlockDoor(door);
			}
		}

		public static void UnlockDoor(int id)
		{
			MapNames currentMap = Utilities.GetCurrentMap();
			if(currentMap != MapNames.Skeld)
			{
				UnlockDoor(ShipStatus.Instance.AllDoors[id]);
				return;
			}


			for(byte i = 0; i < ShipStatus.Instance.AllDoors.Count; i++)
			{
				OpenableDoor door = ShipStatus.Instance.AllDoors[i];
				if(door.Id == id) UnlockDoor(door, i);
			}
		}

		public static void UnlockDoor(OpenableDoor door, int index = 0)
		{
			if(!AmongUsClient.Instance.AmHost)
			{
				ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Doors, (byte)(door.Id | 64));
				return;
			}

			door.SetDoorway(true);

			Il2CppSystem.Type doorType = door.GetIl2CppType();
			if(doorType == Il2CppType.From(typeof(AutoOpenDoor)))
			{
				AutoDoorsSystemType doorSystem = ShipStatus.Instance.Systems[SystemTypes.Doors].Cast<AutoDoorsSystemType>();
				doorSystem.dirtyBits |= 1U << index;
			}
			else if(doorType == Il2CppType.From(typeof(PlainDoor)))
			{
				DoorsSystemType doorSystem = ShipStatus.Instance.Systems[SystemTypes.Doors].Cast<DoorsSystemType>();
				doorSystem.IsDirty = true;
			}
			else
			{
				Centro.Log.LogError($"Door type {doorType.FullName} is unknown, cannot mark it dirty");
			}
		}

		public static void SabotageAll()
		{
			Dictionary<string, SystemTypes> sabotages = GetSabotages();
			foreach(SystemTypes system in sabotages.Values)
			{
				SabotageSystem(system);
			}
		}

		public static void FixAllSabotages()
		{
			Dictionary<string, SystemTypes> sabotages = GetSabotages();
			foreach(SystemTypes system in sabotages.Values)
			{
				FixSabotage(system);
			}
		}

		public static void LockAll()
		{
			Dictionary<string, SystemTypes> doors = GetDoors();

			BatchedMessage batch = new BatchedMessage();

			foreach(SystemTypes door in doors.Values)
			{
				batch.QueueCloseDoors(door);
			}

			batch.FinishBatch();
		}

		public static void UnlockAll()
		{
			for(byte i = 0; i < ShipStatus.Instance.AllDoors.Count; i++)
			{
				UnlockDoor(ShipStatus.Instance.AllDoors[i], i);
			}
		}
	}
}
