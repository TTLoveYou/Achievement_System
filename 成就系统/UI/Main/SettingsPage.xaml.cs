using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using 成就系统.Services;
using 成就系统.Utilities;

namespace 成就系统.UI.Main
{
    /// <summary>
    /// 相机优先级类型
    /// </summary>
    public enum CameraPriorityType
    {
        /// <summary>
        /// 优先使用移动端摄像头 (ADB)
        /// </summary>
        Mobile,
        /// <summary>
        /// 优先使用本地相机
        /// </summary>
        Local
    }

    /// <summary>
    /// SettingsPage.xaml 的交互逻辑
    /// </summary>
    public partial class SettingsPage : Page
    {
        // 当前选择的主题
        private ThemeType _selectedTheme = ThemeType.Light;
        // 当前数据库配置
        private Services.ThemeSettings.DatabaseConfig _currentDbConfig = new();
        // 当前相机优先级
        private CameraPriorityType _selectedCameraPriority = CameraPriorityType.Mobile;

        public SettingsPage()
        {
            InitializeComponent();
            Loaded += SettingsPage_Loaded;
        }

        private void SettingsPage_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeThemeSelection();
            InitializeDatabaseConfig();
            InitializePageSizeSettings();
            InitializeAnimationSpeedSettings();
            InitializeAchievementsImagePathSettings();
            InitializeAdbToolPathSettings();
            InitializeCameraPrioritySettings();
        }

        /// <summary>
        /// 初始化相机优先级设置
        /// </summary>
        private void InitializeCameraPrioritySettings()
        {
            try
            {
                // 加载保存的相机优先级设置
                string savedPriority = ThemeSettings.LoadCameraPriority();
                if (string.IsNullOrEmpty(savedPriority) || savedPriority == "Adb")
                {
                    // 默认使用移动端摄像头
                    _selectedCameraPriority = CameraPriorityType.Mobile;
                    if (MobileCameraRadio != null)
                    {
                        MobileCameraRadio.IsChecked = true;
                    }
                }
                else if (savedPriority == "Local")
                {
                    // 使用本地相机
                    _selectedCameraPriority = CameraPriorityType.Local;
                    if (LocalCameraRadio != null)
                    {
                        LocalCameraRadio.IsChecked = true;
                    }
                }
                else
                {
                    // 解析失败，使用默认值
                    _selectedCameraPriority = CameraPriorityType.Mobile;
                    if (MobileCameraRadio != null)
                    {
                        MobileCameraRadio.IsChecked = true;
                    }
                }
            }
            catch (Exception)
            {
                // 发生错误时使用默认值
                _selectedCameraPriority = CameraPriorityType.Mobile;
                if (MobileCameraRadio != null)
                {
                    MobileCameraRadio.IsChecked = true;
                }
            }
        }

        /// <summary>
        /// 相机优先级RadioButton的Checked事件处理程序
        /// </summary>
        private void CameraPriorityRadio_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is not RadioButton radioButton)
                    return;
                if (radioButton == null)
                    return;

                // 根据选择的RadioButton更新相机优先级
                if (radioButton == MobileCameraRadio)
                {
                    _selectedCameraPriority = CameraPriorityType.Mobile;
                }
                else if (radioButton == LocalCameraRadio)
                {
                    _selectedCameraPriority = CameraPriorityType.Local;
                }
            }
            catch (Exception)
            {
                // 发生错误时忽略，确保界面不会崩溃
            }
        }

        /// <summary>
        /// 初始化分页大小设置
        /// </summary>
        private void InitializePageSizeSettings()
        {
            try
            {
                // 加载保存的分页大小设置
                int pageSize = ThemeSettings.LoadPageSize();

                // 设置滑块值
                if (PageSizeSlider != null)
                {
                    PageSizeSlider.Value = pageSize;
                }

                // 更新显示文本
                UpdatePageSizeValueText();
            }
            catch (Exception)
            {
                // 发生错误时使用默认值
                if (PageSizeSlider != null)
                {
                    PageSizeSlider.Value = 20;
                }

                // 更新显示文本
                UpdatePageSizeValueText();
            }
        }

        /// <summary>
        /// 更新分页大小值显示文本
        /// </summary>
        private void UpdatePageSizeValueText()
        {
            if (PageSizeValueText != null && PageSizeSlider != null)
            {
                PageSizeValueText.Text = $"{PageSizeSlider.Value}项";
            }
        }

        /// <summary>
        /// 分页大小减按钮点击事件
        /// </summary>
        private void PageSizeDecreaseBtn_Click(object sender, RoutedEventArgs e)
        {
            if (PageSizeSlider != null)
            {
                if (PageSizeSlider.Value > PageSizeSlider.Minimum)
                {
                    PageSizeSlider.Value -= 2; // 每次减少2
                    UpdatePageSizeValueText();
                }
            }
        }

        /// <summary>
        /// 分页大小加按钮点击事件
        /// </summary>
        private void PageSizeIncreaseBtn_Click(object sender, RoutedEventArgs e)
        {
            if (PageSizeSlider != null)
            {
                if (PageSizeSlider.Value < PageSizeSlider.Maximum)
                {
                    PageSizeSlider.Value += 2; // 每次增加2
                    UpdatePageSizeValueText();
                }
            }
        }

        /// <summary>
        /// 分页大小滑块值变化事件
        /// </summary>
        private void PageSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdatePageSizeValueText();
        }

        /// <summary>
        /// 初始化ADB工具路径设置
        /// </summary>
        private void InitializeAdbToolPathSettings()
        {
            try
            {
                // 加载保存的ADB工具路径设置
                string savedPath = ThemeSettings.LoadAdbToolPath();

                // 设置文本框值
                if (AdbToolPathTextBox != null)
                {
                    AdbToolPathTextBox.Text = savedPath;
                }
            }
            catch (Exception)
            {
                // 发生错误时使用空值
                if (AdbToolPathTextBox != null)
                {
                    AdbToolPathTextBox.Text = string.Empty;
                }
            }
        }

        /// <summary>
        /// 浏览ADB工具路径按钮点击事件
        /// </summary>
        private void BrowseAdbPathBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 使用 OpenFileDialog 选择adb.exe文件
                var openFileDialog = new OpenFileDialog
                {
                    Title = "选择ADB工具路径",
                    Filter = "ADB工具 (adb.exe)|adb.exe|所有文件 (*.*)|*.*",
                    ValidateNames = true,
                    CheckFileExists = true,
                    CheckPathExists = true
                };

                // 设置默认路径
                string currentPath = AdbToolPathTextBox?.Text ?? string.Empty;
                if (!string.IsNullOrEmpty(currentPath) && File.Exists(currentPath))
                {
                    openFileDialog.InitialDirectory = Path.GetDirectoryName(currentPath);
                }
                else
                {
                    // 使用默认路径
                    string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                    if (baseDir != null)
                    {
                        openFileDialog.InitialDirectory = Path.Combine(baseDir, "Resources");
                    }
                }

                // 显示对话框
                if (openFileDialog.ShowDialog() == true)
                {
                    // 获取选择的文件路径
                    string filePath = openFileDialog.FileName;
                    if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                    {
                        // 更新文本框值
                        if (AdbToolPathTextBox != null)
                        {
                            AdbToolPathTextBox.Text = filePath;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // 显示错误消息
                MessageBox.Show($"浏览ADB工具路径时出错：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 初始化动画速度设置
        /// </summary>
        private void InitializeAnimationSpeedSettings()
        {
            try
            {
                // 加载保存的动画速度设置
                var (birthdaySpeed, anniversarySpeed) = ThemeSettings.LoadAnimationSpeed();

                // 设置滑块值
                if (BirthdaySpeedSlider != null)
                {
                    BirthdaySpeedSlider.Value = birthdaySpeed;
                }
                if (AnniversarySpeedSlider != null)
                {
                    AnniversarySpeedSlider.Value = anniversarySpeed;
                }

                // 更新显示文本
                UpdateSpeedValueText();
            }
            catch (Exception)
            {
                // 发生错误时使用默认值
                if (BirthdaySpeedSlider != null)
                {
                    BirthdaySpeedSlider.Value = 90;
                }
                if (AnniversarySpeedSlider != null)
                {
                    AnniversarySpeedSlider.Value = 90;
                }

                // 更新显示文本
                UpdateSpeedValueText();
            }
        }

        /// <summary>
        /// 初始化成就图片路径设置
        /// </summary>
        private void InitializeAchievementsImagePathSettings()
        {
            try
            {
                // 加载保存的成就图片路径设置
                string savedPath = ThemeSettings.LoadAchievementsImagePath();

                // 设置文本框值
                if (AchievementsImagePathTextBox != null)
                {
                    AchievementsImagePathTextBox.Text = savedPath;
                }
            }
            catch (Exception)
            {
                // 发生错误时使用空值
                if (AchievementsImagePathTextBox != null)
                {
                    AchievementsImagePathTextBox.Text = string.Empty;
                }
            }
        }

        /// <summary>
        /// 更新速度值显示文本
        /// </summary>
        private void UpdateSpeedValueText()
        {
            if (BirthdaySpeedValueText != null && BirthdaySpeedSlider != null)
            {
                BirthdaySpeedValueText.Text = $"{BirthdaySpeedSlider.Value}秒";
            }
            if (AnniversarySpeedValueText != null && AnniversarySpeedSlider != null)
            {
                AnniversarySpeedValueText.Text = $"{AnniversarySpeedSlider.Value}秒";
            }
        }

        /// <summary>
        /// 初始化主题选择状态
        /// </summary>
        private void InitializeThemeSelection()
        {
            try
            {
                // 获取当前主题
                _selectedTheme = ThemeManager.CurrentTheme;

                // 设置对应的RadioButton为选中状态
                switch (_selectedTheme)
                {
                    case ThemeType.Light:
                        if (LightThemeRadio != null)
                        {
                            LightThemeRadio.IsChecked = true;
                        }
                        if (ThemeDescription != null)
                        {
                            ThemeDescription.Text = "当前使用亮色主题";
                        }
                        break;
                    case ThemeType.Dark:
                        if (DarkThemeRadio != null)
                        {
                            DarkThemeRadio.IsChecked = true;
                        }
                        if (ThemeDescription != null)
                        {
                            ThemeDescription.Text = "当前使用暗色主题";
                        }
                        break;
                    case ThemeType.System:
                        if (SystemThemeRadio != null)
                        {
                            SystemThemeRadio.IsChecked = true;
                        }
                        if (ThemeDescription != null)
                        {
                            var systemTheme = ThemeManager.GetSystemThemeType();
                            ThemeDescription.Text = $"当前跟随系统主题（{systemTheme}）";
                        }
                        break;
                    default:
                        // 默认使用亮色主题
                        if (LightThemeRadio != null)
                        {
                            LightThemeRadio.IsChecked = true;
                        }
                        if (ThemeDescription != null)
                        {
                            ThemeDescription.Text = "当前使用亮色主题";
                        }
                        break;
                }
            }
            catch (Exception)
            {
                // 发生错误时，使用默认主题
                if (LightThemeRadio != null)
                {
                    LightThemeRadio.IsChecked = true;
                }
                if (ThemeDescription != null)
                {
                    ThemeDescription.Text = "当前使用亮色主题";
                }
            }
        }

        /// <summary>
        /// 主题选择RadioButton的Checked事件处理程序
        /// </summary>
        private void ThemeRadio_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is not RadioButton radioButton)
                    return;
                if (radioButton == null)
                    return;

                // 根据选择的RadioButton更新主题描述
                if (radioButton == LightThemeRadio)
                {
                    _selectedTheme = ThemeType.Light;
                    if (ThemeDescription != null)
                    {
                        ThemeDescription.Text = "当前选择：亮色主题";
                    }
                }
                else if (radioButton == DarkThemeRadio)
                {
                    _selectedTheme = ThemeType.Dark;
                    if (ThemeDescription != null)
                    {
                        ThemeDescription.Text = "当前选择：暗色主题";
                    }
                }
                else if (radioButton == SystemThemeRadio)
                {
                    _selectedTheme = ThemeType.System;
                    if (ThemeDescription != null)
                    {
                        try
                        {
                            var systemTheme = ThemeManager.GetSystemThemeType();
                            ThemeDescription.Text = $"当前选择：跟随系统主题（{systemTheme}）";
                        }
                        catch (Exception)
                        {
                            ThemeDescription.Text = "当前选择：跟随系统主题";
                        }
                    }
                }
            }
            catch (Exception)
            {
                // 发生错误时忽略，确保界面不会崩溃
            }
        }

        /// <summary>
        /// 主题选项的点击事件处理程序
        /// </summary>
        private void ThemeOption_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (sender is not Border border)
                    return;

                // 根据点击的Border设置对应的RadioButton为选中状态
                if (border == LightThemeOption && LightThemeRadio != null)
                {
                    LightThemeRadio.IsChecked = true;
                }
                else if (border == DarkThemeOption && DarkThemeRadio != null)
                {
                    DarkThemeRadio.IsChecked = true;
                }
                else if (border == SystemThemeOption && SystemThemeRadio != null)
                {
                    SystemThemeRadio.IsChecked = true;
                }
            }
            catch (Exception)
            {
                // 发生错误时忽略，确保界面不会崩溃
            }
        }

        /// <summary>
        /// 初始化数据库配置
        /// </summary>
        private void InitializeDatabaseConfig()
        {
            try
            {
                // 加载当前数据库配置
                _currentDbConfig = Services.ThemeSettings.LoadDatabaseConfig();

                // 设置到UI控件
                if (DbServerTextBox != null)
                {
                    DbServerTextBox.Text = _currentDbConfig.Server;
                }
                if (DbNameTextBox != null)
                {
                    DbNameTextBox.Text = _currentDbConfig.Database;
                }
                if (DbUsernameTextBox != null)
                {
                    DbUsernameTextBox.Text = _currentDbConfig.Username;
                }
                if (DbPasswordBox != null)
                {
                    DbPasswordBox.Password = _currentDbConfig.Password;
                }
                if (UsersTableTextBox != null)
                {
                    UsersTableTextBox.Text = _currentDbConfig.UsersTable;
                }
                if (CategoriesTableTextBox != null)
                {
                    CategoriesTableTextBox.Text = _currentDbConfig.CategoriesTable;
                }
                if (AchievementsTableTextBox != null)
                {
                    AchievementsTableTextBox.Text = _currentDbConfig.AchievementsTable;
                }
            }
            catch (Exception)
            {
                // 发生错误时使用默认值
                if (DbServerTextBox != null)
                {
                    DbServerTextBox.Text = "localhost";
                }
                if (DbNameTextBox != null)
                {
                    DbNameTextBox.Text = "achievement_system";
                }
                if (DbUsernameTextBox != null)
                {
                    DbUsernameTextBox.Text = "root";
                }
                if (DbPasswordBox != null)
                {
                    DbPasswordBox.Password = "Root@123";
                }
                if (UsersTableTextBox != null)
                {
                    UsersTableTextBox.Text = "users";
                }
                if (CategoriesTableTextBox != null)
                {
                    CategoriesTableTextBox.Text = "achievement_categories";
                }
                if (AchievementsTableTextBox != null)
                {
                    AchievementsTableTextBox.Text = "achievements";
                }
            }
        }

        /// <summary>
        /// 测试连接按钮的Click事件处理程序
        /// </summary>
        private async void TestConnectionButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 获取UI中的配置
                var config = new Services.ThemeSettings.DatabaseConfig
                {
                    Server = DbServerTextBox?.Text ?? "",
                    Database = DbNameTextBox?.Text ?? "",
                    Username = DbUsernameTextBox?.Text ?? "",
                    Password = DbPasswordBox?.Password ?? ""
                };

                if (ConnectionTestResult != null)
                {
                    ConnectionTestResult.Text = "正在测试连接...";
                    ConnectionTestResult.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(52, 152, 219));
                }

                // 测试连接
                using var dbHelper = new DatabaseHelper();
                // 尝试执行一个简单的查询来测试连接
                var result = await dbHelper.ExecuteScalarAsync("SELECT 1");

                if (ConnectionTestResult != null)
                {
                    ConnectionTestResult.Text = "连接成功！";
                    ConnectionTestResult.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(46, 204, 113));
                }

                MessageBox.Show("数据库连接成功！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                if (ConnectionTestResult != null)
                {
                    ConnectionTestResult.Text = $"连接失败：{ex.Message}";
                    ConnectionTestResult.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(231, 76, 60));
                }

                MessageBox.Show($"数据库连接失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 生日成就动画速度减按钮点击事件
        /// </summary>
        private void BirthdaySpeedDecreaseBtn_Click(object sender, RoutedEventArgs e)
        {
            if (BirthdaySpeedSlider != null)
            {
                if (BirthdaySpeedSlider.Value > BirthdaySpeedSlider.Minimum)
                {
                    BirthdaySpeedSlider.Value -= 10; // 每次减少10秒
                    UpdateSpeedValueText();
                }
            }
        }

        /// <summary>
        /// 生日成就动画速度加按钮点击事件
        /// </summary>
        private void BirthdaySpeedIncreaseBtn_Click(object sender, RoutedEventArgs e)
        {
            if (BirthdaySpeedSlider != null)
            {
                if (BirthdaySpeedSlider.Value < BirthdaySpeedSlider.Maximum)
                {
                    BirthdaySpeedSlider.Value += 10; // 每次增加10秒
                    UpdateSpeedValueText();
                }
            }
        }

        /// <summary>
        /// 生日成就动画速度滑块值变化事件
        /// </summary>
        private void BirthdaySpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateSpeedValueText();
        }

        /// <summary>
        /// 周年成就动画速度减按钮点击事件
        /// </summary>
        private void AnniversarySpeedDecreaseBtn_Click(object sender, RoutedEventArgs e)
        {
            if (AnniversarySpeedSlider != null)
            {
                if (AnniversarySpeedSlider.Value > AnniversarySpeedSlider.Minimum)
                {
                    AnniversarySpeedSlider.Value -= 10; // 每次减少10秒
                    UpdateSpeedValueText();
                }
            }
        }

        /// <summary>
        /// 周年成就动画速度加按钮点击事件
        /// </summary>
        private void AnniversarySpeedIncreaseBtn_Click(object sender, RoutedEventArgs e)
        {
            if (AnniversarySpeedSlider != null)
            {
                if (AnniversarySpeedSlider.Value < AnniversarySpeedSlider.Maximum)
                {
                    AnniversarySpeedSlider.Value += 10; // 每次增加10秒
                    UpdateSpeedValueText();
                }
            }
        }

        /// <summary>
        /// 周年成就动画速度滑块值变化事件
        /// </summary>
        private void AnniversarySpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateSpeedValueText();
        }

        /// <summary>
        /// 浏览成就图片路径按钮点击事件
        /// </summary>
        private void BrowseImagePathBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 使用 OpenFileDialog 并设置为选择文件夹模式
                var openFileDialog = new OpenFileDialog
                {
                    Title = "选择成就图片保存路径",
                    ValidateNames = false,
                    CheckFileExists = false,
                    CheckPathExists = true,
                    FileName = "选择文件夹"
                };

                // 设置默认路径
                string currentPath = AchievementsImagePathTextBox?.Text ?? string.Empty;
                if (!string.IsNullOrEmpty(currentPath) && Directory.Exists(currentPath))
                {
                    openFileDialog.InitialDirectory = currentPath;
                }
                else
                {
                    // 使用默认路径
                    string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                    if (baseDir != null)
                    {
                        openFileDialog.InitialDirectory = Path.Combine(baseDir, "Resources", "AsImages");
                    }
                }

                // 显示对话框
                if (openFileDialog.ShowDialog() == true)
                {
                    // 获取选择的文件夹路径
                    string fileName = openFileDialog.FileName;
                    string folderPath = fileName != null ? Path.GetDirectoryName(fileName) ?? string.Empty : string.Empty;
                    if (!string.IsNullOrEmpty(folderPath))
                    {
                        // 更新文本框值
                        if (AchievementsImagePathTextBox != null)
                        {
                            AchievementsImagePathTextBox.Text = folderPath;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // 显示错误消息
                MessageBox.Show($"浏览文件夹时出错：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 应用按钮的Click事件处理程序
        /// </summary>
        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 应用选中的主题
                ThemeManager.ApplyTheme(_selectedTheme);

                // 更新主题描述
                if (ThemeDescription != null)
                {
                    switch (_selectedTheme)
                    {
                        case ThemeType.Light:
                            ThemeDescription.Text = "当前使用亮色主题";
                            break;
                        case ThemeType.Dark:
                            ThemeDescription.Text = "当前使用暗色主题";
                            break;
                        case ThemeType.System:
                            try
                            {
                                var systemTheme = ThemeManager.GetSystemThemeType();
                                ThemeDescription.Text = $"当前跟随系统主题（{systemTheme}）";
                            }
                            catch (Exception)
                            {
                                ThemeDescription.Text = "当前跟随系统主题";
                            }
                            break;
                    }
                }

                // 保存数据库配置
                _currentDbConfig = new Services.ThemeSettings.DatabaseConfig
                {
                    Server = DbServerTextBox?.Text ?? "localhost",
                    Database = DbNameTextBox?.Text ?? "achievement_system",
                    Username = DbUsernameTextBox?.Text ?? "root",
                    Password = DbPasswordBox?.Password ?? "",
                    UsersTable = UsersTableTextBox?.Text ?? "users",
                    CategoriesTable = CategoriesTableTextBox?.Text ?? "achievement_categories",
                    AchievementsTable = AchievementsTableTextBox?.Text ?? "achievements"
                };
                Services.ThemeSettings.SaveDatabaseConfig(_currentDbConfig);

                // 保存动画速度设置
                double birthdaySpeed = BirthdaySpeedSlider?.Value ?? 90;
                double anniversarySpeed = AnniversarySpeedSlider?.Value ?? 90;
                ThemeSettings.SaveAnimationSpeed(birthdaySpeed, anniversarySpeed);

                // 保存成就图片路径设置
                string oldImagePath = ThemeSettings.LoadAchievementsImagePath();
                string achievementsImagePath = AchievementsImagePathTextBox?.Text ?? string.Empty;
                ThemeSettings.SaveAchievementsImagePath(achievementsImagePath);

                // 同步移动二级文件夹
                if (!string.IsNullOrWhiteSpace(oldImagePath) && !string.IsNullOrWhiteSpace(achievementsImagePath) && oldImagePath != achievementsImagePath)
                {
                    if (System.IO.Directory.Exists(oldImagePath))
                    {
                        try
                        {
                            // 确保新路径存在
                            if (!System.IO.Directory.Exists(achievementsImagePath))
                            {
                                System.IO.Directory.CreateDirectory(achievementsImagePath);
                                System.Diagnostics.Debug.WriteLine($"创建新的成就图片路径: {achievementsImagePath}");
                            }

                            // 获取所有二级文件夹
                            var subDirs = System.IO.Directory.GetDirectories(oldImagePath);
                            System.Diagnostics.Debug.WriteLine($"找到 {subDirs.Length} 个二级文件夹需要移动");

                            // 移动所有二级文件夹
                            int movedCount = 0;
                            int skippedCount = 0;

                            foreach (string subDir in subDirs)
                            {
                                string subDirName = System.IO.Path.GetFileName(subDir);

                                // 安全检查：确保文件夹名称不为空
                                if (string.IsNullOrWhiteSpace(subDirName))
                                {
                                    System.Diagnostics.Debug.WriteLine("文件夹名称为空，跳过移动");
                                    skippedCount++;
                                    continue;
                                }

                                string newSubDir = System.IO.Path.Combine(achievementsImagePath, subDirName);

                                // 安全检查：确保新路径在目标目录内
                                if (!newSubDir.StartsWith(achievementsImagePath, StringComparison.OrdinalIgnoreCase))
                                {
                                    System.Diagnostics.Debug.WriteLine($"路径安全检查失败，跳过移动: {newSubDir}");
                                    skippedCount++;
                                    continue;
                                }

                                if (!System.IO.Directory.Exists(newSubDir))
                                {
                                    System.IO.Directory.Move(subDir, newSubDir);
                                    System.Diagnostics.Debug.WriteLine($"移动文件夹: {subDir} 到 {newSubDir}");
                                    movedCount++;
                                }
                                else
                                {
                                    System.Diagnostics.Debug.WriteLine($"文件夹已存在，跳过移动: {newSubDir}");
                                    skippedCount++;
                                }
                            }

                            System.Diagnostics.Debug.WriteLine($"文件夹移动完成: 成功移动 {movedCount} 个，跳过 {skippedCount} 个");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"移动二级文件夹时出错: {ex.Message}");
                            // 显示错误消息给用户
                            MessageBox.Show($"移动二级文件夹时出错: {ex.Message}", "错误",
                                            MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }

                // 保存ADB工具路径设置
                string adbToolPath = AdbToolPathTextBox?.Text ?? string.Empty;
                ThemeSettings.SaveAdbToolPath(adbToolPath);

                // 保存分页大小设置
                int pageSize = (int)(PageSizeSlider?.Value ?? 20);
                ThemeSettings.SavePageSize(pageSize);

                // 保存相机优先级设置
                string cameraPriority = _selectedCameraPriority == CameraPriorityType.Mobile ? "Adb" : "Local";
                ThemeSettings.SaveCameraPriority(cameraPriority);

                // 显示成功消息
                MessageBox.Show("设置已成功应用", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                // 显示错误消息
                MessageBox.Show($"应用设置时出错：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
