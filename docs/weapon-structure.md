# 武器の構造と登録（調査メモ）

ゲームバージョン 1.1.0 の `Assembly-CSharp.dll` を調べた結果です。

## 構成

- 武器は「武器種（ベース）→ 進化1 → 進化2」の構成で、それぞれが独立した `WeaponEntity` です。
- 親から子への関係は次のどちらかで表されます。`WeaponDatabase.Initialize` が両方を集計します。
  - 親の `standardEnhancements`（`EnhancementMetadata { enabled, enhanced }` のリスト）
  - 子の `enhanceFromId`（親のID。なければ `-1`）
- 両方を設定すると進化先が重複して登録されるため、どちらか一方にします。
- 武器種（ベース）とは、進化先を持ち、かつ他の武器の進化先になっていない武器です（`GetBaseWeapons`）。

## 登録

- `OnLoadWeaponDatabase` で `WeaponDatabase.Register(WeaponEntity)` を呼びます。
- 進化関係の集計は `OnLoadWeaponDatabase` の後に行われるため、このイベント内で `standardEnhancements` を設定すれば反映されます。
- 既存の武器を `Object.Instantiate` で複製して、`id`、`aName`、`standardEnhancements` を差し替える方法を使っています。
- 複製した武器の `standardEnhancements` を空にしないと、複製元の進化先が自分の進化先として表示されます。
- 複製元の `enhanceFromId` が残っていると、複製元の進化先に混ざるため、`-1` にします。

## 一覧への表示

- 武器選択画面（`UI_PresetPanel`）と図鑑（`UI_JournalContent_Weapon`）は、データベースではなく画面側の `baseWeaponDatas` から武器種を並べます。
- 登録しただけでは一覧に出ないため、Harmony で各画面の `Start` / `Initialize` の前にリストへ追加しています。
- 進化先は `WeaponDatabase.GetWeaponEnhancements(id)` から取得され、`WeaponEntity.IsUnlocked()` で絞り込まれます。
- 一覧に追加する `BaseWeaponData` の `unlockKey` は空にします（空なら常に解放済み）。複製元のキーを引き継ぐと、未解放扱いで非表示になる場合があります。

## 複製元と進化先のたどり方

- 杖（ID 500、`0500_Staff`）は、進化先が `standardEnhancements` ではなく子の `enhanceFromId` で結び付いています。
- そのため進化の連なりを複製するときは、`standardEnhancements` に加えて、全武器から `enhanceFromId == 親のID` の武器も探します。
- `GetBaseWeapons` には杖が含まれないため、複製元は `FindWeaponById(500)` で取得します。

## 武器置き場（TownWeaponStand）

- 街や訓練場の武器の台は `TownWeaponStand`（`weapon` に `WeaponEntity` を持つ）です。触れると `EquipWeapon` で装備し、`StartingWeaponID` を保存します。
- 台は各フロアに置かれたシーンのオブジェクトなので、`OnFloorAllocatedClientside` の後に `FindObjectsByType<TownWeaponStand>` で探し、既存の台を複製して追加します。
- 台は近い位置ごとにまとめ、組ごとに一番右の台の隣へ置きます。間隔は既存の台の最小間隔に合わせます。
- 複製した台は `NetworkIdentity` を外し、`weapon` をテスト用の武器種にします。台の上の絵は、名前が `Sword` のレンダラーを `icon` に差し替えます。
- 台の追加は、ゲームの読み込み完了を待つため、フロア読み込みから最大20秒間、毎フレーム探します。

## 入手

- 初期武器になるのは `isDefaultWeapon` が `true` の武器です。
- 装備は `WeaponControllerSimple.EquipWeapon(bool fromTownObject, int weaponID, bool showFx)` で行われます。
