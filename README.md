# MornEditor

<p align="center">
  <img src="src/Editor/MornEditor.png" alt="MornEditor" width="640" />
</p>

<p align="center">
  <img src="https://img.shields.io/github/license/TsukumiStudio/MornEditor" alt="License" />
</p>

## 概要

Unity エディタの Inspector を拡張するカスタム属性・PropertyDrawer・ヘルパー集。`[ReadOnly]` `[ShowIf]` `[Button]` `[MinMaxSlider]` `[TexturePreview]` などの属性を 1 行付けるだけで Inspector の表示・編集を柔軟にカスタマイズできる。

## 導入方法

Unity Package Manager で以下の Git URL を追加:

```
https://github.com/TsukumiStudio/MornEditor.git?path=src#1.0.0
```

`Window > Package Manager > + > Add package from git URL...` に貼り付けてください。

## 機能

### 表示制御

```csharp
[ReadOnly] public int level;                          // 読み取り専用フィールド
[ShowIf(nameof(isActive))] public string activeName;  // 条件付き表示
[HideIf(nameof(isDebug))] public float normalValue;   // 条件付き非表示
[EnableIf(nameof(canEdit))] public int editableValue; // 条件付き有効化
[DisableIf(nameof(isLocked))] public string lockedText; // 条件付き無効化
```

### 入力 UI

```csharp
[MinMaxSlider(0f, 100f)] public Vector2 hpRange;     // Min-Max スライダー
[Label("Health Points")] public int hp;               // ラベルカスタマイズ
[HelpBox("重要な設定項目です", HelpBoxType.Info)] public string important;
```

### プレビュー

```csharp
[TexturePreview] public Texture2D previewTexture;
[SpritePreview(100)] public Sprite previewSprite;
[ViewableSearch] public ScriptableObject targetAsset; // 検索可能なアセット選択
```

### ボタン

```csharp
[Button("リセット")]
public void ResetValues() { /* ... */ }
```

`MonoBehaviour` / `ScriptableObject` の Inspector に「リセット」ボタンが表示される。複数選択時は全ての対象オブジェクトに対して実行される。

### Arbor 連携 (オプション)

`USE_ARBOR` 定義時のみ有効。`MornStateBehaviourEditor` / `ArborEditorWindowExtension` 等の Arbor State 拡張を提供する。

## 依存関係

| 種別 | 名前 |
|------|------|
| 外部パッケージ | [Arbor](https://arbor.caitsithware.com/) (オプション: `USE_ARBOR` 定義時のみ) |
| Morn ライブラリ | なし |

## ライセンス

[The Unlicense](LICENSE)
