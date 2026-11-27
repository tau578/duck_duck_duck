using UnityEngine;
using UnityEngine.UI;

namespace Duck
{
    /// <summary>Small FPS label for device testing; hidden in release builds.</summary>
    public class FpsCounter : MonoBehaviour
    {
        public Text label;
        public float interval = 0.5f;

        int frames;
        float timer;

        void Awake()
        {
            if (!Debug.isDebugBuild) gameObject.SetActive(false);
        }

        void Update()
        {
            frames++;
            timer += Time.unscaledDeltaTime;
            if (timer < interval) return;
            float fps = frames / timer;
            if (label != null) label.text = $"{fps:0} FPS";
            frames = 0;
            timer = 0f;
        }
    }
}
