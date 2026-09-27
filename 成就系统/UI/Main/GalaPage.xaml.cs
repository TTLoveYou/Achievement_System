using iNKORE.UI.WPF.Modern.Media.Animation;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using 成就系统.Services;

namespace 成就系统.UI.Main
{
    public partial class GalaPage : Page
    {
        public ObservableCollection<AchievementCard> BirthdayAchievements { get; } = [];   //生日成就
        public ObservableCollection<AchievementCard> AnniversaryAchievements { get; } = [];    //周年成就

        public string UserName { get; }
        private Storyboard? _birthdayStoryboard = null;             //生日动画板
        private Storyboard? _anniversaryStoryboard = null;          //周年动画板
        private DoubleAnimation? _birthdayAnimation = null;         //生日动画
        private DoubleAnimation? _anniversaryAnimation = null;      //周年动画
        private double _birthdayContentWidth;               //生日区域宽度
        private double _anniversaryContentWidth;            //周年区域宽度
        private bool _isMouseOverBirthday;                  //生日区域是否有鼠标
        private bool _isMouseOverAnniversary;               //周年区域是否有鼠标

        // 动画速度属性
        public double BirthdayAnimationSpeed { get; set; } = 90; // 默认90秒
        public double AnniversaryAnimationSpeed { get; set; } = 90; // 默认90秒

        public GalaPage(string userName)
        {
            UserName = userName;
            InitializeComponent();
            DataContext = this;

            // 加载保存的动画速度设置
            LoadAnimationSpeedSettings();

            // 初始化动画
            InitializeAnimations();

            Loaded += async (s, e) => await LoadGalaAchievementsAsync();
        }

        /// <summary>
        /// 加载保存的动画速度设置
        /// </summary>
        private void LoadAnimationSpeedSettings()
        {
            try
            {
                var (birthdaySpeed, anniversarySpeed) = ThemeSettings.LoadAnimationSpeed();
                BirthdayAnimationSpeed = birthdaySpeed;
                AnniversaryAnimationSpeed = anniversarySpeed;
            }
            catch (Exception)
            {
                // 发生错误时使用默认值
                BirthdayAnimationSpeed = 90;
                AnniversaryAnimationSpeed = 90;
            }
        }

        //首次进入加载数据
        private async Task LoadGalaAchievementsAsync()
        {
            try
            {
                // 显示加载状态
                LoadingIndicator.Visibility = Visibility.Visible;
                BirthdayContainer.Visibility = Visibility.Collapsed;
                AnniversaryContainer.Visibility = Visibility.Collapsed;
                ErrorContainer.Visibility = Visibility.Collapsed;

                using (var achievementService = new AchievementService())
                {
                    // 并行加载两类成就
                    var birthdayTask = achievementService.GetBirthdayAchievementsAsync(UserName);
                    var anniversaryTask = achievementService.GetAnniversaryAchievementsAsync(UserName);

                    await Task.WhenAll(birthdayTask, anniversaryTask);

                    // 修复：直接使用任务结果
                    var birthdayResults = await birthdayTask;
                    var anniversaryResults = await anniversaryTask;

                    // 更新生日成就
                    BirthdayAchievements.Clear();
                    foreach (var card in birthdayResults)
                    {
                        // 确保所有必要属性都已初始化
                        if (card != null && !string.IsNullOrEmpty(card.AchievementName) && !string.IsNullOrEmpty(card.Description))
                        {
                            BirthdayAchievements.Add(card);
                        }
                    }

                    // 更新整周年成就
                    AnniversaryAchievements.Clear();
                    foreach (var card in anniversaryResults)
                    {
                        // 确保所有必要属性都已初始化
                        if (card != null && !string.IsNullOrEmpty(card.AchievementName) && !string.IsNullOrEmpty(card.Description))
                        {
                            AnniversaryAchievements.Add(card);
                        }
                    }
                }

                // UI线程更新
                Application.Current.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        // 更新UI状态
                        UpdateSectionVisibility(BirthdayAchievements, BirthdayContainer, NoBirthdayText);
                        UpdateSectionVisibility(AnniversaryAchievements, AnniversaryContainer, NoAnniversaryText);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"更新UI状态失败: {ex.Message}");
                    }
                });

                // 更新UI后检查是否启动轮播
                Application.Current.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        CheckAndStartScrolling();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"启动轮播失败: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"加载庆典成就失败: {ex.Message}");
                ErrorText.Text = $"加载成就失败: {ex.Message}";
                ErrorContainer.Visibility = Visibility.Visible;
            }
            finally
            {
                // UI线程更新
                Application.Current.Dispatcher.Invoke(() =>
                {
                    LoadingIndicator.Visibility = Visibility.Collapsed;
                });
            }
        }

        //更新显示
        private static void UpdateSectionVisibility(ObservableCollection<AchievementCard> achievements, FrameworkElement container, TextBlock noDataText)
        {
            if (achievements.Count > 0)
            {
                container.Visibility = Visibility.Visible;
                noDataText.Visibility = Visibility.Collapsed;
            }
            else
            {
                container.Visibility = Visibility.Collapsed;
                noDataText.Visibility = Visibility.Visible;
            }
        }

        // 打开成就详情
        private void AchievementCard_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Border border && border.DataContext is AchievementCard achievementCard)
                {
                    if (achievementCard != null && !string.IsNullOrEmpty(achievementCard.AchievementName))
                    {
                        var transition = new DrillInNavigationTransitionInfo();
                        NavigationService?.Navigate(
                            new AddAchievementPage(UserName, achievementCard),
                            transition
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"打开成就详情失败: {ex.Message}");
            }
        }

        // 刷新庆典数据
        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorContainer.Visibility = Visibility.Collapsed;
            await LoadGalaAchievementsAsync();
        }

        //初始化动画
        private void InitializeAnimations()
        {
            // 生日成就动画
            _birthdayAnimation = new DoubleAnimation
            {
                Duration = TimeSpan.FromSeconds(BirthdayAnimationSpeed),
                RepeatBehavior = RepeatBehavior.Forever
            };
            _birthdayStoryboard = new Storyboard();
            Storyboard.SetTargetProperty(_birthdayAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));
            _birthdayStoryboard.Children.Add(_birthdayAnimation);

            // 周年成就动画
            _anniversaryAnimation = new DoubleAnimation
            {
                Duration = TimeSpan.FromSeconds(AnniversaryAnimationSpeed),
                RepeatBehavior = RepeatBehavior.Forever
            };
            _anniversaryStoryboard = new Storyboard();
            Storyboard.SetTargetProperty(_anniversaryAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));
            _anniversaryStoryboard.Children.Add(_anniversaryAnimation);
        }

        // 更新动画速度
        public void UpdateAnimationSpeed(double birthdaySpeed, double anniversarySpeed)
        {
            BirthdayAnimationSpeed = birthdaySpeed;
            AnniversaryAnimationSpeed = anniversarySpeed;

            // 更新动画持续时间
            if (_birthdayAnimation != null)
            {
                _birthdayAnimation.Duration = TimeSpan.FromSeconds(birthdaySpeed);
            }

            if (_anniversaryAnimation != null)
            {
                _anniversaryAnimation.Duration = TimeSpan.FromSeconds(anniversarySpeed);
            }

            // 重新启动动画
            CheckAndStartScrolling();
        }

        // 检查并启动轮播
        private void CheckAndStartScrolling()
        {
            // 检查生日成就区域
            CheckAndStartScrollForSection(
                BirthdayContainer,
                BirthdayItemsControl,
                BirthdayAchievements.Count,
                _birthdayStoryboard,
                _birthdayAnimation,
                true);

            // 检查周年成就区域
            CheckAndStartScrollForSection(
                AnniversaryContainer,
                AnniversaryItemsControl,
                AnniversaryAchievements.Count,
                _anniversaryStoryboard,
                _anniversaryAnimation,
                false);
        }

        //动画逻辑
        private void CheckAndStartScrollForSection(
            FrameworkElement container,
            ItemsControl itemsControl,
            int itemCount,
            Storyboard? storyboard,
            DoubleAnimation? animation,
            bool isBirthdaySection)
        {
            try
            {
                if (itemCount <= 0 || itemsControl == null || storyboard == null || animation == null)
                {
                    storyboard?.Stop();
                    return;
                }

                // 等待UI渲染完成
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        // 计算内容总宽度
                        double contentWidth = CalculateContentWidth(itemsControl);

                        // 存储内容宽度
                        if (isBirthdaySection)
                        {
                            _birthdayContentWidth = contentWidth;
                        }
                        else
                        {
                            _anniversaryContentWidth = contentWidth;
                        }

                        // 获取容器宽度
                        double containerWidth = container.ActualWidth;

                        // 获取鼠标悬停状态
                        bool isMouseOver = isBirthdaySection ? _isMouseOverBirthday : _isMouseOverAnniversary;

                        // 如果成就内容宽度大于容器宽度，则启动轮播
                        if (contentWidth > containerWidth)
                        {
                            // 计算滚动距离（内容宽度 - 容器宽度）
                            double scrollDistance = contentWidth - containerWidth + 50; // 加50像素缓冲

                            // 设置动画
                            animation.From = 0;
                            animation.To = -scrollDistance;

                            // 设置动画目标
                            Storyboard.SetTarget(animation, itemsControl);

                            // 启动动画
                            if (!isMouseOver)
                            {
                                storyboard.Begin();
                            }
                        }
                        else
                        {
                            // 不需要滚动时重置位置
                            if (itemsControl.RenderTransform is TranslateTransform transform)
                            {
                                transform.X = 0;
                            }
                            storyboard.Stop();
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"动画逻辑执行失败: {ex.Message}");
                    }
                }), DispatcherPriority.Loaded);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"检查并启动滚动失败: {ex.Message}");
            }
        }

        // 计算内容总宽度
        private static double CalculateContentWidth(ItemsControl itemsControl)
        {
            try
            {
                double totalWidth = 0;

                // 遍历所有项目容器
                for (int i = 0; i < itemsControl.Items.Count; i++)
                {
                    if (itemsControl.ItemContainerGenerator.ContainerFromIndex(i) is FrameworkElement container)
                    {
                        // 确保容器已经测量
                        if (double.IsNaN(container.ActualWidth) || container.ActualWidth == 0)
                        {
                            container.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                            container.Arrange(new Rect(0, 0, container.DesiredSize.Width, container.DesiredSize.Height));
                        }
                        totalWidth += container.ActualWidth;
                    }
                }

                return totalWidth;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"计算内容宽度失败: {ex.Message}");
                return 0;
            }
        }

        // 鼠标进入事件处理
        private void ScrollViewer_MouseEnter(object sender, MouseEventArgs e)
        {
            try
            {
                if (sender == BirthdayContainer)
                {
                    _isMouseOverBirthday = true;
                    if (_birthdayStoryboard != null && _birthdayStoryboard.GetCurrentState() == ClockState.Active)
                    {
                        _birthdayStoryboard.Pause();
                    }
                }
                else if (sender == AnniversaryContainer)
                {
                    _isMouseOverAnniversary = true;
                    if (_anniversaryStoryboard != null && _anniversaryStoryboard.GetCurrentState() == ClockState.Active)
                    {
                        _anniversaryStoryboard.Pause();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"鼠标进入事件处理失败: {ex.Message}");
            }
        }

        // 鼠标离开事件处理
        private void ScrollViewer_MouseLeave(object sender, MouseEventArgs e)
        {
            try
            {
                if (sender == BirthdayContainer)
                {
                    _isMouseOverBirthday = false;
                    if (_birthdayStoryboard != null && BirthdayAchievements.Count > 0 && _birthdayContentWidth > BirthdayContainer.ActualWidth)
                    {
                        _birthdayStoryboard.Resume();
                    }
                }
                else if (sender == AnniversaryContainer)
                {
                    _isMouseOverAnniversary = false;
                    if (_anniversaryStoryboard != null && AnniversaryAchievements.Count > 0 && _anniversaryContentWidth > AnniversaryContainer.ActualWidth)
                    {
                        _anniversaryStoryboard.Resume();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"鼠标离开事件处理失败: {ex.Message}");
            }
        }

        // 页面大小变化时重新计算
        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            CheckAndStartScrolling();
        }

        // 页面卸载时停止所有动画
        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            _birthdayStoryboard?.Stop();
            _anniversaryStoryboard?.Stop();
        }

    }
}
