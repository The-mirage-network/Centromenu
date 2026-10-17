using BepInEx.Unity.IL2CPP.Utils.Collections;
using CentroFeatures.network;
using CentroFeatures.ui.sections;
using InnerNet;
using UnityEngine.AddressableAssets;

namespace CentroFeatures.modules.host
{
	internal class FlipSkeld : Module
	{
		public FlipSkeld() : base("FlipSkeld") { }

		private void SwapMapAssets()
		{
			if(AmongUsClient.Instance == null)
			{
				Enabled = false;
				return;
			}





			AssetReference temp = AmongUsClient.Instance.ShipPrefabs[3];
			AmongUsClient.Instance.ShipPrefabs[3] = AmongUsClient.Instance.ShipPrefabs[0];
			AmongUsClient.Instance.ShipPrefabs[0] = temp;
		}









		private void OnNetObjectSpawn(InnerNetObject netObject)
		{
			if(AmongUsClient.Instance.NetworkMode != NetworkModes.LocalGame || AmongUsClient.Instance.AmHost || netObject.SpawnId != (int)SpawnType.SkeldShipStatus) return;
			Centro.Log.LogMessage($"Despawning ship and spawning in a new one");

			BatchedMessage batch = new BatchedMessage();
			batch.QueueDespawn(netObject);
			batch.FinishBatch();

			AmongUsClient.Instance.StartCoroutine(HostSection.SpawnMap(0).WrapToIl2Cpp());
		}

		protected override void OnEnable()
		{
			EventCoordinator.OnNetObjectSpawn += OnNetObjectSpawn;

			SwapMapAssets();
		}

		protected override void OnDisable()
		{
			EventCoordinator.OnNetObjectSpawn -= OnNetObjectSpawn;

			SwapMapAssets();
		}
	}
}
