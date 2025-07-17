using BitterCMS.Utility;
using BitterCMS.Utility.Interfaces;
using System;

namespace BitterCMS.CMSSystem
{
    public class CMSDatabaseInitializer : InteractionCore, IInitInRoot
    {
        public override Priority PriorityInteraction => Priority.FIRST_TASK;

        public void Init()
        {
            UpdateDatabase(true);
        }

        public static void UpdateDatabase(bool forceUpdate = false)
        {
            var allDatabase = ReflectionUtility.FindAllImplement<CMSDatabaseCore>();
            foreach (var database in allDatabase)
            {
                if (Activator.CreateInstance(database) is CMSDatabaseCore newDatabase)
                    newDatabase.Initialize(forceUpdate);
            }
        }
    }
}
