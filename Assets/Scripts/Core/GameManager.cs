using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpaceAndCrafts.Core
{
    /// <summary>Owns run-wide state such as the player's crystal total.</summary>
    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public int CrystalCount { get; private set; }

        public event Action<int> CrystalCountChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void AddCrystals(int amount)
        {
            if (amount <= 0)
            {
                Debug.LogWarning("Crystal amount must be greater than zero.", this);
                return;
            }

            CrystalCount += amount;
            CrystalCountChanged?.Invoke(CrystalCount);
            Debug.Log($"Crystals: {CrystalCount}");
        }

        public void RestartCurrentLevel()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void ResetRun()
        {
            CrystalCount = 0;
            CrystalCountChanged?.Invoke(CrystalCount);
        }
    }
}
