#if UNITY_EDITOR

using UnityEngine;

namespace Myy
{
    /**
     * <summary>Log wrapper. Currently just a proxy to Debug.Log* methods.</summary>
     */
    public class MyyLogger
    {
        /**
         * <summary>Log a debugging message.</summary>
         * 
         * <param name="message">Message to log</param>
         */
        public static void Log(string message)
        {
            Debug.Log(message);
        }

        /**
         * <summary>Log the content of an object,
         * using default toString() conversion</summary>
         * 
         * <param name="o">Object to log</param>
         */
        public static void Log(object o)
        {
            Debug.Log(o);
        }

        /**
         * <summary>Log a formatted message. Format follows C# conventions.</summary>
         * 
         * <remarks>You might want to use interpolated strings ($"") with
         * Log(string) instead.</remarks>
         * <param name="format">Formatted message</param>
         * <param name="o">Interpolated arguments</param>
         */
        public static void Log(string format, params object[] o)
        {
            Debug.LogFormat(format, o);
        }

        /**
         * <summary>Log an error message.</summary>
         * 
         * <param name="message">The error message to log</param>
         */
        public static void LogError(string message)
        {
            Debug.LogError(message);
        }

        /**
         * <summary>Log a warning message.</summary>
         * 
         * <param name="message">The warning message to log</param>
         */

        public static void LogWarning(string message)
        {
            Debug.LogWarning(message);
        }

        /**
         * <summary>Log a formatted error message.</summary>
         * 
         * <remarks>You might want to use interpolated strings ($"") with
         * LogError(string) instead.</remarks>
         * 
         * <param name="message">Formatted error message</param>
         * <param name="o">Interpolated arguments</param>
         */
        public static void LogError(string message, params object[] o)
        {
            Debug.LogErrorFormat(message, o);
        }
    }

}
#endif