using System;
using System.Collections.Generic;
using System.IO;

namespace BitterCMS.CMSSystem.Exceptions
{
    public class ViewDatabaseInitializationException : InvalidOperationException
    {
        public ViewDatabaseInitializationException(string message, Exception inner)
            : base(message, inner)
        { }
    }

    public class ViewNotFoundException : KeyNotFoundException
    {
        public ViewNotFoundException(string message) : base(message) { }
    }

    public class InvalidViewException : ArgumentNullException
    {
        public InvalidViewException(string message) : base(message) { }
    }
    
    public class EntityDatabaseInitializationException : InvalidOperationException
    {
        public EntityDatabaseInitializationException(string message, Exception inner)
            : base(message, inner)
        { }
    }

    public class EntityNotFoundException : FileNotFoundException
    {
        public EntityNotFoundException(string message) : base(message) { }
    }
    
    public class ComponentDatabaseInitializationException : InvalidOperationException
    {
        public ComponentDatabaseInitializationException(string message, Exception inner)
            : base(message, inner)
        { }
    }
}
