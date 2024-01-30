using MyAssets.MiniGames.Scripts;
using UnityEngine.Video;

namespace Scenes.ElectricityShocl_Scene.Video
{
    public class VideoController : MiniGame
    {
        public VideoPlayer videoPlayer;

        public override void Awake() {}

        public override void StartGame()
        {
            base.StartGame();
            videoPlayer.loopPointReached += OnVideoEnd;
            videoPlayer.Play();
        }

        private void OnVideoEnd(VideoPlayer source)
        {
            source.loopPointReached -= OnVideoEnd;
                
            EndGame();
        }
    }
}