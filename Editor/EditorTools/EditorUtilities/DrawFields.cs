using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Xml.Serialization;
using UnityEditor;
using UnityEngine;

namespace BitterCMS.UnityIntegration.Editor
{
    static class DrawFields
    {
        private readonly static Dictionary<Type, Func<string, object, Type, object>> TypeDrawers = new Dictionary<Type, Func<string, object, Type, object>>
        {
            { typeof(string), (label, value, _) => EditorGUILayout.TextField(label, (string)value) },
            { typeof(int), (label, value, _) => EditorGUILayout.IntField(label, (int)value) },
            { typeof(float), (label, value, _) => EditorGUILayout.FloatField(label, (float)value) },
            { typeof(bool), (label, value, _) => EditorGUILayout.Toggle(label, (bool)value) }
        };

        public static void DrawFieldsForObject(object target)
        {
            if (target == null) return;

            var type = target.GetType();
            
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            foreach (var field in fields)
            {
                if (Attribute.IsDefined(field, typeof(XmlIgnoreAttribute))) 
                    continue;

                EditorGUI.BeginChangeCheck();
                var newValue = DrawField(field.Name, field.GetValue(target), field.FieldType);
                
                if (EditorGUI.EndChangeCheck())
                    field.SetValue(target, newValue);
            }
            
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            foreach (var property in properties)
            {
                if (!property.CanRead || Attribute.IsDefined(property, typeof(XmlIgnoreAttribute)))
                    continue;
                
                if (property.GetIndexParameters().Length > 0)
                    continue;
                
                var isCollection = IsGenericList(property.PropertyType) || property.PropertyType.IsArray;
                if (!property.CanWrite && !isCollection)
                    continue;

                EditorGUI.BeginChangeCheck();
                var currentValue = property.GetValue(target);
                var newValue = DrawField(property.Name, currentValue, property.PropertyType);

                if (!EditorGUI.EndChangeCheck())
                    continue;
                
                if (property.CanWrite)
                    property.SetValue(target, newValue);
                else if (isCollection && currentValue != null)
                    HandleCollectionModification(currentValue, newValue, property.PropertyType);
                
            }
        }

        private static void HandleCollectionModification(object currentCollection, object newCollection, Type collectionType)
        {
            if (collectionType.IsArray)
            {
                var currentArray = (Array)currentCollection;
                var newArray = (Array)newCollection;
                
                if (currentArray.Length != newArray.Length)
                    return;
                
                for (int i = 0; i < currentArray.Length; i++)
                    currentArray.SetValue(newArray.GetValue(i), i);
            }
            else if (IsGenericList(collectionType))
            {
                var currentList = (IList)currentCollection;
                var newList = (IList)newCollection;
                
                currentList.Clear();
                foreach (var item in newList)
                    currentList.Add(item);
            }
        }
        
        private static object DrawField(string label, object value, Type type)
        {
            if (type == null)
                return HandleNullType(label, value);

            if (TypeDrawers.TryGetValue(type, out var drawer))
                return drawer(label, value, type);

            if (type.IsEnum)
                return EditorGUILayout.EnumPopup(label, (Enum)value);

            if (type.IsArray)
                return DrawArrayField(label, value, type);

            if (IsGenericList(type))
                return DrawListField(label, value, type);

            return HandleUnsupportedType(label, value, type);
        }

        private static object HandleNullType(string label, object value)
        {
            EditorGUILayout.LabelField(label, "Type is null");
            return value;
        }

        private static object HandleUnsupportedType(string label, object value, Type type)
        {
            EditorGUILayout.LabelField(label, type.IsClass 
                ? $"{label} (Unsupported class type: {type.Name})" 
                : $"Unsupported type: {type.Name}");
            return value;
        }

        private static bool IsGenericList(Type type) => 
            type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>);

        private static object DrawArrayField(string label, object value, Type arrayType)
        {
            var elementType = arrayType.GetElementType();
            var array = (Array)value;
            var currentSize = array?.Length ?? 0;

            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            
            using (new EditorGUI.IndentLevelScope())
            {
                var newSize = Mathf.Max(0, EditorGUILayout.IntField("Size", currentSize));
                
                if ((array == null || newSize != currentSize) && elementType != null)
                    array = Array.CreateInstance(elementType, newSize);

                for (int i = 0; i < newSize; i++)
                    array?.SetValue(DrawField($"Element {i}", array.GetValue(i), elementType), i);
            }

            return array;
        }

        private static object DrawListField(string label, object value, Type listType)
        {
            var elementType = listType.GetGenericArguments()[0];
            var list = (IList)(value ?? Activator.CreateInstance(listType));

            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            
            using (new EditorGUI.IndentLevelScope())
            {
                DrawListElements(list, elementType);
                DrawListButtons(list, elementType);
            }

            return list;
        }

        private static void DrawListElements(IList list, Type elementType)
        {
            for (int i = 0; i < list.Count; i++)
            {
                var newValue = DrawField($"Element {i}", list[i], elementType);
                if (!Equals(newValue, list[i]))
                    list[i] = newValue;
            }
        }

        private static void DrawListButtons(IList list, Type elementType)
        {
            EditorGUILayout.Space();
            
            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("+"))
                    list.Add(CreateDefault(elementType));

                GUI.enabled = list.Count > 0;
                if (GUILayout.Button("-"))
                    list.RemoveAt(list.Count - 1);
                GUI.enabled = true;
            }
            EditorGUILayout.EndHorizontal();
        }

        private static object CreateDefault(Type type) => 
            type.IsValueType ? Activator.CreateInstance(type) : null;
    }
}