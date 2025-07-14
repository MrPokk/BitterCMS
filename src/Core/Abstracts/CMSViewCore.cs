using BitterCMS.Utility.Interfaces;
using System;
using UnityEngine;

namespace BitterCMS.CMSSystem
{
    public abstract class CMSViewCore : MonoBehaviour, IInitializable<CMSPresenterCore.CMSPresenterProperty>
    {
        public Type ID => GetType();
        public CMSPresenterCore.CMSPresenterProperty Properties { get; set; }

        public void Init(CMSPresenterCore.CMSPresenterProperty property)
        {
            Properties ??= property;
        }

        public CMSEntityCore GetModel()
        {
            return Properties.PresenterCore.GetEntityByView(this);
        }

        public T GetModel<T>() where T : CMSEntityCore
        {
            return GetModel() as T;
        }
    }
}
