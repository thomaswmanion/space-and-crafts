using SpaceAndCrafts.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SpaceAndCrafts.UI
{
    /// <summary>
    /// Home-menu save slot picker (3 slots). Left/Right change slot; confirms via HomeMenuController.
    /// </summary>
    public sealed class SaveSlotBoard : MonoBehaviour
    {
        [SerializeField] private TextMesh label;
        [SerializeField] private int selected;

        public int Selected => Mathf.Clamp(selected, 0, GameSlotStore.SlotCount - 1);

        public string SelectedSummary => GameSlotStore.DescribeSlot(Selected);

        public bool SelectedIsEmpty =>
            Selected >= 0 && Selected < GameSlotStore.SlotCount && !GameSlotStore.Index.slots[Selected].occupied;

        public void Nudge(int delta)
        {
            selected = (Selected + delta + GameSlotStore.SlotCount) % GameSlotStore.SlotCount;
            Refresh();
        }

        private void OnEnable()
        {
            GameSlotStore.LoadIndex();
            Refresh();
        }

        private void Update()
        {
            var left = Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A);
            var right = Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D);
            var kb = Keyboard.current;
            if (kb != null)
            {
                left |= kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame;
                right |= kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame;
            }

            if (left)
            {
                selected = (Selected + GameSlotStore.SlotCount - 1) % GameSlotStore.SlotCount;
                Refresh();
            }
            else if (right)
            {
                selected = (Selected + 1) % GameSlotStore.SlotCount;
                Refresh();
            }
        }

        public void Refresh()
        {
            if (label == null)
            {
                var named = transform.Find("SaveSlot");
                if (named != null)
                {
                    label = named.GetComponent<TextMesh>();
                }

                if (label == null)
                {
                    foreach (var tm in GetComponentsInChildren<TextMesh>(true))
                    {
                        if (tm != null && tm.gameObject.name == "SaveSlot")
                        {
                            label = tm;
                            break;
                        }
                    }
                }
            }

            if (label != null)
            {
                label.text = SelectedSummary;
            }
        }
    }
}
