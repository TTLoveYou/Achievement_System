namespace 成就系统.Services
{
    public class AchievementCard
    {
        public int AchievementId { get; set; }                      //成就ID
        public int? UserId { get; set; }                            //用户ID
        public int? CategoryId { get; set; }                        //类别ID
        public required string AchievementName { get; set; }        //成就名称
        public required string Description { get; set; }            //成就描述
        public DateTime AchievementDate { get; set; }               //成就日期                                                               
        public int? AnniversaryYears { get; set; }                   //成就周年
        public required string CompletionDegree { get; set; }       //成就完成度
        public decimal Score { get; set; }                          //成就评分
        public string? ImagePath { get; set; }                      //成就图片路径      
    }
}
