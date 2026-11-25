using System.Collections.Generic;
using SpaceAndCrafts.Crafting;
using SpaceAndCrafts.Core;
using SpaceAndCrafts.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpaceAndCrafts.Gameplay
{
    /// <summary>
    /// Soft Odyssey-style quest tracking over collect / craft / boss / secrets.
    /// Use/checkpoint objective substeps restore from the selected world on Continue;
    /// presentation is handled by QuestBoardHud.
    /// </summary>
    public sealed class WorldQuestDirector : MonoBehaviour
    {
        [SerializeField] private List<QuestDefinition> quests = new();

        private readonly Dictionary<string, int> _stepProgress = new();
        private readonly Dictionary<string, int> _collectibleCounts = new();
        private readonly Dictionary<string, int> _usedInventionCounts = new();
        private readonly HashSet<string> _reachedCheckpointIds = new();
        private readonly HashSet<string> _claimedWonderIds = new();
        private readonly HashSet<string> _completedQuests = new();
        private int _collectCount;
        private int _secretCount;
        private bool _bossDefeated;
        private bool _levelDone;

        public IReadOnlyList<QuestDefinition> Quests => quests;

        public void Configure(List<QuestDefinition> definitions)
        {
            quests = definitions ?? new List<QuestDefinition>();
            _stepProgress.Clear();
            _collectibleCounts.Clear();
            _usedInventionCounts.Clear();
            _reachedCheckpointIds.Clear();
            _claimedWonderIds.Clear();
            _completedQuests.Clear();
            _collectCount = 0;
            _secretCount = 0;
            _bossDefeated = false;
            _levelDone = false;
            RecomputeAll();
        }

        public static WorldQuestDirector Ensure()
        {
            var existing = FindAnyObjectByType<WorldQuestDirector>();
            if (existing != null)
            {
                return existing;
            }

            var systems = GameObject.Find("ArrivalSystems")
                ?? GameObject.Find("WorldSystems")
                ?? new GameObject("SYS_Quests");
            return systems.GetComponent<WorldQuestDirector>() ?? systems.AddComponent<WorldQuestDirector>();
        }

        private void OnEnable()
        {
            GameEvents.CollectibleAcquired += HandleCollect;
            GameEvents.BuildCompleted += HandleBuild;
            GameEvents.InventionUsed += HandleInventionUsed;
            GameEvents.CheckpointReached += HandleCheckpointReached;
            GameEvents.LevelCompleted += HandleLevelComplete;
            GameEvents.SecretWonderClaimed += HandleSecret;
            GameEvents.LevelRestartRequested += HandleRestart;
        }

        private void OnDisable()
        {
            GameEvents.CollectibleAcquired -= HandleCollect;
            GameEvents.BuildCompleted -= HandleBuild;
            GameEvents.InventionUsed -= HandleInventionUsed;
            GameEvents.CheckpointReached -= HandleCheckpointReached;
            GameEvents.LevelCompleted -= HandleLevelComplete;
            GameEvents.SecretWonderClaimed -= HandleSecret;
            GameEvents.LevelRestartRequested -= HandleRestart;
        }

        private void Start()
        {
            // Continue restores these per-world quest beats. A new game and an
            // explicit level restart start with fresh use/checkpoint objectives.
            if (PlayerProgressSave.ShouldRestoreSessionProgress)
            {
                RestoreQuestSubsteps();
            }

            // Rebuild objective progress from restored inventory/build state on Continue.
            RecomputeAll();
        }

        private void RestoreQuestSubsteps()
        {
            var sceneId = SceneManager.GetActiveScene().name;
            var usedIds = PlayerProgressSave.GetUsedInventionIds(sceneId);
            for (var i = 0; i < usedIds.Count; i++)
            {
                var inventionId = usedIds[i];
                _usedInventionCounts[inventionId] =
                    PlayerProgressSave.GetInventionUseCount(sceneId, inventionId);
            }

            var checkpointIds = PlayerProgressSave.GetReachedCheckpointIds(sceneId);
            for (var i = 0; i < checkpointIds.Count; i++)
            {
                _reachedCheckpointIds.Add(checkpointIds[i]);
            }

            // Wonder claims are persisted globally, so the registry intersects
            // them with this scene's beacons before restoring the optional quest.
            var wonders = SecretWonderRegistry.Ensure();
            foreach (var wonderId in wonders.ClaimedIds)
            {
                if (_claimedWonderIds.Add(wonderId))
                {
                    _secretCount++;
                }
            }
        }

        public bool IsQuestComplete(string questId) => _completedQuests.Contains(questId);

        public bool HasReachedCheckpoint(string checkpointId) =>
            !string.IsNullOrWhiteSpace(checkpointId) && _reachedCheckpointIds.Contains(checkpointId);

        public int GetStepProgress(string questId, int stepIndex)
        {
            var key = StepKey(questId, stepIndex);
            return _stepProgress.TryGetValue(key, out var value) ? value : 0;
        }

        public bool IsStepComplete(QuestDefinition quest, int stepIndex)
        {
            if (quest == null || stepIndex < 0 || stepIndex >= quest.steps.Count)
            {
                return false;
            }

            var step = quest.steps[stepIndex];
            return GetStepProgress(quest.id, stepIndex) >= Mathf.Max(1, step.requiredCount);
        }

        public string GetActiveBoardLine()
        {
            foreach (var quest in quests)
            {
                if (quest == null || _completedQuests.Contains(quest.id))
                {
                    continue;
                }

                for (var i = 0; i < quest.steps.Count; i++)
                {
                    if (IsStepComplete(quest, i))
                    {
                        continue;
                    }

                    var step = quest.steps[i];
                    var have = GetStepProgress(quest.id, i);
                    var need = Mathf.Max(1, step.requiredCount);
                    var tag = quest.isSideQuest ? "Side" : "Main";
                    return $"[{tag}] {quest.title}: {step.label} ({have}/{need})";
                }
            }

            return _completedQuests.Count > 0 ? "Quests clear — keep exploring!" : string.Empty;
        }

        public string GetActiveSideQuestLines()
        {
            var lines = new List<string>();
            foreach (var quest in quests)
            {
                if (quest == null || !quest.isSideQuest || _completedQuests.Contains(quest.id))
                {
                    continue;
                }

                for (var i = 0; i < quest.steps.Count; i++)
                {
                    if (IsStepComplete(quest, i))
                    {
                        continue;
                    }

                    var step = quest.steps[i];
                    var have = GetStepProgress(quest.id, i);
                    var need = Mathf.Max(1, step.requiredCount);
                    lines.Add($"{GetStepDisplayLabel(quest, step)} ({have}/{need})");
                    break;
                }
            }

            return string.Join("\n", lines);
        }

        private void HandleCollect(CollectibleDefinition definition)
        {
            _collectCount++;
            if (definition != null && !string.IsNullOrWhiteSpace(definition.Id))
            {
                _collectibleCounts.TryGetValue(definition.Id, out var count);
                _collectibleCounts[definition.Id] = count + 1;
            }

            RecomputeAll();
        }

        private void HandleBuild(string inventionId)
        {
            RecomputeAll(builtInventionId: inventionId);
        }

        private void HandleInventionUsed(string inventionId)
        {
            if (string.IsNullOrWhiteSpace(inventionId))
            {
                return;
            }

            // Ignore ambient/demo pads until this invention has actually been built.
            // Otherwise an early launch could pre-complete the later use objective.
            var session = FindAnyObjectByType<LevelSession>();
            if (session == null || !session.HasInvention(inventionId))
            {
                return;
            }

            _usedInventionCounts.TryGetValue(inventionId, out var count);
            _usedInventionCounts[inventionId] = count + 1;
            PlayerProgressSave.RecordInventionUse(SceneManager.GetActiveScene().name, inventionId);
            RecomputeAll();
        }

        private void HandleLevelComplete()
        {
            _bossDefeated = true;
            _levelDone = true;
            RecomputeAll();
        }

        private void HandleCheckpointReached(string checkpointId)
        {
            if (!string.IsNullOrWhiteSpace(checkpointId) && _reachedCheckpointIds.Add(checkpointId))
            {
                PlayerProgressSave.RecordReachedCheckpoint(SceneManager.GetActiveScene().name, checkpointId);
                RecomputeAll();
            }
        }

        private void HandleSecret(string wonderId)
        {
            if (string.IsNullOrWhiteSpace(wonderId) || !_claimedWonderIds.Add(wonderId))
            {
                return;
            }

            _secretCount++;
            RecomputeAll();
        }

        private void HandleRestart()
        {
            // Restart intentionally abandons saved quest beats for only this
            // scene; other world's Continue progress is left intact.
            PlayerProgressSave.ClearQuestSubstepsForWorld(SceneManager.GetActiveScene().name);
            _stepProgress.Clear();
            _collectibleCounts.Clear();
            _usedInventionCounts.Clear();
            _reachedCheckpointIds.Clear();
            _claimedWonderIds.Clear();
            _completedQuests.Clear();
            _collectCount = 0;
            _secretCount = 0;
            _bossDefeated = false;
            _levelDone = false;
            GameEvents.RaiseQuestBoardChanged();
        }

        private static string GetStepDisplayLabel(QuestDefinition quest, QuestStep step)
        {
            // Older Arrival scenes serialize the original wording directly into the
            // quest list. Keep their live board clear while the scene data is refreshed.
            if (quest != null && quest.id == "arrival_wonder_seeker"
                && step != null && step.kind == QuestStepKind.FindSecrets
                && step.label == "Find secret rainbow hushes")
            {
                return "Find two hidden rainbow wonders";
            }

            return step != null ? step.label : string.Empty;
        }

        private void RecomputeAll(string builtInventionId = null)
        {
            foreach (var quest in quests)
            {
                if (quest == null || string.IsNullOrWhiteSpace(quest.id) || _completedQuests.Contains(quest.id))
                {
                    continue;
                }

                var allDone = quest.steps.Count > 0;
                for (var i = 0; i < quest.steps.Count; i++)
                {
                    var step = quest.steps[i];
                    var key = StepKey(quest.id, i);
                    var value = EvaluateStep(step, builtInventionId);
                    var prev = _stepProgress.TryGetValue(key, out var old) ? old : 0;
                    if (value > prev)
                    {
                        _stepProgress[key] = value;
                        GameEvents.RaiseObjectiveProgressed(quest.id, i, value);
                    }
                    else if (!_stepProgress.ContainsKey(key))
                    {
                        _stepProgress[key] = value;
                    }

                    if (_stepProgress[key] < Mathf.Max(1, step.requiredCount))
                    {
                        allDone = false;
                    }
                }

                if (allDone)
                {
                    _completedQuests.Add(quest.id);
                    GameplayFeedback.Toast($"Quest complete — {quest.title}!");
                    GameEvents.RaiseQuestCompleted(quest.id);
                }
            }

            GameEvents.RaiseQuestBoardChanged();
        }

        private int EvaluateStep(QuestStep step, string builtInventionId)
        {
            switch (step.kind)
            {
                case QuestStepKind.CollectCount:
                    return _collectCount;
                case QuestStepKind.CollectItem:
                {
                    if (string.IsNullOrWhiteSpace(step.targetId))
                    {
                        return 0;
                    }

                    var count = _collectibleCounts.TryGetValue(step.targetId, out var itemCount)
                        ? itemCount
                        : 0;
                    var inventory = FindAnyObjectByType<InventoryComponent>();
                    var restored = PlayerProgressSave.ShouldRestoreSessionProgress
                        && inventory != null && inventory.Has(step.targetId);
                    return Mathf.Max(count, restored ? 1 : 0);
                }
                case QuestStepKind.BuildInvention:
                {
                    if (!string.IsNullOrEmpty(builtInventionId)
                        && (string.IsNullOrEmpty(step.targetId)
                            || InventionCompatibility.Satisfies(step.targetId, builtInventionId)))
                    {
                        return 1;
                    }

                    var progress = PlayerProgressSave.Current;
                    for (var i = 0; PlayerProgressSave.ShouldRestoreSessionProgress
                        && i < progress.unlockedInventionIds.Count; i++)
                    {
                        if (string.IsNullOrEmpty(step.targetId)
                            || InventionCompatibility.Satisfies(step.targetId, progress.unlockedInventionIds[i]))
                        {
                            return 1;
                        }
                    }

                    return GetExistingBuildProgress(step);
                }
                case QuestStepKind.UseInvention:
                    var used = 0;
                    foreach (var pair in _usedInventionCounts)
                    {
                        if (string.IsNullOrEmpty(step.targetId)
                            || InventionCompatibility.Satisfies(step.targetId, pair.Key))
                        {
                            used += pair.Value;
                        }
                    }
                    return used;
                case QuestStepKind.ReachCheckpoint:
                    return !string.IsNullOrWhiteSpace(step.targetId)
                        && _reachedCheckpointIds.Contains(step.targetId) ? 1 : 0;
                case QuestStepKind.DefeatBoss:
                    return _bossDefeated ? 1 : 0;
                case QuestStepKind.FindSecrets:
                    return _secretCount;
                case QuestStepKind.CompleteLevel:
                    return _levelDone ? 1 : 0;
                default:
                    return 0;
            }
        }

        private int GetExistingBuildProgress(QuestStep step)
        {
            // Persist build completion across recomputes within the session.
            foreach (var quest in quests)
            {
                if (quest == null)
                {
                    continue;
                }

                for (var i = 0; i < quest.steps.Count; i++)
                {
                    var s = quest.steps[i];
                    if (s.kind != QuestStepKind.BuildInvention)
                    {
                        continue;
                    }

                    if (!string.IsNullOrEmpty(step.targetId) && s.targetId != step.targetId)
                    {
                        continue;
                    }

                    var key = StepKey(quest.id, i);
                    if (_stepProgress.TryGetValue(key, out var value) && value > 0)
                    {
                        return value;
                    }
                }
            }

            return 0;
        }

        private static string StepKey(string questId, int stepIndex) => $"{questId}:{stepIndex}";
    }
}
