using System;
using UnityEngine;
using UnityEngine.UI;

namespace SpinForFortune
{
    // 画面中央下に表示するルーレットのスロットUI。
    public class RouletteUI : MonoBehaviour
    {
        private static RouletteUI instance;

        private static readonly Color[] SymbolColors =
        {
            new Color(0.9f, 0.1f, 0.2f), // CHERRY
            new Color(0.95f, 0.75f, 0.1f), // BELL
            new Color(0.15f, 0.15f, 0.15f), // BAR
            new Color(0.85f, 0.05f, 0.1f), // 7
            new Color(0.75f, 0.7f, 0.05f), // LEMON
            new Color(0.5f, 0.15f, 0.65f) // PLUM
        };

        private static readonly Color Gold = new Color(1f, 0.8f, 0.2f);

        private GameObject canvasObject;
        private RectTransform panel;
        private CanvasGroup panelGroup;
        private Image frame;
        private Image flash;
        private readonly Image[] cellBackgrounds = new Image[3];
        private readonly Text[] cellTexts = new Text[3];
        private readonly Image[] cellIcons = new Image[3];
        private Sprite[] symbolSprites;

        public static string PackageRoot;
        private const float MasterVolume = 0.4f;
        private Text caption;
        private Font font;

        private RouletteSfx tickClip, stopClip, reachClip, missClip, winClip, jackpotClip;

        private RouletteSpin spin;
        private bool active;
        private float time;
        private readonly bool[] stopped = new bool[3];
        private readonly float[] nextSwap = new float[3];
        private readonly float[] stopTimes = new float[3];
        private readonly float[] popTimers = new float[3];
        private bool reachStarted;
        private bool resultShown;
        private float resultTimer;
        private float flashAlpha;
        private Color flashColor = Color.white;
        private float shakeTime;
        private float shakePower;
        private Vector3 cameraOffset;
        private Transform shakenCamera;

        private const float ResultHoldSeconds = 1.8f;
        private const float FadeSeconds = 0.4f;
        private static readonly Vector2 PanelBasePosition = new Vector2(0f, 60f);

        public static void Play(RouletteSpin spin)
        {
            if (instance == null)
            {
                var go = new GameObject("SpinForFortune_RouletteUI");
                DontDestroyOnLoad(go);
                instance = go.AddComponent<RouletteUI>();
                instance.Build();
            }
            instance.Begin(spin);
        }

        private void Begin(RouletteSpin newSpin)
        {
            spin = newSpin;
            RouletteAudio.StopAll();
            active = true;
            time = 0f;
            reachStarted = false;
            resultShown = false;
            resultTimer = 0f;
            flashAlpha = 0f;
            for (int i = 0; i < 3; i++)
            {
                stopped[i] = false;
                nextSwap[i] = 0f;
                popTimers[i] = 0f;
                cellBackgrounds[i].transform.localScale = Vector3.one;
            }
            stopTimes[0] = spin.Reel1StopTime;
            stopTimes[1] = spin.Reel2StopTime;
            stopTimes[2] = spin.Reel3StopTime;

            caption.text = "";
            frame.color = Gold;
            panelGroup.alpha = 1f;
            panel.anchoredPosition = PanelBasePosition;
            canvasObject.SetActive(true);
        }

        private void Update()
        {
            RestoreCamera();
            if (!active)
                return;

            float dt = Time.deltaTime;
            time += dt;

            for (int i = 0; i < 3; i++)
                UpdateReel(i, dt);

            if (!reachStarted && stopped[1] && !stopped[2] && spin.reach)
                StartReach();

            if (stopped[2] && !resultShown)
                ShowResult();

            UpdateFrame();
            UpdateEffects(dt);

            if (resultShown)
            {
                resultTimer += dt;
                if (resultTimer > ResultHoldSeconds)
                    panelGroup.alpha = Mathf.Clamp01(1f - (resultTimer - ResultHoldSeconds) / FadeSeconds);
                if (resultTimer > ResultHoldSeconds + FadeSeconds)
                {
                    active = false;
                    canvasObject.SetActive(false);
                }
            }
        }

        private void LateUpdate()
        {
            if (!active || shakeTime <= 0f)
                return;

            var cam = Camera.main;
            if (cam == null)
                return;

            var offset = new Vector3(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f), 0f) * shakePower * 0.25f;
            cam.transform.position += offset;
            cameraOffset = offset;
            shakenCamera = cam.transform;
        }

        private void RestoreCamera()
        {
            if (shakenCamera != null)
            {
                shakenCamera.position -= cameraOffset;
                shakenCamera = null;
            }
            cameraOffset = Vector3.zero;
        }

        private void UpdateReel(int index, float dt)
        {
            if (stopped[index])
            {
                if (popTimers[index] > 0f)
                {
                    popTimers[index] -= dt;
                    float pop = 1f + Mathf.Clamp01(popTimers[index] / 0.2f) * 0.35f;
                    cellBackgrounds[index].transform.localScale = new Vector3(pop, pop, 1f);
                }
                return;
            }

            if (time >= stopTimes[index])
            {
                stopped[index] = true;
                SetSymbol(index, spin.reels[index]);
                popTimers[index] = 0.2f;
                if (index == 2)
                    RouletteAudio.StopAll();
                Play(stopClip, 1f);
                return;
            }

            if (time >= nextSwap[index])
            {
                SetSymbol(index, UnityEngine.Random.Range(0, RouletteTable.SymbolCount));
                float interval = 0.06f / spin.speed;
                if (index == 2 && spin.reach)
                {
                    float progress = Mathf.InverseLerp(spin.Reel2StopTime, stopTimes[2], time);
                    interval = Mathf.Lerp(0.07f, 0.5f, progress * progress) / spin.speed;
                }
                nextSwap[index] = time + interval;
                Play(tickClip, 0.35f);
            }
        }

        private void SetSymbol(int index, int symbol)
        {
            var sprite = symbolSprites[symbol];
            cellIcons[index].enabled = sprite != null;
            cellIcons[index].sprite = sprite;
            cellTexts[index].text = sprite != null ? "" : RouletteTable.SymbolNames[symbol];
            cellTexts[index].color = SymbolColors[symbol];
        }

        // 図柄画像は <mod>/images/roulette/symbol_<名前>.png。無ければ文字で表示する。
        private static Sprite[] LoadSymbolSprites()
        {
            var sprites = new Sprite[RouletteTable.SymbolCount];
            for (int i = 0; i < sprites.Length; i++)
            {
                var path = System.IO.Path.Combine(PackageRoot ?? "", "images", "roulette", $"symbol_{RouletteTable.SymbolFileNames[i]}.png");
                if (!System.IO.File.Exists(path))
                    continue;
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
                if (!ImageConversion.LoadImage(texture, System.IO.File.ReadAllBytes(path)))
                {
                    Debug.LogError($"[SpinForFortune] 図柄画像を読み込めません: {path}");
                    continue;
                }
                sprites[i] = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
            }
            return sprites;
        }

        private static readonly string[] SoundExtensions = { ".wav", ".ogg", ".mp3" };

        // 効果音は <mod>/sounds/roulette/<名前>.(wav|ogg|mp3) があればそれを使い、無ければ生成音を使う。
        private static void LoadClip(string name, Action<RouletteSfx> assign)
        {
            foreach (var extension in SoundExtensions)
            {
                var path = System.IO.Path.Combine(PackageRoot ?? "", "sounds", "roulette", name + extension);
                if (!System.IO.File.Exists(path))
                    continue;

                var sfx = RouletteAudio.LoadFile(path);
                if (sfx == null)
                    continue;
                assign(sfx);
                Debug.Log($"[SpinForFortune] 効果音を読み込みました: {path}");
                return;
            }
        }

        private void StartReach()
        {
            reachStarted = true;
            caption.text = "REACH!!";
            caption.color = new Color(1f, 0.3f, 0.2f);
            flashColor = new Color(1f, 0.9f, 0.4f);
            flashAlpha = 0.6f;
            shakeTime = 0.8f;
            shakePower = 14f;
            Play(reachClip, 1f);
        }

        private void ShowResult()
        {
            resultShown = true;
            switch (spin.tier)
            {
                case RouletteTier.Miss:
                    caption.text = "MISS";
                    caption.color = new Color(0.7f, 0.7f, 0.75f);
                    Play(missClip, 0.8f);
                    break;
                case RouletteTier.Small:
                    caption.text = "WIN";
                    caption.color = Color.white;
                    Flash(Color.white, 0.35f, 0.3f, 8f);
                    Play(winClip, 0.8f);
                    break;
                case RouletteTier.Mid:
                    caption.text = "GREAT!";
                    caption.color = new Color(1f, 0.85f, 0.2f);
                    Flash(new Color(1f, 0.9f, 0.4f), 0.5f, 0.5f, 12f);
                    Play(winClip, 1f);
                    break;
                case RouletteTier.Big:
                    caption.text = "BIG WIN!!";
                    caption.color = new Color(0.8f, 0.4f, 1f);
                    Flash(new Color(0.8f, 0.5f, 1f), 0.7f, 1f, 20f);
                    Play(winClip, 1f);
                    break;
                case RouletteTier.Jackpot:
                    caption.text = "JACKPOT!!!";
                    caption.color = new Color(1f, 0.15f, 0.55f);
                    Flash(new Color(1f, 0.95f, 0.6f), 1f, 1.8f, 28f);
                    Play(jackpotClip, 1f);
                    break;
            }
        }

        private void Flash(Color color, float alpha, float shakeSeconds, float power)
        {
            flashColor = color;
            flashAlpha = alpha;
            shakeTime = shakeSeconds;
            shakePower = power;
        }

        private void UpdateFrame()
        {
            if (resultShown)
            {
                switch (spin.tier)
                {
                    case RouletteTier.Jackpot:
                        frame.color = Color.HSVToRGB(Mathf.Repeat(time * 1.5f, 1f), 0.8f, 1f);
                        break;
                    case RouletteTier.Big:
                        frame.color = new Color(0.8f, 0.4f, 1f);
                        break;
                    case RouletteTier.Mid:
                        frame.color = new Color(1f, 0.85f, 0.2f);
                        break;
                    default:
                        frame.color = Gold;
                        break;
                }
            }
            else if (reachStarted)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(time * 16f);
                frame.color = Color.Lerp(Gold, new Color(1f, 0.1f, 0.1f), pulse);
            }
        }

        private void UpdateEffects(float dt)
        {
            if (flashAlpha > 0f)
                flashAlpha = Mathf.Max(0f, flashAlpha - dt * 1.6f);
            flash.color = new Color(flashColor.r, flashColor.g, flashColor.b, flashAlpha);

            if (shakeTime > 0f)
            {
                shakeTime -= dt;
                var jitter = new Vector2(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f)) * shakePower;
                panel.anchoredPosition = PanelBasePosition + jitter;
            }
            else
            {
                panel.anchoredPosition = PanelBasePosition;
            }
        }

        private void Play(RouletteSfx clip, float volume)
        {
            RouletteAudio.Play(clip, volume * MasterVolume);
        }

        private void OnDestroy()
        {
            RestoreCamera();
        }

        private void Build()
        {
            font = LoadFont();
            symbolSprites = LoadSymbolSprites();

            canvasObject = new GameObject("RouletteCanvas");
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5000;
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            flash = MakeImage(canvasObject.transform, "Flash", new Color(1f, 1f, 1f, 0f));
            Stretch(flash.rectTransform);

            var panelObject = new GameObject("Panel", typeof(RectTransform), typeof(CanvasGroup));
            panelObject.transform.SetParent(canvasObject.transform, false);
            panel = panelObject.GetComponent<RectTransform>();
            panelGroup = panelObject.GetComponent<CanvasGroup>();
            panelGroup.blocksRaycasts = false;
            panelGroup.interactable = false;
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0.5f, 0f);
            panel.sizeDelta = new Vector2(300f, 125f);
            panel.anchoredPosition = PanelBasePosition;

            frame = MakeImage(panel, "Frame", Gold);
            Stretch(frame.rectTransform);
            var background = MakeImage(panel, "Background", new Color(0.12f, 0.04f, 0.08f, 0.95f));
            Stretch(background.rectTransform);
            background.rectTransform.offsetMin = new Vector2(4f, 4f);
            background.rectTransform.offsetMax = new Vector2(-4f, -4f);

            for (int i = 0; i < 3; i++)
            {
                var cell = MakeImage(panel, "Reel" + i, new Color(0.98f, 0.96f, 0.9f));
                cell.rectTransform.anchorMin = cell.rectTransform.anchorMax = new Vector2(0.5f, 1f);
                cell.rectTransform.sizeDelta = new Vector2(84f, 66f);
                cell.rectTransform.anchoredPosition = new Vector2((i - 1) * 92f, -42f);
                cellBackgrounds[i] = cell;

                var icon = MakeImage(cell.transform, "Icon", Color.white);
                Stretch(icon.rectTransform);
                icon.rectTransform.offsetMin = new Vector2(4f, 4f);
                icon.rectTransform.offsetMax = new Vector2(-4f, -4f);
                icon.preserveAspect = true;
                icon.enabled = false;
                cellIcons[i] = icon;

                var text = MakeText(cell.transform, "Symbol", 22, TextAnchor.MiddleCenter);
                Stretch(text.rectTransform);
                text.resizeTextForBestFit = true;
                text.resizeTextMinSize = 8;
                text.resizeTextMaxSize = 22;
                text.horizontalOverflow = HorizontalWrapMode.Overflow;
                cellTexts[i] = text;
                SetSymbol(i, 3);
            }

            caption = MakeText(panel, "Caption", 24, TextAnchor.MiddleCenter);
            caption.rectTransform.anchorMin = new Vector2(0f, 0f);
            caption.rectTransform.anchorMax = new Vector2(1f, 0f);
            caption.rectTransform.pivot = new Vector2(0.5f, 0f);
            caption.rectTransform.sizeDelta = new Vector2(0f, 30f);
            caption.rectTransform.anchoredPosition = new Vector2(0f, 8f);
            caption.gameObject.AddComponent<Outline>().effectColor = Color.black;

            BuildClips();

            canvasObject.SetActive(false);
        }

        private static Font LoadFont()
        {
            try
            {
                var builtin = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (builtin != null)
                    return builtin;
            }
            catch (Exception)
            {
            }
            return Font.CreateDynamicFontFromOSFont(new[] { "Arial", "Yu Gothic UI" }, 32);
        }

        private static Image MakeImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private Text MakeText(Transform parent, string name, int size, TextAnchor anchor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
            text.alignment = anchor;
            text.raycastTarget = false;
            return text;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        // 蜉ｹ譫憺浹縺ｯ繝輔ぃ繧､繝ｫ繧剃ｽｿ繧上★縲∵ｳ｢蠖｢繧偵さ繝ｼ繝峨〒逕滓・縺吶ｋ縲・
        private void BuildClips()
        {
            tickClip = MakeClip("tick", 0.04f, (t, d) => (Mathf.Sin(2f * Mathf.PI * 1000f * t) > 0f ? 0.4f : -0.4f) * (1f - t / d));
            stopClip = MakeClip("stop", 0.15f, (t, d) =>
                (Mathf.Sin(2f * Mathf.PI * 180f * t) * 0.7f + UnityEngine.Random.Range(-0.2f, 0.2f)) * (1f - t / d));
            reachClip = MakeClip("reach", 1.5f, (t, d) =>
                Mathf.Sin(2f * Mathf.PI * (300f * t + 400f / d * t * t)) * 0.5f * (0.7f + 0.3f * Mathf.Sin(t * 40f)) * Mathf.Min(1f, (d - t) * 4f));
            missClip = MakeClip("miss", 0.5f, (t, d) =>
                Mathf.Sin(2f * Mathf.PI * (t < d / 2f ? 330f : 220f) * t) * 0.4f * (1f - t / d));
            winClip = MakeArpeggio("win", new[] { 523f, 659f, 784f, 1047f }, 0.12f);
            jackpotClip = MakeArpeggio("jackpot", new[] { 523f, 659f, 784f, 1047f, 1319f, 1568f, 2093f, 1568f, 2093f, 2637f }, 0.13f);

            LoadClip("tick", c => tickClip = c);
            LoadClip("stop", c => stopClip = c);
            LoadClip("reach", c => reachClip = c);
            LoadClip("miss", c => missClip = c);
            LoadClip("win", c => winClip = c);
            LoadClip("jackpot", c => jackpotClip = c);
        }

        private static RouletteSfx MakeArpeggio(string name, float[] notes, float noteSeconds)
        {
            float total = noteSeconds * notes.Length + 0.3f;
            return MakeClip(name, total, (t, d) =>
            {
                int index = Mathf.Min(notes.Length - 1, (int)(t / noteSeconds));
                float local = t - index * noteSeconds;
                float env = Mathf.Max(0f, 1f - local / (noteSeconds * 1.8f));
                float f = notes[index];
                return (Mathf.Sin(2f * Mathf.PI * f * t) * 0.35f + Mathf.Sin(4f * Mathf.PI * f * t) * 0.1f) * env;
            });
        }

        private static RouletteSfx MakeClip(string name, float seconds, Func<float, float, float> sample)
        {
            const int rate = 44100;
            int count = Mathf.CeilToInt(seconds * rate);
            var data = new float[count];
            for (int i = 0; i < count; i++)
                data[i] = Mathf.Clamp(sample(i / (float)rate, seconds), -1f, 1f);
            return RouletteAudio.FromSamples(data, rate, "roulette_" + name);
        }
    }
}
