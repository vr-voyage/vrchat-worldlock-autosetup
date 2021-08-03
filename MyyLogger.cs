#if UNITY_EDITOR

using UnityEngine;

public partial class SetupWindow
{
    public class MyyLogger
    {
        public static void Log(string message)
        {
            Debug.Log(message);
        }

        public static void Log(object o)
        {
            Debug.Log(o);
        }

        public static void Log(string format, params object[] o)
        {
            Debug.LogFormat(format, o);
        }

        public static void LogError(string message)
        {
            Debug.LogError(message);
        }

        public static void LogError(string message, params object[] o)
        {
            Debug.LogErrorFormat(message, o);
        }
    }
}

#endif