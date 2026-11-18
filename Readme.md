# 進化するAIサッカー

KaiRAキャラクターを操作して学習済みAIと対戦するUnity製サッカーゲームです。
5段階のレベル選択、対戦、結果表示、レベル別ランキングを収録しています。
スタート画面では、選択した学習済みAIによる8面同時の対戦デモを観戦できます。

## 起動方法

1. リポジトリ全体を取得します。
2. Unity Hubで `Project` フォルダを追加し、**Unity 6000.0.84f1** で開きます。
3. 初回はPackage Managerによる依存パッケージの取得とインポート完了を待ちます。
4. `Assets/KaiRASoccerIntegrated/Scenes/SoccerStartMenu.unity` を開き、Playを押します。

初回のパッケージ取得にはインターネット接続が必要です。Python・学習環境は不要です。

## 操作

| 操作 | キーボード | コントローラー（DualShock表記） |
| --- | --- | --- |
| メニュー選択 | ↑↓ / W・S | 十字キー / 左スティック |
| 決定 | Enter / Space | 〇 |
| 選手移動 | ↑↓←→ | 十字キー / 左スティック |
| ダッシュ | Spaceを長押し | 〇を長押し |
| 一時停止・再開 | P | □ |
| 一時停止中にスタート画面へ戻る | Esc | × |

移動中にボールへ接触すると蹴ります。先に10点を取ったチームが勝利します。
人間が操作するキャラクターは、体が黄色で表示されます。
結果画面で名前を入力してランキングに登録できます。
ランキングとプレイヤー名は各端末のPlayerPrefsに保存されます。

レベルを選ぶと、味方・相手の全AIに次の学習済みモデルを適用して対戦を開始します。

| レベル表示 | モデルの学習step数 |
| --- | ---: |
| レベル1（学習時間：20分） | 499,706 |
| レベル2（学習時間：80分） | 1,999,860 |
| レベル3（学習時間：160分） | 3,999,998 |
| レベル4（学習時間：320分） | 7,999,989 |
| レベル5（学習時間：754分） | 18,858,676 |

表示する学習時間は50万step＝20分として換算し、分単位に四捨五入しています。

キーボード・コントローラーでレベルにカーソルを合わせると、対戦デモも対応モデルに切り替わります。
「デモのモデル」では38個の学習済みチェックポイントを観戦用に選択できます。
対戦では常に選んだレベルに対応するモデルを使用します。
ランキングはレベル1〜5ごとに保存し、旧3難易度の記録とは分けて管理します。

## ビルド

Build Profiles / Build Settingsには次の3シーンをこの順で登録済みです。

1. `Assets/KaiRASoccerIntegrated/Scenes/SoccerStartMenu.unity`
2. `Assets/KaiRASoccerIntegrated/Scenes/SoccerHumanVsAI.unity`
3. `Assets/KaiRASoccerIntegrated/Scenes/SoccerResult.unity`

メニュー **KaiRA → ゲームを開く（Build Settingsを設定）** からも設定できます。
独自のBuild Profileでシーン一覧を上書きする場合も、この3シーンを指定してください。

## 構成

```text
Project/
  Assets/KaiRASoccerIntegrated/
    Characters/   # キャラクター、マテリアル、メッシュ、見た目用スクリプト
    Editor/       # 起動・ビルド設定メニュー
    Scenes/       # スタート・対戦・結果
    Shared/       # ゲームから参照される共通アセット
    Soccer/       # ゲーム処理、フィールド、ボール、学習済みモデル
  Packages/       # パッケージ定義とロックファイル
  ProjectSettings/
com.unity.ml-agents/  # AI実行に必要なローカルUnityパッケージ
```

`Project` と `com.unity.ml-agents` は同じ階層に置いてください。
`KaiRASoccerIntegrated` フォルダ単体では、Unity設定とAI実行パッケージが不足します。

GitHubにはこのREADMEのあるリポジトリルートから登録してください。
ゲームで使う `.meta` と `.onnx` は含め、`Library`・`Logs`・`UserSettings`・
録画・ビルド・学習結果は `.gitignore` で除外します。

ML-Agentsの他のサンプル、Python学習ツール、学習ログ、開発・性能測定用プロジェクト、
旧シーン、アセット生成ツールは削除しています。

## ライセンス

Unity ML-Agents由来のコード・アセットのライセンスは `LICENSE.md` と
`com.unity.ml-agents/LICENSE.md`、第三者ライブラリの表記は `Third Party Notices.md` を参照してください。
