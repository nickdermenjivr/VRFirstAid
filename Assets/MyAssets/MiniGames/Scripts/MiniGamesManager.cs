using System.Collections.Generic;
using UnityEngine;

namespace MyAssets.MiniGames.Scripts
{
    public class MiniGamesManager : MonoBehaviour
    {
        public List<MiniGame> miniGames;

        protected virtual void Start()
        {
            StartMiniGame(0);
        }

        protected virtual void OnEnable()
        {
            for (var i = 0; i < miniGames.Count; i++)
            {
                var index = i;
                miniGames[i].onGameComplete.AddListener(() => OnMiniGameComplete(index));
            }
        }

        protected virtual void OnDisable()
        {
            foreach (var g in miniGames)
            {
                g.onGameComplete.RemoveAllListeners();
            }
        }
        
        protected virtual void OnMiniGameComplete(int index)
        {
            if (index >= miniGames.Count-1)
            {
                return;
            }

            StartMiniGame(++index);
        }

        protected virtual void StartMiniGame(int index)
        {
            miniGames[index].gameObject.SetActive(true);
            miniGames[index].StartGame();
        }
    }
}