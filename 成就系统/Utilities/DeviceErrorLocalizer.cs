using 成就系统.Services;

namespace 成就系统.Utilities
{
    // 错误消息中心
    public static class DeviceErrorLocalizer
    {
        private static readonly Dictionary<DeviceErrorType, (string TechMsg, string UserMsg, string Code)> _messages = new()
        {
            [DeviceErrorType.NotConnected] = ("设备未连接", "请连接您的Android设备", "E1001"),
            [DeviceErrorType.CameraLaunchFailed] = ("相机启动失败", "无法启动相机应用", "E1002"),
            [DeviceErrorType.ScreenshotFailed] = ("截图保存失败", "截图保存失败，请检查存储权限", "E1003"),
            [DeviceErrorType.CommandExecutionFailed] = ("命令执行失败", "设备通信异常", "E1004"),
            [DeviceErrorType.ScreenLocked] = ("无法唤醒屏幕", "设备屏幕未唤醒", "E1005"),
            [DeviceErrorType.FaceCaptureFailed] = ("人脸图像捕获失败", "无法获取人脸图像，请重试", "E1006")
        };

        public static string GetTechMessage(DeviceErrorType type)
            => _messages.TryGetValue(type, out var msg) ? msg.TechMsg : "未知设备错误";

        public static string GetUserMessage(DeviceErrorType type)
            => _messages.TryGetValue(type, out var msg) ? msg.UserMsg : "设备发生未知错误";

        public static string GetErrorCode(DeviceErrorType type)
            => _messages.TryGetValue(type, out var msg) ? msg.Code : "E9999";
    }
}
