using Hazel;

namespace CentroFeatures.network
{
	// Shorthand, and more convenient way, of sending RPCs without having to create a new batch through BatchedMessage
	internal class RPCEmitter
	{
		// The PlayerControl::RpcPlayAnimation function does not send the RPC if visual tasks are off
		// If we want the task animation to show up even if visual tasks are enabled, then we will need to reimplement it
		public static void SendPlayAnimation(byte animation)
		{
			// Render the task animation for ourselves
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