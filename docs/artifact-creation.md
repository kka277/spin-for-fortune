# 新しいアーティファクトの作り方

このMODの実装例をもとに、DLL MODでアーティファクトを追加する手順をまとめます。現状の実装例は [ルーレット](../Artifacts/Roulette/RouletteArtifact.cs)、効果クラスは [RouletteCharm.cs](../Artifacts/Roulette/RouletteCharm.cs)、登録と解放の接続は [Entry.cs](../Entry.cs) を参照してください。

## 仕組み

アーティファクトは `ItemEntity` と、その `resourcePrefab` に付けた `Charm_Basic` 派生コンポーネントの組み合わせです。データベース初期化時にアイテムを登録し、ネットワーク経由で生成するプレハブも登録します。効果のゲーム状態変更はサーバー側で行い、演出UIなどローカル表示はクライアント側に分けます。

## 追加手順

1. **一意なIDと名前キーを決める**
   - 他MODと衝突しないIDを選び、コード内で定数にします。例: `RouletteSpeedUpCharm.ItemId = 2000000101`。
   - `ItemEntity.name` は `<ID>_<slug>` 形式にします(例: `2000000101_roulette-speedup`)。ローカライズキーにはMOD IDを含め、`<modId>:<slug>-name` のようにします。`modId` は `metadata.json` の値を使います。
2. **効果クラスを作る**
   - `Charm_Basic` を継承し、効果やイベント購読を実装します。
   - 効果を有効化するときは `OnEnabledEffect`、解除時は `OnDisabledEffect` をオーバーライドし、購読と解除を対にします。
   - ネットワークプレハブ上で動くCharmは `Weaved()` が `true` を返す実装例に合わせます。
3. **データベース登録を追加する**
   - `HorayModAPI.OnLoadItemDatabase` のハンドラー内で、既存のCharmを `Object.Instantiate` して `ItemEntity` を作ります。
   - 複製元の `id`、名前、ローカライズキー、レア度、価格、カテゴリー、アイコンを設定します。複製元の `Charm_Basic` を取り除き、新しい効果コンポーネントを追加したプレハブを `resourcePrefab` に設定してから `ItemDatabase.Register(item)` を呼びます。
   - 現在の実装は複製元としてID `1064` を使用しています。ゲーム更新後は複製元が有効なCharmか確認してください。
4. **ネットワークプレハブを登録する**
   - 生成したプレハブを `HorayModAPI.RegisterNetworkPrefab(prefab, stableKey)` で登録します。`stableKey` はMOD IDを含めて固定します。
   - 登録時点で `NetworkManager.singleton` がない場合があります。現行実装のように、`OnAllDatabasesReady` や `OnTitleMenuReady` から再試行してください。
5. **アイテムを解放する**
   - `activeType = EItemActiveType.Default` だけでは、ゲーム内の取得候補に出ないことがあります。現行実装では `PlayerSpawner.OnStartServer` のHarmony Postfixから `unlockedCharms` にIDを追加しています。
   - 解放方法はゲーム内部の実装に依存します。ゲーム更新後に対象メソッドとHarmonyパッチを確認してください。
6. **ローカライズ・ビルド設定を更新する**
   - `Localization/ja-JP.json` に `-name`、`-flavor`、`-effect` のキーを追加します。
   - `EnableDefaultCompileItems` が無効なので、新しい `.cs` ファイルを [SpinForFortune.csproj](../SpinForFortune.csproj) の `<Compile Include="..."/>` に追加します。
   - アイコンなどのファイルを使う場合は、実行時に参照するパッケージ内のパスにも含めます。現行実装では `images/roulette/` から読み込みます。
   - `dotnet build -c Release` でビルドし、ゲーム内で取得・装備・効果・マルチプレイ動作を確認します。

## 効果クラスの最小例

以下は装備中に攻撃イベントを購読する形です。`HandleAttackUnit` の内容は実際の効果に置き換えてください。

```csharp
namespace SpinForFortune
{
    public class ExampleCharm : Charm_Basic
    {
        protected override void OnEnabledEffect()
        {
            base.OnEnabledEffect();
            NetworkAvatar.OnAttackUnit += HandleAttackUnit;
        }

        protected override void OnDisabledEffect()
        {
            base.OnDisabledEffect();
            NetworkAvatar.OnAttackUnit -= HandleAttackUnit;
        }

        private void HandleAttackUnit(UnitAvatar target, DamageInstance damage)
        {
            if (!IsEffectEnabled || NetworkAvatar == null || NetworkAvatar.IsDead)
                return;
            if (damage.fromType != EDamageFromType.DirectAttack)
                return;

            // 効果の処理をここに実装する。ゲーム状態の変更はサーバー側で行う。
        }

        public override bool Weaved() => true;
    }
}
```

## このプロジェクトで使えるAPI

### MODローダーが公開するAPI

| API | 用途 |
|---|---|
| `HorayModAPI.OnLoadItemDatabase` | アイテムDB初期化後にアイテムを登録・変更する。購読は `OnModLoaded` で行う。 |
| `HorayModAPI.OnAllDatabasesReady` | DB初期化完了後にプレハブ登録などを再試行する。 |
| `HorayModAPI.OnTitleMenuReady` | タイトルメニュー準備後の再試行ポイント。 |
| `HorayModAPI.OnPlayerAvatarSpawnedServerside` | サーバー側でプレイヤーのスポーンに対応する。 |
| `HorayModAPI.RegisterNetworkPrefab(GameObject, string stableKey)` | ネットワークで生成するプレハブを登録する。安定した `stableKey` を渡す。 |

イベントの説明・シグネチャは[APIリファレンス](../ja/api/README.md)と[HorayModAPI](../ja/api/HorayModAPI.md)、イベント購読やマルチプレイ上の注意は[DLL MODガイド](../ja/dll-mods.md)を参照してください。

### ゲーム本体（`Assembly-CSharp.dll`）のAPI

以下はこのプロジェクトの実装で利用しているゲーム側の型・メンバーです。MODローダーの公開API一覧とは別で、ゲーム更新により変更される可能性があります。

| API | 用途・注意 |
|---|---|
| `ItemDatabase.FindItemById(int)` | 既存アイテムをIDで取得する。複製元の確認やID重複チェックに使う。 |
| `ItemDatabase.Register(ItemEntity)` | 作成したアイテムをDBへ登録する。`OnLoadItemDatabase` 中に呼び出す。 |
| `ItemEntity` | `id`, `name`, `aName`, `aFlavorText`, `rarity`, `cost`, `customCost`, `activeType`, `categories`, `icon`, `iconInWorld`, `resourcePrefab` などを設定する。 |
| `Charm_Basic` | アーティファクトの効果コンポーネントの基底クラス。`OnEnabledEffect` / `OnDisabledEffect` / `OnUpdate`、`NetworkAvatar`、`IsEffectEnabled`、`maxLevel`、`effectsString` を利用する。 |
| `NetworkAvatar.OnAttackUnit` | 攻撃イベント。ハンドラー例は `Action<UnitAvatar, DamageInstance>`。有効化時に購読し、無効化時に必ず解除する。 |
| `UnitAvatar` | 効果対象のAPI。ルーレット実装では `HasMoney` / `SubMoney` / `AddMoney`、`hp` / `Networkhp`、`HealPercent`、`ApplyBuff`、`ApplyDamage` などを使用している。 |
| `DamageInstance.GetDamage(...)` | ダメージインスタンスを作成する。作成後に属性を設定し、`UnitAvatar.ApplyDamage` で適用する。 |
| `ItemDatabase` と `PlayerSpawner.unlockedCharms` | 現行実装ではアイテムのDB登録と解放は別処理。解放には `PlayerSpawner.OnStartServer` へのHarmony Postfixを使用する。 |
| Unity `Object.Instantiate`, `Object.DestroyImmediate`, `GameObject.AddComponent` | 既存アイテム／プレハブの複製、元の効果コンポーネントの置換、新しいCharmの追加に使う。 |
| `LocalizedString(string key)` | アイテム名・説明・効果文をローカライズキーで参照する。 |

実際の利用例は [RouletteArtifact.cs](../Artifacts/Roulette/RouletteArtifact.cs) と [RouletteCharm.cs](../Artifacts/Roulette/RouletteCharm.cs) を参照してください。ゲーム側APIの詳細を確認するときは、現在インストールされているゲームの `Sephiria_Data/Managed/Assembly-CSharp.dll` と対応バージョンを基準にしてください。

## 確認項目

- [ ] IDが重複していない。`name`、`stableKey`、ローカライズキーにはMOD IDが含まれている。
- [ ] `OnLoadItemDatabase` で登録し、アイテムとプレハブの両方を設定している。
- [ ] ネットワークプレハブが登録され、ホスト・参加者の両方で同じMODが利用可能になっている。
- [ ] `OnEnabledEffect` で購読したイベントを `OnDisabledEffect` で解除している。
- [ ] ゲーム状態の変更はサーバー側で行い、ローカルUIなどの表示はクライアント側で制御している。
- [ ] 名前・フレーバー・効果文、画像などのアセットが配布パッケージに含まれている。
- [ ] Releaseビルドとゲーム内の取得・装備・効果を確認している。
