#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using DragonTD.TowerDefense;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace DragonTD.Editor
{
    public static class EnemyAnimatorControllerBuilder
    {
        private const string ControllersDir = "Assets/Art/Enemies/Animation/Controllers";
        private const string ClipsDir = "Assets/Art/Enemies/Animation/Clips";
        private const string DirectionalDir = "Assets/Art/Enemies/Directional";
        private const string OrcRunnerControllerPath = ControllersDir + "/OrcRunner.controller";
        private static readonly string[] TargetPrefabPaths =
        {
            "Assets/Prefabs/Enemies/OrcRunner.prefab",
            "Assets/Prefabs/Enemies/OrcEnemy.prefab",
            "Assets/Prefabs/Enemies/OrcBrute.prefab",
            "Assets/Prefabs/Enemies/OrcFlying.prefab",
            "Assets/Prefabs/Enemies/OrcShielded.prefab",
            "Assets/Prefabs/Enemies/OrcRegenerator.prefab"
        };

        [MenuItem("DragonTD/Enemies/Build OrcRunner Animator Prototype")]
        public static void BuildOrcRunnerAnimatorPrototype()
        {
            EnsureFolders();

            AnimationClip idle = CreateIdleClip("OrcRunner_Idle", 1.2f);
            AnimationClip run = CreateRunClip("OrcRunner_Run", 0.55f);
            AnimationClip hit = CreateHitClip("OrcRunner_Hit", 0.18f);
            AnimationClip stunned = CreateStunnedClip("OrcRunner_Stunned", 0.8f);
            AnimationClip death = CreateDeathClip("OrcRunner_Death", 0.45f);
            AnimatorController controller = CreateController(idle, run, hit, stunned, death);

            foreach (string prefabPath in TargetPrefabPaths)
                WirePrefab(controller, prefabPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[EnemyAnimatorControllerBuilder] Built OrcRunner Animator Controller and wired orc prefabs.");
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/Art/Enemies", "Animation");
            EnsureFolder("Assets/Art/Enemies/Animation", "Controllers");
            EnsureFolder("Assets/Art/Enemies/Animation", "Clips");
            EnsureFolder("Assets/Art/Enemies", "Directional");
        }

        private static void EnsureFolder(string parent, string folder)
        {
            string path = $"{parent}/{folder}";
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, folder);
        }

        private static AnimatorController CreateController(AnimationClip idle, AnimationClip run, AnimationClip hit, AnimationClip stunned, AnimationClip death)
        {
            AssetDatabase.DeleteAsset(OrcRunnerControllerPath);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(OrcRunnerControllerPath);
            controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Speed01", AnimatorControllerParameterType.Float);
            controller.AddParameter("FacingX", AnimatorControllerParameterType.Float);
            controller.AddParameter("FacingY", AnimatorControllerParameterType.Float);
            controller.AddParameter("FacingDirection", AnimatorControllerParameterType.Int);
            controller.AddParameter("IsStunned", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Death", AnimatorControllerParameterType.Trigger);

            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            machine.states = System.Array.Empty<ChildAnimatorState>();
            machine.anyStateTransitions = System.Array.Empty<AnimatorStateTransition>();

            AnimatorState idleState = machine.AddState("Idle", new Vector3(240f, 0f, 0f));
            AnimatorState runState = machine.AddState("Run", new Vector3(480f, 0f, 0f));
            AnimatorState hitState = machine.AddState("Hit", new Vector3(360f, -160f, 0f));
            AnimatorState stunnedState = machine.AddState("Stunned", new Vector3(240f, -320f, 0f));
            AnimatorState deathState = machine.AddState("Death", new Vector3(600f, -320f, 0f));

            idleState.motion = idle;
            runState.motion = run;
            hitState.motion = hit;
            stunnedState.motion = stunned;
            deathState.motion = death;
            deathState.writeDefaultValues = true;
            machine.defaultState = idleState;

            AddBoolTransition(idleState, runState, "IsMoving", true);
            AddBoolTransition(runState, idleState, "IsMoving", false);
            AddBoolTransition(idleState, stunnedState, "IsStunned", true);
            AddBoolTransition(runState, stunnedState, "IsStunned", true);
            AddBoolTransition(stunnedState, idleState, "IsStunned", false);
            AddTriggerTransition(machine, hitState, "Hit");
            AddTriggerTransition(machine, deathState, "Death");
            AddExitTransition(hitState, idleState, 0.16f);

            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void AddBoolTransition(AnimatorState from, AnimatorState to, string parameter, bool value)
        {
            AnimatorStateTransition transition = from.AddTransition(to);
            transition.hasExitTime = false;
            transition.duration = 0.05f;
            transition.AddCondition(value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, parameter);
        }

        private static void AddTriggerTransition(AnimatorStateMachine machine, AnimatorState to, string parameter)
        {
            AnimatorStateTransition transition = machine.AddAnyStateTransition(to);
            transition.hasExitTime = false;
            transition.duration = 0.02f;
            transition.canTransitionToSelf = false;
            transition.AddCondition(AnimatorConditionMode.If, 0f, parameter);
        }

        private static void AddExitTransition(AnimatorState from, AnimatorState to, float exitTime)
        {
            AnimatorStateTransition transition = from.AddTransition(to);
            transition.hasExitTime = true;
            transition.exitTime = exitTime;
            transition.duration = 0.04f;
        }

        private static AnimationClip CreateIdleClip(string name, float duration)
        {
            var clip = NewClip(name, duration, true);
            AddFloatCurve(clip, "SkeletalRig/body", "m_LocalPosition.y", 0f, 0.0459f, duration * 0.5f, 0.06f, duration, 0.0459f);
            AddScaleCurve(clip, "SkeletalRig/body", duration, 1f, 1.04f, 1f);
            AddRotationCurve(clip, "SkeletalRig/head", duration, 0f, -2f, 0f);
            return SaveClip(clip);
        }

        private static AnimationClip CreateRunClip(string name, float duration)
        {
            var clip = NewClip(name, duration, true);
            AddFloatCurve(clip, "SkeletalRig/body", "m_LocalPosition.y", 0f, 0.0459f, duration * 0.25f, 0.095f, duration * 0.5f, 0.0459f, duration * 0.75f, 0.095f, duration, 0.0459f);
            AddRotationCurve(clip, "SkeletalRig/front_upper_leg", duration, -24f, 24f, -24f);
            AddRotationCurve(clip, "SkeletalRig/back_upper_leg", duration, 24f, -24f, 24f);
            AddRotationCurve(clip, "SkeletalRig/front_lower_leg", duration, 16f, -18f, 16f);
            AddRotationCurve(clip, "SkeletalRig/back_lower_leg", duration, -18f, 16f, -18f);
            AddRotationCurve(clip, "SkeletalRig/front_arm", duration, 16f, -12f, 16f);
            AddRotationCurve(clip, "SkeletalRig/back_arm", duration, -12f, 16f, -12f);
            AddRotationCurve(clip, "SkeletalRig/tail", duration, -10f, 10f, -10f);
            return SaveClip(clip);
        }

        private static AnimationClip CreateHitClip(string name, float duration)
        {
            var clip = NewClip(name, duration, false);
            AddScaleCurve(clip, "SkeletalRig", duration, 1f, 1.1f, 1f);
            AddRotationCurve(clip, "SkeletalRig/body", duration, 0f, -8f, 0f);
            return SaveClip(clip);
        }

        private static AnimationClip CreateStunnedClip(string name, float duration)
        {
            var clip = NewClip(name, duration, true);
            AddRotationCurve(clip, "SkeletalRig", duration, -3f, 3f, -3f);
            AddScaleCurve(clip, "SkeletalRig", duration, 1f, 0.96f, 1f);
            return SaveClip(clip);
        }

        private static AnimationClip CreateDeathClip(string name, float duration)
        {
            var clip = NewClip(name, duration, false);
            AddScaleCurve(clip, "SkeletalRig", duration, 1f, 1.08f, 0.55f);
            AddRotationCurve(clip, "SkeletalRig", duration, 0f, 12f, -80f);
            AddFloatCurve(clip, "SkeletalRig", "m_LocalPosition.y", 0f, 0f, duration * 0.45f, 0.08f, duration, -0.18f);
            return SaveClip(clip);
        }

        private static AnimationClip NewClip(string name, float duration, bool loop)
        {
            var clip = new AnimationClip { name = name, frameRate = 12f };
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            settings.stopTime = duration;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            return clip;
        }

        private static AnimationClip SaveClip(AnimationClip clip)
        {
            string path = $"{ClipsDir}/{clip.name}.anim";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(clip, path);
            return AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        }

        private static void AddScaleCurve(AnimationClip clip, string path, float duration, float start, float middle, float end)
        {
            AddFloatCurve(clip, path, "m_LocalScale.x", 0f, start, duration * 0.5f, middle, duration, end);
            AddFloatCurve(clip, path, "m_LocalScale.y", 0f, start, duration * 0.5f, middle, duration, end);
            AddFloatCurve(clip, path, "m_LocalScale.z", 0f, 1f, duration * 0.5f, 1f, duration, 1f);
        }

        private static void AddRotationCurve(AnimationClip clip, string path, float duration, float startZ, float middleZ, float endZ)
        {
            AnimationCurve z = new AnimationCurve(
                Key(0f, startZ),
                Key(duration * 0.5f, middleZ),
                Key(duration, endZ));
            clip.SetCurve(path, typeof(Transform), "localEulerAnglesRaw.z", z);
        }

        private static void AddFloatCurve(AnimationClip clip, string path, string property, params float[] keyPairs)
        {
            var keys = new Keyframe[keyPairs.Length / 2];
            for (int i = 0; i < keys.Length; i++)
                keys[i] = Key(keyPairs[i * 2], keyPairs[i * 2 + 1]);
            clip.SetCurve(path, typeof(Transform), property, new AnimationCurve(keys));
        }

        private static Keyframe Key(float time, float value)
        {
            var key = new Keyframe(time, value);
            key.inTangent = 0f;
            key.outTangent = 0f;
            return key;
        }

        private static void WirePrefab(AnimatorController controller, string prefabPath)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[EnemyAnimatorControllerBuilder] Missing prefab: {prefabPath}");
                return;
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null) return;

            Animator animator = instance.GetComponent<Animator>();
            if (animator != null)
                Object.DestroyImmediate(animator);

            EnemyAnimatorBridge bridge = instance.GetComponent<EnemyAnimatorBridge>();
            if (bridge == null)
                bridge = instance.AddComponent<EnemyAnimatorBridge>();

            Transform skeletalRig = instance.transform.Find("SkeletalRig");
            if (skeletalRig != null)
                skeletalRig.gameObject.SetActive(false);

            SpriteRenderer rootRenderer = instance.GetComponent<SpriteRenderer>();
            if (rootRenderer != null)
                rootRenderer.enabled = false;

            RemoveDirectionalView(instance.transform, "FrontView");
            RemoveDirectionalView(instance.transform, "RightView");
            RemoveDirectionalView(instance.transform, "BackView");
            RemoveDirectionalView(instance.transform, "LeftView");
            RemoveDirectionalView(instance.transform, "DirectionalSpriteView");
            Sprite[] frontFrames = LoadDirectionalWalkSprites("front");
            Sprite[] rightFrames = LoadDirectionalWalkSprites("right");
            Sprite[] backFrames = LoadDirectionalWalkSprites("back");
            Sprite[] leftFrames = rightFrames;
            SpriteRenderer frontRenderer = EnsureDirectionalView(instance.transform, "FrontView", frontFrames, 0f);
            SpriteRenderer rightRenderer = EnsureDirectionalView(instance.transform, "RightView", rightFrames, 0f);
            SpriteRenderer backRenderer = EnsureDirectionalView(instance.transform, "BackView", backFrames, 0f);
            SpriteRenderer leftRenderer = EnsureDirectionalView(instance.transform, "LeftView", leftFrames, 0f);
            frontRenderer.flipX = false;
            rightRenderer.flipX = false;
            backRenderer.flipX = false;
            leftRenderer.flipX = true;
            frontRenderer.gameObject.SetActive(false);
            rightRenderer.gameObject.SetActive(true);
            backRenderer.gameObject.SetActive(false);
            leftRenderer.gameObject.SetActive(false);

            var so = new SerializedObject(bridge);
            so.FindProperty("_animator").objectReferenceValue = null;
            so.FindProperty("_spriteRenderer").objectReferenceValue = null;
            so.FindProperty("_facingRoot").objectReferenceValue = instance.transform;
            so.FindProperty("_frontView").objectReferenceValue = frontRenderer.gameObject;
            so.FindProperty("_rightView").objectReferenceValue = rightRenderer.gameObject;
            so.FindProperty("_backView").objectReferenceValue = backRenderer.gameObject;
            so.FindProperty("_leftView").objectReferenceValue = leftRenderer.gameObject;
            so.FindProperty("_usesDirectionalSpriteFrames").boolValue = true;
            so.ApplyModifiedProperties();

            EnemyDirectionalSpriteAnimator spriteAnimator = instance.GetComponent<EnemyDirectionalSpriteAnimator>();
            if (spriteAnimator == null)
                spriteAnimator = instance.AddComponent<EnemyDirectionalSpriteAnimator>();

            var spriteSo = new SerializedObject(spriteAnimator);
            spriteSo.FindProperty("_bridge").objectReferenceValue = bridge;
            spriteSo.FindProperty("_spriteRenderer").objectReferenceValue = null;
            spriteSo.FindProperty("_frontRenderer").objectReferenceValue = frontRenderer;
            spriteSo.FindProperty("_rightRenderer").objectReferenceValue = rightRenderer;
            spriteSo.FindProperty("_backRenderer").objectReferenceValue = backRenderer;
            spriteSo.FindProperty("_leftRenderer").objectReferenceValue = leftRenderer;
            AssignSpriteArray(spriteSo.FindProperty("_frontWalkFrames"), frontFrames);
            AssignSpriteArray(spriteSo.FindProperty("_rightWalkFrames"), rightFrames);
            AssignSpriteArray(spriteSo.FindProperty("_backWalkFrames"), backFrames);
            AssignSpriteArray(spriteSo.FindProperty("_leftWalkFrames"), leftFrames);
            spriteSo.FindProperty("_walkFps").floatValue = 10f;
            spriteSo.FindProperty("_strideBobAmplitude").floatValue = 0.018f;
            spriteSo.FindProperty("_strideSwayAmplitude").floatValue = 0.006f;
            spriteSo.ApplyModifiedProperties();

            PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            Object.DestroyImmediate(instance);
        }

        private static void RemoveDirectionalView(Transform root, string name)
        {
            Transform existing = root.Find(name);
            if (existing != null)
                Object.DestroyImmediate(existing.gameObject);
        }

        private static SpriteRenderer EnsureDirectionalView(Transform root, string name, Sprite[] frames, float yOffset)
        {
            Transform existing = root.Find(name);
            if (existing != null)
                Object.DestroyImmediate(existing.gameObject);

            var view = new GameObject(name);
            view.transform.SetParent(root, false);
            view.transform.localPosition = new Vector3(0f, yOffset, 0f);
            view.transform.localScale = Vector3.one;
            var sr = view.AddComponent<SpriteRenderer>();
            sr.sprite = frames != null && frames.Length > 0 ? frames[0] : null;
            sr.sortingOrder = 8;
            sr.color = Color.white;
            view.SetActive(true);
            return sr;
        }

        private static Sprite[] LoadDirectionalWalkSprites(string direction)
        {
            string path = $"{DirectionalDir}/Animation/OrcRunner_walk_{direction}_strip.png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Multiple;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.spritePixelsPerUnit = 320f;
                importer.maxTextureSize = 2048;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.spritesheet = BuildSpriteSheet(direction);
                importer.SaveAndReimport();
            }

            var sprites = new List<Sprite>();
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
                if (asset is Sprite sprite)
                    sprites.Add(sprite);
            sprites.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));
            return sprites.ToArray();
        }

        private static SpriteMetaData[] BuildSpriteSheet(string direction)
        {
            const int frameCount = 6;
            const int frameWidth = 256;
            const int frameHeight = 512;
            var sheet = new SpriteMetaData[frameCount];
            for (int i = 0; i < frameCount; i++)
            {
                sheet[i] = new SpriteMetaData
                {
                    name = $"OrcRunner_{direction}_{i:00}",
                    rect = new Rect(i * frameWidth, 0, frameWidth, frameHeight),
                    alignment = (int)SpriteAlignment.BottomCenter,
                    pivot = new Vector2(0.5f, 0f)
                };
            }

            return sheet;
        }

        private static void AssignSpriteArray(SerializedProperty property, Sprite[] sprites)
        {
            property.arraySize = sprites.Length;
            for (int i = 0; i < sprites.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
        }
    }
}
#endif
