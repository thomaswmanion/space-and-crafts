using System;
using SpaceAndCrafts.Gameplay;
using UnityEngine;

namespace SpaceAndCrafts.Core
{
    /// <summary>
    /// Decoupled gameplay signals. Presentation and UI subscribe; core logic raises.
    /// Keep payloads serializable-friendly for a future save system.
    /// </summary>
    public static class GameEvents
    {
        public static event Action<CollectibleDefinition> CollectibleAcquired;
        public static event Action<string> BuildCompleted;
        public static event Action<string> InventionUsed;
        public static event Action BuildRefused;
        public static event Action LevelRestartRequested;
        public static event Action LevelCompleted;
        public static event Action GatewaySignalReceived;
        public static event Action PlayerRespawned;
        public static event Action<string> CheckpointReached;
        public static event Action<string> SecretWonderClaimed;
        public static event Action<string, int, int> ObjectiveProgressed;
        public static event Action<string> QuestCompleted;
        public static event Action QuestBoardChanged;
        public static event Action CombatStarted;
        public static event Action CombatEnded;

        public static void RaiseCollectibleAcquired(CollectibleDefinition definition)
        {
            CollectibleAcquired?.Invoke(definition);
        }

        public static void RaiseBuildCompleted(string inventionId)
        {
            BuildCompleted?.Invoke(inventionId);
        }

        public static void RaiseInventionUsed(string inventionId)
        {
            if (!string.IsNullOrWhiteSpace(inventionId))
            {
                InventionUsed?.Invoke(inventionId);
            }
        }

        public static void RaiseBuildRefused()
        {
            BuildRefused?.Invoke();
        }

        public static void RaiseLevelRestartRequested()
        {
            LevelRestartRequested?.Invoke();
        }

        public static void RaiseLevelCompleted()
        {
            LevelCompleted?.Invoke();
        }

        public static void RaiseGatewaySignalReceived()
        {
            GatewaySignalReceived?.Invoke();
        }

        public static void RaisePlayerRespawned()
        {
            PlayerRespawned?.Invoke();
        }

        public static void RaiseCheckpointReached(string checkpointId)
        {
            if (!string.IsNullOrWhiteSpace(checkpointId))
            {
                CheckpointReached?.Invoke(checkpointId);
            }
        }

        public static void RaiseSecretWonderClaimed(string wonderId)
        {
            SecretWonderClaimed?.Invoke(wonderId);
        }

        public static void RaiseObjectiveProgressed(string questId, int stepIndex, int progress)
        {
            ObjectiveProgressed?.Invoke(questId, stepIndex, progress);
        }

        public static void RaiseQuestCompleted(string questId)
        {
            QuestCompleted?.Invoke(questId);
        }

        public static void RaiseQuestBoardChanged()
        {
            QuestBoardChanged?.Invoke();
        }

        public static void RaiseCombatStarted()
        {
            CombatStarted?.Invoke();
        }

        public static void RaiseCombatEnded()
        {
            CombatEnded?.Invoke();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            CollectibleAcquired = null;
            BuildCompleted = null;
            InventionUsed = null;
            BuildRefused = null;
            LevelRestartRequested = null;
            LevelCompleted = null;
            GatewaySignalReceived = null;
            PlayerRespawned = null;
            CheckpointReached = null;
            SecretWonderClaimed = null;
            ObjectiveProgressed = null;
            QuestCompleted = null;
            QuestBoardChanged = null;
            CombatStarted = null;
            CombatEnded = null;
        }
    }
}
