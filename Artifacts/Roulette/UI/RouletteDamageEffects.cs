using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SpinForFortune
{
    public class RouletteDamageEffects : MonoBehaviour
    {
        private sealed class Spark
        {
            public RectTransform Rect;
            public Image Image;
            public Vector2 Direction;
            public Color Color;
            public float Distance;
            public float Delay;
            public float Rotation;
        }

        private sealed class Impact
        {
            public RectTransform Root;
            public CanvasGroup Group;
            public Image Glow;
            public RectTransform Core;
            public List<Spark> Sparks;
            public RectTransform CaptionRect;
            public Text Caption;
            public float Duration;
            public float Time;
        }

        private static RouletteDamageEffects instance;
        private static Sprite sparkleSprite;
        private static Sprite glowSprite;

        private readonly List<Impact> impacts = new List<Impact>();
        private RectTransform canvasRect;

        public static void ShowImpact(Vector3 worldPosition, RouletteTier tier)
        {
            var camera = Camera.main;
            if (camera == null)
                return;

            Vector3 screenPosition = camera.WorldToScreenPoint(worldPosition);
            if (screenPosition.z <= 0f)
                return;

            if (instance == null)
                CreateInstance();

            instance.AddImpact(screenPosition, tier);
        }

        private static void CreateInstance()
        {
            var gameObject = new GameObject("SpinForFortune_RouletteDamageEffects", typeof(RectTransform));
            DontDestroyOnLoad(gameObject);
            instance = gameObject.AddComponent<RouletteDamageEffects>();

            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5900;

            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            instance.canvasRect = gameObject.GetComponent<RectTransform>();
            sparkleSprite = CreateSparkleSprite();
            glowSprite = CreateGlowSprite();
        }

        private void AddImpact(Vector3 screenPosition, RouletteTier tier)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, screenPosition, null, out Vector2 localPosition))
                return;

            Color color;
            float duration;
            float maxDistance;
            float effectScale;
            int sparkCount;
            GetStyle(tier, out color, out maxDistance, out duration, out effectScale, out sparkCount);

            var root = new GameObject("RouletteSparks", typeof(RectTransform), typeof(CanvasGroup));
            root.transform.SetParent(transform, false);
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = localPosition;
            rect.sizeDelta = new Vector2(128f, 128f);
            rect.localScale = Vector3.one * effectScale;

            var group = root.GetComponent<CanvasGroup>();
            var glow = CreateImage(rect, "SoftGlow", glowSprite, color, new Vector2(120f, 120f));
            var core = CreateImage(rect, "CenterSparkle", sparkleSprite, Color.white, new Vector2(56f, 56f)).rectTransform;
            core.localScale = Vector3.one * 1.2f;

            var sparks = new List<Spark>(sparkCount);
            for (int i = 0; i < sparkCount; i++)
            {
                float angle = Random.Range(0f, Mathf.PI * 2f);
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                float size = Random.Range(13f, tier == RouletteTier.Jackpot ? 32f : 24f);
                var image = CreateImage(
                    rect,
                    "FlyingSpark",
                    sparkleSprite,
                    i % 4 == 0 ? Color.white : i % 3 == 0 ? new Color(1f, 0.96f, 0.62f) : color,
                    new Vector2(size, size));
                sparks.Add(new Spark
                {
                    Rect = image.rectTransform,
                    Image = image,
                    Direction = direction,
                    Color = image.color,
                    Distance = Random.Range(maxDistance * 0.55f, maxDistance),
                    Delay = Random.Range(0f, duration * 0.22f),
                    Rotation = Random.Range(-240f, 240f)
                });
            }

            var captionObject = new GameObject("HitCaption", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(Outline));
            captionObject.transform.SetParent(rect, false);
            var captionRect = captionObject.GetComponent<RectTransform>();
            captionRect.anchorMin = captionRect.anchorMax = new Vector2(0.5f, 0.5f);
            captionRect.pivot = new Vector2(0.5f, 0.5f);
            captionRect.anchoredPosition = new Vector2(0f, 78f);
            captionRect.sizeDelta = new Vector2(460f, 100f);

            var caption = captionObject.GetComponent<Text>();
            caption.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            caption.fontSize = tier == RouletteTier.Jackpot ? 72 : tier == RouletteTier.Big ? 64 : 48;
            caption.fontStyle = FontStyle.Bold;
            caption.alignment = TextAnchor.MiddleCenter;
            caption.horizontalOverflow = HorizontalWrapMode.Overflow;
            caption.verticalOverflow = VerticalWrapMode.Overflow;
            caption.color = Color.white;
            caption.raycastTarget = false;
            caption.text = GetCaption(tier);

            var outline = captionObject.GetComponent<Outline>();
            outline.effectColor = new Color(color.r * 0.3f, color.g * 0.2f, color.b * 0.35f, 1f);
            outline.effectDistance = new Vector2(5f, -5f);

            impacts.Add(new Impact
            {
                Root = rect,
                Group = group,
                Glow = glow,
                Core = core,
                Sparks = sparks,
                CaptionRect = captionRect,
                Caption = caption,
                Duration = duration,
                Time = 0f
            });
        }

        private void Update()
        {
            float deltaTime = Time.unscaledDeltaTime;
            for (int i = impacts.Count - 1; i >= 0; i--)
            {
                var impact = impacts[i];
                impact.Time += deltaTime;
                float progress = Mathf.Clamp01(impact.Time / impact.Duration);
                impact.Group.alpha = 1f - progress;

                impact.Glow.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.65f, 2.2f, progress);
                impact.Glow.color = new Color(1f, 0.9f, 0.58f, (1f - progress) * 0.55f);
                impact.Core.localScale = Vector3.one * Mathf.Lerp(1.6f, 0.05f, Mathf.Clamp01(progress * 2.5f));
                impact.Core.localRotation = Quaternion.Euler(0f, 0f, progress * 80f);
                impact.CaptionRect.anchoredPosition = new Vector2(0f, Mathf.Lerp(72f, 145f, progress));
                impact.CaptionRect.localScale = Vector3.one * Mathf.Lerp(0.65f, 1f, Mathf.Clamp01(progress * 5f));
                impact.Caption.color = new Color(1f, 1f, 1f, 1f - progress);

                foreach (var spark in impact.Sparks)
                {
                    float particleProgress = Mathf.Clamp01((impact.Time - spark.Delay) / (impact.Duration - spark.Delay));
                    float travel = 1f - Mathf.Pow(1f - particleProgress, 2f);
                    float wave = 0.6f + 0.4f * Mathf.Abs(Mathf.Sin(particleProgress * Mathf.PI * 3f));
                    float gravity = -22f * particleProgress * particleProgress;
                    spark.Rect.anchoredPosition = spark.Direction * (spark.Distance * travel) + Vector2.up * gravity;
                    spark.Rect.localScale = Vector3.one * Mathf.Lerp(1f, 0.12f, particleProgress) * wave;
                    spark.Rect.localRotation = Quaternion.Euler(0f, 0f, spark.Rotation * particleProgress);
                    var particleColor = spark.Color;
                    particleColor.a = impact.Time < spark.Delay
                        ? 0f
                        : (1f - particleProgress) * (0.65f + 0.35f * wave);
                    spark.Image.color = particleColor;
                }

                if (progress >= 1f)
                {
                    Destroy(impact.Root.gameObject);
                    impacts.RemoveAt(i);
                }
            }
        }

        private static Image CreateImage(RectTransform parent, string name, Sprite sprite, Color color, Vector2 size)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            gameObject.transform.SetParent(parent, false);
            var rect = gameObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;

            var image = gameObject.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static string GetCaption(RouletteTier tier)
        {
            switch (tier)
            {
                case RouletteTier.Mid: return "GREAT!";
                case RouletteTier.Big: return "BIG HIT!";
                case RouletteTier.Jackpot: return "JACKPOT!";
                default: return "HIT!";
            }
        }

        private static void GetStyle(RouletteTier tier, out Color color, out float maxDistance, out float duration, out float effectScale, out int sparkCount)
        {
            switch (tier)
            {
                case RouletteTier.Mid:
                    color = new Color(1f, 0.63f, 0.12f);
                    maxDistance = 150f;
                    duration = 0.7f;
                    effectScale = 1.7f;
                    sparkCount = 18;
                    break;
                case RouletteTier.Big:
                    color = new Color(0.85f, 0.5f, 1f);
                    maxDistance = 190f;
                    duration = 0.82f;
                    effectScale = 1.9f;
                    sparkCount = 24;
                    break;
                case RouletteTier.Jackpot:
                    color = new Color(1f, 0.25f, 0.42f);
                    maxDistance = 230f;
                    duration = 1f;
                    effectScale = 2.1f;
                    sparkCount = 32;
                    break;
                default:
                    color = new Color(1f, 0.88f, 0.28f);
                    maxDistance = 110f;
                    duration = 0.58f;
                    effectScale = 1.5f;
                    sparkCount = 14;
                    break;
            }
        }

        private static Sprite CreateSparkleSprite()
        {
            const int size = 32;
            var pixels = new Color32[size * size];
            float center = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = Mathf.Abs(x - center) / center;
                    float ny = Mathf.Abs(y - center) / center;
                    float alpha = Mathf.Clamp01((1f - (Mathf.Sqrt(nx) + Mathf.Sqrt(ny))) * 2.2f);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }
            return CreateSprite(size, pixels);
        }

        private static Sprite CreateGlowSprite()
        {
            const int size = 64;
            var pixels = new Color32[size * size];
            float center = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (x - center) / center;
                    float ny = (y - center) / center;
                    float alpha = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Sqrt(nx * nx + ny * ny)), 2f);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }
            return CreateSprite(size, pixels);
        }

        private static Sprite CreateSprite(int size, Color32[] pixels)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
        }
    }
}
