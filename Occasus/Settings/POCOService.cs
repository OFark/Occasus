using LanguageExt;
using Occasus.Helpers;
using Occasus.Settings.Interfaces;
using Occasus.Settings.Models;
using System.Collections;
using System.Reflection;

namespace Occasus.Settings
{
    internal class POCOService : IPOCOService
    {

        public Option<object> AddValue(object? POCO, SettingProperty SettingProperty)
        {

            if (SettingProperty.NotNullType.IsCollection())
            {
                var length = SettingProperty.PropertyInfo.GetIndexParameters().Length;
                var list = SettingProperty.PropertyInfo.GetValue(POCO, Enumerable.Range(0, length).Cast<object>().ToArray());
                var gType = SettingProperty.NotNullType.CollectionType();

                list ??= SettingProperty.NotNullType.IsArray ? Array.CreateInstance(gType, 0) : Activator.CreateInstance(SettingProperty.NotNullType);

                if (list is IEnumerable coll)
                {
                    return (SettingProperty.NotNullType.IsArray ?
                        AddToArray(gType, coll, NewValueorNewObject(SettingProperty.NewValue, gType)) :
                        AddToList(gType, coll, NewValueorNewObject(SettingProperty.NewValue, gType))) as object;
                }

            }

            if (SettingProperty.NotNullType.IsDictionary())
            {
                var length = SettingProperty.PropertyInfo.GetIndexParameters().Length;
                var list = SettingProperty.PropertyInfo.GetValue(POCO, Enumerable.Range(0, length).Cast<object>().ToArray());
                var gType = SettingProperty.NotNullType.DictionaryValueType();

                if (gType is not null)
                {
                    list ??= Activator.CreateInstance(SettingProperty.PropertyInfo.PropertyType);

                    if (list is IDictionary dict && (SettingProperty.NewValue is null || dict[SettingProperty.NewValue] is null))
                    {
                        dict.Add(SettingProperty.NewValue ?? $"Item {dict.Count + 1}", gType == typeof(string) ? null : Activator.CreateInstance(gType));
                        return dict as object;
                    }
                }
            }

            return Option<object>.None;

        }

        public Option<object> ChangeDictionaryItem(object? POCO, PropertyInfo propertyInfo, object? value, object key)
        {
            if ((propertyInfo.GetValue(POCO, null) ?? Activator.CreateInstance(propertyInfo.PropertyType)) is IDictionary d)
            {
                d[key] = value;

                return d as object;
            }

            return Option<object>.None;
        }

        public Option<object> ChangeListItem(object? POCO, PropertyInfo propertyInfo, object? value, int index)
        {

            var list = propertyInfo.GetValue(POCO, null) ?? Activator.CreateInstance(propertyInfo.PropertyType);

            if (list is IList l)
            {
                if (l.Count > index)
                {
                    l[index] = value;
                }
                else
                {
                    l.Insert(index, value);
                }
                return l as object;
            }
            else if (list is IEnumerable e)
            {
                var newlist = Activator.CreateInstance(propertyInfo.PropertyType);
                if (newlist is not null)
                {
                    var gType = propertyInfo.PropertyType.GetGenericArguments()[0];
                    if (newlist.GetType().GetMethod("Add", new Type[] { gType }) is MethodInfo method) //Find the right method
                    {

                        var i = 0;
                        IEnumerator enumerator = e.GetEnumerator();
                        {
                            while (enumerator.MoveNext())
                            {
                                method.Invoke(newlist, new object?[] { index == i ? value : enumerator.Current });
                                i++;
                            }

                        }

                        enumerator.Reset();

                        return newlist;
                    }
                }
            }

            return Option<object>.None;

        }

        public Option<object> RemoveValue(object? POCO, SettingProperty SettingProperty, int index)
        {
            var propertyInfo = SettingProperty.PropertyInfo;
            if (SettingProperty.NotNullType.IsCollection())
            {
                var length = propertyInfo.GetIndexParameters().Length;
                var list = propertyInfo.GetValue(POCO, Enumerable.Range(0, length).Cast<object>().ToArray());
                var gType = SettingProperty.NotNullType.CollectionType();

                if (list is IList l && l.Count > index && !l.IsFixedSize)
                {
                    l.RemoveAt(index);

                    return l as object;
                }

                if (list is Array arr)
                {

                    return RemoveFromArray(gType, arr, index) as object;
                }

                if (list is IEnumerable e)
                {

                    return RemoveFromEnumerable(propertyInfo.PropertyType, gType, e, index) as object;
                }
            }


            return Option<object>.None;
        }

        public Option<object> RemoveValueWithKey(object? POCO, PropertyInfo propertyInfo, object key)
        {
            var dict = propertyInfo.GetValue(POCO, null);

            if (dict is IDictionary d)
            {
                d.Remove(key);
                return d as object;
            }

            return Option<object>.None;
        }

        public void SetValue(object? POCO, PropertyInfo propertyInfo, object? value)
        {
            propertyInfo.SetValue(POCO, value);
        }

        private static IEnumerable AddToArray(Type gType, IEnumerable arr, object? newValue)
        {
            if (arr is Array array)
            {
                var newArray = Array.CreateInstance(gType, array.Length + 1);
                array.CopyTo(newArray, 0);
                newArray.SetValue(NewValueorNewObject(newValue, gType), newArray.Length - 1);

                return newArray;
            }

            throw new Exception($"{nameof(arr)} is not an Array");
        }

        private static IEnumerable AddToList(Type gType, IEnumerable list, object? newValue)
        {
            var method = list.GetType().GetMethod("Add", new Type[] { gType }); //Find the right method
            method?.Invoke(list, new object?[] { NewValueorNewObject(newValue, gType) });

            return list;
        }

        private static object? NewValueorNewObject(object? newValue, Type objectType) => newValue ?? (objectType == typeof(string) ? newValue?.ToString() ?? string.Empty : Activator.CreateInstance(objectType));
        private static ICollection RemoveFromArray(Type gType, Array arr, int index)
        {
            var newArray = Array.CreateInstance(gType, arr.Length - 1);

            if (newArray.Length == 0)
            {
                return newArray;
            }

            if (index > 0)
            {
                Array.Copy(arr, 0, newArray, 0, index);
            }

            if (index < arr.Length)
            {
                Array.Copy(arr, index + 1, newArray, index, arr.Length - (index + 1));
            }

            return newArray;
        }

        private static IEnumerable RemoveFromEnumerable(Type enumerableType, Type itemType, IEnumerable list, int index)
        {
            var newlist = Activator.CreateInstance(enumerableType) as IEnumerable;

            if (newlist is not null)
            {

                var method = newlist.GetType().GetMethod("Add", new Type[] { itemType }); //Find the right method

                var i = 0;
                IEnumerator enumerator = list.GetEnumerator();
                {
                    while (enumerator.MoveNext())
                    {
                        if (i != index)
                        {
                            method?.Invoke(newlist, new object[] { enumerator.Current });
                        }
                        i++;
                    }

                }
                enumerator.Reset();

                return newlist;
            }
            throw new Exception($"Failed to create a new IEnumerable from type ({enumerableType.Name})");
        }
    }
}
