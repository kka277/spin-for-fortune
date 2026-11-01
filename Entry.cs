using System.Collections.Generic;
using UnityEngine;

namespace SpinForFortune
{
    /// <summary>
    /// MODのエントリーポイント。metadata.jsonのentryClassが、このクラスの完全修飾名("SpinForFortune.Entry")を指定します。
    ///
    /// 武器データベース初期化時に既存のスタッフを複製してテスト用武器として登録し、
    /// プレイヤーアバターがサーバー上でスポーンしたとき、物理ダメージのカスタムステータスを5加算します。
    /// テスト用武器の入手手段は追加しません。
    /// customStatsはネットワーク同期される辞書なので、サーバーで加算するとすべてのクライアントに反映されます。
    /// そのため、ゲーム状態の変更はServersideイベントでのみ行い、Clientsideイベントでは確認ログだけを出力します。
    ///
    /// 確認方法: Player.logにテスト用スタッフの登録ログが記録されます。ステータス画面では物理ダメージ+5を確認できます。
    /// </summary>
    public class Entry : WorkshopModBase
    {
        public const int Bonus = 5;
        private const int TestStaffId = 2000000001;
        private const int MaxTiers = 3;
        private const string TestStaffNameKeyPrefix = "kka.spin-for-fortune:test-staff-";
        private HarmonyLib.Harmony harmony;

        // プロセスごとに一度だけ呼び出されます。イベントの購読はここで一度だけ行ってください。
        protected override void OnModLoaded()
        {
            Debug.Log($"[SpinForFortune] OnModLoaded: {metadata.modName} v{metadata.modVersion} by {metadata.modAuthor} " +
                      $"| package={Context?.PackageRoot}");

            // 武器一覧UIはUIプレハブ内のリストから作られるため、Harmonyで一覧にテスト用スタッフを追加します。
            harmony = new HarmonyLib.Harmony(Context.Definition.ModId);
            harmony.PatchAll(typeof(Entry).Assembly);

            HorayModAPI.OnLoadWeaponDatabase += HandleWeaponDatabaseLoaded;
            HorayModAPI.OnLoadKeywordDatabase += HandleKeywordDatabaseLoaded;
            HorayModAPI.OnLoadItemDatabase += HandleItemDatabaseLoaded;
            HorayModAPI.OnAllDatabasesReady += TryRegisterBodies;
            HorayModAPI.OnTitleMenuReady += HandleTitleMenuReady;
            HorayModAPI.OnFloorAllocatedClientside += HandleFloorAllocatedClientside;
            HorayModAPI.OnPlayerAvatarSpawnedServerside += HandlePlayerSpawnedServer;
            HorayModAPI.OnPlayerAvatarSpawnedClientside += HandlePlayerSpawnedClient;
        }

        private void HandleItemDatabaseLoaded()
        {
            RouletteUI.PackageRoot = Context.PackageRoot;
            RouletteArtifact.Register(Context.Definition.ModId);
        }

        private void HandleKeywordDatabaseLoaded()
        {
            RouletteArtifact.RegisterComboInfoKeywords(Context.Definition.ModId);
        }

        protected override void OnModUnloaded()
        {
            harmony?.UnpatchAll(harmony.Id);
            HorayModAPI.OnLoadWeaponDatabase -= HandleWeaponDatabaseLoaded;
            HorayModAPI.OnLoadKeywordDatabase -= HandleKeywordDatabaseLoaded;
            HorayModAPI.OnLoadItemDatabase -= HandleItemDatabaseLoaded;
            HorayModAPI.OnAllDatabasesReady -= TryRegisterBodies;
            HorayModAPI.OnTitleMenuReady -= HandleTitleMenuReady;
            HorayModAPI.OnFloorAllocatedClientside -= HandleFloorAllocatedClientside;
            HorayModAPI.OnPlayerAvatarSpawnedServerside -= HandlePlayerSpawnedServer;
            HorayModAPI.OnPlayerAvatarSpawnedClientside -= HandlePlayerSpawnedClient;
        }

        private void HandleWeaponDatabaseLoaded()
        {
            if (Context == null || Context.Definition == null)
            {
                Debug.LogError("[SpinForFortune] MODコンテキストを取得できないため、テスト用スタッフを登録できません。");
                return;
            }

            var weapons = WeaponDatabase.GetAll();
            if (weapons == null)
            {
                Debug.LogError("[SpinForFortune] 武器データベースを取得できませんでした。");
                return;
            }

            foreach (var weapon in weapons)
            {
                if (weapon != null && weapon.id == TestStaffId)
                {
                    Debug.LogError($"[SpinForFortune] テスト用スタッフのID {TestStaffId} は既に使われています。");
                    return;
                }
            }

            // 武器は「武器種 → 進化1 → 進化2」の構成なので、複製元の進化の連なりごと複製して登録します。
            // 武器種「杖」(ID 500)を複製元にします。見つからなければ、名前にStaffを含む武器を探します。
            const int StaffWeaponId = 500;
            WeaponEntity staffTemplate = WeaponDatabase.FindWeaponById(StaffWeaponId);
            if (staffTemplate == null || staffTemplate.mainWeaponPrefab == null)
            {
                staffTemplate = null;
                foreach (var weapon in weapons)
                {
                    if (weapon == null || weapon.mainWeaponPrefab == null || weapon.enhanceFromId >= 0)
                        continue;
                    if (weapon.mainWeaponPrefab.name.IndexOf("Staff", System.StringComparison.OrdinalIgnoreCase) < 0)
                        continue;
                    staffTemplate = weapon;
                    break;
                }
            }
            Debug.Log($"[SpinForFortune] 複製元: {(staffTemplate != null ? staffTemplate.id + " " + staffTemplate.name : "なし")} / 全武器: {weapons.Count}");

            if (staffTemplate == null)
            {
                Debug.LogError("[SpinForFortune] 複製元にできるスタッフ武器が見つかりませんでした。");
                return;
            }

            var modId = Context.Definition.ModId;
            var current = staffTemplate;
            WeaponEntity previous = null;
            for (int tier = 0; tier < MaxTiers && current != null; tier++)
            {
                var clone = UnityEngine.Object.Instantiate(current);
                clone.id = TestStaffId + tier;
                clone.name = $"{modId}:test-staff-{tier}";
                clone.aName = new LocalizedString(TestStaffNameKeyPrefix + tier + "-name");
                clone.isDefaultWeapon = false;
                clone.enhanceFromId = -1;
                clone.standardEnhancements = new List<EnhancementMetadata>();
                ApplyIcons(clone, tier);
                PrepareBodyPrefab(clone, tier);
                WeaponDatabase.Register(clone);

                if (previous != null)
                    previous.standardEnhancements.Add(new EnhancementMetadata { enabled = true, enhanced = clone });

                previous = clone;
                current = FirstEnhancement(current);
            }
            Debug.Log($"[SpinForFortune] テスト用スタッフを登録しました (template={staffTemplate.id})");
        }

        private class PendingBody
        {
            public WeaponEntity entity;
            public GameObject prefab;
            public string key;
        }

        private readonly List<PendingBody> pendingBodies = new List<PendingBody>();
        private GameObject prefabHolder;

        private Texture2D LoadTexture(string fileName)
        {
            var path = System.IO.Path.Combine(Context.PackageRoot, "images", fileName);
            if (!System.IO.File.Exists(path))
            {
                Debug.LogWarning($"[SpinForFortune] 画像が見つかりません: {path}");
                return null;
            }

            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            if (!ImageConversion.LoadImage(texture, System.IO.File.ReadAllBytes(path)))
            {
                Debug.LogError($"[SpinForFortune] 画像を読み込めません: {path}");
                return null;
            }
            texture.name = System.IO.Path.GetFileNameWithoutExtension(path);
            return texture;
        }

        // 元のスプライトの1ユニットあたりのピクセル数と、相対的な基準点を引き継いで新しいスプライトを作ります。
        private static Sprite CreateSprite(Texture2D texture, Sprite reference)
        {
            var pivot = new Vector2(0.5f, 0.5f);
            float pixelsPerUnit = 100f;
            if (reference != null)
            {
                pixelsPerUnit = reference.pixelsPerUnit;
                pivot = new Vector2(reference.pivot.x / reference.rect.width, reference.pivot.y / reference.rect.height);
            }

            var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), pivot, pixelsPerUnit);
            sprite.name = texture.name;
            return sprite;
        }

        private void ApplyIcons(WeaponEntity weapon, int tier)
        {
            var small = LoadTexture($"tier{tier}_icon.png");
            if (small != null)
                weapon.icon = CreateSprite(small, weapon.icon);

            var big = LoadTexture("big_icon.png");
            if (big != null)
                weapon.bigIcon = CreateSprite(big, weapon.bigIcon);
        }

        // 手に持つ見た目は武器プレハブ内のスプライトなので、プレハブを複製してスプライトを差し替え、ネットワークプレハブとして登録します。
        private void PrepareBodyPrefab(WeaponEntity weapon, int tier)
        {
            var texture = LoadTexture($"tier{tier}_body.png");
            if (texture == null || weapon.mainWeaponPrefab == null)
                return;

            if (prefabHolder == null)
            {
                prefabHolder = new GameObject("SpinForFortune_PrefabHolder");
                prefabHolder.SetActive(false);
                UnityEngine.Object.DontDestroyOnLoad(prefabHolder);
            }

            var prefab = UnityEngine.Object.Instantiate(weapon.mainWeaponPrefab, prefabHolder.transform);
            prefab.name = $"{Context.Definition.ModId}:test-staff-{tier}-main";

            SpriteRenderer body = null;
            foreach (var renderer in prefab.GetComponentsInChildren<SpriteRenderer>(true))
            {
                var sprite = renderer.sprite;
                Debug.Log($"[SpinForFortune] tier{tier} renderer: {renderer.name} sprite={(sprite != null ? sprite.name + " " + sprite.rect.width + "x" + sprite.rect.height : "null")}");
                if (sprite != null && (body == null || sprite.rect.height > body.sprite.rect.height))
                    body = renderer;
            }

            if (body == null)
            {
                Debug.LogWarning($"[SpinForFortune] tier{tier}: 差し替え対象のSpriteRendererが見つかりません。");
                UnityEngine.Object.Destroy(prefab);
                return;
            }

            body.sprite = CreateSprite(texture, body.sprite);
            pendingBodies.Add(new PendingBody { entity = weapon, prefab = prefab, key = $"{Context.Definition.ModId}:test-staff-{tier}-main" });
        }

        // NetworkManagerが用意できるまで登録できないため、準備ができたイベントで何度か試します。
        private void TryRegisterBodies()
        {
            RouletteArtifact.TryRegisterPrefab();
            if (pendingBodies.Count == 0 || Mirror.NetworkManager.singleton == null)
                return;

            foreach (var pending in pendingBodies)
            {
                if (HorayModAPI.RegisterNetworkPrefab(pending.prefab, pending.key))
                {
                    pending.entity.mainWeaponPrefab = pending.prefab;
                    Debug.Log($"[SpinForFortune] 武器本体の画像を反映しました: {pending.key}");
                }
                else
                {
                    Debug.LogWarning($"[SpinForFortune] 武器本体のプレハブを登録できませんでした: {pending.key}");
                }
            }
            pendingBodies.Clear();
        }

        private void HandleTitleMenuReady(UI_TitleLobby lobby) => TryRegisterBodies();

        // 街の武器置き場（TownWeaponStand）に、テスト用の武器種の台を追加します。
        private void HandleFloorAllocatedClientside(string guid, string floorName, FloorGenerator generator)
        {
            Debug.Log($"[SpinForFortune] フロア読み込み: {floorName}");
            var injector = new GameObject("SpinForFortune_StandInjector").AddComponent<StandInjector>();
            injector.weaponId = TestStaffId;
        }

        private class StandInjector : MonoBehaviour
        {
            public int weaponId;
            private float elapsed;

            private void Update()
            {
                elapsed += Time.unscaledDeltaTime;
                if (elapsed > 20f)
                {
                    Destroy(gameObject);
                    return;
                }

                var stands = FindObjectsByType<TownWeaponStand>(FindObjectsSortMode.None);
                if (stands.Length == 0)
                    return;

                if (TryInject(stands))
                    Destroy(gameObject);
            }

            private bool TryInject(TownWeaponStand[] stands)
            {
                var entity = WeaponDatabase.FindWeaponById(weaponId);
                if (entity == null)
                    return true;

                // 台は場所ごとに固まって並んでいるので、近い台を同じ組として扱い、組ごとに1つ追加します。
                var groups = new List<List<TownWeaponStand>>();
                foreach (var stand in stands)
                {
                    if (stand == null || stand is TownWeaponStand_RandomWeapon)
                        continue;

                    List<TownWeaponStand> group = null;
                    foreach (var candidate in groups)
                    {
                        if (Vector2.Distance(candidate[0].transform.position, stand.transform.position) < 30f)
                        {
                            group = candidate;
                            break;
                        }
                    }
                    if (group == null)
                    {
                        group = new List<TownWeaponStand>();
                        groups.Add(group);
                    }
                    group.Add(stand);
                }

                foreach (var group in groups)
                    InjectIntoGroup(group, entity);
                return groups.Count > 0;
            }

            private static void InjectIntoGroup(List<TownWeaponStand> group, WeaponEntity entity)
            {
                TownWeaponStand source = null;
                TownWeaponStand last = null;
                foreach (var stand in group)
                {
                    if (stand.weapon != null && stand.weapon.id == entity.id)
                        return;
                    if (stand.weapon != null && stand.weapon.id == 500)
                        source = stand;
                    if (last == null
                        || stand.transform.position.x > last.transform.position.x
                        || (Mathf.Approximately(stand.transform.position.x, last.transform.position.x) && stand.transform.position.y > last.transform.position.y))
                        last = stand;
                }

                if (source == null)
                    source = last;

                // 並んでいる台の最小の間隔を、新しい台の間隔にします。
                float spacing = float.MaxValue;
                foreach (var a in group)
                {
                    foreach (var b in group)
                    {
                        var dx = b.transform.position.x - a.transform.position.x;
                        if (dx > 0.1f && dx < spacing)
                            spacing = dx;
                    }
                }
                if (spacing == float.MaxValue)
                    spacing = 2f;

                var offset = new Vector3(spacing, 0f, 0f);
                var clone = Instantiate(source.gameObject, last.transform.position + offset, last.transform.rotation, source.transform.parent);
                clone.name = "SpinForFortune_TestStaffStand";

                // 台はクライアント側の操作で装備コマンドを送るだけなので、ネットワーク識別子は外します。
                var identity = clone.GetComponent<Mirror.NetworkIdentity>();
                if (identity != null)
                    DestroyImmediate(identity);

                var cloneStand = clone.GetComponent<TownWeaponStand>();
                cloneStand.weapon = entity;

                // 台の上に浮かぶ武器の絵（"Sword"）を、テスト用武器のアイコンに差し替えます。
                foreach (var renderer in clone.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    Debug.Log($"[SpinForFortune] 台のレンダラー: {renderer.name} sprite={renderer.sprite?.name}");
                    if (renderer.sprite != null && entity.icon != null && renderer.name == "Sword")
                        renderer.sprite = entity.icon;
                }

                Debug.Log($"[SpinForFortune] 武器置き場にテスト用の武器種を追加しました: pos={clone.transform.position}");
            }
        }

        private static WeaponEntity FirstEnhancement(WeaponEntity weapon)
        {
            if (weapon.standardEnhancements != null)
            {
                foreach (var enhancement in weapon.standardEnhancements)
                {
                    if (enhancement != null && enhancement.enabled && enhancement.enhanced != null)
                        return enhancement.enhanced;
                }
            }

            // 杖などは進化先が enhanceFromId で結び付いているので、全武器から探します。
            foreach (var candidate in WeaponDatabase.GetAll())
            {
                if (candidate != null && candidate.enabled && candidate.enhanceFromId == weapon.id)
                    return candidate;
            }
            return null;
        }

        // 武器種（進化の最初の段階）だけを一覧に追加します。進化1・進化2は武器種の進化先として表示されます。
        private static void AddToWeaponList(List<UI_JournalContent_Weapon.BaseWeaponData> list, string owner)
        {
            if (list == null)
                return;

            foreach (var data in list)
            {
                if (data?.weaponEntity != null && data.weaponEntity.id == TestStaffId)
                    return;
            }

            var testStaff = WeaponDatabase.FindWeaponById(TestStaffId);
            if (testStaff == null)
            {
                Debug.LogWarning($"[SpinForFortune] {owner}: テスト用の武器種が見つかりません。");
                return;
            }

            // 解放キーが空なら常に解放済みとして扱われます。
            list.Add(new UI_JournalContent_Weapon.BaseWeaponData
            {
                weaponEntity = testStaff,
                unlockKey = ""
            });
            Debug.Log($"[SpinForFortune] {owner}: 武器一覧に武器種を追加しました (id={testStaff.id}, 件数={list.Count})");
        }

        [HarmonyLib.HarmonyPatch(typeof(UI_PresetPanel), "Start")]
        private static class PresetPanelStartPatch
        {
            private static void Prefix(UI_PresetPanel __instance) => AddToWeaponList(__instance.baseWeaponDatas, "UI_PresetPanel");
        }

        [HarmonyLib.HarmonyPatch(typeof(UI_JournalContent_Weapon), nameof(UI_JournalContent_Weapon.Initialize))]
        private static class JournalWeaponInitializePatch
        {
            private static void Prefix(UI_JournalContent_Weapon __instance) => AddToWeaponList(__instance.baseWeaponDatas, "UI_JournalContent_Weapon");
        }

        // MODのアイテムはゲーム側の解放処理(Resources読み込み)に含まれないため、サーバー開始時に解放済みにする。
        [HarmonyLib.HarmonyPatch(typeof(PlayerSpawner), nameof(PlayerSpawner.OnStartServer))]
        private static class PlayerSpawnerUnlockPatch
        {
            private static void Postfix(PlayerSpawner __instance) => RouletteArtifact.Unlock(__instance);
        }

        // ホスト（サーバー）でのみ呼び出されます。ランを再開するたびに、新しいアバターに対して再度呼び出されます。
        private static void HandlePlayerSpawnedServer(PlayerAvatar player)
        {
            if (player == null)
                return;
            player.AddCustomStat(ECustomStat.PhysicalDamage, Bonus);
            Debug.Log($"[SpinForFortune] サーバー: {player.name} PhysicalDamage +{Bonus} → {player.GetCustomStat(ECustomStat.PhysicalDamage)}");
        }

        // ホストを含むすべての参加者で呼び出されます。ここでは同期された値を確認するだけです。
        private static void HandlePlayerSpawnedClient(PlayerAvatar player)
        {
            if (player == null)
                return;
            RouletteArtifact.TryRegisterPrefab();
            if (player.isLocalPlayer)
            {
                RouletteProgression.ResetClientSnapshot();
                player.OnCustomMessageReceivedClientside += RouletteCharm.HandleClientMessage;
            }
            Debug.Log($"[SpinForFortune] クライアント: {player.name} (local={player.isLocalPlayer}) PhysicalDamage={player.GetCustomStat(ECustomStat.PhysicalDamage)}");
        }
    }
}
