using MySql.Data.MySqlClient;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Text;
using 成就系统.Utilities;

namespace 成就系统.Services
{
    class AdbDeviceManager : IDisposable
    {
        private readonly string _adbPath;   // 配置读取策略：通过ThemeSettings获取ADB路径
        private const int MaxRetries = 2;   //容错机制：关键操作最大重试次数
        private bool _disposed;             //资源管理：实现IDisposable接口防止资源泄露
        private readonly Process? _activeProcess = null;

        public AdbDeviceManager()
        {
            // 从设置中加载ADB路径
            string savedPath = ThemeSettings.LoadAdbToolPath();

            // 如果没有保存的路径，使用默认路径
            if (string.IsNullOrEmpty(savedPath))
            {
                savedPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "adb.exe");
            }

            _adbPath = savedPath;
            // 不再在构造函数中验证ADB文件存在性，允许软件正常启动
        }

        /// <summary>
        /// 检查ADB文件是否存在
        /// </summary>
        /// <returns>ADB文件是否存在</returns>
        public bool IsAdbAvailable()
        {
            return File.Exists(_adbPath);
        }

        /// <summary>
        /// 获取ADB路径
        /// </summary>
        /// <returns>ADB路径</returns>
        public string GetAdbPath()
        {
            return _adbPath;
        }

        // 异步获取Android设备型号信息（ADB协议实现）
        public async Task<string> GetDeviceNameAsync()
        {
            try
            {
                var output = await ExecuteAdbCommandAsync("shell getprop ro.product.model");
                return string.IsNullOrWhiteSpace(output) ? "未知设备" : output.Trim();      // 三元表达式处理空响应（防御性编程）
            }
            catch
            {
                return "不知名的设备";            // 统一异常兜底（设备信息标准化）
            }
        }

        //人脸采集
        public async Task<string> CaptureFaceImageAsync(bool skipCameraManagement = false)
        {
            try
            {
                if (!skipCameraManagement)
                {
                    await ValidateDeviceState();
                    await LaunchCameraAppWithRetry();
                }

                return await CaptureAndSaveScreenshot();
            }
            finally
            {
                // 只有不跳过摄像头管理时才关闭
                if (!skipCameraManagement)
                {
                    await CloseCameraAppAsync();
                }
            }
        }

        //摄像头启动流程
        public async Task PrepareCameraForCaptureAsync(bool autoClose = false)
        {
            // 1. 验证设备状态
            await ValidateDeviceState();
            await LaunchCameraAppWithRetry();

            if (autoClose)
            {
                // 设置自动关闭
            }
        }

        //设备状态验证模块
        public async Task ValidateDeviceState()
        {
            //双重状态校验：连接状态+屏幕状态
            if (!await IsDeviceConnectedAsync())
                throw new DeviceException(DeviceErrorType.NotConnected);

            //屏幕唤醒策略：检测到休眠立即唤醒
            if (!await IsScreenAwakeAsync())
                await WakeScreenAsync();
        }

        //连接状态检测
        public async Task<bool> IsDeviceConnectedAsync()
        {
            var output = await ExecuteAdbCommandAsync("devices");
            return output.Contains("\tdevice");
        }

        //屏幕状态检测
        public async Task<bool> IsScreenAwakeAsync()
        {
            var output = await ExecuteAdbCommandAsync("shell dumpsys power");   // 获取电源服务状态信息
            return output.Contains("mWakefulness=Awake");                       // 解析唤醒状态标志（兼容不同Android版本）
        }

        //唤醒屏幕
        public async Task WakeScreenAsync()
        {
            await ExecuteAdbCommandAsync("shell input keyevent KEYCODE_POWER"); // 发送电源键事件（物理按键模拟）
            await Task.Delay(1000);                                             // 等待屏幕完全唤醒（经验值1秒，可动态调整）
        }

        //启动应用
        private async Task LaunchCameraAppWithRetry()
        {
            // 先检测是否已处于拍照模式
            if (await IsCameraAppActiveAsync())
                return;

            //重试策略
            for (int i = 0; i <= MaxRetries; i++)
            {
                // 使用IMAGE_CAPTURE意图启动前置摄像头
                await ExecuteAdbCommandAsync("shell am start -a android.media.action.IMAGE_CAPTURE --ei android.intent.extras.CAMERA_FACING 1");
                await Task.Delay(1000); // 异步等待与状态检测

                if (await IsCameraAppActiveAsync())
                    return;

                if (i == MaxRetries)    // 异常熔断机制,超过最大重试次数则抛出异常
                    throw new DeviceException(DeviceErrorType.CameraLaunchFailed);
            }
        }

        //检测应用状态
        private async Task<bool> IsCameraAppActiveAsync()
        {
            //var output = await ExecuteAdbCommandAsync("shell dumpsys activity activities");
            //if (!output.Contains("com.android.camera/com.android.camera.Camera"))
            //{
            //    return false;
            //}

            //output = await ExecuteAdbCommandAsync("shell \"dumpsys window | grep mCurrentFocus\"");
            //if (output.Contains("com.android.camera"))
            //{
            //    return true;     
            //}
            //else
            //{
            //    output = await ExecuteAdbCommandAsync("shell am start -a android.media.action.IMAGE_CAPTURE --ei android.intent.extras.CAMERA_FACING 1");
            //    return (output.Contains("Warning: Activity not started, its current task has been brought to the front"));
            //}

            var output = await ExecuteAdbCommandAsync("shell \"dumpsys window | grep mCurrentFocus\"");
            if (output.Contains("com.android.camera"))
            {
                return true;
            }

            output = await ExecuteAdbCommandAsync("shell dumpsys activity activities");
            if (output.Contains("com.android.camera/com.android.camera.Camera"))
            {
                await ExecuteAdbCommandAsync("shell am force-stop com.android.camera");
            }
            return false;
        }

        // 添加关闭相机应用的方法
        public async Task CloseCameraAppAsync()
        {
            try
            {
                // 双重关闭策略：先尝试优雅关闭，再强制停止
                await ExecuteAdbCommandAsync("shell input keyevent KEYCODE_BACK");
                await Task.Delay(300);
                await ExecuteAdbCommandAsync("shell am force-stop com.android.camera");
            }
            catch
            {
                // 静默处理关闭失败
            }
        }

        //只拍照，不开关摄像头
        public async Task<string> CaptureFaceImageWithoutCameraControlAsync()
        {
            // 直接拍照，不执行摄像头管理
            return await CaptureAndSaveScreenshot();
        }

        // 人脸验证
        public async Task<bool> VerifyFaceAsync(string username)
        {
            try
            {
                // 捕获当前人脸图像
                string imagePath = await CaptureFaceImageAsync();
                var currentImage = File.ReadAllBytes(imagePath);

                // 提取当前人脸特征
                float[] currentEmbeddings;
                using (var faceService = new FaceRecognitionService())
                {
                    currentEmbeddings = faceService.GetFaceEmbeddings(currentImage);
                    if (currentEmbeddings == null)
                        return false;
                }

                // 从数据库获取用户存储的人脸特征
                using var userService = new UserService();
                using var db = new DatabaseHelper();

                // 直接查询当前用户的人脸特征
                string usersTable = DatabaseTableManager.GetUsersTable();
                string sql = $"SELECT face_embeddings FROM {usersTable} WHERE account = @username";
                var parameter = new MySqlParameter("@username", username);

                using var dt = await db.ExecuteQuery(sql, parameter);
                if (dt.Rows.Count == 0)
                    return false;

                DataRow row = dt.Rows[0];
                if (row["face_embeddings"] is DBNull)
                    return false;

                // 字节流→浮点数组转换
                var embeddingsBytes = (byte[])row["face_embeddings"];
                var storedEmbeddings = new float[embeddingsBytes.Length / 4];
                Buffer.BlockCopy(embeddingsBytes, 0, storedEmbeddings, 0, embeddingsBytes.Length);

                // 比较人脸特征
                using var faceService2 = new FaceRecognitionService();
                bool isMatch = FaceRecognitionService.CompareFaces(storedEmbeddings, currentEmbeddings);

                return isMatch;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"人脸验证失败: {ex.Message}");
                return false;
            }
        }

        //截图保存模块
        private async Task<string> CaptureAndSaveScreenshot()
        {
            // 阶段1：路径生成与存储准备
            var savePath = GetScreenshotSavePath();                     // 生成带时间戳的保存路径（防止文件名冲突）
            string? directoryPath = Path.GetDirectoryName(savePath);
            if (!string.IsNullOrEmpty(directoryPath))
            {
                Directory.CreateDirectory(directoryPath); // 自动创建目录（处理多级嵌套路径）
            }

            //阶段2： ADB 截图与流式写入
            using (var fileStream = File.Create(savePath))
            {
                await ExecuteAdbCommandAsync("exec-out screencap -p", outputStream: fileStream);
            }

            // 阶段3：结果校验与异常处理
            if (!File.Exists(savePath))
                throw new DeviceException(DeviceErrorType.ScreenshotFailed);

            // 阶段4：图像后处理（裁剪与尺寸调整）
            try
            {
                // 使用FaceRecognitionService进行裁剪
                using var faceService = new FaceRecognitionService();
                var croppedPath = FaceRecognitionService.CropAndResizeImage(savePath); //224 224

                // 替换原图
                File.Delete(savePath);
                File.Move(croppedPath, savePath);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"图片裁剪失败: {ex.Message}");
                // 裁剪失败时保留原图
            }

            return savePath;
        }

        //生成带时间戳的字符串
        private static string GetScreenshotSavePath()
        {
            // 获取应用程序执行目录
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            // 创建faceImage子目录
            string faceImageDir = Path.Combine(baseDir, "faceImage");
            Directory.CreateDirectory(faceImageDir); // 确保目录存在

            return Path.Combine(faceImageDir, $"{DateTime.Now:yyyyMMdd_HHmmss}.png");
        }

        //Adb命令执行引擎
        private async Task<string> ExecuteAdbCommandAsync(string arguments, Stream? outputStream = null)
        {
            // 检查ADB是否可用
            if (!File.Exists(_adbPath))
            {
                throw new DeviceException(DeviceErrorType.CommandExecutionFailed, $"ADB未找到：{_adbPath}");
            }

            // // 进程配置（隐藏命令行窗口）
            using var process = new Process
            {
                StartInfo = CreateProcessStartInfo(arguments)
            };
            process.Start();

            var error = new StringBuilder();
            process.ErrorDataReceived += (sender, e) => error.AppendLine(e.Data);

            var output = await ProcessOutputAsync(process, outputStream);   // 异步处理输出流（支持直接写入文件）                                  
            using var cts = new CancellationTokenSource(15000);             // 添加执行超时控制（30秒熔断）
            await process.WaitForExitAsync(cts.Token);                      // 等待进程退出（防止僵尸进程）

            // 非零退出码处理（标准错误流优先）
            if (process.ExitCode != 0)
                throw new DeviceException(DeviceErrorType.CommandExecutionFailed, $"Exit code: {process.ExitCode}, Error: {error.ToString().Trim()}");

            return output;
        }

        //进程配置
        private ProcessStartInfo CreateProcessStartInfo(string arguments)
        {
            return new ProcessStartInfo
            {
                FileName = _adbPath,                       // 避免硬编码路径，从配置文件动态获取
                Arguments = arguments,                     // 参数隔离设计，防止命令注入攻击
                UseShellExecute = false,                   // 禁用Shell执行确保安全
                RedirectStandardOutput = true,             // 重定向输出流实现异步捕获
                RedirectStandardError = true,              // 分离错误流与标准输出流
                CreateNoWindow = true                      // 隐藏命令行窗口提升用户体验
            };
        }

        //异步输出处理引擎
        private static async Task<string> ProcessOutputAsync(Process process, Stream? outputStream)
        {
            var output = "";
            if (outputStream != null)                                               // 双通道输出设计：支持直接流写入或字符串返回
            {
                await process.StandardOutput.BaseStream.CopyToAsync(outputStream);  // 流式复制避免内存溢出（适合处理大体积截图）
            }
            else
            {
                output = await process.StandardOutput.ReadToEndAsync();             // 全量读取适合小数据量场景
            }

            // 错误流优先处理原则（防止错误信息被遗漏）
            var error = await process.StandardError.ReadToEndAsync();
            if (!string.IsNullOrEmpty(error))
                throw new DeviceException(DeviceErrorType.CommandExecutionFailed, error);

            return output;
        }

        //资源释放模式
        public void Dispose()
        {
            if (_disposed) return;

            _activeProcess?.Kill();
            _activeProcess?.Dispose();

            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }

    //错误分类枚举
    public enum DeviceErrorType
    {
        NotConnected,           // 设备离线
        ScreenLocked,           // 屏幕锁定
        CameraLaunchFailed,     // 相机启动失败
        ScreenshotFailed,       // 截图失败
        CommandExecutionFailed,  // 命令执行异常
        FaceCaptureFailed      // 人脸捕获失败
    }

    //异常类实现
    public class DeviceException : Exception
    {
        // 自定义异常类型携带错误分类
        public DeviceErrorType ErrorType { get; }
        public string ErrorCode => DeviceErrorLocalizer.GetErrorCode(ErrorType);

        public DeviceException(DeviceErrorType errorType, string? message = null)
            : base(message ?? DeviceErrorLocalizer.GetTechMessage(errorType))
        {
            ErrorType = errorType;
            Data.Add("ErrorCode", ErrorCode);  // 保留日志记录能力
        }
    }

    // 设备错误类型扩展方法
    public static class DeviceErrorTypeExtensions
    {
        public static string GetDescription(this DeviceErrorType type)
            => DeviceErrorLocalizer.GetTechMessage(type);
    }
}
