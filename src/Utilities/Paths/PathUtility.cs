using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace BitterCMS.Utility
{
    public static class PathUtility
    {
        private static string[] _cachedPaths = null;
        private static int _cachedFieldCount = 0;

        private const string ROOT_PATH = "Resources";
        
        public static void GenerationConstPath()
        {
            try
            {
                var allPath = GetAllPaths();
                foreach (var path in allPath)
                {
                    var finalPath = GetFullPath(path);   
                    if (!Directory.Exists(finalPath))
                        Directory.CreateDirectory(finalPath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        
        

        private static string[] GetAllPaths()
        {
            if (IsCacheValid())
                return _cachedPaths;

            _cachedPaths = GetValidPathsFromFields();
            _cachedFieldCount = _cachedPaths.Length;

            return _cachedPaths;
        }

        private static bool IsCacheValid()
        {
            return _cachedPaths != null && _cachedPaths.Length == _cachedFieldCount;
        }

        private static string[] GetValidPathsFromFields()
        {
            var fields = typeof(PathProject)
                .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

            return fields
                .Where(field => field.FieldType == typeof(string) && field.IsLiteral && !field.IsInitOnly)
                .Select(field => (string)field.GetValue(null))
                .ToArray();
        }

        private static string GetFullPath(string pathBase)
        {
            var allBasePath = GetAllPaths();

            if (!allBasePath.Contains(pathBase))
                throw new ArgumentException($"ERROR: Path not base: {pathBase}");
            

            var fullPath = Path.Combine(
                Application.dataPath,
                $"!{Application.productName}",
                ROOT_PATH,
                pathBase);

            return fullPath;
        }

    }
}
