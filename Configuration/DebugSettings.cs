namespace SteamGiveawaysBot.Configuration
{
    public sealed class DebugSettings
    {
        public string CrashScreenshotFilePath { get; set; }
        
        public bool IsDebugMode { get; set; }

        public bool IsHeadless => !IsDebugMode;
        
        public bool IsCrashScreenshotEnabled => !string.IsNullOrWhiteSpace(CrashScreenshotFilePath);
    }
}
