using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace 成就系统.Utilities
{
    //评分  ==>  评价
    public class ScoreToGradeConverter : IValueConverter
    {
        /// <summary>
        /// 将评分转换为等级字母的转换器
        /// </summary>
        /// <param name="value">成就分数</param>
        /// <param name="targetType">指示目标绑定类型</param>
        /// <param name="parameter">等级</param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal score)
            {
                if (score >= 9.0m) return "S";
                if (score >= 8.0m) return "A";
                if (score >= 7.0m) return "B";
                return "C";
            }
            return "C";
        }

        //反向转换，占位
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    //评分  ==>  等级颜色
    public class ScoreToGradeColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal score)
            {
                if (score >= 9.0m) return new SolidColorBrush(Color.FromRgb(255, 215, 0));   // S级金色
                if (score >= 8.0m) return new SolidColorBrush(Color.FromRgb(192, 192, 192)); // A级银色
                if (score >= 7.0m) return new SolidColorBrush(Color.FromRgb(205, 127, 50));  // B级铜色
                return new SolidColorBrush(Color.FromRgb(128, 128, 128));                  // C级灰色
            }
            return new SolidColorBrush(Colors.Gray);
        }

        //反向转换，占位
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
