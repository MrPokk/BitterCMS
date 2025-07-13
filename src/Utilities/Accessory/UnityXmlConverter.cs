using BitterCMS.System.Serialization;
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BitterCMS.UnityIntegration.Utility
{
    public static class UnityXmlConverter
    {
        public static void SaveXml(TextAsset xmlAsset, string xmlContent)
        {
            if (!xmlAsset) return;

            var path = AssetDatabase.GetAssetPath(xmlAsset);
            File.WriteAllText(path, xmlContent);
            AssetDatabase.Refresh();

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
        }

        public static bool IsXmlFile(TextAsset asset)
        {
            if (!asset) return false;
            var path = AssetDatabase.GetAssetPath(asset);
            return path.EndsWith(".xml", StringComparison.OrdinalIgnoreCase);
        }

        public static bool TryGetSelectedXmlFile(out TextAsset selectedFile)
        {
            var newSelection = Selection.activeObject as TextAsset;
            if (!newSelection || !IsXmlFile(newSelection))
            {
                selectedFile = null;
                return false;
            }

            selectedFile = newSelection;
            return true;
        }

        public static T DeserializeEntityFromXml<T>(TextAsset xmlAsset) where T : class, new()
        {
            if (!xmlAsset) return null;

            var path = AssetDatabase.GetAssetPath(xmlAsset);

            return SerializerUtility.TryDeserialize<T>(path);
        }

        public static object DeserializeEntityFromXml(Type typeObject, TextAsset xmlAsset)
        {
            if (!xmlAsset) return null;

            var path = AssetDatabase.GetAssetPath(xmlAsset);

            return SerializerUtility.TryDeserialize(typeObject, path);
        }
    }
}
