using System.Security.Cryptography;

namespace 成就系统.Utilities
{
    // 基于PBKDF2算法的密码安全处理模块（NIST SP 800-132标准实现）
    public static class PasswordHasher
    {
        // 密码学参数配置（符合OWASP 2025安全标准）
        private const int SaltSize = 32;            // 盐值长度：256位（抵御彩虹表攻击）
        private const int Iterations = 12000;       // 迭代次数（平衡安全性与性能）12000
        private const int HashSize = 64;            // 哈希输出长度：512位（SHA512特性）

        // 密码哈希生成器（安全存储方案）
        public static (string Hash, string Salt) CreateHash(string password)
        {
            // 生成随机盐值（提前创建）
            byte[] saltBytes = new byte[SaltSize];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(saltBytes);
            }

            // 执行密钥派生
            using var deriveBytes = new Rfc2898DeriveBytes(
                password,
                saltBytes,          // 显式传入盐值
                Iterations,
                HashAlgorithmName.SHA512
            );

            byte[] hashBytes = deriveBytes.GetBytes(HashSize);

            return (
                Convert.ToBase64String(hashBytes),
                Convert.ToBase64String(saltBytes)
            );
        }

        // 验证密码
        public static bool VerifyHash(string password, string storedHash, string storedSalt)
        {
            byte[] saltBytes = Convert.FromBase64String(storedSalt);
            byte[] expectedHash = Convert.FromBase64String(storedHash);

            using var deriveBytes = new Rfc2898DeriveBytes(
                password,
                saltBytes,
                Iterations,
                HashAlgorithmName.SHA512
            );

            byte[] actualHash = deriveBytes.GetBytes(HashSize);
            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
    }
}