using Microsoft.Win32;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;
using 成就系统.Config;
using 成就系统.Services;

namespace 成就系统.UI.Main
{
    /// <summary>
    /// AddAchievementPage.xaml 的交互逻辑
    /// </summary>
    public partial class AddAchievementPage : Page
    {
        private readonly AchievementService _achievementService = new();

        public string UserName { get; set; }  //用户名
        private readonly AchievementCard? _achievementCard; // 存储传入的成就卡片


        private DateTime _lastValidDate = DateTime.Today; // 存储上次有效的日期
        private double _currentProgress = 0; // 当前完成度百分比
        private double _currentScore = 0.0; // 当前评分
        private string _selectedImagePath = string.Empty;   //选择的图片路径


        public AddAchievementPage(string userName, AchievementCard? achievementCard = null)
        {
            UserName = userName;
            _achievementCard = achievementCard;
            InitializeComponent();

            Loaded += AddAchievementPage_Loaded; // 添加页面加载事件
        }

        //页面加载初始化
        private async void AddAchievementPage_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadAllBigCategoriesAsync();

            // 如果是查看/编辑现有成就
            if (_achievementCard != null)
            {
                // 直接使用传入的成就卡片数据填充UI
                await FillAchievementData(_achievementCard);
                SetEditMode(false); // 初始为不可编辑
                EditModeCheckBox.Visibility = Visibility.Visible; // 显示编辑复选框
                AnniversaryLine.Visibility = Visibility.Visible;
                BirthdayLine.Visibility = Visibility.Visible;
                AchievementOperation.Text = "成就详情"; // 修改标题

                ShowAchievementGreeting();
            }
        }

        // 填充成就数据
        private async Task FillAchievementData(AchievementCard achievementCard)
        {
            try
            {
                // 填充数据
                AchievementNameTextBox.Text = achievementCard.AchievementName;
                DescriptionTextBox.Text = achievementCard.Description;
                AchievementDatePicker.SelectedDate = achievementCard.AchievementDate;
                _currentProgress = GetProgressFromCompletionDegree(achievementCard.CompletionDegree);
                _currentScore = (double)achievementCard.Score;

                // 设置图片
                if (!string.IsNullOrEmpty(achievementCard.ImagePath) && File.Exists(achievementCard.ImagePath) && !achievementCard.ImagePath.Equals(AppConfig.DefaultImagePath, StringComparison.OrdinalIgnoreCase))
                {
                    _selectedImagePath = achievementCard.ImagePath;
                    ShowImagePreview();
                }

                // 设置类别
                int categoryId = achievementCard.CategoryId ?? 0;
                var (bigCategory, smallCategory) = await _achievementService.GetCategoryNamesAsync(categoryId);

                CategoryComboBox.SelectedItem = bigCategory;
                // 注意：这里需要异步加载小类，但不需要等待
                _ = LoadSmallCategoriesForSelectedBigCategoryAsync();
                SubcategoryComboBox.SelectedItem = smallCategory;

                UpdateProgressDisplay();
                UpdateScoreDisplay();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"填充成就数据失败: {ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 设置编辑模式
        private void SetEditMode(bool isEdit)
        {
            // 设置控件可编辑状态
            CategoryComboBox.IsEnabled = isEdit;
            SubcategoryComboBox.IsEnabled = isEdit;
            AchievementDatePicker.IsEnabled = isEdit;
            ProgressBlock1.IsHitTestVisible = isEdit;
            ProgressBlock2.IsHitTestVisible = isEdit;
            ProgressBlock3.IsHitTestVisible = isEdit;
            ProgressBlock4.IsHitTestVisible = isEdit;
            DecreaseScoreButton.IsEnabled = isEdit;
            IncreaseScoreButton.IsEnabled = isEdit;
            ScoreTextBox.IsEnabled = isEdit;
            SelectImageButton.IsEnabled = isEdit;
            AchievementNameTextBox.IsReadOnly = !isEdit;
            DescriptionTextBox.IsReadOnly = !isEdit;

            // 更新删除按钮状态（有图片时才可删除）
            DeleteImageButton.IsEnabled = isEdit && !string.IsNullOrEmpty(_selectedImagePath);

            SaveButton.IsEnabled = isEdit;
        }

        //编辑
        private void EditModeCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            SetEditMode(true);
            AchievementOperation.Text = "编辑成就";
        }

        //查看
        private void EditModeCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            SetEditMode(false);
            AchievementOperation.Text = "成就详情";
        }

        // 显示成就周年祝贺
        private void ShowAchievementGreeting()
        {
            DateTime today = DateTime.Today;

            // 计算成就年龄（整数年）
            int achievementAge = today.Year - _lastValidDate.Year;

            // 如果当前月份在成就月份之前，或者同月但日期在成就日期之前，年龄减1
            if (today.Month < _lastValidDate.Month || (today.Month == _lastValidDate.Month && today.Day < _lastValidDate.Day))
            {
                achievementAge--;
            }

            // 判断是否是整周年（10、20、30等）
            bool isMilestoneToday = achievementAge > 0 && achievementAge % 10 == 0;

            // 判断今天是否是成就日期（月日相同）
            bool isBirthdayToday = today.Month == _lastValidDate.Month &&
                                     today.Day == _lastValidDate.Day;

            if (isMilestoneToday)
            {
                AnniversaryLine.Text = $"🎉🎉{achievementAge}周年纪念日快乐！🎉🎉";
            }
            else
            {
                int nextMilestoneAge = ((achievementAge / 10) + 1) * 10;

                // 计算距离下个整周年的时间
                (int yearsToMilestone, int monthsToMilestone, int daysToMilestone) =
                    CalculateTimeToMilestone(today, nextMilestoneAge);

                if (yearsToMilestone > 0)
                {
                    AnniversaryLine.Text = $"距离{nextMilestoneAge}周年纪念日还有 {yearsToMilestone} 年";
                }
                else if (monthsToMilestone > 0)
                {
                    AnniversaryLine.Text = $"距离{nextMilestoneAge}周年纪念日还有 {monthsToMilestone} 个月";
                }
                else
                {
                    AnniversaryLine.Text = $"距离{nextMilestoneAge}周年纪念日还有 {daysToMilestone} 天";
                }
            }
            if (isBirthdayToday)
            {
                BirthdayLine.Text = "🎂🎂生日快乐！🎂🎂";
            }
            else
            {
                // 计算距离下一个成就生日的天数
                int daysToBirthday = CalculateDaysToNextBirthday(today);
                BirthdayLine.Text = $"距离生日还有 {daysToBirthday} 天";
            }

        }

        // 计算距离下个整周年的时间
        private (int years, int months, int days) CalculateTimeToMilestone(DateTime today, int nextMilestoneAge)
        {
            DateTime milestoneDate = CalculateNextMilestoneDate(nextMilestoneAge);
            TimeSpan timeSpan = milestoneDate - today;
            int totalDays = timeSpan.Days;

            // 计算年数
            int years = totalDays / 365;
            int remainingDays = totalDays % 365;

            // 计算月数
            int months = remainingDays / 30;
            int days = remainingDays % 30;

            return (years, months, days);
        }

        // 计算下一个整周年日期
        private DateTime CalculateNextMilestoneDate(int nextMilestoneAge)
        {
            DateTime milestoneDate = _lastValidDate.AddYears(nextMilestoneAge);

            // 处理2月29日特殊情况
            if (_lastValidDate.Month == 2 && _lastValidDate.Day == 29)
            {
                // 非闰年使用3月1日
                if (!DateTime.IsLeapYear(milestoneDate.Year))
                {
                    milestoneDate = new DateTime(milestoneDate.Year, 3, 1);
                }
            }

            return milestoneDate;
        }

        // 计算距离下一个成就生日的天数
        private int CalculateDaysToNextBirthday(DateTime today)
        {
            DateTime nextBirthday = new(today.Year, _lastValidDate.Month, _lastValidDate.Day);

            // 处理2月29日特殊情况
            if (_lastValidDate.Month == 2 && _lastValidDate.Day == 29)
            {
                if (!DateTime.IsLeapYear(today.Year))
                {
                    // 非闰年使用3月1日
                    nextBirthday = new DateTime(today.Year, 3, 1);
                }
            }

            // 如果今年的生日已过，计算明年生日
            if (nextBirthday < today)
            {
                nextBirthday = nextBirthday.AddYears(1);

                // 再次处理2月29日特殊情况
                if (_lastValidDate.Month == 2 && _lastValidDate.Day == 29)
                {
                    if (!DateTime.IsLeapYear(nextBirthday.Year))
                    {
                        nextBirthday = new DateTime(nextBirthday.Year, 3, 1);
                    }
                }
            }

            return (nextBirthday - today).Days;
        }

        //获取所有大类
        private async Task LoadAllBigCategoriesAsync()
        {
            try
            {
                // 获取所有大类（不限于当前用户）
                var bigCategories = await _achievementService.GetbigCategoriesAsync();

                CategoryComboBox.ItemsSource = bigCategories;

                // 默认选择第一个大类
                if (bigCategories.Count != 0)
                {
                    CategoryComboBox.SelectedIndex = 0;
                    await LoadSmallCategoriesForSelectedBigCategoryAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载分类失败: {ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //获取所有小类
        private async Task LoadSmallCategoriesForSelectedBigCategoryAsync()
        {
            if (CategoryComboBox.SelectedItem is string selectedBigCategory)
            {
                try
                {
                    // 获取该大类下所有小类（不限于当前用户）
                    var smallCategories = await _achievementService.GetSmallCategoriesAsync(selectedBigCategory);

                    SubcategoryComboBox.ItemsSource = smallCategories;

                    // 默认选择第一个小类
                    if (smallCategories.Count != 0)
                    {
                        SubcategoryComboBox.SelectedIndex = 0;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"加载子分类失败: {ex.Message}", "错误",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        //大类变化时变化小类
        private async void CategoryComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CategoryComboBox.SelectedItem != null)
            {
                await LoadSmallCategoriesForSelectedBigCategoryAsync();
            }
        }

        // 日期变化时验证
        private void AchievementDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AchievementDatePicker.SelectedDate.HasValue)
            {
                // 更新最后有效日期
                _lastValidDate = AchievementDatePicker.SelectedDate.Value;
            }
        }

        // 进度块点击事件
        private void ProgressBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is string tagValue)
            {
                if (double.TryParse(tagValue, out double progress))
                {
                    _currentProgress = progress;
                    UpdateProgressDisplay();
                }
            }
        }

        // 更新进度显示
        private void UpdateProgressDisplay()
        {
            // 重置所有进度块
            ProgressBlock1.Background = Brushes.Transparent;
            ProgressBlock2.Background = Brushes.Transparent;
            ProgressBlock3.Background = Brushes.Transparent;
            ProgressBlock4.Background = Brushes.Transparent;

            // 根据当前进度填充块
            if (_currentProgress >= 25)
                ProgressBlock1.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF4CAF50"));
            if (_currentProgress >= 50)
                ProgressBlock2.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF4CAF50"));
            if (_currentProgress >= 75)
                ProgressBlock3.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF4CAF50"));
            if (_currentProgress >= 100)
                ProgressBlock4.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF4CAF50"));

            ProgressText.Text = $"{_currentProgress}%";
        }

        // 减少评分按钮点击事件
        private void DecreaseScoreButton_Click(object sender, RoutedEventArgs e)
        {
            _currentScore = Math.Max(0.0, _currentScore - 0.1);
            UpdateScoreDisplay();
        }

        // 增加评分按钮点击事件
        private void IncreaseScoreButton_Click(object sender, RoutedEventArgs e)
        {
            _currentScore = Math.Min(9.9, _currentScore + 0.1);
            UpdateScoreDisplay();
        }

        // 评分输入框获得焦点
        private void ScoreTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                textBox.SelectAll();

                // 添加短暂的延迟确保选中操作完成
                Dispatcher.BeginInvoke((Action)(() =>
                {
                    textBox.SelectAll();
                }), DispatcherPriority.Input);
            }
        }

        // 评分输入基础校验
        private void ScoreTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            TextBox? textBox = sender as TextBox;
            string currentText = textBox.Text;
            int selectionStart = textBox.SelectionStart;        // 当前光标起始位置
            int selectionLength = textBox.SelectionLength;      // 选中文本长度

            // 计算实际文本位置
            string newText = string.Concat(currentText.AsSpan(0, selectionStart), e.Text, currentText.AsSpan(selectionStart + selectionLength));

            // 验证正则表达式：允许0-9和小数点
            Regex regex = MyRegex();

            // 验证输入是否符合格式
            if (!regex.IsMatch(newText))
            {
                e.Handled = true;
                return;
            }

            // 验证值范围
            if (double.TryParse(newText, NumberStyles.Any, CultureInfo.InvariantCulture, out double value))
            {
                if (value < 0 || value > 9.9)
                {
                    e.Handled = true;
                }
            }
            else
            {
                e.Handled = true;
            }
        }

        // 评分处理键盘输入（如方向键、退格键）
        private void DatePickerorScoreBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // 允许控制键和数字键
            if (e.Key == Key.Back || e.Key == Key.Delete ||
                e.Key == Key.Left || e.Key == Key.Right ||
                e.Key == Key.Home || e.Key == Key.End ||
                e.Key == Key.Tab || e.Key == Key.Enter ||
                (e.Key >= Key.D0 && e.Key <= Key.D9) ||
                (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9) ||
                e.Key == Key.Decimal) // 允许 . 符号
            {
                return;
            }

            e.Handled = true; // 阻止其他按键
        }

        // 评分输入完成校验
        private void ScoreTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (double.TryParse(ScoreTextBox.Text, out double result))
            {
                _currentScore = Math.Max(0.0, Math.Min(9.9, result));
                UpdateScoreDisplay();
            }
            else
            {
                ScoreTextBox.Text = _currentScore.ToString("0.0");
            }
        }

        // 更新评分显示
        private void UpdateScoreDisplay()
        {
            ScoreTextBox.Text = _currentScore.ToString("0.0");
        }

        // 选择图片按钮点击事件
        private void SelectImageButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new()
            {
                Filter = "图片文件|*.jpg;*.jpeg;*.png;*.bmp|所有文件|*.*",
                Title = "选择成就图片"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                _selectedImagePath = openFileDialog.FileName;
                ShowImagePreview();
            }
        }

        // 显示图片预览
        private void ShowImagePreview()
        {
            if (!string.IsNullOrEmpty(_selectedImagePath) && File.Exists(_selectedImagePath))
            {
                try
                {
                    BitmapImage bitmap = new();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(_selectedImagePath);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();

                    ImagePreview.Source = bitmap;
                    ImagePreview.Visibility = Visibility.Visible;
                    PlusIcon.Visibility = Visibility.Collapsed;
                    DeleteImageButton.Visibility = Visibility.Visible;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"加载图片失败: {ex.Message}", "错误",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // 删除图片按钮点击事件
        private void DeleteImageButton_Click(object sender, RoutedEventArgs e)
        {
            _selectedImagePath = string.Empty;
            ImagePreview.Source = null;
            ImagePreview.Visibility = Visibility.Collapsed;
            PlusIcon.Visibility = Visibility.Visible;
            DeleteImageButton.Visibility = Visibility.Collapsed;
        }

        // 检查必填字段是否有效
        private bool ValidateInputs()
        {
            // 1. 验证分类
            bool categoryValid = !string.IsNullOrEmpty(CategoryComboBox.SelectedItem as string) && !string.IsNullOrEmpty(SubcategoryComboBox.SelectedItem as string);

            // 2. 验证日期
            bool dateValid = AchievementDatePicker.SelectedDate.HasValue;

            // 5. 验证评分（在0-9.9之间）
            bool scoreValid = _currentScore >= 0 && _currentScore <= 9.9;

            // 3. 验证名称
            bool nameValid = !string.IsNullOrWhiteSpace(AchievementNameTextBox.Text);

            // 4. 验证描述（可选）
            bool descValid = !string.IsNullOrWhiteSpace(DescriptionTextBox.Text);

            return categoryValid && dateValid && scoreValid && nameValid && descValid;
        }

        // 添加进度转完成度字符串的方法
        private static string GetCompletionDegreeString(double progress)
        {
            return progress switch
            {
                >= 100 => "++++",
                >= 75 => "+++",
                >= 50 => "++",
                >= 25 => "+",
                _ => ""
            };
        }

        // 将完成度字符串转换为百分比
        private static double GetProgressFromCompletionDegree(string completionDegree)
        {
            return completionDegree switch
            {
                "++++" => 100,
                "+++" => 75,
                "++" => 50,
                "+" => 25,
                _ => 0
            };
        }

        // 保存按钮点击事件
        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInputs())
            {
                MessageBox.Show("请填写所有必填字段", "输入不完整", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // 获取选择的类别
                string? bigCategory = CategoryComboBox.SelectedItem as string;
                string? smallCategory = SubcategoryComboBox.SelectedItem as string;

                // 创建 AchievementCard 实例
                var achievement = new AchievementCard
                {
                    AchievementId = _achievementCard?.AchievementId ?? 0,
                    AchievementName = AchievementNameTextBox.Text,
                    Description = DescriptionTextBox.Text,
                    AchievementDate = _lastValidDate,
                    CompletionDegree = GetCompletionDegreeString(_currentProgress),
                    Score = (decimal)_currentScore,
                    ImagePath = _selectedImagePath
                };

                // 调用保存方法（传入类别信息）
                bool success = await _achievementService.SaveAchievementAsync(achievement, UserName, bigCategory, smallCategory);

                if (success)
                {
                    MessageBox.Show(achievement.AchievementId == 0 ? "成就添加成功" : "成就修改成功", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    NavigationService.GoBack();
                }
                else
                {
                    MessageBox.Show(achievement.AchievementId == 0 ? "成就添加失败，请重试" : "成就修改失败，请重试", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存成就时出错: {ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 返回按钮点击事件
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService.CanGoBack)
            {
                NavigationService.GoBack();
            }
        }

        [GeneratedRegex(@"^[0-9]*(?:\.[0-9]{0,1})?$")]
        private static partial Regex MyRegex();
    }
}
