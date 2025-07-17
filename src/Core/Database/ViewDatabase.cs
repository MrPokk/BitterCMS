using BitterCMS.CMSSystem.Exceptions;
using BitterCMS.Utility;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace BitterCMS.CMSSystem
{
    /// <summary>
    /// Database for managing CMS views
    /// </summary>
    public class ViewDatabase : CMSDatabaseCore
    {
        private readonly static Dictionary<Type, CMSViewCore> AllView = new Dictionary<Type, CMSViewCore>();

        /// <summary>
        /// Gets a view by its type
        /// </summary>
        /// <param name="viewType">Type of the view</param>
        /// <returns>The view instance</returns>
        /// <exception cref="KeyNotFoundException">Thrown when view type not found</exception>
        /// <exception cref="ArgumentNullException">Thrown when view is null</exception>
        public static CMSViewCore Get(Type viewType)
        {
            EnsureInitialized(() => new ViewDatabase());

            if (!AllView.TryGetValue(viewType, out var viewResult))
                throw new ViewNotFoundException($"View of type {viewType.Name} not found in database");

            if (!viewResult)
                throw new InvalidViewException($"View is null. Check path: Resources/{PathProject.CMS_VIEWS}");

            return viewResult;
        }

        /// <summary>
        /// Gets a view by its generic type
        /// </summary>
        /// <typeparam name="T">Type of the view</typeparam>
        /// <returns>The view instance</returns>
        public static T Get<T>() where T : CMSViewCore => Get(typeof(T)) as T;

        /// <summary>
        /// Gets all registered views
        /// </summary>
        /// <returns>Collection of all views</returns>
        public static ICollection<CMSViewCore> GetAll()
        {
            EnsureInitialized(() => new ViewDatabase());
            return AllView.Values;
        }

        public override void Initialize(bool forceUpdate = false)
        {
            if (IsInit && !forceUpdate)
                return;

            try
            {
                if (forceUpdate)
                    AllView.Clear();

                var allGameObjects = Resources.LoadAll<GameObject>(PathProject.CMS_VIEWS);
                if (allGameObjects == null || allGameObjects.Length == 0)
                {
                    Debug.LogWarning($"No views found at path: {PathProject.CMS_VIEWS}");
                    return;
                }

                foreach (var view in allGameObjects)
                {
                    if (!view) 
                        continue;

                    var component = view.GetComponent<CMSViewCore>();
                    if (!component)
                    {
                        Debug.LogError($"BaseView missing in prefab: {view.name}");
                        continue;
                    }

                    AllView.TryAdd(component.ID, component);
                }

                IsInit = true;
            }
            catch (Exception ex)
            {
                throw new ViewDatabaseInitializationException(
                    $"Database initialization failed: {ex.Message}", ex);
            }
        }
    }
}
