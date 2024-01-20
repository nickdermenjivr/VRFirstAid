using MyAssets.MiniGames.Scripts;
using UnityEngine;

namespace MyAssets.Collectibles.Scripts
{
    public class CollectibleItem : MiniGame
    {
        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            gameObject.SetActive(false);
            EndGame();
        }
    }
}