using BitterCMS.CMSSystem.Exceptions;
using BitterCMS.Utility.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace BitterCMS.CMSSystem
{
    /// <summary>
    /// Database for managing CMS component types
    /// </summary>
    public class ComponentDatabase : CMSDatabaseCore
    {
        private readonly static Dictionary<string, Type> ComponentTypesCache = new Dictionary<string, Type>();

        /// <summary>
        /// Gets a component type by its name
        /// </summary>
        /// <param name="name">Name of the component type</param>
        /// <returns>The Type if found, null otherwise</returns>
        /// <exception cref="ArgumentNullException">Thrown when name is null or empty</exception>
        public static Type GetTypeByName(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            EnsureInitialized(() => new ComponentDatabase());

            return ComponentTypesCache.GetValueOrDefault(name);
        }

        /// <summary>
        /// Gets all registered component types
        /// </summary>
        /// <returns>Read-only dictionary of component types</returns>
        public static IReadOnlyDictionary<string, Type> GetAllComponentTypes()
        {
            EnsureInitialized(() => new ComponentDatabase());
            return new Dictionary<string, Type>(ComponentTypesCache);
        }

        public override void Initialize(bool forceUpdate = false)
        {

            if (IsInit && !forceUpdate)
                return;

            try
            {
                if (forceUpdate)
                {
                    ComponentTypesCache.Clear();
                }

                var componentTypes = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(assembly => assembly.GetTypes())
                    .Where(t => !t.IsAbstract && typeof(IEntityComponent).IsAssignableFrom(t));

                foreach (var type in componentTypes)
                {
                    ComponentTypesCache.TryAdd(type.Name, type);
                }

                IsInit = true;
            }
            catch (ReflectionTypeLoadException ex)
            {
                var loaderErrors = string.Join(", ", ex.LoaderExceptions.Select(e => e?.Message));
                throw new ComponentDatabaseInitializationException(
                    $"Component type loading failed. Loader exceptions: {loaderErrors}", ex);
            }
            catch (Exception ex)
            {
                throw new ComponentDatabaseInitializationException(
                    $"Database initialization failed: {ex.Message}", ex);
            }

        }
    }
}
