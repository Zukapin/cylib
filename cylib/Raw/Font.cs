using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Drawing;

namespace cylib
{
    public class Font : IDisposable
    {
        public class GlyphData
        {
            //char metrics
            public readonly float bearingX;
            public readonly float bearingY;
            public readonly float advanceX;

            public readonly float gWidth;
            public readonly float gHeight;

            public readonly Dictionary<char, float> kerningMap;

            //Atlas positioning in [0, 1] tex coords
            public readonly float aPosX;
            public readonly float aPosY;

            public readonly float aWidth;
            public readonly float aHeight;

            public GlyphData(float aPosX, float aPosY, float aWidth, float aHeight, float bearingX, float bearingY, float advanceX, float gWidth, float gHeight, Dictionary<char, float> kerningMap)
            {
                this.aPosX = aPosX;
                this.aPosY = aPosY;
                this.aWidth = aWidth;
                this.aHeight = aHeight;

                this.bearingX = bearingX;
                this.bearingY = bearingY;
                this.advanceX = advanceX;

                this.gWidth = gWidth;
                this.gHeight = gHeight;

                this.kerningMap = kerningMap;
            }
        }

        public readonly bool isSDF;
        public readonly float atlasHeight;
        public readonly float ascenderHeight;
        public readonly int packingBuffer;

        /// <summary>
        /// Generally negative. Ascender - Descender = Total Height
        /// </summary>
        public readonly float descenderHeight;

        public readonly Texture atlas;

        public readonly Dictionary<char, GlyphData> glyphs = new Dictionary<char, GlyphData>();

        public readonly GlyphData unsupportedGlyph;

#if DEBUG
        public Font(Renderer renderer, string filePath)
            : this(renderer, new FileStream(filePath, FileMode.Open))
        {
        }
#endif

        public Font(Renderer renderer, Stream str)
        {
            using (BinaryReader rs = new BinaryReader(str, Encoding.Unicode))
            {
                int type = rs.ReadInt32();
                isSDF = type == 1;
                atlasHeight = (float)rs.ReadInt32();
                ascenderHeight = rs.ReadSingle();
                descenderHeight = rs.ReadSingle();
                packingBuffer = rs.ReadInt32();

                int aWidth = rs.ReadInt32();
                int aHeight = rs.ReadInt32();

                int numChars = rs.ReadInt32();

                for (int i = 0; i < numChars; i++)
                {
                    char c = rs.ReadChar();
                    float aPosX = rs.ReadInt32() / (float)aWidth;
                    float aPosY = rs.ReadInt32() / (float)aHeight;
                    float width = rs.ReadSingle() / aWidth;
                    float height = rs.ReadSingle() / aHeight;
                    float bearingX = rs.ReadSingle();
                    float bearingY = rs.ReadSingle();
                    float advanceX = rs.ReadSingle();
                    float gWidth = rs.ReadSingle();
                    float gHeight = rs.ReadSingle();

                    int numKern = rs.ReadInt32();

                    Dictionary<char, float> kern = new Dictionary<char, float>();
                    for (int t = 0; t < numKern; t++)
                    {
                        char b = rs.ReadChar();
                        float k = rs.ReadSingle();

                        kern.Add(b, k);
                    }

                    glyphs.Add(c, new GlyphData(aPosX, aPosY, width, height, bearingX, bearingY, advanceX, gWidth, gHeight, kern));
                }

                byte[] img = new byte[aWidth * aHeight * 4];

                for (int i = 0; i < img.Length; i++)
                {
                    img[i] = rs.ReadByte();
                }

                atlas = new Texture(renderer, img, aWidth, aHeight, 0);
            }

            unsupportedGlyph = glyphs.GetValueOrDefault('\u25A1', glyphs['a']);
        }

        public void Dispose()
        {
            atlas.Dispose();
        }
    }
}
