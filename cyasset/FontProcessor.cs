using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using SharpFont;

using SDL2;
using System.Runtime.InteropServices;

namespace cyasset
{
    /// <summary>
    /// Replacing the old Framework System.Drawing stuff here with a really simple Bitmap implementation.
    /// </summary>
    class Bitmap
    {
        public int Width { get; }
        public int Height { get; }

        public readonly byte[] Buffer;

        public Bitmap(int Width, int Height)
        {
            this.Width = Width;
            this.Height = Height;

            Buffer = new byte[Width * Height * 4];
            for (int i = 0; i < Buffer.Length; i++)
            {
                Buffer[i] = 0;
            }
        }

        public void Save(string Path)
        {
            var memHandle = GCHandle.Alloc(Buffer, GCHandleType.Pinned);
            var memPtr = memHandle.AddrOfPinnedObject();
            var surf = SDL.SDL_CreateRGBSurfaceWithFormatFrom(memPtr, Width, Height, 4, 4 * Width, SDL.SDL_PIXELFORMAT_BGRA8888);
            SDL_image.IMG_SavePNG(surf, Path);

            memHandle.Free();
        }
    }

    class FontProcessor
    {
        public class GlyphData
        {
            //char metrics
            public float bearingX;
            public float bearingY;
            public float advanceX;

            public float gWidth;
            public float gHeight;

            public Dictionary<char, float> kerningMap = new Dictionary<char, float>();

            //Atlas positioning
            public int aPosX;
            public int aPosY;

            public float aWidth;
            public float aHeight;

            public GlyphData()
            {

            }
        }

        public static void ProcessFont(string font, Dictionary<string, string> opts, DateTime optsTime, string tempDir, out ContentHeaderInformation[] outInf, out DateTime latestDate)
        {
            int numOutputs = 1;
            if (opts.TryGetValue("NUMOUT", out string numString))
            {
                if (!int.TryParse(numString, out numOutputs))
                    throw new Exception("NUMOUT Arg for font " + font + " is invalid: " + numString);

                if (numOutputs < 1)
                    throw new Exception("NUMOUT Arg for font " + font + " is invalid: " + numString);
            }

            string[] assetName = new string[numOutputs];
            assetName[0] = "FONT_" + Path.GetFileNameWithoutExtension(font);
            for (int i = 1; i < numOutputs; i++)
            {
                assetName[i] = assetName[0] + "_" + i;
            }
            if (numOutputs != 1)
                assetName[0] = assetName[0] + "_0";

            if (opts.TryGetValue("NAME", out string nameString))
            {
                if (numOutputs == 1)
                    assetName[0] = nameString;
                else
                {
                    var names = nameString.Split(";");
                    if (names.Length != numOutputs)
                        throw new Exception("NAME Arg for font " + font + " doesn't match the number of outputs: " + nameString);

                    for(int i = 0; i < numOutputs; i++)
                    {
                        assetName[i] = names[i];
                    }
                }
            }

            string supportedChars = "abcdefghijklmnopqrstuvwxyz ABCDEFGHIJKLMNOPQRSTUVWXYZ`1234567890-=[];',./\\~!@#$%^&*()_+{}:\"<>?|\u25A1";
            if (opts.TryGetValue("SUPPORTEDCHARS", out string optChars))
            {
                supportedChars = optChars;
            }

            bool[] isSDF = new bool[numOutputs];
            for (int i = 0; i < numOutputs; i++)
            {
                isSDF[i] = true;
            }
            if (opts.TryGetValue("ISSDF", out string sdfString))
            {
                var sdfSplit = sdfString.Split(';');
                if (sdfSplit.Length != numOutputs)
                    throw new Exception("ISSDF Arg for font " + font + " doesn't match the number of outputs: " + sdfSplit.Length + " " + numOutputs + " " + sdfString);

                for (int i = 0; i < numOutputs; i++)
                {
                    if (bool.TryParse(sdfSplit[i], out bool boolParse))
                    {
                        isSDF[i] = boolParse;
                    }
                    else
                        throw new Exception("ISSDF Arg for font " + font + " has an invalid bool: " + sdfString);
                }
            }

            int[] maxAtlasWidth = new int[numOutputs];
            for (int i = 0; i < numOutputs; i++)
            {
                maxAtlasWidth[i] = 1024;
            }
            if (opts.TryGetValue("ATLASWIDTH", out string atlasWidthString))
            {
                var atlasSplit = atlasWidthString.Split(';');
                if (atlasSplit.Length != numOutputs)
                    throw new Exception("ATLASWIDTH Arg for font " + font + " doesn't match the number of outputs: " + atlasWidthString);

                for (int i = 0; i < numOutputs; i++)
                {
                    if (int.TryParse(atlasSplit[i], out int intParse))
                    {
                        maxAtlasWidth[i] = intParse;
                    }
                    else
                        throw new Exception("ATLASWIDTH Arg for font " + font + " has an invalid int: " + atlasWidthString);
                }
            }

            int[] packingBuffer = new int[numOutputs];
            for (int i = 0; i < numOutputs; i++)
            {
                packingBuffer[i] = 4;
            }
            if (opts.TryGetValue("PACKINGBUFFER", out string packingString))
            {
                var packingSplit = packingString.Split(';');
                if (packingSplit.Length != numOutputs)
                    throw new Exception("PACKINGBUFFER Arg for font " + font + " doesn't match the number of outputs: " + packingString);

                for (int i = 0; i < numOutputs; i++)
                {
                    if (int.TryParse(packingSplit[i], out int intParse))
                    {
                        packingBuffer[i] = intParse;
                    }
                    else
                        throw new Exception("PACKINGBUFFER Arg for font " + font + " has an invalid int: " + packingString);
                }
            }

            int[] renderHeight = new int[numOutputs];
            for (int i = 0; i < numOutputs; i++)
            {
                renderHeight[i] = 256;
            }
            if (opts.TryGetValue("RENDERHEIGHT", out string renderHeightString))
            {
                var renderSplit = renderHeightString.Split(';');
                if (renderSplit.Length != numOutputs)
                    throw new Exception("RENDERHEIGHT Arg for font " + font + " doesn't match the number of outputs: " + renderHeightString);

                for (int i = 0; i < numOutputs; i++)
                {
                    if (int.TryParse(renderSplit[i], out int intParse))
                    {
                        renderHeight[i] = intParse;
                    }
                    else
                        throw new Exception("RENDERHEIGHT Arg for font " + font + " has an invalid int: " + renderHeightString);
                }
            }

            int[] atlasHeight = new int[numOutputs];
            for (int i = 0; i < numOutputs; i++)
            {
                atlasHeight[i] = 32;
            }
            if (opts.TryGetValue("ATLASHEIGHT", out string atlasHeightString))
            {
                var atlasSplit = atlasHeightString.Split(';');
                if (atlasSplit.Length != numOutputs)
                    throw new Exception("ATLASHEIGHT Arg for font " + font + " doesn't match the number of outputs: " + atlasHeightString);

                for (int i = 0; i < numOutputs; i++)
                {
                    if (int.TryParse(atlasSplit[i], out int intParse))
                    {
                        atlasHeight[i] = intParse;
                    }
                    else
                        throw new Exception("ATLASHEIGHT Arg for font " + font + " has an invalid int: " + atlasHeightString);
                }
            }

            float[] sdfRange = new float[numOutputs];
            for (int i = 0; i < numOutputs; i++)
            {
                sdfRange[i] = 64f;
            }
            if (opts.TryGetValue("SDFRANGE", out string sdfRangeString))
            {
                var sdfRangeSplit = sdfRangeString.Split(';');
                if (sdfRangeSplit.Length != numOutputs)
                    throw new Exception("SDFRANGE Arg for font " + font + " doesn't match the number of outputs: " + sdfRangeString);

                for (int i = 0; i < numOutputs; i++)
                {
                    if (float.TryParse(sdfRangeSplit[i], out float floatParse))
                    {
                        sdfRange[i] = floatParse;
                    }
                    else
                        throw new Exception("SDFRANGE Arg for font " + font + " has an invalid int: " + sdfRangeString);
                }
            }

            string[] outputPaths = new string[numOutputs];
            for (int i = 0; i < numOutputs; i++)
            {
                outputPaths[i] = tempDir + font + i;
            }

            var lastInputTime = File.GetLastWriteTimeUtc(font);
            if (optsTime > lastInputTime)
                lastInputTime = optsTime;

            latestDate = DateTime.MinValue;
            outInf = new ContentHeaderInformation[numOutputs];
            for (int i = 0; i < numOutputs; i++)
            {
                ContentHeaderInformation inf = new ContentHeaderInformation();
                inf.FileLength = 0;
                inf.Path = outputPaths[i];
                inf.Type = AssetTypes.FONT;
                inf.Name = assetName[i];

                bool genNew = true;

                var thisOutputTime = DateTime.MinValue;
                if (File.Exists(outputPaths[i]))
                {
                    thisOutputTime = File.GetLastWriteTimeUtc(outputPaths[i]);
                    if (thisOutputTime > lastInputTime)
                    {//this file has been updated more recently than the inputs, so we can ignore
                        genNew = false;
                    }
                }

                //okay, we need to write the new output
                if (genNew)
                {
                    Console.WriteLine("Processing font: " + font + " to output " + outputPaths[i] + " with asset name " + assetName[i]);
                    var proc = new FontProcessor(isSDF[i], supportedChars, maxAtlasWidth[i], packingBuffer[i], renderHeight[i], atlasHeight[i], sdfRange[i]);
                    proc.ProcessFont(font, outputPaths[i], Program.SAVE_DEBUG_FONTS);
                    thisOutputTime = File.GetLastWriteTimeUtc(outputPaths[i]);
                }

                if (thisOutputTime > latestDate)
                    latestDate = thisOutputTime;

                FileInfo fileDetails = new FileInfo(outputPaths[i]);
                inf.FileLength = fileDetails.Length;

                outInf[i] = inf;
            }
        }

        //string of supported characters
        //whitespace characters handled specially by the renderer
        //space is in sup char list to gen kerning/advanceX info
        //tab should just be expanded to howevermany spaces, newlines handled however
        readonly string supportedChars = "abcdefghijklmnopqrstuvwxyz ABCDEFGHIJKLMNOPQRSTUVWXYZ`1234567890-=[];',./\\~!@#$%^&*()_+{}:\"<>?|";
        readonly int maxAtlasWidth = 1024;
        readonly int packingBuffer = 4; //number of blank pixels added on all sides of a char in the atlas to ensure linear sampling doesn't pick up another char
        readonly int renderHeight = 4096; //how tall, in pixels, we should render individual characters for SDF-processing
        readonly int atlasHeight = 16; //how tall, in pixels, characters should be in the atlas
        readonly float sdfRange = 128f; //the 'range' to look for a border-pixel, in number of pixel
        readonly float heightRatio;
        float ascenderHeight;
        float descenderHeight;
        readonly Library lib;

        bool isSDF = false;

        Dictionary<char, GlyphData> glyphs = new Dictionary<char, GlyphData>();

        public FontProcessor(bool isSDF, string chars, int maxAtlasWidth, int packingBuffer, int renderHeight, int atlasHeight, float sdfRange)
        {
            this.isSDF = isSDF;
            this.supportedChars = chars;
            this.maxAtlasWidth = maxAtlasWidth;
            this.packingBuffer = packingBuffer;
            this.renderHeight = renderHeight;
            this.atlasHeight = atlasHeight;
            this.sdfRange = sdfRange;

            heightRatio = 1 / (float)renderHeight;

            lib = new Library();
        }

        public void ProcessFont(string path, string outPath, bool saveDebug)
        {
            Face face = new Face(lib, path);
            face.SetPixelSizes(0, (uint)renderHeight);

            BuildGlyphList(face, supportedChars);

            GetAtlasDim(face, supportedChars, out int atlasWidth, out int atlasHeight);

            Bitmap bOut = new Bitmap(atlasWidth, atlasHeight);

            RenderAtlas(face, supportedChars, bOut);

            SaveFont(bOut, outPath);

            if (saveDebug)
                bOut.Save(outPath + ".png");
        }

        /// <summary>
        /// Saves the processed font to a packed file.
        /// Format:
        ///     1. Font Data
        ///         int32 type: mostly for possible future use
        ///             0 = normal atlas
        ///             1 = sdf atlas
        ///         int32 AtlasHeight
        ///         float ascenderHeight
        ///         float descenderHeight
        ///         int32 width
        ///         int32 height
        ///         int32 numChars
        ///         GlyphData[] chars
        ///             char c
        ///             int32 atlasPosX
        ///             int32 atlasPosY
        ///             float aWidth
        ///             float aHeight
        ///             float bearingX
        ///             float bearingY
        ///             float advanceX
        ///             float gWidth
        ///             float gHeight
        ///             int32 numKerns
        ///             Kern[] kernChars
        ///                 char next
        ///                 float kern
        /// 
        ///         byte[] RGBA bitmap
        /// 
        /// </summary>
        private void SaveFont(Bitmap atlas, string outPath)
        {
            var dir = Path.GetDirectoryName(outPath);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            using (BinaryWriter wr = new BinaryWriter(new FileStream(outPath, FileMode.Create), System.Text.Encoding.Unicode))
            {
                int type = 0;
                if (isSDF)
                    type = 1;

                wr.Write(type);
                wr.Write(atlasHeight);
                wr.Write(ascenderHeight);
                wr.Write(descenderHeight);
                wr.Write(packingBuffer);
                wr.Write(atlas.Width);
                wr.Write(atlas.Height);
                wr.Write(glyphs.Count);

                foreach (KeyValuePair<char, GlyphData> vals in glyphs)
                {
                    GlyphData g = vals.Value;
                    wr.Write(vals.Key);
                    wr.Write(g.aPosX);
                    wr.Write(g.aPosY);
                    wr.Write(g.aWidth);
                    wr.Write(g.aHeight);
                    wr.Write(g.bearingX);
                    wr.Write(g.bearingY);
                    wr.Write(g.advanceX);
                    wr.Write(g.gWidth);
                    wr.Write(g.gHeight);
                    wr.Write(g.kerningMap.Count);

                    foreach (KeyValuePair<char, float> kerns in g.kerningMap)
                    {
                        wr.Write(kerns.Key);
                        wr.Write(kerns.Value);
                    }
                }

                for (int i = 0; i < atlas.Buffer.Length; i++)
                {
                    wr.Write(atlas.Buffer[i]);
                }
            }
        }

        /// <summary>
        /// Gets the texture atlas dimensions. Packs the characters as closely as possible to eachother, ignoring the baseline.
        /// This should mirror render atlas code pretty closely.
        /// </summary>
        private void GetAtlasDim(Face f, string chars, out int x, out int y)
        {
            int width = 0;
            int height = 0;
            int totalHeight = 0;
            bool maxWidth = false;

            for (int i = 0; i < chars.Length; i++)
            {
                int w = 0;
                int h = 0;

                getCharAtlasDim(f, chars[i], out w, out h);

                if (w == packingBuffer * 2 || h == packingBuffer * 2)
                    continue;

                if (width + w > maxAtlasWidth)
                {
                    maxWidth = true;
                    width = w;
                    totalHeight += height;
                    height = h;
                }
                else
                {
                    width += w;
                    height = Math.Max(height, h);
                }
            }

            x = maxWidth ? maxAtlasWidth : width;
            y = totalHeight + height;
        }

        /// <summary>
        /// Gets the char bitmap dimensions, rounded up to nearest pixel integer with added packing buffer.
        /// </summary>
        private void getCharAtlasDim(Face f, char a, out int x, out int y)
        {
            uint gid = f.GetCharIndex(a);
            f.LoadGlyph(gid, LoadFlags.Default, LoadTarget.Normal);

            x = (int)Math.Ceiling((float)f.Glyph.Metrics.Width * heightRatio * atlasHeight) + packingBuffer * 2;
            y = (int)Math.Ceiling((float)f.Glyph.Metrics.Height * heightRatio * atlasHeight) + packingBuffer * 2;
        }

        /// <summary>
        /// Renders the character atlas.
        /// Should mirror the atlas dim code pretty closely.
        /// </summary>
        private void RenderAtlas(Face f, string chars, Bitmap atlas)
        {
            int maxHeight = 0;
            int x = 0;
            int y = 0;

            for (int i = 0; i < chars.Length; i++)
            {
                getCharAtlasDim(f, chars[i], out int w, out int h);

                if (w == packingBuffer * 2 || h == packingBuffer * 2)
                {
                    var glyph = glyphs[chars[i]];
                    glyph.aPosX = x;
                    glyph.aPosY = y;
                    continue;
                }

                if (x + w > maxAtlasWidth)
                {
                    x = 0;
                    y += maxHeight;
                    maxHeight = h;
                }
                else
                {
                    maxHeight = Math.Max(maxHeight, h);
                }

                RenderChar(f, chars[i], atlas, x, y, w, h);
                x += w;
            }
        }

        private void RenderChar(Face f, char a, Bitmap atlas, int x, int y, int rW, int rH)
        {
            if (isSDF)
            {
                uint gid = f.GetCharIndex(a);
                f.LoadGlyph(gid, LoadFlags.NoHinting, LoadTarget.Normal);
                f.Glyph.RenderGlyph(RenderMode.Normal);

                FTBitmap ftbmp = f.Glyph.Bitmap;
                int width = ftbmp.Width;
                int height = ftbmp.Rows;

                byte[] sdf = new byte[rW * rH];
                byte[] src = ftbmp.BufferData;

                //calc the sdf
                int mul = renderHeight / atlasHeight;
                int mulh = mul / 2;
                int s = (int)Math.Ceiling(sdfRange);
                for (int h = 0; h < rH; h++)
                {
                    int hi = h * rW;
                    for (int w = 0; w < rW; w++)
                    {
                        int iSDF = hi + w;

                        //xy for center of our sample
                        int ch = (h - packingBuffer) * mul + mulh;
                        int cw = (w - packingBuffer) * mul + mulh;

                        //first, get the average alpha
                        //of all of the pixels our sdfpixel is covering in the bmp
                        int numPix = 0;
                        int sumAlpha = 0;

                        int minh = ch - mulh;
                        int maxh = ch + mulh;
                        int minw = cw - mulh;
                        int maxw = cw + mulh;
                        for (int hh = minh; hh < maxh; hh++)
                        {
                            int hi2 = hh * width;
                            for (int ww = minw; ww < maxw; ww++)
                            {
                                if (hh < 0 || hh >= height || ww < 0 || ww >= width)
                                {
                                }
                                else
                                {
                                    sumAlpha += src[hi2 + ww];
                                }
                                numPix++;
                            }
                        }

                        float avgAlpha = sumAlpha / (float)numPix;

                        //now find closest opposite-alpha pixel in bmp
                        if (avgAlpha < 128)
                        {
                            float cl = float.PositiveInfinity;
                            minh = Math.Max(0, ch - s);
                            maxh = Math.Min(height, ch + s);
                            minw = Math.Max(0, cw - s);
                            maxw = Math.Min(width, cw + s);
                            for (int hh = minh; hh < maxh; hh++)
                            {
                                int hi2 = hh * width;
                                for (int ww = minw; ww < maxw; ww++)
                                {
                                    if (src[hi2 + ww] >= 128)
                                    {
                                        float dx = ww - cw + 0.5f;
                                        float dy = hh - ch + 0.5f;
                                        float d = dx * dx + dy * dy;

                                        if (d < cl)
                                            cl = d;
                                    }
                                }
                            }

                            sdf[iSDF] = (byte)(128 - (byte)Math.Min(128, 128 * Math.Sqrt(cl) / sdfRange));
                        }
                        else if (avgAlpha > 128)
                        {
                            float cl = float.PositiveInfinity;
                            minh = ch - s;
                            maxh = ch + s;
                            minw = cw - s;
                            maxw = cw + s;
                            for (int hh = minh; hh < maxh; hh++)
                            {
                                int hi2 = hh * width;
                                for (int ww = minw; ww < maxw; ww++)
                                {
                                    if (hh < 0 || hh >= height || ww < 0 || ww >= width)
                                    {//if we're out of image bounds, that counts as 0 alpha
                                        float dx = ww - cw + 0.5f;
                                        float dy = hh - ch + 0.5f;
                                        float d = dx * dx + dy * dy;

                                        if (d < cl)
                                            cl = d;
                                    }
                                    else
                                    {
                                        if (src[hi2 + ww] <= 128)
                                        {
                                            float dx = ww - cw + 0.5f;
                                            float dy = hh - ch + 0.5f;
                                            float d = dx * dx + dy * dy;

                                            if (d < cl)
                                                cl = d;
                                        }
                                    }
                                }
                            }

                            sdf[iSDF] = (byte)(128 + (byte)Math.Min(127, 127 * Math.Sqrt(cl) / sdfRange));
                        }
                        else
                        {
                            sdf[iSDF] = 128;
                        }
                    }
                }

                for (int h = 0; h < rH; h++)
                {
                    int hi = h * rW;
                    int yi = (y + h) * atlas.Width + x;
                    for (int w = 0; w < rW; w++)
                    {
                        int xi = (yi + w) * 4;
                        atlas.Buffer[xi + 0] = 255;
                        atlas.Buffer[xi + 1] = 255;
                        atlas.Buffer[xi + 2] = 255;
                        atlas.Buffer[xi + 3] = sdf[hi + w];
                    }
                }

                var glyph = glyphs[a];
                glyph.aPosX = x + packingBuffer;
                glyph.aPosY = y + packingBuffer;
            }
            else //not SDF
            {
                f.SetPixelSizes(0, (uint)atlasHeight);
                uint gid = f.GetCharIndex(a);
                f.LoadGlyph(gid, LoadFlags.NoHinting, LoadTarget.Normal);
                f.Glyph.RenderGlyph(RenderMode.Normal);

                FTBitmap ftbmp = f.Glyph.Bitmap;
                byte[] src = ftbmp.BufferData;

                for (int h = 0; h < ftbmp.Rows; h++)
                {
                    int hi = h * ftbmp.Width;
                    int yi = (y + packingBuffer + h) * atlas.Width + x + packingBuffer; //drawing at (x + packingbuffer, y + packingbuffer)
                    for (int w = 0; w < ftbmp.Width; w++)
                    {
                        int xi = (yi + w) * 4;
                        atlas.Buffer[xi + 0] = src[hi + w + 0];
                        atlas.Buffer[xi + 1] = src[hi + w + 0];
                        atlas.Buffer[xi + 2] = src[hi + w + 0];
                        atlas.Buffer[xi + 3] = src[hi + w + 0];
                    }
                }

                var glyph = glyphs[a];
                glyph.aPosX = x + packingBuffer;
                glyph.aPosY = y + packingBuffer;
                f.SetPixelSizes(0, (uint)renderHeight);
            }
        }

        private void BuildGlyphList(Face f, string chars)
        {
            float r = heightRatio; // 1f / ((float)f.Size.Metrics.Ascender - (float)f.Size.Metrics.Descender);

            ascenderHeight = (float)f.Size.Metrics.Ascender * r;
            descenderHeight = (float)f.Size.Metrics.Descender * r;

            //Console.WriteLine("test: " + f.Size.Metrics.Ascender + " " + f.Size.Metrics.Descender + " " + f.Size.Metrics.Height + " " + f.Size.Metrics.ScaleX + " " + f.Size.Metrics.ScaleY + " " + f.Size.Metrics.NominalHeight);

            for (int i = 0; i < chars.Length; i++)
            {
                uint gid = f.GetCharIndex(chars[i]);
                f.LoadGlyph(gid, LoadFlags.NoHinting, LoadTarget.Normal);

                var g = new GlyphData();
                g.advanceX = (float)f.Glyph.Metrics.HorizontalAdvance * r;
                g.bearingX = (float)f.Glyph.Metrics.HorizontalBearingX * r;
                g.bearingY = (float)f.Glyph.Metrics.HorizontalBearingY * r;
                g.gWidth = (float)f.Glyph.Metrics.Width * r;
                g.gHeight = (float)f.Glyph.Metrics.Height * r;
                g.aWidth = (float)f.Glyph.Metrics.Width * heightRatio * atlasHeight;
                g.aHeight = (float)f.Glyph.Metrics.Height * heightRatio * atlasHeight;

                for (int t = 0; t < chars.Length; t++)
                {
                    uint id = f.GetCharIndex(chars[t]);
                    g.kerningMap.Add(chars[t], (float)f.GetKerning(gid, id, KerningMode.Unfitted).X * r);
                }

                glyphs.Add(chars[i], g);
            }
        }
    }
}
