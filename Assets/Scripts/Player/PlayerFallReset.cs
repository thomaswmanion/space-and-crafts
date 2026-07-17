using SpaceAndCrafts.Core;
using UnityEngine;

namespace SpaceAndCrafts.Player
{
    public sealed class PlayerFallReset : MonoBehaviour
    {
        [SerializeField] private float restartBelowY = -8f;

        private bool restarting;

        private void Update()
        {
            if (!restarting && transform.position.y < restartBelowY)
            {
                restarting = true;
                GameManager.Instance.RestartCurrentLevel();
            }
        }
    }
}
