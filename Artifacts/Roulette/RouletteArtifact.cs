using System.Collections.Generic;
using UnityEngine;

namespace SpinForFortune
{
    // 既存アーティファクトを複製して、伝説アーティファクト「ルーレット」と、その強化アーティファクトをアイテムDBに登録する。
    public static class RouletteArtifact
    {
        private const int TemplateItemId = 1064;
        private static GameObject prefabHolder;
        private static readonly List<KeyValuePair<string, GameObject>> pendingPrefabs = new List<KeyValuePair<string, GameObject>>();
        private static string modId;
        private static GameObject gamblingComboPrefab;

        public static RouletteDamageBuff MidDamageBuff { get; private set; }
        public static RouletteDamageBuff JackpotDamageBuff { get; private set; }
        public static RouletteInvulnerabilityBuff InvulnerabilityBuff { get; private set; }
        internal static string ModId => modId;

        // 登録するアーティファクトの定義。強化アーティファクトを足すときは、ここに1件足す。
        private class Definition
        {
            public int id;
            public string slug; // 名前・説明・プレハブのキーの元になる名前
            public EItemRarity rarity;
            public int cost;
            public System.Type charmType;
            public string iconFile;
            public string worldIconFile;
        }

        private static readonly Definition[] Definitions =
        {
            new Definition { id = RouletteTable.ItemId, slug = "roulette", rarity = EItemRarity.Legend, cost = 1200, charmType = typeof(RouletteCharm), iconFile = "icon.png", worldIconFile = "icon_world.png" },
            new Definition { id = RouletteSpeedUpCharm.ItemId, slug = "roulette-speedup", rarity = EItemRarity.Rare, cost = 500, charmType = typeof(RouletteSpeedUpCharm), iconFile = "speedup_icon.png", worldIconFile = "speedup_icon_world.png" },
        };

        public static void Register(string id)
        {
            modId = id;
            var template = ItemDatabase.FindItemById(TemplateItemId);
            if (template == null || template.resourcePrefab == null || template.type != EItemType.Charm)
            {
                Debug.LogError($"[SpinForFortune] 複製元のアーティファクト {TemplateItemId} が見つかりません。");
                return;
            }

            foreach (var definition in Definitions)
                RegisterItem(template, definition);

            MidDamageBuff = CreateDamageBuff("mid", RouletteTable.MidBuffDamageBonus, RouletteTable.MidBuffSeconds);
            JackpotDamageBuff = CreateDamageBuff("jackpot", RouletteTable.JackpotBuffDamageBonus, RouletteTable.JackpotBuffSeconds);
            InvulnerabilityBuff = CreateInvulnerabilityBuff(RouletteTable.JackpotInvincibleSeconds);
            RouletteBuffHUD.Ensure();
        }

        private static RouletteDamageBuff CreateDamageBuff(string suffix, int damageBonus, float duration)
        {
            string key = $"{modId}:roulette-damage-{suffix}-buff";
            var prefab = new GameObject(key);
            prefab.transform.SetParent(GetPrefabHolder().transform, false);
            prefab.AddComponent<Mirror.NetworkIdentity>();
            var buff = prefab.AddComponent<RouletteDamageBuff>();
            buff.customID = suffix;
            buff.damageBonus = damageBonus;
            buff.defaultDuration = duration;
            buff.HUD_ID = RouletteBuffHUD.DamageId(suffix);
            pendingPrefabs.Add(new KeyValuePair<string, GameObject>(key, prefab));
            return buff;
        }

        private static RouletteInvulnerabilityBuff CreateInvulnerabilityBuff(float duration)
        {
            string key = $"{modId}:roulette-invulnerability-buff";
            var prefab = new GameObject(key);
            prefab.transform.SetParent(GetPrefabHolder().transform, false);
            prefab.AddComponent<Mirror.NetworkIdentity>();
            var buff = prefab.AddComponent<RouletteInvulnerabilityBuff>();
            buff.defaultDuration = duration;
            buff.HUD_ID = RouletteBuffHUD.InvulnerabilityId;
            pendingPrefabs.Add(new KeyValuePair<string, GameObject>(key, prefab));
            return buff;
        }

        internal static GameObject GetPrefabHolder()
        {
            if (prefabHolder == null)
            {
                prefabHolder = new GameObject("SpinForFortune_RoulettePrefabHolder");
                prefabHolder.SetActive(false);
                Object.DontDestroyOnLoad(prefabHolder);
            }
            return prefabHolder;
        }

        private static void RegisterItem(ItemEntity template, Definition definition)
        {
            if (ItemDatabase.FindItemById(definition.id) != null)
            {
                Debug.LogError($"[SpinForFortune] アーティファクトのID {definition.id} は既に使われています。");
                return;
            }

            var item = Object.Instantiate(template);
            Debug.Log($"[SpinForFortune] 複製元アイコン寸法: icon={template.icon?.rect.size} world={template.iconInWorld?.rect.size} ppu={template.icon?.pixelsPerUnit}");
            item.id = definition.id;
            item.name = $"{definition.id}_{definition.slug}";
            item.aName = new LocalizedString($"{modId}:{definition.slug}-name");
            item.aFlavorText = new LocalizedString($"{modId}:{definition.slug}-flavor");
            item.rarity = definition.rarity;
            // 複製元は未解放扱い(Locked)の場合があるので、最初から解放済みにする。
            item.activeType = EItemActiveType.Default;
            item.cannotBeReward = false;
            item.cost = definition.cost;
            item.customCost = true;
            item.categories = new List<string> { EnsureGamblingCategory() };
            item.icon = LoadIcon(definition.iconFile, template.icon) ?? CreateIcon(template.icon);
            item.iconInWorld = LoadIcon(definition.worldIconFile, template.iconInWorld != null ? template.iconInWorld : template.icon)
                ?? item.icon;

            var key = $"{modId}:{definition.slug}-charm";
            var prefab = BuildPrefab(template.resourcePrefab, key, definition);
            if (prefab == null)
                return;
            item.resourcePrefab = prefab;
            pendingPrefabs.Add(new KeyValuePair<string, GameObject>(key, prefab));

            ItemDatabase.Register(item);
            Debug.Log($"[SpinForFortune] アーティファクト「{definition.slug}」を登録しました (template={template.id})");
        }

        internal const string GamblingCategoryId = "Gambling";
        private const string CategoryTemplateId = "Savvy";

        // ゲームに「ギャンブル」カテゴリーは無いので、既存カテゴリー(交渉)を複製して追加する。
        // カテゴリー辞書は非公開で登録APIが無いため、リフレクションで書き込む。
        private static string EnsureGamblingCategory()
        {
            var category = ItemDatabase.FindItemCategory(GamblingCategoryId);
            if (category != null)
            {
                ConfigureGamblingCategory(category);
                return GamblingCategoryId;
            }

            var template = ItemDatabase.FindItemCategory(CategoryTemplateId);
            if (template == null)
            {
                var all = ItemDatabase.GetAllItemCategory();
                template = all.Length > 0 ? all[0] : null;
            }
            var field = typeof(ItemDatabase).GetField("itemCategories", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            var dictionary = field?.GetValue(null) as Dictionary<string, ItemCategoryEntity>;
            if (template == null || dictionary == null)
            {
                Debug.LogError($"[SpinForFortune] ギャンブルカテゴリーを登録できません。template={template != null} field={field != null} dict={dictionary != null}");
                return CategoryTemplateId;
            }

            category = Object.Instantiate(template);
            category.name = $"{modId}:gambling";
            category.id = GamblingCategoryId;
            category.categoryName = new LocalizedString($"{modId}:category-gambling");
            category.categoryFruitName = new LocalizedString($"{modId}:category-gambling-fruit");
            ConfigureGamblingCategory(category);
            dictionary[GamblingCategoryId] = category;
            return GamblingCategoryId;
        }

        private static void ConfigureGamblingCategory(ItemCategoryEntity category)
        {
            category.setStatus = new ItemCategoryEntity.SetTarget[0];
            if (gamblingComboPrefab == null)
            {
                const string key = "gambling-combo-effect";
                gamblingComboPrefab = new GameObject($"{modId}:{key}");
                gamblingComboPrefab.transform.SetParent(GetPrefabHolder().transform, false);
                gamblingComboPrefab.AddComponent<Mirror.NetworkIdentity>();
                var combo = gamblingComboPrefab.AddComponent<RouletteGamblingComboEffect>();
                combo.defaultEffect = new LocalizedString($"{modId}:roulette-gambling-default-effect");
                combo.addStatByCombo = new[]
                {
                    new ComboEffectBase.ComboStat { comboCount = 2, status = new string[0] },
                    new ComboEffectBase.ComboStat { comboCount = 4, status = new string[0] }
                };
                pendingPrefabs.Add(new KeyValuePair<string, GameObject>($"{modId}:{key}", gamblingComboPrefab));
            }
            category.comboEffectPrefab = gamblingComboPrefab;
        }

        // 解放済みアーティファクトの一覧に追加する。サーバー側のPlayerSpawnerで呼ぶ。
        public static void Unlock(PlayerSpawner spawner)
        {
            if (spawner == null)
                return;
            foreach (var definition in Definitions)
            {
                if (ItemDatabase.FindItemById(definition.id) != null && !spawner.unlockedCharms.Contains(definition.id))
                    spawner.unlockedCharms.Add(definition.id);
            }
        }

        private static GameObject BuildPrefab(GameObject source, string key, Definition definition)
        {
            var prefab = Object.Instantiate(source, GetPrefabHolder().transform);
            prefab.name = key;

            var old = prefab.GetComponent<Charm_Basic>();
            if (old == null)
            {
                Debug.LogError("[SpinForFortune] 複製元のプレハブにCharm_Basicがありません。");
                Object.Destroy(prefab);
                return null;
            }
            Object.DestroyImmediate(old);

            var charm = (Charm_Basic)prefab.AddComponent(definition.charmType);
            charm.maxLevel = 1;
            charm.effectsString = new[] { new LocalizedString($"{modId}:{definition.slug}-effect") };
            return prefab;
        }

        // NetworkManagerが用意できてから登録する。準備できたイベントのたびに試す。
        public static void TryRegisterPrefab()
        {
            var networkManager = Mirror.NetworkManager.singleton;
            if (pendingPrefabs.Count == 0 || networkManager == null)
                return;

            var retryPrefabs = new List<KeyValuePair<string, GameObject>>();
            foreach (var pair in pendingPrefabs)
            {
                if (!networkManager.spawnPrefabs.Contains(pair.Value) &&
                    !HorayModAPI.RegisterNetworkPrefab(pair.Value, pair.Key))
                {
                    Debug.LogWarning($"[SpinForFortune] プレハブを登録できませんでした: {pair.Key}");
                    retryPrefabs.Add(pair);
                    continue;
                }

                if (!Mirror.NetworkClient.active)
                {
                    retryPrefabs.Add(pair);
                    continue;
                }

                var identity = pair.Value.GetComponent<Mirror.NetworkIdentity>();
                if (identity == null)
                {
                    Debug.LogError($"[SpinForFortune] ネットワークプレハブにNetworkIdentityがありません: {pair.Key}");
                    retryPrefabs.Add(pair);
                    continue;
                }

                if (Mirror.NetworkClient.GetPrefab(identity.assetId, out var registeredPrefab))
                {
                    if (registeredPrefab != pair.Value)
                    {
                        Debug.LogError($"[SpinForFortune] クライアントに別のプレハブが登録済みです: {pair.Key} (assetId={identity.assetId:X8})");
                        retryPrefabs.Add(pair);
                        continue;
                    }
                }
                else
                {
                    Mirror.NetworkClient.RegisterPrefab(pair.Value);
                    if (!Mirror.NetworkClient.GetPrefab(identity.assetId, out registeredPrefab) ||
                        registeredPrefab != pair.Value)
                    {
                        Debug.LogWarning($"[SpinForFortune] クライアントのネットワークプレハブを登録できませんでした: {pair.Key}");
                        retryPrefabs.Add(pair);
                        continue;
                    }
                }

                Debug.Log($"[SpinForFortune] ネットワークプレハブを登録しました: {pair.Key}");
            }

            pendingPrefabs.Clear();
            pendingPrefabs.AddRange(retryPrefabs);
        }

        // images/roulette/ の画像から作る。無ければ null を返し、仮アイコンにフォールバックする。
        internal static Sprite LoadIcon(string fileName, Sprite reference)
        {
            var path = System.IO.Path.Combine(RouletteUI.PackageRoot ?? "", "images", "roulette", fileName);
            if (!System.IO.File.Exists(path))
                return null;

            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            if (!ImageConversion.LoadImage(texture, System.IO.File.ReadAllBytes(path)))
            {
                Debug.LogError($"[SpinForFortune] アイコン画像を読み込めません: {path}");
                return null;
            }
            texture.name = "roulette_" + System.IO.Path.GetFileNameWithoutExtension(fileName);

            var pivot = new Vector2(0.5f, 0.5f);
            float ppu = 100f;
            if (reference != null)
            {
                ppu = reference.pixelsPerUnit;
                pivot = new Vector2(reference.pivot.x / reference.rect.width, reference.pivot.y / reference.rect.height);
            }
            var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), pivot, ppu);
            sprite.name = texture.name;
            return sprite;
        }

        // 画像素材ができるまでの仮アイコン。元アイコンと同じ大きさのスロットマシン風ドット絵をコードで描く。
        private static Sprite CreateIcon(Sprite reference)
        {
            int w = reference != null ? Mathf.RoundToInt(reference.rect.width) : 32;
            int h = reference != null ? Mathf.RoundToInt(reference.rect.height) : 32;
            var pixels = new Color32[w * h];
            var clear = new Color32(0, 0, 0, 0);
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = clear;

            var outline = new Color32(40, 10, 20, 255);
            var gold = new Color32(255, 200, 40, 255);
            var body = new Color32(190, 30, 60, 255);
            var window = new Color32(250, 245, 230, 255);
            var seven = new Color32(220, 20, 40, 255);

            int mx = Mathf.Max(1, w / 8), my = Mathf.Max(1, h / 6);
            FillRect(pixels, w, h, mx, my, w - mx, h - my, outline);
            FillRect(pixels, w, h, mx + 1, my + 1, w - mx - 1, h - my - 1, gold);
            FillRect(pixels, w, h, mx + 2, my + 2, w - mx - 2, h - my - 2, body);

            int wy0 = h / 2 - h / 8, wy1 = h / 2 + h / 8;
            int cell = (w - 2 * (mx + 3)) / 3;
            for (int c = 0; c < 3; c++)
            {
                int x0 = mx + 3 + c * cell;
                FillRect(pixels, w, h, x0 + 1, wy0, x0 + cell - 1, wy1, window);
                FillRect(pixels, w, h, x0 + cell / 2 - 1, wy0 + 1, x0 + cell / 2 + 1, wy1 - 1, seven);
            }

            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            texture.SetPixels32(pixels);
            texture.Apply();
            texture.name = "roulette_icon";

            var pivot = new Vector2(0.5f, 0.5f);
            float ppu = 100f;
            if (reference != null)
            {
                ppu = reference.pixelsPerUnit;
                pivot = new Vector2(reference.pivot.x / reference.rect.width, reference.pivot.y / reference.rect.height);
            }
            var sprite = Sprite.Create(texture, new Rect(0, 0, w, h), pivot, ppu);
            sprite.name = texture.name;
            return sprite;
        }

        private static void FillRect(Color32[] pixels, int w, int h, int x0, int y0, int x1, int y1, Color32 color)
        {
            for (int y = Mathf.Max(0, y0); y < Mathf.Min(h, y1); y++)
                for (int x = Mathf.Max(0, x0); x < Mathf.Min(w, x1); x++)
                    pixels[y * w + x] = color;
        }
    }
}
