# ワークショップ公開時に画像・音が反映されない問題

## アップロード準備タスク

VS Code の **ターミナル > タスクの実行 > Prepare Workshop upload and launch Sephiria** を実行する。
Release ビルド後、`metadata.json` と `thumbnail.png`、および `metadata.json` の全モジュールのパスがMODフォルダにあることを確認し、Sephiriaを起動する。
タスクはプロジェクトの `ModFolderName` を上書きせず、ワークスペースフォルダをMODフォルダとして検証する。ワークスペースを対象の `WorkshopLocal/<ModFolderName>/` から開いて実行する。

タスクはゲーム内アップロードを自動実行しない。起動後、タイトル画面の **Steamワークショップ** パネルからMODを選び、**ワークショップにアップロード** を押す。アップロード後は `uploadticket.json` の `zipPath` が指すZIPを確認し、`images/` と `sounds/` が含まれていることを確認する。`uploadticket.json` は次回以降の更新に必要なので削除しない。

## 症状
ワークショップにアップロードしたMODで、`images/` の画像（アイコン・ルーレット図柄・UI背景）が適用されない。

## 原因
アップロードされたzip（`%LOCALAPPDATA%Low/TEAMHORAY/Sephiria/WorkshopSubmissions/<modId>/<key>.zip`）の中身を確認したところ、
`metadata.json` / `thumbnail.png` / `modules` に書かれたパス（`bin/*.dll`, `Localization/`）だけが含まれ、
`images/` と `sounds/` は含まれていなかった。
コードは `Context.PackageRoot` 配下の `images/`, `sounds/` を読むため、公開版ではファイルが存在せず仮アイコンにフォールバックしていた。

## 対応
`metadata.json` の `modules` に `data` モジュールとして `images` と `sounds` を宣言し、アップロード対象に含める。
今回のアップロードでは `version` を `0.0.3` に更新した。以降も内容を更新するたびに、`metadata.json` の `version` を上げる。

```json
{ "type": "data", "path": "images" },
{ "type": "data", "path": "sounds" }
```

## 確認方法
再アップロード後、`uploadticket.json` の `zipPath` のzipを開き、`images/`・`sounds/` が入っているか確認する。
入っていない場合は、`data` モジュールでは含まれない可能性があるため別の方法を検討する。
