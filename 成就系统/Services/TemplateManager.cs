using System.IO;

namespace 成就系统.Services
{
    public class TemplateManager
    {
        private const string TemplateResourcePath = "成就系统.Resources.AchievementTemplate.xlsx";
        private const string TemplateSheetName = "Template";

        // 从嵌入式资源获取模板文件
        public static byte[] GetTemplateBytes()
        {
            using var stream = typeof(TemplateManager).Assembly
                .GetManifestResourceStream(TemplateResourcePath) ?? throw new FileNotFoundException("模板文件未找到");
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            return ms.ToArray();
        }
    }
}
