using BitterCMS.CMSSystem.Exceptions;
using BitterCMS.System.Serialization;
using BitterCMS.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BitterCMS.CMSSystem
{
    /// <summary>
    /// Database for managing CMS entities and their storage paths
    /// </summary>
    public class EntityDatabase : CMSDatabaseCore
    {
        private readonly static Dictionary<Type, string> AllEntity = new Dictionary<Type, string>();

        /// <summary>
        /// Gets an entity by its type
        /// </summary>
        /// <param name="typeEntity">Type of the entity</param>
        /// <returns>The deserialized entity</returns>
        /// <exception cref="TypeAccessException">Thrown when type is not a CMSEntity</exception>
        public static CMSEntityCore GetEntity(Type typeEntity)
        {
            if (!typeof(CMSEntityCore).IsAssignableFrom(typeEntity))
                throw new TypeAccessException("Type must inherit from CMSEntity");

            var path = GetPath(typeEntity);
            if (string.IsNullOrEmpty(path))
                throw new EntityNotFoundException($"Path not found for entity type: {typeEntity.Name}");

            return SerializerUtility.TryDeserialize(typeEntity, path) as CMSEntityCore;
        }

        /// <summary>
        /// Gets an entity by its generic type
        /// </summary>
        /// <typeparam name="T">Type of the entity</typeparam>
        /// <returns>The deserialized entity</returns>
        public static T GetEntity<T>() where T : CMSEntityCore, new()
        {
            var path = GetPath<T>();
            if (string.IsNullOrEmpty(path))
                throw new EntityNotFoundException($"Path not found for entity type: {typeof(T).Name}");

            return SerializerUtility.TryDeserialize<T>(path);
        }

        /// <summary>
        /// Gets the storage path for an entity type
        /// </summary>
        /// <param name="typeEntity">Type of the entity</param>
        /// <returns>The storage path if found, null otherwise</returns>
        public static string GetPath(Type typeEntity)
        {
            EnsureInitialized(() => new EntityDatabase());
            return AllEntity.GetValueOrDefault(typeEntity);
        }

        public static string GetPath<T>() where T : CMSEntityCore => GetPath(typeof(T));

        /// <summary>
        /// Gets all registered entities and their paths
        /// </summary>
        /// <returns>Read-only dictionary of entity types and paths</returns>
        public static IReadOnlyDictionary<Type, string> GetAll()
        {
            EnsureInitialized(() => new EntityDatabase());
            return new Dictionary<Type, string>(AllEntity);
        }

        public override void Initialize(bool forceUpdate = false)
        {
            if (IsInit && !forceUpdate)
                return;

            try
            {
                if (forceUpdate)
                {
                    AllEntity.Clear();
                }

                var allImplementEntity = ReflectionUtility.FindAllImplement<CMSEntityCore>()
                    .Where(entity => entity.IsDefined(typeof(SerializableAttribute), false));

                foreach (var typeEntity in allImplementEntity)
                {
                    var fullPath = GetPathToXmlEntity(typeEntity);
                    AllEntity.TryAdd(typeEntity, fullPath);
                }

                IsInit = true;
            }
            catch (Exception ex)
            {
                throw new EntityDatabaseInitializationException(
                    $"Database initialization failed: {ex.Message}", ex);
            }
        }

        private string GetPathToXmlEntity(Type typeEntity)
        {
            var directoryPath = PathUtility.GetFullPath(PathProject.CMS_ENTITIES);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            var fileName = $"{typeEntity.Name}.xml";
            return Path.Combine(directoryPath, fileName);
        }
    }
}
