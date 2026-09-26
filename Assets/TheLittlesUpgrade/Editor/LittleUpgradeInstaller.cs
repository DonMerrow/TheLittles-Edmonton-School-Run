#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TheLittles.Upgrade;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace TheLittles.Upgrade.Editor
{
    public static class LittleUpgradeInstaller
    {
        private const string GeneratedFolder = "Assets/TheLittlesUpgrade/Generated";

        [MenuItem("The Littles/Install Traversal Camera and Hair Fix")]
        public static void Install()
        {
            GameObject player = FindPlayer();
            Camera mainCamera = Camera.main != null ? Camera.main : UnityEngine.Object.FindFirstObjectByType<Camera>();
            if (player == null || mainCamera == null)
            {
                EditorUtility.DisplayDialog("The Littles Upgrade",
                    "Open the playable scene first. The installer needs one CharacterController and one Camera.", "OK");
                return;
            }

            CharacterController character = player.GetComponent<CharacterController>();
            Animator animator = player.GetComponentInChildren<Animator>(true);
            Behaviour locomotion = FindLocomotion(player);

            LittleTraversalController traversal = GetOrAdd<LittleTraversalController>(player);
            traversal.Configure(animator, locomotion);
            LittleProceduralClimbIK ik = GetOrAdd<LittleProceduralClimbIK>(player);
            ik.Configure(traversal, animator);
            DisableCompetingClimbers(player, traversal, ik);

            LittleAdaptiveCamera cameraController = GetOrAdd<LittleAdaptiveCamera>(mainCamera.gameObject);
            cameraController.Configure(player.transform, character);
            DisableCompetingCameraDrivers(cameraController);

            int hairMaterials = FixHairMaterials(player);
            EnableAnimatorIK(animator);
            AddAnimatorParameters(animator);

            EditorUtility.SetDirty(player);
            EditorUtility.SetDirty(mainCamera.gameObject);
            EditorSceneManager.MarkSceneDirty(player.scene);
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("The Littles Upgrade Installed",
                "Installed climbing, mantling, procedural climb IK, collision-aware third-person camera, " +
                "first-person view, developer camera, and " + hairMaterials + " corrected hair material(s).\n\n" +
                "Controls: Space at wall = climb; WASD = climb; C = drop; Shift+Space = jump away; " +
                "mouse wheel = zoom; F = first person; V = shoulder; F5 = developer camera.", "Done");
        }

        private static T GetOrAdd<T>(GameObject owner) where T : Component
        {
            T component = owner.GetComponent<T>();
            return component != null ? component : Undo.AddComponent<T>(owner);
        }

        private static GameObject FindPlayer()
        {
            GameObject tagged = GameObject.FindGameObjectWithTag("Player");
            if (tagged != null && tagged.GetComponent<CharacterController>() != null) return tagged;
            CharacterController[] controllers = UnityEngine.Object.FindObjectsByType<CharacterController>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            return controllers.Length > 0 ? controllers[0].gameObject : null;
        }

        private static Behaviour FindLocomotion(GameObject player)
        {
            foreach (Behaviour behaviour in player.GetComponents<Behaviour>())
            {
                string name = behaviour.GetType().Name;
                if (name == "ThirdPersonController" || name == "MothMotor3D") return behaviour;
            }
            return null;
        }

        private static void DisableCompetingClimbers(GameObject player,
            LittleTraversalController traversal, LittleProceduralClimbIK ik)
        {
            foreach (Behaviour behaviour in player.GetComponentsInChildren<Behaviour>(true))
            {
                if (behaviour == traversal || behaviour == ik || behaviour == null) continue;
                string name = behaviour.GetType().Name;
                if (name.IndexOf("Climb", StringComparison.OrdinalIgnoreCase) < 0) continue;
                Undo.RecordObject(behaviour, "Disable old climbing driver");
                behaviour.enabled = false;
                EditorUtility.SetDirty(behaviour);
            }
        }

        private static void DisableCompetingCameraDrivers(LittleAdaptiveCamera keep)
        {
            foreach (Behaviour behaviour in UnityEngine.Object.FindObjectsByType<Behaviour>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (behaviour == keep || behaviour == null || behaviour is Camera) continue;
                Type type = behaviour.GetType();
                string fullName = type.FullName ?? type.Name;
                bool knownDriver = type.Name == "MothOrbitCamera" || type.Name == "CinemachineBrain" ||
                    type.Name.IndexOf("CameraMode", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    type.Name.IndexOf("OrbitCamera", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    type.Name.IndexOf("FollowCamera", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    fullName.IndexOf("CinemachineBrain", StringComparison.OrdinalIgnoreCase) >= 0;
                if (knownDriver)
                {
                    Undo.RecordObject(behaviour, "Disable old camera driver");
                    behaviour.enabled = false;
                    EditorUtility.SetDirty(behaviour);
                }
            }
        }

        private static int FixHairMaterials(GameObject player)
        {
            Directory.CreateDirectory(GeneratedFolder);
            Dictionary<Material, Material> replacements = new Dictionary<Material, Material>();
            int corrected = 0;

            foreach (Renderer renderer in player.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < materials.Length; i++)
                {
                    Material source = materials[i];
                    if (source == null || !LooksLikeHair(renderer, source)) continue;
                    if (!replacements.TryGetValue(source, out Material replacement))
                    {
                        replacement = CreateOrUpdateCutoutMaterial(source);
                        replacements.Add(source, replacement);
                        corrected++;
                    }
                    materials[i] = replacement;
                    changed = true;
                }

                if (changed)
                {
                    Undo.RecordObject(renderer, "Correct Moth hair material");
                    renderer.sharedMaterials = materials;
                    EditorUtility.SetDirty(renderer);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                }
            }
            return corrected;
        }

        private static bool LooksLikeHair(Renderer renderer, Material material)
        {
            string combined = (renderer.name + " " + material.name).ToLowerInvariant();
            return combined.Contains("hair") || combined.Contains("uma3_hair");
        }

        private static Material CreateOrUpdateCutoutMaterial(Material source)
        {
            string safeName = new string(source.name.Select(c =>
                char.IsLetterOrDigit(c) || c == '_' || c == '-' ? c : '_').ToArray());
            string path = AssetDatabase.GenerateUniqueAssetPath(
                GeneratedFolder + "/" + safeName + "_URP_Cutout.mat");
            Texture colorTexture = source.HasProperty("_BaseMap") ? source.GetTexture("_BaseMap") :
                source.HasProperty("_MainTex") ? source.GetTexture("_MainTex") : null;
            Color color = source.HasProperty("_BaseColor") ? source.GetColor("_BaseColor") :
                source.HasProperty("_Color") ? source.GetColor("_Color") : Color.white;
            Material material = new Material(source) { name = source.name + " URP Cutout" };

            Shader urp = Shader.Find("Universal Render Pipeline/Lit");
            if (urp != null && (material.shader == null ||
                material.shader.name.IndexOf("Universal Render Pipeline", StringComparison.OrdinalIgnoreCase) < 0))
                material.shader = urp;

            if (colorTexture != null && material.HasProperty("_BaseMap"))
                material.SetTexture("_BaseMap", colorTexture);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 0f);
            if (material.HasProperty("_AlphaClip")) material.SetFloat("_AlphaClip", 1f);
            if (material.HasProperty("_Cutoff")) material.SetFloat("_Cutoff", 0.32f);
            if (material.HasProperty("_Cull")) material.SetFloat("_Cull", (float)CullMode.Off);
            if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 1f);
            material.EnableKeyword("_ALPHATEST_ON");
            material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetOverrideTag("RenderType", "TransparentCutout");
            material.renderQueue = (int)RenderQueue.AlphaTest;
            material.doubleSidedGI = true;

            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void EnableAnimatorIK(Animator animator)
        {
            if (animator == null) return;
            RuntimeAnimatorController runtime = animator.runtimeAnimatorController;
            if (runtime is AnimatorOverrideController overrideController)
                runtime = overrideController.runtimeAnimatorController;
            if (!(runtime is AnimatorController controller) || controller.layers.Length == 0) return;
            controller.layers[0].iKPass = true;
            EditorUtility.SetDirty(controller);
        }

        private static void AddAnimatorParameters(Animator animator)
        {
            if (animator == null) return;
            RuntimeAnimatorController runtime = animator.runtimeAnimatorController;
            if (runtime is AnimatorOverrideController overrideController)
                runtime = overrideController.runtimeAnimatorController;
            if (!(runtime is AnimatorController controller)) return;

            AddParameter(controller, "Climbing", AnimatorControllerParameterType.Bool);
            AddParameter(controller, "Hanging", AnimatorControllerParameterType.Bool);
            AddParameter(controller, "ClimbHorizontal", AnimatorControllerParameterType.Float);
            AddParameter(controller, "ClimbVertical", AnimatorControllerParameterType.Float);
            EditorUtility.SetDirty(controller);
        }

        private static void AddParameter(AnimatorController controller, string name,
            AnimatorControllerParameterType type)
        {
            if (controller.parameters.Any(parameter => parameter.name == name)) return;
            controller.AddParameter(name, type);
        }
    }
}
#endif
