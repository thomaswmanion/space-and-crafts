using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SpaceAndCrafts.Core
{
    /// <summary>
    /// Persistent Odyssey-style progress: worlds, inventions, components, wonders, sparks.
    /// JSON under Application.persistentDataPath.
    /// </summary>
    [Serializable]
    public sealed class PlayerProgressData
    {
        public int version = 5;
        public List<string> completedWorldIds = new();
        public List<string> unlockedInventionIds = new();
        public List<string> claimedWonderIds = new();
        public List<string> discoveryBadgeIds = new();
        public List<string> acquiredComponentIds = new();
        // Quest events are namespaced with the active scene so Continue can
        // restore a world's use/checkpoint steps without bleeding across worlds.
        public List<string> inventionUseProgressKeys = new();
        public List<string> reachedCheckpointProgressKeys = new();
        public List<string> activeCheckpointKeys = new();
        public int sparkCount;
        public int workshopTier;
        public string lastPlayedWorldId = "RainbowWorld_Arrival";
        public string continueSceneId = string.Empty;
    }

    public static class PlayerProgressSave
    {
        private const string LegacyFileName = "space_and_crafts_progress.json";
        private static int _boundSlot = -1;
        private static PlayerProgressData _cache;
        private static bool _restoreSessionProgress;

        public static bool ShouldRestoreSessionProgress => _restoreSessionProgress;

        public static void BeginFreshSession() => _restoreSessionProgress = false;

        public static void BeginContinueSession() => _restoreSessionProgress = true;

        /// <summary>Active slot file, or legacy single-save path when no slot bound.</summary>
        public static string SavePath =>
            _boundSlot >= 0
                ? GameSlotStore.SlotProgressPath(_boundSlot)
                : Path.Combine(Application.persistentDataPath, LegacyFileName);

        public static void BindSlot(int slot)
        {
            _boundSlot = slot;
            _cache = null;
        }

        public static PlayerProgressData Current
        {
            get
            {
                if (_cache == null)
                {
                    _cache = Load();
                }

                return _cache;
            }
        }

        public static PlayerProgressData Load()
        {
            try
            {
                if (File.Exists(SavePath))
                {
                    var json = File.ReadAllText(SavePath);
                    var data = JsonUtility.FromJson<PlayerProgressData>(json);
                    if (data != null)
                    {
                        Normalize(data);
                        _cache = data;
                        return _cache;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Space & Crafts] Save load failed: {ex.Message}");
            }

            _cache = new PlayerProgressData();
            return _cache;
        }

        private static void Normalize(PlayerProgressData data)
        {
            // Older slot files predate component persistence and may deserialize
            // missing lists as null. Keep them safe to read and write.
            data.completedWorldIds ??= new List<string>();
            data.unlockedInventionIds ??= new List<string>();
            data.claimedWonderIds ??= new List<string>();
            data.discoveryBadgeIds ??= new List<string>();
            data.acquiredComponentIds ??= new List<string>();
            data.inventionUseProgressKeys ??= new List<string>();
            data.reachedCheckpointProgressKeys ??= new List<string>();
            data.activeCheckpointKeys ??= new List<string>();
            data.version = Math.Max(data.version, 5);
            if (string.IsNullOrWhiteSpace(data.continueSceneId))
            {
                var lastWorld = string.IsNullOrWhiteSpace(data.lastPlayedWorldId)
                    ? "RainbowWorld_Arrival"
                    : data.lastPlayedWorldId;
                data.continueSceneId = data.completedWorldIds.Contains(lastWorld)
                    ? ResolveCompletionResumeScene(lastWorld)
                    : lastWorld;
            }
        }

        public static void Save()
        {
            if (_cache == null)
            {
                _cache = new PlayerProgressData();
            }

            try
            {
                var json = JsonUtility.ToJson(_cache, true);
                File.WriteAllText(SavePath, json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Space & Crafts] Save write failed: {ex.Message}");
            }
        }

        public static void ResetAll()
        {
            _cache = new PlayerProgressData();
            try
            {
                if (File.Exists(SavePath))
                {
                    File.Delete(SavePath);
                }
            }
            catch
            {
                // ignore
            }
        }

        public static bool TryAddUnique(List<string> list, string id)
        {
            if (string.IsNullOrWhiteSpace(id) || list.Contains(id))
            {
                return false;
            }

            list.Add(id);
            return true;
        }

        public static void RecordBuild(string inventionId)
        {
            var data = Current;
            if (TryAddUnique(data.unlockedInventionIds, inventionId))
            {
                data.workshopTier = Mathf.Max(data.workshopTier, data.unlockedInventionIds.Count);
                Save();
            }
        }

        public static void RecordComponent(string componentId)
        {
            var data = Current;
            if (TryAddUnique(data.acquiredComponentIds, componentId))
            {
                Save();
            }
        }

        public static void RecordInventionUse(string sceneId, string inventionId)
        {
            var key = ScopedProgressKey(sceneId, inventionId);
            if (key == null)
            {
                return;
            }

            // Keep duplicates: repeated uses count toward objectives that need
            // more than one activation.
            Current.inventionUseProgressKeys.Add(key);
            Save();
        }

        public static int GetInventionUseCount(string sceneId, string inventionId)
        {
            var key = ScopedProgressKey(sceneId, inventionId);
            if (key == null)
            {
                return 0;
            }

            var count = 0;
            var keys = Current.inventionUseProgressKeys;
            for (var i = 0; i < keys.Count; i++)
            {
                if (keys[i] == key)
                {
                    count++;
                }
            }

            return count;
        }

        public static List<string> GetUsedInventionIds(string sceneId)
        {
            var prefix = ScopedProgressPrefix(sceneId);
            var ids = new List<string>();
            if (prefix == null)
            {
                return ids;
            }

            var keys = Current.inventionUseProgressKeys;
            for (var i = 0; i < keys.Count; i++)
            {
                var key = keys[i];
                if (key != null && key.StartsWith(prefix, StringComparison.Ordinal))
                {
                    var id = key.Substring(prefix.Length);
                    if (!ids.Contains(id))
                    {
                        ids.Add(id);
                    }
                }
            }

            return ids;
        }

        public static void RecordReachedCheckpoint(string sceneId, string checkpointId)
        {
            var key = ScopedProgressKey(sceneId, checkpointId);
            if (key != null && TryAddUnique(Current.reachedCheckpointProgressKeys, key))
            {
                Save();
            }
        }

        public static List<string> GetReachedCheckpointIds(string sceneId)
        {
            var prefix = ScopedProgressPrefix(sceneId);
            var ids = new List<string>();
            if (prefix == null)
            {
                return ids;
            }

            var keys = Current.reachedCheckpointProgressKeys;
            for (var i = 0; i < keys.Count; i++)
            {
                var key = keys[i];
                if (key != null && key.StartsWith(prefix, StringComparison.Ordinal))
                {
                    ids.Add(key.Substring(prefix.Length));
                }
            }

            return ids;
        }

        public static void RecordActiveCheckpoint(string sceneId, string checkpointId)
        {
            var prefix = ScopedProgressPrefix(sceneId);
            var key = ScopedProgressKey(sceneId, checkpointId);
            if (prefix == null || key == null)
            {
                return;
            }

            var keys = Current.activeCheckpointKeys;
            keys.RemoveAll(existing => existing != null
                && existing.StartsWith(prefix, StringComparison.Ordinal));
            keys.Add(key);
            Save();
        }

        public static string GetActiveCheckpointId(string sceneId)
        {
            var prefix = ScopedProgressPrefix(sceneId);
            if (prefix == null)
            {
                return string.Empty;
            }

            var keys = Current.activeCheckpointKeys;
            for (var i = keys.Count - 1; i >= 0; i--)
            {
                var key = keys[i];
                if (key != null && key.StartsWith(prefix, StringComparison.Ordinal))
                {
                    return key.Substring(prefix.Length);
                }
            }

            return string.Empty;
        }

        public static void ClearQuestSubstepsForWorld(string sceneId)
        {
            var prefix = ScopedProgressPrefix(sceneId);
            if (prefix == null)
            {
                return;
            }

            var data = Current;
            var usesChanged = data.inventionUseProgressKeys.RemoveAll(
                key => key != null && key.StartsWith(prefix, StringComparison.Ordinal)) > 0;
            var checkpointsChanged = data.reachedCheckpointProgressKeys.RemoveAll(
                key => key != null && key.StartsWith(prefix, StringComparison.Ordinal)) > 0;
            var activeCheckpointChanged = data.activeCheckpointKeys.RemoveAll(
                key => key != null && key.StartsWith(prefix, StringComparison.Ordinal)) > 0;
            if (usesChanged || checkpointsChanged || activeCheckpointChanged)
            {
                Save();
            }
        }

        private static string ScopedProgressPrefix(string sceneId) =>
            string.IsNullOrWhiteSpace(sceneId) ? null : sceneId + "\u001f";

        private static string ScopedProgressKey(string sceneId, string id) =>
            string.IsNullOrWhiteSpace(id) ? null : ScopedProgressPrefix(sceneId) + id;

        public static void RecordWorldStart(string worldId)
        {
            if (string.IsNullOrWhiteSpace(worldId))
            {
                return;
            }

            var data = Current;
            data.lastPlayedWorldId = worldId;
            data.continueSceneId = worldId;
            Save();
            RefreshActiveSlotMetadata();
        }

        public static void RecordWorldComplete(string worldId)
        {
            if (string.IsNullOrWhiteSpace(worldId))
            {
                return;
            }

            var data = Current;
            var firstCompletion = TryAddUnique(data.completedWorldIds, worldId);
            data.lastPlayedWorldId = worldId;
            data.continueSceneId = ResolveCompletionResumeScene(worldId);
            if (firstCompletion)
            {
                data.workshopTier = Mathf.Max(data.workshopTier, 1 + data.completedWorldIds.Count);
                data.sparkCount += 3;
            }

            Save();
            RefreshActiveSlotMetadata();
        }

        private static string ResolveCompletionResumeScene(string completedWorldId)
        {
            if (Application.CanStreamedLevelBeLoaded("WorkshopHub"))
            {
                return "WorkshopHub";
            }

            return Application.CanStreamedLevelBeLoaded("HomeMenu")
                ? "HomeMenu"
                : completedWorldId;
        }

        public static void RecordWonder(string wonderId)
        {
            var data = Current;
            if (TryAddUnique(data.claimedWonderIds, wonderId))
            {
                data.sparkCount += 1;
                Save();
                RefreshActiveSlotMetadata();
            }
        }

        public static void RecordDiscoveryBadge(string badgeId)
        {
            var data = Current;
            if (TryAddUnique(data.discoveryBadgeIds, badgeId))
            {
                data.sparkCount += 2;
                Save();
                RefreshActiveSlotMetadata();
            }
        }

        private static void RefreshActiveSlotMetadata()
        {
            var activeSlot = GameSlotStore.ActiveSlot;
            if (activeSlot >= 0)
            {
                GameSlotStore.RefreshMetaFromProgress(activeSlot);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _cache = null;
            _restoreSessionProgress = false;
        }
    }
}
