using Hazel;
using System;

namespace CentroFeatures.anticheat.rpc
{
	internal class CloseDoorsOfType : RpcCheck
	{

		public override bool Validate(PlayerControl player, MessageReader reader)
		{


			if(GameManager.Instance.IsHideAndSeek())
			{
				Anticheat.Flag("Someone attempted to close doors while in Hide and Seek.");
				return false;
			}

			return true;
		}

		public override RpcCalls GetId()
		{
			return RpcCalls.CloseDoorsOfType;
		}

		public override Type GetExpectedNetObject()
		{
			return typeof(ShipStatus);
		}
	}
}
