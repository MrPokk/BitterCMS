using BitterCMS.Component;
using BitterCMS.Utility.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BitterCMS.CMSSystem
{
    public abstract class CMSPresenterCore : InteractionCore, IEnterInLateUpdate
    {
        private readonly Dictionary<CMSViewCore, CMSEntityCore> _loadedEntity = new Dictionary<CMSViewCore, CMSEntityCore>();
        private readonly HashSet<Type> _allowedEntityTypes = new HashSet<Type>();
        private readonly static HashSet<CMSViewCore> AllDestroy = new HashSet<CMSViewCore>();

        protected CMSPresenterCore(params Type[] allowedTypes)
        {
            foreach (var type in allowedTypes)
            {
                if (!typeof(CMSEntityCore).IsAssignableFrom(type))
                    throw new ArgumentException($"Type {type.Name} must inherit from CMSEntity");

                _allowedEntityTypes.Add(type);
            }
        }

        #region [Additionally Data]

        public sealed class CMSPresenterProperty : InitializableProperty
        {
            public readonly CMSPresenterCore PresenterCore;
            public CMSPresenterProperty(CMSPresenterCore presenterCore)
            {
                PresenterCore = presenterCore;
            }
        }
        private struct CMSPresenterInfo
        {
            public readonly Type Type;
            public readonly Vector3 Position;
            public readonly Quaternion Rotation;
            public readonly Transform Parent;

            public CMSPresenterInfo(Type type, Vector3 position = default, Quaternion rotation = default, Transform parent = null)
            {
                Type = type;
                Position = position;
                Rotation = rotation;
                Parent = parent;
            }
        }

        #endregion

        #region [SpawnEntity]

        /// <summary>
        /// Spawn entity from database if the type is [Serializable], other creates of type
        /// </summary>
        public virtual CMSViewCore SpawnFromDB(
            Type type,
            Vector3 position = default, Quaternion rotation = default, Transform parent = null
        )
        {
            var info = new CMSPresenterInfo(type, position, rotation, parent);

            if (!type.IsDefined(typeof(SerializableAttribute)))
                return Create(info);

            var newEntity = EntityDatabase.GetEntity(type);
            return Create(newEntity, info);
        }

        /// <summary>
        /// Spawn a new entity of type
        /// </summary>
        public virtual CMSViewCore SpawnEntity(
            Type type,
            Vector3 position = default, Quaternion rotation = default, Transform parent = null
        )
        {
            return Create(new CMSPresenterInfo(type, position, rotation, parent));
        }

        /// <summary>
        /// Spawn an entity from an existing CMSEntity instance
        /// </summary>
        public virtual CMSViewCore SpawnEntity(
            CMSEntityCore valueEntityCore,
            Vector3 position = default, Quaternion rotation = default, Transform parent = null
        )
        {
            return Create(valueEntityCore, new CMSPresenterInfo(valueEntityCore.GetType(), position, rotation, parent));
        }

        #endregion

        #region [CreateEntity]

        private CMSViewCore Create(CMSPresenterInfo info)
        {
            if (info.Type.IsAbstract)
                throw new TypeAccessException($"Type {info.Type.Name} is Abstract!");

            if (!IsTypeAllowed(info.Type))
                throw new InvalidOperationException($"Type {info.Type.Name} is not allowed for this presenter");

            if (!typeof(CMSEntityCore).IsAssignableFrom(info.Type))
                throw new TypeAccessException($"Type {info.Type.Name} is not a CMSEntity!");

            if (Activator.CreateInstance(info.Type) is not CMSEntityCore newObject)
                return null;

            newObject.Init(new CMSPresenterProperty(this));

            if (!newObject.TryGetComponent<ViewComponent>(out var view))
                return null;

            var newView = LinkingMonobehaviour(newObject, view, info.Position, info.Rotation, info.Parent);
            newView?.Init(new CMSPresenterProperty(this));
            return newView;
        }

        private CMSViewCore Create(CMSEntityCore cmsEntityCore, CMSPresenterInfo info)
        {
            cmsEntityCore.Init(new CMSPresenterProperty(this));

            if (!cmsEntityCore.TryGetComponent<ViewComponent>(out var view))
                return null;

            var newView = LinkingMonobehaviour(cmsEntityCore, view, info.Position, info.Rotation, info.Parent);
            newView?.Init(new CMSPresenterProperty(this));
            return newView;
        }

        #endregion

        #region [Entity Management]

        private CMSViewCore LinkingMonobehaviour(
            CMSEntityCore entityCore, ViewComponent view,
            Vector3 position, Quaternion rotation, Transform parent
        )
        {
            if (!view?.Properties.Original || entityCore == null)
                return null;

            var newView = Object.Instantiate(view.Properties.Original, position, rotation, parent);
            newView.name = $"{entityCore.ID.Name} [NEW]";

            view.Properties.Current = newView;
            _loadedEntity[newView] = entityCore;

            return newView;
        }

        #endregion

        #region [GetEntity]

        /// <summary>
        /// Filters entities based on required and excluded component types
        /// </summary>
        /// <param name="requiredComponents">Component types that entities must have (null to ignore)</param>
        /// <param name="excludedComponents">Component types that entities must not have (null to ignore)</param>
        /// <returns>Array of entities matching the filter criteria</returns>
        public CMSEntityCore[] FilterEntities(
            Type[] requiredComponents = null,
            Type[] excludedComponents = null
        )
        {
            var allEntity = GetModelEntities();

            return allEntity.Where(entity => {
                var hasRequired = requiredComponents == null ||
                                  requiredComponents.All(entity.HasComponent);

                var hasExcluded = excludedComponents != null &&
                                  excludedComponents.Any(entity.HasComponent);

                return hasRequired && !hasExcluded;
            }).ToArray();
        }

        /// <summary>
        /// Filters entities that have all specified component types
        /// </summary>
        /// <param name="typeComponent">Component types that entities must have</param>
        /// <returns>Array of entities containing all specified components</returns>
        public CMSEntityCore[] FilterEntities(params Type[] typeComponent)
        {
            var allEntity = GetModelEntities();

            return (from entity in allEntity
                let hasAllComponents =
                    typeComponent.All(entity.HasComponent)
                where hasAllComponents
                select entity).ToArray();
        }

        /// <summary>
        /// Filters entities that have TRequired component but don't have TExcluded component
        /// </summary>
        /// <typeparam name="TRequired">Required component type</typeparam>
        /// <typeparam name="TExcluded">Excluded component type</typeparam>
        /// <returns>Collection of entities matching the component criteria</returns>
        public IReadOnlyCollection<CMSEntityCore> FilterEntities<TRequired, TExcluded>()
            where TRequired : IEntityComponent
            where TExcluded : IEntityComponent
        {
            return GetModelEntities()
                .Where(entity => entity.HasComponent<TRequired>() && !entity.HasComponent<TExcluded>())
                .ToArray();
        }

        /// <summary>
        /// Filters entities that have the specified component type
        /// </summary>
        /// <typeparam name="TRequire">Component type that entities must have</typeparam>
        /// <returns>Collection of entities containing the specified component</returns>
        public IReadOnlyCollection<CMSEntityCore> FilterEntities<TRequire>() where TRequire : IEntityComponent
        {
            return GetModelEntities().Where(entity => entity.HasComponent<TRequire>()).ToArray();
        }

        /// <summary>
        /// Gets entity of specific type by its view ID
        /// </summary>
        /// <typeparam name="T">Type of entity to retrieve</typeparam>
        /// <param name="ID">View ID of the entity</param>
        /// <returns>Entity cast to specified type or null if not found</returns>
        public T GetEntityByID<T>(in CMSViewCore ID) where T : CMSEntityCore => GetEntityByID(ID) as T;

        /// <summary>
        /// Gets entity by its view ID
        /// </summary>
        /// <param name="ID">View ID of the entity</param>
        /// <returns>Entity or null if not found</returns>
        public CMSEntityCore GetEntityByID(in CMSViewCore ID) => ID ? _loadedEntity.GetValueOrDefault(ID) : null;

        /// <summary>
        /// Gets entity of specific type by its type
        /// </summary>
        /// <typeparam name="T">Type of entity to retrieve</typeparam>
        /// <returns>Entity cast to specified type or null if not found</returns>
        public T GetEntityByType<T>() where T : CMSEntityCore => GetEntityByType(typeof(T)) as T;

        /// <summary>
        /// Gets entity by its type
        /// </summary>
        /// <param name="type">Type of entity to retrieve</param>
        /// <returns>Entity or null if not found</returns>
        public CMSEntityCore GetEntityByType(Type type) => _loadedEntity.Values.FirstOrDefault(entity => entity.ID == type);

        /// <summary>
        /// Gets all entities with their view associations
        /// </summary>
        /// <returns>Read-only dictionary of view-entity pairs</returns>
        public IReadOnlyDictionary<CMSViewCore, CMSEntityCore> GetEntities() => _loadedEntity;

        /// <summary>
        /// Gets all view instances of loaded entities
        /// </summary>
        /// <returns>Read-only collection of entity views</returns>
        public IReadOnlyCollection<CMSViewCore> GetViewEntities() => _loadedEntity.Keys;

        /// <summary>
        /// Gets all entity models (without view references)
        /// </summary>
        /// <returns>Read-only collection of entity models</returns>
        public IReadOnlyCollection<CMSEntityCore> GetModelEntities() => _loadedEntity.Values;

        #endregion

        #region [DestroyEntity]

        public void LateUpdate(float timeDelta)
        {
            if (!AllDestroy.Any())
                return;

            foreach (var viewDestroy in AllDestroy)
            {
                _loadedEntity.Remove(viewDestroy);
                Object.Destroy(viewDestroy.gameObject);
            }
            AllDestroy.Clear();
        }

        public virtual void DestroyEntity(in CMSViewCore ID)
        {
            if (!ID)
                return;

            AllDestroy.Add(ID);
        }

        public virtual void DestroyAllEntities()
        {
            foreach (var entity in _loadedEntity.Keys)
            {
                if (entity && entity.gameObject)
                    Object.Destroy(entity.gameObject);
            }
            _loadedEntity.Clear();
        }

        #endregion

        #region [Helper Methods]

        public bool IsTypeAllowed(Type type)
        {
            if (_allowedEntityTypes.Count == 0)
                return typeof(CMSEntityCore).IsAssignableFrom(type);

            return _allowedEntityTypes.Any(allowedType => allowedType.IsAssignableFrom(type));
        }

        #endregion
    }
}
