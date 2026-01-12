using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace MornLib
{
    [CustomPropertyDrawer(typeof(ViewableSearchAttribute))]
    public sealed class ViewableSearchDrawer : PropertyDrawer
    {
        private const int PreviewSizeW = 50;
        private const int PreviewSizeH = 20;
        private const int ButtonWidth = 60;
        private const int Spacing = 4; 
    
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var fieldType = fieldInfo.FieldType;
            var errorStyle = new GUIStyle { normal = { textColor = Color.red } };

            if (!typeof(ScriptableObject).IsAssignableFrom(fieldType))
            {
                EditorGUI.LabelField(position, label, new GUIContent("Error: ScriptableObject を継承していないクラスです"), errorStyle);
                return;
            }

            var previewProp = fieldType.GetProperty("Preview", BindingFlags.Public | BindingFlags.Instance);
            if (previewProp == null)
            {
                EditorGUI.LabelField(position, label, new GUIContent("Error: 'Preview' プロパティが見つかりません"), errorStyle);
                return;
            }

            if (previewProp.PropertyType != typeof(Sprite))
            {
                EditorGUI.LabelField(position, label, new GUIContent("Error: 'Preview' プロパティは Sprite 型でなければなりません"), errorStyle);
                return;
            }

            var obj = property.objectReferenceValue as ScriptableObject;
            if (obj != null)
            {
                var sprite = previewProp.GetValue(obj) as Sprite;
                if (sprite)
                {
                    var texture = AssetPreview.GetAssetPreview(sprite);
                    var previewRect = new Rect(position.x, position.y, PreviewSizeW, PreviewSizeH);
                    GUI.DrawTexture(previewRect, texture, ScaleMode.ScaleToFit);
                }
            }
        
            // ScriptableObject を通常の ObjectField で選択
            var normalRect = new Rect(position.x + PreviewSizeW + Spacing, position.y, position.width - PreviewSizeW - ButtonWidth - Spacing * 2, position.height);
            var buttonRect = new Rect(position.x + position.width - ButtonWidth, position.y, ButtonWidth, position.height);
            EditorGUI.BeginProperty(normalRect, label, property);
        
            EditorGUI.PropertyField(normalRect, property, GUIContent.none);
            if (GUI.Button(buttonRect, "Select"))
            {
                ViewableSearchWindow.ShowWindow(property, fieldType);
            }

            EditorGUI.EndProperty();
        }
    
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return PreviewSizeH;
        }
    }
}