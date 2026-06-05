#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using DragonTD.TowerDefense;

namespace DragonTD.Editor
{
    public static class EnemySkeletalRigBuilder
    {
        private const string PrefabDir = "Assets/Prefabs/Enemies";
        private const string OrcRunnerRigFolder = "Assets/Art/Enemies/Skeletal/OrcRunner";
        private static readonly string[] OrcPrefabNames =
        {
            "OrcEnemy",
            "OrcRunner",
            "OrcBrute",
            "OrcShielded",
            "OrcRegenerator",
            "OrcFlying"
        };

        [MenuItem("DragonTD/Enemies/Apply Skeletal Rig From Selected Folder")]
        public static void ApplySelectedFolderToEnemyPrefab()
        {
            string folderPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (string.IsNullOrEmpty(folderPath) || !AssetDatabase.IsValidFolder(folderPath))
            {
                Debug.LogWarning("[EnemySkeletalRigBuilder] Select an enemy skeletal export folder in the Project window first.");
                return;
            }

            ApplyFolderToEnemyPrefab(folderPath);
        }

        public static void ApplyOrcRunnerSkeletalRig()
        {
            ApplyFolderToEnemyPrefab(OrcRunnerRigFolder, "OrcRunner");
        }

        public static void ApplyOrcRunnerRigToAllOrcPrefabs()
        {
            foreach (string prefabName in OrcPrefabNames)
                ApplyFolderToEnemyPrefab(OrcRunnerRigFolder, prefabName);
        }

        private static void ApplyFolderToEnemyPrefab(string folderPath)
        {
            string enemyName = Path.GetFileName(folderPath.TrimEnd('/', '\\'));
            ApplyFolderToEnemyPrefab(folderPath, enemyName);
        }

        private static void ApplyFolderToEnemyPrefab(string folderPath, string enemyName)
        {
            string prefabPath = $"{PrefabDir}/{enemyName}.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[EnemySkeletalRigBuilder] No enemy prefab found at {prefabPath}. Name the export folder after the enemy prefab, e.g. OrcRunner.");
                return;
            }

            RigDefinition rig = LoadRigDefinition(folderPath);
            List<PartDefinition> parts = ResolveParts(folderPath, rig);
            if (parts.Count == 0)
            {
                Debug.LogWarning($"[EnemySkeletalRigBuilder] No PNG part sprites found in {folderPath}.");
                return;
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null) return;

            Transform existingRig = instance.transform.Find("SkeletalRig");
            if (existingRig != null)
                Object.DestroyImmediate(existingRig.gameObject);

            var rigRoot = new GameObject("SkeletalRig");
            rigRoot.transform.SetParent(instance.transform, false);
            rigRoot.transform.localPosition = rig.rootPosition;
            rigRoot.transform.localScale = rig.rootScale == Vector3.zero ? Vector3.one : rig.rootScale;

            var animator = rigRoot.AddComponent<EnemySkeletalAnimator>();
            var bindMap = new Dictionary<string, Transform>();

            foreach (PartDefinition part in parts)
            {
                Sprite sprite = LoadPartSprite(folderPath, part.sprite, rig.pixelsPerUnit);
                if (sprite == null) continue;

                var partObject = new GameObject(part.name);
                partObject.transform.SetParent(rigRoot.transform, false);
                partObject.transform.localPosition = part.position;
                partObject.transform.localRotation = Quaternion.Euler(0f, 0f, part.rotation);
                partObject.transform.localScale = part.scale == Vector3.zero ? Vector3.one : part.scale;

                var sr = partObject.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.color = Color.white;
                sr.sortingOrder = part.sortingOrder;
                bindMap[Normalize(part.name)] = partObject.transform;
            }

            BindAnimator(animator, rigRoot.transform, bindMap);
            DisableRootSprite(instance);

            PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            Object.DestroyImmediate(instance);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[EnemySkeletalRigBuilder] Applied skeletal rig to {prefabPath}.");
        }

        private static RigDefinition LoadRigDefinition(string folderPath)
        {
            string jsonPath = $"{folderPath}/rig.json";
            TextAsset json = AssetDatabase.LoadAssetAtPath<TextAsset>(jsonPath);
            if (json == null)
                return RigDefinition.Default();

            RigDefinition rig = JsonUtility.FromJson<RigDefinition>(json.text);
            return rig ?? RigDefinition.Default();
        }

        private static List<PartDefinition> ResolveParts(string folderPath, RigDefinition rig)
        {
            var parts = new List<PartDefinition>();
            if (rig.parts != null && rig.parts.Length > 0)
            {
                parts.AddRange(rig.parts);
                return parts;
            }

            string[] spriteGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { folderPath });
            foreach (string guid in spriteGuids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (!assetPath.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase))
                    continue;

                string name = Path.GetFileNameWithoutExtension(assetPath);
                parts.Add(PartDefinition.FromName(name, $"{name}.png"));
            }

            parts.Sort((a, b) => a.sortingOrder.CompareTo(b.sortingOrder));
            return parts;
        }

        private static Sprite LoadPartSprite(string folderPath, string spriteFile, float pixelsPerUnit)
        {
            string path = $"{folderPath}/{spriteFile}";
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.spritePixelsPerUnit = pixelsPerUnit > 0f ? pixelsPerUnit : 1024f;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void BindAnimator(EnemySkeletalAnimator animator, Transform rigRoot, Dictionary<string, Transform> parts)
        {
            var so = new SerializedObject(animator);
            so.FindProperty("_facingRoot").objectReferenceValue = rigRoot;
            so.FindProperty("_body").objectReferenceValue = Find(parts, "body", "torso", "chest", "hips");
            so.FindProperty("_head").objectReferenceValue = Find(parts, "head", "face");
            so.FindProperty("_tail").objectReferenceValue = Find(parts, "tail", "scarf", "cape");
            so.FindProperty("_frontUpperLeg").objectReferenceValue = Find(parts, "front_upper_leg", "front_thigh", "right_upper_leg", "right_thigh", "leg_front_upper");
            so.FindProperty("_frontLowerLeg").objectReferenceValue = Find(parts, "front_lower_leg", "front_shin", "right_lower_leg", "right_shin", "leg_front_lower", "front_foot");
            so.FindProperty("_frontArm").objectReferenceValue = Find(parts, "front_arm", "front_upper_arm", "right_arm", "weapon_arm", "arm_front");
            so.FindProperty("_backUpperLeg").objectReferenceValue = Find(parts, "back_upper_leg", "back_thigh", "left_upper_leg", "left_thigh", "leg_back_upper");
            so.FindProperty("_backLowerLeg").objectReferenceValue = Find(parts, "back_lower_leg", "back_shin", "left_lower_leg", "left_shin", "leg_back_lower", "back_foot");
            so.FindProperty("_backArm").objectReferenceValue = Find(parts, "back_arm", "back_upper_arm", "left_arm", "shield_arm", "arm_back");
            so.ApplyModifiedProperties();
        }

        private static Transform Find(Dictionary<string, Transform> parts, params string[] aliases)
        {
            foreach (string alias in aliases)
                if (parts.TryGetValue(Normalize(alias), out Transform part))
                    return part;
            return null;
        }

        private static void DisableRootSprite(GameObject enemy)
        {
            SpriteRenderer sr = enemy.GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.enabled = false;
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrEmpty(value)
                ? string.Empty
                : value.ToLowerInvariant().Replace(" ", "_").Replace("-", "_");
        }

        [System.Serializable]
        private class RigDefinition
        {
            public Vector3 rootPosition = Vector3.zero;
            public Vector3 rootScale = Vector3.one;
            public float pixelsPerUnit = 1024f;
            public PartDefinition[] parts;

            public static RigDefinition Default()
            {
                return new RigDefinition
                {
                    rootPosition = Vector3.zero,
                    rootScale = Vector3.one,
                    pixelsPerUnit = 1024f,
                    parts = System.Array.Empty<PartDefinition>()
                };
            }
        }

        [System.Serializable]
        private class PartDefinition
        {
            public string name;
            public string sprite;
            public Vector3 position;
            public Vector3 scale = Vector3.one;
            public float rotation;
            public int sortingOrder;

            public static PartDefinition FromName(string name, string sprite)
            {
                return new PartDefinition
                {
                    name = name,
                    sprite = sprite,
                    position = DefaultPosition(name),
                    scale = Vector3.one,
                    sortingOrder = DefaultSortingOrder(name)
                };
            }

            private static Vector3 DefaultPosition(string name)
            {
                string normalized = Normalize(name);
                if (normalized.Contains("head") || normalized.Contains("face")) return new Vector3(-0.18f, 0.44f, 0f);
                if (normalized.Contains("tail") || normalized.Contains("scarf") || normalized.Contains("cape")) return new Vector3(-0.22f, 0.2f, 0f);
                if (normalized.Contains("front_upper_leg") || normalized.Contains("right_upper_leg") || normalized.Contains("front_thigh")) return new Vector3(0.12f, -0.18f, 0f);
                if (normalized.Contains("front_lower_leg") || normalized.Contains("right_lower_leg") || normalized.Contains("front_shin") || normalized.Contains("front_foot")) return new Vector3(0.2f, -0.48f, 0f);
                if (normalized.Contains("back_upper_leg") || normalized.Contains("left_upper_leg") || normalized.Contains("back_thigh")) return new Vector3(-0.12f, -0.18f, 0f);
                if (normalized.Contains("back_lower_leg") || normalized.Contains("left_lower_leg") || normalized.Contains("back_shin") || normalized.Contains("back_foot")) return new Vector3(-0.2f, -0.48f, 0f);
                if (normalized.Contains("front_arm") || normalized.Contains("right_arm") || normalized.Contains("weapon_arm")) return new Vector3(0.22f, 0.15f, 0f);
                if (normalized.Contains("back_arm") || normalized.Contains("left_arm") || normalized.Contains("shield_arm")) return new Vector3(-0.12f, 0.14f, 0f);
                return Vector3.zero;
            }

            private static int DefaultSortingOrder(string name)
            {
                string normalized = Normalize(name);
                if (normalized.Contains("back")) return 2;
                if (normalized.Contains("tail") || normalized.Contains("cape")) return 1;
                if (normalized.Contains("body") || normalized.Contains("torso")) return 4;
                if (normalized.Contains("head") || normalized.Contains("face")) return 7;
                if (normalized.Contains("front")) return 8;
                return 5;
            }
        }
    }
}
#endif
