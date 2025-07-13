#if UNITY_EDITOR
using BitterCMS.CMSSystem;
using BitterCMS.UnityIntegration.Editor;
using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace BitterCMS.UnityIntegration.Editor
{
    public class CMSEditor : EditorWindow
    {
        // The order of rendering depends on the index
        private readonly static Dictionary<(int index, string name), CMSEditorTabCore> Tabs = new Dictionary<(int, string), CMSEditorTabCore>()
        {
            { (0, "Database"), new CMSDatabaseEditor() },
            { (1, "Inspector"), new CMSInspectorEditor() },
            { (2, "Settings"), new CMSSettingEditor() }
        };
        private int _currentTabIndex = 0;
        private bool _forceUpdate = true;

        private Vector2 _scrollPosition;

        [MenuItem("CMS/CMS CENTER")]
        public static void ShowWindow()
        {
           GetWindow<CMSEditor>("CMS Center").Focus();
        }

        private void OnProjectChange()
        {
            UpdateDatabaseInEditor(true);
        }
        
        private void OnEnable()
        {
            UpdateDatabaseInEditor(true);
        }

        private void OnSelectionChange()
        {
            foreach (var tabs in Tabs.Values)
            {
                tabs.OnSelectionChange();
            }
        }

        private void InitTab()
        {
            foreach (var tabs in Tabs.Values)
            {
                tabs.OnEnable(this);
            }
        }

        private void UpdateDatabaseInEditor(bool forceUpdate = false)
        {
            CMSDatabaseInitializer.UpdateDatabase(forceUpdate);
            AssetDatabase.Refresh();
            InitTab();
        }

        private void OnGUI()
        {
            if (!hasFocus)
                return;
            
            EditorGUILayout.BeginVertical();
            {
                DrawHeader();
                DrawSlider();
                DrawTabButtons();
                DrawSlider();
                DrawCurrentTab();
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Refresh Database", GUILayout.Width(150), GUILayout.Height(25)))
                    UpdateDatabaseInEditor(_forceUpdate);

                GUILayout.FlexibleSpace();

            }
            EditorGUILayout.EndHorizontal();

            _forceUpdate = EditorGUILayout.ToggleLeft("Force Update", _forceUpdate, GUILayout.Width(100));
        }

        private void DrawTabButtons()
        {
            EditorGUILayout.BeginHorizontal();
            {
                foreach (var tabKey in Tabs.Keys.OrderBy(x => x.index))
                {
                    if (GUILayout.Toggle(_currentTabIndex == tabKey.index, tabKey.name, "Button", GUILayout.MinWidth(80)))
                    {
                        _currentTabIndex = tabKey.index;
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawCurrentTab()
        {
            var currentTabKey = Tabs.Keys.FirstOrDefault(x => x.index == _currentTabIndex);

            if (currentTabKey != default && Tabs.TryGetValue(currentTabKey, out var currentTab))
            {
                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
                {
                    EditorGUILayout.BeginVertical(GUI.skin.box);
                    {
                        currentTab?.Draw();
                    }
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndScrollView();
            }
            else
                EditorGUILayout.HelpBox($"Tab with index {_currentTabIndex} is not available", MessageType.Warning);
        }

        private void DrawSlider(int padding = 1)
        {
            EditorGUILayout.Space(padding);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            EditorGUILayout.Space(padding);
        }
    }
}
#endif
