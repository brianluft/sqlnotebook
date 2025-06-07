using System;
using System.ComponentModel;
using System.Globalization;

namespace SqlNotebook;

// adapted from http://stackoverflow.com/a/7422801
public sealed class DescriptionEnumConverter : EnumConverter
{
    private Type _enumType;

    public DescriptionEnumConverter(Type type)
        : base(type)
    {
        _enumType = type;
    }

    public override bool CanConvertTo(ITypeDescriptorContext context, Type destType) => destType == typeof(string);

    public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destType)
    {
        var fi = _enumType.GetField(Enum.GetName(_enumType, value));
        var dna = (DescriptionAttribute)Attribute.GetCustomAttribute(fi, typeof(DescriptionAttribute));
        return dna?.Description ?? value.ToString();
    }

    public override bool CanConvertFrom(ITypeDescriptorContext context, Type srcType) => srcType == typeof(string);

    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
    {
        foreach (var fi in _enumType.GetFields())
        {
            var dna = (DescriptionAttribute)Attribute.GetCustomAttribute(fi, typeof(DescriptionAttribute));
            if (dna != null && (string)value == dna.Description)
            {
                return Enum.Parse(_enumType, fi.Name);
            }
        }
        return Enum.Parse(_enumType, (string)value);
    }
}
