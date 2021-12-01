using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace cyUtility
{
    /// <summary>
    /// Logging level
    /// </summary>
    public enum LogType
    {
        /// <summary>
        /// An unexpected or unhandled error.
        /// </summary>
        ERROR,

        /// <summary>
        /// An edge case error that may or may not be handled
        /// </summary>
        POSSIBLE_ERROR,

        /// <summary>
        /// Usually important state information that should be logged even outside of DEBUG mode.
        /// </summary>
        INFO,

        /// <summary>
        /// Debug information
        /// </summary>
        DEBUG,

        /// <summary>
        /// Verbose debug information
        /// </summary>
        VERBOSE,

        /// <summary>
        /// Very verbose debug information
        /// </summary>
        VERBOSE2,

        /// <summary>
        /// Extremely verbose debug information
        /// </summary>
        VERBOSE3,
    }

    /// <summary>
    /// Class for holding convenience method for logging. 
    /// </summary>
    public abstract class Logger
    {
        /// <summary>
        /// This is the static logging level. Any log at this level or more important will be logged.
        /// (For example, if the LogLevel is DEBUG, all VERBOSE logs will be silently ignored.)
        /// Use FileLevel for file output filtering.
        /// </summary>
        public static LogType LogLevel = LogType.DEBUG;

        /// <summary>
        /// This is the static logging level for file writing. Any log at this level or more important will be logged.
        /// (For example, if the LogLevel is DEBUG, all VERBOSE logs will be silently ignored.)
        /// Use LogLevel for console output filtering.
        /// </summary>
        public static LogType FileLevel = LogType.INFO;

        /// <summary>
        /// Where to write the log to a file, if we can.
        /// </summary>
        private static StreamWriter LogOutput;

        /// <summary>
        /// Sets the logging path. If this isn't set, logging will only go to the console.
        /// You can set the FileLevel property to indicate what should be logged to a file.
        /// </summary>
        /// <param name="FilePath"></param>
        public static void SetFileOutput(string FilePath)
        {
            if (LogOutput != null)
            {
                LogOutput.Flush();
                LogOutput.Close();
                LogOutput.Dispose();
            }

            LogOutput = new StreamWriter(FilePath, true);
        }

        /// <summary>
        /// Write a line to the log.
        /// </summary>
        /// <param name="type">Logging level for this message.</param>
        /// <param name="s">Log Message</param>
        public static void WriteLine(LogType type, string s)
        {
            if (type > LogLevel && type > FileLevel)
                return;

            ConsoleColor fg = ConsoleColor.White;
            ConsoleColor bg = ConsoleColor.Black;

            switch (type)
            {
                case LogType.DEBUG:
                    fg = ConsoleColor.White;
                    bg = ConsoleColor.Black;
                    break;
                case LogType.ERROR:
                    fg = ConsoleColor.White;
                    bg = ConsoleColor.Red;
                    break;
                case LogType.POSSIBLE_ERROR:
                    fg = ConsoleColor.Red;
                    bg = ConsoleColor.Black;
                    break;
            }

            if (type <= LogLevel)
            {
                Console.ForegroundColor = fg;
                Console.BackgroundColor = bg;
                Console.WriteLine(s);
            }

            if (LogOutput != null && type <= FileLevel)
            {
                LogOutput.WriteLine(s);
                LogOutput.Flush(); //may not want this if we have a very active log
            }
        }
    }
}
