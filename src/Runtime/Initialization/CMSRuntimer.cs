using BitterCMS.Utility;
using BitterCMS.Utility.Interfaces;
using System;
using System.Collections.Generic;

namespace BitterCMS.CMSSystem
{
    public sealed class CMSRuntimer : InteractionCore, IInitInRoot
    {
        public override Priority PriorityInteraction => Priority.High;

        private readonly static Dictionary<Type,CMSPresenterCore> CmsPresenters = new Dictionary<Type,CMSPresenterCore>();

        public void Init()
        {
            FindAll();
        }
        
        public static T GetPresenter<T>() where T : CMSPresenterCore
        {
            if (CmsPresenters.TryGetValue(typeof(T), out var value ))
                return value as T;
            
            throw new Exception("CMSManager not found");
        }
        
        public static CMSPresenterCore GetPresenterForEntityType<TEntity>() where TEntity : CMSEntityCore
        {
            foreach (var presenter in CmsPresenters.Values)
            {
                if (presenter.IsTypeAllowed(typeof(TEntity)))   
                    return presenter;
            }
            throw new Exception($"No presenter found that can handle type {typeof(TEntity).Name}");
        }

        public static IReadOnlyCollection<CMSPresenterCore> GetAllPresenters()
        {
            return CmsPresenters.Values;
        }

        private static void FindAll()
        {
            CmsPresenters.Clear();
            
            var manager = ReflectionUtility.FindAllImplement<CMSPresenterCore>();
            foreach (var element in manager)
            {
                CmsPresenters.TryAdd(element, Activator.CreateInstance(element) as CMSPresenterCore);
            }
        }
    }
}
