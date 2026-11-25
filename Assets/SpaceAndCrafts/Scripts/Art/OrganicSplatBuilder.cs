using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SpaceAndCrafts.Art
{
    /// <summary>
    /// Builds the approved compact Splat silhouette from cohesive soft meshes.
    /// Older skinned imports remain isolated candidates because their appearance
    /// and/or deformation does not match the approved design well enough to ship.
    /// </summary>
    public static class OrganicSplatBuilder
    {
        public const string VisualRootName = "ART_SplatVisual";
        public const string FaceVersionMarkerName = "ART_SplatFace_v66";
        public const string HipsName = "Bone_Hips";
        public const string SpineName = "Bone_Spine";
        public const string ChestName = "Bone_Chest";
        public const string HeadName = "Bone_Head";
        // Retained for animator compatibility; the approved Splat has no antenna.
        public const string AntennaName = "Bone_Antenna";
        public const string ArmLName = "Bone_ArmL";
        public const string ArmRName = "Bone_ArmR";

        public static Transform BuildVisualRoot(Transform splatRoot, System.Action<GameObject, string, Color, float, float> applyMat)
        {
            // Rebuilds can be requested by more than one startup path. Replace any
            // existing direct visual roots so there is never a doubled face/body.
            for (var i = splatRoot.childCount - 1; i >= 0; i--)
            {
                var child = splatRoot.GetChild(i);
                if (child.name != VisualRootName)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    child.gameObject.SetActive(false);
                    child.name = VisualRootName + "_Retired";
                    Object.Destroy(child.gameObject);
                }
                else
                {
                    Object.DestroyImmediate(child.gameObject);
                }
            }

            // Meshy rig candidates are not runtime-ready: the most recent design
            // match is static, while the older humanoid imports have face and
            // contact defects. Build the current approved concept until a single
            // cohesive, tested compatible rig clears the same acceptance gates.
            var visual = new GameObject(VisualRootName);
            visual.transform.SetParent(splatRoot, false);
            var versionMarker = new GameObject(FaceVersionMarkerName);
            versionMarker.transform.SetParent(visual.transform, false);

            // Keep the animator's familiar hierarchy, but give Splat a compact,
            // soft silhouette instead of the imported hero's lanky human proportions.
            var hips = Bone(visual.transform, HipsName, new Vector3(0f, 0.35f, 0f));
            var spine = Bone(hips, SpineName, new Vector3(0f, 0.22f, 0f));
            var chest = Bone(spine, ChestName, new Vector3(0f, 0.2f, 0f));
            var head = Bone(chest, HeadName, new Vector3(0f, 0.28f, 0.05f));
            // Let each continuous arm surface begin farther inside the soft body.
            // The previous shoulder pivots sat almost at the body's widest point,
            // so the purple limbs read like detached mittens in side-lit captures.
            var armL = Bone(chest, ArmLName, new Vector3(-0.48f, -0.04f, 0.03f));
            var armR = Bone(chest, ArmRName, new Vector3(0.48f, -0.04f, 0.03f));
            var armTipL = Bone(armL, "Bone_ArmTipL", new Vector3(-0.27f, 0f, 0f));
            var armTipR = Bone(armR, "Bone_ArmTipR", new Vector3(0.27f, 0f, 0f));
            var legL = Bone(hips, "Bone_LegL", new Vector3(-0.40f, -0.08f, -0.08f));
            var legR = Bone(hips, "Bone_LegR", new Vector3(0.40f, -0.08f, -0.08f));
            var footL = Bone(legL, "Bone_FootL", new Vector3(0f, -0.08f, 0.02f));
            var footR = Bone(legR, "Bone_FootR", new Vector3(0f, -0.08f, 0.02f));

            var body = Make(chest, "ART_SplatBody", OrganicMeshFactory.SoftBlob(4, 0.59f, 1.0f, 0.025f));
            body.transform.localPosition = new Vector3(0f, -0.05f, 0f);
            body.transform.localScale = Vector3.one;
            applyMat(body, "Mat_SplatBody", new Color(0.88f, 0.83f, 0.72f), 0f, 0.12f);

            // Keep the complete face/cap assembly on the same chest transform
            // as the white shell. Rotating a tiny head pivot independently made
            // the eyes and smile visibly peel away from the body's curved front.
            var cap = Make(chest, "ART_BackwardsCap", OrganicMeshFactory.BackwardsBaseballCap());
            // Seat the back half of the open crown on the soft body and raise
            // its front edge slightly so the brow line remains readable.
            cap.transform.localPosition = new Vector3(0f, 0.41f, 0.035f);
            cap.transform.localRotation = Quaternion.Euler(-9f, 0f, 0f);
            applyMat(cap, "Mat_SplatBackwardsCap", new Color(0.24f, 0.035f, 0.72f), 0f, 0.18f);
            var capStrap = Make(chest, "ART_BackwardsCapStrap", OrganicMeshFactory.BackwardsCapFrontStrap());
            capStrap.transform.localPosition = cap.transform.localPosition;
            capStrap.transform.localRotation = cap.transform.localRotation;
            applyMat(capStrap, "Mat_SplatCapStrap", new Color(0.38f, 0.15f, 0.80f), 0f, 0.22f);

            MakeEye(chest, "ART_EyeRootL", new Vector3(-0.205f, 0.215f, 0.396f), applyMat);
            MakeEye(chest, "ART_EyeRootR", new Vector3(0.205f, 0.215f, 0.396f), applyMat);
            MakeBrow(chest, "ART_SplatBrowL", -1f, applyMat);
            MakeBrow(chest, "ART_SplatBrowR", 1f, applyMat);

            var smilePath = new List<Vector3>(17);
            for (var i = 0; i <= 16; i++)
            {
                var t = i / 16f;
                var arc = Mathf.Sin(t * Mathf.PI);
                smilePath.Add(new Vector3(
                    Mathf.Lerp(-0.115f, 0.115f, t),
                    0.125f - arc * 0.06f,
                    0.53f + arc * 0.022f));
            }
            var smile = Make(chest, "ART_SplatSmile", OrganicMeshFactory.TubeAlongPath(
                smilePath, 0.018f, 12, "Splat_Smile"));
            applyMat(smile, "Mat_SplatSmile", new Color(0.045f, 0.035f, 0.065f), 0f, 0.1f);

            MakeFoot(legL, footL, -1f, applyMat);
            MakeFoot(legR, footR, 1f, applyMat);

            MakeArm(armL, armTipL, -1f, -18f, applyMat);
            MakeArm(armR, armTipR, 1f, 18f, applyMat);

            return visual.transform;
        }

        private static Material _iter310SkinMaterial;
        private static bool _iter310ShaderWarningLogged;
        private static Material _authoredSkinMaterial;
        private static bool _authoredShaderWarningLogged;

        private static void ApplyAuthoredVertexColorSkin(GameObject visual)
        {
            if (_authoredSkinMaterial == null)
            {
                var shader = Shader.Find("SpaceAndCrafts/SplatVertexColorLit");
                if (shader == null)
                {
                    if (!_authoredShaderWarningLogged)
                    {
                        Debug.LogWarning("Splat vertex-color shader is unavailable; the imported Blender material will be used.");
                        _authoredShaderWarningLogged = true;
                    }
                    return;
                }

                _authoredSkinMaterial = new Material(shader)
                {
                    name = "Mat_Splat_Authored_VertexColor",
                    hideFlags = HideFlags.DontSave
                };
                // The Iter378 vertex paint reads too pink under Arrival's cool light.
                // Nudge red/green down while preserving blue so the hero reads violet
                // in-game, without changing the mesh or its painted facial details.
                _authoredSkinMaterial.SetColor("_BaseColor", new Color(0.56f, 0.72f, 1f, 1f));
                _authoredSkinMaterial.SetColor("_SkinColor", new Color(0.31f, 0.12f, 0.57f, 1f));
            }

            foreach (var renderer in visual.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (renderer == null || !(renderer.name.Contains("ART_SplatBody_Iter355")
                    || renderer.name.Contains("ART_SplatBody_Iter375")
                    || renderer.name.Contains("ART_SplatBody_Iter378")))
                {
                    continue;
                }

                renderer.sharedMaterial = _authoredSkinMaterial;
                renderer.GetPropertyBlock(_authoredSkinProperties);
                _authoredSkinProperties.SetFloat("_EyeBlink", 0f);
                _authoredSkinProperties.SetVector("_EyeExpression", new Vector4(1f, 1f, 0f, 0f));
                _authoredSkinProperties.SetVector("_EyeGaze", Vector4.zero);
                renderer.SetPropertyBlock(_authoredSkinProperties);
            }
        }

        private static readonly MaterialPropertyBlock _authoredSkinProperties = new();

        private static void ApplyIter310VertexColorSkin(GameObject visual)
        {
            if (_iter310SkinMaterial == null)
            {
                var shader = Shader.Find("SpaceAndCrafts/SplatVertexColorLit");
                if (shader == null)
                {
                    if (!_iter310ShaderWarningLogged)
                    {
                        Debug.LogWarning("Splat Iter310 vertex-color shader is unavailable; its Blender material will be used.");
                        _iter310ShaderWarningLogged = true;
                    }
                    return;
                }

                _iter310SkinMaterial = new Material(shader)
                {
                    name = "Mat_Splat_Iter310_VertexColor",
                    hideFlags = HideFlags.DontSave
                };
                _iter310SkinMaterial.SetColor("_BaseColor", Color.white);
                _iter310SkinMaterial.SetColor("_SkinColor", new Color(0.31f, 0.12f, 0.57f, 1f));
            }

            foreach (var renderer in visual.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (renderer == null || !renderer.name.Contains("ART_SplatBody_Iter310"))
                {
                    continue;
                }

                renderer.sharedMaterial = _iter310SkinMaterial;
                renderer.GetPropertyBlock(_iter310SkinProperties);
                _iter310SkinProperties.SetFloat("_EyeBlink", 0f);
                _iter310SkinProperties.SetVector("_EyeExpression", new Vector4(1f, 1f, 0f, 0f));
                _iter310SkinProperties.SetVector("_EyeGaze", Vector4.zero);
                renderer.SetPropertyBlock(_iter310SkinProperties);
            }
        }

        private static readonly MaterialPropertyBlock _iter310SkinProperties = new();

        public static bool ContainsBone(Transform visualRoot, string boneName)
        {
            return ContainsNamedTransform(visualRoot, boneName);
        }

        private static bool ContainsNamedTransform(Transform root, string name)
        {
            if (root == null)
            {
                return false;
            }

            if (root.name == name)
            {
                return true;
            }

            for (var i = 0; i < root.childCount; i++)
            {
                if (ContainsNamedTransform(root.GetChild(i), name))
                {
                    return true;
                }
            }

            return false;
        }

        private static Transform Bone(Transform parent, string name, Vector3 localPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            return go.transform;
        }

        private static void MakeEye(Transform parent, string rootName, Vector3 localPos, System.Action<GameObject, string, Color, float, float> applyMat)
        {
            var eyeRoot = Bone(parent, rootName, localPos);
            // Keep the eyes tall and compact. Both irises use one deep-violet
            // color; a single tiny catchlight gives them warmth without rings,
            // teal layers, cheeks, or the wide startled look of earlier passes.
            eyeRoot.localScale = Vector3.one * 0.86f;
            var iris = Make(eyeRoot, "ART_Iris", OrganicMeshFactory.SoftBlob(3, 0.125f, 1f, 0.002f));
            iris.transform.localPosition = new Vector3(0f, 0f, 0.035f);
            iris.transform.localScale = new Vector3(0.80f, 1.42f, 0.52f);
            applyMat(iris, "Mat_SplatEye", new Color(0.30f, 0.075f, 0.78f), 0f, 0.20f);

            var catchlight = Make(eyeRoot, "ART_EyeCatchlight",
                OrganicMeshFactory.SoftBlob(2, 0.024f, 1f, 0.001f));
            catchlight.transform.localPosition = new Vector3(-0.030f, 0.045f, 0.098f);
            catchlight.transform.localScale = new Vector3(1f, 1f, 0.34f);
            applyMat(catchlight, "Mat_SplatEyeGlint", new Color(1f, 0.99f, 0.96f), 0f, 0.05f);
        }

        private static void MakeBrow(Transform parent, string browName, float side,
            System.Action<GameObject, string, Color, float, float> applyMat)
        {
            // Keep each short arch flush with the curved shell. A round tube
            // catches the perspective at the eyes and reads as a stray spike.
            var brow = Make(parent, browName, OrganicMeshFactory.SplatFaceBrow(side));
            applyMat(brow, "Mat_SplatBrow", new Color(0.38f, 0.12f, 0.86f), 0f, 0.16f);
        }

        private static void MakeArm(Transform armBone, Transform wristBone, float side, float zRot,
            System.Action<GameObject, string, Color, float, float> applyMat)
        {
            armBone.localRotation = Quaternion.Euler(0f, 0f, zRot * 0.35f);
            var paw = new GameObject(side < 0f ? "ART_PurplePawL" : "ART_PurplePawR");
            paw.transform.SetParent(armBone, false);
            var skin = paw.AddComponent<SkinnedMeshRenderer>();
            skin.sharedMesh = OrganicMeshFactory.SkinnedPawArm(Mathf.RoundToInt(side), 0.40f,
                radialSegments: 20, lengthSegments: 20, radiusScale: 0.84f);
            skin.bones = new[] { armBone, wristBone };
            skin.rootBone = armBone;
            skin.quality = SkinQuality.Bone2;
            skin.localBounds = skin.sharedMesh.bounds;
            var bindPoses = new Matrix4x4[2];
            bindPoses[0] = armBone.worldToLocalMatrix * paw.transform.localToWorldMatrix;
            bindPoses[1] = wristBone.worldToLocalMatrix * paw.transform.localToWorldMatrix;
            skin.sharedMesh.bindposes = bindPoses;
            applyMat(paw, "Mat_SplatLimb", new Color(0.24f, 0.035f, 0.72f), 0f, 0.12f);
        }

        private static void MakeFoot(Transform leg, Transform footBone, float side, System.Action<GameObject, string, Color, float, float> applyMat)
        {
            var legMesh = Make(leg, side < 0f ? "ART_PurpleLegL" : "ART_PurpleLegR",
                OrganicMeshFactory.SoftBlob(4, 0.19f, 1.0f, 0.012f));
            legMesh.transform.localPosition = new Vector3(0f, -0.055f, 0.015f);
            legMesh.transform.localScale = new Vector3(0.78f, 1.3f, 0.86f);
            applyMat(legMesh, "Mat_SplatLimb", new Color(0.24f, 0.035f, 0.72f), 0f, 0.12f);

            var foot = Make(footBone, side < 0f ? "ART_PurpleFootL" : "ART_PurpleFootR",
                OrganicMeshFactory.SplatFoot(4, 0.21f, 0.78f, 0.012f));
            foot.transform.localPosition = new Vector3(0f, -0.08f, 0.055f);
            foot.transform.localScale = new Vector3(1.00f, 0.74f, 0.98f);
            applyMat(foot, "Mat_SplatLimb", new Color(0.24f, 0.035f, 0.72f), 0f, 0.12f);
        }

        private static GameObject Make(Transform parent, string name, Mesh mesh)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            go.AddComponent<MeshRenderer>();
            return go;
        }
    }
}
