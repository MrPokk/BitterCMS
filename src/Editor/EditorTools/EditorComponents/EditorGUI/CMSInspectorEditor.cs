#if UNITY_EDITOR
using BitterCMS.CMSSystem;
using BitterCMS.System.Serialization;
using BitterCMS.UnityIntegration.Utility;
using BitterCMS.Utility.Interfaces;
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace BitterCMS.UnityIntegration.Editor
{
    public class CMSInspectorEditor : CMSEditorTabCore
    {
        private EditorWindow _parentWindow;
        private readonly static InspectorInfo Info = new InspectorInfo();
        private bool _showXmlView;

        private static GUIStyle TextStyleComponent => new GUIStyle(GUI.skin.label)
        {
            fontStyle = FontStyle.Bold,
            fontSize = 12,
        };
        
        private static GUIStyle TextStyleHeader => new GUIStyle(GUI.skin.label)
        {
            fontStyle = FontStyle.Bold,
            fontSize = 16,
        };

        private static GUIStyle StyleWindow => new GUIStyle(EditorStyles.textArea)
        {
            wordWrap = true,
            normal = new GUIStyleState { textColor = Color.gray },
        };


        public override void OnEnable(EditorWindow editorWindow)
        {
            _parentWindow = editorWindow;
            Info.RefreshInfo();
        }

        public override void OnSelectionChange()
        {
            Info.RefreshInfo();
            _parentWindow.Repaint();
        }

        public override void Draw()
        {
            PanelFileXmlEditor();

            if (Info.SelectedXmlAsset)
            {
                PanelViewToggle();

                if (_showXmlView)
                    PanelXmlEditor();
                else
                    PanelObjectEditor();

                PanelButtonXmlEditor();
            }
            else
                EditorGUILayout.HelpBox("Select an XML file to edit", MessageType.Info);
            
        }

        private void PanelViewToggle()
        {
            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();
                _showXmlView = GUILayout.Toggle(_showXmlView, "XML View", "Button", GUILayout.Width(100));
                _showXmlView = !GUILayout.Toggle(!_showXmlView, "Object View", "Button", GUILayout.Width(100));
                GUILayout.FlexibleSpace();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void PanelObjectEditor()
        {
            if (Info.DeserializedEntityCore == null)
            {
                ShowErrorMessage();
                return;
            }
            
            EditorGUILayout.LabelField("Entity Properties", TextStyleHeader);
            EditorGUILayout.BeginVertical(GUI.skin.box);
            {
                DrawFields.DrawFieldsForObject(Info.DeserializedEntityCore);
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.LabelField("Components", TextStyleHeader);
            DrawComponent();
        }

        private void PanelXmlEditor()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("XML Content (Read Only)", EditorStyles.boldLabel);
            EditorGUILayout.SelectableLabel(Info.XMLText, StyleWindow, GUILayout.ExpandHeight(true));
        }

        private void ShowErrorMessage()
        {
            if (Info.DeserializedEntityCore == null)
            {
                EditorGUILayout.HelpBox("Failed to deserialize the object. Please ensure:", MessageType.Error);
                EditorGUILayout.HelpBox(
                    "1. This is a concrete type inheriting from CMSEntity\n" +
                    "2. The class has a default constructor\n" +
                    "3. All used components are serializable\n" +
                    "4. The XML file contains valid data",
                    MessageType.Info);
            }
            else
                EditorGUILayout.HelpBox("The object contains no serializable properties", MessageType.Warning);
        }

        private void PanelFileXmlEditor()
        {
            EditorGUILayout.BeginHorizontal();
            {
                var fileInField = EditorGUILayout.ObjectField("XML File", Info.SelectedXmlAsset, typeof(TextAsset), false) as TextAsset;

                if (fileInField && fileInField != Info.SelectedXmlAsset && UnityXmlConverter.IsXmlFile(fileInField))
                {
                    Info.SelectedXmlAsset = fileInField;
                    Info.XMLText = Info.SelectedXmlAsset.text;
                    _parentWindow.Repaint();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void PanelButtonXmlEditor()
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            {
                SaveButton();
                RefreshButton();
            }
            EditorGUILayout.EndHorizontal();
        }
        private void SaveButton()
        {
            if (!GUILayout.Button("Save") || Info.DeserializedEntityCore == null)
                return;

            CreateNewXml();
        }
        private void RefreshButton()
        {
            if (!GUILayout.Button("Refresh") || Info.DeserializedEntityCore == null)
                return;

            ComparisonXmlToReplacement();
            CreateNewXml();
        }


        private void DrawComponent()
        {
            var allSerializeComponents = Info.DeserializedEntityCore.GetSerializableComponents();
            foreach (var component in allSerializeComponents)
            {
                EditorGUILayout.BeginVertical(GUI.skin.box);
                {
                    EditorGUILayout.LabelField(component.GetType().Name, TextStyleComponent);
                    DrawFields.DrawFieldsForObject(component);

                    DrawPropertyComponent(component);
                }
                EditorGUILayout.EndVertical();
            }
        }

        private void DrawPropertyComponent(IEntityComponent component)
        {
            var componentType = component.GetType();
            var properties = componentType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var property in properties)
            {
                if (!typeof(InitializableProperty).IsAssignableFrom(property.PropertyType))
                    continue;

                var propertyValue = property.GetValue(component);
                if (propertyValue == null) continue;

                EditorGUILayout.BeginVertical(GUI.skin.box);
                EditorGUILayout.LabelField(property.Name, EditorStyles.boldLabel);
                DrawFields.DrawFieldsForObject(propertyValue);
                EditorGUILayout.EndVertical();
            }
        }

        private void ComparisonXmlToReplacement()
        {
            if (Activator.CreateInstance(Info.DeserializedEntityCore.ID) is not CMSEntityCore newEntity)
                return;

            var allComponent = Info.DeserializedEntityCore.GetSerializableComponents();
            foreach (var component in allComponent)
            {
                var idComponent = component.ID;
                if (!newEntity.HasComponent(idComponent))
                    Info.DeserializedEntityCore.RemoveComponent(idComponent);
            }
        }

        private void CreateNewXml()
        {
            Info.XMLText = SerializerUtility.TrySerialize(
                Info.DeserializedEntityCore,
                AssetDatabase.GetAssetPath(Info.SelectedXmlAsset));
            AssetDatabase.Refresh();

            Info.RefreshInfo();
            _parentWindow.Repaint();
        }
    }
}
#endif
