using System.IO;
using 成就系统.Services;

namespace 成就系统.Config
{
    public static class AppConfig
    {
        // 支持的图片格式（包括常见格式）
        public static readonly string[] SupportedImageExtensions = {
            ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".tiff"
        };

        // 获取图片目录路径（优先使用保存的设置，否则使用默认路径）
        public static string ImagesDirectoryPath
        {
            get
            {
                // 尝试加载保存的图片路径设置
                string savedPath = ThemeSettings.LoadAchievementsImagePath();
                if (!string.IsNullOrEmpty(savedPath) && Directory.Exists(savedPath))
                {
                    return savedPath;
                }
                // 如果没有保存的设置或路径不存在，使用默认路径
                return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "AsImages");
            }
        }

        // 默认图片路径
        public static string DefaultImagePath =>
            Path.Combine(ImagesDirectoryPath, "DefaultImage.png");
    }
}
