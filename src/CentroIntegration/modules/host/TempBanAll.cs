using AmongUs.GameOptions;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using HarmonyLib;
using CentroFeatures.network;
using InnerNet;
using System.Collections;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace CentroFeatures.modules.host
{
	internal class TempBanAll : Module
	{
		public TempBanAll() : base("TempBanAll") { }

		private static TempBanAll Instance
		{
			get { return ModuleManager.tempBanAll; }
		}







		private readonly int START_GAME_COUNT = 282;







		private IEnumerator Run()
		{




			IGameOptions gameOptions = GameOptions.CreateCloneOptions(GameManager.Instance.LogicOptions.currentGameOptions);
			gameOptions.SetByte(ByteOptionNames.MapId, (byte)MapNames.Fungle);
			GameOptions.SendGameOptionsToClient(gameOptions, -1);



			for(int i = 0; i < START_GAME_COUNT; i++)
			{
				AmongUsClient.Instance.SendStartGame();
			}














			BatchedMessage batch = new BatchedMessage();

			if(LobbyBehaviour.Instance != null)
			{
				batch.QueueDespawn(LobbyBehaviour.Instance);
			}

			if(ShipStatus.Instance == null)
			{
				AsyncOperationHandle<GameObject> asyncHandle = AmongUsClient.Instance.ShipPrefabs[0].InstantiateAsync(null, false);
				yield return asyncHandle;

				ShipStatus ship = asyncHandle.Result.GetComponent<ShipStatus>();
				batch.QueueSpawn(ship, -2, SpawnFlags.None);
			}

			foreach(PlayerControl player in PlayerControl.AllPlayerControls)
			{



				if(player == PlayerControl.LocalPlayer || player.Data == null) continue;

				if(!player.roleAssigned)
				{
					batch.QueueSetRole(player, RoleTypes.Crewmate);
				}

				if(player.myTasks.Count == 0)
				{
					batch.QueueSetTasks(player.Data, [0]);
				}
			}



			ClientData myClient = AmongUsClient.Instance.FindClientById(AmongUsClient.Instance.ClientId);
			if(!myClient.IsReady)
			{
				batch.QueueClientReady(myClient);
			}

			batch.FinishBatch();

			Enabled = false;
		}





		[HarmonyPatch(typeof(AmongUsClient._CoStartGame_d__32), nameof(AmongUsClient._CoStartGame_d__32.MoveNext))]
		class BlockGameStart
		{
			static bool Prefix()
			{
				return !Instance.Enabled;
			}
		}


		private void OnDisconnect()
		{
			Enabled = false;
		}

		protected override void OnEnable()
		{

			if(AmongUsClient.Instance == null)
			{
				_enabled = false;
			}

			if(AmongUsClient.Instance.NetworkMode != NetworkModes.LocalGame && !AmongUsClient.Instance.AmHost)
			{
				Centro.notifications.Send("Temp Ban All", "You need to be the host of the lobby in order to use this feature.");
				Enabled = false;
				return;
			}

			EventCoordinator.OnDisconnect += OnDisconnect;

			AmongUsClient.Instance.StartCoroutine(Run().WrapToIl2Cpp());
		}

		protected override void OnDisable()
		{
			EventCoordinator.OnDisconnect -= OnDisconnect;
		}
	}
}
