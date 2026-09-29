using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GymRats.Tests
{
    /// <summary>Isolates legacy single-rat regression checks; multiplayer tests use the unmodified session.</summary>
    internal static class SoloGameplayFixture
    {
        public static void Configure()
        {
            var session = Object.FindFirstObjectByType<LocalMultiplayerSession>();
            session.enabled = false;
            foreach (var owner in session.Players)
                owner.Bind(owner.PlayerNumber == 1
                    ? new InputDevice[] { Keyboard.current, Gamepad.current }.Where(d => d != null).ToArray()
                    : new InputDevice[0]);
            var camera = Camera.main;
            camera.GetComponent<RatSharedCamera>().enabled = false;
            camera.GetComponent<RatFollowCamera>().enabled = true;
        }
    }
}
