using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cyasset
{
    class ImageHandler
    {
        public static void ProcessImage(string texture, Dictionary<string, string> opts, DateTime optsTime, string tempDir, out ContentHeaderInformation[] outInf, out DateTime latestDate)
        {
            //so we don't Super do anything here -- we don't make an intermediate file, just use the base image file
            //*may* do that in the future if we want resize or format options or something

            ContentHeaderInformation inf = new ContentHeaderInformation();
            inf.Path = texture;
            inf.Name = "TEX_" + Path.GetFileNameWithoutExtension(texture);
            inf.Type = AssetTypes.TEXTURE;

            var outWritten = File.GetLastWriteTimeUtc(texture);
            if (optsTime > outWritten)
                outWritten = optsTime;

            FileInfo fileDetails = new FileInfo(inf.Path);
            inf.FileLength = fileDetails.Length;
            latestDate = outWritten;

            outInf = new ContentHeaderInformation[1];
            outInf[0] = inf;
        }
    }
}
