using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;
using Common.Domain;

namespace Common.XamlHelpers
{
    [ValueConversion(typeof(ItemType), typeof(Brush))]
    public class TypeToBrushConverter : MarkupExtension, IMultiValueConverter
    {
        private static TypeToBrushConverter _converter;

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return _converter ?? (_converter = new TypeToBrushConverter());
        }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length != 2)
                throw new Exception("Plase specify multibinding bindings");
            
            if (!(values[0] is ItemType))
                throw new Exception("value should be an ItemType");

            if (!(values[1] is FrameworkElement))
                throw new Exception("parameter must be window");

            var type = (ItemType)values[0];
            var frameworkElement = (FrameworkElement)values[1];

            switch (type)
            {
                case ItemType.Property:
                    return frameworkElement.FindResource("PropertyBrush");
                case ItemType.Method:
                    return frameworkElement.FindResource("MethodBrush");
                case ItemType.Field:
                    return frameworkElement.FindResource("FieldBrush");
                case ItemType.Constructor:
                    return frameworkElement.FindResource("ConstructorBrush");
                case ItemType.Const:
                    return frameworkElement.FindResource("ConstBrush");
                case ItemType.Region:
                    return null;
                case ItemType.Class:
                    return frameworkElement.FindResource("ClassBrush");
                case ItemType.Namespace:
                    return frameworkElement.FindResource("NamespaceBrush");
                case ItemType.Interface:
                    return frameworkElement.FindResource("InterfaceBrush");
                case ItemType.Enum:
                    return frameworkElement.FindResource("EnumBrush");
                case ItemType.Struct:
                    return frameworkElement.FindResource("StructBrush");
                case ItemType.EnumMember:
                    return frameworkElement.FindResource("EnumMemberBrush");
                case ItemType.Event:
                    return frameworkElement.FindResource("EventMemberBrush");
                case ItemType.Delegate:
                    return frameworkElement.FindResource("DelegateBrush");
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}