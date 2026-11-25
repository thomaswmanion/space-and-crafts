using UnityEngine;

namespace SpaceAndCrafts.Art
{
    /// <summary>
    /// If a Meshy FBX shipped with Animation/Animator clips, play the first idle-like loop.
    /// No new generation — only existing clips on existing assets.
    /// </summary>
    public sealed class MeshyIdleAnim : MonoBehaviour
    {
        private void Start()
        {
            TryPlayAnimator();
            TryPlayLegacyAnimation();
        }

        private void TryPlayAnimator()
        {
            var animators = GetComponentsInChildren<Animator>(true);
            foreach (var anim in animators)
            {
                if (anim == null || anim.runtimeAnimatorController == null)
                {
                    continue;
                }

                anim.enabled = true;
                anim.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
                // Prefer a clip/state whose name suggests idle; else play default state.
                var infos = anim.runtimeAnimatorController.animationClips;
                if (infos == null || infos.Length == 0)
                {
                    continue;
                }

                AnimationClip pick = null;
                foreach (var clip in infos)
                {
                    if (clip == null)
                    {
                        continue;
                    }

                    var n = clip.name.ToLowerInvariant();
                    if (n.Contains("idle") || n.Contains("wait") || n.Contains("loop") || n.Contains("breath"))
                    {
                        pick = clip;
                        break;
                    }
                }

                pick ??= infos[0];
                if (pick != null && anim.HasState(0, Animator.StringToHash(pick.name)))
                {
                    // Animator.Play addresses a controller state, not an
                    // AnimationClip subasset. A clip can be present in the
                    // controller while its state has a different name; in
                    // that case leave the controller's default state running.
                    anim.Play(pick.name, 0, Random.value);
                }
            }
        }

        private void TryPlayLegacyAnimation()
        {
            var anims = GetComponentsInChildren<Animation>(true);
            foreach (var anim in anims)
            {
                if (anim == null)
                {
                    continue;
                }

                anim.playAutomatically = true;
                anim.enabled = true;
                string pick = null;
                foreach (AnimationState st in anim)
                {
                    if (st == null || st.clip == null)
                    {
                        continue;
                    }

                    var n = st.clip.name.ToLowerInvariant();
                    if (n.Contains("idle") || n.Contains("wait") || n.Contains("loop") || n.Contains("breath"))
                    {
                        pick = st.clip.name;
                        break;
                    }

                    pick ??= st.clip.name;
                }

                if (!string.IsNullOrEmpty(pick))
                {
                    var state = anim[pick];
                    if (state != null)
                    {
                        state.wrapMode = WrapMode.Loop;
                        anim.Play(pick);
                    }
                }
            }
        }
    }
}
