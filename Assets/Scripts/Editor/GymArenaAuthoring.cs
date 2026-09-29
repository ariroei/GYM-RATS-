using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace GymRats.Editor
{
    /// <summary>Original SUNBURST CLUB arena, authored from reusable primitive modules and baked static meshes.</summary>
    public static class GymArenaAuthoring
    {
        private const string Folder = "Assets/Environment/GymArena";
        private static readonly Dictionary<string, Material> Palette = new Dictionary<string, Material>();

        public static void Build()
        {
            EnsureFolder(Folder);
            ColorMaterial("Ink", new Color(0.045f, 0.08f, 0.14f));
            ColorMaterial("Floor", new Color(0.11f, 0.18f, 0.24f));
            ColorMaterial("Teal", new Color(0.025f, 0.53f, 0.52f));
            ColorMaterial("Mint", new Color(0.32f, 0.88f, 0.73f));
            ColorMaterial("Orange", new Color(1f, 0.42f, 0.12f));
            ColorMaterial("Cream", new Color(1f, 0.91f, 0.70f));
            ColorMaterial("Coral", new Color(0.86f, 0.23f, 0.28f));
            ColorMaterial("Purple", new Color(0.28f, 0.19f, 0.42f));

            var bench = BuildBench();
            var rack = BuildRack();
            var arena = new GameObject("Gym Arena");
            try
            {
                Box(arena, "Gym Floor", new Vector3(0, -0.3f, 0), new Vector3(26, 0.6f, 24), "Floor", true);
                Box(arena, "Central Fighting Mat", new Vector3(0, 0.008f, 0), new Vector3(14, 0.012f, 13), "Teal");
                Border(arena, Vector3.zero, 14, 13, "Cream", 0.08f);
                Border(arena, Vector3.zero, 14.6f, 13.6f, "Orange", 0.16f);
                // Discrete tile seams keep the walking surface completely flat.
                for (int x = -6; x <= 6; x += 2)
                    Box(arena, "Mat Seam", new Vector3(x, 0.017f, 0), new Vector3(0.018f, 0.005f, 12.8f), "Mint");
                for (int z = -6; z <= 6; z += 2)
                    Box(arena, "Mat Seam", new Vector3(0, 0.017f, z), new Vector3(13.8f, 0.005f, 0.018f), "Mint");
                Zone(arena, "Strength Zone", new Vector3(-9.5f, 0, 0), 4f, 17f, "Orange");
                Zone(arena, "Mobility Zone", new Vector3(9.5f, 0, 0), 4f, 17f, "Purple");
                Zone(arena, "Training Bay", new Vector3(0, 0, 9.5f), 14f, 3f, "Coral");
                for (int i = -1; i <= 1; i++)
                {
                    Box(arena, "Stretch Mat", new Vector3(9.5f, 0.025f, i * 3f), new Vector3(2.5f, 0.02f, 1.3f), "Mint");
                    Box(arena, "Mat Strap", new Vector3(9.5f, 0.04f, i * 3f), new Vector3(0.14f, 0.01f, 1.3f), "Ink");
                }
                // Visible cutaway walls have no duplicate colliders; the separate shell is continuous.
                Box(arena, "Back Wall", new Vector3(0, 2.8f, 12), new Vector3(26, 5.6f, 0.5f), "Ink");
                Box(arena, "Back Accent Band", new Vector3(0, 1.15f, 11.7f), new Vector3(25.6f, 1.8f, 0.08f), "Teal");
                Box(arena, "Back Wall Stripe", new Vector3(0, 2.18f, 11.6f), new Vector3(25.6f, 0.18f, 0.06f), "Orange");
                for (int side = -1; side <= 1; side += 2)
                {
                    Box(arena, "Side Wall", new Vector3(side * 13, 1.8f, 7), new Vector3(0.5f, 3.6f, 10), "Ink");
                    Box(arena, "Low Side Padding", new Vector3(side * 13, 0.45f, -5), new Vector3(0.5f, 0.9f, 14), "Teal");
                    Box(arena, "Side Rail", new Vector3(side * 12.7f, 0.95f, -5), new Vector3(0.12f, 0.12f, 14), "Orange");
                }
                Box(arena, "Front Cutaway Padding", new Vector3(0, 0.35f, -12), new Vector3(26, 0.7f, 0.5f), "Teal");
                Box(arena, "Front Safety Stripe", new Vector3(0, 0.76f, -12), new Vector3(26, 0.12f, 0.6f), "Orange");
                foreach (int side in new[] { -1, 1 })
                {
                    Barrier(arena, "Side Containment " + side, new Vector3(side * 13, 6, 0), new Vector3(0.5f, 12, 24.5f));
                    Barrier(arena, "End Containment " + side, new Vector3(0, 6, side * 12), new Vector3(26.5f, 12, 0.5f));
                }
                Barrier(arena, "Overhead Containment", new Vector3(0, 12, 0), new Vector3(26.5f, 0.5f, 24.5f));
                // A large original pixel-letter sign is mesh geometry, not a runtime font dependency.
                Box(arena, "Club Sign Backplate", new Vector3(0, 3.95f, 11.65f), new Vector3(12, 2.55f, 0.12f), "Orange");
                Letters(arena, "GYM RATS", new Vector3(-4.35f, 4.95f, 11.52f), 0.2f);
                for (int i = -2; i <= 2; i++)
                {
                    float x = i * 5.5f;
                    Box(arena, "Wall Light", new Vector3(x, 5.25f, 11.6f), new Vector3(2.2f, 0.12f, 0.13f), "Cream");
                    Box(arena, "Wall Rib", new Vector3(x, 1.1f, 11.55f), new Vector3(0.15f, 2f, 0.15f), "Orange");
                }
                // Recognizable rear punching bags, with wide clear approaches from the center mat.
                for (int side = -1; side <= 1; side += 2)
                {
                    Vector3 p = new Vector3(side * 5.3f, 0, 10.8f);
                    Shape(arena, "Bag Base", PrimitiveType.Cylinder, p + Vector3.up * 0.13f,
                        new Vector3(1.5f, 0.13f, 1.5f), "Ink", false);
                    Shape(arena, "Training Bag", PrimitiveType.Capsule, p + Vector3.up * 1.5f,
                        new Vector3(0.85f, 1.4f, 0.85f), "Coral", true);
                    Box(arena, "Bag Stripe", p + new Vector3(0, 1.45f, -0.43f), new Vector3(0.55f, 0.17f, 0.08f), "Cream");
                }
                // Rear corner lockers and hydration station are decorative, against solid outer walls.
                for (int i = 0; i < 4; i++)
                {
                    Vector3 p = new Vector3(8f + i * 1.05f, 1.6f, 10.7f);
                    Box(arena, "Locker", p, new Vector3(0.9f, 3.2f, 0.8f), i % 2 == 0 ? "Purple" : "Teal", true);
                    Box(arena, "Locker Handle", p + new Vector3(0.27f, 0, -0.45f), new Vector3(0.06f, 0.35f, 0.06f), "Cream");
                    for (int vent = 0; vent < 3; vent++)
                        Box(arena, "Locker Vent", p + new Vector3(0, 0.9f + vent * 0.13f, -0.41f), new Vector3(0.55f, 0.04f, 0.02f), "Ink");
                }
                Box(arena, "Water Cooler", new Vector3(-11.5f, 0.65f, 10.5f), new Vector3(1, 1.3f, 1), "Cream", true);
                Shape(arena, "Water Bottle", PrimitiveType.Cylinder, new Vector3(-11.5f, 1.75f, 10.5f), new Vector3(0.8f, 0.45f, 0.8f), "Mint", false);

                // Bake environmental art to one mesh per material before adding reusable nested modules.
                Bake(arena, "Arena");
                Instance(bench, arena, new Vector3(-5.5f, 0, -3.8f), 0);
                Instance(bench, arena, new Vector3(5.5f, 0, 4.5f), 90);
                Instance(rack, arena, new Vector3(-9f, 0, 10.7f), 0);
                var layout = arena.AddComponent<GymArenaLayout>();
                var spawns = new[] { new Vector3(0, 0.05f, 0), new Vector3(3.5f, 0.05f, 2.5f),
                    new Vector3(-3.5f, 0.05f, 2.5f), new Vector3(0, 0.05f, -4.5f) };
                var so = new SerializedObject(layout); var list = so.FindProperty("spawnPoints"); list.arraySize = spawns.Length;
                for (int i = 0; i < spawns.Length; i++)
                {
                    var marker = new GameObject("Rat Spawn " + (i + 1)).transform;
                    marker.SetParent(arena.transform, false); marker.localPosition = spawns[i];
                    list.GetArrayElementAtIndex(i).objectReferenceValue = marker;
                }
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(arena, "Assets/Prefabs/GymArena.prefab");
            }
            finally { Object.DestroyImmediate(arena); }
            AssetDatabase.SaveAssets();
        }

        private static GameObject BuildBench()
        {
            var root = new GameObject("Padded Cover Bench");
            Box(root, "Bench Pad", new Vector3(0, 0.95f, 0), new Vector3(2.8f, 0.35f, 1.1f), "Orange");
            Box(root, "Bench Base", new Vector3(0, 0.55f, 0), new Vector3(2.7f, 0.75f, 1f), "Ink");
            for (int side = -1; side <= 1; side += 2)
                Box(root, "Bench Foot", new Vector3(side, 0.12f, 0), new Vector3(0.2f, 0.24f, 1.2f), "Mint");
            Box(root, "Gym Stripe", new Vector3(0, 0.97f, -0.56f), new Vector3(2.5f, 0.08f, 0.02f), "Cream");
            var collision = new GameObject("Bench Collision"); collision.transform.SetParent(root.transform, false);
            var box = collision.AddComponent<BoxCollider>(); box.center = Vector3.up * 0.55f; box.size = new Vector3(2.8f, 1.1f, 1.2f);
            Bake(root, "Bench");
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, "Assets/Prefabs/PaddedCoverBench.prefab");
            Object.DestroyImmediate(root); return prefab;
        }

        private static GameObject BuildRack()
        {
            var root = new GameObject("Strength Rack");
            for (int side = -1; side <= 1; side += 2)
                Box(root, "Rack Upright", new Vector3(side * 1.2f, 1.3f, 0), new Vector3(0.18f, 2.6f, 0.3f), "Ink");
            for (int tier = 0; tier < 2; tier++)
            {
                float y = 0.8f + tier * 1.1f;
                Box(root, "Rack Shelf", new Vector3(0, y, 0), new Vector3(2.8f, 0.14f, 0.7f), "Cream");
                for (int weight = -2; weight <= 2; weight++)
                    Shape(root, "Stored Plate", PrimitiveType.Cylinder, new Vector3(weight * 0.5f, y + 0.3f, 0),
                        new Vector3(0.4f, 0.12f, 0.4f), weight % 2 == 0 ? "Teal" : "Orange", false);
            }
            var collider = root.AddComponent<BoxCollider>(); collider.center = Vector3.up * 1.3f; collider.size = new Vector3(2.8f, 2.6f, 0.9f);
            Bake(root, "Rack");
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, "Assets/Prefabs/StrengthRack.prefab");
            Object.DestroyImmediate(root); return prefab;
        }

        private static void Zone(GameObject root, string name, Vector3 p, float width, float depth, string color)
        {
            Box(root, name, p + Vector3.up * 0.014f, new Vector3(width, 0.012f, depth), color);
            Border(root, p, width, depth, "Cream", 0.05f);
        }
        private static void Border(GameObject root, Vector3 p, float width, float depth, string color, float thickness)
        {
            foreach (int side in new[] { -1, 1 })
            {
                Box(root, "Floor Border", p + new Vector3(side * width * 0.5f, 0.024f, 0), new Vector3(thickness, 0.01f, depth), color);
                Box(root, "Floor Border", p + new Vector3(0, 0.024f, side * depth * 0.5f), new Vector3(width, 0.01f, thickness), color);
            }
        }
        private static void Barrier(GameObject root, string name, Vector3 p, Vector3 size)
        {
            var obj = new GameObject(name); obj.transform.SetParent(root.transform, false); obj.transform.localPosition = p;
            obj.layer = 2; // Ignore Raycast layer is excluded only by the camera, never by gameplay collision queries.
            obj.AddComponent<BoxCollider>().size = size;
        }
        private static void Instance(GameObject prefab, GameObject parent, Vector3 p, float yaw)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent.transform);
            instance.transform.localPosition = p; instance.transform.localRotation = Quaternion.Euler(0, yaw, 0);
        }
        private static void Box(GameObject root, string name, Vector3 p, Vector3 size, string color, bool solid = false)
            => Shape(root, name, PrimitiveType.Cube, p, size, color, solid);
        private static void Shape(GameObject root, string name, PrimitiveType type, Vector3 p, Vector3 size, string color, bool solid)
        {
            var obj = GameObject.CreatePrimitive(type); obj.name = name;
            obj.transform.SetParent(root.transform, false); obj.transform.localPosition = p; obj.transform.localScale = size;
            obj.GetComponent<Renderer>().sharedMaterial = Palette[color];
            if (!solid) Object.DestroyImmediate(obj.GetComponent<Collider>());
            obj.isStatic = true;
        }
        private static void Letters(GameObject root, string text, Vector3 topLeft, float pixel)
        {
            var glyphs = new Dictionary<char, string[]> {
                {'G',new[]{"01110","10000","10000","10111","10001","10001","01110"}},
                {'Y',new[]{"10001","10001","01010","00100","00100","00100","00100"}},
                {'M',new[]{"10001","11011","10101","10101","10001","10001","10001"}},
                {'R',new[]{"11110","10001","10001","11110","10100","10010","10001"}},
                {'A',new[]{"01110","10001","10001","11111","10001","10001","10001"}},
                {'T',new[]{"11111","00100","00100","00100","00100","00100","00100"}},
                {'S',new[]{"01111","10000","10000","01110","00001","00001","11110"}}
            };
            for (int i = 0; i < text.Length; i++)
            {
                if (!glyphs.TryGetValue(text[i], out var rows)) continue;
                for (int y = 0; y < 7; y++) for (int x = 0; x < 5; x++)
                    if (rows[y][x] == '1') Box(root, "Sign Pixel", topLeft + new Vector3((i * 6 + x) * pixel, -y * pixel, 0), new Vector3(pixel * 0.92f, pixel * 0.92f, 0.07f), "Cream");
            }
        }
        private static void Bake(GameObject root, string prefix)
        {
            var filters = root.GetComponentsInChildren<MeshFilter>();
            foreach (var group in filters.GroupBy(f => f.GetComponent<Renderer>().sharedMaterial))
            {
                var mesh = new Mesh { name = prefix + " " + group.Key.name, indexFormat = IndexFormat.UInt32 };
                mesh.CombineMeshes(group.Select(f => new CombineInstance { mesh = f.sharedMesh,
                    transform = root.transform.worldToLocalMatrix * f.transform.localToWorldMatrix }).ToArray());
                string path = Folder + "/" + prefix + group.Key.name + ".asset";
                var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if (existing == null) AssetDatabase.CreateAsset(mesh, path);
                else { EditorUtility.CopySerialized(mesh, existing); Object.DestroyImmediate(mesh); mesh = existing; EditorUtility.SetDirty(mesh); }
                var visual = new GameObject("Baked " + group.Key.name); visual.transform.SetParent(root.transform, false);
                visual.AddComponent<MeshFilter>().sharedMesh = mesh;
                visual.AddComponent<MeshRenderer>().sharedMaterial = group.Key; visual.isStatic = true;
            }
            foreach (var filter in filters)
            {
                var obj = filter.gameObject;
                if (obj.GetComponent<Collider>() == null) Object.DestroyImmediate(obj);
                else { Object.DestroyImmediate(obj.GetComponent<Renderer>()); Object.DestroyImmediate(filter); }
            }
        }
        private static void ColorMaterial(string name, Color color)
        {
            string path = Folder + "/Arena" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat, path); }
            mat.name = "Arena" + name; mat.SetColor("_BaseColor", color); mat.SetFloat("_Smoothness", 0.2f);
            mat.enableInstancing = true; EditorUtility.SetDirty(mat); Palette[name] = mat;
        }
        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/'); EnsureFolder(path.Substring(0, slash));
            AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
        }
    }
}
