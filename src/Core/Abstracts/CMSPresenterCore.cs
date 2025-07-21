using BitterCMS.Component;
using BitterCMS.UnityIntegration.Utility;
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
        private readonly Dictionary<CMSEntityCore, CMSViewCore> _entitiesWithViews = new Dictionary<CMSEntityCore, CMSViewCore>();
        private readonly List<CMSEntityCore> _entitiesWithoutViews = new List<CMSEntityCore>();
        private readonly HashSet<Type> _allowedEntityTypes = new HashSet<Type>();

        private readonly static HashSet<CMSEntityCore> AllDestroy = new HashSet<CMSEntityCore>();

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
        public virtual CMSEntityCore SpawnFromDB(
            Type type, Vector3 position = default, Quaternion rotation = default, Transform parent = null
        )
        {
            var info = new CMSPresenterInfo(type, position, rotation, parent);

            if (!type.IsDefined(typeof(SerializableAttribute)))
                return Create(info);

            var newEntity = EntityDatabase.GetEntity(type);
            return Create(newEntity, info);
        }

        /// <summary>
        /// Spawn entity from database if the type is [Serializable], other creates of type
        /// </summary>
        public virtual T SpawnFromDB<T>(
            Vector3 position = default, Quaternion rotation = default, Transform parent = null
        ) where T : CMSEntityCore
        {
            return SpawnFromDB(typeof(T), position, rotation, parent) as T;
        }

        /// <summary>
        /// Spawn a new entity of type
        /// </summary>
        public virtual CMSEntityCore SpawnEntity(
            Type type, Vector3 position = default, Quaternion rotation = default, Transform parent = null
        )
        {
            return Create(new CMSPresenterInfo(type, position, rotation, parent));
        }

        /// <summary>
        /// Spawn a new entity of type
        /// </summary>
        public virtual T SpawnEntity<T>(
            Vector3 position = default, Quaternion rotation = default, Transform parent = null
        ) where T : CMSEntityCore
        {
            return SpawnEntity(typeof(T), position, rotation, parent) as T;
        }

        /// <summary>
        /// Spawn an entity from an existing CMSEntity instance
        /// </summary>
        public virtual CMSEntityCore SpawnEntity(
            CMSEntityCore valueEntityCore, Vector3 position = default, Quaternion rotation = default, Transform parent = null
        )
        {
            return Create(valueEntityCore, new CMSPresenterInfo(valueEntityCore.GetType(), position, rotation, parent));
        }

        /// <summary>
        /// Spawn an entity from an existing CMSEntity instance
        /// </summary>
        public virtual T SpawnEntity<T>(
            T valueEntityCore, Vector3 position = default, Quaternion rotation = default, Transform parent = null
        ) where T : CMSEntityCore
        {
            return SpawnEntity((CMSEntityCore)valueEntityCore, position, rotation, parent) as T;
        }

        #endregion

        #region [CreateEntity]

        private CMSEntityCore Create(CMSPresenterInfo info)
        {
            if (info.Type.IsAbstract)
                throw new TypeAccessException($"Type {info.Type.Name} is Abstract!");

            if (!IsTypeAllowed(info.Type))
                throw new InvalidOperationException($"Type {info.Type.Name} is not allowed for this presenter");

            if (!typeof(CMSEntityCore).IsAssignableFrom(info.Type))
                throw new TypeAccessException($"Type {info.Type.Name} is not a CMSEntity!");

            if (Activator.CreateInstance(info.Type) is not CMSEntityCore newEntityCore)
                return null;

            newEntityCore.Init(new CMSPresenterProperty(this));

            if (!newEntityCore.TryGetComponent<ViewComponent>(out var view))
            {
                #if DEBUG || UNITY_EDITOR
                Debug.LogWarning($"Not found ViewComponent in {newEntityCore}");
                #endif
                _entitiesWithoutViews.Add(newEntityCore);
                return newEntityCore;
            }

            var newView = LinkingMonobehaviour(newEntityCore, view, info.Position, info.Rotation, info.Parent);

            if (newView)
                _entitiesWithViews.Add(newEntityCore, newView);
            else
                _entitiesWithoutViews.Add(newEntityCore);
            
            newView?.Init(new CMSPresenterProperty(this));

            return newEntityCore;
        }

        private CMSEntityCore Create(CMSEntityCore cmsEntityCore, CMSPresenterInfo info)
        {
            cmsEntityCore.Init(new CMSPresenterProperty(this));

            if (!cmsEntityCore.TryGetComponent<ViewComponent>(out var view))
            {
                #if DEBUG || UNITY_EDITOR
                Debug.LogWarning($"Not found ViewComponent in {cmsEntityCore}");
                #endif
                _entitiesWithoutViews.Add(cmsEntityCore);
                return cmsEntityCore;
            }

            var newView = LinkingMonobehaviour(cmsEntityCore, view, info.Position, info.Rotation, info.Parent);

            if (newView)
                _entitiesWithViews.Add(cmsEntityCore, newView);
            else
                _entitiesWithoutViews.Add(cmsEntityCore);
            
            newView?.Init(new CMSPresenterProperty(this));

            return cmsEntityCore;
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
            return newView;
        }

        #endregion

        #region [GetEntity]

        /// <summary>
        /// Filters entities based on required and excluded component types
        /// </summary>
        public CMSEntityCore[] FilterEntities(
            Type[] requiredComponents = null,
            Type[] excludedComponents = null
        )
        {
            var allEntity = GetAllEntities();

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
        public CMSEntityCore[] FilterEntities(params Type[] typeComponent)
        {
            var allEntity = GetAllEntities();

            return (from entity in allEntity
                let hasAllComponents = typeComponent.All(entity.HasComponent)
                where hasAllComponents
                select entity).ToArray();
        }

        /// <summary>
        /// Filters entities that have TRequired component but don't have TExcluded component
        /// </summary>
        public IReadOnlyCollection<CMSEntityCore> FilterEntities<TRequired, TExcluded>()
            where TRequired : IEntityComponent
            where TExcluded : IEntityComponent
        {
            return GetAllEntities()
                .Where(entity => entity.HasComponent<TRequired>() && !entity.HasComponent<TExcluded>())
                .ToArray();
        }

        /// <summary>
        /// Filters entities that have the specified component type
        /// </summary>
        public IReadOnlyCollection<CMSEntityCore> FilterEntities<TRequire>() where TRequire : IEntityComponent
        {
            return GetAllEntities().Where(entity => entity.HasComponent<TRequire>()).ToArray();
        }

        /// <summary>
        /// Gets entity of specific type by its view 
        /// </summary>
        public T GetEntityByView<T>(CMSViewCore viewCore) where T : CMSEntityCore => GetEntityByView(viewCore) as T;

        /// <summary>
        /// Gets entity by its view 
        /// </summary>
        public CMSEntityCore GetEntityByView(CMSViewCore viewCore) => !viewCore ? null : _entitiesWithViews.FirstOrDefault(pair => pair.Value == viewCore).Key;

        /// <summary>
        /// Gets view by its entity
        /// </summary>
        public CMSViewCore GetViewByEntity(CMSEntityCore entityCore) => entityCore == null ? null : _entitiesWithViews.GetValueOrDefault(entityCore);

        /// <summary>
        /// Gets entity of specific type by its type
        /// </summary>
        public T GetEntityByType<T>() where T : CMSEntityCore => GetEntityByType(typeof(T)) as T;

        /// <summary>
        /// Gets entity by its type
        /// </summary>
        public CMSEntityCore GetEntityByType(Type type)
        {
            return GetAllEntities().FirstOrDefault(entity => entity.ID == type);
        }

        /// <summary>
        /// Gets all entities with their view associations
        /// </summary>
        public IReadOnlyDictionary<CMSEntityCore, CMSViewCore> GetEntitiesWithViews() => _entitiesWithViews;

        /// <summary>
        /// Gets all entities without views
        /// </summary>
        public IReadOnlyCollection<CMSEntityCore> GetEntitiesWithoutViews() => _entitiesWithoutViews;

        /// <summary>
        /// Gets all entities (both with and without views)
        /// </summary>
        public IReadOnlyCollection<CMSEntityCore> GetAllEntities()
        {
            var allEntities = new List<CMSEntityCore>(_entitiesWithViews.Count + _entitiesWithoutViews.Count);
            allEntities.AddRange(_entitiesWithViews.Keys);
            allEntities.AddRange(_entitiesWithoutViews);
            return allEntities;
        }

        /// <summary>
        /// Gets all view instances of loaded entities
        /// </summary>
        public IReadOnlyCollection<CMSViewCore> GetViewEntities() => _entitiesWithViews.Values;

        #endregion

        #region [DestroyEntity]

        #region [DestroyEntity]

        public void LateUpdate(float timeDelta)
        {
            if (!AllDestroy.Any())
                return;

            var entitiesToDestroy = new List<CMSEntityCore>(AllDestroy);
            AllDestroy.Clear();

            foreach (var entityToDestroy in entitiesToDestroy)
            {
                var entityPresenter = entityToDestroy?.Properties?.PresenterCore;
                if (entityPresenter == null)
                    continue;

                if (entityPresenter._entitiesWithViews.TryGetValue(entityToDestroy, out var view))
                {
                    if (view && view.gameObject)
                        Object.Destroy(view.gameObject);
                    entityPresenter._entitiesWithViews.Remove(entityToDestroy);
                }
                else if (entityPresenter._entitiesWithoutViews.Contains(entityToDestroy))
                    entityPresenter._entitiesWithoutViews.Remove(entityToDestroy);
            }
        }

        public virtual void DestroyEntity(CMSEntityCore entityCore)
        {
            if (entityCore == null || !AllDestroy.Add(entityCore))
                return;
        }

        public virtual void DestroyAllEntities()
        {
            CoroutineUtility.StopAll();

            foreach (var view in _entitiesWithViews.Values)
            {
                if (view != null && view.gameObject != null)
                    Object.Destroy(view.gameObject);
            }

            _entitiesWithViews.Clear();
            _entitiesWithoutViews.Clear();
        }

        #endregion 

        #endregion

        #region [Helper Methods]

        public bool IsTypeAllowed(Type type)
        {
            return _allowedEntityTypes.Count == 0 
                ? typeof(CMSEntityCore).IsAssignableFrom(type) 
                : _allowedEntityTypes.Any(allowedType => allowedType.IsAssignableFrom(type));
        }

        #endregion
    }
}
