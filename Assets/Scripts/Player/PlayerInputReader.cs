using UnityEngine;
using UnityEngine.InputSystem;

namespace Relicfall.Player
{
    public sealed class PlayerInputReader : MonoBehaviour
    {
        private InputActionMap gameplay;
        private InputAction move;
        private InputAction jump;
        private InputAction dash;
        private InputAction attack;
        private InputAction slot1;
        private InputAction slot2;
        private InputAction weaponSwitch;
        private bool jumpPressed;
        private bool dashPressed;
        private int attackPresses;
        private bool slot1Pressed;
        private bool slot2Pressed;
        private bool weaponSwitchPressed;
#if UNITY_EDITOR
        private bool debugOverride;
        private float debugMove;
#endif

        public float Move
        {
            get
            {
#if UNITY_EDITOR
                if (debugOverride) return debugMove;
#endif
                return move != null ? move.ReadValue<float>() : 0f;
            }
        }

        private void Awake()
        {
            gameplay = new InputActionMap("Gameplay");
            move = gameplay.AddAction("Move", InputActionType.Value);
            move.AddCompositeBinding("1DAxis")
                .With("Negative", "<Keyboard>/a")
                .With("Positive", "<Keyboard>/d");
            move.AddCompositeBinding("1DAxis")
                .With("Negative", "<Keyboard>/leftArrow")
                .With("Positive", "<Keyboard>/rightArrow");
            move.AddBinding("<Gamepad>/leftStick/x");

            jump = gameplay.AddAction("Jump", InputActionType.Button);
            jump.AddBinding("<Keyboard>/w");
            jump.performed += OnJump;

            dash = gameplay.AddAction("Dash", InputActionType.Button);
            dash.AddBinding("<Keyboard>/leftShift");
            dash.AddBinding("<Gamepad>/rightShoulder");
            dash.performed += OnDash;

            attack = gameplay.AddAction("Attack", InputActionType.Button);
            attack.AddBinding("<Keyboard>/j");
            attack.performed += OnAttack;

            slot1 = gameplay.AddAction("WeaponSlot1", InputActionType.Button);
            slot1.AddBinding("<Keyboard>/1");
            slot1.performed += OnSlot1;
            slot2 = gameplay.AddAction("WeaponSlot2", InputActionType.Button);
            slot2.AddBinding("<Keyboard>/2");
            slot2.performed += OnSlot2;
            weaponSwitch = gameplay.AddAction("SwitchWeapon", InputActionType.Button);
            weaponSwitch.AddBinding("<Keyboard>/q");
            weaponSwitch.AddBinding("<Gamepad>/dpad/up");
            weaponSwitch.performed += OnWeaponSwitch;
        }

        private void OnEnable() => gameplay?.Enable();

        private void OnDisable()
        {
            gameplay?.Disable();
            jumpPressed = false;
            dashPressed = false;
            attackPresses = 0;
            slot1Pressed = false;
            slot2Pressed = false;
            weaponSwitchPressed = false;
        }

        private void OnDestroy()
        {
            if (jump != null) jump.performed -= OnJump;
            if (dash != null) dash.performed -= OnDash;
            if (attack != null) attack.performed -= OnAttack;
            if (slot1 != null) slot1.performed -= OnSlot1;
            if (slot2 != null) slot2.performed -= OnSlot2;
            if (weaponSwitch != null) weaponSwitch.performed -= OnWeaponSwitch;
            gameplay?.Dispose();
        }

        private void OnJump(InputAction.CallbackContext context) => jumpPressed = true;
        private void OnDash(InputAction.CallbackContext context) => dashPressed = true;
        private void OnAttack(InputAction.CallbackContext context) => attackPresses = Mathf.Min(attackPresses + 1, 3);
        private void OnSlot1(InputAction.CallbackContext context) => slot1Pressed = true;
        private void OnSlot2(InputAction.CallbackContext context) => slot2Pressed = true;
        private void OnWeaponSwitch(InputAction.CallbackContext context) => weaponSwitchPressed = true;

        public bool ConsumeJump()
        {
            bool value = jumpPressed;
            jumpPressed = false;
            return value;
        }

        public bool ConsumeDash()
        {
            bool value = dashPressed;
            dashPressed = false;
            return value;
        }

        public bool ConsumeAttack()
        {
            if (attackPresses <= 0) return false;
            attackPresses--;
            return true;
        }

        public bool ConsumeSlot1()
        {
            bool value = slot1Pressed;
            slot1Pressed = false;
            return value;
        }

        public bool ConsumeSlot2()
        {
            bool value = slot2Pressed;
            slot2Pressed = false;
            return value;
        }

        public bool ConsumeWeaponSwitch()
        {
            bool value = weaponSwitchPressed;
            weaponSwitchPressed = false;
            return value;
        }

#if UNITY_EDITOR
        public void SetDebugInput(float horizontal, bool pressJump = false, bool pressDash = false,
            bool pressAttack = false)
        {
            debugOverride = true;
            debugMove = horizontal;
            jumpPressed |= pressJump;
            dashPressed |= pressDash;
            if (pressAttack) attackPresses = Mathf.Min(attackPresses + 1, 3);
        }

        public void ClearDebugInput()
        {
            debugOverride = false;
            debugMove = 0f;
        }
#endif
    }
}
