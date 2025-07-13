using System;
using System.Xml.Serialization;

namespace BitterCMS.Utility.Interfaces
{
    public interface IEntityComponent
    {
        public Type ID => GetType();
    }
    
    public interface ISerializerProvider
    {
        Type GetObjectType();
        string GetFullPath();

        string Serialization();
        object Deserialize();
    }
    
    public interface IRoot
    {
        protected internal void PreStartGame();
        protected internal void UpdateGame( float timeDelta);
        protected internal void PhysicUpdateGame(float timeDelta);
        protected internal void LateUpdateGame(float timeDelta);

        protected internal void StoppedGame();
    }

    public interface IXmlIncludeExtraType
    {
        public Type[] GetExtraType();
    }
    
    public interface IInitializable
    {
        public void Init();
    }
    
    public interface IInitializable<T> where T : InitializableProperty
    {
        [XmlIgnore]
        public T Properties { get; set; }

        public void Init(T property);
        public T ValidateProperty(T property) { return property; }
    }
    
    public abstract class InitializableProperty : IXmlIncludeExtraType
    {
        public virtual Type[] GetExtraType() => new[] { GetType() };
    }
}
