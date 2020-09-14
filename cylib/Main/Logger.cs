using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace log
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
        /// (For example, if the LogLevel is DEBUG, all VERBOSE logs will be silently ignored.
        /// </summary>
        public static LogType LogLevel = LogType.DEBUG;

        /// <summary>
        /// Write a line to the log.
        /// </summary>
        /// <param name="type">Logging level for this message.</param>
        /// <param name="s">Log Message</param>
        public static void WriteLine(LogType type, string s)
        {
            if (type > LogLevel)
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

            Console.ForegroundColor = fg;
            Console.BackgroundColor = bg;
            Console.WriteLine(s);
        }
    }
}
