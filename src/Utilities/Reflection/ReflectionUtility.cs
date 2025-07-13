using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace BitterCMS.Utility
{
    public static class ReflectionUtility
    {
        public static List<Type> FindAllImplement<T>()
        {
            var type = typeof(T);

            var assembly = Assembly.GetAssembly(type);

            return assembly.GetTypes().Where(elementType => elementType.IsSubclassOf(type) && !elementType.IsAbstract).ToList();
        }
    }
}
