using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Markup;

namespace FiveTwoFiveTwo.XAMLHelpers
{
    public abstract class BaseConverter : MarkupExtension
    {
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }
    [ValueConversion(typeof(object), typeof(string))]
    public class StringFormatConverter : BaseConverter, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
                          System.Globalization.CultureInfo culture)
        {
            string format = parameter as string;
            if (!string.IsNullOrEmpty(format))
            {
                return string.Format(culture, format, value);
            }
            else
            {
                return value.ToString();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter,
                        System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }
    [ValueConversion(typeof(double), typeof(double))]
    public class SubtractConverter : BaseConverter, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
                          System.Globalization.CultureInfo culture)
        {
            double param = System.Convert.ToDouble(parameter);
            return (double)value - param;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
                        System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }
    [ValueConversion(typeof(Decimal), typeof(Double))]
    public class D2PConverter : BaseConverter, IMultiValueConverter 
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Double foo = System.Convert.ToDouble(values[0]);
            Double bar = System.Convert.ToDouble(values[1]);
            if(foo > bar)
            {
                return 100.0d;
            }
            if(foo < 0 || bar < 0)
            {
                return 0.0d;
            }
            

            return ((foo / bar) * 100.0d);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    [ValueConversion(typeof(double), typeof(double))]
    public class DoubleDividerConverter : BaseConverter, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
                          System.Globalization.CultureInfo culture)
        {
            try
            {
                double doubleParam = System.Convert.ToDouble(parameter);
                double doubleValue = System.Convert.ToDouble(value);
                return doubleValue / doubleParam;
           
            }
            catch (Exception)
            {
                
                throw;
            }

        }

        public object ConvertBack(object value, Type targetType, object parameter,
                        System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }
}
