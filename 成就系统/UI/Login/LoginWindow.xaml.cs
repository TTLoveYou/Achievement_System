using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using 成就系统.Services;
using 成就系统.UI.Main;
using 成就系统.Utilities;


namespace 成就系统.UI.Login
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        private readonly UserService userService = new();
        private readonly AdbDeviceManager _deviceManager = new(); // 新增设备管理器实例
        private readonly List<Control> _sensitiveControls = [];   //用于存储控件的列表

        public LoginWindow()
        {
            InitializeComponent();
            Loaded += async (s, e) => await InitializeDeviceStatusAsync(); // 窗体加载事件绑定异步初始化（生命周期管理）
        }

        //统一管理控件
        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            // 初始化敏感控件列表
            _sensitiveControls.AddRange(
            [
                btnLogin,
                btnRegister,
                btnFaceLogin,
                btnForgetPassword,
                btnRefresh,
                txtUsername,
                pwdPassword
            ]);
        }

        //超链接委托
        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = e.Uri.AbsoluteUri,
                UseShellExecute = true
            });
            e.Handled = true;
        }

        //登录请求
        private async void Login_Response(object sender, RoutedEventArgs e)
        {
            try
            {
                // 禁用所有敏感控件
                SetSensitiveControlsEnabled(false);

                // 验证输入
                if (!ValidateUsernameInput(out string username)) return;
                if (!ValidatePasswordInput(out string password)) return;

                // 检查是否为特殊凭证（打开配置文件）
                if (username == "KunKun" && password == "Opendata@110")
                {
                    // 打开配置文件
                    await PerformSecureOperation(() =>
                    {
                        try
                        {
                            string configFilePath = ThemeSettings.GetSettingsFilePath();
                            if (File.Exists(configFilePath))
                            {
                                Process.Start(new ProcessStartInfo
                                {
                                    FileName = configFilePath,
                                    UseShellExecute = true
                                });
                                LoadingText.Text = "已打开配置文件!";
                            }
                            else
                            {
                                LoadingText.Text = "配置文件不存在!";
                                MessageBox.Show("配置文件不存在，请先运行一次程序生成配置文件", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                        }
                        catch (Exception ex)
                        {
                            LoadingText.Text = "打开配置文件失败!";
                            MessageBox.Show($"打开配置文件失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        return Task.CompletedTask;
                    }, "打开配置文件");
                    return;
                }

                // 执行常规登录
                await PerformSecureOperation(async () =>
                {
                    bool isSuccess = await userService.ValidateLoginAsync(username, password);
                    LoadingText.Text = isSuccess ? "登录成功!" : "登录失败!";

                    if (isSuccess)
                    {
                        // 在UI线程上执行跳转
                        Dispatcher.Invoke(() =>
                        {
                            // 创建主窗口实例
                            var mainWindow = new MainWindow(username);
                            mainWindow.Show();

                            // 设置新主窗口
                            Application.Current.MainWindow = mainWindow;

                            // 关闭登录窗口
                            this.Close();
                        });

                        // 登录成功后创建用户拥有的大类文件夹
                        await CreateUserBigCategoryDirectories(username);
                    }
                }, "登录");
            }
            finally
            {
                // 恢复控件状态
                SetSensitiveControlsEnabled(true);

                // 新增：无论注册成功与否，都清除输入框内容
                txtUsername.Text = string.Empty;
                pwdPassword.Password = string.Empty;
            }
        }

        //注册请求
        private async void Response_Response(object sender, RoutedEventArgs e)
        {
            try
            {
                // 禁用所有敏感控件
                SetSensitiveControlsEnabled(false);

                // 验证输入
                if (!ValidateUsernameInput(out string username)) return;
                if (!ValidatePasswordInput(out string password)) return;

                // 检查账户是否存在
                if (await userService.AccountExistsAsync(username))
                {
                    MessageBox.Show("用户名已存在！", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 执行注册流程
                await PerformSecureOperation(async () =>
                {
                    await PerformRegistration(username, password);
                }, "注册");
            }
            finally
            {
                // 恢复控件状态
                SetSensitiveControlsEnabled(true);

                // 新增：无论注册成功与否，都清除输入框内容
                txtUsername.Text = string.Empty;
                pwdPassword.Password = string.Empty;
            }
        }

        //人脸快捷登录
        private async void FaceLogin_Click(object sender, RoutedEventArgs e)
        {
            string? recognizedUsername = null; // 存储识别结果

            try
            {
                // 禁用所有敏感控件
                SetSensitiveControlsEnabled(false);

                // 执行人脸登录
                await PerformSecureOperation(async () =>
                {
                    recognizedUsername = await PerformFaceLogin();
                }, "人脸登录");

                //创建主窗口
                if (!string.IsNullOrEmpty(recognizedUsername))
                {
                    Dispatcher.Invoke(() =>
                    {
                        // 使用识别的用户名创建主窗口
                        var mainWindow = new MainWindow(recognizedUsername);
                        mainWindow.Show();
                        Application.Current.MainWindow = mainWindow;
                        this.Close();
                    });

                    // 登录成功后创建用户拥有的大类文件夹
                    await CreateUserBigCategoryDirectories(recognizedUsername);
                }
            }
            finally
            {
                // 恢复控件状态
                SetSensitiveControlsEnabled(true);
            }
        }

        //忘记密码
        private async void ForgetPassword_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 禁用所有敏感控件
                SetSensitiveControlsEnabled(false);

                // 验证输入
                if (!ValidateUsernameInput(out string username)) return;
                if (!ValidatePasswordInput(out string newPassword)) return;
                // 检查账户是否存在
                if (!await userService.AccountExistsAsync(username))
                {
                    MessageBox.Show("用户名不存在", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // 执行密码重置
                await PerformSecureOperation(async () =>
                {
                    await PerformPasswordReset(username, newPassword);
                }, "密码重置");
            }
            finally
            {
                // 恢复控件状态
                SetSensitiveControlsEnabled(true);

                // 新增：无论注册成功与否，都清除输入框内容
                txtUsername.Text = string.Empty;
                pwdPassword.Password = string.Empty;
            }
        }

        // 创建用户拥有的大类文件夹
        private static async Task CreateUserBigCategoryDirectories(string username)
        {
            try
            {
                using var achievementService = new AchievementService();
                // 获取用户拥有的所有成就大类
                var bigCategories = await achievementService.GetbigCategoriesAsync(username);
                string imagesDir = 成就系统.Config.AppConfig.ImagesDirectoryPath;

                // 确保图片目录存在
                if (!System.IO.Directory.Exists(imagesDir))
                {
                    System.IO.Directory.CreateDirectory(imagesDir);
                    System.Diagnostics.Debug.WriteLine($"创建图片目录: {imagesDir}");
                }

                // 检查并创建每个大类对应的二级文件夹
                foreach (var bigCategory in bigCategories)
                {
                    string bigCategoryDir = System.IO.Path.Combine(imagesDir, bigCategory);
                    if (!System.IO.Directory.Exists(bigCategoryDir))
                    {
                        System.IO.Directory.CreateDirectory(bigCategoryDir);
                        System.Diagnostics.Debug.WriteLine($"创建用户大类文件夹: {bigCategoryDir}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"创建用户大类文件夹时出错: {ex.Message}");
            }
        }

        // 设备状态初始化方法
        private async Task InitializeDeviceStatusAsync()
        {
            try
            {
                // 检查ADB是否可用
                if (!_deviceManager.IsAdbAvailable())
                {
                    // ADB不可用，显示提示信息
                    Dispatcher.Invoke(() =>
                    {
                        DevicesName.Text = "警告：ADB未找到！"; // 使用扩展方法
                        DevicesName.Foreground = Brushes.Red;
                    });
                    return;
                }

                // 双重验证机制（设备状态检查+信息获取）
                await _deviceManager.ValidateDeviceState();
                var deviceName = await _deviceManager.GetDeviceNameAsync();

                // 使用Dispatcher确保UI线程更新(UI线程安全更新（跨线程通信方案）)
                Dispatcher.Invoke(() =>
                {
                    DevicesName.Text = $"设备名称：{deviceName}";
                    DevicesName.Foreground = Brushes.Green;     // 状态颜色编码体系
                });
            }
            catch (DeviceException ex)
            {
                // 使用扩展方法实现关注点分离（SOC原则）
                Dispatcher.Invoke(() =>
                {
                    DevicesName.Text = $"设备错误：{ex.ErrorType.GetDescription()}"; // 使用扩展方法
                    // 根据错误类型设置不同颜色
                    if (ex.ErrorType == DeviceErrorType.NotConnected)
                    {
                        DevicesName.Foreground = Brushes.Yellow;
                    }
                    else
                    {
                        DevicesName.Foreground = Brushes.Red;
                    }
                });
            }
            catch (Exception ex)
            {
                // 系统级异常特殊处理（分层异常体系）
                Dispatcher.Invoke(() =>
                {
                    DevicesName.Text = $"初始化异常：{ex.Message}";
                    DevicesName.Foreground = Brushes.DarkOrange;        // 警告级别颜色
                });
            }
        }

        // 刷新按钮点击事件
        private async void RefreshDeviceStatus_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button) return;

            // 禁用按钮防止重复点击
            button.IsEnabled = false;

            try
            {
                // 调用设备状态初始化
                await InitializeDeviceStatusAsync();
                LoadingIcon.IsActive = true;
                LoadingText.Text = "刷新中…";
            }
            finally
            {
                // 恢复按钮状态
                button.IsEnabled = true;
                LoadingIcon.IsActive = false;
                LoadingText.Text = "刷新成功";
                await Task.Delay(6000);
                LoadingText.Text = "";
            }
        }

        //禁用/使能 按钮，防重复点击
        private void SetControlsEnabled(bool enabled)
        {
            txtUsername.IsEnabled = enabled;
            pwdPassword.IsEnabled = enabled;
            btnLogin.IsEnabled = enabled;
            btnRegister.IsEnabled = enabled;
            btnForgetPassword.IsEnabled = enabled;
            btnFaceLogin.IsEnabled = enabled;
        }

        // 设置敏感控件状态    禁用/使能
        private void SetSensitiveControlsEnabled(bool enabled)
        {
            foreach (var control in _sensitiveControls)
            {
                control.IsEnabled = enabled;
            }
        }

        // 执行安全操作（带状态管理和错误处理）
        private async Task PerformSecureOperation(Func<Task> operation, string operationName)
        {
            LoadingIcon.IsActive = true;
            LoadingText.Text = $"{operationName}中...";

            try
            {
                await operation();
            }
            catch (DeviceException dex)
            {
                HandleDeviceError(dex.ErrorType);
            }
            catch (FaceRecognitionException frex)
            {
                LoadingText.Text = $"{operationName}失败";
                MessageBox.Show(frex.Message, "验证失败",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                LoadingText.Text = $"{operationName}异常";
                MessageBox.Show($"发生错误: {ex.Message}", "系统错误",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                LoadingIcon.IsActive = false;
                await Task.Delay(1500);
                LoadingText.Text = "";
            }
        }

        // 用户名输入验证
        private bool ValidateUsernameInput(out string username)
        {
            username = txtUsername.Text.Trim();

            if (string.IsNullOrWhiteSpace(username))
            {
                MessageBox.Show("请输入用户名", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (username.Length < 4)
            {
                MessageBox.Show("用户名至少需要4个字符", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        // 密码输入验证
        private bool ValidatePasswordInput(out string password)
        {
            password = pwdPassword.Password;

            if (string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("请输入密码", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // 密码强度验证（复用注册时的规则）
            var regex = MyRegex();
            if (!regex.IsMatch(password))
            {
                MessageBox.Show("密码必须包含大小写字母和数字，且长度至少8位", "提示",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        // 执行注册流程
        private async Task PerformRegistration(string username, string password)
        {
            // 询问用户是否录入人脸
            var dialogResult = MessageBox.Show(
                "是否现在录入人脸或选择照片？\n\n是 - 使用摄像头拍照\n否 - 选择本地照片\n取消 - 跳过人脸录入",
                "人脸录入",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);

            byte[]? faceData = null;
            string? tempImagePath = null; // 用于保存临时截图路径

            switch (dialogResult)
            {
                case MessageBoxResult.Yes: // 使用摄像头拍照
                    string cameraPriority = GetCameraPriority();
                    if (cameraPriority == "Adb" && _deviceManager.IsAdbAvailable())
                    {
                        // 使用ADB相机
                        var (adbFaceData, adbTempPath) = await CaptureFaceWithAdbCamera();
                        faceData = adbFaceData;
                        tempImagePath = adbTempPath;
                    }
                    else
                    {
                        // 使用本地相机
                        var (localFaceData, localTempPath) = await CaptureFaceWithLocalCamera();
                        faceData = localFaceData;
                        tempImagePath = localTempPath;
                    }
                    break;

                case MessageBoxResult.No: // 选择本地照片
                    var openFileDialog = new Microsoft.Win32.OpenFileDialog
                    {
                        Filter = "图片文件|*.jpg;*.jpeg;*.png;*.bmp",
                        Title = "选择人脸照片"
                    };

                    if (openFileDialog.ShowDialog() == true)
                    {
                        LoadingText.Text = "正在处理照片...";

                        try
                        {
                            // 新增：使用临时文件路径进行裁剪
                            tempImagePath = openFileDialog.FileName;
                            string? croppedPath = null;

                            // 使用FaceRecognitionService裁剪图片
                            using (var faceService = new FaceRecognitionService())
                            {
                                croppedPath = FaceRecognitionService.CropAndResizeImage(tempImagePath);
                            }

                            // 读取裁剪后的图片
                            faceData = File.ReadAllBytes(croppedPath);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"本地照片裁剪失败: {ex.Message}");
                            // 裁剪失败时尝试直接使用原图
                            try
                            {
                                faceData = File.ReadAllBytes(openFileDialog.FileName);
                                MessageBox.Show("图片裁剪失败，已使用原始图片。建议使用正脸清晰照片以提高识别率。",
                                                "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                            catch
                            {
                                MessageBox.Show("图片处理失败，请选择其他照片", "错误",
                                               MessageBoxButton.OK, MessageBoxImage.Error);
                                faceData = null;
                            }
                        }
                    }
                    break;

                case MessageBoxResult.Cancel: // 跳过人脸录入
                default:
                    break;
            }

            // 执行注册（带人脸数据）
            bool isSuccess = await userService.RegisterAsync(username, password, faceData);
            LoadingText.Text = isSuccess ? "注册成功!请重新登录！" : "注册失败!请稍后重试！";

            // 清理临时文件
            if (tempImagePath != null && File.Exists(tempImagePath))
            {
                File.Delete(tempImagePath);
            }
        }

        //使用ADB相机捕获人脸
        private async Task<(byte[]?, string?)> CaptureFaceWithAdbCamera()
        {
            string? tempImagePath = null;
            byte[]? faceData = null;

            try
            {
                LoadingText.Text = "正在启动摄像头...";

                // 1. 启动前置摄像头
                await _deviceManager.PrepareCameraForCaptureAsync();

                // 2. 准备拍照
                LoadingText.Text = "摄像头已就绪，请按空格键拍照";
                var captureCompletionSource = new TaskCompletionSource<bool>();

                // 使用局部函数作为事件处理程序，设置空格键监听
                void KeyDownHandler(object s, KeyEventArgs args)
                {
                    if (args.Key == Key.Space)
                    {
                        args.Handled = true; // 阻止事件继续传播
                        captureCompletionSource.TrySetResult(true);
                    }
                }

                this.KeyDown += KeyDownHandler;

                try
                {
                    // 4. 等待空格键或超时
                    var timeoutTask = Task.Delay(30000);
                    var completedTask = await Task.WhenAny(captureCompletionSource.Task, timeoutTask);

                    if (completedTask == timeoutTask)
                    {
                        throw new TimeoutException("拍照超时，未检测到操作");
                    }

                    // 5. 执行拍照
                    LoadingText.Text = "正在拍照...";
                    tempImagePath = await _deviceManager.CaptureFaceImageAsync();
                    LoadingText.Text = "正在处理人脸图像...";
                    faceData = File.ReadAllBytes(tempImagePath);
                }
                finally
                {
                    // 确保移除事件处理
                    this.KeyDown -= KeyDownHandler;
                    // 确保关闭相机（双重保障）
                    await _deviceManager.CloseCameraAppAsync();
                }
            }
            catch (Exception ex)
            {
                LoadingText.Text = "相机操作失败";
                MessageBox.Show($"相机操作失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return (faceData, tempImagePath);
        }

        //使用本地相机捕获人脸
        private async Task<(byte[]?, string?)> CaptureFaceWithLocalCamera()
        {
            string? tempImagePath = null;
            byte[]? faceData = null;

            try
            {
                LoadingText.Text = "正在启动本地相机...";

                // 使用LocalCameraManager捕获人脸图像
                using var localCameraManager = new LocalCameraManager();
                tempImagePath = await localCameraManager.CaptureFaceImageAsync();
                LoadingText.Text = "正在处理人脸图像...";
                faceData = File.ReadAllBytes(tempImagePath);
            }
            catch (Exception ex)
            {
                LoadingText.Text = "本地相机操作失败";
                MessageBox.Show($"本地相机操作失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return (faceData, tempImagePath);
        }

        //获取相机优先级设置
        private static string GetCameraPriority()
        {
            try
            {
                return ThemeSettings.LoadCameraPriority();
            }
            catch
            {
                return "Adb"; // 默认使用ADB相机
            }
        }

        //执行人脸登录
        private async Task<string?> PerformFaceLogin()
        {
            string cameraPriority = GetCameraPriority();
            string? username;

            // 根据优先级选择相机
            if (cameraPriority == "Adb" && _deviceManager.IsAdbAvailable())
            {
                // 使用ADB相机
                username = await PerformAdbFaceLogin();
            }
            else
            {
                // 使用本地相机
                username = await PerformLocalFaceLogin();
            }

            return username;
        }

        //使用ADB相机执行人脸登录
        private async Task<string?> PerformAdbFaceLogin()
        {
            // 准备摄像头
            await _deviceManager.PrepareCameraForCaptureAsync();
            LoadingText.Text = "请正对摄像头...";

            const int maxRetries = 3; // 最大重试次数
            int retryCount = 0;
            bool success = false;
            string? username = null;

            try
            {
                while (retryCount < maxRetries && !success)
                {
                    retryCount++;
                    LoadingText.Text = $"人脸识别中...({retryCount}/{maxRetries})";

                    try
                    {
                        // 调用人脸识别服务
                        using var userService = new UserService();
                        username = await userService.FaceLoginAsync(false);

                        success = true;
                        if (success)
                        {
                            LoadingText.Text = $"欢迎回来，{username}！";
                            return username; // 返回识别的用户名
                        }
                    }
                    catch (FaceRecognitionException)
                    {
                        if (retryCount == maxRetries)
                        {
                            throw;
                        }

                        LoadingText.Text = "请正对摄像头，保持面部清晰";
                        await Task.Delay(1000);
                    }
                }

                // 如果所有重试都失败但未抛出异常
                throw new FaceRecognitionException("人脸识别失败");
            }
            finally
            {
                await _deviceManager.CloseCameraAppAsync();
            }
        }

        //使用本地相机执行人脸登录
        private async Task<string?> PerformLocalFaceLogin()
        {
            LoadingText.Text = "请正对摄像头...";

            const int maxRetries = 3; // 最大重试次数
            int retryCount = 0;
            bool success = false;
            string? username = null;

            try
            {
                while (retryCount < maxRetries && !success)
                {
                    retryCount++;
                    LoadingText.Text = $"人脸识别中...({retryCount}/{maxRetries})";

                    try
                    {
                        // 调用人脸识别服务
                        using var userService = new UserService();
                        username = await userService.LocalFaceLoginAsync();

                        success = true;
                        if (success)
                        {
                            LoadingText.Text = $"欢迎回来，{username}！";
                            return username; // 返回识别的用户名
                        }
                    }
                    catch (FaceRecognitionException)
                    {
                        if (retryCount == maxRetries)
                        {
                            throw;
                        }

                        LoadingText.Text = "请正对摄像头，保持面部清晰";
                        await Task.Delay(1000);
                    }
                }

                // 如果所有重试都失败但未抛出异常
                throw new FaceRecognitionException("人脸识别失败");
            }
            catch (Exception ex)
            {
                LoadingText.Text = "本地相机人脸识别失败";
                MessageBox.Show($"本地相机人脸识别失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        // 执行密码重置
        private async Task PerformPasswordReset(string username, string newPassword)
        {
            string cameraPriority = GetCameraPriority();
            bool resetSuccess;

            // 根据优先级选择相机
            if (cameraPriority == "Adb" && _deviceManager.IsAdbAvailable())
            {
                // 使用ADB相机
                resetSuccess = await PerformAdbPasswordReset(username, newPassword);
            }
            else
            {
                // 使用本地相机
                resetSuccess = await PerformLocalPasswordReset(username, newPassword);
            }

            if (resetSuccess)
            {
                LoadingText.Text = "密码重置成功！";
                MessageBox.Show("密码已成功重置", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        //使用ADB相机执行密码重置
        private async Task<bool> PerformAdbPasswordReset(string username, string newPassword)
        {
            // 准备摄像头
            await _deviceManager.PrepareCameraForCaptureAsync();
            LoadingText.Text = "请正对摄像头...";

            // 设置拍照完成信号
            var captureCompletionSource = new TaskCompletionSource<bool>();

            // 键盘事件处理
            void KeyDownHandler(object s, KeyEventArgs args)
            {
                if (args.Key == Key.Space)
                {
                    args.Handled = true;
                    captureCompletionSource.TrySetResult(true);
                }
            }

            this.KeyDown += KeyDownHandler;
            LoadingText.Text = "按下空格键拍照";

            try
            {
                // 等待拍照或超时
                var timeoutTask = Task.Delay(30000);
                var completedTask = await Task.WhenAny(captureCompletionSource.Task, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    throw new TimeoutException("拍照超时");
                }

                // 执行拍照
                LoadingText.Text = "正在拍照...";
                string tempImagePath = await _deviceManager.CaptureFaceImageAsync();
                byte[] faceData = File.ReadAllBytes(tempImagePath);

                // 调用密码重置服务
                LoadingText.Text = "正在验证身份...";
                bool resetSuccess = await userService.ResetPasswordAsync(username, newPassword, faceData);

                if (!resetSuccess)
                {
                    throw new FaceRecognitionException("人脸验证未通过或系统错误");
                }

                // 清理临时文件
                if (File.Exists(tempImagePath))
                {
                    File.Delete(tempImagePath);
                }

                return true;
            }
            finally
            {
                this.KeyDown -= KeyDownHandler;
                await _deviceManager.CloseCameraAppAsync();
            }
        }

        //使用本地相机执行密码重置
        private async Task<bool> PerformLocalPasswordReset(string username, string newPassword)
        {
            try
            {
                LoadingText.Text = "请正对摄像头...";

                // 调用本地相机人脸验证
                LoadingText.Text = "正在验证身份...";
                using var localUserService = new UserService();
                bool verifySuccess = await localUserService.LocalVerifyFaceAsync(username);

                if (!verifySuccess)
                {
                    throw new FaceRecognitionException("人脸验证未通过");
                }

                // 验证通过后，更新密码
                bool resetSuccess = await userService.UpdatePasswordAsync(username, newPassword);

                if (!resetSuccess)
                {
                    throw new FaceRecognitionException("密码重置失败");
                }

                return true;
            }
            catch (Exception ex)
            {
                LoadingText.Text = "本地相机人脸识别失败";
                MessageBox.Show($"本地相机人脸识别失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        //设备错误处理中心
        private static void HandleDeviceError(DeviceErrorType errorType)
        {
            var message = DeviceErrorLocalizer.GetUserMessage(errorType);
            MessageBox.Show(message, "设备错误", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        [GeneratedRegex(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$")]
        private static partial Regex MyRegex();
    }
}