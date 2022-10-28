using LanguageExt;
using Occasus.Settings.Models;
using System.Reflection;

namespace Occasus.Settings.Interfaces;

public interface IPOCOService
{
    Option<object> AddValue(object? POCO, SettingProperty SettingProperty);
    Option<object> ChangeDictionaryItem(object? POCO, PropertyInfo propertyInfo, object? value, object key);
    Option<object> ChangeListItem(object? POCO, PropertyInfo propertyInfo, object? value, int index);
    Option<object> RemoveValue(object? POCO, SettingProperty SettingProperty, int index);
    Option<object> RemoveValueWithKey(object? POCO, PropertyInfo propertyInfo, object key);
    void SetValue(object POCO, PropertyInfo propertyInfo, object? value);
}
