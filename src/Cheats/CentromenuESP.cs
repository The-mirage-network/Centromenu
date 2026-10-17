using UnityEngine;
using Sentry.Internal.Extensions;

namespace Centromenu;
public static class CentromenuESP
{
    private static bool _freecamActive;
    private static bool _resolutionChangeNeeded;
    private static float _zoomTarget = 3f;
    public static void SporeCloudVision(Mushroom mushroom)
    {
        if (CheatToggles.noShadows)
        {


            mushroom.sporeMask.transform.position = new Vector3(mushroom.sporeMask.transform.position.x, mushroom.sporeMask.transform.position.y, -1);
            return;
        }


        mushroom.sporeMask.transform.position = new Vector3(mushroom.sporeMask.transform.position.x, mushroom.sporeMask.transform.position.y, 5f);
    }

    public static bool IsFullbrightActive()
    {



        return CheatToggles.noShadows || (PlayerControl.LocalPlayer?.Data && PlayerControl.LocalPlayer.Data.IsDead) || Camera.main.orthographicSize > 3f || Camera.main.gameObject.GetComponent<FollowerCamera>().Target != PlayerControl.LocalPlayer;
    }

    public static void ZoomOut(HudManager hudManager)
    {
        if (CheatToggles.zoomOut)
        {

            if (hudManager.Chat.IsOpenOrOpening || MatchInfoGuide.Instance.IsActive || PlayerCustomizationMenu.Instance ||
            (Utils.isLobby && (FriendsListUI.Instance.IsOpen || GameStartManager.Instance.LobbyInfoPane.LobbyViewSettingsPane.gameObject.active || GameStartManager.Instance.RulesEditPanel))) return;

            _resolutionChangeNeeded = true;

            var scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll < 0f) _zoomTarget = Mathf.Min(18f, _zoomTarget + 1f);
            else if (scroll > 0f) _zoomTarget = Mathf.Max(3f, _zoomTarget - 1f);

            var nextSize = CheatToggles.smoothZoom
                ? Mathf.Lerp(Camera.main.orthographicSize, _zoomTarget, 12f * Time.unscaledDeltaTime)
                : _zoomTarget;

            if (Mathf.Abs(Camera.main.orthographicSize - nextSize) > 0.001f)
            {
                Camera.main.orthographicSize = nextSize;
                hudManager.UICamera.orthographicSize = nextSize;
                Utils.AdjustResolution();
            }
        }
        else
        {

            Camera.main.orthographicSize = 3f;
            hudManager.UICamera.orthographicSize = 3f;
            _zoomTarget = 3f;


            if (_resolutionChangeNeeded)
            {
                Utils.AdjustResolution();
                _resolutionChangeNeeded = false;
            }
        }
    }

    public static void MeetingNametags(MeetingHud meetingHud)
    {
        try
        {
            foreach (var playerState in meetingHud.playerStates)
            {

                var data = GameData.Instance.GetPlayerById(playerState.PlayerId);

                if (data.IsNull() || data.Disconnected || data.Outfits[PlayerOutfitType.Default].IsNull()) continue;


                playerState.NameText.text = Utils.GetNameTag(data, data.DefaultOutfit.PlayerName);


                if (CheatToggles.seeRoles && CheatToggles.seePlayerInfo)
                {
                    playerState.NameText.transform.localPosition = new Vector3(0.33f, 0.08f, 0f);
                    playerState.NameText.transform.localScale = new Vector3(0.75f, 0.75f, 0.75f);
                }
                else if (CheatToggles.seeRoles || CheatToggles.seePlayerInfo)
                {
                    playerState.NameText.transform.localPosition = new Vector3(0.3384f, 0.1125f, -0.1f);
                    playerState.NameText.transform.localScale = new Vector3(0.9f, 1f, 1f);
                }
                else
                {

                    playerState.NameText.transform.localPosition = new Vector3(0.3384f, 0.0311f, -0.1f);
                    playerState.NameText.transform.localScale = new Vector3(0.9f, 1f, 1f);
                }
            }
        } catch { }
    }

    public static void PlayerNametags(PlayerPhysics playerPhysics)
    {
        try
        {
            string outfitName = playerPhysics.myPlayer.CurrentOutfit.PlayerName;
            playerPhysics.myPlayer.cosmetics.SetName(Utils.GetNameTag(playerPhysics.myPlayer.Data, outfitName));




            if (playerPhysics.myPlayer.AmOwner && NameSpoofManager.IsOwner(playerPhysics.myPlayer.Data, outfitName))
            {
                playerPhysics.myPlayer.cosmetics.nameText.enabled = true;
                playerPhysics.myPlayer.cosmetics.nameText.gameObject.SetActive(true);
            }


            if (CheatToggles.seeRoles && CheatToggles.seePlayerInfo)
            {
                playerPhysics.myPlayer.cosmetics.nameText.transform.localPosition = new Vector3(0f, 0.186f, 0f);
            }
            else if (CheatToggles.seeRoles || CheatToggles.seePlayerInfo)
            {
                playerPhysics.myPlayer.cosmetics.nameText.transform.localPosition = new Vector3(0f, 0.093f, 0f);
            }
            else
            {
                playerPhysics.myPlayer.cosmetics.nameText.transform.localPosition = new Vector3(0f, 0f, 0f);
            }
        } catch { }
    }

    public static void ChatNametags(ChatBubble chatBubble)
    {
        try
        {

            chatBubble.NameText.text = Utils.GetNameTag(chatBubble.playerInfo, chatBubble.NameText.text, true);


            chatBubble.NameText.ForceMeshUpdate(true, true);
            chatBubble.Background.size = new Vector2(5.52f, 0.2f + chatBubble.NameText.GetNotDumbRenderedHeight() + chatBubble.TextArea.GetNotDumbRenderedHeight());
            chatBubble.MaskArea.size = chatBubble.Background.size - new Vector2(0f, 0.03f);

        } catch { }
    }

    public static void SeeGhostsCheat(PlayerPhysics playerPhysics)
    {
        try{

            if(playerPhysics.myPlayer.Data.IsDead && !PlayerControl.LocalPlayer.Data.IsDead)
            {
                playerPhysics.myPlayer.Visible = CheatToggles.seeGhosts;
            }

        }catch{}
    }

    public static void FreecamCheat()
    {
        if (CheatToggles.freecam)
        {

            if (!_freecamActive)
            {

                Camera.main.gameObject.GetComponent<FollowerCamera>().enabled = false;
                Camera.main.gameObject.GetComponent<FollowerCamera>().Target = null;

                _freecamActive = true;

            }


            PlayerControl.LocalPlayer.moveable = false;


            var movement = new Vector3(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"), 0.0f);



            Camera.main.transform.position = Camera.main.transform.position + movement * 10f * Time.deltaTime;

        }
        else
        {

            if (!_freecamActive) return;
            PlayerControl.LocalPlayer.moveable = true;
            Camera.main.gameObject.GetComponent<FollowerCamera>().enabled = true;
            Camera.main.gameObject.GetComponent<FollowerCamera>().SetTarget(PlayerControl.LocalPlayer);
            _freecamActive = false;
        }
    }
}
