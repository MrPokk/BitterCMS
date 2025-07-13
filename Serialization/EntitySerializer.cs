using BitterCMS.CMSSystem;
using BitterCMS.Utility.Interfaces;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Serialization;

namespace BitterCMS.System.Serialization
{
    public static class EntitySerializer
    {
        public static void ReadXml(XmlReader reader, Action<Type,IEntityComponent> callback)
        {
            if (reader.NodeType == XmlNodeType.XmlDeclaration)
                reader.Read();

            if (reader.IsEmptyElement)
            {
                reader.Read();
                return;
            }
           
            reader.ReadStartElement();

            while (reader is { NodeType: XmlNodeType.Element, LocalName: "Component" })
            {
                var typeName = reader.GetAttribute("type", "http://www.w3.org/2001/XMLSchema-instance");
                if (string.IsNullOrEmpty(typeName))
                {
                    reader.Skip();
                    continue;
                }

                var componentType = ComponentDatabase.GetTypeByName(typeName);
                if (componentType == null)
                {
                    reader.Skip();
                    continue;
                }
              
                reader.ReadStartElement("Component");
                try
                {
                    var serializer = new XmlSerializer(componentType);
                    var component = serializer.Deserialize(reader) as IEntityComponent;
                    
                    callback?.Invoke(componentType,component);
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

            reader.ReadEndElement();
        }
        public static void WriteXml(XmlWriter writer, IEnumerable<IEntityComponent> allSerializeComponents)
        {
            writer.WriteAttributeString("xmlns:xsi", "http://www.w3.org/2001/XMLSchema-instance");
            writer.WriteAttributeString("xmlns:xsd", "http://www.w3.org/2001/XMLSchema");

            foreach (var component in allSerializeComponents)
            {
                writer.WriteStartElement("Component");
                writer.WriteAttributeString("xsi", "type", "http://www.w3.org/2001/XMLSchema-instance",
                    component.GetType().Name);

                var serializer = new XmlSerializer(component.GetType());
                serializer.Serialize(writer, component);

                writer.WriteEndElement();
            }
        }

    }
}
