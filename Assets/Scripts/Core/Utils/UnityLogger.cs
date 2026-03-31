using System;
using UnityEngine;

namespace BlockAndDagger
{
    // Small internal LogLevel enum to avoid depending on external logging packages
    public enum LogLevel
    {
        Debug,
        Info,
        Warn,
        Error
    }

    public interface ILogger
    {
        void Log<TState>(LogLevel logLevel, TState state, Exception exception,
            Func<TState, Exception, string> formatter);

        void Log(string message);
    }

    public class UnityLogger : ILogger
    {
        public static readonly UnityLogger Default = new();

        private const string Tag = "BlockAndDagger";

        public void Log<TState>(LogLevel logLevel, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            // Build the message using the provided formatter if present, otherwise fall back to ToString
            string message;
            try
            {
                if (formatter != null)
                    message = formatter(state, exception);
                else
                    message = state != null ? state.ToString() : string.Empty;
            }
            catch (Exception fmtEx)
            {
                // If formatter throws, make sure we still log something useful
                message = $"<Formatter threw {fmtEx.GetType().Name}: {fmtEx.Message}>";
            }

            if (exception != null && !message.Contains(exception.Message))
            {
                message = message + "\n" + exception.Message;
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            // Delegate Android Logcat work to platform-specific helper (keeps this file clean)
            try
            {
                if (UnityLoggerAndroid.TryLog(logLevel, Tag, message, exception))
                    return;
            }
            catch (Exception e)
            {
                // Fall back to Unity logging if Android logging helper fails
                Debug.LogWarning($"Android Logcat unavailable ({Tag}): {e.Message}");
            }
#endif

            // Default fallback: Unity's logger (works in Editor, standalone, and in case Android logging fails)
            switch (logLevel)
            {
                case LogLevel.Debug:
                case LogLevel.Info:
                    Debug.Log(message);
                    break;
                case LogLevel.Warn:
                    Debug.LogWarning(message);
                    break;
                default:
                case LogLevel.Error:
                    if (exception != null)
                    {
                        Debug.LogException(exception);
                        Debug.LogError(message + "\n" + exception.StackTrace);
                    }
                    else
                    {
                        Debug.LogError(message);
                    }
                    break;
            }
        }

        public void Log(string message)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                if (UnityLoggerAndroid.TryLog(LogLevel.Info, Tag, message, null))
                    return;
            }
            catch
            {
                // ignore and fallback
            }
#endif
            Debug.Log(message);
        }
    }

    // Convenience extension methods for ILogger
    public static class LoggerExtensions
    {
        public static void Debug(this ILogger logger, string message)
            => logger?.Log(LogLevel.Debug, message, null, (s, _) => s ?? string.Empty);

        public static void Info(this ILogger logger, string message)
            => logger?.Log(LogLevel.Info, message, null, (s, _) => s ?? string.Empty);

        public static void Warn(this ILogger logger, string message)
            => logger?.Log(LogLevel.Warn, message, null, (s, _) => s ?? string.Empty);

        public static void Error(this ILogger logger, string message, Exception exception = null)
            => logger?.Log(LogLevel.Error, message, exception, (s, _) => s ?? string.Empty);
    }
}
