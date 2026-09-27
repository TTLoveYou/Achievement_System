using iNKORE.UI.WPF.Modern.Controls;
using iNKORE.UI.WPF.Modern.Media.Animation;
using System.Windows;
using 成就系统.UI.Login;

namespace 成就系统.UI.Main
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        // 新增：账户名属性
        public string UserName { get; set; }

        // 定义页面类型映射
        private readonly Dictionary<Type, string> _pageMap = new()
        {
            { typeof(HomePage), "Home_P" },
            { typeof(GalaPage), "Gala_P" },
            { typeof(ConsolePage), "Console_P" },
            { typeof(StatisticsPage), "Statistics_P" },
            { typeof(HelpPage), "Help_P" },
            { typeof(AboutPage), "About_P" },
            { typeof(SettingsPage), "Settings_P" }
        };

        // 定义反向映射用于导航
        private readonly Dictionary<string, Type> _tagMap = new()
        {
            { "Home_P", typeof(HomePage) },
            {  "Gala_P", typeof(GalaPage) },
            { "Console_P", typeof(ConsolePage) },
            { "Statistics_P", typeof(StatisticsPage) },
            { "Help_P", typeof(HelpPage) },
            { "About_P", typeof(AboutPage) },
            { "Settings_P", typeof(SettingsPage) }
        };

        public MainWindow(string userName)
        {
            UserName = userName; // 保存账户名
            InitializeComponent();

            // 设置账户名显示
            AccountName.Text = UserName;

            // 设置初始选中的导航项
            NavView.SelectedItem = NavView.MenuItems.OfType<NavigationViewItem>().FirstOrDefault(item => item.Tag?.ToString() == "home");

            // 初始加载首页
            NavigateTo(typeof(HomePage));
        }

        //导航点击事件
        private void OnNavigationSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItemContainer is NavigationViewItem selectedItem)
            {
                // 获取导航标签
                string? tag = selectedItem.Tag?.ToString();

                if (!string.IsNullOrEmpty(tag) && _tagMap.TryGetValue(tag, out Type pageType))
                {
                    // 获取当前页面类型
                    var currentPageType = MainContentFrame.Content?.GetType();

                    // 仅在不同页面间导航
                    if (currentPageType != pageType)
                    {
                        NavigateTo(pageType);
                    }
                }
            }
        }

        //导航页面
        private void NavigateTo(Type pageType)
        {
            // 特殊处理 HomePage，传递用户名参数
            if (pageType == typeof(HomePage))
            {
                var transition = new DrillInNavigationTransitionInfo();
                MainContentFrame.Navigate(new HomePage(UserName), string.Empty, transition);
            }
            else if (pageType == typeof(GalaPage))
            {
                var transition = new DrillInNavigationTransitionInfo();
                MainContentFrame.Navigate(new GalaPage(UserName), string.Empty, transition);
            }
            else if (pageType == typeof(ConsolePage))
            {
                var transition = new DrillInNavigationTransitionInfo();
                MainContentFrame.Navigate(new ConsolePage(UserName), string.Empty, transition);
            }
            else if (pageType == typeof(StatisticsPage))
            {
                var transition = new DrillInNavigationTransitionInfo();
                MainContentFrame.Navigate(new StatisticsPage(UserName), string.Empty, transition);
            }
            else if (pageType == typeof(AccountSettingsPage))
            {
                var transition = new DrillInNavigationTransitionInfo();
                MainContentFrame.Navigate(new AccountSettingsPage(UserName), string.Empty, transition);
            }
            else
            {
                // 使用 ModernUI Frame 提供的导航系统
                var transition = new DrillInNavigationTransitionInfo();
                MainContentFrame.Navigate(pageType, string.Empty, transition);
            }

            // 更新导航视图状态
            UpdateNavigationViewSelection(pageType);
        }

        // 更新导航视图选择状态
        private void UpdateNavigationViewSelection(Type pageType)
        {
            if (_pageMap.TryGetValue(pageType, out string? tag))
            {
                // 查找匹配的导航项
                var items = NavView.MenuItems.OfType<NavigationViewItem>()
                                    .Concat(NavView.FooterMenuItems.OfType<NavigationViewItem>());

                foreach (var item in items)
                {
                    if (item.Tag?.ToString() == tag)
                    {
                        NavView.SelectedItem = item;
                        break;
                    }
                }
            }
        }

        // 账户设置菜单点击事件
        private void OnAccountSettingsMenuItemClick(object sender, RoutedEventArgs e)
        {
            // 导航到账户设置页面
            NavigateTo(typeof(AccountSettingsPage));
        }

        // 退出菜单点击事件
        private void OnExitMenuItemClick(object sender, RoutedEventArgs e)
        {
            // 创建登录窗口
            var loginWindow = new LoginWindow();

            // 显示登录窗口
            loginWindow.Show();

            // 关闭当前主窗口
            this.Close();
        }

    }
}
