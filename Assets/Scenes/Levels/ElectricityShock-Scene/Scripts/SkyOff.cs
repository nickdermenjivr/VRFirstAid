using UnityEngine;
using UnityEngine.Rendering;

namespace Scenes.Levels.ElectricityShock_Scene.Scripts
{
    public class SkyOff : MonoBehaviour
    {
        private void OnEnable()
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientIntensity = 0f;
            RenderSettings.ambientLight = new Color(0.07f,0.07f,0.07f);
            RenderSettings.skybox = null;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
        }
    }
}