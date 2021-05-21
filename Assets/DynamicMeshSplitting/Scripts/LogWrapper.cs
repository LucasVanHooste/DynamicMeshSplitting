using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JL.Splitting
{
    public static class LogWrapper
    {
        #region Error
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void LogErrorFormat(UnityEngine.Object context, string template, params object[] args)
        {
            var message = string.Format(template, args);
            LogError(context, message);
        }
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void LogErrorFormat(string template, params object[] args)
        {
            var message = string.Format(template, args);
            LogError(message);
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void LogError(object message)
        {
            Debug.LogError(message);
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void LogError(UnityEngine.Object context, object message)
        {
            Debug.LogError(message, context);
        }
        #endregion

        #region Warning
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void LogWarningFormat(UnityEngine.Object context, string template, params object[] args)
        {
            var message = string.Format(template, args);
            Warning(context, message);
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void LogWarningFormat(string template, params object[] args)
        {
            var message = string.Format(template, args);
            LogWarning(message);
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void LogWarning(object message)
        {
            Debug.LogWarning(message);
        }

        public static void Warning(UnityEngine.Object context, object message)
        {
            Debug.LogWarning(message, context);
        }
        #endregion

        #region Message
        public static void LogFormat(UnityEngine.Object context, string template, params object[] args)
        {
            var message = string.Format(template, args);
            Log(context, message);
        }

        public static void LogFormat(string template, params object[] args)
        {
            var message = string.Format(template, args);
            Log(message);
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void Log(object message)
        {
            Debug.Log(message);
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void Log(UnityEngine.Object context, object message)
        {
            Debug.Log(message, context);
        }
        #endregion

        #region Verbose
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void VerboseFormat(UnityEngine.Object context, string template, params object[] args)
        {
            var message = string.Format(template, args);
            Verbose(context, message);
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void VerboseFormat(string template, params object[] args)
        {
            var message = string.Format(template, args);
            Verbose(message);
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void Verbose(object message)
        {
            Debug.Log(string.Concat("<color=grey>[VERBOSE]</color> ", message));
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void Verbose(UnityEngine.Object context, object message)
        {
            Debug.Log(string.Concat("<color=grey>[VERBOSE]</color> ", message), context);
        }
        #endregion
    }
}
