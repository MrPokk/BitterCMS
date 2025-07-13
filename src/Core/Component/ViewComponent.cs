using BitterCMS.CMSSystem;
using BitterCMS.Utility.Interfaces;
using System;

namespace BitterCMS.Component
{
    public class ViewComponent : IEntityComponent, IInitializable<ViewComponent.ViewProperty>
    {
        public ViewProperty Properties { get; set; }

        public void Init(ViewProperty property)
        {
            if (property == null)
                return;

            var viewBase = ViewDatabase.Get(property.ViewType);
            Properties = new ViewProperty(viewBase);
        }
        
        public class ViewProperty : InitializableProperty
        {
            public Type ViewType { get; private set; }
            [NonSerialized] private CMSViewCore _current;  
            public CMSViewCore Current { get => _current ??= ViewDatabase.Get(ViewType); set => _current = value; }
            public CMSViewCore Original => ViewDatabase.Get(ViewType);

            public ViewProperty(CMSViewCore variableView)
            {
                ViewType = variableView.ID; 
                _current = variableView;
            }
        }
    }
}
