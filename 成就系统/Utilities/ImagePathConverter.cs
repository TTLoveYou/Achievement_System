using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using 成就系统.Config;

namespace 成就系统.Utilities
{
    public class ImagePathConverter : IValueConverter
    {
        //路径 ==> 图片
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                string? imagePath = value as string;

                // 使用全局默认路径
                string defaultPath = AppConfig.DefaultImagePath;

                // 检查路径有效性
                if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
                {
                    // 尝试加载默认头像
                    return LoadImageSafe(defaultPath);
                }

                // 尝试加载指定图片
                return LoadImageSafe(imagePath);
            }

            catch
            {
                return LoadFallbackImage();
            }
        }

        // 安全加载图片（带错误处理）
        private static BitmapSource LoadImageSafe(string filePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                    return LoadFallbackImage();

                var bitmap = new BitmapImage();
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = stream;
                    bitmap.EndInit();
                }

                // 确保图片已加载完成
                bitmap.Freeze();
                return bitmap;
            }
            catch
            {
                return LoadFallbackImage();
            }
        }

        // 后备图片（当所有加载都失败时使用）
        private static BitmapSource LoadFallbackImage()
        {
            try
            {
                // 尝试加载嵌入的默认图片资源
                var uri = new Uri("pack://application:,,,/成就系统;component/Resources/AsImages/DefaultImage.png", UriKind.Absolute);
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = uri;
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
            catch
            {
                // 终极后备方案 - 创建单色图片
                return CreateSolidColorImage();
            }
        }

        // 创建单色图片作为最后的备选方案
        private static BitmapSource CreateSolidColorImage()
        {
            try
            {
                // 创建纯色位图
                int width = 100;
                int height = 100;
                WriteableBitmap writeableBitmap = new(
                    width, height, 96, 96, PixelFormats.Bgr32, null);

                int bytesPerPixel = 4; // Bgr32 格式，每个像素4字节
                int stride = width * bytesPerPixel;
                byte[] pixels = new byte[height * stride];

                // 填充浅灰色 (R:200, G:200, B:200)
                for (int i = 0; i < pixels.Length; i += bytesPerPixel)
                {
                    pixels[i] = 200;     // Blue
                    pixels[i + 1] = 200; // Green
                    pixels[i + 2] = 200; // Red
                                         // Alpha通道不需要设置，Bgr32格式没有Alpha
                }

                // 写入像素数据
                writeableBitmap.WritePixels(new Int32Rect(0, 0, width, height),
                    pixels, stride, 0);
                writeableBitmap.Freeze();

                return writeableBitmap;
            }
            catch
            {
                // 终极后备方案 - 创建纯色位图作为图像源
                DrawingVisual drawingVisual = new();
                using (DrawingContext drawingContext = drawingVisual.RenderOpen())
                {
                    drawingContext.DrawRectangle(Brushes.LightGray, null, new Rect(0, 0, 100, 100));
                }

                RenderTargetBitmap rtb = new(
                    100, 100, 96, 96, PixelFormats.Pbgra32);
                rtb.Render(drawingVisual);
                rtb.Freeze();

                return rtb;
            }
        }

        //清除图片缓存
        public static void ClearImageCache(string filePath)
        {
            try
            {
                BitmapImage bitmap = new();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.None;
                bitmap.UriSource = new Uri(filePath, UriKind.Absolute);
                bitmap.EndInit();
                bitmap.Freeze();
            }
            catch { /* 忽略异常 */ }
        }

        //图片 ==> (sting)路径
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}