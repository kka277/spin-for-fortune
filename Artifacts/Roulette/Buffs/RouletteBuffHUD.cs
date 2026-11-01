using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SpinForFortune
{
    // バフHUDは Resources/EffectHUD のエンティティから作られるため、ルーレット用のHUDをコードで組み立てて辞書に追加する。
    [HarmonyLib.HarmonyPatch(typeof(EffectHUDDatabase), nameof(EffectHUDDatabase.Initialize))]
    public static class RouletteBuffHUD
    {
        private const string IdPrefix = "PHYSICALDAMAGEPLUS_ROULETTE_";
        public const string InvulnerabilityId = IdPrefix + "INVULNERABLE";
        private const float HudSize = 16f;

        private class Definition
        {
            public string id;
            public string slug; // ローカライズキーの元になる名前
            public Color frameColor;
        }

        private static readonly Definition[] Definitions =
        {
            new Definition { id = DamageId("mid"), slug = "damage-mid", frameColor = new Color(0.95f, 0.5f, 0.15f) },
            new Definition { id = DamageId("jackpot"), slug = "damage-jackpot", frameColor = new Color(1f, 0.82f, 0.2f) },
            new Definition { id = InvulnerabilityId, slug = "invulnerable", frameColor = new Color(0.55f, 0.8f, 1f) },
        };

        private static readonly Dictionary<string, EffectHUDEntity> entities = new Dictionary<string, EffectHUDEntity>();

        public static string DamageId(string suffix)
        {
            return IdPrefix + "DAMAGE_" + suffix.ToUpperInvariant();
        }

        [HarmonyLib.HarmonyPostfix]
        public static void Postfix()
        {
            Ensure();
        }

        // 辞書はゲーム側の初期化で作り直されるため、初期化後とMOD読み込み後の両方から呼ぶ。
        public static void Ensure()
        {
            if (RouletteUI.PackageRoot == null || string.IsNullOrEmpty(RouletteArtifact.ModId))
                return;

            var field = typeof(EffectHUDDatabase).GetField("effectHUDDictionary", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            var dictionary = field?.GetValue(null) as Dictionary<string, EffectHUDEntity>;
            if (dictionary == null)
                return;

            foreach (var definition in Definitions)
            {
                if (!entities.TryGetValue(definition.id, out var entity) || entity == null)
                {
                    entity = ScriptableObject.CreateInstance<EffectHUDEntity>();
                    entity.name = definition.id;
                    entity.id = definition.id;
                    entity.hudPrefab = BuildPrefab(definition);
                    entities[definition.id] = entity;
                }
                dictionary[definition.id] = entity;
            }
        }

        private static GameObject BuildPrefab(Definition definition)
        {
            var root = new GameObject(definition.id, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement), typeof(UI_BuffHUD));
            root.transform.SetParent(RouletteArtifact.GetPrefabHolder().transform, false);
            root.GetComponent<RectTransform>().sizeDelta = new Vector2(HudSize, HudSize);

            var layout = root.GetComponent<LayoutElement>();
            layout.minWidth = layout.preferredWidth = HudSize;
            layout.minHeight = layout.preferredHeight = HudSize;

            var frame = root.GetComponent<Image>();
            frame.color = definition.frameColor;

            var solid = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height), new Vector2(0.5f, 0.5f));

            var icon = CreateChild(root.transform, "Icon", 1f);
            icon.sprite = RouletteArtifact.LoadIcon("icon.png", null);
            icon.color = icon.sprite != null ? Color.white : new Color(0.3f, 0.1f, 0.15f);
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            // 経過に応じて暗くなるタイマー表示。
            var fill = CreateChild(root.transform, "Fill", 1f);
            fill.sprite = solid;
            fill.color = new Color(0f, 0f, 0f, 0.55f);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Radial360;
            fill.fillOrigin = (int)Image.Origin360.Top;
            fill.fillClockwise = false;
            fill.fillAmount = 0f;
            fill.raycastTarget = false;

            var overlay = CreateChild(root.transform, "WhiteOverlay", 0f);
            overlay.color = new Color(1f, 1f, 1f, 0f);
            overlay.raycastTarget = false;

            string modId = RouletteArtifact.ModId;
            var hud = root.GetComponent<UI_BuffHUD>();
            hud.buffFillImage = fill;
            hud.whiteOverlayImage = overlay;
            hud.effectType = UI_EffectHUD_Basic.EEffectType.Boon;
            hud.effectName = new LocalizedString($"{modId}:buff-{definition.slug}-name");
            hud.effectFlavorText = new LocalizedString($"{modId}:buff-{definition.slug}-flavor");
            return root;
        }

        private static Image CreateChild(Transform parent, string childName, float inset)
        {
            var child = new GameObject(childName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            child.transform.SetParent(parent, false);
            var rect = child.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
            return child.GetComponent<Image>();
        }
    }
}
