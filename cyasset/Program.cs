using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using SDL2;

namespace cyasset
{
    //This is from cylib -- we don't want hard dependencies, so either need a secondary lib both can depend on, or just manually deal with it
    //currently doing the 'manually deal with it'
    public enum AssetTypes
    {
        ERR = -1,
        SHADER = 0,
        TEXTURE = 1,
        VERTEX_BUFFER = 2,
        FONT = 3,
        BUFFER = 4,
        CUSTOM = 5,
    }

    public struct ContentHeaderInformation
    {
        public string Name;
        public AssetTypes Type;
        public long FileLength;
        public string Path;
    }

    class Program
    {
        public const bool SAVE_DEBUG_FONTS = false;

        static void Main(string[] args)
        {
            var tempDir = AppDomain.CurrentDomain.BaseDirectory + @"tempFiles\" + args[2].Substring(2) + "\\";
            var outDir = Environment.CurrentDirectory;

            SDL_image.IMG_Init(SDL_image.IMG_InitFlags.IMG_INIT_PNG | SDL_image.IMG_InitFlags.IMG_INIT_JPG);

            Console.WriteLine("args 0: " + args[0]);
            Console.WriteLine("args 1: " + args[1]);

            var inputFiles = args[0].Substring(8).Split(';');
            var outputFile = args[1].Substring(9);

            List<ContentHeaderInformation> contentFiles = new List<ContentHeaderInformation>();
            DateTime lastContentUpdate = DateTime.MinValue;
            foreach (var inFile in inputFiles)
            {
                if (inFile.EndsWith(".cyopt"))
                {
                    continue;
                }

                ReadOptions(inFile, out var opts, out var lastWrittenTo);

                if (inFile.EndsWith(".ttf"))
                {
                    //process font
                    FontProcessor.ProcessFont(inFile, opts, lastWrittenTo, tempDir, out var contentInf, out var lastDate);
                    contentFiles.AddRange(contentInf);

                    if (lastDate > lastContentUpdate)
                        lastContentUpdate = lastDate;
                }
            }

            //clean the build directory now -- anything in the directory that's not a direct output here can be delete'd
            {
                HashSet<string> usedPaths = new HashSet<string>();
                foreach (var f in contentFiles)
                {
                    usedPaths.Add(Path.GetFullPath(f.Path));
                }

                foreach (var f in Directory.EnumerateFiles(tempDir, "*", SearchOption.AllDirectories))
                {
                    if (!usedPaths.Contains(f))
                    {
                        File.Delete(f);
                    }
                }
            }

            //check if we need to update the output
            if (File.Exists(outputFile))
            {
                if (File.GetLastWriteTimeUtc(outputFile) >= lastContentUpdate)
                    return;
            }

            //start writing the output
            using Stream os = new FileStream(outputFile, FileMode.Create, FileAccess.ReadWrite);
            using BinaryWriter wr = new BinaryWriter(os, Encoding.Unicode);

            wr.Write(contentFiles.Count);

            long curPos = os.Position;
            wr.Write((long)0); //after the header is written, write the header len here

            long offset = 0;
            foreach (var of in contentFiles)
            {
                wr.Write(of.Name);
                wr.Write((int)of.Type);
                wr.Write(offset);
                wr.Write(of.FileLength);

                offset += of.FileLength;
            }

            long endOfHeaderPos = os.Position;
            os.Seek(curPos, SeekOrigin.Begin);
            wr.Write(endOfHeaderPos);
            os.Seek(endOfHeaderPos, SeekOrigin.Begin);

            foreach (var of in contentFiles)
            {
                os.Write(File.ReadAllBytes(of.Path));
            }
        }

        static void ReadOptions(string file, out Dictionary<string, string> opts, out DateTime lastWrittenTo)
        {
            var optFile = file + ".cyopt";
            opts = new Dictionary<string, string>();
            lastWrittenTo = DateTime.MinValue;

            if (!File.Exists(optFile))
                return;

            lastWrittenTo = File.GetLastWriteTimeUtc(optFile);

            using FileStream fs = new FileStream(optFile, FileMode.Open, FileAccess.Read);
            using StreamReader sr = new StreamReader(fs);

            string line;
            while ((line = sr.ReadLine()) != null)
            {
                int i = line.IndexOf('=');
                if (i < 0)
                    continue;

                opts.Add(line.Substring(0, i).ToUpper(), line.Substring(i + 1));
            }
        }
    }
}
