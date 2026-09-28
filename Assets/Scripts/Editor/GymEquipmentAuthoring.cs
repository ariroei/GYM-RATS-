using UnityEditor;
using UnityEngine;

namespace GymRats.Editor
{
    /// <summary>Original primitive gym props. Rebuilds only generated equipment prefab assets.</summary>
    public static class GymEquipmentAuthoring
    {
        public static void Build()
        {
            Material teal = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/RatCharacter/RatTeal.mat");
            Material dark = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/RatCharacter/RatDarkTeal.mat");
            Material orange = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/MedicineBall.mat");
            Material cream = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/RatCharacter/RatCream.mat");
            var ball = PrefabUtility.LoadPrefabContents("Assets/Prefabs/MedicineBall.prefab");
            Configure(ball, 4f, 1f, 0.6f, 8f, 0.45f, 0.2f);
            PrefabUtility.SaveAsPrefabAsset(ball, "Assets/Prefabs/MedicineBall.prefab");
            PrefabUtility.UnloadPrefabContents(ball);

            var dumbbell = new GameObject("Dumbbell");
            try
            {
                Shape(dumbbell, "Rubber Grip", PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.19f, 0.5f, 0.19f), dark, true);
                for (int side = -1; side <= 1; side += 2)
                {
                    var plate = Shape(dumbbell, "Square Rubber Weight " + side, PrimitiveType.Cube,
                        Vector3.right * side * 0.55f, new Vector3(0.32f, 0.66f, 0.66f), teal);
                    var box = plate.AddComponent<BoxCollider>();
                    Shape(dumbbell, "Orange End Cap " + side, PrimitiveType.Cylinder,
                        Vector3.right * side * 0.72f, new Vector3(0.4f, 0.025f, 0.4f), orange, true);
                    Shape(dumbbell, "Grip Collar " + side, PrimitiveType.Cylinder,
                        Vector3.right * side * 0.32f, new Vector3(0.25f, 0.04f, 0.25f), cream, true);
                }
                var grip = dumbbell.AddComponent<CapsuleCollider>(); grip.direction = 0; grip.height = 1f; grip.radius = 0.1f;
                Configure(dumbbell, 8f, 0.72f, 0.8f, 9f, 0.8f, 0.05f);
                PrefabUtility.SaveAsPrefabAsset(dumbbell, "Assets/Prefabs/Dumbbell.prefab");
            }
            finally { Object.DestroyImmediate(dumbbell); }

            var roller = new GameObject("Foam Roller");
            try
            {
                Shape(roller, "Foam Body", PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.52f, 0.65f, 0.52f), teal, true);
                for (int i = -4; i <= 4; i++)
                    Shape(roller, "Grip Rib " + i, PrimitiveType.Cylinder, Vector3.right * i * 0.13f,
                        new Vector3(0.56f, 0.035f, 0.56f), i % 2 == 0 ? dark : teal, true);
                for (int side = -1; side <= 1; side += 2)
                {
                    Shape(roller, "End Rim " + side, PrimitiveType.Cylinder, Vector3.right * side * 0.65f,
                        new Vector3(0.5f, 0.015f, 0.5f), orange, true);
                    Shape(roller, "Recessed Core " + side, PrimitiveType.Cylinder, Vector3.right * side * 0.67f,
                        new Vector3(0.27f, 0.008f, 0.27f), dark, true);
                }
                var capsule = roller.AddComponent<CapsuleCollider>(); capsule.direction = 0; capsule.height = 1.4f; capsule.radius = 0.28f;
                Configure(roller, 1.2f, 1.15f, 0.3f, 4.5f, 0.65f, 0.12f);
                PrefabUtility.SaveAsPrefabAsset(roller, "Assets/Prefabs/FoamRoller.prefab");
            }
            finally { Object.DestroyImmediate(roller); }
            AssetDatabase.SaveAssets();
        }

        private static void Configure(GameObject root, float mass, float launch, float impact, float maxImpact, float friction, float bounce)
        {
            var body = root.GetComponent<Rigidbody>();
            if (body == null) body = root.AddComponent<Rigidbody>();
            body.mass = mass; body.linearDamping = 0.35f; body.angularDamping = 0.8f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.solverIterations = 10; body.solverVelocityIterations = 4;
            if (root.GetComponent<Grabbable>() == null) root.AddComponent<Grabbable>();
            var equipment = root.GetComponent<ThrownEquipment>();
            if (equipment == null) equipment = root.AddComponent<ThrownEquipment>();
            var settings = new SerializedObject(equipment);
            settings.FindProperty("launchMultiplier").floatValue = launch;
            settings.FindProperty("knockbackPerSpeed").floatValue = impact;
            settings.FindProperty("maximumKnockback").floatValue = maxImpact;
            settings.ApplyModifiedPropertiesWithoutUndo();
            string path = "Assets/Materials/" + root.name.Replace(" ", "") + "Physics.physicMaterial";
            var material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
            if (material == null) { material = new PhysicsMaterial(root.name + " Physics"); AssetDatabase.CreateAsset(material, path); }
            material.dynamicFriction = friction; material.staticFriction = friction;
            material.bounciness = bounce; material.bounceCombine = PhysicsMaterialCombine.Maximum;
            material.frictionCombine = PhysicsMaterialCombine.Average;
            EditorUtility.SetDirty(material);
            foreach (var collider in root.GetComponentsInChildren<Collider>()) collider.sharedMaterial = material;
        }

        private static GameObject Shape(GameObject parent, string name, PrimitiveType type, Vector3 position,
            Vector3 scale, Material material, bool horizontal = false)
        {
            var part = GameObject.CreatePrimitive(type); part.name = name;
            part.transform.SetParent(parent.transform, false); part.transform.localPosition = position;
            part.transform.localScale = scale;
            if (horizontal) part.transform.localRotation = Quaternion.Euler(0, 0, 90);
            part.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(part.GetComponent<Collider>());
            return part;
        }
    }
}
