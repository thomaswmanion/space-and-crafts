using SpaceAndCrafts.Gameplay;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpaceAndCrafts.Core
{
    /// <summary>
    /// Listens to gameplay events and writes PlayerProgressSave.
    /// </summary>
    public sealed class ProgressSaveBridge : MonoBehaviour
    {
        private static ProgressSaveBridge _instance;

        public static ProgressSaveBridge Ensure()
        {
            if (_instance != null)
            {
                return _instance;
            }

            var existing = FindAnyObjectByType<ProgressSaveBridge>();
            if (existing != null)
            {
                _instance = existing;
                return _instance;
            }

            var go = new GameObject("SYS_ProgressSave");
            if (Application.isPlaying)
            {
                DontDestroyOnLoad(go);
            }

            _instance = go.AddComponent<ProgressSaveBridge>();
            return _instance;
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            if (Application.isPlaying)
            {
                DontDestroyOnLoad(gameObject);
            }

            PlayerProgressSave.Load();
        }

        private void OnEnable()
        {
            GameEvents.BuildCompleted += HandleBuild;
            GameEvents.LevelCompleted += HandleLevelComplete;
            GameEvents.SecretWonderClaimed += HandleWonder;
            GameEvents.CollectibleAcquired += HandleCollectible;
        }

        private void OnDisable()
        {
            GameEvents.BuildCompleted -= HandleBuild;
            GameEvents.LevelCompleted -= HandleLevelComplete;
            GameEvents.SecretWonderClaimed -= HandleWonder;
            GameEvents.CollectibleAcquired -= HandleCollectible;
        }

        private void HandleBuild(string inventionId) => PlayerProgressSave.RecordBuild(inventionId);

        private void HandleLevelComplete()
        {
            PlayerProgressSave.RecordWorldComplete(SceneManager.GetActiveScene().name);
        }

        private void HandleWonder(string wonderId) => PlayerProgressSave.RecordWonder(wonderId);

        private void HandleCollectible(CollectibleDefinition definition)
        {
            if (definition == null)
            {
                return;
            }

            if (definition.Category == CollectibleCategory.DiscoveryBadge)
            {
                PlayerProgressSave.RecordDiscoveryBadge(definition.Id);
            }
            else if (definition.Category == CollectibleCategory.Component)
            {
                PlayerProgressSave.RecordComponent(definition.Id);
            }
        }
    }
}
