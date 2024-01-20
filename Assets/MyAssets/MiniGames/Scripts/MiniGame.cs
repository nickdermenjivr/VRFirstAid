using UnityEngine;
using UnityEngine.Events;

namespace MyAssets.MiniGames.Scripts
{
    public class MiniGame : MonoBehaviour
    {
        public UnityEvent onGameComplete;
        public virtual void Awake()
        {
            gameObject.SetActive(false);
        }

        public virtual void StartGame()
        {
            gameObject.SetActive(true);
        }

        public virtual void EndGame()
        {
            onGameComplete?.Invoke();
            gameObject.SetActive(false);
        }
    }
}
