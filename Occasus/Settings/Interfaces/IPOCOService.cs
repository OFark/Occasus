using Occasus.Settings.Models;
using System.Reflection;

namespace Occasus.Settings.Interfaces;

public interface IPOCOService
{
    object? AddValue(object? POCO, SettingProperty SettingProperty);
    object? ChangeDictionaryItem(object? POCO, PropertyInfo propertyInfo, object? value, object key);
    object? ChangeListItem(object? POCO, PropertyInfo propertyInfo, object? value, int index);
    object? RemoveValue(object? POCO, SettingProperty SettingProperty, int index);
    object? RemoveValueWithKey(object? POCO, PropertyInfo propertyInfo, object key);
    void SetValue(object POCO, PropertyInfo propertyInfo, object? value);
}
