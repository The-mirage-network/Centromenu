using AmongUs.GameOptions;

namespace CentroFeatures.modules.protections
{
	internal class BypassShapeshiftRatelimits : Module
	{


		public BypassShapeshiftRatelimits() : base("BypassShapeshiftRatelimits")
		{
			base.Enabled = true;
		}

		private void OnGameStart()
		{
			if(!AmongUsClient.Instance.AmHost || ModuleManager.tempBanAll.Enabled) return;

			PlayerControl player = Utilities.GetRandomPlayer();
			if(player == null) return;

			IGameOptions options = GameOptions.CreateCloneOptions(GameManager.Instance.LogicOptions.currentGameOptions);
			options.SetFloat(FloatOptionNames.ShapeshifterCooldown, 0.0f);


			GameOptions.SendGameOptionsToClient(options, player.OwnerId);
		}

		protected override void OnEnable()
		{
			EventCoordinator.OnGameStart += OnGameStart;
		}

		protected override void OnDisable()
		{
			EventCoordinator.OnGameStart -= OnGameStart;
		}
	}
}
