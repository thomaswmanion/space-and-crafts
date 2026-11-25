#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using SpaceAndCrafts.AI;

namespace SpaceAndCrafts.EditorTools
{
    public static class World03EncounterAuditCycle73
    {
        private const string ScenePath = "Assets/SpaceAndCrafts/Scenes/Worlds/World03_MagnetMarsh.unity";

        [MenuItem("Space & Crafts/Apply World 03 Far Patrol Staging Cycle 83")]
        public static void ApplyFarPatrolStagingCycle83()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var names = new[] { "ART_ScrapMite_4", "ART_ScrapMite_5" };
            foreach (var name in names)
            {
                var member = GameObject.Find(name);
                var mite = member != null ? member.GetComponent<ScrapMite>() : null;
                if (mite == null)
                {
                    Debug.LogError($"[World 03 staging C83] {name} or its ScrapMite component is missing.");
                    EditorApplication.Exit(1);
                    return;
                }

                var serializedMite = new SerializedObject(mite);
                var noticeRadius = serializedMite.FindProperty("noticeRadius");
                if (noticeRadius == null)
                {
                    Debug.LogError($"[World 03 staging C83] {name} has no noticeRadius field.");
                    EditorApplication.Exit(1);
                    return;
                }

                noticeRadius.floatValue = 3.0f;
                serializedMite.ApplyModifiedPropertiesWithoutUndo();
                Debug.Log($"[World 03 staging C83] {name} notice radius={noticeRadius.floatValue:F2}m");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            var saved = EditorSceneManager.SaveScene(scene);
            Debug.Log($"[World 03 staging C83] staged two far patrols for later pad approach; saved={saved}.");
            EditorApplication.Exit(saved ? 0 : 1);
        }

        public static void ApplyFarPatrolStagingCycle84()
        {
            ApplyNoticeRadius("World 03 staging C84", 2.6f);
        }

        public static void ApplyLateralFarPatrolSpreadCycle85()
        {
            ApplyLateralSpread("World 03 spread C85",
                new[] { "ART_ScrapMite_4", "ART_ScrapMite_5" });
        }

        public static void ApplyLateralRemainingPatrolSpreadCycle86()
        {
            ApplyLateralSpread("World 03 spread C86",
                new[] { "ART_ScrapMite_3", "ART_ScrapMite_6", "ART_ScrapMite_7" });
        }

        public static void ApplyNearSidePatrolShiftCycle87()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var anchor = GameObject.Find("ART_ScrapMite_7");
            if (anchor == null)
            {
                Debug.LogError("[World 03 shift C87] Missing ART_ScrapMite_7.");
                EditorApplication.Exit(1);
                return;
            }

            var local = anchor.transform.localPosition;
            local.x *= 2.2f;
            anchor.transform.localPosition = local;
            EditorSceneManager.MarkSceneDirty(scene);
            var saved = EditorSceneManager.SaveScene(scene);
            Debug.Log($"[World 03 shift C87] ART_ScrapMite_7 local={local}, saved={saved}");
            EditorApplication.Exit(saved ? 0 : 1);
        }

        public static void ApplyOuterPatrolSpreadCycle88()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            foreach (var name in new[] { "ART_ScrapMite_6", "ART_ScrapMite_7" })
            {
                var anchor = GameObject.Find(name);
                if (anchor == null)
                {
                    Debug.LogError($"[World 03 outer spread C88] Missing {name}.");
                    EditorApplication.Exit(1);
                    return;
                }

                var local = anchor.transform.localPosition;
                local.x *= 1.5f;
                anchor.transform.localPosition = local;
                Debug.Log($"[World 03 outer spread C88] {name} local={local}");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            var saved = EditorSceneManager.SaveScene(scene);
            Debug.Log($"[World 03 outer spread C88] saved={saved}");
            EditorApplication.Exit(saved ? 0 : 1);
        }

        public static void RestoreNearSidePatrolCycle87()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var restore = new Dictionary<string, float>
            {
                { "ART_ScrapMite_6", -2.97f },
                { "ART_ScrapMite_7", 2.50f }
            };

            foreach (var entry in restore)
            {
                var anchor = GameObject.Find(entry.Key);
                if (anchor == null)
                {
                    Debug.LogError($"[World 03 restore C88] Missing {entry.Key}.");
                    EditorApplication.Exit(1);
                    return;
                }

                var local = anchor.transform.localPosition;
                local.x = entry.Value;
                anchor.transform.localPosition = local;
                Debug.Log($"[World 03 restore C88] {entry.Key} local={local}");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            var saved = EditorSceneManager.SaveScene(scene);
            Debug.Log($"[World 03 restore C88] saved={saved}");
            EditorApplication.Exit(saved ? 0 : 1);
        }

        public static void ApplyPackForwardStagingCycle90()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var pack = GameObject.Find("ART_EncounterPack");
            if (pack == null)
            {
                Debug.LogError("[World 03 staging C90] ART_EncounterPack is missing.");
                EditorApplication.Exit(1);
                return;
            }

            var position = pack.transform.position;
            position.z += 0.35f;
            pack.transform.position = position;
            EditorSceneManager.MarkSceneDirty(scene);
            var saved = EditorSceneManager.SaveScene(scene);
            Debug.Log($"[World 03 staging C90] pack world={pack.transform.position}, saved={saved}");
            EditorApplication.Exit(saved ? 0 : 1);
        }

        public static void ApplyPursuitCapCycle91()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var mites = Object.FindObjectsByType<ScrapMite>(FindObjectsInactive.Exclude);
            var changed = 0;
            foreach (var mite in mites)
            {
                if (mite == null) continue;
                var serializedMite = new SerializedObject(mite);
                var limitEnabled = serializedMite.FindProperty("limitConcurrentPursuers");
                var limit = serializedMite.FindProperty("concurrentPursuerLimit");
                if (limitEnabled == null || limit == null)
                {
                    Debug.LogError($"[World 03 pursuit C91] {mite.name} missing pursuit fields.");
                    EditorApplication.Exit(1);
                    return;
                }

                limitEnabled.boolValue = true;
                limit.intValue = 2;
                serializedMite.ApplyModifiedPropertiesWithoutUndo();
                changed++;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            var saved = EditorSceneManager.SaveScene(scene);
            Debug.Log($"[World 03 pursuit C91] enabled two-pursuer limit on {changed} Mites, saved={saved}");
            EditorApplication.Exit(saved ? 0 : 1);
        }

        public static void ApplyPadTwoApproachSpacingCycle97()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var anchor = GameObject.Find("ART_ScrapMite_4");
            if (anchor == null)
            {
                Debug.LogError("[World 03 pad spacing C97] Missing ART_ScrapMite_4.");
                EditorApplication.Exit(1);
                return;
            }

            var local = anchor.transform.localPosition;
            local.z = 1.9972187f;
            anchor.transform.localPosition = local;
            var beacon = GameObject.Find("ART_Wonder_w3_0");
            if (beacon == null)
            {
                Debug.LogError("[World 03 pad spacing C97] Missing ART_Wonder_w3_0.");
                EditorApplication.Exit(1);
                return;
            }
            beacon.transform.localScale = Vector3.one * 1.05f;
            EditorSceneManager.MarkSceneDirty(scene);
            var saved = EditorSceneManager.SaveScene(scene);
            Debug.Log($"[World 03 signpost C97] restored ART_ScrapMite_4 patrol anchor {anchor.transform.position} (local={local}) and increased ART_Wonder_w3_0 visual scale to {beacon.transform.localScale}; saved={saved}");
            EditorApplication.Exit(saved ? 0 : 1);
        }

        public static void ApplySinglePursuerCycle92()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var mites = Object.FindObjectsByType<ScrapMite>(FindObjectsInactive.Exclude);
            foreach (var mite in mites)
            {
                if (mite == null) continue;
                var serializedMite = new SerializedObject(mite);
                var limit = serializedMite.FindProperty("concurrentPursuerLimit");
                if (limit == null)
                {
                    Debug.LogError($"[World 03 pursuit C92] {mite.name} missing concurrentPursuerLimit.");
                    EditorApplication.Exit(1);
                    return;
                }

                limit.intValue = 1;
                serializedMite.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            var saved = EditorSceneManager.SaveScene(scene);
            Debug.Log($"[World 03 pursuit C92] set one active pursuer on {mites.Length} Mites, saved={saved}");
            EditorApplication.Exit(saved ? 0 : 1);
        }

        private static void ApplyLateralSpread(string label, string[] names)
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            foreach (var name in names)
            {
                var anchor = GameObject.Find(name);
                if (anchor == null)
                {
                    Debug.LogError($"[{label}] Missing anchor {name}.");
                    EditorApplication.Exit(1);
                    return;
                }

                var local = anchor.transform.localPosition;
                local.x *= 1.4f;
                anchor.transform.localPosition = local;
                Debug.Log($"[{label}] {name} local={local}");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            var saved = EditorSceneManager.SaveScene(scene);
            Debug.Log($"[{label}] saved={saved}");
            EditorApplication.Exit(saved ? 0 : 1);
        }

        private static void ApplyNoticeRadius(string label, float radius)
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var names = new[] { "ART_ScrapMite_4", "ART_ScrapMite_5" };
            foreach (var name in names)
            {
                var member = GameObject.Find(name);
                var mite = member != null ? member.GetComponent<ScrapMite>() : null;
                if (mite == null)
                {
                    Debug.LogError($"[{label}] Missing {name} or ScrapMite component.");
                    EditorApplication.Exit(1);
                    return;
                }

                var serializedMite = new SerializedObject(mite);
                var noticeRadius = serializedMite.FindProperty("noticeRadius");
                if (noticeRadius == null)
                {
                    Debug.LogError($"[{label}] {name} has no noticeRadius field.");
                    EditorApplication.Exit(1);
                    return;
                }

                noticeRadius.floatValue = radius;
                serializedMite.ApplyModifiedPropertiesWithoutUndo();
                Debug.Log($"[{label}] {name} notice radius={noticeRadius.floatValue:F2}m");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            var saved = EditorSceneManager.SaveScene(scene);
            Debug.Log($"[{label}] save={saved}");
            EditorApplication.Exit(saved ? 0 : 1);
        }

        [MenuItem("Space & Crafts/Apply World 03 Staggered Notice Range Cycle 79")]
        public static void ApplyStaggeredNoticeRangeCycle79()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var names = new[] { "ART_ScrapMite_4", "ART_ScrapMite_5" };
            foreach (var name in names)
            {
                var member = GameObject.Find(name);
                var mite = member != null ? member.GetComponent<ScrapMite>() : null;
                if (mite == null)
                {
                    Debug.LogError($"[World 03 aggro C79] {name} or its ScrapMite component is missing.");
                    EditorApplication.Exit(1);
                    return;
                }

                var serializedMite = new SerializedObject(mite);
                var noticeRadius = serializedMite.FindProperty("noticeRadius");
                if (noticeRadius == null)
                {
                    Debug.LogError($"[World 03 aggro C79] {name} has no noticeRadius field.");
                    EditorApplication.Exit(1);
                    return;
                }

                noticeRadius.floatValue = 3.35f;
                serializedMite.ApplyModifiedPropertiesWithoutUndo();
                Debug.Log($"[World 03 aggro C79] {name} notice radius={noticeRadius.floatValue:F2}m");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            var saved = EditorSceneManager.SaveScene(scene);
            Debug.Log($"[World 03 aggro C79] staggered two outer Mite notice ranges; saved={saved}.");
            EditorApplication.Exit(saved ? 0 : 1);
        }

        [MenuItem("Space & Crafts/Apply World 03 Softer Contact Cycle 76")]
        public static void ApplySofterContactCycle76()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var members = GameObject.Find("ART_EncounterPack")?.GetComponentsInChildren<ScrapMite>(true);
            if (members == null || members.Length != 5)
            {
                Debug.LogError($"[World 03 contact C76] Expected five Scrap Mites, found {members?.Length ?? 0}.");
                EditorApplication.Exit(1);
                return;
            }

            foreach (var mite in members)
            {
                var serializedMite = new SerializedObject(mite);
                var speed = serializedMite.FindProperty("contactKnockbackSpeed");
                if (speed == null)
                {
                    Debug.LogError($"[World 03 contact C76] {mite.name} is missing contactKnockbackSpeed.");
                    EditorApplication.Exit(1);
                    return;
                }

                speed.floatValue = 6.5f;
                serializedMite.ApplyModifiedPropertiesWithoutUndo();
                Debug.Log($"[World 03 contact C76] {mite.name} contact knockback={speed.floatValue:F1}m/s");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            var saved = EditorSceneManager.SaveScene(scene);
            Debug.Log($"[World 03 contact C76] applied reduced bump force to five encounter Mites; saved={saved}.");
            EditorApplication.Exit(saved ? 0 : 1);
        }

        [MenuItem("Space & Crafts/Apply World 03 Lateral Mite Stagger Cycle 73")]
        public static void ApplyLateralStagger()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var pack = GameObject.Find("ART_EncounterPack");
            if (pack == null)
            {
                Debug.LogError("[World 03 encounter C73] ART_EncounterPack is missing.");
                EditorApplication.Exit(1);
                return;
            }

            var moved = 0;
            var members = pack.GetComponentsInChildren<Transform>(true);
            foreach (var member in members)
            {
                if (member == pack.transform || !member.name.StartsWith("ART_ScrapMite_"))
                    continue;

                member.localPosition = new Vector3(member.localPosition.x * 1.25f,
                    member.localPosition.y, member.localPosition.z);
                moved++;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            var saved = EditorSceneManager.SaveScene(scene);
            Debug.Log($"[World 03 encounter C73] widened {moved} mite patrol anchors laterally by 25%; saved={saved}.");
            EditorApplication.Exit(saved ? 0 : 1);
        }

        [MenuItem("Space & Crafts/Audit World 03 Encounter Members Cycle 73")]
        public static void Audit()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var pack = GameObject.Find("ART_EncounterPack");
            if (pack == null)
            {
                Debug.LogError("[World 03 encounter audit C73] ART_EncounterPack is missing.");
                EditorApplication.Exit(1);
                return;
            }

            Debug.Log($"[World 03 encounter audit C73] pack world={pack.transform.position}");
            var members = pack.GetComponentsInChildren<Transform>(true);
            foreach (var member in members)
            {
                if (member == pack.transform || !member.name.StartsWith("ART_ScrapMite_"))
                    continue;

                var colliders = member.GetComponentsInChildren<Collider>(true);
                var colliderSummary = string.Empty;
                foreach (var collider in colliders)
                    colliderSummary += $" {collider.GetType().Name}(trigger={collider.isTrigger}, enabled={collider.enabled})";
                Debug.Log($"[World 03 encounter audit C73] member={member.name} local={member.localPosition} world={member.position} parent={member.parent?.name} colliders={colliderSummary}");
            }

            EditorApplication.Exit(0);
        }
    }
}
#endif
