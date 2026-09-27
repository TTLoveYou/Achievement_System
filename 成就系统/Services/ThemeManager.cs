using Microsoft.Win32;
using System.Windows;
using System.Windows.Media;

namespace 成就系统.Services
{
    /// <summary>
    /// 主题类型枚举
    /// </summary>
    public enum ThemeType
    {
        /// <summary>
        /// 亮色主题
        /// </summary>
        Light,

        /// <summary>
        /// 暗色主题
        /// </summary>
        Dark,

        /// <summary>
        /// 跟随系统主题
        /// </summary>
        System
    }

    /// <summary>
    /// 主题管理器，负责处理应用主题的切换和管理
    /// </summary>
    public class ThemeManager
    {
        /// <summary>
        /// 当前主题类型
        /// </summary>
        public static ThemeType CurrentTheme { get; private set; } = ThemeType.Light;

        /// <summary>
        /// 颜色常量类，集中管理所有主题颜色
        /// </summary>
        private static class ColorConstants
        {
            #region 亮色主题颜色
            /// <summary>
            /// 亮色主题背景色
            /// </summary>
            public static readonly Color LightBackground = Color.FromRgb(248, 249, 250);

            /// <summary>
            /// 亮色主题主文本色
            /// </summary>
            public static readonly Color LightText = Color.FromRgb(30, 30, 30);

            /// <summary>
            /// 亮色主题次要文本色
            /// </summary>
            public static readonly Color LightSecondaryText = Color.FromRgb(80, 80, 80);

            /// <summary>
            /// 亮色主题禁用文本色
            /// </summary>
            public static readonly Color LightDisabledText = Color.FromRgb(160, 160, 160);

            /// <summary>
            /// 亮色主题卡片背景色
            /// </summary>
            public static readonly Color LightCardBackground = Color.FromRgb(255, 255, 255);

            /// <summary>
            /// 亮色主题卡片阴影色
            /// </summary>
            public static readonly Color LightCardShadow = Color.FromRgb(200, 200, 200);

            /// <summary>
            /// 亮色主题边框色
            /// </summary>
            public static readonly Color LightBorder = Color.FromRgb(220, 220, 220);

            /// <summary>
            /// 亮色主题导航栏背景色
            /// </summary>
            public static readonly Color LightNavBackground = Color.FromRgb(230, 232, 235);
            #endregion

            #region 暗色主题颜色
            /// <summary>
            /// 暗色主题背景色
            /// </summary>
            public static readonly Color DarkBackground = Color.FromRgb(17, 24, 39);

            /// <summary>
            /// 暗色主题主文本色
            /// </summary>
            public static readonly Color DarkText = Color.FromRgb(243, 244, 246);

            /// <summary>
            /// 暗色主题次要文本色
            /// </summary>
            public static readonly Color DarkSecondaryText = Color.FromRgb(156, 163, 175);

            /// <summary>
            /// 暗色主题禁用文本色
            /// </summary>
            public static readonly Color DarkDisabledText = Color.FromRgb(107, 114, 128);

            /// <summary>
            /// 暗色主题卡片背景色
            /// </summary>
            public static readonly Color DarkCardBackground = Color.FromRgb(31, 41, 55);

            /// <summary>
            /// 暗色主题卡片阴影色
            /// </summary>
            public static readonly Color DarkCardShadow = Color.FromRgb(0, 0, 0);

            /// <summary>
            /// 暗色主题边框色
            /// </summary>
            public static readonly Color DarkBorder = Color.FromRgb(55, 65, 81);

            /// <summary>
            /// 暗色主题导航栏背景色
            /// </summary>
            public static readonly Color DarkNavBackground = Color.FromRgb(31, 41, 55);
            #endregion

            #region 通用颜色
            /// <summary>
            /// 主色调（亮色主题）
            /// </summary>
            public static readonly Color PrimaryColor = Color.FromRgb(37, 99, 235);

            /// <summary>
            /// 主色调悬停色（亮色主题）
            /// </summary>
            public static readonly Color PrimaryHover = Color.FromRgb(29, 78, 216);

            /// <summary>
            /// 强调色（亮色主题）
            /// </summary>
            public static readonly Color AccentColor = Color.FromRgb(16, 185, 129);

            /// <summary>
            /// 错误色（亮色主题）
            /// </summary>
            public static readonly Color ErrorColor = Color.FromRgb(239, 68, 68);

            /// <summary>
            /// 成功色（亮色主题）
            /// </summary>
            public static readonly Color SuccessColor = Color.FromRgb(16, 185, 129);

            /// <summary>
            /// 警告色
            /// </summary>
            public static readonly Color WarningColor = Color.FromRgb(245, 158, 11);

            /// <summary>
            /// 信息色（亮色主题）
            /// </summary>
            public static readonly Color InfoColor = Color.FromRgb(37, 99, 235);
            #endregion

            #region 暗色主题变体颜色
            /// <summary>
            /// 主色调（暗色主题）
            /// </summary>
            public static readonly Color DarkPrimaryColor = Color.FromRgb(59, 130, 246);

            /// <summary>
            /// 主色调悬停色（暗色主题）
            /// </summary>
            public static readonly Color DarkPrimaryHover = Color.FromRgb(37, 99, 235);

            /// <summary>
            /// 强调色（暗色主题）
            /// </summary>
            public static readonly Color DarkAccentColor = Color.FromRgb(5, 150, 105);

            /// <summary>
            /// 错误色（暗色主题）
            /// </summary>
            public static readonly Color DarkErrorColor = Color.FromRgb(239, 68, 68);

            /// <summary>
            /// 成功色（暗色主题）
            /// </summary>
            public static readonly Color DarkSuccessColor = Color.FromRgb(16, 185, 129);

            /// <summary>
            /// 信息色（暗色主题）
            /// </summary>
            public static readonly Color DarkInfoColor = Color.FromRgb(59, 130, 246);
            #endregion
        }

        /// <summary>
        /// 初始化主题管理器
        /// </summary>
        public static void Initialize()
        {
            // 加载保存的主题设置
            CurrentTheme = ThemeSettings.LoadTheme();
            // 立即应用主题（基础资源设置）
            ApplyThemeCore(CurrentTheme);
            // 注册应用启动完成事件，确保所有窗口都已加载后再次应用主题
            Application.Current.Startup += (sender, e) =>
            {
                // 延迟一点时间确保所有窗口都已完全加载
                System.Threading.Tasks.Task.Delay(100).ContinueWith(_ =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ApplyTheme(CurrentTheme);
                    });
                });
            };
        }

        /// <summary>
        /// 应用指定的主题
        /// </summary>
        /// <param name="themeType">主题类型</param>
        public static void ApplyTheme(ThemeType themeType)
        {
            CurrentTheme = themeType;

            // 应用主题并添加动画效果
            ApplyThemeWithAnimation(themeType);

            // 保存主题设置
            ThemeSettings.SaveTheme(themeType);
        }

        /// <summary>
        /// 应用主题并添加动画效果
        /// </summary>
        /// <param name="themeType">主题类型</param>
        private static void ApplyThemeWithAnimation(ThemeType themeType)
        {
            // 应用主题（无论是否有主窗口）
            ApplyThemeCore(themeType);

            // 获取主窗口并添加动画效果
            var mainWindow = Application.Current.MainWindow;
            if (mainWindow != null)
            {
                // 创建淡入淡出动画
                var fadeOutAnimation = new System.Windows.Media.Animation.DoubleAnimation
                {
                    From = 1.0,
                    To = 0.7,
                    Duration = new Duration(TimeSpan.FromMilliseconds(200))
                };

                var fadeInAnimation = new System.Windows.Media.Animation.DoubleAnimation
                {
                    From = 0.7,
                    To = 1.0,
                    Duration = new Duration(TimeSpan.FromMilliseconds(300))
                };

                // 先淡出
                fadeOutAnimation.Completed += (sender, e) =>
                {
                    // 再淡入
                    mainWindow.BeginAnimation(UIElement.OpacityProperty, fadeInAnimation);
                };

                mainWindow.BeginAnimation(UIElement.OpacityProperty, fadeOutAnimation);
            }
        }

        /// <summary>
        /// 核心主题应用逻辑
        /// </summary>
        /// <param name="themeType">主题类型</param>
        private static void ApplyThemeCore(ThemeType themeType)
        {
            switch (themeType)
            {
                case ThemeType.System:
                    ApplySystemTheme();
                    break;
                case ThemeType.Light:
                    ApplyLightTheme();
                    break;
                case ThemeType.Dark:
                    ApplyDarkTheme();
                    break;
            }
        }

        /// <summary>
        /// 应用亮色主题
        /// </summary>
        private static void ApplyLightTheme()
        {
            if (Application.Current == null) return;

            // 亮色主题颜色定义
            SetResource("ApplicationBackground", Color.FromArgb(200, 248, 249, 250)); // 半透明背景
            SetResource("TextForeground", ColorConstants.LightText);
            SetResource("TextSecondary", ColorConstants.LightSecondaryText);
            SetResource("TextDisabled", ColorConstants.LightDisabledText);
            SetResource("CardBackground", Color.FromArgb(220, 255, 255, 255)); // 半透明卡片背景
            SetResource("CardShadow", ColorConstants.LightCardShadow);
            SetResource("BorderColor", ColorConstants.LightBorder);
            SetResource("PrimaryColor", ColorConstants.PrimaryColor);
            SetResource("PrimaryHover", ColorConstants.PrimaryHover);
            SetResource("AccentColor", ColorConstants.AccentColor);
            SetResource("ErrorColor", ColorConstants.ErrorColor);
            SetResource("SuccessColor", ColorConstants.SuccessColor);
            SetResource("WarningColor", ColorConstants.WarningColor);
            SetResource("InfoColor", ColorConstants.InfoColor);

            // 设置系统颜色资源
            SetResource("SystemControlBackgroundAltHighBrush", ColorConstants.LightNavBackground);
            SetResource("SystemControlForegroundBaseHighBrush", ColorConstants.LightText);
            SetResource("SystemControlHighlightBrush", ColorConstants.PrimaryColor);
            SetResource("SystemControlHighlightAltBrush", Color.FromRgb(230, 242, 255));

            // 导航栏相关颜色资源 - 亮色模式下使用透明背景
            SetResource("NavigationViewDefaultPaneBackground", Color.FromArgb(0, 248, 249, 250)); // 完全透明背景
            SetResource("NavigationViewExpandedPaneBackground", Color.FromArgb(0, 248, 249, 250)); // 完全透明背景
            SetResource("NavigationViewDefaultPaneBorderBrush", ColorConstants.LightBorder);
            SetResource("NavigationViewItemForeground", ColorConstants.LightText);
            SetResource("NavigationViewItemHoverForeground", ColorConstants.PrimaryColor);
            SetResource("NavigationViewItemSelectedForeground", ColorConstants.PrimaryColor);

            // 设置iNKORE控件库主题为亮色
            SetINKORETheme("Light");

            // 强制更新所有窗口
            UpdateAllWindows();
        }

        /// <summary>
        /// 应用暗色主题
        /// </summary>
        private static void ApplyDarkTheme()
        {
            if (Application.Current == null) return;

            // 暗色主题颜色定义
            SetResource("ApplicationBackground", Color.FromArgb(200, 17, 24, 39)); // 半透明背景
            SetResource("TextForeground", ColorConstants.DarkText);
            SetResource("TextSecondary", ColorConstants.DarkSecondaryText);
            SetResource("TextDisabled", ColorConstants.DarkDisabledText);
            SetResource("CardBackground", Color.FromArgb(220, 31, 41, 55)); // 半透明卡片背景
            SetResource("CardShadow", ColorConstants.DarkCardShadow);
            SetResource("BorderColor", ColorConstants.DarkBorder);
            SetResource("PrimaryColor", ColorConstants.DarkPrimaryColor);
            SetResource("PrimaryHover", ColorConstants.DarkPrimaryHover);
            SetResource("AccentColor", ColorConstants.DarkAccentColor);
            SetResource("ErrorColor", ColorConstants.DarkErrorColor);
            SetResource("SuccessColor", ColorConstants.DarkSuccessColor);
            SetResource("WarningColor", ColorConstants.WarningColor);
            SetResource("InfoColor", ColorConstants.DarkInfoColor);

            // 设置系统颜色资源
            SetResource("SystemControlBackgroundAltHighBrush", ColorConstants.DarkNavBackground);
            SetResource("SystemControlForegroundBaseHighBrush", ColorConstants.DarkText);
            SetResource("SystemControlHighlightBrush", ColorConstants.DarkPrimaryColor);
            SetResource("SystemControlHighlightAltBrush", Color.FromRgb(26, 55, 87));

            // 导航栏相关颜色资源 - 暗色模式下使用半透明背景
            SetResource("NavigationViewDefaultPaneBackground", Color.FromArgb(180, 31, 41, 55)); // 半透明背景
            SetResource("NavigationViewExpandedPaneBackground", Color.FromArgb(180, 31, 41, 55)); // 半透明背景
            SetResource("NavigationViewDefaultPaneBorderBrush", ColorConstants.DarkBorder);
            SetResource("NavigationViewItemForeground", ColorConstants.DarkText);
            SetResource("NavigationViewItemHoverForeground", ColorConstants.DarkPrimaryColor);
            SetResource("NavigationViewItemSelectedForeground", ColorConstants.DarkPrimaryColor);

            // 设置iNKORE控件库主题为暗色
            SetINKORETheme("Dark");

            // 强制更新所有窗口
            UpdateAllWindows();
        }

        /// <summary>
        /// 设置资源颜色的辅助方法
        /// </summary>
        /// <param name="resourceKey">资源键名</param>
        /// <param name="color">颜色值</param>
        private static void SetResource(string resourceKey, Color color)
        {
            Application.Current.Resources[resourceKey] = new SolidColorBrush(color);
        }

        /// <summary>
        /// 强制更新所有窗口
        /// </summary>
        private static void UpdateAllWindows()
        {
            try
            {
                foreach (var window in Application.Current.Windows)
                {
                    if (window is Window w)
                    {
                        // 强制窗口重绘
                        w.InvalidateVisual();
                        w.UpdateLayout();

                        // 递归更新所有子控件
                        UpdateControls(w);
                    }
                }
            }
            catch (Exception)
            {
                // 忽略错误，确保主题切换功能不受影响
            }
        }

        /// <summary>
        /// 递归更新控件
        /// </summary>
        /// <param name="element">控件元素</param>
        private static void UpdateControls(DependencyObject element)
        {
            try
            {
                if (element == null) return;

                // 强制控件重绘
                if (element is UIElement uiElement)
                {
                    uiElement.InvalidateVisual();
                    uiElement.UpdateLayout();
                }

                // 递归更新子控件
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
                {
                    var child = VisualTreeHelper.GetChild(element, i);
                    UpdateControls(child);
                }
            }
            catch (Exception)
            {
                // 忽略错误，确保主题切换功能不受影响
            }
        }

        /// <summary>
        /// 设置iNKORE控件库主题
        /// </summary>
        /// <param name="themeName">主题名称：Light 或 Dark</param>
        private static void SetINKORETheme(string themeName)
        {
            try
            {
                // 方法1：尝试直接设置Application.Current的RequestedTheme属性
                var applicationType = Application.Current.GetType();
                var requestedThemeProperty = applicationType.GetProperty("RequestedTheme");
                if (requestedThemeProperty != null)
                {
                    // 尝试获取Theme枚举类型
                    var themeType = Type.GetType("iNKORE.UI.WPF.Modern.Themes.ElementTheme, iNKORE.UI.WPF.Modern");
                    if (themeType != null)
                    {
                        // 查找对应的枚举值
                        var themeValue = Enum.Parse(themeType, themeName);
                        requestedThemeProperty.SetValue(Application.Current, themeValue);
                    }
                }

                // 方法2：尝试更新现有的ThemeResources
                var themeResourcesType = Type.GetType("iNKORE.UI.WPF.Modern.Themes.ThemeResources, iNKORE.UI.WPF.Modern");
                if (themeResourcesType != null)
                {
                    var resources = Application.Current.Resources;
                    if (resources != null)
                    {
                        // 查找并更新现有的ThemeResources
                        foreach (var resource in resources.MergedDictionaries)
                        {
                            if (resource.GetType() == themeResourcesType)
                            {
                                var themeResourcesRequestedThemeProperty = themeResourcesType.GetProperty("RequestedTheme");
                                if (themeResourcesRequestedThemeProperty != null)
                                {
                                    var themeType = Type.GetType("iNKORE.UI.WPF.Modern.Themes.ElementTheme, iNKORE.UI.WPF.Modern");
                                    if (themeType != null)
                                    {
                                        var themeValue = Enum.Parse(themeType, themeName);
                                        themeResourcesRequestedThemeProperty.SetValue(resource, themeValue);
                                    }
                                }
                                break;
                            }
                        }
                    }
                }

                // 方法3：直接设置导航视图的背景色
                UpdateNavigationViewTheme(themeName);
            }
            catch (Exception)
            {
                // 发生错误时忽略，确保主题切换功能不受影响
            }
        }

        /// <summary>
        /// 更新导航视图的主题
        /// </summary>
        /// <param name="themeName">主题名称：Light 或 Dark</param>
        private static void UpdateNavigationViewTheme(string themeName)
        {
            try
            {
                // 遍历所有窗口
                foreach (var window in Application.Current.Windows)
                {
                    if (window is Window w)
                    {
                        // 查找NavigationView控件
                        var navView = FindNavigationView(w);
                        if (navView == null) continue;

                        // 设置导航视图的背景色
                        var backgroundColor = themeName == "Light" ?
                            new SolidColorBrush(Color.FromArgb(0, 248, 249, 250)) : // 完全透明背景
                            new SolidColorBrush(ColorConstants.DarkNavBackground);

                        // 使用反射设置背景色
                        var backgroundProperty = navView.GetType().GetProperty("Background");
                        backgroundProperty?.SetValue(navView, backgroundColor);

                        // 设置导航视图的前景色
                        var foregroundColor = themeName == "Light" ?
                            new SolidColorBrush(ColorConstants.LightText) :
                            new SolidColorBrush(ColorConstants.DarkText);

                        var foregroundProperty = navView.GetType().GetProperty("Foreground");
                        foregroundProperty?.SetValue(navView, foregroundColor);

                        // 设置导航视图的边框色
                        var borderColor = themeName == "Light" ?
                            new SolidColorBrush(ColorConstants.LightBorder) :
                            new SolidColorBrush(ColorConstants.DarkBorder);

                        var borderBrushProperty = navView.GetType().GetProperty("BorderBrush");
                        borderBrushProperty?.SetValue(navView, borderColor);

                        // 强制更新控件
                        if (navView is UIElement uiElement)
                        {
                            uiElement.InvalidateVisual();
                            uiElement.UpdateLayout();
                        }
                    }
                }
            }
            catch (Exception)
            {
                // 发生错误时忽略，确保主题切换功能不受影响
            }
        }

        /// <summary>
        /// 查找导航视图控件
        /// </summary>
        /// <param name="element">起始元素</param>
        /// <returns>找到的导航视图控件，或null</returns>
        private static object? FindNavigationView(DependencyObject element)
        {
            try
            {
                // 检查当前元素
                if (element == null) return null;

                var elementType = element.GetType();
                if (elementType.FullName != null && elementType.FullName.Contains("NavigationView"))
                {
                    return element;
                }

                // 递归查找子元素
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
                {
                    var child = VisualTreeHelper.GetChild(element, i);
                    var result = FindNavigationView(child);
                    if (result != null)
                    {
                        return result;
                    }
                }
            }
            catch (Exception)
            {
                // 发生错误时返回null
            }
            return null;
        }

        /// <summary>
        /// 应用系统主题
        /// </summary>
        private static void ApplySystemTheme()
        {
            if (IsSystemDarkTheme())
            {
                ApplyDarkTheme();
            }
            else
            {
                ApplyLightTheme();
            }
        }

        /// <summary>
        /// 检测系统是否使用暗色主题
        /// </summary>
        /// <returns>如果系统使用暗色主题返回true，否则返回false</returns>
        public static bool IsSystemDarkTheme()
        {
            try
            {
                // 读取系统主题设置
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                if (key == null) return false;

                var value = key.GetValue("AppsUseLightTheme");
                if (value == null) return false;

                // 0表示暗色主题，1表示亮色主题
                return (int)value == 0;
            }
            catch (Exception)
            {
                // 发生错误时默认返回false（亮色主题）
                return false;
            }
        }

        /// <summary>
        /// 获取当前系统主题类型
        /// </summary>
        /// <returns>系统主题类型</returns>
        public static ThemeType GetSystemThemeType()
        {
            return IsSystemDarkTheme() ? ThemeType.Dark : ThemeType.Light;
        }
    }

    /// <summary>
    /// 主题设置，负责主题设置的持久化存储
    /// </summary>
    public static class ThemeSettings
    {
        /// <summary>
        /// 设置文件名称
        /// </summary>
        private const string SettingsFile = "appSettings.json";

        /// <summary>
        /// 加载保存的主题设置
        /// </summary>
        /// <returns>保存的主题类型，如果没有保存的设置则返回Light</returns>
        public static ThemeType LoadTheme()
        {
            try
            {
                var settings = LoadSettings();
                return settings?.Theme?.Type ?? ThemeType.Light;
            }
            catch (Exception)
            {
                // 发生错误时返回默认主题
                return ThemeType.Light;
            }
        }

        /// <summary>
        /// 保存主题设置
        /// </summary>
        /// <param name="themeType">要保存的主题类型</param>
        public static void SaveTheme(ThemeType themeType)
        {
            try
            {
                var settings = LoadSettings();
                settings.Theme ??= new ThemeConfig();
                settings.Theme.Type = themeType;
                settings.App.LastUpdated = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                SaveSettings(settings);
            }
            catch (Exception)
            {
                // 发生错误时忽略，不影响应用运行
            }
        }

        /// <summary>
        /// 保存动画速度设置
        /// </summary>
        /// <param name="birthdaySpeed">生日成就动画速度（秒）</param>
        /// <param name="anniversarySpeed">周年成就动画速度（秒）</param>
        public static void SaveAnimationSpeed(double birthdaySpeed, double anniversarySpeed)
        {
            try
            {
                var settings = LoadSettings();
                settings.Theme ??= new ThemeConfig();
                settings.Theme.BirthdayAnimationSpeed = birthdaySpeed;
                settings.Theme.AnniversaryAnimationSpeed = anniversarySpeed;
                settings.App.LastUpdated = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                SaveSettings(settings);
            }
            catch (Exception)
            {
                // 发生错误时忽略，不影响应用运行
            }
        }

        /// <summary>
        /// 加载动画速度设置
        /// </summary>
        /// <returns>动画速度设置（生日速度，周年速度）</returns>
        public static (double, double) LoadAnimationSpeed()
        {
            try
            {
                var settings = LoadSettings();
                return (settings?.Theme?.BirthdayAnimationSpeed ?? 90, settings?.Theme?.AnniversaryAnimationSpeed ?? 90);
            }
            catch (Exception)
            {
                // 发生错误时返回默认值
                return (90, 90);
            }
        }

        /// <summary>
        /// 保存成就图片路径设置
        /// </summary>
        /// <param name="imagePath">成就图片路径</param>
        public static void SaveAchievementsImagePath(string imagePath)
        {
            try
            {
                var settings = LoadSettings();
                settings.App ??= new AppConfig();
                settings.App.AchievementsImagePath = imagePath;
                settings.App.LastUpdated = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                SaveSettings(settings);
            }
            catch (Exception)
            {
                // 发生错误时忽略，不影响应用运行
            }
        }

        /// <summary>
        /// 加载成就图片路径设置
        /// </summary>
        /// <returns>成就图片路径，如果没有设置则返回空字符串</returns>
        public static string LoadAchievementsImagePath()
        {
            try
            {
                var settings = LoadSettings();
                return settings?.App?.AchievementsImagePath ?? string.Empty;
            }
            catch (Exception)
            {
                // 发生错误时返回空字符串
                return string.Empty;
            }
        }

        /// <summary>
        /// 保存ADB工具路径设置
        /// </summary>
        /// <param name="adbPath">ADB工具路径</param>
        public static void SaveAdbToolPath(string adbPath)
        {
            try
            {
                var settings = LoadSettings();
                settings.App ??= new AppConfig();
                settings.App.AdbToolPath = adbPath;
                settings.App.LastUpdated = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                SaveSettings(settings);
            }
            catch (Exception)
            {
                // 发生错误时忽略，不影响应用运行
            }
        }

        /// <summary>
        /// 加载ADB工具路径设置
        /// </summary>
        /// <returns>ADB工具路径，如果没有设置则返回空字符串</returns>
        public static string LoadAdbToolPath()
        {
            try
            {
                var settings = LoadSettings();
                return settings?.App?.AdbToolPath ?? string.Empty;
            }
            catch (Exception)
            {
                // 发生错误时返回空字符串
                return string.Empty;
            }
        }

        /// <summary>
        /// 保存分页大小设置
        /// </summary>
        /// <param name="pageSize">分页大小</param>
        public static void SavePageSize(int pageSize)
        {
            try
            {
                var settings = LoadSettings();
                settings.App ??= new AppConfig();
                settings.App.PageSize = pageSize;
                settings.App.LastUpdated = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                SaveSettings(settings);
            }
            catch (Exception)
            {
                // 发生错误时忽略，不影响应用运行
            }
        }

        /// <summary>
        /// 加载分页大小设置
        /// </summary>
        /// <returns>分页大小，如果没有设置则返回默认值20</returns>
        public static int LoadPageSize()
        {
            try
            {
                var settings = LoadSettings();
                return settings?.App?.PageSize ?? 20;
            }
            catch (Exception)
            {
                // 发生错误时返回默认值
                return 20;
            }
        }

        /// <summary>
        /// 保存相机优先级设置
        /// </summary>
        /// <param name="cameraPriority">相机优先级设置</param>
        public static void SaveCameraPriority(string cameraPriority)
        {
            try
            {
                var settings = LoadSettings();
                settings.App ??= new AppConfig();
                settings.App.CameraPriority = cameraPriority;
                settings.App.LastUpdated = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                SaveSettings(settings);
            }
            catch (Exception)
            {
                // 发生错误时忽略，不影响应用运行
            }
        }

        /// <summary>
        /// 加载相机优先级设置
        /// </summary>
        /// <returns>相机优先级设置，如果没有设置则返回默认值Adb</returns>
        public static string LoadCameraPriority()
        {
            try
            {
                var settings = LoadSettings();
                return settings?.App?.CameraPriority ?? "Adb";
            }
            catch (Exception)
            {
                // 发生错误时返回默认值
                return "Adb";
            }
        }

        /// <summary>
        /// 加载数据库配置
        /// </summary>
        /// <returns>数据库配置，如果没有保存的配置则返回默认配置</returns>
        public static DatabaseConfig LoadDatabaseConfig()
        {
            try
            {
                var settings = LoadSettings();
                return settings?.Database ?? GetDefaultDatabaseConfig();
            }
            catch (Exception)
            {
                // 发生错误时返回默认配置
                return GetDefaultDatabaseConfig();
            }
        }

        /// <summary>
        /// 保存数据库配置
        /// </summary>
        /// <param name="config">要保存的数据库配置</param>
        public static void SaveDatabaseConfig(DatabaseConfig config)
        {
            try
            {
                var settings = LoadSettings();
                settings.Database = config;
                settings.App.LastUpdated = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                SaveSettings(settings);
            }
            catch (Exception)
            {
                // 发生错误时忽略，不影响应用运行
            }
        }

        /// <summary>
        /// 加载所有设置
        /// </summary>
        /// <returns>所有设置，如果没有保存的设置则返回默认设置</returns>
        private static AppSettingsModel LoadSettings()
        {
            try
            {
                string settingsPath = GetSettingsFilePath();

                if (!System.IO.File.Exists(settingsPath))
                {
                    // 首次运行，获取默认设置（会从 App.config 读取）
                    var defaultSettings = GetDefaultSettings();
                    // 保存到 appSettings.json 文件中
                    SaveSettings(defaultSettings);
                    return defaultSettings;
                }

                string json = System.IO.File.ReadAllText(settingsPath);
                var settings = System.Text.Json.JsonSerializer.Deserialize<AppSettingsModel>(json);
                return settings ?? GetDefaultSettings();
            }
            catch (Exception)
            {
                // 发生错误时返回默认设置
                return GetDefaultSettings();
            }
        }

        /// <summary>
        /// 获取配置文件路径
        /// </summary>
        /// <returns>配置文件的完整路径</returns>
        public static string GetSettingsFilePath()
        {
            return System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "成就系统",
                SettingsFile
            );
        }

        /// <summary>
        /// 保存所有设置
        /// </summary>
        /// <param name="settings">要保存的设置</param>
        private static void SaveSettings(AppSettingsModel settings)
        {
            try
            {
                string settingsDir = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "成就系统"
                );

                // 确保目录存在
                if (!System.IO.Directory.Exists(settingsDir))
                {
                    System.IO.Directory.CreateDirectory(settingsDir);
                }

                string settingsPath = GetSettingsFilePath();
                string json = System.Text.Json.JsonSerializer.Serialize(settings, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                System.IO.File.WriteAllText(settingsPath, json);
            }
            catch (Exception)
            {
                // 发生错误时忽略，不影响应用运行
            }
        }

        /// <summary>
        /// 获取默认设置
        /// </summary>
        /// <returns>默认设置</returns>
        private static AppSettingsModel GetDefaultSettings()
        {
            return new AppSettingsModel
            {
                Theme = new ThemeConfig
                {
                    Type = ThemeType.Light
                },
                Database = GetDefaultDatabaseConfig(),
                App = new AppConfig
                {
                    Version = "1.0.0",
                    LastUpdated = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                }
            };
        }

        /// <summary>
        /// 获取默认数据库配置
        /// </summary>
        /// <returns>默认数据库配置</returns>
        private static DatabaseConfig GetDefaultDatabaseConfig()
        {
            // 尝试从 App.config 文件中读取数据库配置
            try
            {
                // 直接使用项目中的 Config/App.config 文件路径
                string configPath = System.IO.Path.Combine(
                    System.IO.Directory.GetCurrentDirectory(),
                    "Config",
                    "App.config"
                );

                // 输出路径信息用于调试
                System.Diagnostics.Debug.WriteLine($"尝试读取 App.config 文件: {configPath}");
                System.Diagnostics.Debug.WriteLine($"文件是否存在: {System.IO.File.Exists(configPath)}");

                if (System.IO.File.Exists(configPath))
                {
                    // 直接读取并解析 XML 文件
                    var xmlDoc = new System.Xml.XmlDocument();
                    xmlDoc.Load(configPath);

                    // 输出 XML 内容用于调试
                    System.Diagnostics.Debug.WriteLine($"App.config 文件内容: {xmlDoc.InnerXml}");

                    var connectionStringNode = xmlDoc.SelectSingleNode("/configuration/connectionStrings/add[@name='MySQLConnection']");
                    if (connectionStringNode != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"找到 MySQLConnection 节点");

                        if (connectionStringNode.Attributes != null)
                        {
                            var connectionStringAttr = connectionStringNode.Attributes["connectionString"];
                            if (connectionStringAttr != null)
                            {
                                var connectionString = connectionStringAttr.Value;
                                System.Diagnostics.Debug.WriteLine($"连接字符串: {connectionString}");

                                if (!string.IsNullOrEmpty(connectionString))
                                {
                                    // 解析连接字符串
                                    var dbConfig = ParseConnectionString(connectionString);
                                    System.Diagnostics.Debug.WriteLine($"解析结果 - Server: {dbConfig.Server}, Database: {dbConfig.Database}, Username: {dbConfig.Username}, Password: {dbConfig.Password}");
                                    return dbConfig;
                                }
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine("MySQLConnection 节点没有 connectionString 属性");
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("MySQLConnection 节点没有 Attributes");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("未找到 MySQLConnection 节点");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"App.config 文件不存在: {configPath}");

                    // 尝试其他可能的路径
                    string currentDir = System.IO.Directory.GetCurrentDirectory();
                    string assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly()?.Location;
                    string assemblyDir = assemblyLocation != null ? System.IO.Path.GetDirectoryName(assemblyLocation) : null;

                    List<string> pathsList = [];
                    if (currentDir != null)
                    {
                        pathsList.Add(System.IO.Path.Combine(currentDir, "App.config"));
                    }
                    if (assemblyDir != null)
                    {
                        pathsList.Add(System.IO.Path.Combine(assemblyDir, "App.config"));
                        pathsList.Add(System.IO.Path.Combine(assemblyDir, "Config", "App.config"));
                    }

                    string[] possiblePaths = [.. pathsList];

                    foreach (var path in possiblePaths)
                    {
                        System.Diagnostics.Debug.WriteLine($"尝试其他路径: {path}, 存在: {System.IO.File.Exists(path)}");
                    }
                }
            }
            catch (Exception ex)
            {
                // 发生错误时使用硬编码的默认配置
                System.Diagnostics.Debug.WriteLine($"读取 App.config 失败: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"异常堆栈: {ex.StackTrace}");
            }

            // 默认配置
            System.Diagnostics.Debug.WriteLine("使用硬编码的默认数据库配置");
            return GetHardcodedDefaultDatabaseConfig();
        }

        /// <summary>
        /// 获取硬编码的默认数据库配置
        /// </summary>
        /// <returns>默认数据库配置</returns>
        private static DatabaseConfig GetHardcodedDefaultDatabaseConfig()
        {
            return new DatabaseConfig
            {
                Server = "localhost",
                Database = "achievement_system",
                Username = "root",
                Password = "123"
            };
        }

        /// <summary>
        /// 解析连接字符串
        /// </summary>
        /// <param name="connectionString">连接字符串</param>
        /// <returns>数据库配置</returns>
        private static DatabaseConfig ParseConnectionString(string connectionString)
        {
            try
            {
                var config = new DatabaseConfig();
                var parts = connectionString.Split(';');

                foreach (var part in parts)
                {
                    if (string.IsNullOrWhiteSpace(part)) continue;

                    var keyValue = part.Split('=');
                    if (keyValue.Length != 2) continue;

                    var key = keyValue[0].Trim().ToLower();
                    var value = keyValue[1].Trim();

                    switch (key)
                    {
                        case "server":
                            config.Server = value;
                            break;
                        case "database":
                            config.Database = value;
                            break;
                        case "uid":
                            config.Username = value;
                            break;
                        case "pwd":
                            config.Password = value;
                            break;
                    }
                }

                return config;
            }
            catch (Exception)
            {
                // 发生错误时返回硬编码的默认配置
                return GetHardcodedDefaultDatabaseConfig();
            }
        }

        /// <summary>
        /// 应用设置模型
        /// </summary>
        private class AppSettingsModel
        {
            /// <summary>
            /// 主题配置
            /// </summary>
            public ThemeConfig Theme { get; set; } = new ThemeConfig();

            /// <summary>
            /// 数据库配置
            /// </summary>
            public DatabaseConfig Database { get; set; } = new DatabaseConfig();

            /// <summary>
            /// 应用配置
            /// </summary>
            public AppConfig App { get; set; } = new AppConfig();
        }

        /// <summary>
        /// 主题配置模型
        /// </summary>
        private class ThemeConfig
        {
            /// <summary>
            /// 主题类型
            /// </summary>
            public ThemeType Type { get; set; } = ThemeType.Light;

            /// <summary>
            /// 生日成就动画速度（秒）
            /// </summary>
            public double BirthdayAnimationSpeed { get; set; } = 90;

            /// <summary>
            /// 周年成就动画速度（秒）
            /// </summary>
            public double AnniversaryAnimationSpeed { get; set; } = 90;
        }

        /// <summary>
        /// 数据库配置模型
        /// </summary>
        public class DatabaseConfig
        {
            /// <summary>
            /// 服务器地址
            /// </summary>
            public string Server { get; set; } = "localhost";

            /// <summary>
            /// 数据库名称
            /// </summary>
            public string Database { get; set; } = "achievement_system";

            /// <summary>
            /// 用户名
            /// </summary>
            public string Username { get; set; } = "root";

            /// <summary>
            /// 密码
            /// </summary>
            public string Password { get; set; } = "123";

            /// <summary>
            /// 用户表名称
            /// </summary>
            public string UsersTable { get; set; } = "users";

            /// <summary>
            /// 成就分类表名称
            /// </summary>
            public string CategoriesTable { get; set; } = "achievement_categories";

            /// <summary>
            /// 成就表名称
            /// </summary>
            public string AchievementsTable { get; set; } = "achievements";

            /// <summary>
            /// 获取连接字符串
            /// </summary>
            /// <returns>数据库连接字符串</returns>
            public string GetConnectionString()
            {
                return $"Server={Server};Database={Database};Uid={Username};Pwd={Password};CharSet=utf8;";
            }
        }

        /// <summary>
        /// 应用配置模型
        /// </summary>
        private class AppConfig
        {
            /// <summary>
            /// 应用版本
            /// </summary>
            public string Version { get; set; } = "2.0.0";

            /// <summary>
            /// 最后更新时间
            /// </summary>
            public string LastUpdated { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            /// <summary>
            /// 成就图片路径
            /// </summary>
            public string AchievementsImagePath { get; set; } = string.Empty;

            /// <summary>
            /// ADB工具路径
            /// </summary>
            public string AdbToolPath { get; set; } = string.Empty;

            /// <summary>
            /// 页面一次性展示成就数量
            /// </summary>
            public int PageSize { get; set; } = 20;

            /// <summary>
            /// 相机优先级设置
            /// </summary>
            public string CameraPriority { get; set; } = "Adb";
        }
    }
}