#if UNITY_EDITOR
using UnityEditor;

namespace BitterCMS.UnityIntegration.Editor
{
    public abstract class CMSEditorTabCore
    {
        public virtual void OnEnable(EditorWindow editorWindow) { }
        public virtual void OnSelectionChange() { }
        public abstract void Draw();
    }
}
#endif
