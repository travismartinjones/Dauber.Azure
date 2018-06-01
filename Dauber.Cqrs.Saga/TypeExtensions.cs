using System;
using System.Linq;

namespace Dauber.Cqrs.Saga
{
    public static class TypeExtensions
    {
        public static bool DoesTypeImplementInterface(this Type type, Type @interface)
        {
            if (type.IsAbstract || type.IsInterface)
                return false;

            if (@interface.IsGenericType && type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == @interface))
                return true;

            return @interface.IsAssignableFrom(type);
        }
    }
}