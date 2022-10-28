using MudBlazor;
using Occasus.Attributes;
using Occasus.Helpers;
using System.Reflection;

namespace Occasus.Settings.Models
{
    public class SettingProperty
    {
        public SettingProperty(PropertyInfo propertyInfo)
        {
            PropertyInfo = propertyInfo;
        }
        public InputAttribute InputAttribute =>
            Attribute.GetCustomAttribute(PropertyInfo, typeof(InputAttribute), false) as InputAttribute ??
            new(ValueType.CollectionType().NonNullableType() == typeof(DateTime) ? InputType.DateTimeLocal : InputType.Text);

        public object? NewValue { get; set; }
        public Type NotNullType => Nullable.GetUnderlyingType(PropertyInfo.PropertyType) ?? PropertyInfo.PropertyType;
        public PropertyInfo PropertyInfo { get; init; }
        public Type Type => PropertyInfo.PropertyType;
        public Type ValueType => NotNullType.IsDictionary() ? NotNullType.DictionaryValueType()! :
                                   NotNullType.IsArray ? NotNullType.GetElementType()! :
                                                                NotNullType.CollectionType();


    }
}
