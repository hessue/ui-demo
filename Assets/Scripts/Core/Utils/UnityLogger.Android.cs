#if UNITY_ANDROID && !UNITY_EDITOR
using System;
using UnityEngine;

namespace BlockAndDagger
{
    /// <summary>
    /// Android-specific logging helper. This file is compiled only for Android builds.
    /// It provides a single TryLog method that returns true if it successfully logged to Logcat.
    /// </summary>
    internal static class UnityLoggerAndroid
    {
        public static bool TryLog(LogLevel level, string tag, string message, Exception exception)
        {
            try
            {
                using (var logClass = new AndroidJavaClass("android.util.Log"))
                {
                    switch (level)
                    {
                        case LogLevel.Debug:
                            logClass.CallStatic<int>("d", tag, message);
                            break;
                        case LogLevel.Info:
                            logClass.CallStatic<int>("i", tag, message);
                            break;
                        case LogLevel.Warn:
                            logClass.CallStatic<int>("w", tag, message);
                            break;
                        default:
                        case LogLevel.Error:
                            if (exception != null)
                                logClass.CallStatic<int>("e", tag, message + "\n" + exception.StackTrace);
                            else
                                logClass.CallStatic<int>("e", tag, message);
                            break;
                    }
                }

                return true;
            }
            catch (Exception)
            {
                // Let caller handle fallback
                return false;
            }
        }
    }
}
#endif

