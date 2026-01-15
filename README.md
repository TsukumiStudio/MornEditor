# MornEditor

## 概要

UnityエディタのInspectorを拡張するカスタムAttribute群で、プロパティの表示・編集を柔軟にカスタマイズするライブラリ。

## 依存関係

| 種別 | 名前 |
|------|------|
| 外部パッケージ | Arbor（オプション: USE_ARBOR定義時） |
| Mornライブラリ | なし |

## 使い方

### 基本的なAttribute

```csharp
// 読み取り専用フィールド
[ReadOnly] public int level;

// 条件付き表示
[ShowIf("isActive")] public string activeName;
[HideIf("isDebug")] public float normalValue;

// 条件付き有効化
[EnableIf("canEdit")] public int editableValue;
[DisableIf("isLocked")] public string lockedText;

// Min-Maxスライダー
[MinMaxSlider(0f, 100f)] public float healthPoints;

// ヘルプボックス
[HelpBox("重要な設定項目です", HelpBoxType.Info)] public string important;

// ボタン
[Button("リセット")]
public void ResetValues() { }
```

### 高度な機能

```csharp
// ラベルのカスタマイズ
[Label("Health Points")] public int hp;

// プレビュー表示
[TexturePreview] public Texture2D previewTexture;
[SpritePreview(100)] public Sprite previewSprite;

// 検索可能なアセット選択
[ViewableSearch] public ScriptableObject targetAsset;
```
