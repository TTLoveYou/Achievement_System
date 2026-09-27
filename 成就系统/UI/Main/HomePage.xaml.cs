using iNKORE.UI.WPF.Modern.Media.Animation;
using Microsoft.Win32;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using 成就系统.Config;
using 成就系统.Services;
using SearchReplace = iNKORE.UI.WPF.Modern.Controls;

namespace 成就系统.UI.Main
{
    /// <summary>
    /// HomePage.xaml 的交互逻辑
    /// </summary>
    public partial class HomePage : Page
    {
        // 新增：用户名属性
        public string UserName { get; set; } = string.Empty;

        // 新增：加载状态指示器
        private ProgressBar? _loadingIndicator = null;

        // 新增：排序状态枚举
        private enum SortState
        {
            Unsorted,
            Ascending,
            Descending
        }

        // 新增：当前排序状态字典
        private readonly Dictionary<string, SortState> _sortStates = new()
        {
            { "Name", SortState.Unsorted },
            { "Rating", SortState.Unsorted },
            { "Progress", SortState.Unsorted },
            { "Date", SortState.Unsorted }
        };

        // 新增：当前激活的排序字段
        private string _activeSortField = "";
        // 新增：当前排序方向
        private bool _isSortDescending = true;

        // 添加字段记录当前选中的大类
        private string _currentBigCategory = string.Empty;

        // 添加字段记录当前选中的小类
        private string? _currentSmallCategory;

        // 添加搜索状态标记
        private readonly bool _isSearching = false;
        private string _searchText = string.Empty;

        // 修改成就卡片集合类型为可观察集合（ObservableCollection）
        private ObservableCollection<AchievementCard> _achievementCards = [];

        // 数据缓存：键为缓存键（用户名+类别+排序），值为成就卡片列表
        private readonly Dictionary<string, List<AchievementCard>> _achievementCache = [];
        // 缓存大小限制
        private const int MAX_CACHE_SIZE = 50;
        // 缓存项访问时间：键为缓存键，值为最后访问时间
        private readonly Dictionary<string, DateTime> _cacheAccessTimes = [];
        // 图片缓存：键为图片路径，值为BitmapImage
        private readonly Dictionary<string, BitmapImage> _imageCache = [];
        // 图片缓存大小限制
        private const int MAX_IMAGE_CACHE_SIZE = 100;
        // 图片缓存访问时间：键为图片路径，值为最后访问时间
        private readonly Dictionary<string, DateTime> _imageCacheAccessTimes = [];
        // 分页大小
        private readonly int _pageSize;
        // 当前页码
        private int _currentPage = 0;
        // 是否正在加载更多
        private bool _isLoadingMore = false;
        // 总成就数量
        private int _totalAchievements = 0;

        public HomePage(string userName)
        {
            UserName = userName;
            InitializeComponent();

            // 初始化分页大小
            _pageSize = ThemeSettings.LoadPageSize();

            // 初始化加载指示器
            InitializeLoadingIndicator();

            // 页面加载完成后异步加载数据
            Loaded += async (s, e) => await InitializeDataAsync();

            // 订阅大类选择变化事件
            A_category.SelectionChanged += OnCategorySelectionChanged;

            // 订阅搜索框文本改变事件
            SearchBox.TextChanged += SearchBox_TextChanged;
        }

        // 初始化数据加载
        private async Task InitializeDataAsync()
        {
            try
            {
                // 先加载大类数据
                await LoadAchievementCategoriesAsync();

                // 显示成就区域，即使还没有数据
                ShowAchievementsArea(true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"初始化数据加载失败: {ex.Message}");
                MessageBox.Show($"初始化数据加载失败: {ex.Message}", "错误",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 缓存管理：确保缓存大小不超过限制
        private void ManageCacheSize()
        {
            // 当缓存大小超过限制时，清理最旧的缓存项
            while (_achievementCache.Count > MAX_CACHE_SIZE)
            {
                // 找到最旧的缓存项
                string oldestKey = _cacheAccessTimes.OrderBy(kv => kv.Value).First().Key;

                // 移除最旧的缓存项
                _achievementCache.Remove(oldestKey);
                _cacheAccessTimes.Remove(oldestKey);
            }
        }

        // 更新缓存访问时间
        private void UpdateCacheAccessTime(string key)
        {
            _cacheAccessTimes[key] = DateTime.Now;
        }

        // 图片缓存管理：确保图片缓存大小不超过限制
        private void ManageImageCacheSize()
        {
            // 当图片缓存大小超过限制时，清理最旧的图片缓存
            while (_imageCache.Count > MAX_IMAGE_CACHE_SIZE)
            {
                // 找到最旧的图片缓存项
                string oldestKey = _imageCacheAccessTimes.OrderBy(kv => kv.Value).First().Key;

                // 移除最旧的图片缓存项
                _imageCache.Remove(oldestKey);
                _imageCacheAccessTimes.Remove(oldestKey);
            }
        }

        // 更新图片缓存访问时间
        private void UpdateImageCacheAccessTime(string key)
        {
            _imageCacheAccessTimes[key] = DateTime.Now;
        }

        // 加载图片（使用缓存）
        private BitmapImage? LoadImage(string imagePath)
        {
            // 检查图片路径是否为空
            if (string.IsNullOrEmpty(imagePath))
            {
                return null;
            }

            // 检查图片缓存
            if (_imageCache.TryGetValue(imagePath, out BitmapImage cachedImage))
            {
                // 更新缓存访问时间
                UpdateImageCacheAccessTime(imagePath);
                return cachedImage;
            }

            try
            {
                // 图片缓存未命中，从文件加载
                BitmapImage image = new();
                image.BeginInit();
                image.UriSource = new Uri(imagePath);
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.EndInit();
                image.Freeze(); // 冻结图片，使其可以在多线程中使用

                // 存入图片缓存
                _imageCache[imagePath] = image;
                _imageCacheAccessTimes[imagePath] = DateTime.Now;
                // 管理图片缓存大小
                ManageImageCacheSize();

                return image;
            }
            catch
            {
                // 图片加载失败，返回null
                return null;
            }
        }

        // 预加载图片
        private async Task PreloadImagesAsync(List<AchievementCard> cards)
        {
            // 异步预加载图片
            await Task.Run(() =>
            {
                foreach (var card in cards)
                {
                    if (!string.IsNullOrEmpty(card.ImagePath))
                    {
                        LoadImage(card.ImagePath);
                    }
                }
            });
        }

        // 新增：加载成就大类类别
        private async Task LoadAchievementCategoriesAsync()
        {
            try
            {
                // 使用成就服务获取类别
                using var achievementService = new AchievementService();
                var categories = await achievementService.GetbigCategoriesAsync(UserName);

                // 清除现有项并添加新项
                A_category.Items.Clear();
                A_category.Items.Add("所有");
                // 对类别进行排序后添加
                foreach (var category in categories.OrderBy(c => c))
                {
                    A_category.Items.Add(category);
                }

                // 设置默认选中项（如果有数据）
                if (A_category.Items.Count > 1)
                {
                    A_category.SelectedIndex = 1;
                }
            }
            catch (Exception ex)
            {
                // 错误处理
                Debug.WriteLine($"加载成就类别失败: {ex.Message}");
                MessageBox.Show($"加载成就类别失败: {ex.Message}", "错误",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 初始化加载指示器
        private void InitializeLoadingIndicator()
        {
            _loadingIndicator = new ProgressBar
            {
                IsIndeterminate = true,
                Height = 4,
                Margin = new Thickness(0, 5, 0, 5),
                Visibility = Visibility.Collapsed
            };
            B_category.Children.Add(_loadingIndicator);
        }

        // 处理大类选择变化
        private async void OnCategorySelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (A_category.SelectedItem is string selectedBigCategory)
            {
                _currentBigCategory = selectedBigCategory; // 记录当前大类
                _currentSmallCategory = null; // 重置小类选择

                // 重置搜索状态
                _searchText = string.Empty;
                SearchBox.Text = string.Empty;

                // 并行加载小类和成就数据，提高加载速度
                var loadTasks = new List<Task>
                {
                    LoadSmallCategoriesAsync(selectedBigCategory),
                    LoadAchievementsByBigCategoryAsync(selectedBigCategory)
                };

                // 等待所有加载任务完成
                await Task.WhenAll(loadTasks);
            }
        }

        // 加载小类
        private async Task LoadSmallCategoriesAsync(string bigCategory)
        {
            try
            {
                // 显示加载状态
                SetLoadingState(true);

                // 清除现有B类按钮
                ClearSmallCategoryButtons();

                // 如果是"所有"类别，不加载小类按钮
                if (bigCategory == "所有")
                {
                    // 隐藏小类区域
                    B_category.Visibility = Visibility.Collapsed;
                    return;
                }

                // 显示小类区域
                B_category.Visibility = Visibility.Visible;

                // 获取B类数据
                using var achievementService = new AchievementService();
                var smallCategories = await achievementService.GetSmallCategoriesAsync(bigCategory, UserName);

                // 动态创建按钮
                foreach (var smallCategory in smallCategories)
                {
                    var button = new Button
                    {
                        Content = smallCategory,
                        Width = 120,
                        Height = 35,
                        Margin = new Thickness(5),
                        Style = (Style)FindResource("SmallCategoryButtonStyle")
                    };

                    // 添加点击事件处理
                    button.Click += (s, e) => OnSmallCategoryClick(smallCategory);

                    B_category.Children.Add(button);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"加载小类别失败: {ex.Message}");
                MessageBox.Show($"加载小类别失败: {ex.Message}", "错误",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // 隐藏加载状态
                SetLoadingState(false);
            }
        }

        // 清除现有小类按钮（保留加载指示器）
        private void ClearSmallCategoryButtons()
        {
            // 保存加载指示器
            var temp = _loadingIndicator;

            // 清空子元素
            B_category.Children.Clear();

            // 重新添加加载指示器
            B_category.Children.Add(temp);
        }

        // 设置加载状态
        private void SetLoadingState(bool isLoading)
        {
            if (_loadingIndicator != null)
            {
                _loadingIndicator.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        // 新增方法：根据大类加载成就
        private async Task LoadAchievementsByBigCategoryAsync(string bigCategory, bool loadMore = false)
        {
            try
            {
                // 显示加载状态
                SetLoadingState(true);

                // 只有在非加载更多模式下才隐藏成就区域
                if (!loadMore)
                {
                    ShowAchievementsArea(false);
                }

                // 获取排序参数
                string? sortField = null;
                if (!string.IsNullOrEmpty(_activeSortField) && _sortStates[_activeSortField] != SortState.Unsorted)
                {
                    sortField = _activeSortField;
                    _isSortDescending = _sortStates[_activeSortField] == SortState.Descending;
                }

                // 生成缓存键
                string cacheKey = $"{UserName}_{bigCategory}_{sortField}_{_isSortDescending}";

                // 检查缓存
                if (!loadMore && _achievementCache.TryGetValue(cacheKey, out List<AchievementCard> cachedCards))
                {
                    // 更新缓存访问时间
                    UpdateCacheAccessTime(cacheKey);

                    // 使用缓存数据
                    _totalAchievements = cachedCards.Count;
                    _currentPage = loadMore ? _currentPage : 0;

                    // 分页处理
                    var cachedPaginatedCards = loadMore
                        ? cachedCards.Skip((_currentPage + 1) * _pageSize).Take(_pageSize).ToList()
                        : [.. cachedCards.Take(_pageSize)];

                    if (loadMore && _achievementCards != null)
                    {
                        // 加载更多：批量添加到现有集合
                        if (cachedPaginatedCards.Count != 0)
                        {
                            // 使用List临时存储，然后批量添加
                            var cardsToAdd = new List<AchievementCard>(cachedPaginatedCards);
                            foreach (var card in cardsToAdd)
                            {
                                _achievementCards.Add(card);
                            }
                            _currentPage++;
                        }
                    }
                    else
                    {
                        // 初始加载：创建新集合
                        _achievementCards = new ObservableCollection<AchievementCard>(cachedPaginatedCards);
                        AchievementsContainer.ItemsSource = _achievementCards;
                        _currentPage = 0;
                    }

                    ShowAchievementsArea(true);
                    SetLoadingState(false);
                    return;
                }

                // 缓存未命中，从数据库加载
                List<AchievementCard> cards;
                using (var achievementService = new AchievementService())
                {
                    // 处理"所有"类别
                    if (bigCategory == "所有")
                    {
                        cards = await achievementService.GetAllAchievementCardsAsync(UserName, sortField, _isSortDescending);
                    }
                    else
                    {
                        cards = await achievementService.GetAchievementCardsByBigCategoryAsync(UserName, bigCategory, sortField, _isSortDescending);
                    }
                }

                // 存入缓存
                _achievementCache[cacheKey] = cards;
                _cacheAccessTimes[cacheKey] = DateTime.Now;
                // 管理缓存大小
                ManageCacheSize();
                _totalAchievements = cards.Count;

                // 分页处理
                var paginatedCards = loadMore
                    ? [.. cards.Skip((_currentPage + 1) * _pageSize).Take(_pageSize)]
                    : cards.Take(_pageSize).ToList();

                if (loadMore && _achievementCards != null)
                {
                    // 加载更多：批量添加到现有集合
                    if (paginatedCards.Count != 0)
                    {
                        // 使用List临时存储，然后批量添加
                        var cardsToAdd = new List<AchievementCard>(paginatedCards);
                        foreach (var card in cardsToAdd)
                        {
                            _achievementCards.Add(card);
                        }
                        _currentPage++;
                    }
                }
                else
                {
                    // 初始加载：创建新集合
                    _achievementCards = new ObservableCollection<AchievementCard>(paginatedCards);
                    AchievementsContainer.ItemsSource = _achievementCards;
                    _currentPage = 0;
                }

                // 根据是否有数据显示相应区域
                if (cards.Count > 0)
                {
                    ShowAchievementsArea(true);
                }
                else
                {
                    ShowNoAchievementsMessage(bigCategory);
                }

                // 异步预加载图片，不阻塞UI
                _ = PreloadImagesAsync(cards);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"加载大类成就失败: {ex.Message}");
                MessageBox.Show($"加载成就失败: {ex.Message}", "错误",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                SetLoadingState(false);
            }
        }

        // 处理小类点击事件
        private async void OnSmallCategoryClick(string smallCategory)
        {
            // 如果点击的是当前已选中的小类，则不再执行操作
            if (smallCategory == _currentSmallCategory)
            {
                return;
            }

            // 更新按钮选中状态
            UpdateButtonSelection(smallCategory);
            _currentSmallCategory = smallCategory; // 记录当前小类

            // 异步加载成就数据
            await LoadAchievementsBySmallCategoryAsync(smallCategory);

            // 重置搜索状态
            _searchText = string.Empty;
            SearchBox.Text = string.Empty;
        }

        // 显示无成就提示
        private void ShowNoAchievementsMessage(string? categoryName)
        {
            ShowAchievementsArea(false);

            // 可以在这里定制化提示信息
            string safeCategoryName = categoryName ?? "该";
            NoAchievementsText.Text = $"'{safeCategoryName}'类别下暂无成就";
        }

        //是-显示成就区域/否-显示提示区域
        private void ShowAchievementsArea(bool show)
        {
            AchievementScrollViewer.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            NoAchievementsPanel.Visibility = show ? Visibility.Collapsed : Visibility.Visible;
        }

        // 使用_currentSmallCategory更新按钮状态
        private void UpdateButtonSelection(string smallCategory)
        {
            // 更新所有按钮的视觉状态
            foreach (Button button in B_category.Children.OfType<Button>())
            {
                // 获取按钮对应的小类名称
                var buttonCategory = button.Content.ToString();
                bool isSelected = buttonCategory == smallCategory;

                // 设置视觉状态
                VisualStateManager.GoToState(
                    button,
                    isSelected ? "Selected" : "Unselected",
                    true
                );

                // 更新按钮焦点状态
                if (isSelected)
                {
                    button.Focus();
                }
            }
        }

        // 小类按钮异步加载成就数据
        private async Task LoadAchievementsBySmallCategoryAsync(string smallCategory, bool loadMore = false)
        {
            try
            {
                // 显示加载状态
                SetLoadingState(true);
                ShowAchievementsArea(false);        // 先隐藏成就区域

                // 获取排序参数
                string? sortField = null;
                if (!string.IsNullOrEmpty(_activeSortField) && _sortStates[_activeSortField] != SortState.Unsorted)
                {
                    sortField = _activeSortField;
                    _isSortDescending = _sortStates[_activeSortField] == SortState.Descending;
                }

                // 生成缓存键
                string cacheKey = $"{UserName}_Small_{smallCategory}_{sortField}_{_isSortDescending}";

                // 检查缓存
                if (!loadMore && _achievementCache.TryGetValue(cacheKey, out List<AchievementCard> cachedCards))
                {
                    // 更新缓存访问时间
                    UpdateCacheAccessTime(cacheKey);

                    // 使用缓存数据
                    _totalAchievements = cachedCards.Count;
                    _currentPage = loadMore ? _currentPage : 0;

                    // 分页处理
                    var cachedPaginatedCards = loadMore
                        ? cachedCards.Skip((_currentPage + 1) * _pageSize).Take(_pageSize).ToList()
                        : [.. cachedCards.Take(_pageSize)];

                    if (loadMore && _achievementCards != null)
                    {
                        // 加载更多：批量添加到现有集合
                        if (cachedPaginatedCards.Count != 0)
                        {
                            // 使用List临时存储，然后批量添加
                            var cardsToAdd = new List<AchievementCard>(cachedPaginatedCards);
                            foreach (var card in cardsToAdd)
                            {
                                _achievementCards.Add(card);
                            }
                            _currentPage++;
                        }
                    }
                    else
                    {
                        // 初始加载：创建新集合
                        _achievementCards = new ObservableCollection<AchievementCard>(cachedPaginatedCards);
                        AchievementsContainer.ItemsSource = _achievementCards;
                        _currentPage = 0;
                    }

                    ShowAchievementsArea(true);
                    SetLoadingState(false);
                    return;
                }

                // 缓存未命中，从数据库加载
                List<AchievementCard> cards;
                using (var achievementService = new AchievementService())
                {
                    cards = await achievementService.GetAchievementCardsAsync(UserName, smallCategory, sortField, _isSortDescending);
                }

                // 存入缓存
                _achievementCache[cacheKey] = cards;
                _cacheAccessTimes[cacheKey] = DateTime.Now;
                // 管理缓存大小
                ManageCacheSize();
                _totalAchievements = cards.Count;

                // 预加载图片
                await PreloadImagesAsync(cards);

                // 分页处理
                var paginatedCards = loadMore
                    ? [.. cards.Skip((_currentPage + 1) * _pageSize).Take(_pageSize)]
                    : cards.Take(_pageSize).ToList();

                if (loadMore && _achievementCards != null)
                {
                    // 加载更多：批量添加到现有集合
                    if (paginatedCards.Count != 0)
                    {
                        // 使用List临时存储，然后批量添加
                        var cardsToAdd = new List<AchievementCard>(paginatedCards);
                        foreach (var card in cardsToAdd)
                        {
                            _achievementCards.Add(card);
                        }
                        _currentPage++;
                    }
                }
                else
                {
                    // 初始加载：创建新集合
                    _achievementCards = new ObservableCollection<AchievementCard>(paginatedCards);
                    AchievementsContainer.ItemsSource = _achievementCards;
                    _currentPage = 0;
                }

                // 如果没有成就，显示提示信息
                if (cards.Count > 0)
                {
                    ShowAchievementsArea(true); // 显示成就区域
                }
                else
                {
                    ShowNoAchievementsMessage(smallCategory); // 显示无成就提示
                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"加载成就卡片失败: {ex.Message}");
                MessageBox.Show($"加载成就失败: {ex.Message}", "错误",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // 隐藏加载状态
                SetLoadingState(false);
            }
        }

        //排序按钮
        private async void SortButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string sortField)
            {
                // 获取当前排序状态
                var currentState = _sortStates[sortField];

                // 如果点击的是已激活的排序字段，则切换状态
                if (sortField == _activeSortField)
                {
                    _sortStates[sortField] = currentState switch
                    {
                        SortState.Unsorted => SortState.Ascending,
                        SortState.Ascending => SortState.Descending,
                        SortState.Descending => SortState.Ascending,
                        _ => SortState.Ascending
                    };
                }
                // 如果点击的是新字段，则重置其他字段状态
                else
                {
                    // 重置所有排序状态
                    foreach (var key in _sortStates.Keys.ToList())
                    {
                        _sortStates[key] = SortState.Unsorted;
                    }

                    // 设置新字段为升序
                    _sortStates[sortField] = SortState.Ascending;
                    _activeSortField = sortField;
                }

                // 更新按钮视觉状态
                UpdateSortButtonVisualState();

                // 重新加载当前数据（带排序参数）
                await ReloadCurrentDataAsync();
            }
        }

        // 更新按钮视觉状态
        private void UpdateSortButtonVisualState()
        {
            // 为每个按钮设置视觉状态
            VisualStateManager.GoToState(SortByNameButton,
                GetStateName("Name"), true);

            VisualStateManager.GoToState(SortByRatingButton,
                GetStateName("Rating"), true);

            VisualStateManager.GoToState(SortByProgressButton,
                GetStateName("Progress"), true);

            VisualStateManager.GoToState(SortByDateButton,
                GetStateName("Date"), true);
        }

        // 重新加载当前数据
        private async Task ReloadCurrentDataAsync()
        {
            // 重置页码
            _currentPage = 0;
            _totalAchievements = 0;

            if (!string.IsNullOrEmpty(_searchText) && _achievementCards != null && _achievementCards.Count != 0)
            {
                await PerformSearchAsync(_searchText);
            }
            else if (!string.IsNullOrEmpty(_currentSmallCategory))
            {
                await LoadAchievementsBySmallCategoryAsync(_currentSmallCategory, false);
            }
            else if (!string.IsNullOrEmpty(_currentBigCategory))
            {
                await LoadAchievementsByBigCategoryAsync(_currentBigCategory, false);
            }
            else
            {
                await LoadAchievementsByBigCategoryAsync("所有", false);
            }

            // 重置搜索状态
            _searchText = string.Empty;
            SearchBox.Text = string.Empty;
        }

        // 获取状态名称
        private string GetStateName(string field)
        {
            return _sortStates[field] switch
            {
                SortState.Ascending => "Ascending",
                SortState.Descending => "Descending",
                _ => "Unsorted"
            };
        }

        // 处理搜索框文本改变
        private async void SearchBox_TextChanged(object sender, SearchReplace.AutoSuggestBoxTextChangedEventArgs e)
        {
            if (e.Reason == iNKORE.UI.WPF.Modern.Controls.AutoSuggestionBoxTextChangeReason.UserInput)
            {
                _searchText = SearchBox.Text;

                // 空搜索时恢复原始类别
                if (string.IsNullOrWhiteSpace(_searchText))
                {
                    await RestoreOriginalCategoryAsync();
                }
            }
        }

        // 恢复原始类别显示
        private async Task RestoreOriginalCategoryAsync()
        {
            // 重置页码
            _currentPage = 0;
            _totalAchievements = 0;

            if (!string.IsNullOrEmpty(_currentSmallCategory))
            {
                await LoadAchievementsBySmallCategoryAsync(_currentSmallCategory, false);
            }
            else if (!string.IsNullOrEmpty(_currentBigCategory))
            {
                await LoadAchievementsByBigCategoryAsync(_currentBigCategory, false);
            }
            else
            {
                await LoadAchievementsByBigCategoryAsync("所有", false);
            }
        }

        // 处理搜索提交
        private async void SearchBox_QuerySubmitted(object sender, SearchReplace.AutoSuggestBoxQuerySubmittedEventArgs e)
        {
            _searchText = e.QueryText;

            if (!string.IsNullOrWhiteSpace(_searchText))
            {
                await PerformSearchAsync(_searchText);
            }
        }

        // 执行搜索
        private async Task PerformSearchAsync(string searchText)
        {
            try
            {
                SetLoadingState(true);
                ShowAchievementsArea(false);

                // 获取排序参数
                string? sortField = null;
                if (!string.IsNullOrEmpty(_activeSortField) && _sortStates[_activeSortField] != SortState.Unsorted)
                {
                    sortField = _activeSortField;
                    _isSortDescending = _sortStates[_activeSortField] == SortState.Descending;
                }

                // 生成缓存键
                string cacheKey = $"{UserName}_Search_{searchText}_{sortField}_{_isSortDescending}";

                // 检查缓存
                if (_achievementCache.TryGetValue(cacheKey, out List<AchievementCard> cachedCards))
                {
                    // 更新缓存访问时间
                    UpdateCacheAccessTime(cacheKey);

                    // 使用缓存数据
                    _totalAchievements = cachedCards.Count;
                    _currentPage = 0;

                    // 分页处理
                    var cachedPaginatedCards = cachedCards.Take(_pageSize).ToList();
                    // 批量创建新集合
                    _achievementCards = new ObservableCollection<AchievementCard>(cachedPaginatedCards);
                    AchievementsContainer.ItemsSource = _achievementCards;

                    if (cachedCards.Count > 0)
                    {
                        ShowAchievementsArea(true);
                    }
                    else
                    {
                        ShowNoAchievementsMessage($"搜索 '{searchText}' 无结果");
                    }

                    SetLoadingState(false);
                    return;
                }

                // 缓存未命中，从数据库加载
                List<AchievementCard> cards;
                using (var achievementService = new AchievementService())
                {
                    // 调用服务层的搜索方法
                    cards = await achievementService.SearchAchievementsByNameAsync(UserName, searchText, sortField, _isSortDescending);
                }

                // 存入缓存
                _achievementCache[cacheKey] = cards;
                _cacheAccessTimes[cacheKey] = DateTime.Now;
                // 管理缓存大小
                ManageCacheSize();
                _totalAchievements = cards.Count;

                // 预加载图片
                await PreloadImagesAsync(cards);

                // 分页处理
                var paginatedCards = cards.Take(_pageSize).ToList();
                // 批量创建新集合
                _achievementCards = new ObservableCollection<AchievementCard>(paginatedCards);
                AchievementsContainer.ItemsSource = _achievementCards;
                _currentPage = 0;

                if (cards.Count > 0)
                {
                    ShowAchievementsArea(true);
                }
                else
                {
                    ShowNoAchievementsMessage($"搜索 '{searchText}' 无结果");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"搜索失败: {ex.Message}");
                MessageBox.Show($"搜索失败: {ex.Message}", "错误",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                SetLoadingState(false);
            }
        }

        // 打开图片位置功能
        private void OpenImageLocation_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 获取菜单项对应的成就卡片数据
                var menuItem = sender as MenuItem;
                var contextMenu = menuItem?.Parent as ContextMenu;
                var border = contextMenu?.PlacementTarget as Border;

                if (border?.DataContext is not AchievementCard achievementCard || string.IsNullOrEmpty(achievementCard.ImagePath))
                    return;

                // 获取图片完整路径
                string imagePath = achievementCard.ImagePath;

                // 检查文件是否存在
                if (!File.Exists(imagePath))
                {
                    MessageBox.Show("图片文件不存在！", "错误",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 使用Windows资源管理器打开图片所在目录并选中文件
                string argument = $"/select,\"{imagePath}\"";
                Process.Start("explorer.exe", argument);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"打开图片位置失败: {ex.Message}");
                MessageBox.Show($"打开图片位置失败: {ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //设置默认图片
        private void SetAsDefaultImage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 1. 获取当前成就卡片的图片
                var menuItem = sender as MenuItem;
                var contextMenu = menuItem?.Parent as ContextMenu;
                var border = contextMenu?.PlacementTarget as Border;

                if (border?.DataContext is not AchievementCard achievementCard || string.IsNullOrEmpty(achievementCard.ImagePath))
                {
                    MessageBox.Show("未找到有效图片路径", "错误",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string sourcePath = achievementCard.ImagePath;

                // 2. 验证图片有效性
                if (!File.Exists(sourcePath))
                {
                    MessageBox.Show("原始图片文件不存在", "错误",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // 3. 准备目标路径
                string targetDir = AppConfig.ImagesDirectoryPath;
                string targetPath = AppConfig.DefaultImagePath;

                // 4. 确保目录存在
                if (!Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }

                // 5. 处理图片格式转换
                if (!Path.GetExtension(sourcePath).Equals(".png", StringComparison.OrdinalIgnoreCase))
                {
                    ConvertToPng(sourcePath, targetPath); // 格式转换方法
                }
                else
                {
                    File.Copy(sourcePath, targetPath, true); // 直接复制PNG文件
                }

                // 强制刷新所有成就卡片
                ForceRefreshAllAchievementCards();

                MessageBox.Show("默认图片设置成功!", "成功",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"设置默认图片失败: {ex.Message}");
                MessageBox.Show($"设置失败: {ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 图片格式转换方法
        private static void ConvertToPng(string sourcePath, string targetPath)
        {
            using var original = System.Drawing.Image.FromFile(sourcePath);
            using var bitmap = new System.Drawing.Bitmap(original);
            bitmap.Save(targetPath, System.Drawing.Imaging.ImageFormat.Png);
        }

        //强制刷新
        private void ForceRefreshAllAchievementCards()
        {
            if (_achievementCards == null) return;

            // 创建临时副本
            var cards = _achievementCards.ToList();

            // 重置集合
            _achievementCards.Clear();
            foreach (var card in cards)
            {
                _achievementCards.Add(card);
            }
        }

        //添加成就
        private void AddAchievementButton_Click(object sender, RoutedEventArgs e)
        {
            // 使用导航服务导航到添加成就页面
            var drillInTransition = new DrillInNavigationTransitionInfo();
            this.NavigationService?.Navigate(new AddAchievementPage(UserName), drillInTransition);
        }

        // 添加防抖定时器
        private System.Threading.Timer? _scrollDebounceTimer;
        private readonly Lock _timerLock = new();

        // 滚动事件处理，实现自动加载更多
        private void AchievementScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (sender is ScrollViewer scrollViewer)
            {
                // 计算滚动位置，当滚动到接近底部时加载更多
                double scrollPosition = scrollViewer.VerticalOffset + scrollViewer.ViewportHeight;
                double scrollHeight = scrollViewer.ScrollableHeight;

                // 当滚动到距离底部100像素以内时加载更多
                if (scrollPosition >= scrollHeight - 100 && !_isLoadingMore && _achievementCards != null && _achievementCards.Count < _totalAchievements)
                {
                    // 使用防抖机制，避免频繁触发
                    lock (_timerLock)
                    {
                        // 取消之前的定时器
                        _scrollDebounceTimer?.Dispose();

                        // 创建新的定时器，200毫秒后执行加载
                        _scrollDebounceTimer = new System.Threading.Timer((_) =>
                        {
                            // 使用Dispatcher确保在UI线程上执行
                            this.Dispatcher.Invoke(async () =>
                            {
                                if (!_isLoadingMore)
                                {
                                    _isLoadingMore = true;
                                    await LoadMoreAchievementsAsync();
                                    _isLoadingMore = false;
                                }
                            });
                        }, null, 200, System.Threading.Timeout.Infinite);
                    }
                }
            }
        }

        // 加载更多成就
        private async Task LoadMoreAchievementsAsync()
        {
            try
            {
                if (_isSearching)
                {
                    // 搜索模式下不支持加载更多
                    return;
                }

                if (!string.IsNullOrEmpty(_currentSmallCategory))
                {
                    // 加载小类更多成就
                    await LoadAchievementsBySmallCategoryAsync(_currentSmallCategory, true);
                }
                else if (!string.IsNullOrEmpty(_currentBigCategory))
                {
                    // 加载大类更多成就
                    await LoadAchievementsByBigCategoryAsync(_currentBigCategory, true);
                }
                else
                {
                    // 加载所有更多成就
                    await LoadAchievementsByBigCategoryAsync("所有", true);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"加载更多成就失败: {ex.Message}");
            }
        }

        // 删除成就功能
        private async void DeleteAchievement_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 获取菜单项对应的成就卡片数据
                var menuItem = sender as MenuItem;
                var contextMenu = menuItem?.Parent as ContextMenu;
                var border = contextMenu?.PlacementTarget as Border;

                if (border?.DataContext is not AchievementCard achievementCard) return;

                // 确认删除
                var result = MessageBox.Show($"确定要删除成就 '{achievementCard.AchievementName}' 吗？",
                    "删除确认",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes) return;

                // 执行删除
                using var achievementService = new AchievementService();
                bool success = await achievementService.DeleteAchievementAsync(
                    UserName,
                    achievementCard.AchievementId);

                if (success)
                {
                    // 从UI集合中移除该项
                    if (_achievementCards != null && _achievementCards.Contains(achievementCard))
                    {
                        _achievementCards.Remove(achievementCard);
                    }

                    // 更新UI状态（无论成功与否都更新）
                    UpdateUIAfterDeletion();
                }
                else
                {
                    MessageBox.Show("删除成就失败，请重试", "错误",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"删除成就失败: {ex.Message}");
                MessageBox.Show($"删除成就失败: {ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 新增方法：删除成就后更新UI
        private void UpdateUIAfterDeletion()
        {
            if (_achievementCards == null || _achievementCards.Count == 0)
            {
                ShowNoAchievementsMessage(_currentSmallCategory);
            }
            else
            {
                // 确保成就区域可见
                AchievementScrollViewer.Visibility = Visibility.Visible;
                NoAchievementsPanel.Visibility = Visibility.Collapsed;
            }
        }


        // 详情功能
        private void AchievementNameButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 1. 获取按钮对应的成就卡片数据
                if (sender is Button button && button.DataContext is AchievementCard achievementCard)
                {
                    // 2. 创建导航动画效果
                    var drillInTransition = new DrillInNavigationTransitionInfo();

                    // 3. 导航到成就详情页面（使用AddAchievementPage）
                    this.NavigationService?.Navigate(
                        new AddAchievementPage(UserName, achievementCard),
                        drillInTransition
                    );
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"查看成就详情失败: {ex.Message}");
                MessageBox.Show($"打开成就详情失败: {ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 打印成就按钮点击事件
        private async void PrintAchievementButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 隐藏添加按钮和打印按钮
                AddAchievementButton.Visibility = Visibility.Collapsed;
                PrintAchievementButton.Visibility = Visibility.Collapsed;

                // 等待UI更新
                await Task.Delay(100);

                // 生成默认文件名
                string defaultFileName = $"成就展示_{DateTime.Now:yyyyMMdd_HHmmss}";

                // 创建保存文件对话框
                var saveFileDialog = new SaveFileDialog
                {
                    Title = "保存成就截图",
                    Filter = "PDF文件 (*.pdf)|*.pdf|PNG图片 (*.png)|*.png",
                    DefaultExt = "pdf",
                    AddExtension = true,
                    FileName = defaultFileName
                };

                // 显示保存文件对话框
                if (saveFileDialog.ShowDialog() == true)
                {
                    string filePath = saveFileDialog.FileName;
                    string extension = Path.GetExtension(filePath).ToLower();

                    // 获取成就卡片展示区的内容
                    var scrollViewer = AchievementScrollViewer;
                    if (scrollViewer != null)
                    {
                        // 获取滚动查看器的可见区域大小
                        double visibleWidth = scrollViewer.ViewportWidth;
                        double visibleHeight = scrollViewer.ViewportHeight;

                        // 创建一个与可见区域大小相同的视觉元素
                        if (scrollViewer.Content is UIElement content)
                        {
                            // 为2K屏幕设置合适的DPI参数
                            // 2K屏幕推荐DPI: 180
                            // 说明：
                            // - DPI过小（如96）：生成的图片在2K屏幕上会显得模糊，细节不清晰
                            // - DPI过大（如300+）：生成的图片文件会非常大，保存和打开速度会变慢，
                            //   同时可能会超出内存限制，导致程序崩溃
                            int dpi = 180;

                            // 计算实际像素大小（基于DPI）
                            int pixelWidth = (int)(visibleWidth * dpi / 96);
                            int pixelHeight = (int)(visibleHeight * dpi / 96);

                            // 测量和排列内容（使用可见区域大小）
                            content.Measure(new Size(visibleWidth, visibleHeight));
                            content.Arrange(new Rect(0, 0, visibleWidth, visibleHeight));

                            // 创建渲染目标位图（提高DPI）
                            var renderTargetBitmap = new RenderTargetBitmap(
                                pixelWidth,
                                pixelHeight,
                                dpi, dpi, PixelFormats.Pbgra32);

                            // 渲染内容
                            renderTargetBitmap.Render(content);

                            if (extension == ".png")
                            {
                                // 保存为PNG图片
                                using (var fileStream = new FileStream(filePath, FileMode.Create))
                                {
                                    var pngEncoder = new PngBitmapEncoder();
                                    pngEncoder.Frames.Add(BitmapFrame.Create(renderTargetBitmap));
                                    pngEncoder.Save(fileStream);
                                }

                                MessageBox.Show("成就截图已成功保存为PNG文件！", "成功",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                            else if (extension == ".pdf")
                            {
                                // 保存为PDF文件
                                try
                                {
                                    // 创建PDF文档
                                    using (PdfDocument document = new())
                                    {
                                        document.Info.Title = "成就系统截图";
                                        document.Info.Author = "成就系统";
                                        document.Info.Subject = "成就卡片截图";
                                        document.Info.Keywords = "成就,截图,PDF";
                                        document.Info.CreationDate = DateTime.Now;

                                        // 创建PDF页面
                                        PdfPage page = document.AddPage();

                                        // 设置页面大小为图片大小
                                        page.Width = new XUnit((int)pixelWidth);
                                        page.Height = new XUnit((int)pixelHeight);

                                        // 创建XGraphics对象
                                        using (XGraphics gfx = XGraphics.FromPdfPage(page))
                                        {
                                            // 将RenderTargetBitmap转换为BitmapSource
                                            BitmapSource bitmapSource = renderTargetBitmap;

                                            // 创建临时内存流
                                            using var memoryStream = new System.IO.MemoryStream();
                                            // 将BitmapSource保存为PNG到内存流
                                            var encoder = new PngBitmapEncoder();
                                            encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                                            encoder.Save(memoryStream);
                                            memoryStream.Position = 0;

                                            // 使用PdfSharp加载图片
                                            using XImage image = XImage.FromStream(memoryStream);
                                            // 绘制图片到PDF页面
                                            gfx.DrawImage(image, 0, 0, page.Width.Point, page.Height.Point);
                                        }

                                        // 保存PDF文档
                                        document.Save(filePath);
                                    }

                                    MessageBox.Show("成就截图已成功保存为PDF文件！", "成功",
                                        MessageBoxButton.OK, MessageBoxImage.Information);
                                }
                                catch (Exception pdfEx)
                                {
                                    // 如果PDF保存失败，尝试保存为PNG
                                    string fallbackPath = filePath.Replace(".pdf", ".png");
                                    using (var fileStream = new FileStream(fallbackPath, FileMode.Create))
                                    {
                                        var pngEncoder = new PngBitmapEncoder();
                                        pngEncoder.Frames.Add(BitmapFrame.Create(renderTargetBitmap));
                                        pngEncoder.Save(fileStream);
                                    }

                                    MessageBox.Show($"PDF保存失败，已保存为PNG文件：{pdfEx.Message}", "警告",
                                        MessageBoxButton.OK, MessageBoxImage.Warning);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"打印成就失败: {ex.Message}");
                MessageBox.Show($"打印成就失败: {ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // 显示添加按钮和打印按钮
                AddAchievementButton.Visibility = Visibility.Visible;
                PrintAchievementButton.Visibility = Visibility.Visible;
            }
        }
    }
}
