#if UNITY_EDITOR
using BitterCMS.Utility;
using UnityEditor;
using UnityEngine;

namespace BitterCMS.UnityIntegration.Editor
{
    public class CMSSettingEditor : CMSEditorTabCore
    {
        private bool _reloadDomain;
        private bool _reloadScene;

        public override void OnEnable(EditorWindow editorWindow)
        {
            _reloadDomain = EditorSettings.enterPlayModeOptionsEnabled 
                           && EditorSettings.enterPlayModeOptions.HasFlag(EnterPlayModeOptions.DisableDomainReload);
            _reloadScene = EditorSettings.enterPlayModeOptionsEnabled 
                          && EditorSettings.enterPlayModeOptions.HasFlag(EnterPlayModeOptions.DisableSceneReload);
        }

        public override void Draw()
        {
            EditorGUILayout.LabelField("CMS Settings", EditorStyles.boldLabel);
            ModePlay();
            ButtonCreate();
        }
        
        private void ModePlay()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Enter Play Mode Settings", EditorStyles.boldLabel);
            
            EditorGUI.BeginChangeCheck();
            
            _reloadDomain = EditorGUILayout.Toggle("Disable Domain Reload", _reloadDomain);
            _reloadScene = EditorGUILayout.Toggle("Disable Scene Reload", _reloadScene);

            var options = EnterPlayModeOptions.None;
            if (EditorGUI.EndChangeCheck())
            {
                EditorSettings.enterPlayModeOptionsEnabled = _reloadDomain || _reloadScene;

                if (_reloadDomain) options |= EnterPlayModeOptions.DisableDomainReload;
                if (_reloadScene) options |= EnterPlayModeOptions.DisableSceneReload;
                
                EditorSettings.enterPlayModeOptions = options;
            }
            EditorGUILayout.Space();
        }
        
        private void ButtonCreate()
        {
            if (!GUILayout.Button("Create const path", GUILayout.Height(25)))
                return;
            
            PathUtility.GenerationConstPath();
            AssetDatabase.Refresh();
        }
    }
}
#endif