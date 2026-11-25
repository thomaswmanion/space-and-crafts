#if UNITY_EDITOR
using SpaceAndCrafts.UI;
using UnityEditor;
using UnityEngine;

namespace SpaceAndCrafts.EditorTools
{
    /// <summary>
    /// MCP-friendly Play Mode confirm: keyboard injection often misses Game view focus.
    /// </summary>
    public static class HomeMenuPlayConfirmMenu
    {
        [MenuItem("Space & Crafts/Play Mode/Confirm Home Menu Slot")]
        public static void ConfirmHomeSlot()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[Space & Crafts] Enter Play Mode on HomeMenu first.");
                return;
            }

            var menu = Object.FindAnyObjectByType<HomeMenuController>();
            if (menu == null)
            {
                Debug.LogError("[Space & Crafts] HomeMenuController not found in loaded scenes.");
                return;
            }

            menu.ConfirmSlotAndPlay();
            Debug.Log("[Space & Crafts] Home menu slot confirmed via menu.");
        }

        [MenuItem("Space & Crafts/Play Mode/Advance Opening Story")]
        public static void AdvanceOpeningStory()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[Space & Crafts] Enter Play Mode first.");
                return;
            }

            var story = Object.FindAnyObjectByType<OpeningStoryPresenter>();
            if (story == null)
            {
                Debug.LogWarning("[Space & Crafts] OpeningStoryPresenter not active.");
                return;
            }

            story.ForceAdvanceCard();
            Debug.Log("[Space & Crafts] Opening story advanced via menu.");
        }

        [MenuItem("Space & Crafts/Play Mode/Review Opening Card 1 (Hold)")]
        public static void ReviewOpeningCard1() => ReviewOpeningCard(0);

        [MenuItem("Space & Crafts/Play Mode/Review Opening Card 2 (Hold)")]
        public static void ReviewOpeningCard2() => ReviewOpeningCard(1);

        [MenuItem("Space & Crafts/Play Mode/Review Opening Card 3 (Hold)")]
        public static void ReviewOpeningCard3() => ReviewOpeningCard(2);

        [MenuItem("Space & Crafts/Play Mode/Review Opening Card 4 (Hold)")]
        public static void ReviewOpeningCard4() => ReviewOpeningCard(3);

        private static void ReviewOpeningCard(int index)
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[Space & Crafts] Enter Play Mode before holding a story card for review.");
                return;
            }

            OpeningStoryPresenter.ReviewCard(index);
        }

        [MenuItem("Space & Crafts/Play Mode/Preview Opening Story (no save)")]
        public static void PreviewOpeningStoryWithoutSave()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[Space & Crafts] Enter Play Mode first.");
                return;
            }

            if (Object.FindAnyObjectByType<OpeningStoryPresenter>() != null)
            {
                Debug.LogWarning("[Space & Crafts] Opening story is already active.");
                return;
            }

            var menu = Object.FindAnyObjectByType<HomeMenuController>();
            if (menu == null)
            {
                Debug.LogError("[Space & Crafts] HomeMenuController not found for story preview.");
                return;
            }

            menu.PreviewOpeningStoryWithoutSave();
            Debug.Log("[Space & Crafts] Previewing the opening story without creating a save slot.");
        }

        [MenuItem("Space & Crafts/Play Mode/Skip Opening Story To Arrival")]
        public static void SkipStoryToArrival()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[Space & Crafts] Enter Play Mode first.");
                return;
            }

            var story = Object.FindAnyObjectByType<OpeningStoryPresenter>();
            if (story != null)
            {
                Object.Destroy(story.gameObject);
            }

            var ui = GameObject.Find("UI_OpeningStory");
            if (ui != null)
            {
                Object.Destroy(ui);
            }

            UnityEngine.SceneManagement.SceneManager.LoadScene("RainbowWorld_Arrival");
            Debug.Log("[Space & Crafts] Skipped opening story → Arrival.");
        }
    }
}
#endif
