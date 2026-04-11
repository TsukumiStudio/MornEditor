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

### 依存パッケージ

- [Arbor](https://arbor.caitsithware.com/) (オプション: `USE_ARBOR` 定義時のみ有効)

## 機能

| カテゴリ | 主な属性 / 機能 | 用途 |
|----------|----------------|------|
| 表示制御 | `[ReadOnly]` `[ShowIf]` `[HideIf]` `[EnableIf]` `[DisableIf]` | フィールドの表示・有効化を条件で制御 |
| 入力 UI | `[MinMaxSlider]` `[Label]` `[HelpBox]` | Inspector の入力 UI をカスタマイズ |
| プレビュー | `[TexturePreview]` `[SpritePreview]` `[ViewableSearch]` | アセット参照を視覚的に表示・検索 |
| ボタン | `[Button]` | メソッドを Inspector からワンクリック実行 |
| 共通エディタ | `MonoBehaviourEditor` / `ScriptableObjectEditor` | 全対象に対する `[CanEditMultipleObjects]` / `[Button]` 一括実行 |
| Arbor 連携 | `MornStateBehaviourEditor` 等 | Arbor State の Inspector 拡張 (`USE_ARBOR` 時のみ) |

## 使い方

### 表示制御

```csharp
[ReadOnly] public int level;                            // 読み取り専用
[ShowIf(nameof(isActive))] public string activeName;    // 条件付き表示
[HideIf(nameof(isDebug))] public float normalValue;     // 条件付き非表示
[EnableIf(nameof(canEdit))] public int editableValue;   // 条件付き有効化
[DisableIf(nameof(isLocked))] public string lockedText; // 条件付き無効化
```

### 入力 UI

```csharp
[MinMaxSlider(0f, 100f)] public Vector2 hpRange;
[Label("Health Points")] public int hp;
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

## ライセンス

[The Unlicense](LICENSE)
