using BitterCMS.CMSSystem;
using BitterCMS.Utility.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Serialization;
using UnityEngine;

namespace BitterCMS.System.Serialization
{
    public static class EntitySerializer
    {

        #region [ReadXml]

        public static void ReadXml(XmlReader reader, Action<Type, IEntityComponent> callback, object targetEntity)
        {
            if (reader.NodeType == XmlNodeType.XmlDeclaration)
                reader.Read();

            if (reader.IsEmptyElement)
            {
                reader.Read();
                return;
            }

            reader.ReadStartElement();

            while (reader.NodeType == XmlNodeType.Element)
            {
                if (reader.LocalName == "Component")
                    ReadComponent(reader, callback);
                else
                    ReadSimpleField(reader, targetEntity);
            }

            reader.ReadEndElement();
        }

        private static void ReadComponent(XmlReader reader, Action<Type, IEntityComponent> callback)
        {
            var typeName = reader.GetAttribute("type", "http://www.w3.org/2001/XMLSchema-instance");
            if (string.IsNullOrEmpty(typeName))
            {
                reader.Skip();
                return;
            }

            var componentType = ComponentDatabase.GetTypeByName(typeName);
            if (componentType == null)
            {
                reader.Skip();
                return;
            }

            reader.ReadStartElement("Component");
            try
            {
                var serializer = new XmlSerializer(componentType);
                var component = serializer.Deserialize(reader) as IEntityComponent;

                callback?.Invoke(componentType, component);
            }
            catch (Exception ex)
            {
                throw new SerializationException($"Error deserializing {typeName}: {ex.Message}");
            }
            finally
            {
                while (reader.NodeType != XmlNodeType.EndElement)
                    reader.Read();
                reader.ReadEndElement();
            }
        }

        private static void ReadSimpleField(XmlReader reader, object targetEntity)
        {
            var fieldName = reader.LocalName;
            var field = targetEntity.GetType().GetField(fieldName,
                BindingFlags.Public | BindingFlags.Instance);

            if (field != null)
            {
                if (IsCollection(field.FieldType))
                    ReadCollection(reader, field, targetEntity);
                else
                {
                    var valueObject = reader.ReadElementContentAs(field.FieldType, null);
                    try
                    {
                        field.SetValue(targetEntity, valueObject);
                    }
                    catch (Exception ex)
                    {
                        throw new SerializationException($"Error setting field {fieldName} with value '{valueObject}': {ex.Message}");
                    }
                }
            }
            else
            {
                reader.Skip();
            }
        }

        private static void ReadCollection(XmlReader reader, FieldInfo field, object targetEntity)
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));
            if (field == null) throw new ArgumentNullException(nameof(field));
            if (targetEntity == null) throw new ArgumentNullException(nameof(targetEntity));

            var elementType = GetElementCollectionType(field.FieldType);
            var items = CreateListForElementType(elementType);

            if (reader.IsEmptyElement)
            {
                reader.Read();
                SetFieldValue(field, targetEntity, items);
                return;
            }

            reader.ReadStartElement();

            while (reader.IsStartElement())
            {
                if (reader.LocalName == "Item")
                    items.Add(reader.ReadElementContentAs(elementType, null));
                else
                    reader.Skip();
            }

            if (reader.NodeType == XmlNodeType.EndElement)
                reader.ReadEndElement();

            SetFieldValue(field, targetEntity, items);

        }
        
        #endregion
        
        #region [WriteXML]

        public static void WriteXml(XmlWriter writer, IEnumerable<IEntityComponent> allSerializeComponents, object entity)
        {
            writer.WriteAttributeString("xmlns:xsi", "http://www.w3.org/2001/XMLSchema-instance");
            writer.WriteAttributeString("xmlns:xsd", "http://www.w3.org/2001/XMLSchema");

            var fields = entity.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (var field in fields)
            {
                if (field.GetValue(entity) == null)
                    continue;

                writer.WriteStartElement(field.Name);

                if (IsCollection(field.FieldType))
                    WriteCollection(writer, field.GetValue(entity));
                else
                    writer.WriteValue(field.GetValue(entity));

                writer.WriteEndElement();
            }

            foreach (var component in allSerializeComponents)
            {
                writer.WriteStartElement("Component");
                writer.WriteAttributeString("xsi", "type", "http://www.w3.org/2001/XMLSchema-instance",
                    component.GetType().Name);

                var serializer = SerializerUtility.GetXmlSerializer(component.ID);
                serializer.Serialize(writer, component);

                writer.WriteEndElement();
            }
        }

        private static void WriteCollection(XmlWriter writer, object collection)
        {

            if (collection is not IEnumerable enumerable)
                return;

            foreach (var item in enumerable)
            {
                writer.WriteStartElement("Item");
                writer.WriteValue(item);
                writer.WriteEndElement();
            }
        }

        #endregion

        #region [Helper Method]

        private static bool IsCollection(Type type)
        {
            return type.IsArray ||
                   type.IsGenericType &&
                   (type.GetGenericTypeDefinition() == typeof(List<>) ||
                    type.GetGenericTypeDefinition() == typeof(HashSet<>));
        }

        private static Type GetElementCollectionType(Type collectionType)
        {
            if (collectionType.IsArray)
                return collectionType.GetElementType();

            if (IsCollection(collectionType))
                return collectionType.GetGenericArguments().Single();

            throw new NotSupportedException($"Unsupported collection type: {collectionType}");
        }

        private static object CreateCollection(Type targetType, IList items)
        {
            if (targetType == null) throw new ArgumentNullException(nameof(targetType));
            if (items == null) throw new ArgumentNullException(nameof(items));

            if (targetType.IsArray)
                return CreateArray(targetType, items);

            var collection = Activator.CreateInstance(targetType, items);

            return collection;
        }

        private static Array CreateArray(Type targetType, IList items)
        {
            var elementType = GetElementCollectionType(targetType);

            var array = Array.CreateInstance(elementType, items.Count);
            items.CopyTo(array, 0);
            
            return array;
        }

        private static IList CreateListForElementType(Type elementType)
        {
            var listType = typeof(List<>).MakeGenericType(elementType);
            return (IList)Activator.CreateInstance(listType);
        }

        private static void SetFieldValue(FieldInfo field, object target, IList items)
        {
            var collection = CreateCollection(field.FieldType, items);
            field.SetValue(target, collection);
        }

        #endregion
    }
}
