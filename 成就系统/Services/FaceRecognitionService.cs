using FaceRecognitionDotNet;
using System.Diagnostics;
using System.IO;
using System;

namespace 成就系统.Services
{
    // 定义一个实现了IDisposable接口的FaceRecognitionService类，用于管理面部识别服务并确保资源正确释放
    public class FaceRecognitionService : IDisposable
    {
        private readonly FaceRecognition _faceRecognition;      // 私有只读字段，用于存储FaceRecognition实例
        private bool _disposed;                                 // 私有字段，用于标记对象是否已被释放

        public FaceRecognitionService()
        {
            // 获取应用程序基目录
            string appPath = AppDomain.CurrentDomain.BaseDirectory;

            // 构建模型文件完整模型路径
            string modelDir = Path.Combine(appPath, "models");

            // 验证目录存在
            if (!Directory.Exists(modelDir))
            {
                Directory.CreateDirectory(modelDir);
            }

            // 定义一个委托（函数），用于生成模型文件的完整路径
            string modelPath(string fileName)
            {
                string path = Path.Combine(modelDir, fileName);
                if (!File.Exists(path))
                {
                    throw new FileNotFoundException($"模型文件缺失: {fileName}\n路径: {path}");
                }
                return path;
            }

            // 创建模型参数对象，并加载模型文件
            var modelparams = new ModelParameter
            {
                CnnFaceDetectorModel = File.ReadAllBytes(modelPath("mmod_human_face_detector.dat")),                         // 加载人脸检测模型
                PosePredictor5FaceLandmarksModel = File.ReadAllBytes(modelPath("shape_predictor_5_face_landmarks.dat")),    // 加载5点人脸特征点检测模型
                FaceRecognitionModel = File.ReadAllBytes(modelPath("dlib_face_recognition_resnet_model_v1.dat")),           // 加载人脸识别模型
                PosePredictor68FaceLandmarksModel = File.ReadAllBytes(modelPath("shape_predictor_68_face_landmarks.dat"))   // 加载68点人脸特征点检测模型（可选）
            };

            // 使用加载的模型参数创建FaceRecognition实例
            _faceRecognition = FaceRecognition.Create(modelparams);
        }

        // 从图像中提取人脸特征向量
        public float[]? GetFaceEmbeddings(byte[] imageBytes)
        {
            // 检查图像数据是否为空
            if (imageBytes == null || imageBytes.Length == 0)
            {
                Debug.WriteLine("错误：图像数据为空");
                return null;
            }

            try
            {
                // 创建临时文件来保存图像
                string tempPath = Path.Combine(Path.GetTempPath(), $"temp_face_{Guid.NewGuid()}.png");
                File.WriteAllBytes(tempPath, imageBytes);

                try
                {
                    // 从临时文件加载图像
                    using var image = FaceRecognition.LoadImageFile(tempPath);

                    // 检测图像中的人脸位置并提取特征
                    var faceLocations = _faceRecognition.FaceLocations(image).ToArray();
                    if (faceLocations.Length == 0) 
                    {
                        Debug.WriteLine("未检测到人脸");
                        return null;
                    }                     // 如果没有检测到人脸，则返回null

                    // 取最大的人脸区域
                    var mainFace = faceLocations
                        .OrderByDescending(r => r.Right - r.Left)
                        .First();

                    // 提取人脸特征向量，并将double[]转换为float[]
                    double[] doubleEmbeddings = _faceRecognition.FaceEncodings(image, [mainFace]).First().GetRawEncoding();
                    float[] floatEmbeddings = Array.ConvertAll(doubleEmbeddings, x => (float)x);
                    
                    Debug.WriteLine($"成功提取人脸特征向量，长度: {floatEmbeddings.Length}");
                    return floatEmbeddings;
                }
                finally
                {
                    // 清理临时文件
                    if (File.Exists(tempPath))
                    {
                        File.Delete(tempPath);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"图像处理失败: {ex}");
                return null;
            }
        }

        // 比较两个人脸特征向量是否相似
        public static bool CompareFaces(float[] storedEmbedding, float[] currentEmbedding, double threshold = 0.6)
        {
            // 如果任一特征向量为空，则返回false
            if (storedEmbedding == null || currentEmbedding == null)
                return false;

            // 计算两个特征向量之间的欧氏距离
            // 使用 float 计算避免隐式转换问题
            float distanceSquared = 0f;
            for (int i = 0; i < storedEmbedding.Length; i++)
            {
                float diff = storedEmbedding[i] - currentEmbedding[i];
                distanceSquared += diff * diff;
            }

            // 计算实际距离并与阈值比较
            double distance = Math.Sqrt(distanceSquared);
            return distance < threshold;
        }

        // 新增裁剪方法（简化版，避免使用System.Drawing）
        //后期优化空间，扩展方法，新增 int width = 444, int height = 444 两个参数
        public static string CropAndResizeImage(string imagePath)
        {
            try
            {
                // 读取图像字节
                byte[] imageBytes = File.ReadAllBytes(imagePath);
                
                // 保存裁剪后的图片
                string directoryName = Path.GetDirectoryName(imagePath) ?? AppDomain.CurrentDomain.BaseDirectory;
                string fileName = Path.GetFileName(imagePath) ?? "cropped_image.png";
                string croppedPath = Path.Combine(
                    directoryName,
                    "cropped_" + fileName);
                
                // 直接保存原始图像（简化处理，避免使用System.Drawing）
                File.WriteAllBytes(croppedPath, imageBytes);
                return croppedPath;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"裁剪图像失败: {ex.Message}");
                return imagePath; // 失败时返回原始路径
            }
        }

        public void Dispose()
        {
            if (_disposed) return;              // 防止重复释放
            _faceRecognition?.Dispose();        // 安全调用
            _disposed = true;                   // 标记释放状态
        }
    }
}