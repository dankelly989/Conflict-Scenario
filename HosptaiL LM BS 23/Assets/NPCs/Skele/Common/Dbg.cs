#define ALLOW_LOG

using UnityEngine;
using System.Collections.Generic;
using System;

using Object = UnityEngine.Object;
using System.Text;
using System.Collections;

public class Dbg {

    #region Log functions

    #region "Normal Log"
    public static void Log<T>(T msg)
    {
#if ALLOW_LOG
        if (Debug.isDebugBuild)
        {
            Debug.Log(msg);
        }
#endif
    }

    public static void Log<T>(string fmt, T par1)
    {
#if ALLOW_LOG
        if (Debug.isDebugBuild)
        {
            string msg = string.Format(fmt, par1);
            Dbg.Log(msg);
        }
#endif
    }

    public static void Log<T1, T2>(string fmt, T1 par1, T2 par2)
    {
#if ALLOW_LOG
        if (Debug.isDebugBuild)
        {
            string msg = string.Format(fmt, par1, par2);
            Dbg.Log(msg);
        }
#endif
    }

    public static void Log<T1, T2, T3>(string fmt, T1 par1, T2 par2, T3 par3)
    {
#if ALLOW_LOG
        if (Debug.isDebugBuild)
        {
            string msg = string.Format(fmt, par1, par2, par3);
            Dbg.Log(msg);
        }
#endif
    }

    public static void Log<T1, T2, T3, T4>(string fmt, T1 par1, T2 par2, T3 par3, T4 par4)
    {
#if ALLOW_LOG
        if (Debug.isDebugBuild)
        {
            string msg = string.Format(fmt, par1, par2, par3, par4);
            Dbg.Log(msg);
        }
#endif
    }

    public static void Log<T1, T2, T3, T4, T5>(string fmt, T1 par1, T2 par2, T3 par3, T4 par4, T5 par5)
    {
#if ALLOW_LOG
        if (Debug.isDebugBuild)
        {
            string msg = string.Format(fmt, par1, par2, par3, par4, par5);
            Dbg.Log(msg);
        }
#endif
    }

    public static void Log<T1, T2, T3, T4, T5, T6>(string fmt, T1 par1, T2 par2, T3 par3, T4 par4, T5 par5, T6 par6)
    {
#if ALLOW_LOG
        if (Debug.isDebugBuild)
        {
            string msg = string.Format(fmt, par1, par2, par3, par4, par5, par6);
            Dbg.Log(msg);
        }
#endif
    }

    public static void Log<T1, T2, T3, T4, T5, T6, T7>(string fmt, T1 par1, T2 par2, T3 par3, T4 par4, T5 par5, T6 par6, T7 par7)
    {
#if ALLOW_LOG
        if (Debug.isDebugBuild)
        {
            string msg = string.Format(fmt, par1, par2, par3, par4, par5, par6, par7);
            Dbg.Log(msg);
        }
#endif
    }

    public static void Log<T1, T2, T3, T4, T5, T6, T7, T8>(string fmt, T1 par1, T2 par2, T3 par3, T4 par4, T5 par5, T6 par6, T7 par7, T8 par8)
    {
#if ALLOW_LOG
        if (Debug.isDebugBuild)
        {
            string msg = string.Format(fmt, par1, par2, par3, par4, par5, par6, par7, par8);
            Dbg.Log(msg);
        }
#endif
    }

    public static void Log<T1, T2, T3, T4, T5, T6, T7, T8, T9>(string fmt, T1 par1, T2 par2, T3 par3, T4 par4, T5 par5, T6 par6, T7 par7, T8 par8, T9 par9)
    {
#if ALLOW_LOG
        if (Debug.isDebugBuild)
        {
            string msg = string.Format(fmt, par1, par2, par3, par4, par5, par6, par7, par8, par9);
            Dbg.Log(msg);
        }
#endif
    }

    public static void CLog<T1>(Object context, T1 msg)
    {
#if ALLOW_LOG
        if (Debug.isDebugBuild)
        {
            Debug.Log(msg, context);
        }
#endif
    }

    public static void CLog<T>(Object context, string fmt, T par1)
    {
#if ALLOW_LOG
        if (Debug.isDebugBuild)
        {
            string msg = string.Format(fmt, par1);
            Dbg.CLog(context, msg);
        }
#endif
    }

    public static void CLog<T1, T2>(Object context, string fmt, T1 par1, T2 par2)
    {
#if ALLOW_LOG
        if (Debug.isDebugBuild)
        {
            string msg = string.Format(fmt, par1, par2);
            Dbg.CLog(context, msg);
        }
#endif
    }

    public static void CLog<T1, T2, T3>(Object context, string fmt, T1 par1, T2 par2, T3 par3)
    {
#if ALLOW_LOG
        if (Debug.isDebugBuild)
        {
            string msg = string.Format(fmt, par1, par2, par3);
            Dbg.CLog(context, msg);
        }
#endif
    }

    public static void CLog<T1, T2, T3, T4>(Object context, string fmt, T1 par1, T2 par2, T3 par3, T4 par4)
    {
#if ALLOW_LOG
        if (Debug.isDebugBuild)
        {
            string msg = string.Format(fmt, par1, par2, par3, par4);
            Dbg.CLog(context, msg);
        }
#endif
    }

    public static void CLog<T1, T2, T3, T4, T5>(Object context, string fmt, T1 par1, T2 par2, T3 par3, T4 par4, T5 par5)
    {
#if ALLOW_LOG
        if (Debug.isDebugBuild)
        {
            string msg = string.Format(fmt, par1, par2, par3, par4, par5);
            Dbg.CLog(context, msg);
        }
#endif
    }

    public static void CLog<T1, T2, T3, T4, T5, T6>(Object context, string fmt, T1 par1, T2 par2, T3 par3, T4 par4, T5 par5, T6 par6)
    {
#if ALLOW_LOG
        if (Debug.isDebugBuild)
        {
            string msg = string.Format(fmt, par1, par2, par3, par4, par5, par6);
            Dbg.CLog(context, msg);
        }
#endif
    }

	// "Normal Log" 
	
#endregion "Normal Log"

#region "warn log"

    public static void LogWarn<T>(T msg)
    {
        Debug.LogWarning(msg);
    }



    public static void LogWarn<T1>(string fmt, T1 par1)
    {
        string msg = string.Format(fmt, par1);
        Dbg.LogWarn(msg);
    }

    public static void LogWarn<T1, T2>(string fmt, T1 par1, T2 par2)
    {
        string msg = string.Format(fmt, par1, par2);
        Dbg.LogWarn(msg);
    }

    public static void LogWarn<T1, T2, T3>(string fmt, T1 par1, T2 par2, T3 par3)
    {
        string msg = string.Format(fmt, par1, par2, par3);
        Dbg.LogWarn(msg);
    }

    public static void LogWarn<T1, T2, T3, T4>(string fmt, T1 par1, T2 par2, T3 par3, T4 par4)
    {
        string msg = string.Format(fmt, par1, par2, par3, par4);
        Dbg.LogWarn(msg);
    }

    public static void CLogWarn<T>(Object context, T msg)
    {
        Debug.LogWarning(msg, context);
    }
    
    public static void CLogWarn<T1>(Object ctx, string fmt, T1 par1)
    {
        string msg = string.Format(fmt, par1);
        Dbg.CLogWarn(ctx, msg);
    }

    public static void CLogWarn<T1, T2>(Object ctx, string fmt, T1 par1, T2 par2)
    {
        string msg = string.Format(fmt, par1, par2);
        Dbg.CLogWarn(ctx, msg);
    }

    public static void CLogWarn<T1, T2, T3>(Object ctx, string fmt, T1 par1, T2 par2, T3 par3)
    {
        string msg = string.Format(fmt, par1, par2, par3);
        Dbg.CLogWarn(ctx, msg);
    }

    public static void CLogWarn<T1, T2, T3, T4>(Object ctx, string fmt, T1 par1, T2 par2, T3 par3, T4 par4)
    {
        string msg = string.Format(fmt, par1, par2, par3, par4);
        Dbg.CLogWarn(ctx, msg);
    }
    
	// "warn log" 	
#endregion "warn log"

#region "Error log"
    public static void LogErr<T>(T msg)
    {
        Debug.LogError(msg);
#if !UNITY_EDITOR
        //Application.Quit();
#else
        if(_pauseOnError)
            Debug.Break();
#endif
    }

    public static void LogErrContext<T>(Object context, T msg) 
    {
        Debug.LogError(msg, context);
#if !UNITY_EDITOR
        //Application.Quit();
#else
        if(_pauseOnError)
            Debug.Break();
#endif
    }

    public static void LogErr<T1>(string fmt, T1 par1)
    {
        string msg = string.Format(fmt, par1);
        Dbg.LogErr(msg);
    }

    public static void LogErr<T1, T2>(string fmt, T1 par1, T2 par2)
    {
        string msg = string.Format(fmt, par1, par2);
        Dbg.LogErr(msg);
    }

    public static void LogErr<T1, T2, T3>(string fmt, T1 par1, T2 par2, T3 par3)
    {
        string msg = string.Format(fmt, par1, par2, par3);
        Dbg.LogErr(msg);
    }

    public static void LogErr<T1, T2, T3, T4>(string fmt, T1 par1, T2 par2, T3 par3, T4 par4)
    {
        string msg = string.Format(fmt, par1, par2, par3, par4);
        Dbg.LogErr(msg);
    }

    public static void LogErr<T1, T2, T3, T4, T5>(string fmt, T1 par1, T2 par2, T3 par3, T4 par4, T5 par5)
    {
        string msg = string.Format(fmt, par1, par2, par3, par4, par5);
        Dbg.LogErr(msg);
    }

    public static void CLogErr(Object ctx, string msg)
    {
        Dbg.LogErrContext(ctx, msg);
    }

    public static void CLogErr<T1>(Object ctx, string fmt, T1 par1)
    {
        string msg = string.Format(fmt, par1);
        Dbg.LogErrContext(ctx, msg);
    }

    public static void CLogErr<T1, T2>(Object ctx, string fmt, T1 par1, T2 par2)
    {
        string msg = string.Format(fmt, par1, par2);
        Dbg.LogErrContext(ctx, msg);
    }

    public static void CLogErr<T1, T2, T3>(Object ctx, string fmt, T1 par1, T2 par2, T3 par3)
    {
        string msg = string.Format(fmt, par1, par2, par3);
        Dbg.LogErrContext(ctx, msg);
    }

    public static void CLogErr<T1, T2, T3, T4>(Object ctx, string fmt, T1 par1, T2 par2, T3 par3, T4 par4)
    {
        string msg = string.Format(fmt, par1, par2, par3, par4);
        Dbg.LogErrContext(ctx, msg);
    }

    public static void CLogErr<T1, T2, T3, T4, T5>(Object ctx, string fmt, T1 par1, T2 par2, T3 par3, T4 par4, T5 par5)
    {
        string msg = string.Format(fmt, par1, par2, par3, par4, par5);
        Dbg.LogErrContext(ctx, msg);
    }

	// "Error log" 
	
#endregion "Error log"
#endregion log functinos

#region Stack functions
    public string GetStack()
    {
#if UNITY_WINRT && !UNITY_EDITOR
        return "no stack for winrt";
#else
        return System.Environment.StackTrace;
#endif
    }

    public void LogStack()
    {
        if(Debug.isDebugBuild)
        {
            Debug.Log(GetStack());
        }
    }

    // log the stack if condition is false
    public void LogStack(bool bCond)
    {
        if (! bCond) 
        {
            if (Debug.isDebugBuild)
            {
                Debug.Log(GetStack());
            }
        }
    }

#endregion

#region Assert functions

    public static void Assert<T1>(bool cond, T1 msg)
    {
        if (cond)
            return;

        Dbg.LogErr(msg);
    }

    public static void Assert<T1>( bool cond, string fmt, T1 par1)
    {
        if (cond)
            return;        

        Dbg.LogErr(fmt, par1);
    }

    public static void Assert<T1, T2>(bool cond, string fmt, T1 par1, T2 par2)
    {
        if (cond)
            return;

        Dbg.LogErr(fmt, par1, par2);
    }

    public static void Assert<T1, T2, T3>( bool cond, string fmt, T1 par1, T2 par2, T3 par3)
    {
        if (cond)
            return;

        Dbg.LogErr(fmt, par1, par2, par3);
    }

    public static void Assert<T1, T2, T3, T4>(bool cond, string fmt, T1 par1, T2 par2, T3 par3, T4 par4)
    {
        if (cond)
            return;

        Dbg.LogErr(fmt, par1, par2, par3, par4);
    }

    public static void Assert<T1, T2, T3, T4, T5>(bool cond, string fmt, T1 par1, T2 par2, T3 par3, T4 par4, T5 par5)
    {
        if (cond)
            return;

        Dbg.LogErr(fmt, par1, par2, par3, par4, par5);
    }


    public static void CAssert<T1>(Object ctx, bool cond, T1 msg)
    {
        if (cond)
            return;

        Dbg.LogErrContext(ctx, msg);
    }

    public static void CAssert<T1>(Object ctx, bool cond, string fmt, T1 par1)
    {
        if (cond)
            return;

        Dbg.CLogErr(ctx, fmt, par1);
    }

    public static void CAssert<T1, T2>(Object ctx, bool cond, string fmt, T1 par1, T2 par2)
    {
        if (cond)
            return;

        Dbg.CLogErr(ctx, fmt, par1, par2);
    }

    public static void CAssert<T1, T2, T3>(Object ctx, bool cond, string fmt, T1 par1, T2 par2, T3 par3)
    {
        if (cond)
            return;

        Dbg.CLogErr(ctx, fmt, par1, par2, par3);
    }

    public static void CAssert<T1, T2, T3, T4>(Object ctx, bool cond, string fmt, T1 par1, T2 par2, T3 par3, T4 par4)
    {
        if (cond)
            return;

        Dbg.CLogErr(ctx, fmt, par1, par2, par3, par4);
    }

    public static void CAssert<T1, T2, T3, T4, T5>(Object ctx, bool cond, string fmt, T1 par1, T2 par2, T3 par3, T4 par4, T5 par5)
    {
        if (cond)
            return;

        Dbg.CLogErr(ctx, fmt, par1, par2, par3, par4, par5);
    }

    #endregion

    #region "constants"
    [Flags]
    public enum LogSetting
    {
        PrefixFrame = 1,
        PrefixTime = 2,
    }
    #endregion

    #region "settings"

    private static bool _pauseOnError = false;
    public static bool pauseOnError
    {
        get { return _pauseOnError; }
        set { _pauseOnError = value; }
    }

    #endregion "settings"

    #region "extra logHandler"

    public static LogSetting logSetting {
        get { return _logHandler.logSetting; }
        set {
            if( _logHandler.logSetting != value )
            {
                _logHandler.logSetting = value;
            }
        }
    }

    public class DbgLogHandler : ILogHandler
    {
        private ILogHandler _defaultHandler = Debug.logger.logHandler;
        private LogSetting _setting = 0;
        private StringBuilder _bld = new StringBuilder();

        public LogSetting logSetting {
            get { return _setting; }
            set {
                if(_setting != value)
                {
                    _setting = value;
                    if( _setting == 0 )
                    {
                        Debug.logger.logHandler = _defaultHandler;
                    }
                    else
                    {
                        Debug.logger.logHandler = this;
                    }
                }
            }
        }

        public void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args)
        {
            if( (_setting & LogSetting.PrefixFrame) != 0 )
            {
                _bld.Append(Time.frameCount);
                _bld.Append(' ');
            }
            if( ( _setting & LogSetting.PrefixTime) != 0 )
            {
                _bld.AppendFormat("[{0}]", Time.time.ToString("F3"));
                _bld.Append(' ');
            }
            _bld.Append(": ");
            _bld.Append(format);

            _defaultHandler.LogFormat(logType, context, _bld.ToString(), args);
            _bld.Remove(0, _bld.Length);
        }

        public void LogException(Exception exception, UnityEngine.Object context)
        {
            _defaultHandler.LogException(exception, context);
        }
    }

    private static DbgLogHandler _logHandler = new DbgLogHandler();

#endregion

}
