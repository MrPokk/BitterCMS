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

        public static string[] GetAllPaths()
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

        public static string GetFullPath(string pathBase, string extraPath = "Resources")
        {
            var allBasePath = GetAllPaths();

            if (!allBasePath.Contains(pathBase))
                throw new ArgumentException("ERROR: Path not found");

            var fullPath = $"{Application.dataPath}/!{Application.productName}/{extraPath}/{pathBase}";
            
            return fullPath;
        }

        public static string GetRelativePath(string absolutePath)
        {
            var projectPath = Application.dataPath;
            var relativePath = Path.GetRelativePath(projectPath, absolutePath);
            return relativePath;
        }

        public static void ValidatePath(ref string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path cannot be null or whitespace", nameof(path));

            try
            {
                var fullPath = Path.GetFullPath(path);

                if (path.Any(c => Path.GetInvalidPathChars().Contains(c)))
                    throw new ArgumentException("Path contains invalid characters");

                var directory = Path.GetDirectoryName(fullPath);
                if (!Directory.Exists(directory))
                {
                    if (directory != null)
                        Directory.CreateDirectory(directory);
                }

                if (!HasWritePermission(directory))
                    throw new UnauthorizedAccessException($"No write permissions for directory: {directory}");

                if (!path.EndsWith(Path.AltDirectorySeparatorChar))
                    path += Path.AltDirectorySeparatorChar;
            }
            catch (Exception ex) when (ex is ArgumentException or PathTooLongException)
            {
                throw new ArgumentException($"Invalid path: {path}", nameof(path), ex);
            }
        }

        private static bool HasWritePermission(string directoryPath)
        {
            try
            {
                var permissionFile = Path.Combine(directoryPath, Guid.NewGuid().ToString());
                File.WriteAllText(permissionFile, "Permission");
                File.Delete(permissionFile);
                return true;
            }
            catch
            {
                return false;
            }
        }

    }
}
