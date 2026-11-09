using System.Collections.Generic;
using UnityEngine;

namespace SpinForFortune
{
    // バフHUDは Resources/EffectHUD のエンティティから作られるため、バニラのプレハブを複製して辞書に追加する。
    [HarmonyLib.HarmonyPatch(typeof(EffectHUDDatabase), nameof(EffectHUDDatabase.Initialize))]
    public static class RouletteBuffHUD
    {
        private const string IdPrefix = "PHYSICALDAMAGEPLUS_ROULETTE_";
        public const string InvulnerabilityId = IdPrefix + "INVULNERABLE";

        private class Definition
        {
            public string id;
            public string slug; // ローカライズキーの元になる名前
            public string iconFile;
            public bool showStackOneText;
        }

        private static readonly Definition[] Definitions =
        {
            new Definition { id = DamageId("mid"), slug = "damage-mid", iconFile = "symbol_bell.png" },
            new Definition { id = DamageId("big"), slug = "damage-big", iconFile = "symbol_bar.png" },
            new Definition { id = DamageId("jackpot"), slug = "damage-jackpot", iconFile = "symbol_seven.png" },
            new Definition { id = InvulnerabilityId, slug = "invulnerable", iconFile = "symbol_seven.png" },
            new Definition { id = ProgressId("heat"), slug = "progress-heat", iconFile = "progress_heat_icon.png", showStackOneText = true },
            new Definition { id = ProgressId("streak"), slug = "progress-streak", iconFile = "progress_streak_icon.png", showStackOneText = true },
            new Definition { id = ProgressId("fever"), slug = "progress-fever", iconFile = "progress_fever_icon.png" },
        };

        private static readonly Dictionary<string, EffectHUDEntity> entities = new Dictionary<string, EffectHUDEntity>();

        public static string DamageId(string suffix)
        {
            return IdPrefix + "DAMAGE_" + suffix.ToUpperInvariant();
        }

        public static string ProgressId(string suffix)
        {
            return IdPrefix + "PROGRESS_" + suffix.ToUpperInvariant();
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
                    var prefab = BuildPrefab(definition);
                    if (prefab == null)
                        continue;

                    entity = ScriptableObject.CreateInstance<EffectHUDEntity>();
                    entity.name = definition.id;
                    entity.id = definition.id;
                    entity.hudPrefab = prefab;
                    entities[definition.id] = entity;
                }
                dictionary[definition.id] = entity;
            }
        }

        private static GameObject BuildPrefab(Definition definition)
        {
            var source = FindVanillaBuffPrefab();
            if (source == null)
            {
                Debug.LogError($"[SpinForFortune] バニラのバフHUDプレハブが見つかりません。作成できません: {definition.id}");
                return null;
            }

            var root = Object.Instantiate(source, RouletteArtifact.GetPrefabHolder().transform);
            root.name = definition.id;
            var hud = root.GetComponent<UI_BuffHUD>();
            var icon = root.GetComponent<UnityEngine.UI.Image>();
            if (hud == null || icon == null)
            {
                Debug.LogError($"[SpinForFortune] バニラのバフHUDプレハブの構成が不正です: {source.name}");
                Object.Destroy(root);
                return null;
            }

            var sprite = RouletteArtifact.LoadIcon(definition.iconFile, null);
            if (sprite != null)
            {
                icon.sprite = sprite;
                icon.color = Color.white;
            }
            else
                Debug.LogWarning($"[SpinForFortune] バフアイコン画像を読み込めません。バニラの画像を使います: {definition.iconFile}");

            string modId = RouletteArtifact.ModId;
            hud.showStackOneText = definition.showStackOneText;
            hud.effectType = UI_EffectHUD_Basic.EEffectType.Boon;
            hud.effectName = new LocalizedString($"{modId}:buff-{definition.slug}-name");
            hud.effectFlavorText = new LocalizedString($"{modId}:buff-{definition.slug}-flavor");
            return root;
        }

        private static GameObject FindVanillaBuffPrefab()
        {
            var field = typeof(EffectHUDDatabase).GetField("effectHUDDictionary", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            var dictionary = field?.GetValue(null) as Dictionary<string, EffectHUDEntity>;
            if (dictionary == null)
                return null;

            foreach (var pair in dictionary)
            {
                if (pair.Key.StartsWith(IdPrefix) || pair.Value == null || pair.Value.hudPrefab == null)
                    continue;

                var prefab = pair.Value.hudPrefab;
                var hud = prefab.GetComponent<UI_BuffHUD>();
                if (hud != null && hud.buffFillImage != null && hud.whiteOverlayImage != null && hud.stackText != null &&
                    prefab.GetComponent<UnityEngine.UI.Image>() != null)
                    return prefab;
            }
            return null;
        }
    }
}
