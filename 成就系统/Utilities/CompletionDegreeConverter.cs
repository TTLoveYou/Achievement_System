using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace 成就系统.Utilities
{
    ////完成度 ==>  百分比
    public class CompletionDegreeToStyleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string degree)
                return "0%";

            // 根据参数决定返回类型
            return degree switch
            {
                "" => GetResult(0, parameter),
                "+" => GetResult(25, parameter),
                "++" => GetResult(50, parameter),
                "+++" => GetResult(75, parameter),
                "++++" => GetResult(100, parameter),
                _ => GetResult(0, parameter)
            };
        }

        //数值/字符串类型匹配转换
        private static object GetResult(int percentage, object param)
        {
            return param is string s && s == "Value"
                ? percentage
                : $"{percentage}%";
        }

        //百分比  ==>  完成度
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    //完成度分级颜色转换
    public class CompletionDegreeToColorConverter : IValueConverter
    {
        // 静态画刷缓存
        private static readonly SolidColorBrush _redBrush = new(Color.FromRgb(0xF4, 0x43, 0x36));       //红色      
        private static readonly SolidColorBrush _orangeBrush = new(Color.FromRgb(0xFF, 0x98, 0x00));    //橙色
        private static readonly SolidColorBrush _yellowBrush = new(Color.FromRgb(0xFF, 0xEB, 0x3B));    //黄色
        private static readonly SolidColorBrush _blueBrush = new(Color.FromRgb(0x21, 0x96, 0xF3));      //蓝色
        private static readonly SolidColorBrush _greenBrush = new(Color.FromRgb(0x4C, 0xAF, 0x50));     //绿色
        private static readonly SolidColorBrush _grayBrush = new(Color.FromRgb(0x99, 0x99, 0x99));      //灰色

        //完成度 ==>  百分比
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string degree)
            {
                // 基于原始值转换为颜色
                return degree switch
                {
                    "" => _redBrush,
                    "+" => _orangeBrush,
                    "++" => _yellowBrush,
                    "+++" => _blueBrush,
                    "++++" => _greenBrush,
                    _ => _grayBrush // 默认颜色
                };
            }
            return _grayBrush;
        }

        //占位用
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}