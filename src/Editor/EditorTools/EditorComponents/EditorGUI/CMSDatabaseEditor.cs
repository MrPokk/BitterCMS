#if UNITY_EDITOR
using BitterCMS.CMSSystem;
using BitterCMS.System.Serialization;
using BitterCMS.Utility;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BitterCMS.UnityIntegration.Editor
{
    public class CMSDatabaseEditor : CMSEditorTabCore
    {
        private bool _showViewsSection = true;
        private bool _showEntitiesSection = true;
        private string _searchString = "";
        private int _sortOption = 0;

        public override void Draw()
        {
            DrawSearchAndSortOptions();

            _showViewsSection = EditorGUILayout.Foldout(_showViewsSection, "Views");
            if (_showViewsSection)
                DrawView();

            EditorGUILayout.Space(10);

            _showEntitiesSection = EditorGUILayout.Foldout(_showEntitiesSection, "Entities");
            if (_showEntitiesSection)
                DrawEntity();
        }

        private void DrawSearchAndSortOptions()
        {
            var searchContent = new GUIContent("", "Search database items");
            var textFieldStyle = new GUIStyle(EditorStyles.textField)
            {
                fontStyle = string.IsNullOrEmpty(_searchString) ? FontStyle.Italic : FontStyle.Normal
            };
            
            EditorGUILayout.BeginHorizontal();
            {

                _searchString = EditorGUILayout.TextField(searchContent, _searchString, 
                    textFieldStyle, GUILayout.ExpandWidth(true));

                if (string.IsNullOrEmpty(_searchString))
                {
                    var textFieldRect = GUILayoutUtility.GetLastRect();
                    EditorGUI.LabelField(textFieldRect, "Search", new GUIStyle(EditorStyles.label)
                    {
                        normal = { textColor = EditorStyles.centeredGreyMiniLabel.normal.textColor },
                        alignment = TextAnchor.MiddleLeft,
                        padding = new RectOffset(5, 0, 0, 0)
                    });
                }

                _sortOption = EditorGUILayout.Popup(_sortOption, new[] { "A-Z", "Z-A" }, GUILayout.Width(80));
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);
        }
        
        private void DrawView()
        {
            var allView = ViewDatabase.GetAll();

            var filteredViews = allView.Where(v => v &&
                                                   v.GetType().Name.ToLower().Contains(_searchString.ToLower())).ToList();

            EditorGUILayout.LabelField($"Loaded Views ({filteredViews.Count})", EditorStyles.boldLabel);
            if (!filteredViews.Any())
            {
                EditorGUILayout.HelpBox("No views found matching search criteria", MessageType.Info);
                return;
            }

            var sortedViews = _sortOption == 0
                ? filteredViews.OrderBy(v => v.GetType().Name)
                : filteredViews.OrderByDescending(v => v.GetType().Name);

            foreach (var view in sortedViews)
            {
                if (!view)
                    continue;

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField(view.GetType().Name, EditorStyles.boldLabel);
                EditorGUILayout.ObjectField(view.gameObject, typeof(GameObject), false);
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(5);
            }
        }

        private void DrawEntity()
        {
            var allEntity = EntityDatabase.GetAll();
            var isRecreate = false;

            var filteredEntities = allEntity.Where(e =>
                e.Key.Name.ToLower().Contains(_searchString.ToLower())).ToList();

            EditorGUILayout.LabelField($"Loaded Entities ({filteredEntities.Count})", EditorStyles.boldLabel);

            if (!filteredEntities.Any())
            {
                EditorGUILayout.HelpBox("No entities found matching search criteria", MessageType.Info);
                return;
            }

            var sortedEntities = _sortOption == 0
                ? filteredEntities.OrderBy(e => e.Key.Name)
                : filteredEntities.OrderByDescending(e => e.Key.Name);

            foreach (var entity in sortedEntities)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField(entity.Key.Name, EditorStyles.boldLabel);

                var relativePath = PathUtility.GetRelativePath(entity.Value);

                if (GUILayout.Button("Create", GUILayout.Height(20)))
                {
                    SerializerUtility.TrySerialize(entity.Key, entity.Value);
                    isRecreate = true;
                }

                EditorGUILayout.SelectableLabel(relativePath, EditorStyles.textField, GUILayout.Height(20));
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(5);
            }

            if (isRecreate)
                AssetDatabase.Refresh();
        }
    }
}
#endif
