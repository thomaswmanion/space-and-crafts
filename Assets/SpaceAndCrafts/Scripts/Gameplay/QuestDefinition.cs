using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceAndCrafts.Gameplay
{
    public enum QuestStepKind
    {
        CollectCount = 0,
        BuildInvention = 1,
        DefeatBoss = 2,
        FindSecrets = 3,
        CompleteLevel = 4,
        CollectItem = 5,
        UseInvention = 6,
        ReachCheckpoint = 7
    }

    [Serializable]
    public sealed class QuestStep
    {
        public QuestStepKind kind = QuestStepKind.CollectCount;
        public string label = "Do the thing";
        public string targetId = string.Empty;
        public int requiredCount = 1;
    }

    [Serializable]
    public sealed class QuestDefinition
    {
        public string id = "quest";
        public string title = "New Quest";
        public bool isSideQuest;
        public List<QuestStep> steps = new();
    }
}
