namespace Linxens.Core.Logger
{
    public interface ILogger
    {
        void LogInfo(string action, string message);
        void LogWarning(string action, string message);
        void LogError(string action, string message);
    }
}