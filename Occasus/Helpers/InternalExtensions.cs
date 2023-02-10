using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Occasus.Settings.Models;
using System.Collections;
using System.ComponentModel;
using System.Reflection;

namespace Occasus.Helpers;

internal static class InternalExtensions
{

    internal static Type CollectionType(this Type type) => type.NonNullableType() is Type ult ? ult.IsGenericType ? ult.GetGenericArguments()[0] :
                                                                                                                    ult.IsArray ? ult.GetElementType()! :
                                                                                                                                  ult :
                                                                                                type;


    internal static Type? DictionaryValueType(this Type type) =>
        type.NonNullableType() is Type ult ?
            ult.IsDictionary() ?
                ult.GetInterfaces()
                   .First(x => x.GUID == typeof(IDictionary<,>).GUID)
                   .GetGenericArguments()[1]
                : null
            : null;

    internal static IEnumerable<PropertyInfo> GetOptionableProperties(this Type type) => type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                                            .Where(p => p.CanWrite //Is writeable
                                                                     && !p.PropertyType.IsAbstract //Isn't Abstract
                                                                     && (p.PropertyType.IsSimple() //Can be converted to from a string
                                                                      || (p.PropertyType.IsArray && p.PropertyType.GetElementType()!.IsSimple()) //Is an array of simples
                                                                      || p.PropertyType.CollectionType().IsSimple() //or the underlying (not nullable generic type) can be converted from a string
                                                                      || p.PropertyType.GetConstructor(Type.EmptyTypes) is not null));






}

public static class PublicExtensions
{
    public static Type? DictionaryKeyType(this Type type) =>
        type.NonNullableType() is Type ult ?
            ult.IsDictionary() ?
                ult.GetInterfaces()
                   .First(x => x.GUID == typeof(IDictionary<,>).GUID)
                   .GetGenericArguments()[0]
                : null
            : null;

    public static bool IsCollection(this Type type) => type.IsArray || type.GetGenericArguments().Length > 0 && (typeof(ICollection<>).MakeGenericType(type.GetGenericArguments()[0])).IsAssignableFrom(type);
    public static bool IsDictionary(this Type type) => type.GetInterfaces().Contains(typeof(IDictionary)) && (type.GetGenericArguments().Length == 2 || type.GetInterfaces().Any(x => x.GetGenericArguments().Length == 2));
    public static bool IsEnumerable(this Type type) => type.GetInterfaces().Contains(typeof(IEnumerable)) && (type.GetGenericArguments().Length > 0 || type.IsArray);
    public static bool IsNullable(this Type type) => !type.IsValueType || Nullable.GetUnderlyingType(type) is not null;
    public static bool IsSimple(this Type type) => TypeDescriptor.GetConverter(type).CanConvertFrom(typeof(string));
    public static Type NonNullableType(this Type type) => Nullable.GetUnderlyingType(type) ?? type;
    //or it has a parameterless constructor
    public static List<SettingStorage> ToSettingItems(this object obj, string path, ILogger? logger)
        => ToSettingItems(obj, new Stack<string>(new List<string>() {  path }), logger);
    public static List<SettingStorage> ToSettingItems(this object obj, Stack<string> path, ILogger? logger)
    {

        var results = new List<SettingStorage>();

        if (obj is null)
        {
            results.Add(new(ConfigurationPath.Combine(path.Reverse()), null));

            return results;
        }

        var type = obj.GetType();

        if (type.IsSimple())
        {
            results.Add(new(ConfigurationPath.Combine(path.Reverse()), obj as string));
            return results;
        }

        if (type.IsDictionary())
        {
            foreach (DictionaryEntry item in (IDictionary)obj)
            {
                path.Push(item.Key.ToString()!);
                if (item.Value?.GetType().IsSimple() ?? true)
                {
                    results.Add(new(ConfigurationPath.Combine(path.Reverse()), item.Value is DateTime dt ? dt.ToString("s") : item.Value?.ToString()));
                }
                else
                {
                    var subitems = ToSettingItems(item, path, logger);
                    results.AddRange(subitems);
                }
                path.Pop();
            }

            return results;
        }

        if (type.IsEnumerable())
        {
            var i = 0;

            foreach (var item in (IEnumerable)obj)
            {
                path.Push(i.ToString());
                if (item.GetType().IsSimple())
                {
                    if (item is not null)
                    {
                        results.Add(new(ConfigurationPath.Combine(path.Reverse()), item is DateTime dt ? dt.ToString("s") : item.ToString()));
                    }
                }
                else
                {

                    var subitems = ToSettingItems(item, path, logger);
                    results.AddRange(subitems);
                }
                path.Pop();


                i++;
            }

            return results;
        }

        foreach (var prop in type.GetOptionableProperties())
        {
            try
            {
                if (obj is not DictionaryEntry || prop.Name != "Value")
                {
                    path.Push(prop.Name);
                }

                if (prop.GetValue(obj) is object value)
                {

                    if (prop.PropertyType.IsSimple())
                    {
                        results.Add(new(ConfigurationPath.Combine(path.Reverse()), value is DateTime dt ? dt.ToString("s") : value.ToString()));
                    }
                    else
                    {
                        results.AddRange(ToSettingItems(value, path, logger));
                    }
                }
                else
                {
                    results.Add(new(ConfigurationPath.Combine(path.Reverse()), null)); // { MarkedForDeletion = true });
                }

                if (obj is not DictionaryEntry || prop.Name != "Value")
                {
                    path.Pop();
                }
            }
            catch (TargetParameterCountException)
            {
                if (logger is not null)
                {
                    logger.LogWarning("Unable to gather this value from {type}", prop.Name);
                }
            }
        }

        return results;

    }
}
