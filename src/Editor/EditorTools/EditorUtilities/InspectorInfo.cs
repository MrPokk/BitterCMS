#if UNITY_EDITOR
using BitterCMS.CMSSystem;
using BitterCMS.System.Serialization;
using BitterCMS.UnityIntegration.Utility;
using UnityEditor;
using UnityEngine;

namespace BitterCMS.UnityIntegration.Editor
{
    public class InspectorInfo
    {
        public TextAsset SelectedXmlAsset;
        public string XMLText;

        public CMSEntityCore DeserializedEntityCore;

        public void RefreshInfo()
        {
            if (UnityXmlConverter.TryGetSelectedXmlFile(out var selectedFile))
            {
                SelectedXmlAsset = selectedFile;
                XMLText = SelectedXmlAsset.text;
                DeserializedEntityCore = GetDeserializedEntity();
            }
            else
            {
                SelectedXmlAsset = null;
                XMLText = null;
                DeserializedEntityCore = null;
            }
        }

        private CMSEntityCore GetDeserializedEntity()
        {
            return UnityXmlConverter.DeserializeEntityFromXml(
                SerializerUtility.GetTypeFromXmlFile(
                    AssetDatabase.GetAssetPath(SelectedXmlAsset)), SelectedXmlAsset) as CMSEntityCore;
        }
    }
}
#endif
