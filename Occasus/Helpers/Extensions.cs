using System.Collections;
using System.ComponentModel;
using System.Reflection;

namespace Occasus.Helpers
{
    internal static class Extensions
    {
        internal static bool IsSimple(this Type type) => TypeDescriptor.GetConverter(type).CanConvertFrom(typeof(string));

        internal static bool IsEnumerable(this Type type) => type.GetInterfaces().Contains(typeof(IEnumerable)) && (type.GetGenericArguments().Length > 0 || type.IsArray);
        internal static bool IsDictionary(this Type type) => type.GetInterfaces().Contains(typeof(IDictionary)) && (type.GetGenericArguments().Length == 2 || type.GetInterfaces().Any(x => x.GetGenericArguments().Length == 2));

        internal static bool IsCollection(this Type type) => type.IsArray || type.GetGenericArguments().Length > 0 && (typeof(ICollection<>).MakeGenericType(type.GetGenericArguments()[0])).IsAssignableFrom(type); //typeof(ICollection).IsAssignableFrom(type);

        internal static Type CollectionType(this Type type) => type.NonNullableType() is Type ult ? ult.IsGenericType ? ult.GetGenericArguments()[0] :
                                                                                                                        ult.IsArray ? ult.GetElementType()! :
                                                                                                                                      ult :
                                                                                                    type;

        internal static Type NonNullableType(this Type type) => Nullable.GetUnderlyingType(type) ?? type;
        internal static Type? DictionaryValueType(this Type type) =>
            type.NonNullableType() is Type ult ?
                ult.IsDictionary() ?
                    ult.GetInterfaces()
                       .First(x => x.GUID == typeof(IDictionary<,>).GUID)
                       .GetGenericArguments()[1]
                    : null
                : null;

        internal static Type? DictionaryKeyType(this Type type) =>
            type.NonNullableType() is Type ult ?
                ult.IsDictionary() ?
                    ult.GetInterfaces()
                       .First(x => x.GUID == typeof(IDictionary<,>).GUID)
                       .GetGenericArguments()[0]
                    : null
                : null;

        internal static IEnumerable<PropertyInfo> GetOptionableProperties(this Type type) => type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                                                .Where(p => p.CanWrite //Is writeable
                                                                         && !p.PropertyType.IsAbstract //Isn't Abstract
                                                                         && (p.PropertyType.IsSimple() //Can be converted to from a string
                                                                          || (p.PropertyType.IsArray && p.PropertyType.GetElementType()!.IsSimple()) //Is an array of simples
                                                                          || p.PropertyType.CollectionType().IsSimple() //or the underlying (not nullable generic type) can be converted from a string
                                                                          || p.PropertyType.GetConstructor(Type.EmptyTypes) is not null)); //or it has a parameterless constructor

        internal static bool IsNullable(this Type type) => !type.IsValueType || Nullable.GetUnderlyingType(type) is not null;

    }
}
