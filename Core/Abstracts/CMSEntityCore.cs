using BitterCMS.Component;
using BitterCMS.System.Serialization;
using BitterCMS.Utility.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace BitterCMS.CMSSystem
{
    /// <summary>
    /// Base class for all CMS entities providing component-based architecture
    /// </summary>
    public abstract class CMSEntityCore : IInitializable<CMSPresenterCore.CMSPresenterProperty>, IXmlIncludeExtraType, IXmlSerializable
    {
        private readonly ConcurrentDictionary<Type, IEntityComponent> _components = new ConcurrentDictionary<Type, IEntityComponent>();

        [XmlIgnore]
        public Type ID => GetType();
        
        [XmlIgnore]
        [field: NonSerialized]
        public CMSPresenterCore.CMSPresenterProperty Properties { get; set; }

        /// <summary>
        /// Gets the number of components attached to this entity
        /// </summary>
        public int ComponentCount => _components.Count;

        /// <summary>
        /// Initializes the entity with the specified properties
        /// </summary>
        public void Init(CMSPresenterCore.CMSPresenterProperty args) => Properties ??= args;

        #region [View Operations]

        /// <summary>
        /// Attempts to get the associated view
        /// </summary>
        public bool TryGetView(out CMSViewCore view)
        {
            if (TryGetComponent<ViewComponent>(out var viewComponent) && 
                viewComponent?.Properties != null)
            {
                view = viewComponent.Properties.Current;
                return view;
            }

            view = null;
            return false;
        }
        
        /// <summary>
        /// Gets the associated view or null if not found
        /// </summary>
        public CMSViewCore GetView() => GetComponent<ViewComponent>()?.Properties?.Current;

        /// <summary>
        /// Gets the associated view of specified type or null if not found
        /// </summary>
        public T GetView<T>() where T : CMSViewCore => GetView() as T;

        /// <summary>
        /// Gets a Unity component from the associated view
        /// </summary>
        public T GetUnityComponent<T>() where T : UnityEngine.Component
        {
            return GetView()?.GetComponent<T>();
        }

        #endregion

        #region [Component Operations]

        /// <summary>
        /// Gets all components attached to this entity
        /// </summary>
        public IReadOnlyDictionary<Type, IEntityComponent> GetAllComponents() => _components;

        /// <summary>
        /// Attempts to get a component of specified type
        /// </summary>
        public bool TryGetComponent<T>(out T component) where T : class, IEntityComponent
        {
            if (_components.TryGetValue(typeof(T), out var foundComponent))
            {
                component = (T)foundComponent;
                return true;
            }
    
            component = null;
            return false;
        }
        
        /// <summary>
        /// Gets a component of specified type or null if not found
        /// </summary>
        public T GetComponent<T>() where T : class, IEntityComponent
        {
            return _components.TryGetValue(typeof(T), out var component) ? (T)component : null;
        }

        /// <summary>
        /// Gets a component of specified type or adds it if not found
        /// </summary>
        public T GetOrAddComponent<T>() where T : class, IEntityComponent, new()
        {
            return (T)_components.GetOrAdd(typeof(T), _ => new T());
        }

        /// <summary>
        /// Checks if the entity has a component of specified type
        /// </summary>
        public bool HasComponent<T>() where T : IEntityComponent
        {
            return _components.ContainsKey(typeof(T));
        }

        /// <summary>
        /// Checks if the entity has a component of specified type
        /// </summary>
        public bool HasComponent(Type componentType)
        {
            return _components.ContainsKey(componentType);
        }

        /// <summary>
        /// Adds a new component to the entity
        /// </summary>
        public T AddComponent<T>() where T : IEntityComponent, new()
        {
            var component = new T();
            _components.TryAdd(typeof(T), component);
            return component;
        }

        /// <summary>
        /// Removes a component from the entity
        /// </summary>
        public bool RemoveComponent<T>() where T : IEntityComponent
        {
            return _components.TryRemove(typeof(T), out _);
        }

        /// <summary>
        /// Removes a component from the entity by type
        /// </summary>
        public bool RemoveComponent(Type componentType)
        {
            return _components.TryRemove(componentType, out _);
        }

        #endregion

        #region [XML Serialization]

        public XmlSchema GetSchema() => null;

        public void ReadXml(XmlReader reader)
        {
            EntitySerializer.ReadXml(reader, (type, component) => 
            {
                _components[type] = component;
            });
        }

        public void WriteXml(XmlWriter writer)
        {
            EntitySerializer.WriteXml(writer, GetSerializableComponents());
        }

        public Type[] GetExtraType()
        {
            return GetSerializableComponents()
                .Select(component => component.GetType())
                .ToArray();
        }

        /// <summary>
        /// Gets all components marked as serializable
        /// </summary>
        public virtual List<IEntityComponent> GetSerializableComponents()
        {
            return _components.Values
                .Where(component => component.GetType().IsDefined(typeof(SerializableAttribute)))
                .ToList();
        }

        #endregion
    }
}