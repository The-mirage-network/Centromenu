using Hazel;

namespace CentroFeatures.network
{

	internal class RPCEmitter
	{


		public static void SendPlayAnimation(byte animation)
		{

			PlayerControl.LocalPlayer.PlayAnimation(animation);

			MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(
				PlayerControl.LocalPlayer.NetId,
				(byte)RpcCalls.PlayAnimation,
				SendOption.None,
				-1
			);

			writer.Write(animation);

			AmongUsClient.Instance.FinishRpcImmediately(writer);
		}
	}
}
