using BitterCMS.CMSSystem.Exceptions;
using BitterCMS.System.Serialization;
using BitterCMS.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace BitterCMS.CMSSystem
{
    /// <summary>
    /// Database for managing CMS entities and their XML data
    /// </summary>
    public class EntityDatabase : CMSDatabaseCore
    {
        private readonly static Dictionary<Type, string> AllEntityXmlData = new Dictionary<Type, string>();

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

            if (!AllEntityXmlData.TryGetValue(typeEntity, out var xmlData))
                throw new EntityNotFoundException($"XML data not found for entity type: {typeEntity.Name}");

            return SerializerUtility.TryDeserialize(typeEntity, xmlData) as CMSEntityCore;
        }

        /// <summary>
        /// Gets an entity by its generic type
        /// </summary>
        /// <typeparam name="T">Type of the entity</typeparam>
        /// <returns>The deserialized entity</returns>
        public static T GetEntity<T>() where T : CMSEntityCore, new() => GetEntity(typeof(T)) as T;

        /// <summary>
        /// Gets all registered entities and their XML data
        /// </summary>
        /// <returns>Read-only dictionary of entity types and XML data</returns>
        public static IReadOnlyDictionary<Type, string> GetAll()
        {
            EnsureInitialized(() => new EntityDatabase());
            return new Dictionary<Type, string>(AllEntityXmlData);
        }

        public override void Initialize(bool forceUpdate = false)
        {
            if (IsInit && !forceUpdate)
                return;

            try
            {
                if (forceUpdate)
                    AllEntityXmlData.Clear();

                var allImplementEntity = ReflectionUtility.FindAllImplement<CMSEntityCore>()
                    .Where(entity => entity.IsDefined(typeof(SerializableAttribute), false));

                foreach (var typeEntity in allImplementEntity)
                {
                    var textAsset = Resources.Load<TextAsset>(GetRelativePathToXmlEntity(typeEntity));
                    if (textAsset)
                        AllEntityXmlData.TryAdd(typeEntity, textAsset.text);
                    else
                        AllEntityXmlData.TryAdd(typeEntity, GetRelativePathToXmlEntity(typeEntity));
                }

                IsInit = true;
            }
            catch (Exception ex)
            {
                throw new EntityDatabaseInitializationException(
                    $"Database initialization failed: {ex.Message}", ex);
            }
        }

        public static void SaveEntity(Type entityType)
        {
            if (!AllEntityXmlData.TryGetValue(entityType, out var xmlData))
                throw new EntityNotFoundException($"XML data not found for entity type: {entityType.Name}");
            
            var relativePath = GetRelativePathToXmlEntity(entityType);
            var fullPath = Path.Combine(
                Application.dataPath,
                $"!{Application.productName}",
                "Resources",
                relativePath + ".xml");

            SerializerUtility.TrySerialize(entityType, fullPath);
        }

        public static void UpdateEntityData(Type entityType)
        {
            if (!typeof(CMSEntityCore).IsAssignableFrom(entityType))
                throw new TypeAccessException("Type must inherit from CMSEntity");

            var xmlData = Resources.Load<TextAsset>(GetRelativePathToXmlEntity(entityType)).text;

            AllEntityXmlData[entityType] = xmlData;
        }

        private static string GetRelativePathToXmlEntity(Type typeEntity)
        {
            return Path.Combine(PathProject.CMS_ENTITIES, typeEntity.Name);
        }
    }
}
