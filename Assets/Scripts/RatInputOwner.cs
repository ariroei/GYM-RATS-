using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GymRats
{
    [DefaultExecutionOrder(-200)]
    public sealed class RatInputOwner : MonoBehaviour
    {
        [SerializeField, Range(1, 2)] private int playerNumber = 1;
        private InputDevice[] devices = new InputDevice[0];
        private readonly List<InputActionAsset> copies = new List<InputActionAsset>();
        public int PlayerNumber => playerNumber;
        public InputDevice Device => devices.FirstOrDefault();

        public InputActionAsset CreateActions(InputActionAsset source)
        {
            var copy = Instantiate(source);
            copy.devices = devices; // An explicit empty array means no device, never every device.
            copies.Add(copy);
            return copy;
        }

        public void Bind(params InputDevice[] assigned)
        {
            devices = assigned.Where(d => d != null && d.added).Distinct().ToArray();
            copies.RemoveAll(copy => copy == null);
            foreach (var copy in copies)
            {
                var enabled = copy.Where(action => action.enabled).ToArray();
                copy.Disable();
                copy.devices = devices;
                foreach (var action in enabled) action.Enable();
            }
        }
    }
}
