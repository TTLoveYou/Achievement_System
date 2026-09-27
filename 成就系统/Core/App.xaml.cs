﻿﻿﻿﻿﻿﻿﻿﻿using System.Diagnostics;
using System.IO;
using System.Windows;
using 成就系统.Config;
using 成就系统.Services;


namespace 成就系统.Core
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            // 初始化主题管理器
            ThemeManager.Initialize();
            EnsureImagesDirectoryExists();
        }

        //创建图片文件夹
        private void EnsureImagesDirectoryExists()
        {
            try
            {
                string imagesDir = AppConfig.ImagesDirectoryPath;

                // 确保目录存在
                if (!Directory.Exists(imagesDir))
                {
                    Directory.CreateDirectory(imagesDir);
                    Debug.WriteLine($"创建图片目录: {imagesDir}");
                }

                // 确保默认图片存在
                string defaultImage = AppConfig.DefaultImagePath;
                if (!File.Exists(defaultImage))
                {
                    // 从资源复制默认图片
                    string resourcePath = "pack://application:,,,/成就系统;component/resources/asImages/DefaultImage.png";
                    var uri = new Uri(resourcePath, UriKind.Absolute);

                    using (var stream = Application.GetResourceStream(uri).Stream)
                    {
                        using (var fileStream = File.Create(defaultImage))
                        {
                            stream.CopyTo(fileStream);
                        }
                    }
                }

                // 首次运行时检查和创建二级文件夹的逻辑已移至登录成功后执行
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"创建图片目录时出错: {ex.Message}");
            }
        }

        // 检查并创建所有成就大类对应的二级文件夹
        private void CheckAndCreateBigCategoryDirectories()
        {
            try
            {
                using (var achievementService = new AchievementService())
                {
                    // 获取所有成就大类
                    var bigCategories = achievementService.GetbigCategoriesAsync().Result;
                    string imagesDir = AppConfig.ImagesDirectoryPath;

                    // 检查并创建每个大类对应的二级文件夹
                    foreach (var bigCategory in bigCategories)
                    {
                        string bigCategoryDir = System.IO.Path.Combine(imagesDir, bigCategory);
                        if (!System.IO.Directory.Exists(bigCategoryDir))
                        {
                            System.IO.Directory.CreateDirectory(bigCategoryDir);
                            Debug.WriteLine($"创建大类文件夹: {bigCategoryDir}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"检查和创建二级文件夹时出错: {ex.Message}");
            }
        }

    }
}
