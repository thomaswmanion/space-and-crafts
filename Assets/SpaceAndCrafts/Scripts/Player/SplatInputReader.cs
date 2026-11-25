using UnityEngine;
using UnityEngine.InputSystem;

namespace SpaceAndCrafts.Player
{
    /// <summary>
    /// Reads Unity Input System actions into movement and device-aware look intents.
    /// Wire an InputActionAsset in the inspector once the project is created.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class SplatInputReader : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private string actionMapName = "Player";
        [SerializeField] private string moveActionName = "Move";
        [SerializeField] private string lookActionName = "Look";
        [SerializeField] private string jumpActionName = "Jump";
        [SerializeField] private string interactActionName = "Interact";
        [SerializeField] private string cycleRecipeActionName = "CycleRecipe";
        [SerializeField] [Range(0f, 0.4f)] private float moveDeadzone = 0.12f;
        [SerializeField] [Range(0.5f, 2.5f)] private float moveResponse = 1.35f;
        [SerializeField] [Range(0f, 0.4f)] private float lookDeadzone = 0.08f;
        [SerializeField] [Range(0.5f, 2.5f)] private float lookResponse = 1.15f;

        private InputActionMap _map;
        private InputAction _move;
        private InputAction _look;
        private InputAction _jump;
        private InputAction _interact;
        private InputAction _cycleRecipe;
        private InputActionAsset _boundInputActions;
        private string _boundActionMapName;
        private bool _warnedMissingAsset;
        private bool _warnedMissingMap;

        public Vector2 Move { get; private set; }
        public Vector2 Look { get; private set; }
        public bool LookIsMouseDelta { get; private set; }
        public bool LookIsGamepadStick { get; private set; }
        public bool JumpPressedThisFrame { get; private set; }
        public bool JumpHeld { get; private set; }
        public bool InteractPressedThisFrame { get; private set; }
        public bool CycleRecipePressedThisFrame { get; private set; }
        public string ActiveBindingGroup { get; private set; } = "Keyboard&Mouse";

        public string GetBindingHint(string actionName, string bindingGroup)
        {
            var map = inputActions != null
                ? inputActions.FindActionMap(actionMapName, throwIfNotFound: false)
                : null;
            var action = map != null ? map.FindAction(actionName, throwIfNotFound: false) : null;
            if (action == null)
            {
                return string.Empty;
            }

            return action.GetBindingDisplayString(group: bindingGroup);
        }

        private bool _debugOverride;
        private Vector2 _debugMove;
        private Vector2 _debugLook;
        private bool _debugJumpPulse;
        private bool _debugJumpHeld;
        private bool _debugInteractPulse;

        /// <summary>Smoke/characterization only — overrides device input while active.</summary>
        public void DebugSetMove(Vector2 move)
        {
            _debugOverride = true;
            _debugMove = Vector2.ClampMagnitude(move, 1f);
        }

        public void DebugSetLook(Vector2 look)
        {
            _debugOverride = true;
            _debugLook = look;
        }

        public void DebugPulseJump(bool held = true)
        {
            _debugOverride = true;
            _debugJumpPulse = true;
            _debugJumpHeld = held;
        }

        public void DebugPulseInteract()
        {
            _debugOverride = true;
            _debugInteractPulse = true;
        }

        public void DebugClear()
        {
            _debugOverride = false;
            _debugMove = Vector2.zero;
            _debugLook = Vector2.zero;
            _debugJumpPulse = false;
            _debugJumpHeld = false;
            _debugInteractPulse = false;
        }

        private void OnEnable()
        {
            EnsureInputMap();
        }

        private void OnDisable()
        {
            ReleaseInputMap();
        }

        private void OnActionTriggered(InputAction.CallbackContext context)
        {
            var device = context.control != null ? context.control.device : null;
            if (device is Gamepad)
            {
                ActiveBindingGroup = "Gamepad";
            }
            else if (device is Keyboard || device is Mouse)
            {
                ActiveBindingGroup = "Keyboard&Mouse";
            }
        }

        private void Update()
        {
            // Runtime-built player objects can receive their action asset after
            // AddComponent has already fired OnEnable. Retry only while the
            // configured asset/map is not the one currently bound.
            EnsureInputMap();
            if (_debugOverride)
            {
                Move = _debugMove;
                Look = _debugLook;
                LookIsMouseDelta = false;
                LookIsGamepadStick = false;
                JumpPressedThisFrame = _debugJumpPulse;
                JumpHeld = _debugJumpHeld;
                InteractPressedThisFrame = _debugInteractPulse;
                CycleRecipePressedThisFrame = false;
                _debugJumpPulse = false;
                _debugInteractPulse = false;
                return;
            }

            Move = ShapeStick(_move != null ? _move.ReadValue<Vector2>() : Vector2.zero, moveDeadzone, moveResponse);
            var rawLook = _look != null ? _look.ReadValue<Vector2>() : Vector2.zero;
            LookIsMouseDelta = _look?.activeControl?.device is Mouse;
            LookIsGamepadStick = _look?.activeControl?.device is Gamepad;
            // Mouse delta is already displacement, so keep it linear and preserve its pixel
            // magnitude. Stick shaping is only appropriate for a normalized analog axis.
            Look = LookIsMouseDelta
                ? rawLook
                : ShapeStick(rawLook, lookDeadzone, lookResponse);
            JumpHeld = _jump != null && _jump.IsPressed();
            JumpPressedThisFrame = _jump != null && _jump.WasPressedThisFrame();
            InteractPressedThisFrame = _interact != null && _interact.WasPressedThisFrame();
            CycleRecipePressedThisFrame = _cycleRecipe != null && _cycleRecipe.WasPressedThisFrame();
        }

        private void EnsureInputMap()
        {
            if (_map != null && _boundInputActions == inputActions
                && _boundActionMapName == actionMapName)
            {
                return;
            }

            ReleaseInputMap();
            if (inputActions == null)
            {
                if (!_warnedMissingAsset)
                {
                    Debug.LogWarning("[SplatInputReader] Assign an InputActionAsset when the Unity Input System is configured.", this);
                    _warnedMissingAsset = true;
                }
                return;
            }

            _warnedMissingAsset = false;
            _map = inputActions.FindActionMap(actionMapName, throwIfNotFound: false);
            if (_map == null)
            {
                if (!_warnedMissingMap)
                {
                    Debug.LogWarning($"[SplatInputReader] Action map '{actionMapName}' not found.", this);
                    _warnedMissingMap = true;
                }
                return;
            }

            _warnedMissingMap = false;
            _boundInputActions = inputActions;
            _boundActionMapName = actionMapName;
            _map.actionTriggered += OnActionTriggered;
            _move = _map.FindAction(moveActionName, throwIfNotFound: false);
            _look = _map.FindAction(lookActionName, throwIfNotFound: false);
            _jump = _map.FindAction(jumpActionName, throwIfNotFound: false);
            _interact = _map.FindAction(interactActionName, throwIfNotFound: false);
            _cycleRecipe = _map.FindAction(cycleRecipeActionName, throwIfNotFound: false);
            _map.Enable();
        }

        private void ReleaseInputMap()
        {
            if (_map != null)
            {
                _map.actionTriggered -= OnActionTriggered;
                _map.Disable();
            }

            _map = null;
            _move = null;
            _look = null;
            _jump = null;
            _interact = null;
            _cycleRecipe = null;
            _boundInputActions = null;
            _boundActionMapName = null;
        }

        private static Vector2 ShapeStick(Vector2 raw, float deadzone, float response)
        {
            var mag = raw.magnitude;
            if (mag <= deadzone)
            {
                return Vector2.zero;
            }

            var shaped = Mathf.Pow(Mathf.InverseLerp(deadzone, 1f, Mathf.Min(1f, mag)), response);
            return raw.normalized * shaped;
        }

        /// <summary>Call after gameplay systems consume edge-triggered inputs.</summary>
        public void ConsumeFrameEdges()
        {
            JumpPressedThisFrame = false;
            InteractPressedThisFrame = false;
            CycleRecipePressedThisFrame = false;
        }
    }
}
