using System;

namespace CentroFeatures.network
{
	public enum OwnerIds
	{

		Everyone = -1,
		Host = -2,

		Self = -3,
		Server = -4
	}

	public enum SpawnType
	{
		SkeldShipStatus = 0,
		MeetingHud = 1,
		LobbyBehavior = 2,
		PlayerControl = 4,
		MiraShipStatus = 5,
		PolusShipStatus = 6,
		DleksShipStatus = 7,
		AirshipShipStatus = 8,
		GameManager = 10,
		NetworkedPlayerInfo = 11,
		VoteBanSystem = 12,
		FungleShipStatus = 13
	}

	[Flags]
	public enum HudOverrideSystemOperation
	{

		Sabotage = 128
	}

	[Flags]
	public enum LifeSuppSystemOperation
	{
		Fix = 16,
		RepairConsole = 64,
		Sabotage = 128
	}

	[Flags]
	public enum ReactorSystemOperation
	{
		Fix = 16,
		UnrepairConsole = 32,
		RepairConsole = 64,
		Sabotage = 128
	}
}
