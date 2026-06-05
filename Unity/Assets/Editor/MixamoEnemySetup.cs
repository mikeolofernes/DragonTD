#if UNITY_EDITOR
using System.Collections.Generic;
using DragonTD.TowerDefense;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace DragonTD.Editor
{
    public static class MixamoEnemySetup
    {
        private const string MixamoDir = "Assets/Art/Enemies/Mixamo";
        private const string ControllerPath = "Assets/Art/Enemies/Animation/Controllers/MixamoEnemy.controller";

        private static readonly string[] TargetPrefabPaths =
        {
            "Assets/Prefabs/Enemies/EmberWraith.prefab",
            "Assets/Prefabs/Enemies/FrostBrute.prefab",
            "Assets/Prefabs/Enemies/GlacialShield.prefab",
            "Assets/Prefabs/Enemies/IceShard.prefab",
            "Assets/Prefabs/Enemies/LavaHound.prefab",
            "Assets/Prefabs/Enemies/MagmaGolem.prefab",
            "Assets/Prefabs/Enemies/OrcRunner.prefab",
            "Assets/Prefabs/Enemies/OrcEnemy.prefab",
            "Assets/Prefabs/Enemies/OrcBrute.prefab",
            "Assets/Prefabs/Enemies/OrcFlying.prefab",
            "Assets/Prefabs/Enemies/OrcShielded.prefab",
            "Assets/Prefabs/Enemies/OrcRegenerator.prefab"
        };

        [MenuItem("DragonTD/Enemies/Install Mixamo Enemy Animator")]
        public static void InstallMixamoEnemyAnimator()
        {
            EnsureFolders();

            AnimationClip idleClip = FindClip("idle");
            AnimationClip walkClip = FindClip("walk") ?? FindClip("run");
            GameObject modelPrefab = FindModelPrefab();

            if (idleClip == null || walkClip == null || modelPrefab == null)
            {
                Debug.LogError("[MixamoEnemySetup] Put Mixamo FBX files in Assets/Art/Enemies/Mixamo. Need one model FBX plus clips with names containing Idle and Walk.");
                return;
            }

            AnimatorController controller = BuildController(idleClip, walkClip);
            foreach (string prefabPath in TargetPrefabPaths)
                WirePrefab(prefabPath, modelPrefab, controller);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[MixamoEnemySetup] Installed Mixamo model, Animator Controller, and Speed-driven animation on enemy prefabs.");
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/Art", "Enemies");
            EnsureFolder("Assets/Art/Enemies", "Mixamo");
            EnsureFolder("Assets/Art/Enemies", "Animation");
            EnsureFolder("Assets/Art/Enemies/Animation", "Controllers");
        }

        private static void EnsureFolder(string parent, string folder)
        {
            string path = $"{parent}/{folder}";
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, folder);
        }

        private static AnimationClip FindClip(string namePart)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:AnimationClip", new[] { MixamoDir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if (clip != null && clip.name.ToLowerInvariant().Contains(namePart))
                    return clip;
            }

            return null;
        }

        private static GameObject FindModelPrefab()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { MixamoDir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string lower = path.ToLowerInvariant();
                if (lower.Contains("idle") || lower.Contains("walk") || lower.Contains("run"))
                    continue;

                GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (model != null)
                    return model;
            }

            return null;
        }

        private static AnimatorController BuildController(AnimationClip idleClip, AnimationClip walkClip)
        {
            AssetDatabase.DeleteAsset(ControllerPath);
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Death", AnimatorControllerParameterType.Trigger);

            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            machine.states = System.Array.Empty<ChildAnimatorState>();
            machine.anyStateTransitions = System.Array.Empty<AnimatorStateTransition>();

            AnimatorState idle = machine.AddState("Idle", new Vector3(240f, 0f, 0f));
            AnimatorState walk = machine.AddState("Walk", new Vector3(480f, 0f, 0f));
            idle.motion = idleClip;
            walk.motion = walkClip;
            machine.defaultState = idle;

            AnimatorStateTransition toWalk = idle.AddTransition(walk);
            toWalk.hasExitTime = false;
            toWalk.duration = 0.12f;
            toWalk.AddCondition(AnimatorConditionMode.Greater, 0.01f, "Speed");

            AnimatorStateTransition toIdle = walk.AddTransition(idle);
            toIdle.hasExitTime = false;
            toIdle.duration = 0.12f;
            toIdle.AddCondition(AnimatorConditionMode.Less, 0.01f, "Speed");

            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void WirePrefab(string prefabPath, GameObject modelPrefab, AnimatorController controller)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) return;

            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null) return;

            RemoveChild(instance.transform, "DirectionalSpriteView");
            RemoveChild(instance.transform, "MixamoModel");

            Transform skeletalRig = instance.transform.Find("SkeletalRig");
            if (skeletalRig != null)
                skeletalRig.gameObject.SetActive(false);

            foreach (SpriteRenderer renderer in instance.GetComponentsInChildren<SpriteRenderer>(true))
                renderer.enabled = false;

            GameObject model = PrefabUtility.InstantiatePrefab(modelPrefab, instance.transform) as GameObject;
            if (model != null)
            {
                model.name = "MixamoModel";
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;
                model.transform.localScale = Vector3.one;
            }

            Animator animator = model != null ? model.GetComponentInChildren<Animator>(true) : null;
            if (animator == null && model != null)
                animator = model.AddComponent<Animator>();
            if (animator != null)
            {
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }

            EnemyAnimatorBridge bridge = instance.GetComponent<EnemyAnimatorBridge>();
            if (bridge == null)
                bridge = instance.AddComponent<EnemyAnimatorBridge>();

            var so = new SerializedObject(bridge);
            so.FindProperty("_animator").objectReferenceValue = animator;
            so.FindProperty("_spriteRenderer").objectReferenceValue = null;
            so.FindProperty("_facingRoot").objectReferenceValue = instance.transform;
            so.FindProperty("_frontView").objectReferenceValue = null;
            so.FindProperty("_rightView").objectReferenceValue = null;
            so.FindProperty("_backView").objectReferenceValue = null;
            so.FindProperty("_leftView").objectReferenceValue = null;
            so.FindProperty("_usesDirectionalSpriteFrames").boolValue = false;
            so.FindProperty("_modelRoot").objectReferenceValue = model != null ? model.transform : null;
            so.FindProperty("_rotateModelRootToFacing").boolValue = true;
            so.FindProperty("_modelYawOffset").floatValue = 0f;
            so.FindProperty("_mixamoSpeedFloat").stringValue = "Speed";
            so.ApplyModifiedProperties();

            EnemyDirectionalSpriteAnimator spriteAnimator = instance.GetComponent<EnemyDirectionalSpriteAnimator>();
            if (spriteAnimator != null)
                Object.DestroyImmediate(spriteAnimator);

            PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            Object.DestroyImmediate(instance);
        }

        private static void RemoveChild(Transform root, string name)
        {
            Transform child = root.Find(name);
            if (child != null)
                Object.DestroyImmediate(child.gameObject);
        }
    }
}
#endif
