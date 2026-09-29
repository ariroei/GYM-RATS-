using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GymRats
{
    [DefaultExecutionOrder(-300)]
    public sealed class LocalMultiplayerSession : MonoBehaviour
    {
        public enum ControlLayout { Auto, KeyboardAndGamepad, TwoGamepads }
        [SerializeField] private RatInputOwner[] players;
        [SerializeField] private ControlLayout layout = ControlLayout.Auto;
        private ControlLayout activeLayout;
        private float nextDeviceCheck;
        public RatInputOwner[] Players => players;
        public ControlLayout ActiveLayout => activeLayout;

        private void Awake() => SetLayout(layout);
        private void OnEnable() => InputSystem.onDeviceChange += DeviceChanged;
        private void OnDisable() => InputSystem.onDeviceChange -= DeviceChanged;

        private void Update()
        {
            // Editor focus can restore a native device after scene Awake without an Added event.
            if (Time.unscaledTime < nextDeviceCheck) return;
            nextDeviceCheck = Time.unscaledTime + 0.5f;
            ReconcileDevices();
        }

        public void SetLayout(ControlLayout choice)
        {
            activeLayout = choice == ControlLayout.Auto
                ? (Gamepad.all.Count >= 2 ? ControlLayout.TwoGamepads : ControlLayout.KeyboardAndGamepad) : choice;
            foreach (var player in players) player.Bind();
            ReconcileDevices();
        }

        private void DeviceChanged(InputDevice device, InputDeviceChange change)
        {
            if (change == InputDeviceChange.Added || change == InputDeviceChange.Removed
                || change == InputDeviceChange.Disconnected || change == InputDeviceChange.Reconnected
                || change == InputDeviceChange.Disabled || change == InputDeviceChange.Enabled)
                ReconcileDevices();
        }

        private void ReconcileDevices()
        {
            // Reserve surviving assignments before filling vacancies, including when P1 disconnects.
            var claimed = new HashSet<InputDevice>();
            var assigned = new InputDevice[players.Length];
            for (int i = 0; i < players.Length; i++)
            {
                bool keyboardSlot = activeLayout == ControlLayout.KeyboardAndGamepad && i == 0;
                var device = players[i].Device;
                if (device != null && device.added && device.enabled
                    && (keyboardSlot ? device is Keyboard : device is Gamepad) && claimed.Add(device))
                    assigned[i] = device;
            }
            for (int i = 0; i < players.Length; i++)
            {
                bool keyboardSlot = activeLayout == ControlLayout.KeyboardAndGamepad && i == 0;
                InputDevice device = assigned[i];
                if (device == null)
                    device = keyboardSlot ? (InputDevice)Keyboard.current : Gamepad.all.FirstOrDefault(pad => pad.enabled && !claimed.Contains(pad));
                if (device != null && (!device.added || !device.enabled)) device = null;
                if (device != null) claimed.Add(device);
                if (players[i].Device != device) players[i].Bind(device);
            }
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(12, 12, 430, 150), GUI.skin.box);
            GUILayout.Label("GYM RATS - LOCAL TWO PLAYER");
            foreach (var player in players)
                GUILayout.Label("P" + player.PlayerNumber + ": " + (player.Device != null ? player.Device.displayName : "Waiting for a device"));
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Keyboard + Gamepad")) SetLayout(ControlLayout.KeyboardAndGamepad);
            if (GUILayout.Button("Two Gamepads")) SetLayout(ControlLayout.TwoGamepads);
            GUILayout.EndHorizontal();
            GUILayout.Label("Keyboard: WASD / arrows, Space, Enter, E, R");
            GUILayout.Label("Gamepad: left stick, A / Cross, X / Square, Y / Triangle, RB / R1");
            GUILayout.EndArea();
        }
    }
}
