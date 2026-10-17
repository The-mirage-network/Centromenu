using CentroFeatures.modules.host;
using CentroFeatures.modules.misc;
using CentroFeatures.modules.protections;
using CentroFeatures.modules.roles;
using CentroFeatures.modules.self;
using CentroFeatures.modules.spoofer;
using CentroFeatures.modules.troll;
using CentroFeatures.modules.visuals;
using System;
using System.Collections.Generic;
using System.Text.Json;
using UnityEngine;

namespace CentroFeatures.modules
{
	internal class ModuleManager : MonoBehaviour
	{

		public static AssignRoles assignRoles = new AssignRoles();
		public static BanMidGame banMidGame = new BanMidGame();
		public static BlockLowLevels blockLowLevels = new BlockLowLevels();
		public static DisableGameEnd disableGameEnd = new DisableGameEnd();
		public static DisableMeetings disableMeetings = new DisableMeetings();
		public static DisableVentClean disableVentClean = new DisableVentClean();
		public static FakeShapeshiftBubble fakeShapeshiftBubble = new FakeShapeshiftBubble();
		public static FlipSkeld flipSkeld = new FlipSkeld();
		public static HostActionRelay hostActionRelay = new HostActionRelay();
		public static TempBanAll tempBanAll = new TempBanAll();
		public static VoteImmune voteImmune = new VoteImmune();


		public static ChatLogger chatLogger = new ChatLogger();
		public static Whisper whisper = new Whisper();


		public static AntiCrash antiCrash = new AntiCrash();
		public static AntiKick antiKick = new AntiKick();
		public static AntiOverload antiOverload = new AntiOverload();
		public static BlockServerTeleports blockServerTeleports = new BlockServerTeleports();
		public static BlockUnauthorizedUpdates blockUnauthorizedUpdates = new BlockUnauthorizedUpdates();
		public static BypassDisconnectPenalty bypassDisconnectPenalty = new BypassDisconnectPenalty();
		public static BypassShapeshiftRatelimits bypassShapeshiftRatelimits = new BypassShapeshiftRatelimits();
		public static ForceDTLs forceDtls = new ForceDTLs();


		public static MoveInVents moveInVents = new MoveInVents();
		public static NoKillChecks noKillChecks = new NoKillChecks();
		public static NoSabotageCooldown noSabotageCooldown = new NoSabotageCooldown();
		public static NoShapeshiftAnimation noShapeshiftAnimation = new NoShapeshiftAnimation();
		public static UnlockSabotageButton unlockSabotageButton = new UnlockSabotageButton();
		public static VentAsCrewmate ventAsCrewmate = new VentAsCrewmate();


		public static AlwaysShowTaskAnimations alwaysShowTaskAnimations = new AlwaysShowTaskAnimations();
		public static Immortality immortality = new Immortality();
		public static NoLadderCooldown noLadderCooldown = new NoLadderCooldown();
		public static NoZiplineCooldown noZiplineCooldown = new NoZiplineCooldown();
		public static SpeedModifier speedModifier = new SpeedModifier();
		public static UnlimitedMeetings unlimitedMeetings = new UnlimitedMeetings();
		public static UpdateStatsFreeplay updateStatsFreeplay = new UpdateStatsFreeplay();


		public static SpoofDevice spoofDevice = new SpoofDevice();
		public static SpoofLevel spoofLevel = new SpoofLevel();
		public static SpoofVersion spoofVersion = new SpoofVersion();


		public static AutoExposeImpostors autoExposeImpostors = new AutoExposeImpostors();
		public static AutoReportBodies autoReportBodies = new AutoReportBodies();
		public static CrashLobby crashLobby = new CrashLobby();
		public static DisableCameras disableCameras = new DisableCameras();
		public static DisableCloseDoors disableCloseDoors = new DisableCloseDoors();
		public static DisableSabotages disableSabotages = new DisableSabotages();
		public static DisableVents disableVents = new DisableVents();


		public static AccurateDisconnectReason accurateDisconnectReason = new AccurateDisconnectReason();
		public static AlwaysVisibleChat alwaysVisibleChat = new AlwaysVisibleChat();
		public static Fullbright fullbright = new Fullbright();
		public static NoSeekerAnimation noSeekerAnimation = new NoSeekerAnimation();
		public static ShowAnonymousVotes showAnonymousVotes = new ShowAnonymousVotes();
		public static ShowGhostMessages showGhostMessages = new ShowGhostMessages();
		public static ShowGhosts showGhosts = new ShowGhosts();
		public static ShowProtections showProtections = new ShowProtections();
		public static SkipShhhAnimation skipShhhAnimation = new SkipShhhAnimation();
		public static SpectatePlayer spectatePlayer = new SpectatePlayer();

		public readonly Module[] moduleList;

		public ModuleManager()
		{
			moduleList = [
				assignRoles,
				banMidGame,
				blockLowLevels,
				disableGameEnd,
				disableMeetings,
				disableVentClean,
				fakeShapeshiftBubble,
				flipSkeld,
				hostActionRelay,
				tempBanAll,
				voteImmune,

				chatLogger,
				whisper,

				antiCrash,
				antiKick,
				antiOverload,
				blockServerTeleports,
				blockUnauthorizedUpdates,
				bypassDisconnectPenalty,
				bypassShapeshiftRatelimits,
				forceDtls,

				moveInVents,
				noKillChecks,
				noSabotageCooldown,
				noShapeshiftAnimation,
				unlockSabotageButton,
				ventAsCrewmate,

				alwaysShowTaskAnimations,
				immortality,
				noLadderCooldown,
				noZiplineCooldown,
				speedModifier,
				unlimitedMeetings,
				updateStatsFreeplay,

				spoofDevice,
				spoofLevel,
				spoofVersion,

				autoExposeImpostors,
				autoReportBodies,
				crashLobby,
				disableCameras,
				disableCloseDoors,
				disableSabotages,
				disableVents,

				accurateDisconnectReason,
				alwaysVisibleChat,
				fullbright,
				noSeekerAnimation,
				showAnonymousVotes,
				showGhostMessages,
				showGhosts,
				showProtections,
				skipShhhAnimation,
				spectatePlayer
			];
		}


		public Dictionary<string, Dictionary<string, JsonElement>> GetConfigData()
		{
			Dictionary<string, Dictionary<string, JsonElement>> moduleConfig = new Dictionary<string, Dictionary<string, JsonElement>>();

			foreach(Module module in moduleList)
			{
				moduleConfig.Add(module.name, module.GetConfigData());
			}

			return moduleConfig;
		}

		public void LoadConfigData(Dictionary<string, Dictionary<string, JsonElement>> moduleConfig)
		{
			foreach((string moduleName, Dictionary<string, JsonElement> configData) in moduleConfig)
			{
				int moduleIndex = Array.FindIndex(moduleList, r => r.name == moduleName);
				if(moduleIndex == -1)
				{
					Centro.Log.LogWarning($"Config has entry for module {moduleName} when there is no such module");
					continue;
				}

				Module module = moduleList[moduleIndex];
				module.LoadConfigData(configData);
			}
		}


		public void Update()
		{

			if(PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.Data == null) return;

			if(unlockSabotageButton.SabotageAsCrewmate) HudManager.Instance.SabotageButton.gameObject.SetActive(true);
			if(ventAsCrewmate.Enabled) HudManager.Instance.ImpostorVentButton.gameObject.SetActive(true);





			if(alwaysVisibleChat.Enabled)
			{
				HudManager.Instance.MatchInfoButton.gameObject.SetActive(MeetingHud.Instance != null);
			}
		}
	}
}
