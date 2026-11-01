using UnityEngine;
using UnityEngine.UI;

namespace SpinForFortune
{
    // フィーバー中のローカルプレイヤーに、無敵中より細く控えめな金色の縁取りを表示する。
    public class RouletteFeverFrameUI : MonoBehaviour
    {
        private const float GlowSize = 120f;
        private const float GlowAlpha = 0.8f;

        private static RouletteFeverFrameUI instance;

        private readonly Image[] edges = new Image[4];

        public static void SetVisible(bool visible)
        {
            if (visible && instance == null)
            {
                var canvasObject = new GameObject("SpinForFortune_RouletteFeverFrameUI");
                DontDestroyOnLoad(canvasObject);
                instance = canvasObject.AddComponent<RouletteFeverFrameUI>();
                instance.Build();
            }

            if (instance != null)
                instance.gameObject.SetActive(visible);
        }

        private void Update()
        {
            float pulse = 0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 2.2f);
            var color = new Color(1f, 0.78f, 0.2f, GlowAlpha * pulse);
            foreach (var edge in edges)
                edge.color = color;
        }

        private void Build()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 6100;

            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            edges[0] = CreateEdge("Top", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, GlowSize), RouletteInvulnerabilityUI.CreateGradient(true, true));
            edges[1] = CreateEdge("Right", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(GlowSize, 0f), RouletteInvulnerabilityUI.CreateGradient(false, false));
            edges[2] = CreateEdge("Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, GlowSize), RouletteInvulnerabilityUI.CreateGradient(true, false));
            edges[3] = CreateEdge("Left", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(GlowSize, 0f), RouletteInvulnerabilityUI.CreateGradient(false, true));
        }

        private Image CreateEdge(string edgeName, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 size, Sprite sprite)
        {
            var edgeObject = new GameObject(edgeName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            edgeObject.transform.SetParent(transform, false);

            var rect = edgeObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.sizeDelta = size;

            var image = edgeObject.GetComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            return image;
        }
    }
}
