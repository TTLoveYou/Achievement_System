﻿﻿﻿﻿﻿﻿﻿﻿using Microsoft.Win32;
using OfficeOpenXml;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using 成就系统.Services;

namespace 成就系统.UI.Main
{
    /// <summary>
    /// ConsolePage.xaml 的交互逻辑
    /// </summary>
    public partial class ConsolePage : Page
    {
        private readonly AchievementService _achievementService = new();
        public string? UserName { get; }
        // 添加类别的字段
        private CategoryItem? _selectedCategory = null;
        // 选择导入的文件路径
        private string _selectedFilePath = string.Empty;

        // 添加常量定义
        private const string NameHeader = "名称";
        private const string DescHeader = "简介";
        private const string DateHeader = "日期";
        private const string ProgressHeader = "进度(+ == 25%)";
        private const string ScoreHeader = "评分(10)";


        public ConsolePage(string? userName = null)
        {
            UserName = userName;
            InitializeComponent();

            Loaded += async (s, e) => await LoadCategoryTree();
        }

        // 加载类别树
        private async Task LoadCategoryTree()
        {
            try
            {
                // 显示加载动画
                LoadingRing.Visibility = Visibility.Visible;
                StatusText.Text = "加载类别数据中...";
                TotalAchievementsText.Text = "加载成就总数...";

                // 获取所有类别（不限于当前用户）
                var categories = await _achievementService.GetAllCategoriesAsync();
                AchievementTree.ItemsSource = categories;

                // 更新状态文本
                int totalCategories = categories.Sum(c => c.Subcategories.Count);
                StatusText.Text = $"已加载 {categories.Count} 个大类, {totalCategories} 个小类";
                TotalAchievementsText.Text = $"成就总数：{categories.SelectMany(c => c.Subcategories).Sum(s => s.AchievementCount)}";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"加载失败: {ex.Message}";
                TotalAchievementsText.Text = "获取成就总数失败";
                Debug.WriteLine($"加载失败: {ex.Message}");
            }
            finally
            {
                // 隐藏加载动画
                LoadingRing.Visibility = Visibility.Collapsed;
            }
        }

        // 树形视图选择变化事件
        private void AchievementTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            _selectedCategory = e.NewValue as CategoryItem;
        }

        //导入操作
        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            ImportDialog.Visibility = Visibility.Visible;
            StatusText.Text = "正在导入成就数据...";
        }

        //选择导入的文件
        private void SelectFile_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Excel文件 (*.xlsx，*.csv)|*.xlsx;*.csv",
                Title = "选择成就数据文件"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                _selectedFilePath = openFileDialog.FileName;
                SelectedFileText.Text = System.IO.Path.GetFileName(_selectedFilePath);
                SelectedFileText.Foreground = Brushes.Green;
            }
        }

        //执行导入
        private async void ConfirmImport_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_selectedFilePath) || !File.Exists(_selectedFilePath))
            {
                MessageBox.Show("请先选择有效的Excel文件", "导入", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // 显示加载状态
                ImportProgressBar.Visibility = Visibility.Visible;

                ImportButton.IsEnabled = false;
                CancelImportButton.IsEnabled = false;
                SelectFileButton.IsEnabled = false;

                // 获取文件名作为大类
                string bigCategory = Path.GetFileNameWithoutExtension(_selectedFilePath);

                //许可证
                ExcelPackage.License.SetNonCommercialPersonal("成就系统");

                // 使用EPPlus库读取Excel文件
                using var package = new ExcelPackage(new FileInfo(_selectedFilePath));
                // 获取所有工作表
                var worksheets = package.Workbook.Worksheets;

                if (worksheets.Count == 0)
                {
                    MessageBox.Show("Excel文件中没有工作表", "导入错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                int totalAchievements = 0;  //总成就
                int successCount = 0;       //成功数
                int failedCount = 0;        //失败数

                // 遍历所有工作表
                foreach (var worksheet in worksheets)
                {
                    string smallCategory = worksheet.Name;
                    if (smallCategory == "说明") continue;

                    // 跳过空工作表
                    if (worksheet.Dimension == null) continue;

                    // 获取行数和列数
                    int rowCount = worksheet.Dimension.Rows;
                    int colCount = worksheet.Dimension.Columns;

                    // 检查列标题
                    var headers = new List<string>();
                    for (int col = 1; col <= colCount; col++)
                    {
                        headers.Add(worksheet.Cells[1, col].Text.Trim());
                    }

                    // 验证列标题
                    if (!ValidateExcelHeaders(headers))
                    {
                        failedCount += rowCount - 1; // 减去标题行
                        continue;
                    }

                    // 处理数据行
                    for (int row = 2; row <= rowCount; row++)
                    {
                        try
                        {
                            totalAchievements++;

                            // 解析成就数据
                            var achievement = ParseAchievementRow(worksheet, row, headers);

                            // 保存成就
                            bool success = await _achievementService.SaveAchievementAsync(achievement, UserName, bigCategory, smallCategory);

                            if (success) successCount++;
                            else failedCount++;

                            // 更新进度
                            ImportProgressBar.Value = (row - 1) * 100 / (rowCount - 1);
                        }
                        catch (Exception ex)
                        {
                            failedCount++;
                            Debug.WriteLine($"导入行 {row} 失败: {ex.Message}");
                        }
                    }
                }

                // 显示导入结果
                StatusText.Text = $"已导入 {successCount} 项成就";

                // 刷新类别树
                await LoadCategoryTree();

                // 显示成功消息
                if (successCount > 0)
                {
                    MessageBox.Show($"成功导入 {successCount} 项成就", "导入完成",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导入失败: {ex.Message}", "错误",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // 重置UI状态
                ImportProgressBar.Visibility = Visibility.Collapsed;
                ImportProgressBar.Value = 0;
                ImportButton.IsEnabled = true;
                CancelImportButton.IsEnabled = true;
                SelectFileButton.IsEnabled = true;
            }
        }

        // 验证Excel列标题
        private static bool ValidateExcelHeaders(List<string> headers)
        {
            // 必需的列标题
            var requiredHeaders = new List<string> { NameHeader, DescHeader, DateHeader, ProgressHeader, ScoreHeader };

            // 检查是否包含所有必需列
            foreach (var required in requiredHeaders)
            {
                if (!headers.Contains(required))
                {
                    MessageBox.Show($"Excel缺少必需的列: {required}", "导入错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }

            return true;
        }

        // 解析成就行数据
        private static AchievementCard ParseAchievementRow(ExcelWorksheet worksheet, int row, List<string> headers)
        {
            // 获取列索引
            int nameIndex = headers.IndexOf(NameHeader) + 1;
            int descIndex = headers.IndexOf(DescHeader) + 1;
            int dateIndex = headers.IndexOf(DateHeader) + 1;
            int progressIndex = headers.IndexOf(ProgressHeader) + 1;
            int scoreIndex = headers.IndexOf(ScoreHeader) + 1;

            var achievement = new AchievementCard()
            {
                AchievementName = worksheet.Cells[row, nameIndex].Text.Trim(),
                Description = worksheet.Cells[row, descIndex].Text.Trim(),
                CompletionDegree = worksheet.Cells[row, progressIndex].Text.Trim()
            };

            // 解析日期
            if (DateTime.TryParse(worksheet.Cells[row, dateIndex].Text, out DateTime date))
            {
                achievement.AchievementDate = date;
            }
            else
            {
                achievement.AchievementDate = DateTime.Today;
            }

            // 解析评分
            if (decimal.TryParse(worksheet.Cells[row, scoreIndex].Text, out decimal score))
            {
                achievement.Score = Math.Clamp(score, 0, 10); // 限制在0-10分之间
            }
            else
            {
                achievement.Score = 9; // 默认值
            }

            return achievement;
        }

        //取消导入
        private void CancelImport_Click(object sender, RoutedEventArgs e)
        {
            ImportDialog.Visibility = Visibility.Collapsed;
        }

        //导出
        private async void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 检查是否有选中的类别
                if (_selectedCategory == null)
                {
                    MessageBox.Show("请先选择一个类别", "导出",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }


                // 创建保存文件对话框
                var saveDialog = new SaveFileDialog
                {
                    Filter = "Excel文件 (*.xlsx)|*.xlsx",
                    FileName = $"{_selectedCategory.BigCategory}_成就数据_{DateTime.Now:yyyyMMdd}.xlsx",
                    Title = "保存成就数据"
                };

                if (saveDialog.ShowDialog() != true) return;

                string filePath = saveDialog.FileName;

                // 显示导出状态
                StatusText.Text = "准备导出数据...";
                ExportButton.IsEnabled = false;

                // 根据选中类别获取导出数据
                Dictionary<string, List<AchievementCard>> exportData;
                string mainCategoryName = _selectedCategory.BigCategory;

                if (_selectedCategory.Type == CategoryType.BigCategory)
                {
                    // 导出整个大类 - 使用现有方法获取所有小类
                    var smallCategories = await _achievementService.GetSmallCategoriesAsync(mainCategoryName, UserName);

                    exportData = [];

                    // 使用现有方法获取每个小类的成就
                    foreach (var smallCategory in smallCategories)
                    {
                        var achievements = await _achievementService.GetAchievementCardsAsync(UserName ?? string.Empty, smallCategory);
                        exportData.Add(smallCategory, achievements);
                    }
                }
                else
                {
                    // 导出单个小类 - 使用现有方法获取成就
                    var achievements = await _achievementService.GetAchievementCardsAsync(UserName ?? string.Empty, _selectedCategory.SmallCategory ?? string.Empty);
                    exportData = new Dictionary<string, List<AchievementCard>>
                                    {
                                        { _selectedCategory.SmallCategory, achievements }
                                    };
                }

                // 使用模板创建新的Excel文件
                await ExportWithTemplate(filePath, exportData);

                // 显示成功消息
                MessageBox.Show($"成就数据已成功导出到:\n{filePath}", "导出完成",
                                MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导出失败: {ex.Message}", "错误",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                StatusText.Text = "就绪";
                ExportButton.IsEnabled = true;
            }

        }

        // 使用模板导出成就数据
        private static Task ExportWithTemplate(string filePath, Dictionary<string, List<AchievementCard>> achievementsByCategory)
        {
            try
            {
                // 获取模板字节数组
                var templateBytes = TemplateManager.GetTemplateBytes();
                ExcelPackage.License.SetNonCommercialPersonal("成就系统");  //许可证

                // 使用模板创建新的Excel包
                using (var templateStream = new MemoryStream(templateBytes))
                using (var templatePackage = new ExcelPackage(templateStream))
                {
                    // 获取模板工作表
                    var templateSheet = templatePackage.Workbook.Worksheets[0];

                    // 创建新的Excel包
                    using var package = new ExcelPackage();
                    // 添加每个小类的工作表
                    foreach (var category in achievementsByCategory)
                    {
                        string smallCategory = category.Key;
                        var achievements = category.Value;

                        // 跳过空类别
                        if (achievements.Count == 0) continue;

                        // 清理工作表名称
                        string sheetName = CleanSheetName(smallCategory);

                        // 复制模板工作表到新工作簿
                        var worksheet = package.Workbook.Worksheets.Add(sheetName, templateSheet);

                        // 填充数据
                        FillAchievementSheet(worksheet, achievements);
                    }

                    // 保存文件
                    package.SaveAs(new FileInfo(filePath));
                }
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"导出失败: {ex}");
                throw; // 重新抛出异常以便上层处理
            }
        }

        // 填充成就数据工作表（使用模板格式）
        private static void FillAchievementSheet(ExcelWorksheet worksheet, List<AchievementCard> achievements)
        {
            // 从第2行开始填充数据（第1行是模板的表头）
            int startRow = 2;

            for (int i = 0; i < achievements.Count; i++)
            {
                int row = startRow + i;
                var achievement = achievements[i];

                // 填充数据（列位置应与模板匹配）
                worksheet.Cells[row, 1].Value = i + 1; // 序号
                worksheet.Cells[row, 2].Value = achievement.AchievementName;
                worksheet.Cells[row, 3].Value = achievement.Description;
                worksheet.Cells[row, 4].Value = achievement.AchievementDate.ToString("yyyy-MM-dd");
                worksheet.Cells[row, 5].Value = achievement.CompletionDegree;
                worksheet.Cells[row, 6].Value = achievement.Score;
            }

            // 自动调整列宽（可选）
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
        }

        // 清理工作表名称（Excel限制）
        private static string CleanSheetName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "Sheet1";

            // 替换非法字符
            string cleaned = name
                .Replace("\\", "")
                .Replace("/", "")
                .Replace("*", "")
                .Replace("?", "")
                .Replace("[", "")
                .Replace("]", "")
                .Replace(":", "");

            // 截断超过31字符的部分
            return cleaned.Length <= 31 ? cleaned : cleaned[..31];
        }

        //添加新类别
        private async void AddCategory_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AddCategoryDialog.Visibility = Visibility.Visible;
                NewSubcategoryName.Text = "";
                StatusText.Text = "添加新分类";

                // 获取所有大类（不限于当前用户）
                var bigCategories = await _achievementService.GetbigCategoriesAsync();

                NewCategoryName.ItemsSource = bigCategories;

                // 默认选择第一个大类
                if (bigCategories.Count != 0)
                {
                    NewCategoryName.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载类别失败: {ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //保存类别
        private async void ConfirmAddCategory_Click(object sender, RoutedEventArgs e)
        {
            string bigCategory = NewCategoryName.Text.Trim();
            string smallCategory = NewSubcategoryName.Text.Trim();


            if (string.IsNullOrWhiteSpace(bigCategory) || string.IsNullOrWhiteSpace(smallCategory))
            {
                MessageBox.Show("请输入完整的类别名称", "添加类别", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                int newCategoryId = await _achievementService.AddCategoryAsync(bigCategory, smallCategory);

                if (newCategoryId > 0)
                {
                    StatusText.Text = $"已添加类别: {bigCategory} > {smallCategory}";
                    await LoadCategoryTree(); // 刷新树形视图

                    // 检查并创建对应的二级文件夹
                    string imagesDir = 成就系统.Config.AppConfig.ImagesDirectoryPath;
                    string bigCategoryDir = System.IO.Path.Combine(imagesDir, bigCategory);
                    if (!System.IO.Directory.Exists(bigCategoryDir))
                    {
                        System.IO.Directory.CreateDirectory(bigCategoryDir);
                    }
                }
                else if (newCategoryId == -1)
                {
                    MessageBox.Show("添加失败，类别已存在", "添加类别",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    MessageBox.Show("添加类别失败，请重试", "添加类别",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show($"添加类别时出错: {ex.Message}", "错误",
                            MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                AddCategoryDialog.Visibility = Visibility.Collapsed;
            }
        }

        //取消添加类别
        private void CancelAddCategory_Click(object sender, RoutedEventArgs e)
        {
            AddCategoryDialog.Visibility = Visibility.Collapsed;
        }

        // 删除类别按钮
        private async void DeleteCategory_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 检查是否有选中的类别
                if (_selectedCategory == null)
                {
                    MessageBox.Show("请先选择一个类别", "删除类别",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 根据类别类型显示不同的确认消息
                string message;
                List<int> categoryIdsToDelete = [];
                if (_selectedCategory.Type == CategoryType.BigCategory)
                {
                    message = $"确定要删除整个大类 '{_selectedCategory.BigCategory}' 及其所有小类吗？\n" +
                              "此操作不影响关联的成就！";

                    // 获取该大类下的所有小类ID
                    foreach (var subcategory in _selectedCategory.Subcategories)
                    {
                        categoryIdsToDelete.Add(subcategory.Id);
                    }
                }
                else
                {
                    message = $"确定要删除小类 '{_selectedCategory.SmallCategory}' 吗？\n" +
                              "此操作不影响关联的成就";

                    categoryIdsToDelete.Add(_selectedCategory.Id);
                }

                // 显示确认对话框
                var result = ShowConfirmationDialog(message, "确认删除");

                if (result != MessageBoxResult.Yes) return;

                // 执行删除操作
                int successCount = 0;
                int failedCount = 0;


                DeleteButton.IsEnabled = false;


                // 遍历所有需要删除的类别
                for (int i = 0; i < categoryIdsToDelete.Count; i++)
                {
                    int categoryId = categoryIdsToDelete[i];

                    try
                    {
                        bool success = await _achievementService.DeleteCategoryAsync(categoryId);
                        if (success)
                        {
                            successCount++;
                        }
                        else
                        {
                            failedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        failedCount++;
                        Debug.WriteLine($"删除类别 {categoryId} 失败: {ex.Message}");
                    }
                }

                // 处理结果
                if (successCount > 0)
                {
                    // 保存要删除的大类名称（在刷新类别树之前）
                    string? bigCategoryNameToDelete = null;
                    if (_selectedCategory.Type == CategoryType.BigCategory)
                    {
                        bigCategoryNameToDelete = _selectedCategory.BigCategory;
                    }

                    StatusText.Text = _selectedCategory.Type == CategoryType.BigCategory
                        ? $"已删除大类 '{_selectedCategory.BigCategory}' 下的 {successCount} 个小类"
                        : $"已删除小类 '{_selectedCategory.SmallCategory}'";

                    // 刷新类别树
                    await LoadCategoryTree();

                    // 如果删除的是大类，同步删除对应的二级文件夹
                    if (_selectedCategory.Type == CategoryType.BigCategory && !string.IsNullOrWhiteSpace(bigCategoryNameToDelete))
                    {
                        string imagesDir = 成就系统.Config.AppConfig.ImagesDirectoryPath;
                        string bigCategoryName = bigCategoryNameToDelete;

                        // 安全检查：确保大类名称不为空
                        if (string.IsNullOrWhiteSpace(bigCategoryName))
                        {
                            Debug.WriteLine("大类名称为空，跳过文件夹删除");
                            return;
                        }

                        string bigCategoryDir = System.IO.Path.Combine(imagesDir, bigCategoryName);

                        // 安全检查：确保路径在图片目录内
                        if (!bigCategoryDir.StartsWith(imagesDir, StringComparison.OrdinalIgnoreCase))
                        {
                            Debug.WriteLine($"路径安全检查失败，跳过文件夹删除: {bigCategoryDir}");
                            return;
                        }

                        Debug.WriteLine($"准备删除大类文件夹: {bigCategoryDir}");

                        if (System.IO.Directory.Exists(bigCategoryDir))
                        {
                            try
                            {
                                // 再次确认文件夹存在
                                if (System.IO.Directory.Exists(bigCategoryDir))
                                {
                                    System.IO.Directory.Delete(bigCategoryDir, true);
                                    Debug.WriteLine($"成功删除大类文件夹: {bigCategoryDir}");
                                    // 显示成功消息给用户
                                    MessageBox.Show($"成功删除大类文件夹: {bigCategoryDir}", "成功",
                                                    MessageBoxButton.OK, MessageBoxImage.Information);
                                }
                                else
                                {
                                    Debug.WriteLine($"文件夹不存在，跳过删除: {bigCategoryDir}");
                                    MessageBox.Show($"文件夹不存在，跳过删除: {bigCategoryDir}", "提示",
                                                    MessageBoxButton.OK, MessageBoxImage.Information);
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"删除二级文件夹时出错: {ex.Message}");
                                // 显示错误消息给用户
                                MessageBox.Show($"删除大类文件夹失败: {ex.Message}", "错误",
                                                MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                        else
                        {
                            Debug.WriteLine($"文件夹不存在，跳过删除: {bigCategoryDir}");
                            MessageBox.Show($"文件夹不存在，跳过删除: {bigCategoryDir}", "提示",
                                            MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }

                if (failedCount > 0)
                {
                    MessageBox.Show($"删除操作部分失败，未能删除 {failedCount} 个类别", "删除失败",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"删除类别时出错: {ex.Message}", "错误",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                DeleteButton.IsEnabled = true;
            }

        }

        // 修改类别名称
        private async void EditCategoryName_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 检查是否有选中的类别
                if (_selectedCategory == null)
                {
                    MessageBox.Show("请先选择一个类别", "修改类别名称",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 根据类别类型获取当前名称
                string currentName = _selectedCategory.Type == CategoryType.BigCategory
                    ? _selectedCategory.BigCategory
                    : _selectedCategory.SmallCategory;

                // 显示WPF输入对话框
                string newName = ShowInputDialog("修改类别名称", "请输入新的类别名称", currentName);

                if (string.IsNullOrWhiteSpace(newName)) return;

                // 检查名称唯一性
                bool isUnique = true;
                if (_selectedCategory.Type == CategoryType.BigCategory)
                {
                    // 检查大类名称唯一性
                    var bigCategories = await _achievementService.GetbigCategoriesAsync();
                    isUnique = !bigCategories.Contains(newName);
                }
                else
                {
                    // 检查同一大类下小类名称唯一性
                    var smallCategories = await _achievementService.GetSmallCategoriesAsync(
                        _selectedCategory.BigCategory, UserName);
                    isUnique = !smallCategories.Contains(newName);
                }

                if (!isUnique)
                {
                    MessageBox.Show("该类别名称已存在，请输入新的名称", "修改失败",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 执行修改操作
                bool success = await _achievementService.UpdateCategoryNameAsync(
                    _selectedCategory.Id,
                    newName,
                    _selectedCategory.Type,
                    currentName);

                if (success)
                {
                    StatusText.Text = $"已成功修改类别名称为 '{newName}'";
                    await LoadCategoryTree();

                    // 如果修改的是大类，检查并创建对应的文件夹
                    if (_selectedCategory.Type == CategoryType.BigCategory)
                    {
                        string imagesDir = 成就系统.Config.AppConfig.ImagesDirectoryPath;
                        string newBigCategoryDir = System.IO.Path.Combine(imagesDir, newName);

                        // 安全检查：确保路径在图片目录内
                        if (newBigCategoryDir.StartsWith(imagesDir, StringComparison.OrdinalIgnoreCase))
                        {
                            if (!System.IO.Directory.Exists(newBigCategoryDir))
                            {
                                try
                                {
                                    System.IO.Directory.CreateDirectory(newBigCategoryDir);
                                    Debug.WriteLine($"成功创建新大类文件夹: {newBigCategoryDir}");
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine($"创建新大类文件夹时出错: {ex.Message}");
                                    // 显示错误消息给用户
                                    MessageBox.Show($"创建新大类文件夹失败: {ex.Message}", "错误",
                                                    MessageBoxButton.OK, MessageBoxImage.Error);
                                }
                            }
                            else
                            {
                                Debug.WriteLine($"新大类文件夹已存在: {newBigCategoryDir}");
                            }
                        }
                        else
                        {
                            Debug.WriteLine($"路径安全检查失败，跳过文件夹创建: {newBigCategoryDir}");
                        }
                    }
                }
                else
                {
                    MessageBox.Show("修改类别名称失败，请重试", "修改失败",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"修改类别名称时出错: {ex.Message}", "错误",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 移动到大类别
        private async void MoveToBigCategory_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 检查是否有选中的类别
                if (_selectedCategory == null)
                {
                    MessageBox.Show("请先选择一个类别", "移动类别数据",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 获取所有大类
                var bigCategories = await _achievementService.GetbigCategoriesAsync();

                // 创建WPF选择对话框
                var dialogWindow = new Window
                {
                    Title = "选择目标大类别",
                    Width = 300,
                    Height = 200,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    Background = Brushes.White,
                    Foreground = Brushes.Black
                };

                var grid = new Grid
                {
                    Margin = new Thickness(10),
                    Background = Brushes.White
                };

                // 添加列定义
                var columnDefinition = new ColumnDefinition();
                grid.ColumnDefinitions.Add(columnDefinition);

                // 添加行定义
                var rowDefinition1 = new RowDefinition { Height = GridLength.Auto };
                var rowDefinition2 = new RowDefinition { Height = new GridLength(1, GridUnitType.Star) };
                var rowDefinition3 = new RowDefinition { Height = GridLength.Auto };
                grid.RowDefinitions.Add(rowDefinition1);
                grid.RowDefinitions.Add(rowDefinition2);
                grid.RowDefinitions.Add(rowDefinition3);

                // 添加标签
                var label = new Label
                {
                    Content = "请选择目标大类别:",
                    Margin = new Thickness(0, 0, 0, 10),
                    Foreground = Brushes.Black
                };
                Grid.SetRow(label, 0);
                grid.Children.Add(label);

                // 添加组合框
                var comboBox = new ComboBox
                {
                    ItemsSource = bigCategories,
                    Margin = new Thickness(0, 0, 0, 10),
                    Background = Brushes.White,
                    Foreground = Brushes.Black,
                    BorderBrush = Brushes.LightGray
                };
                Grid.SetRow(comboBox, 1);
                grid.Children.Add(comboBox);

                // 添加按钮面板
                var buttonPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right
                };
                Grid.SetRow(buttonPanel, 2);

                var cancelButton = new Button
                {
                    Content = "取消",
                    Margin = new Thickness(0, 0, 10, 0),
                    Width = 70,
                    Background = Brushes.LightGray,
                    Foreground = Brushes.Black
                };
                cancelButton.Click += (s, args) => dialogWindow.DialogResult = false;

                var okButton = new Button
                {
                    Content = "确定",
                    Width = 70,
                    Background = Brushes.LightBlue,
                    Foreground = Brushes.Black
                };
                okButton.Click += (s, args) => dialogWindow.DialogResult = true;

                buttonPanel.Children.Add(cancelButton);
                buttonPanel.Children.Add(okButton);
                grid.Children.Add(buttonPanel);

                dialogWindow.Content = grid;

                if (dialogWindow.ShowDialog() == true && comboBox.SelectedItem != null)
                {
                    string targetBigCategory = comboBox.SelectedItem.ToString();

                    // 执行移动操作
                    bool success;
                    if (_selectedCategory.Type == CategoryType.BigCategory)
                    {
                        // 移动大类下所有小类到目标大类
                        success = await _achievementService.MoveCategoriesToBigCategoryAsync(
                            _selectedCategory.BigCategory, targetBigCategory);
                    }
                    else
                    {
                        // 移动小类到目标大类
                        success = await _achievementService.MoveCategoryToBigCategoryAsync(
                            _selectedCategory.Id, targetBigCategory);
                    }

                    if (success)
                    {
                        StatusText.Text = "移动类别数据成功";
                        await LoadCategoryTree();
                    }
                    else
                    {
                        MessageBox.Show("移动类别数据失败，请重试", "移动失败",
                                        MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"移动类别数据时出错: {ex.Message}", "错误",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 移动到小类别
        private async void MoveToSmallCategory_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 检查是否有选中的类别
                if (_selectedCategory == null)
                {
                    MessageBox.Show("请先选择一个类别", "移动类别数据",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 获取所有小类
                var allCategories = await _achievementService.GetAllCategoriesAsync();
                var smallCategories = allCategories
                    .SelectMany(c => c.Subcategories)
                    .Select(s => $"{s.BigCategory} > {s.SmallCategory}")
                    .ToList();

                // 创建WPF选择对话框
                var dialogWindow = new Window
                {
                    Title = "选择目标小类别",
                    Width = 400,
                    Height = 200,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    Background = Brushes.White,
                    Foreground = Brushes.Black
                };

                var grid = new Grid
                {
                    Margin = new Thickness(10),
                    Background = Brushes.White
                };

                // 添加列定义
                var columnDefinition = new ColumnDefinition();
                grid.ColumnDefinitions.Add(columnDefinition);

                // 添加行定义
                var rowDefinition1 = new RowDefinition { Height = GridLength.Auto };
                var rowDefinition2 = new RowDefinition { Height = new GridLength(1, GridUnitType.Star) };
                var rowDefinition3 = new RowDefinition { Height = GridLength.Auto };
                grid.RowDefinitions.Add(rowDefinition1);
                grid.RowDefinitions.Add(rowDefinition2);
                grid.RowDefinitions.Add(rowDefinition3);

                // 添加标签
                var label = new Label
                {
                    Content = "请选择目标小类别:",
                    Margin = new Thickness(0, 0, 0, 10),
                    Foreground = Brushes.Black
                };
                Grid.SetRow(label, 0);
                grid.Children.Add(label);

                // 添加组合框
                var comboBox = new ComboBox
                {
                    ItemsSource = smallCategories,
                    Margin = new Thickness(0, 0, 0, 10),
                    Background = Brushes.White,
                    Foreground = Brushes.Black,
                    BorderBrush = Brushes.LightGray
                };
                Grid.SetRow(comboBox, 1);
                grid.Children.Add(comboBox);

                // 添加按钮面板
                var buttonPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right
                };
                Grid.SetRow(buttonPanel, 2);

                var cancelButton = new Button
                {
                    Content = "取消",
                    Margin = new Thickness(0, 0, 10, 0),
                    Width = 70,
                    Background = Brushes.LightGray,
                    Foreground = Brushes.Black
                };
                cancelButton.Click += (s, args) => dialogWindow.DialogResult = false;

                var okButton = new Button
                {
                    Content = "确定",
                    Width = 70,
                    Background = Brushes.LightBlue,
                    Foreground = Brushes.Black
                };
                okButton.Click += (s, args) => dialogWindow.DialogResult = true;

                buttonPanel.Children.Add(cancelButton);
                buttonPanel.Children.Add(okButton);
                grid.Children.Add(buttonPanel);

                dialogWindow.Content = grid;

                if (dialogWindow.ShowDialog() == true && comboBox.SelectedItem != null)
                {
                    string selectedItem = comboBox.SelectedItem.ToString();
                    string[] parts = selectedItem.Split(new string[] { " > " }, StringSplitOptions.None);
                    string targetBigCategory = parts[0];
                    string targetSmallCategory = parts[1];

                    // 执行移动操作
                    bool success;
                    if (_selectedCategory.Type == CategoryType.BigCategory)
                    {
                        // 移动大类下所有小类的数据到目标小类别
                        success = await _achievementService.MoveBigCategoryDataToSmallCategoryAsync(
                            _selectedCategory.BigCategory, targetBigCategory, targetSmallCategory);
                    }
                    else
                    {
                        // 移动小类下的数据到目标小类别
                        success = await _achievementService.MoveSmallCategoryDataToSmallCategoryAsync(
                            _selectedCategory.Id, targetBigCategory, targetSmallCategory);
                    }

                    if (success)
                    {
                        StatusText.Text = "移动类别数据成功";
                        await LoadCategoryTree();
                    }
                    else
                    {
                        MessageBox.Show("移动类别数据失败，请重试", "移动失败",
                                        MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"移动类别数据时出错: {ex.Message}", "错误",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 删除类别数据
        private async void DeleteCategoryData_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 检查是否有选中的类别
                if (_selectedCategory == null)
                {
                    MessageBox.Show("请先选择一个类别", "删除类别数据",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 根据类别类型显示不同的确认消息
                string message;
                if (_selectedCategory.Type == CategoryType.BigCategory)
                {
                    message = $"确定要删除大类 '{_selectedCategory.BigCategory}' 下所有小类包含的成就数据吗？\n" +
                              "此操作将删除所有关联的成就！";
                }
                else
                {
                    message = $"确定要删除小类 '{_selectedCategory.SmallCategory}' 下所有成就数据吗？\n" +
                              "此操作将删除所有关联的成就！";
                }

                // 显示确认对话框
                var result = ShowConfirmationDialog(message, "确认删除");

                if (result != MessageBoxResult.Yes) return;

                // 执行删除操作
                bool success;
                if (_selectedCategory.Type == CategoryType.BigCategory)
                {
                    // 删除大类下所有小类的成就数据
                    success = await _achievementService.DeleteAchievementsByBigCategoryAsync(
                        _selectedCategory.BigCategory);
                }
                else
                {
                    // 删除小类下的成就数据
                    success = await _achievementService.DeleteAchievementsBySmallCategoryAsync(
                        _selectedCategory.Id);
                }

                if (success)
                {
                    StatusText.Text = "删除类别数据成功";
                    await LoadCategoryTree();
                }
                else
                {
                    MessageBox.Show("删除类别数据失败，请重试", "删除失败",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"删除类别数据时出错: {ex.Message}", "错误",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // WPF原生输入对话框
        private static string ShowInputDialog(string title, string prompt, string defaultText = "")
        {
            var window = new Window
            {
                Title = title,
                Width = 400,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize,
                Background = Brushes.White,
                Foreground = Brushes.Black
            };

            var grid = new Grid
            {
                Margin = new Thickness(20),
                Background = Brushes.White
            };

            // 添加行定义
            var rowDefinition1 = new RowDefinition { Height = GridLength.Auto };
            var rowDefinition2 = new RowDefinition { Height = GridLength.Auto };
            var rowDefinition3 = new RowDefinition { Height = GridLength.Auto };
            grid.RowDefinitions.Add(rowDefinition1);
            grid.RowDefinitions.Add(rowDefinition2);
            grid.RowDefinitions.Add(rowDefinition3);

            // 添加提示标签
            var label = new Label
            {
                Content = prompt,
                Margin = new Thickness(0, 0, 0, 10),
                HorizontalAlignment = HorizontalAlignment.Left,
                Foreground = Brushes.Black
            };
            Grid.SetRow(label, 0);
            grid.Children.Add(label);

            // 添加输入框
            var textBox = new TextBox
            {
                Text = defaultText,
                Margin = new Thickness(0, 0, 0, 20),
                Background = Brushes.White,
                Foreground = Brushes.Black,
                BorderBrush = Brushes.LightGray
            };
            Grid.SetRow(textBox, 1);
            grid.Children.Add(textBox);

            // 添加按钮面板
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            Grid.SetRow(buttonPanel, 2);

            var cancelButton = new Button
            {
                Content = "取消",
                Margin = new Thickness(0, 0, 10, 0),
                Width = 70,
                Background = Brushes.LightGray,
                Foreground = Brushes.Black
            };
            cancelButton.Click += (s, e) =>
            {
                window.DialogResult = false;
                window.Close();
            };

            var okButton = new Button
            {
                Content = "确定",
                Width = 70,
                Background = Brushes.LightBlue,
                Foreground = Brushes.Black
            };
            okButton.Click += (s, e) =>
            {
                window.DialogResult = true;
                window.Close();
            };

            buttonPanel.Children.Add(cancelButton);
            buttonPanel.Children.Add(okButton);
            grid.Children.Add(buttonPanel);

            window.Content = grid;

            // 显示对话框
            bool? result = window.ShowDialog();

            // 返回输入的文本
            return result == true ? textBox.Text : string.Empty;
        }

        // 自定义确认对话框
        private static MessageBoxResult ShowConfirmationDialog(string message, string title)
        {
            var window = new Window
            {
                Title = title,
                Width = 450,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize,
                Background = Brushes.White,
                Foreground = Brushes.Black
            };

            var grid = new Grid
            {
                Margin = new Thickness(20),
                Background = Brushes.White
            };

            // 添加行定义
            var rowDefinition1 = new RowDefinition { Height = new GridLength(1, GridUnitType.Star) };
            var rowDefinition2 = new RowDefinition { Height = GridLength.Auto };
            grid.RowDefinitions.Add(rowDefinition1);
            grid.RowDefinitions.Add(rowDefinition2);

            // 添加消息标签
            var label = new Label
            {
                Content = message,
                Margin = new Thickness(0, 0, 0, 20),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = Brushes.Black,
                FontSize = 14
            };
            Grid.SetRow(label, 0);
            grid.Children.Add(label);

            // 添加按钮面板
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            Grid.SetRow(buttonPanel, 1);

            var cancelButton = new Button
            {
                Content = "取消",
                Margin = new Thickness(0, 0, 10, 0),
                Width = 70,
                Background = Brushes.LightGray,
                Foreground = Brushes.Black
            };
            cancelButton.Click += (s, e) =>
            {
                window.DialogResult = false;
                window.Close();
            };

            var yesButton = new Button
            {
                Content = "确定",
                Width = 70,
                Background = Brushes.LightBlue,
                Foreground = Brushes.Black
            };
            yesButton.Click += (s, e) =>
            {
                window.DialogResult = true;
                window.Close();
            };

            buttonPanel.Children.Add(cancelButton);
            buttonPanel.Children.Add(yesButton);
            grid.Children.Add(buttonPanel);

            window.Content = grid;

            // 显示对话框
            bool? result = window.ShowDialog();

            // 返回结果
            return result == true ? MessageBoxResult.Yes : MessageBoxResult.No;
        }
    }
}
