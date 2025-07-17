using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace BitterCMS.Utility
{
    public static class ReflectionUtility
    {
        public static Type[] FindAllImplement<T>()
        {
            var type = typeof(T);

            var assembly = Assembly.GetAssembly(type);

            return assembly.GetTypes().Where(elementType => elementType.IsSubclassOf(type) && !elementType.IsAbstract).ToArray();
        }

        public static Type[] FindAllAssignments<T>()
        {
            var type = typeof(T);

            var assembly = Assembly.GetAssembly(type);
            
            return assembly.GetTypes().Where(elementType => !elementType.IsAbstract && typeof(T).IsAssignableFrom(elementType)).ToArray();
        }
    }
}
