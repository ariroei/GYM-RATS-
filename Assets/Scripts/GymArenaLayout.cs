using UnityEngine;

namespace GymRats
{
    /// <summary>Spawn reservations and a last-resort recovery behind the arena's solid collision shell.</summary>
    [DefaultExecutionOrder(90)]
    public sealed class GymArenaLayout : MonoBehaviour
    {
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private Vector3 minimum = new Vector3(-13f, -1f, -12f);
        [SerializeField] private Vector3 maximum = new Vector3(13f, 13f, 12f);
        private RatMotor[] rats;
        private Grabbable[] props;
        public Transform[] SpawnPoints => spawnPoints;
        public int RecoveryCount { get; private set; }

        private void Start()
        {
            rats = FindObjectsByType<RatMotor>();
            props = FindObjectsByType<Grabbable>();
        }

        public bool Contains(Vector3 point) => point.x >= minimum.x && point.x <= maximum.x
            && point.y >= minimum.y && point.y <= maximum.y && point.z >= minimum.z && point.z <= maximum.z;

        public bool TryGetFreeSpawn(out Vector3 position)
        {
            foreach (var spawn in spawnPoints)
            {
                if (spawn == null) continue;
                Vector3 p = spawn.position;
                if (!Physics.CheckCapsule(p + Vector3.up * 0.6f, p + Vector3.up * 2.5f,
                    0.55f, ~0, QueryTriggerInteraction.Ignore))
                { position = p; return true; }
            }
            position = default;
            return false;
        }

        private void LateUpdate()
        {
            // Scene participants are cached once; normal play is contained by walls and a ceiling collider.
            foreach (var rat in rats)
            {
                if (rat == null || Contains(rat.transform.position)) continue;
                rat.GetComponent<RatGrabber>()?.ReleaseHeld(false);
                rat.GetComponent<Grabbable>()?.Holder?.ReleaseHeld(false);
                rat.Respawn();
                RecoveryCount++;
            }
            foreach (var prop in props)
            {
                if (prop == null || prop.GetComponent<Rigidbody>() == null || Contains(prop.transform.position)) continue;
                prop.ResetToSpawn();
                RecoveryCount++;
            }
        }
    }
}
