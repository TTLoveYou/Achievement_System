using System.IO;
using System.Windows;
using System.Windows.Controls;
using 成就系统.Services;
using 成就系统.UI.Login;

namespace 成就系统.UI.Main
{
    /// <summary>
    /// AccountSettingsPage.xaml 的交互逻辑
    /// </summary>
    public partial class AccountSettingsPage : Page
    {
        private string _userName;
        private readonly UserService _userService;
        private readonly AdbDeviceManager _deviceManager;

        public AccountSettingsPage(string userName)
        {
            InitializeComponent();
            _userName = userName;
            _userService = new UserService();
            _deviceManager = new AdbDeviceManager();
            Loaded += AccountSettingsPage_Loaded;
        }

        //获取相机优先级设置
        private string GetCameraPriority()
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

        private async void AccountSettingsPage_Loaded(object sender, RoutedEventArgs e)
        {
            await InitializePage();
        }

        //页面初始化
        private async Task InitializePage()
        {
            // 显示当前账户名
            CurrentUserName.Text = _userName;
            // 检查人脸数据状态
            await CheckFaceDataStatus();
        }

        //检查人脸数据
        private async Task CheckFaceDataStatus()
        {
            try
            {
                bool hasFaceData = await _userService.CheckFaceDataStatusAsync(_userName);
                if (hasFaceData)
                {
                    FaceDataStatus.Text = "人脸数据状态: 已录入";
                    FaceDataStatus.Foreground = System.Windows.Media.Brushes.Green;
                }
                else
                {
                    FaceDataStatus.Text = "人脸数据状态: 未录入";
                    FaceDataStatus.Foreground = System.Windows.Media.Brushes.Orange;
                }
            }
            catch (Exception ex)
            {
                ShowError("检查人脸数据状态失败: " + ex.Message);
            }
        }

        // 用户名输入验证（与登录界面相同的逻辑）
        private bool ValidateUsernameInput(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                ShowError("请输入新账户名");
                return false;
            }

            if (username.Length < 4)
            {
                ShowError("账户名至少需要4个字符");
                return false;
            }

            // 检查账户名是否只包含字母、数字和下划线
            foreach (char c in username)
            {
                if (!char.IsLetterOrDigit(c) && c != '_')
                {
                    ShowError("账户名只能包含字母、数字和下划线");
                    return false;
                }
            }

            return true;
        }

        //更新用户名
        private async void UpdateUserNameButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string newUserName = NewUserName.Text.Trim();
                if (!ValidateUsernameInput(newUserName)) return;
                if (newUserName == _userName)
                {
                    ShowError("新账户名与当前账户名相同");
                    return;
                }

                // 检查新账户名是否已存在（采用注册时的校验逻辑）
                bool accountExists = await _userService.AccountExistsAsync(newUserName);
                if (accountExists)
                {
                    ShowError("新账户名已存在，请选择其他账户名");
                    return;
                }

                // 更新账户名
                bool success = await _userService.UpdateUserNameAsync(_userName, newUserName);
                if (success)
                {
                    _userName = newUserName;
                    CurrentUserName.Text = _userName;
                    NewUserName.Text = string.Empty;
                    // 提示用户重新登录
                    MessageBoxResult result = MessageBox.Show(
                        "账户名更新成功，请重新登录！",
                        "更新成功",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    if (result == MessageBoxResult.OK)
                    {
                        // 执行退出登录操作
                        var loginWindow = new LoginWindow();
                        loginWindow.Show();
                        // 关闭当前窗口（主窗口）
                        Application.Current.MainWindow.Close();
                    }
                }
                else
                {
                    ShowError("账户名更新失败，请稍后重试");
                }
            }
            catch (Exception ex)
            {
                ShowError("账户名更新失败: " + ex.Message);
            }
        }

        // 密码输入验证（与登录界面相同的逻辑）
        private bool ValidatePasswordInput(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                ShowError("请输入新密码");
                return false;
            }

            // 密码强度验证（与登录界面相同的规则）
            var regex = MyRegex();
            if (!regex.IsMatch(password))
            {
                ShowError("密码必须包含大小写字母和数字，且长度至少8位");
                return false;
            }

            return true;
        }

        //更新密码
        private async void UpdatePasswordButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string currentPassword = CurrentPassword.Password;
                string newPassword = NewPassword.Password;
                string confirmNewPassword = ConfirmNewPassword.Password;

                if (string.IsNullOrEmpty(currentPassword))
                {
                    ShowError("请输入当前密码");
                    return;
                }
                if (!ValidatePasswordInput(newPassword)) return;
                if (newPassword != confirmNewPassword)
                {
                    ShowError("两次输入的新密码不一致");
                    return;
                }
                if (currentPassword == newPassword)
                {
                    ShowError("新密码与当前密码相同，请输入不同的密码");
                    return;
                }

                // 更新密码
                bool success = await _userService.UpdatePasswordAsync(_userName, currentPassword, newPassword);
                if (success)
                {
                    CurrentPassword.Password = string.Empty;
                    NewPassword.Password = string.Empty;
                    ConfirmNewPassword.Password = string.Empty;
                    // 提示用户重新登录
                    MessageBoxResult result = MessageBox.Show(
                        "密码更新成功，请重新登录！",
                        "更新成功",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    if (result == MessageBoxResult.OK)
                    {
                        // 执行退出登录操作
                        var loginWindow = new LoginWindow();
                        loginWindow.Show();
                        // 关闭当前窗口（主窗口）
                        Application.Current.MainWindow.Close();
                    }
                }
                else
                {
                    ShowError("密码更新失败，当前密码可能不正确");
                }
            }
            catch (Exception ex)
            {
                ShowError("密码更新失败: " + ex.Message);
            }
        }

        //更新人脸功能
        private async void UpdateFaceButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 检查当前是否已有人脸数据
                bool hasFaceData = await _userService.CheckFaceDataStatusAsync(_userName);
                if (hasFaceData)
                {
                    // 如果已有人脸数据，显示确认提示
                    MessageBoxResult result = MessageBox.Show(
                        "当前已有人脸数据，更新将会覆盖原有数据。确定要继续吗？",
                        "确认更新",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.No)
                    {
                        return;
                    }
                }

                string cameraPriority = GetCameraPriority();
                bool success = false;

                // 根据优先级选择相机
                if (cameraPriority == "Adb" && _deviceManager.IsAdbAvailable() && await _deviceManager.IsDeviceConnectedAsync())
                {
                    // 使用ADB相机
                    success = await UpdateFaceWithAdbCamera();
                }
                else
                {
                    // 使用本地相机
                    success = await UpdateFaceWithLocalCamera();
                }

                if (success)
                {
                    // 更新人脸数据状态
                    await CheckFaceDataStatus();
                    ShowSuccess("人脸数据更新成功");
                }
                else
                {
                    ShowError("人脸数据更新失败，请稍后重试");
                }
            }
            catch (Exception ex)
            {
                ShowError("人脸数据更新失败: " + ex.Message);
            }
        }

        //使用ADB相机更新人脸数据
        private async Task<bool> UpdateFaceWithAdbCamera()
        {
            try
            {
                // 录入新的人脸数据
                string imagePath = await _deviceManager.CaptureFaceImageAsync();
                byte[] faceData = File.ReadAllBytes(imagePath);

                // 保存人脸数据到数据库
                bool success = await _userService.EnrollFaceDataAsync(_userName, faceData);

                // 清理临时文件
                if (File.Exists(imagePath))
                {
                    File.Delete(imagePath);
                }

                return success;
            }
            catch (Exception ex)
            {
                ShowError("ADB相机操作失败: " + ex.Message);
                return false;
            }
        }

        //使用本地相机更新人脸数据
        private async Task<bool> UpdateFaceWithLocalCamera()
        {
            try
            {
                // 使用UserService的LocalEnrollFaceDataAsync方法
                bool success = await _userService.LocalEnrollFaceDataAsync(_userName);
                return success;
            }
            catch (Exception ex)
            {
                ShowError("本地相机操作失败: " + ex.Message);
                return false;
            }
        }

        //显示错误信息
        private void ShowError(string message)
        {
            ErrorMessage.Text = message;
            ErrorMessage.Foreground = System.Windows.Media.Brushes.Red;
            ErrorMessage.Visibility = Visibility.Visible;
        }

        //显示成功信息
        private void ShowSuccess(string message)
        {
            ErrorMessage.Text = message;
            ErrorMessage.Foreground = System.Windows.Media.Brushes.Green;
            ErrorMessage.Visibility = Visibility.Visible;
        }

        // 注销账户按钮点击事件
        private async void DeleteAccountButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 检查人脸数据状态
                bool hasFaceData = await _userService.CheckFaceDataStatusAsync(_userName);
                if (!hasFaceData)
                {
                    ShowError("请先录入人脸数据后再注销账户");
                    return;
                }

                // 显示确认提示
                MessageBoxResult confirmResult = MessageBox.Show(
                    "确定要注销当前账户吗？关联成就会永久删除！此操作不可恢复！",
                    "确认注销",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (confirmResult != MessageBoxResult.Yes)
                {
                    return;
                }

                string cameraPriority = GetCameraPriority();
                bool faceRecognized = false;

                // 根据优先级选择相机
                if (cameraPriority == "Adb" && _deviceManager.IsAdbAvailable() && await _deviceManager.IsDeviceConnectedAsync())
                {
                    // 使用ADB相机进行人脸识别
                    faceRecognized = await _deviceManager.VerifyFaceAsync(_userName);
                }
                else
                {
                    // 使用本地相机进行人脸识别
                    using var localUserService = new UserService();
                    faceRecognized = await localUserService.LocalVerifyFaceAsync(_userName);
                }

                if (!faceRecognized)
                {
                    ShowError("人脸识别失败，注销操作已取消");
                    return;
                }

                // 执行注销操作
                bool success = await _userService.DeleteAccountAsync(_userName);
                if (success)
                {
                    // 提示用户注销成功
                    MessageBoxResult result = MessageBox.Show(
                        "账户注销成功，系统将退出",
                        "注销成功",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    if (result == MessageBoxResult.OK)
                    {
                        // 执行退出登录操作
                        var loginWindow = new LoginWindow();
                        loginWindow.Show();
                        // 关闭当前窗口（主窗口）
                        Application.Current.MainWindow.Close();
                    }
                }
                else
                {
                    ShowError("账户注销失败，请稍后重试");
                }
            }
            catch (Exception ex)
            {
                ShowError("账户注销失败: " + ex.Message);
            }
        }

        // 返回按钮点击事件
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.GoBack();
        }

        [System.Text.RegularExpressions.GeneratedRegex(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$")]
        private static partial System.Text.RegularExpressions.Regex MyRegex();
    }
}