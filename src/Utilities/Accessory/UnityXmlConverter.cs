#if UNITY_EDITOR
using BitterCMS.System.Serialization;
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BitterCMS.UnityIntegration.Utility
{
    public static class UnityXmlConverter
    {
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
        
        public static object DeserializeEntityFromXml(Type typeObject, TextAsset xmlAsset)
        {
            return !xmlAsset ? null : SerializerUtility.TryDeserialize(typeObject, xmlAsset.text);
        }
    }
}
#endif
