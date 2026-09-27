namespace 成就系统.Services
{
    public class CategoryItem
    {
        public int Id { get; set; }
        public required string BigCategory { get; set; }    // 修改为匹配数据库字段
        public string? SmallCategory { get; set; }          // 修改为匹配数据库字段
        public int AchievementCount { get; set; }
        public List<CategoryItem> Subcategories { get; set; } = [];

        // 添加类型标识
        public CategoryType Type { get; set; }

        // 添加显示名称属性
        public string DisplayName => Type == CategoryType.BigCategory ?
            $"{BigCategory} ({AchievementCount})" :
            $"{SmallCategory} ({AchievementCount})";
    }

    public enum CategoryType
    {
        BigCategory,
        SmallCategory
    }
}
