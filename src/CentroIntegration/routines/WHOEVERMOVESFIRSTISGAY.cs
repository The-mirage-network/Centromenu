using CentroFeatures.modules;
using System.Collections.Generic;
using UnityEngine;

namespace CentroFeatures.routines;

public sealed class WHOEVERMOVESFIRSTISGAY : Routine
{


    private const float CheckDelay = 0.50f;
    private const float MovementToleranceSquared = 0.0004f;

    private readonly Dictionary<int, Vector2> _startingPositions = new();
    private float _elapsed;

    public WHOEVERMOVESFIRSTISGAY() : base("WHOEVERMOVESFIRSTISGAY") { }

    public override void Run()
    {
        if (ShipStatus.Instance == null)
        {
            Enabled = false;
            return;
        }

        _elapsed += Time.deltaTime;
        if (_elapsed < CheckDelay) return;
        _elapsed = 0f;

        foreach (PlayerControl player in PlayerControl.AllPlayerControls)
        {
            if (player == null || player.Data == null || player.Data.Disconnected) continue;

            int key = player.GetHashCode();
            Vector2 current = player.GetTruePosition();
            if (!_startingPositions.TryGetValue(key, out Vector2 startingPosition))
            {
                _startingPositions[key] = current;
                continue;
            }

            if (Vector2.SqrMagnitude(current - startingPosition) <= MovementToleranceSquared) continue;

            if (player.AmOwner)
            {
                player.NetTransform.RpcSnapTo(startingPosition);
            }
            else if (AmongUsClient.Instance.AmHost)
            {
                Teleporter.TeleportPlayerTo(player, startingPosition);
            }
            else
            {



                Teleporter.TeleportPlayerTo(player, startingPosition);
            }
        }
    }

    protected override void OnEnable()
    {
        if (PlayerControl.LocalPlayer == null || ShipStatus.Instance == null)
        {
            Centro.notifications.Send("Whoever Moves First", "This can only be enabled after the match starts.");
            Enabled = false;
            return;
        }

        _startingPositions.Clear();
        _elapsed = 0f;
        foreach (PlayerControl player in PlayerControl.AllPlayerControls)
        {
            if (player == null || player.Data == null || player.Data.Disconnected) continue;
            _startingPositions[player.GetHashCode()] = player.GetTruePosition();
        }

        EventCoordinator.OnDisconnect += OnDisconnect;
        Centro.notifications.Send("Whoever Moves First", "Starting positions recorded.", 5f);
    }

    protected override void OnDisable()
    {
        _startingPositions.Clear();
        _elapsed = 0f;
        EventCoordinator.OnDisconnect -= OnDisconnect;
    }

    private void OnDisconnect()
    {
        Enabled = false;
    }
}
