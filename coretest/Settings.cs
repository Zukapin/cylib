using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using cyUtility;

using System.IO;

namespace cylib
{
    /// <summary>
    /// Settings class, for user config options.
    /// 
    /// Should primarily be used for global config. Maybe add something for remembering game-specific prefs?
    /// </summary>
    public class Settings
    {
        public int resWidth = 1920;
        public int resHeight = 1080;
        public bool vSync = true;

        public Settings()
        {//this should read from a config file later to remember user settings.
            try
            {
                using (StreamReader fr = new StreamReader(@"Content\config.txt"))
                {
                    while (!fr.EndOfStream)
                    {
                        string line = fr.ReadLine();

                        if (line == null) //probably EndOfStream?
                            continue;
                        if (line == "") //empty line
                            continue;
                        if (line.StartsWith("#")) //commented line
                            continue;

                        string trimmed = line.Trim();

                        string[] parts = trimmed.Split(':');

                        if (parts.Length != 2)
                        {
                            Logger.WriteLine(LogType.POSSIBLE_ERROR, "Error parsing config file, weird number of parts for this line: " + line);
                            continue;
                        }

                        string key = parts[0].Trim().ToLower();
                        string value = parts[1].Trim();

                        try
                        {
                            //we should probably have a dictionary for key -> action here
                            //but that's kinda annoying to setup for a total of four options so we're just gonna make some if statements
                            if (key == "reswidth")
                            {
                                resWidth = Int32.Parse(value);
                            }
                            else if (key == "resheight")
                            {
                                resHeight = Int32.Parse(value);
                            }
                            else if (key == "vsync")
                            {
                                vSync = bool.Parse(value);
                            }
                            else if (key == "loglevel")
                            {
                                Logger.LogLevel = (LogType)Enum.Parse(typeof(LogType), value);
                            }
                            else
                            {
                                Logger.WriteLine(LogType.POSSIBLE_ERROR, "Unknown key in config file: " + key);
                            }
                        }
                        catch (Exception e)
                        {
                            Logger.WriteLine(LogType.POSSIBLE_ERROR, "Error parsing a key:value pair in config file -- " + line + "\n" + e.ToString());
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.WriteLine(LogType.POSSIBLE_ERROR, "Unhandled exception while reading config file. \n" + e.ToString());
            }
        }
    }
}
