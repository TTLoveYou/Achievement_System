using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using 成就系统.Services;

namespace 成就系统.UI.Main
{
    /// <summary>
    /// StatisticsPage.xaml 的交互逻辑
    /// </summary>
    public partial class StatisticsPage : Page
    {
        private readonly AchievementService _achievementService = new();
        private readonly string _username;
        private Button _selectedButton;

        // 统计状态枚举
        //类别状态枚举
        private enum CategoryStatsState
        {
            BigCategory,
            SmallCategory
        }

        //日期状态枚举
        private enum DateStatsState
        {
            Year,
            Month
        }

        // 当前统计状态
        private CategoryStatsState _categoryStatsState = CategoryStatsState.BigCategory;    //首次大类统计
        private DateStatsState _dateStatsState = DateStatsState.Year;                       //首次年份统计

        public StatisticsPage(string username)
        {
            InitializeComponent();
            _username = username;
            InitializeButtonStyles();
            // 更新按钮初始文本
            UpdateCategoryButtonText();
            UpdateDateButtonText();
            // 更新问候语
            UpdateGreetingText();
            // 默认选择种类统计，确保加载大类数据
            _categoryStatsState = CategoryStatsState.BigCategory;                        //首次大类统计
            UpdateButtonStyle(CategoryStatsBtn);
            // 异步加载统计数据
            LoadCategoryStatsAsync();
        }

        /// <summary>
        /// 更新问候语文本
        /// </summary>
        private async void UpdateGreetingText()
        {
            try
            {
                // 获取当前日期
                DateTime today = DateTime.Now;
                string dateString = $"{today.Year}年{today.Month}月{today.Day}日";

                // 获取用户成就数量
                var achievements = await _achievementService.GetAllAchievementCardsAsync(_username);
                int achievementCount = achievements.Count;

                // 更新问候语文本
                GreetingText.Text = $"{_username}，您好！今天是{dateString}，截至目前，您的成就已达到{achievementCount}项！";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"更新问候语失败: {ex.Message}");
                // 如果获取数据失败，显示默认问候语
                DateTime today = DateTime.Now;
                string dateString = $"{today.Year}年{today.Month}月{today.Day}日";
                GreetingText.Text = $"{_username}，您好！今天是{dateString}，欢迎使用统计站……";
            }
        }

        /// <summary>
        /// 异步加载种类统计数据
        /// </summary>
        private async void LoadCategoryStatsAsync()
        {
            await LoadCategoryStats();
        }

        /// <summary>
        /// 初始化按钮样式
        /// </summary>
        private void InitializeButtonStyles()
        {
            // 设置按钮的默认样式
            UpdateButtonStyle(null);
        }

        /// <summary>
        /// 更新按钮样式
        /// </summary>
        /// <param name="selectedBtn">选中的按钮</param>
        private void UpdateButtonStyle(Button? selectedBtn)
        {
            // 重置所有按钮样式
            ResetButtonStyle(CategoryStatsBtn);
            ResetButtonStyle(ScoreStatsBtn);
            ResetButtonStyle(CompletionStatsBtn);
            ResetButtonStyle(DateStatsBtn);

            // 设置选中按钮的样式
            if (selectedBtn != null)
            {
                selectedBtn.Background = (Brush)FindResource("PrimaryColor");
                selectedBtn.Foreground = Brushes.White;
                selectedBtn.BorderBrush = (Brush)FindResource("PrimaryColor");
                _selectedButton = selectedBtn;
            }
        }

        /// <summary>
        /// 重置按钮样式
        /// </summary>
        /// <param name="button">要重置的按钮</param>
        private void ResetButtonStyle(Button button)
        {
            button.Background = (Brush)FindResource("CardBackground");
            button.Foreground = (Brush)FindResource("TextForeground");
            button.BorderBrush = (Brush)FindResource("BorderColor");
        }

        /// <summary>
        /// 种类统计按钮点击事件
        /// </summary>
        private async void CategoryStatsBtn_Click(object sender, RoutedEventArgs e)
        {
            Button? button = sender as Button;

            // 如果点击的是当前选中的按钮，则切换状态
            if (button == _selectedButton)
            {
                // 切换状态
                _categoryStatsState = _categoryStatsState == CategoryStatsState.BigCategory
                    ? CategoryStatsState.SmallCategory
                    : CategoryStatsState.BigCategory;
            }
            else
            {
                // 如果点击的是新按钮，重置为默认状态
                _categoryStatsState = CategoryStatsState.BigCategory;
            }

            // 更新按钮样式和文本
            UpdateButtonStyle(button);
            UpdateCategoryButtonText();

            // 根据当前状态加载统计数据
            if (_categoryStatsState == CategoryStatsState.BigCategory)
            {
                await LoadCategoryStats();
            }
            else
            {
                await LoadSmallCategoryStats();
            }
        }

        /// <summary>
        /// 更新种类按钮文本
        /// </summary>
        private void UpdateCategoryButtonText()
        {
            CategoryStatsBtn.Content = _categoryStatsState == CategoryStatsState.BigCategory
                ? "种类(-︿-)"
                : "种类(◉ω◉)";
        }

        /// <summary>
        /// 评分统计按钮点击事件
        /// </summary>
        private async void ScoreStatsBtn_Click(object sender, RoutedEventArgs e)
        {
            UpdateButtonStyle(sender as Button);
            // 重置种类和日期按钮的状态
            _categoryStatsState = CategoryStatsState.BigCategory;
            _dateStatsState = DateStatsState.Year;
            // 更新按钮文本
            UpdateCategoryButtonText();
            UpdateDateButtonText();
            await LoadScoreStats();
        }

        /// <summary>
        /// 完成度统计按钮点击事件
        /// </summary>
        private async void CompletionStatsBtn_Click(object sender, RoutedEventArgs e)
        {
            UpdateButtonStyle(sender as Button);
            // 重置种类和日期按钮的状态
            _categoryStatsState = CategoryStatsState.BigCategory;
            _dateStatsState = DateStatsState.Year;
            // 更新按钮文本
            UpdateCategoryButtonText();
            UpdateDateButtonText();
            await LoadCompletionStats();
        }

        /// <summary>
        /// 日期统计按钮点击事件
        /// </summary>
        private async void DateStatsBtn_Click(object sender, RoutedEventArgs e)
        {
            Button? button = sender as Button;

            // 如果点击的是当前选中的按钮，则切换状态
            if (button == _selectedButton)
            {
                // 切换状态
                _dateStatsState = _dateStatsState == DateStatsState.Year
                    ? DateStatsState.Month
                    : DateStatsState.Year;
            }
            else
            {
                // 如果点击的是新按钮，重置为默认状态
                _dateStatsState = DateStatsState.Year;
            }

            // 更新按钮样式和文本
            UpdateButtonStyle(button);
            UpdateDateButtonText();

            // 根据当前状态加载统计数据
            if (_dateStatsState == DateStatsState.Year)
            {
                await LoadDateStats();
            }
            else
            {
                await LoadMonthStats();
            }
        }

        /// <summary>
        /// 更新日期按钮文本
        /// </summary>
        private void UpdateDateButtonText()
        {
            DateStatsBtn.Content = _dateStatsState == DateStatsState.Year
                ? "日期(年)"
                : "日期(月)";
        }

        /// <summary>
        /// 加载种类统计
        /// </summary>
        private async System.Threading.Tasks.Task LoadCategoryStats()
        {
            try
            {
                // 获取所有成就
                var achievements = await _achievementService.GetAllAchievementCardsAsync(_username);

                // 统计每个大类别下的成就数量
                Dictionary<string, int> categoryStats = [];

                foreach (var achievement in achievements)
                {
                    // 获取大类别名称
                    var (bigCategory, _) = await _achievementService.GetCategoryNamesAsync(achievement.CategoryId ?? 0);
                    if (!string.IsNullOrEmpty(bigCategory))
                    {
                        if (categoryStats.TryGetValue(bigCategory, out int value))
                        {
                            categoryStats[bigCategory] = ++value;
                        }
                        else
                        {
                            categoryStats[bigCategory] = 1;
                        }
                    }
                }

                // 更新图表
                UpdateChart("成就大类统计", categoryStats);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载种类统计失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 加载评分统计
        /// </summary>
        private async System.Threading.Tasks.Task LoadScoreStats()
        {
            try
            {
                // 获取所有成就
                var achievements = await _achievementService.GetAllAchievementCardsAsync(_username);

                // 统计每个评分区间的成就数量
                Dictionary<string, int> scoreStats = new()
                {
                    { "0.0-0.9", 0 },
                    { "1.0-1.9", 0 },
                    { "2.0-2.9", 0 },
                    { "3.0-3.9", 0 },
                    { "4.0-4.9", 0 },
                    { "5.0-5.9", 0 },
                    { "6.0-6.9", 0 },
                    { "7.0-7.9", 0 },
                    { "8.0-8.9", 0 },
                    { "9.0-9.9", 0 }
                };

                foreach (var achievement in achievements)
                {
                    decimal score = achievement.Score;
                    int scoreIndex = Math.Min((int)Math.Floor(score), 9);
                    string key = $"{scoreIndex}.0-{scoreIndex}.9";
                    if (scoreStats.TryGetValue(key, out int value))
                    {
                        scoreStats[key] = ++value;
                    }
                }

                // 更新图表
                UpdateChart("成就评分统计", scoreStats);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载评分统计失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 加载完成度统计
        /// </summary>
        private async System.Threading.Tasks.Task LoadCompletionStats()
        {
            try
            {
                // 获取所有成就
                var achievements = await _achievementService.GetAllAchievementCardsAsync(_username);

                // 统计每个完成度区间的成就数量
                Dictionary<string, int> completionStats = new()
                {
                    { "0%", 0 },
                    { "25%", 0 },
                    { "50%", 0 },
                    { "75%", 0 },
                    { "100%", 0 }
                };

                foreach (var achievement in achievements)
                {
                    // 解析完成度
                    int completionDegree = 0;
                    if (!string.IsNullOrEmpty(achievement.CompletionDegree))
                    {
                        string completionStr = achievement.CompletionDegree;
                        if (completionStr.Contains('+'))
                        {
                            // 计算+的数量
                            int plusCount = completionStr.Count(c => c == '+');
                            completionDegree = Math.Min(plusCount * 25, 100);
                        }
                        else
                        {
                            // 尝试解析数字完成度
                            _ = int.TryParse(completionStr.TrimEnd('%'), out completionDegree);
                        }
                    }

                    if (completionDegree == 0)
                        completionStats["0%"]++;
                    else if (completionDegree == 25)
                        completionStats["25%"]++;
                    else if (completionDegree == 50)
                        completionStats["50%"]++;
                    else if (completionDegree == 75)
                        completionStats["75%"]++;
                    else
                        completionStats["100%"]++;
                }

                // 更新图表
                UpdateChart("成就完成度统计", completionStats);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载完成度统计失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 加载日期统计
        /// </summary>
        private async System.Threading.Tasks.Task LoadDateStats()
        {
            try
            {
                // 获取所有成就
                var achievements = await _achievementService.GetAllAchievementCardsAsync(_username);

                // 统计每年的成就数量
                Dictionary<string, int> dateStats = [];

                foreach (var achievement in achievements)
                {
                    string year = achievement.AchievementDate.Year.ToString();
                    if (dateStats.TryGetValue(year, out int value))
                    {
                        dateStats[year] = ++value;
                    }
                    else
                    {
                        dateStats[year] = 1;
                    }
                }

                // 对日期进行排序
                var sortedDateStats = dateStats.OrderBy(kv => kv.Key).ToDictionary(kv => kv.Key, kv => kv.Value);

                // 更新图表
                UpdateChart("成就年份统计", sortedDateStats);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载日期统计失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 加载小类统计
        /// </summary>
        private async System.Threading.Tasks.Task LoadSmallCategoryStats()
        {
            try
            {
                // 重置大类颜色映射
                _bigCategoryColorMap.Clear();

                // 获取所有成就
                var achievements = await _achievementService.GetAllAchievementCardsAsync(_username);

                // 统计每个小类的成就数量
                Dictionary<string, int> smallCategoryStats = [];
                // 存储小类到大类的映射
                Dictionary<string, string> smallToBigCategoryMap = [];

                foreach (var achievement in achievements)
                {
                    // 获取大类和小类名称
                    var (bigCategory, smallCategory) = await _achievementService.GetCategoryNamesAsync(achievement.CategoryId ?? 0);
                    if (!string.IsNullOrEmpty(smallCategory))
                    {
                        if (smallCategoryStats.TryGetValue(smallCategory, out int value))
                        {
                            smallCategoryStats[smallCategory] = ++value;
                        }
                        else
                        {
                            smallCategoryStats[smallCategory] = 1;
                            // 存储小类到大类的映射
                            if (!string.IsNullOrEmpty(bigCategory))
                            {
                                smallToBigCategoryMap[smallCategory] = bigCategory;
                            }
                        }
                    }
                }

                // 按大类对小类进行分组并排序
                // 1. 首先按大类对小类进行分组
                var smallCategoriesByBigCategory = new Dictionary<string, List<string>>();
                foreach (var smallCategory in smallCategoryStats.Keys)
                {
                    if (smallToBigCategoryMap.TryGetValue(smallCategory, out string bigCategory))
                    {
                        if (!smallCategoriesByBigCategory.TryGetValue(bigCategory, out List<string>? value))
                        {
                            value = [];
                            smallCategoriesByBigCategory[bigCategory] = value;
                        }

                        value.Add(smallCategory);
                    }
                }

                // 2. 对大类进行排序
                var sortedBigCategories = smallCategoriesByBigCategory.Keys.OrderBy(key => key).ToList();

                // 3. 对每个大类内部的小类进行排序，并构建最终的排序结果
                var sortedSmallCategoryStats = new Dictionary<string, int>();
                foreach (var bigCategory in sortedBigCategories)
                {
                    var smallCategories = smallCategoriesByBigCategory[bigCategory].OrderBy(smallCategory => smallCategory);
                    foreach (var smallCategory in smallCategories)
                    {
                        sortedSmallCategoryStats[smallCategory] = smallCategoryStats[smallCategory];
                    }
                }

                // 更新图表，传递小类到大类的映射
                UpdateSmallCategoryChart("成就小类统计", sortedSmallCategoryStats, smallToBigCategoryMap);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载小类统计失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 加载月份统计
        /// </summary>
        private async System.Threading.Tasks.Task LoadMonthStats()
        {
            try
            {
                // 获取所有成就
                var achievements = await _achievementService.GetAllAchievementCardsAsync(_username);

                // 初始化月份统计字典（1-12月）
                Dictionary<string, int> monthStats = new()
                {
                    { "1月", 0 },
                    { "2月", 0 },
                    { "3月", 0 },
                    { "4月", 0 },
                    { "5月", 0 },
                    { "6月", 0 },
                    { "7月", 0 },
                    { "8月", 0 },
                    { "9月", 0 },
                    { "10月", 0 },
                    { "11月", 0 },
                    { "12月", 0 }
                };

                foreach (var achievement in achievements)
                {
                    int month = achievement.AchievementDate.Month;
                    string monthKey = $"{month}月";
                    if (monthStats.TryGetValue(monthKey, out int value))
                    {
                        monthStats[monthKey] = ++value;
                    }
                }

                // 更新图表
                UpdateChart("成就月份统计", monthStats);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载月份统计失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 图表数据项
        /// </summary>
        private class ChartItem
        {
            public string? Label { get; set; }
            public int Value { get; set; }
            public double BarHeight { get; set; }
            public double XPosition { get; set; }
            public string? Color { get; set; }
        }

        // 颜色库
        private readonly List<string> _colorLibrary =
        [
            "#4A90E2",    // 蓝色
            "#50E3C2",    // 青色
            "#F5A623",    // 橙色
            "#D0021B",    // 红色
            "#9013FE",    // 紫色
            "#7ED321",    // 绿色
            "#B8E986",    // 浅绿色
            "#BD10E0",    // 粉色
            "#417505",    // 深绿色
            "#5978F3",    // 亮蓝色
            "#F8E71C",    // 黄色
            "#4A4A4A"     // 深灰色
        ];

        // 大类颜色映射
        private readonly Dictionary<string, string> _bigCategoryColorMap = [];

        /// <summary>
        /// 获取大类对应的颜色
        /// </summary>
        /// <param name="bigCategory">大类名称</param>
        /// <returns>颜色字符串</returns>
        private string GetColorForBigCategory(string bigCategory)
        {
            // 如果大类已经有分配的颜色，直接返回
            if (_bigCategoryColorMap.TryGetValue(bigCategory, out string color))
            {
                return color;
            }

            // 如果没有分配颜色，从颜色库中循环获取
            int colorIndex = _bigCategoryColorMap.Count % _colorLibrary.Count;
            string newColor = _colorLibrary[colorIndex];

            // 存储大类到颜色的映射
            _bigCategoryColorMap[bigCategory] = newColor;

            return newColor;
        }

        /// <summary>
        /// 更新小类图表
        /// </summary>
        /// <param name="title">图表标题</param>
        /// <param name="data">统计数据</param>
        /// <param name="smallToBigCategoryMap">小类到大类的映射</param>
        private void UpdateSmallCategoryChart(string title, Dictionary<string, int> data, Dictionary<string, string> smallToBigCategoryMap)
        {
            // 设置图表标题
            ChartTitle.Text = title;

            // 计算最大值
            int maxValue = 0;
            foreach (var item in data)
            {
                if (item.Value > maxValue)
                {
                    maxValue = item.Value;
                }
            }

            // 生成图表数据项
            List<ChartItem> chartItems = [];
            int index = 0;
            int itemWidth = 50;
            int startX = 10;

            foreach (var item in data)
            {
                // 限制条形图的最大高度，确保值标签能够显示
                double maxBarHeight = 220;
                double barHeight = maxValue > 0 ? Math.Min((double)item.Value / maxValue * maxBarHeight, maxBarHeight) : 0;
                double xPosition = startX + index * itemWidth;

                // 获取小类对应的大类
                string bigCategory = "其他";
                if (smallToBigCategoryMap.TryGetValue(item.Key, out string foundBigCategory))
                {
                    bigCategory = foundBigCategory;
                }

                // 获取大类对应的颜色
                string color = GetColorForBigCategory(bigCategory);

                chartItems.Add(new ChartItem
                {
                    Label = item.Key,
                    Value = item.Value,
                    BarHeight = barHeight,
                    XPosition = xPosition,
                    Color = color
                });

                index++;
            }

            // 计算图表所需的宽度
            int requiredWidth = startX + data.Count * itemWidth + 100; // 增加额外空间，确保x轴线超过条形图
            int minWidth = 800;
            int chartWidth = Math.Max(requiredWidth, minWidth);

            // 更新图表容器和条形图的宽度
            ChartContainer.Width = chartWidth;
            BarChart.Width = chartWidth - 60;

            // 更新X轴线的长度
            if (FindName("XAxisLine") is System.Windows.Shapes.Line xAxisLine)
            {
                xAxisLine.X2 = chartWidth - 40;
            }

            // 绑定数据
            BarChart.ItemsSource = chartItems;
        }

        /// <summary>
        /// 更新图表
        /// </summary>
        /// <param name="title">图表标题</param>
        /// <param name="data">统计数据</param>
        private void UpdateChart(string title, Dictionary<string, int> data)
        {
            // 设置图表标题
            ChartTitle.Text = title;

            // 计算最大值
            int maxValue = 0;
            foreach (var item in data)
            {
                if (item.Value > maxValue)
                {
                    maxValue = item.Value;
                }
            }

            // 生成图表数据项
            List<ChartItem> chartItems = [];
            int index = 0;
            int itemWidth = 50;
            int startX = 10;

            foreach (var item in data)
            {
                // 限制条形图的最大高度，确保值标签能够显示
                double maxBarHeight = 220;
                double barHeight = maxValue > 0 ? Math.Min((double)item.Value / maxValue * maxBarHeight, maxBarHeight) : 0;
                double xPosition = startX + index * itemWidth;

                // 为每个项分配颜色
                int colorIndex = index % _colorLibrary.Count;
                string color = _colorLibrary[colorIndex];

                chartItems.Add(new ChartItem
                {
                    Label = item.Key,
                    Value = item.Value,
                    BarHeight = barHeight,
                    XPosition = xPosition,
                    Color = color
                });

                index++;
            }

            // 计算图表所需的宽度
            int requiredWidth = startX + data.Count * itemWidth + 100; // 增加额外空间，确保x轴线超过条形图
            int minWidth = 800;
            int chartWidth = Math.Max(requiredWidth, minWidth);

            // 更新图表容器和条形图的宽度
            ChartContainer.Width = chartWidth;
            BarChart.Width = chartWidth - 60;

            // 更新X轴线的长度
            if (FindName("XAxisLine") is System.Windows.Shapes.Line xAxisLine)
            {
                xAxisLine.X2 = chartWidth - 40;
            }

            // 绑定数据
            BarChart.ItemsSource = chartItems;
        }
    }

    /// <summary>
    /// 加法转换器
    /// </summary>
    public class AddConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is double dValue && parameter is string sParameter && double.TryParse(sParameter, out double pValue))
            {
                return dValue + pValue;
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}