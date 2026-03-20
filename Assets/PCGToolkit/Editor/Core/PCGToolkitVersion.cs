namespace PCGToolkit.Core
{
    /// <summary>
    /// PCG Toolkit 版本信息（集中管理）
    /// </summary>
    public static class PCGToolkitVersion
    {
        public const int Major = 0;
        public const int Minor = 5;
        public const int Patch = 0;
        public const string Label = "alpha"; // alpha / beta / rc / ""
        
        /// <summary>对应的 Git commit SHA（构建时更新）</summary>
        public const string CommitSHA = "05f8f419";
        
        /// <summary>构建日期</summary>
        public const string BuildDate = "2026-03-20";

        public static string Version => Label == "" 
            ? $"{Major}.{Minor}.{Patch}" 
            : $"{Major}.{Minor}.{Patch}-{Label}";

        public static string FullVersion => $"{Version} ({CommitSHA})";
    }
}