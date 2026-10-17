using Hazel;
using System;

namespace CentroFeatures.anticheat
{
	internal abstract class RpcCheck : ICheck
	{
		public bool Enabled { get; set; } = true;

		public virtual bool Validate(PlayerControl player, MessageReader reader)
		{
			return true;
		}

		public abstract RpcCalls GetId();

		public virtual bool IsHostOnly()
		{
			return false;
		}

		public virtual Type GetExpectedNetObject()
		{


			return typeof(PlayerControl);
		}
	}
}
