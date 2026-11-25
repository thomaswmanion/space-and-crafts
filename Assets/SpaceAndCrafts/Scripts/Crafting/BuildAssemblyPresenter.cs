using System.Collections;
using SpaceAndCrafts.Art;
using SpaceAndCrafts.Audio;
using SpaceAndCrafts.Cameras;
using SpaceAndCrafts.UI;
using UnityEngine;

namespace SpaceAndCrafts.Crafting
{
    /// <summary>
    /// TEMP presentation for craft assembly: pieces snap in sequence, then invention pops.
    /// Replace with authored VFX later; keep labeled TEMP.
    /// </summary>
    public sealed class BuildAssemblyPresenter : MonoBehaviour
    {
        [SerializeField] private Transform[] pieceRoots;
        [SerializeField] private GameObject inventionRoot;
        [SerializeField] private float pieceSnapDuration = 0.35f;
        [SerializeField] private float betweenPieces = 0.12f;
        [SerializeField] private float inventionPopDuration = 0.35f;

        private Vector3[] _startLocalPositions;
        private Vector3[] _startLocalScales;
        private Quaternion[] _startLocalRotations;

        public bool IsAssembling { get; private set; }

        private void OnDisable()
        {
            IsAssembling = false;
        }

        private void Awake()
        {
            CacheStarts();
            if (inventionRoot != null)
            {
                inventionRoot.SetActive(false);
            }
        }

        public void PlayAssembly()
        {
            StopAllCoroutines();
            IsAssembling = true;
            StartCoroutine(AssembleRoutine());
        }

        private void CacheStarts()
        {
            if (pieceRoots == null)
            {
                return;
            }

            _startLocalPositions = new Vector3[pieceRoots.Length];
            _startLocalScales = new Vector3[pieceRoots.Length];
            _startLocalRotations = new Quaternion[pieceRoots.Length];
            for (var i = 0; i < pieceRoots.Length; i++)
            {
                if (pieceRoots[i] == null)
                {
                    continue;
                }

                _startLocalPositions[i] = pieceRoots[i].localPosition;
                _startLocalScales[i] = pieceRoots[i].localScale;
                _startLocalRotations[i] = pieceRoots[i].localRotation;
                pieceRoots[i].gameObject.SetActive(false);
            }
        }

        private IEnumerator AssembleRoutine()
        {
            // GOAL personality: machine sputters before it works (fits inside BuildSite wait).
            GameplayFeedback.Toast("Whirr… sputter… almost!");
            var rest = transform.localPosition;
            var sputter = 0f;
            while (sputter < 0.28f)
            {
                sputter += Time.deltaTime;
                var kick = Random.insideUnitSphere * 0.04f;
                kick.y = 0f;
                transform.localPosition = rest + kick;
                yield return null;
            }

            transform.localPosition = rest;

            if (pieceRoots == null || pieceRoots.Length == 0)
            {
                yield return PopInvention();
                IsAssembling = false;
                yield break;
            }

            for (var i = 0; i < pieceRoots.Length; i++)
            {
                if (pieceRoots[i] == null)
                {
                    continue;
                }

                yield return SnapPiece(i);
                if (betweenPieces > 0f)
                {
                    yield return new WaitForSeconds(betweenPieces);
                }
            }

            yield return new WaitForSeconds(0.1f);

            for (var i = 0; i < pieceRoots.Length; i++)
            {
                if (pieceRoots[i] != null)
                {
                    pieceRoots[i].gameObject.SetActive(false);
                }
            }

            yield return PopInvention();
            IsAssembling = false;
        }

        private IEnumerator SnapPiece(int index)
        {
            var piece = pieceRoots[index];
            var targetPos = _startLocalPositions[index];
            var targetScale = _startLocalScales[index];
            var targetRot = _startLocalRotations[index];
            var fromPos = targetPos + Vector3.up * 1.8f + Vector3.right * ((index - 1) * 0.55f);
            var spin = 280f * (index % 2 == 0 ? 1f : -1f);

            piece.gameObject.SetActive(true);
            piece.localPosition = fromPos;
            piece.localScale = targetScale * 0.15f;
            piece.localRotation = Quaternion.Euler(25f, 0f, -20f);

            var elapsed = 0f;
            while (elapsed < pieceSnapDuration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.SmoothStep(0f, 1f, elapsed / pieceSnapDuration);
                piece.localPosition = Vector3.Lerp(fromPos, targetPos, t);
                piece.localScale = Vector3.Lerp(targetScale * 0.15f, targetScale * 1.15f, t);
                piece.localRotation = Quaternion.Euler(0f, spin * t, Mathf.Lerp(-20f, 0f, t));
                yield return null;
            }

            // Soft land / weld settle
            elapsed = 0f;
            const float settle = 0.12f;
            while (elapsed < settle)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.SmoothStep(0f, 1f, elapsed / settle);
                piece.localScale = Vector3.Lerp(targetScale * 1.15f, targetScale, t);
                piece.localPosition = targetPos;
                piece.localRotation = Quaternion.Slerp(piece.localRotation, targetRot, t);
                yield return null;
            }

            piece.localPosition = targetPos;
            piece.localScale = targetScale;
            piece.localRotation = targetRot;
            TempCraftSparkBurst.Spawn(piece.position, 7, 0.35f);
            // Weld Chorus: rising pitch per piece; invention pop keeps the big craft stinger.
            TempSfxPlayer.PlayWeldSnap(index);
            var cam = Object.FindAnyObjectByType<PlatformingCameraRig>();
            if (cam != null)
            {
                cam.Punch(0.028f + index * 0.008f);
            }
        }

        private IEnumerator PopInvention()
        {
            if (inventionRoot == null)
            {
                yield break;
            }

            inventionRoot.SetActive(true);
            var root = inventionRoot.transform;
            TempCraftSparkBurst.Spawn(root.position + Vector3.up * 0.4f, 14, 0.55f);
            root.localScale = Vector3.one * 0.15f;
            var elapsed = 0f;
            while (elapsed < inventionPopDuration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.SmoothStep(0f, 1f, elapsed / inventionPopDuration);
                // Overshoot pop then settle
                var scale = t < 0.7f
                    ? Mathf.Lerp(0.15f, 1.2f, t / 0.7f)
                    : Mathf.Lerp(1.2f, 1f, (t - 0.7f) / 0.3f);
                root.localScale = Vector3.one * scale;
                yield return null;
            }

            root.localScale = Vector3.one;
        }
    }
}
