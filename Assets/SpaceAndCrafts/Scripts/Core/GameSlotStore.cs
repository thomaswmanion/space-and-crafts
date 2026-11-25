using System;
using System.IO;
using UnityEngine;

namespace SpaceAndCrafts.Core
{
    /// <summary>
    /// Three new-game / continue slots. Active slot gates PlayerProgressSave path.
    /// </summary>
    [Serializable]
    public sealed class GameSlotMeta
    {
        public bool occupied;
        public string displayName = "Empty";
        public string lastPlayedWorldId = "";
        public int sparkCount;
        public string lastPlayedUtc = "";
    }

    [Serializable]
    public sealed class GameSlotIndex
    {
        public int version = 1;
        public int activeSlot = -1;
        public GameSlotMeta[] slots = { new(), new(), new() };
    }

    public static class GameSlotStore
    {
        public const int SlotCount = 3;
        private const string IndexFile = "space_and_crafts_slots.json";
        private static GameSlotIndex _index;

        public static int ActiveSlot => Index.activeSlot;

        public static GameSlotIndex Index
        {
            get
            {
                if (_index == null)
                {
                    _index = LoadIndex();
                }

                return _index;
            }
        }

        public static string IndexPath => Path.Combine(Application.persistentDataPath, IndexFile);

        public static string SlotProgressPath(int slot) =>
            Path.Combine(Application.persistentDataPath, $"space_and_crafts_progress_slot{slot}.json");

        public static GameSlotIndex LoadIndex()
        {
            try
            {
                if (File.Exists(IndexPath))
                {
                    var data = JsonUtility.FromJson<GameSlotIndex>(File.ReadAllText(IndexPath));
                    if (data?.slots != null && data.slots.Length == SlotCount)
                    {
                        _index = data;
                        return _index;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Space & Crafts] Slot index load failed: {ex.Message}");
            }

            _index = new GameSlotIndex();
            return _index;
        }

        public static void SaveIndex()
        {
            try
            {
                File.WriteAllText(IndexPath, JsonUtility.ToJson(Index, true));
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Space & Crafts] Slot index write failed: {ex.Message}");
            }
        }

        public static void SelectSlot(int slot)
        {
            if (slot < 0 || slot >= SlotCount)
            {
                return;
            }

            Index.activeSlot = slot;
            SaveIndex();
            PlayerProgressSave.BindSlot(slot);
        }

        public static void StartNewGame(int slot, string displayName = null)
        {
            if (slot < 0 || slot >= SlotCount)
            {
                return;
            }

            PlayerProgressSave.BeginFreshSession();

            try
            {
                var path = SlotProgressPath(slot);
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
                // ignore
            }

            Index.activeSlot = slot;
            Index.slots[slot] = new GameSlotMeta
            {
                occupied = true,
                displayName = string.IsNullOrWhiteSpace(displayName) ? $"Slot {slot + 1}" : displayName,
                lastPlayedWorldId = "RainbowWorld_Arrival",
                sparkCount = 0,
                lastPlayedUtc = DateTime.UtcNow.ToString("u"),
            };
            SaveIndex();
            PlayerProgressSave.BindSlot(slot);
            PlayerProgressSave.ResetAll();
            PlayerProgressSave.Current.lastPlayedWorldId = "RainbowWorld_Arrival";
            PlayerProgressSave.Current.continueSceneId = "RainbowWorld_Arrival";
            PlayerProgressSave.Save();
            RefreshMetaFromProgress(slot);
        }

        public static bool ContinueSlot(int slot)
        {
            if (slot < 0 || slot >= SlotCount || !Index.slots[slot].occupied)
            {
                return false;
            }

            SelectSlot(slot);
            PlayerProgressSave.Load();
            PlayerProgressSave.BeginContinueSession();
            RefreshMetaFromProgress(slot);
            return true;
        }

        public static void RefreshMetaFromProgress(int slot)
        {
            if (slot < 0 || slot >= SlotCount)
            {
                return;
            }

            var data = PlayerProgressSave.Current;
            var meta = Index.slots[slot];
            meta.occupied = true;
            meta.lastPlayedWorldId = data.lastPlayedWorldId;
            meta.sparkCount = data.sparkCount;
            meta.lastPlayedUtc = DateTime.UtcNow.ToString("u");
            if (string.IsNullOrWhiteSpace(meta.displayName))
            {
                meta.displayName = $"Slot {slot + 1}";
            }

            SaveIndex();
        }

        public static string DescribeSlot(int slot)
        {
            if (slot < 0 || slot >= SlotCount)
            {
                return "—";
            }

            var m = Index.slots[slot];
            if (!m.occupied)
            {
                return $"Slot {slot + 1}: Empty — New Game";
            }

            var world = string.IsNullOrWhiteSpace(m.lastPlayedWorldId) ? "Arrival" : m.lastPlayedWorldId.Replace("RainbowWorld_", "").Replace("World", "W");
            return $"Slot {slot + 1}: {m.displayName} · {world} · {m.sparkCount} sparks";
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _index = null;
        }
    }
}
