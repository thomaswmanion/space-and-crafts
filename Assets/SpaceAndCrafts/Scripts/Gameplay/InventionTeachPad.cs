using SpaceAndCrafts.Art;
using SpaceAndCrafts.Core;
using SpaceAndCrafts.Crafting;
using SpaceAndCrafts.Player;
using SpaceAndCrafts.UI;
using UnityEngine;

namespace SpaceAndCrafts.Gameplay
{
    /// <summary>
    /// Worlds 02–10 teach volume: after the world invention is crafted, stepping on the pad
    /// applies a real traversal beat (yank / launch / float / speed boost) — not decoration only.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class InventionTeachPad : MonoBehaviour
    {
        public enum TraversalBeat
        {
            VineYank,
            MagnetPull,
            BubbleFloat,
            BridgeBoost,
            TorchDash,
            BoomLaunch,
            VacuumSpit,
            StarFloat,
            GlassBoost,
        }

        [SerializeField] private string inventionId = "vine_spooler";
        [SerializeField] private TraversalBeat beat = TraversalBeat.VineYank;
        [SerializeField] private string successToast = "Pingline Spooler — ping, latch, and zip!";
        [SerializeField] private string lockedToast = "Craft the world invention first!";
        [SerializeField] private Transform targetAnchor;
        [SerializeField] private float cooldown = 1.25f;
        [SerializeField] private float launchSpeed = 18f;
        [SerializeField] private float boostMultiplier = 1.55f;
        [SerializeField] private float boostDuration = 1.6f;
        [SerializeField] private float floatDuration = 1.4f;

        private float _nextReadyTime;

        public string InventionId => inventionId;
        public TraversalBeat Beat => beat;

        private void Reset()
        {
            var col = GetComponent<Collider>();
            col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other) => TryTeach(other);

        private void TryTeach(Collider other)
        {
            if (Time.time < _nextReadyTime)
            {
                return;
            }

            var motor = other.GetComponentInParent<SplatMotor>();
            if (motor == null)
            {
                return;
            }

            if (!PlayerHasInvention())
            {
                GameplayFeedback.Toast(lockedToast);
                TempCraftSparkBurst.Spawn(transform.position + Vector3.up * 0.5f, 4, 0.25f);
                _nextReadyTime = Time.time + cooldown * 0.6f;
                return;
            }

            ApplyBeat(motor);
            InventionRemixDirector.Ensure().NotifyPadActivated(inventionId + ":" + beat);
            if (!string.IsNullOrEmpty(successToast))
            {
                GameplayFeedback.Toast(successToast);
            }

            TempCraftSparkBurst.Spawn(transform.position + Vector3.up * 0.55f, 8, 0.35f);
            PulseInventionVisual();
            _nextReadyTime = Time.time + cooldown;
        }

        private void PulseInventionVisual()
        {
            var presenters = Object.FindObjectsByType<InventionIdlePresenter>();
            for (var i = 0; i < presenters.Length; i++)
            {
                var p = presenters[i];
                if (p != null && (string.IsNullOrEmpty(p.InventionId) || p.InventionId == inventionId))
                {
                    p.PulseActivate();
                    return;
                }
            }
        }

        private bool PlayerHasInvention()
        {
            if (string.IsNullOrWhiteSpace(inventionId))
            {
                return false;
            }

            var session = FindAnyObjectByType<LevelSession>();
            return session != null && session.HasInvention(inventionId);
        }

        private void ApplyBeat(SplatMotor motor)
        {
            switch (beat)
            {
                case TraversalBeat.VineYank:
                    ApplyVineYank(motor);
                    break;
                case TraversalBeat.MagnetPull:
                    ApplyPullTowardAnchor(motor, launchSpeed);
                    break;
                case TraversalBeat.BubbleFloat:
                    motor.ApplyBriefFloat(floatDuration, 3.2f);
                    break;
                case TraversalBeat.BridgeBoost:
                    motor.ApplyTemporaryMoveBoost(boostMultiplier, boostDuration);
                    motor.ApplyLaunch(ResolveForward(motor) * (launchSpeed * 0.55f) + Vector3.up * 4f);
                    TryDeployBridge();
                    break;
                case TraversalBeat.TorchDash:
                    motor.ApplyLaunch(ResolveForward(motor) * launchSpeed + Vector3.up * 6f);
                    motor.ApplyTemporaryMoveBoost(boostMultiplier, boostDuration * 0.75f);
                    TryFireTorch();
                    break;
                case TraversalBeat.BoomLaunch:
                    motor.ApplyLaunch(Vector3.up * launchSpeed);
                    break;
                case TraversalBeat.VacuumSpit:
                    motor.ApplyLaunch(ResolveForward(motor) * launchSpeed + Vector3.up * 5f);
                    break;
                case TraversalBeat.StarFloat:
                    motor.ApplyBriefFloat(floatDuration, 4f);
                    TryPlantStarSeed(motor);
                    break;
                case TraversalBeat.GlassBoost:
                    motor.ApplyTemporaryMoveBoost(boostMultiplier, boostDuration);
                    motor.ApplyLaunch(ResolveForward(motor) * (launchSpeed * 0.7f) + Vector3.up * 8f);
                    TryRaiseShield();
                    break;
            }
        }

        private void ApplyVineYank(SplatMotor motor)
        {
            var anchor = ResolveAnchor();
            if (anchor == null)
            {
                motor.ApplyLaunch(Vector3.up * launchSpeed + ResolveForward(motor) * 6f);
                return;
            }

            var spooler = FindAnyObjectByType<VineSpooler>();
            if (spooler != null)
            {
                spooler.AttachOwner(motor.transform);
                spooler.LatchTo(anchor.position);
                return;
            }

            var to = anchor.position - motor.transform.position;
            motor.ApplyLaunch(to.normalized * launchSpeed + Vector3.up * 2f);
        }

        private void ApplyPullTowardAnchor(SplatMotor motor, float speed)
        {
            var anchor = ResolveAnchor();
            if (anchor == null)
            {
                motor.ApplyLaunch(ResolveForward(motor) * speed + Vector3.up * 4f);
                return;
            }

            var to = anchor.position - motor.transform.position;
            motor.ApplyLaunch(to.normalized * speed + Vector3.up * 1.5f);
        }

        private Transform ResolveAnchor()
        {
            if (targetAnchor != null)
            {
                return targetAnchor;
            }

            var named = GameObject.Find(DefaultAnchorName(beat));
            return named != null ? named.transform : null;
        }

        private static string DefaultAnchorName(TraversalBeat beatType)
        {
            switch (beatType)
            {
                case TraversalBeat.VineYank: return "ART_VineAnchor";
                case TraversalBeat.MagnetPull: return "ART_MagnetTarget";
                case TraversalBeat.BubbleFloat: return "ART_BubbleLane";
                case TraversalBeat.BridgeBoost: return "ART_BridgeGap";
                case TraversalBeat.TorchDash: return "ART_MeltLane";
                case TraversalBeat.BoomLaunch: return "ART_BoomCatch";
                case TraversalBeat.VacuumSpit: return "ART_VacuumSpit";
                case TraversalBeat.StarFloat: return "ART_StarSeedSpot";
                case TraversalBeat.GlassBoost: return "ART_GlassBeam";
                default: return "ART_TeachAnchor";
            }
        }

        private Vector3 ResolveForward(SplatMotor motor)
        {
            var anchor = ResolveAnchor();
            if (anchor != null)
            {
                var to = anchor.position - motor.transform.position;
                to.y = 0f;
                if (to.sqrMagnitude > 0.01f)
                {
                    return to.normalized;
                }
            }

            var planar = motor.PlanarVelocity;
            if (planar.sqrMagnitude > 0.25f)
            {
                return planar.normalized;
            }

            var fwd = motor.transform.forward;
            fwd.y = 0f;
            return fwd.sqrMagnitude > 0.01f ? fwd.normalized : Vector3.forward;
        }

        private void TryDeployBridge()
        {
            var bug = FindAnyObjectByType<WindUpBridgeBug>();
            if (bug != null)
            {
                bug.Deploy();
            }
        }

        private void TryFireTorch()
        {
            var torch = FindAnyObjectByType<HeatTapeTorch>();
            if (torch != null)
            {
                torch.Fire();
            }
        }

        private void TryPlantStarSeed(SplatMotor motor)
        {
            var planter = FindAnyObjectByType<StarSeedPlanter>();
            if (planter == null)
            {
                return;
            }

            var spot = ResolveAnchor();
            var point = spot != null
                ? spot.position
                : motor.transform.position + ResolveForward(motor) * 3f + Vector3.up * 0.2f;
            planter.Plant(point);
        }

        private void TryRaiseShield()
        {
            var shield = FindAnyObjectByType<GlassSailShield>();
            if (shield != null)
            {
                shield.Raise();
            }
        }
    }
}
