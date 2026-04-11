using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace MornLib
{
    public sealed class ViewableSearchWindow : EditorWindow
    {
        private SerializedProperty _property;
        private ScriptableObject[] _allTargets;
        private List<ScriptableObject> _filteredObjects;
        private string _searchText;
        private Vector2 _scrollPos;
        private const float MinSize = 30f;
        private const float MaxSize = 200f;
        private const float DefaultSize = 60f;
        private const string SizeKey = "ViewableSearchWindow.SizeKey";
        private float _imageSize;

        public static void ShowWindow(SerializedProperty property, Type targetType)
        {
            var window = GetWindow<ViewableSearchWindow>("便利検索くん");
            window.Initialize(property, targetType);
            window.Show();
        }

        private void Initialize(SerializedProperty property, Type targetType)
        {
            _property = property;
            _allTargets = AssetDatabase.FindAssets($"t:{targetType.Name}").Select(AssetDatabase.GUIDToAssetPath)
                                       .Select(AssetDatabase.LoadAssetAtPath<ScriptableObject>).OrderBy(obj => obj.name)
                                       .ToArray();
            _filteredObjects = new List<ScriptableObject>(_allTargets);

            // EditorPrefsから以前のサイズを読み込む（設定がなければデフォルトサイズ）
            _imageSize = EditorPrefs.GetFloat(SizeKey, DefaultSize);
        }

        private void OnGUI()
        {
            _imageSize = EditorGUILayout.Slider("Preview Size", _imageSize, MinSize, MaxSize);
            EditorPrefs.SetFloat(SizeKey, _imageSize);
            var cachedText = _searchText;
            _searchText = EditorGUILayout.TextField("Search", _searchText);
            if (cachedText != _searchText)
            {
                _filteredObjects.Clear();
                _filteredObjects.AddRange(_allTargets.Where(obj => obj.name.Contains(_searchText)));
            }

            using var scroll = new EditorGUILayout.ScrollViewScope(_scrollPos);
            for (var i = 0; i < _filteredObjects.Count; i++)
            {
                if (i % 2 == 0)
                {
                    if (i != 0)
                    {
                        EditorGUILayout.EndHorizontal();
                    }

                    EditorGUILayout.BeginHorizontal();
                }

                var cachedBackgroundColor = GUI.backgroundColor;
                if (_property.objectReferenceValue == _filteredObjects[i])
                {
                    GUI.backgroundColor = Color.cyan;
                }

                // box
                using (new GUILayout.VerticalScope(GUI.skin.box))
                {
                    using (new GUILayout.HorizontalScope())
                    {
                        var obj = _filteredObjects[i];
                        var previewProp = obj.GetType().GetProperty("Preview", BindingFlags.Public | BindingFlags.Instance);
                        var sprite = previewProp.GetValue(obj) as Sprite;
                        var texture = AssetPreview.GetAssetPreview(sprite);
                        var rect = GUILayoutUtility.GetRect(_imageSize, _imageSize);
                        GUI.DrawTexture(rect, texture, ScaleMode.ScaleToFit);
                        var buttonWidth = (position.width - _imageSize * 2 - 60) / 2;
                        if (GUILayout.Button(obj.name, GUILayout.Width(buttonWidth), GUILayout.Height(_imageSize)))
                        {
                            _property.objectReferenceValue = obj;
                            _property.serializedObject.ApplyModifiedProperties();
                            Close();
                        }
                    }
                }

                GUI.backgroundColor = cachedBackgroundColor;
            }

            if (_filteredObjects.Count > 0)
            {
                EditorGUILayout.EndHorizontal();
            }

            _scrollPos = scroll.scrollPosition;
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                Close();
            }
        }

        private void OnEnable()
        {
            wantsMouseMove = true;
        }

        private void Update()
        {
            if (focusedWindow != this)
            {
                Close();
            }
        }
    }
}