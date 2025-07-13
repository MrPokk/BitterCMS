using System;

namespace BitterCMS.CMSSystem
{
    /// <summary>
    /// Abstract base class for all CMS databases providing common functionality
    /// </summary>
    public abstract class CMSDatabaseCore
    {
        protected static bool IsInit;

        /// <summary>
        /// Ensures the database is initialized
        /// </summary>
        protected static void EnsureInitialized<T>(Func<T> creator) where T : CMSDatabaseCore
        {
            if (IsInit)
                return;
            
            creator().Initialize();
        }

        /// <summary>
        /// Initializes the database
        /// </summary>
        /// <param name="forceUpdate">If true, forces reinitialization even if already initialized</param>
        public abstract void Initialize(bool forceUpdate = false);
        
    }
}
