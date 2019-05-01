using System;
using System.IO;

using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

using Vector4 = System.Numerics.Vector4;

namespace cylib
{
    public class Texture : IDisposable
    {
        private Texture2D rawTex;
        public ShaderResourceView view;

        public readonly int width;
        public readonly int height;

#if DEBUG
        public Texture(Renderer renderer, string filename)
        {
            Image i = new Image(filename);
            var img = i.GetRGBA();

            this.width = i.Width;
            this.height = i.Height;


            Init(renderer, img, i.Width, i.Height, 0);

            i.Dispose();
        }
#endif

        public Texture(Renderer renderer, Stream file)
        {
            //TODO: fileformat for processed textures
            Image i = new Image(file);
            var img = i.GetRGBA();

            this.width = i.Width;
            this.height = i.Height;


            Init(renderer, img, i.Width, i.Height, 0);

            i.Dispose();
        }

        public Texture(Renderer renderer, byte[] img, int width, int height, int mips)
        {
            this.width = width;
            this.height = height;

            Init(renderer, img, width, height, mips);
        }

        /// <summary>
        /// Actual texture init code. All constructors should call this after parsing w/e file/resource format into what we want to use.
        /// </summary>
        /// <param name="stage">The DX Context to laod the texture into</param>
        /// <param name="img">The image, as an RGBA byte array. Length should be width * height * 4.</param>
        /// <param name="width">Image pixel width</param>
        /// <param name="height">Image pixel height</param>
        /// <param name="mips">Number of mip levels to generate. 0 for entire mip chain, 1 the base image, 2 for base image + one mip level, etc.</param>
        private void Init(Renderer renderer, byte[] img, int width, int height, int mips)
        {//img should be RGBA
            if (img == null)
                throw new ArgumentNullException("img bytes null");

            int numMips = mips;

            if (numMips == 0)
            {
                numMips = 1;
                int cur = Math.Max(width, height);
                while (cur != 1)
                {
                    numMips++;
                    cur = cur / 2;
                }
            }

            Texture2DDescription desc = new Texture2DDescription()
            {
                ArraySize = 1,
                BindFlags = BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.None,
                Format = Format.R8G8B8A8_UNorm_SRgb,
                Height = height,
                Width = width,
                MipLevels = mips,
                OptionFlags = ResourceOptionFlags.None,
                Usage = ResourceUsage.Default,
                SampleDescription = new SampleDescription()
                {
                    Count = 1,
                    Quality = 0
                }
            };

            DataStream[] streams = new DataStream[numMips];
            DataRectangle[] data = new DataRectangle[numMips];

            {//set the starting mip
                DataStream s = new DataStream(width * height * 4, true, true);
                s.Write(img, 0, width * height * 4);

                streams[0] = s;
                data[0] = new DataRectangle(s.DataPointer, width * 4);
            }

            float[] lastRect = new float[img.Length];
            for (int x = 0; x < img.Length; x++)
            {
                lastRect[x] = convertToLinear(img[x], x % 4);
            }

            int oldStride = width * 4;
            bool wOdd = width % 2 == 1;
            bool hOdd = height % 2 == 1;
            int w = width / 2;
            int h = height / 2;
            bool wCap = false;
            bool hCap = false;
            for (int x = 1; x < numMips; x++)
            {
                if (w == 0)
                {
                    w = 1;
                    wCap = true;
                }

                if (h == 0)
                {
                    h = 1;
                    hCap = true;
                }

                int stride = w * 4;
                float[] nextRect = new float[w * h * 4];

                if (!wCap && !hCap)
                {
                    for (int yy = 0; yy < h; yy++)
                    {
                        int yi = yy * stride;
                        int oy = yy * 2 * oldStride;
                        int ny = oy + oldStride;
                        bool addRow = hOdd && yy == h - 1;
                        for (int xx = 0; xx < w; xx++)
                        {
                            int i = yi + xx * 4;
                            int ox = xx * 2 * 4;
                            int nx = ox + 4;
                            bool addCol = wOdd && xx == w - 1;

                            if (!addRow && !addCol)
                            {
                                nextRect[i + 0] = (lastRect[oy + ox + 0] + lastRect[ny + ox + 0] + lastRect[oy + nx + 0] + lastRect[ny + nx + 0]) / 4;
                                nextRect[i + 1] = (lastRect[oy + ox + 1] + lastRect[ny + ox + 1] + lastRect[oy + nx + 1] + lastRect[ny + nx + 1]) / 4;
                                nextRect[i + 2] = (lastRect[oy + ox + 2] + lastRect[ny + ox + 2] + lastRect[oy + nx + 2] + lastRect[ny + nx + 2]) / 4;
                                nextRect[i + 3] = (lastRect[oy + ox + 3] + lastRect[ny + ox + 3] + lastRect[oy + nx + 3] + lastRect[ny + nx + 3]) / 4;
                            }
                            else if (addRow && !addCol)
                            {
                                nextRect[i + 0] = (lastRect[oy + ox + 0] + lastRect[ny + ox + 0] + lastRect[oy + nx + 0] + lastRect[ny + nx + 0] + lastRect[ny + ox + oldStride + 0] + lastRect[ny + nx + oldStride + 0]) / 6;
                                nextRect[i + 1] = (lastRect[oy + ox + 1] + lastRect[ny + ox + 1] + lastRect[oy + nx + 1] + lastRect[ny + nx + 1] + lastRect[ny + ox + oldStride + 1] + lastRect[ny + nx + oldStride + 1]) / 6;
                                nextRect[i + 2] = (lastRect[oy + ox + 2] + lastRect[ny + ox + 2] + lastRect[oy + nx + 2] + lastRect[ny + nx + 2] + lastRect[ny + ox + oldStride + 2] + lastRect[ny + nx + oldStride + 2]) / 6;
                                nextRect[i + 3] = (lastRect[oy + ox + 3] + lastRect[ny + ox + 3] + lastRect[oy + nx + 3] + lastRect[ny + nx + 3] + lastRect[ny + ox + oldStride + 3] + lastRect[ny + nx + oldStride + 3]) / 6;
                            }
                            else if (addCol && !addRow)
                            {
                                nextRect[i + 0] = (lastRect[oy + ox + 0] + lastRect[ny + ox + 0] + lastRect[oy + nx + 0] + lastRect[ny + nx + 0] + lastRect[oy + nx + 4 + 0] + lastRect[ny + nx + 4 + 0]) / 6;
                                nextRect[i + 1] = (lastRect[oy + ox + 1] + lastRect[ny + ox + 1] + lastRect[oy + nx + 1] + lastRect[ny + nx + 1] + lastRect[oy + nx + 4 + 1] + lastRect[ny + nx + 4 + 1]) / 6;
                                nextRect[i + 2] = (lastRect[oy + ox + 2] + lastRect[ny + ox + 2] + lastRect[oy + nx + 2] + lastRect[ny + nx + 2] + lastRect[oy + nx + 4 + 2] + lastRect[ny + nx + 4 + 2]) / 6;
                                nextRect[i + 3] = (lastRect[oy + ox + 3] + lastRect[ny + ox + 3] + lastRect[oy + nx + 3] + lastRect[ny + nx + 3] + lastRect[oy + nx + 4 + 3] + lastRect[ny + nx + 4 + 3]) / 6;
                            }
                            else //addRow && addCol
                            {
                                int nny = ny + oldStride;
                                int nnx = nx + 4;
                                nextRect[i + 0] = (lastRect[oy + ox + 0] + lastRect[ny + ox + 0] + lastRect[oy + nx + 0] + lastRect[ny + nx + 0]
                                    + lastRect[ox + nny + 0] + lastRect[nx + nny + 0] + lastRect[nnx + nny + 0] + lastRect[nnx + oy + 0] + lastRect[nnx + ny + 0]) / 9;
                                nextRect[i + 1] = (lastRect[oy + ox + 1] + lastRect[ny + ox + 1] + lastRect[oy + nx + 1] + lastRect[ny + nx + 1]
                                    + lastRect[ox + nny + 1] + lastRect[nx + nny + 1] + lastRect[nnx + nny + 1] + lastRect[nnx + oy + 1] + lastRect[nnx + ny + 1]) / 9;
                                nextRect[i + 2] = (lastRect[oy + ox + 2] + lastRect[ny + ox + 2] + lastRect[oy + nx + 2] + lastRect[ny + nx + 2]
                                    + lastRect[ox + nny + 2] + lastRect[nx + nny + 2] + lastRect[nnx + nny + 2] + lastRect[nnx + oy + 2] + lastRect[nnx + ny + 2]) / 9;
                                nextRect[i + 3] = (lastRect[oy + ox + 3] + lastRect[ny + ox + 3] + lastRect[oy + nx + 3] + lastRect[ny + nx + 3]
                                    + lastRect[ox + nny + 3] + lastRect[nx + nny + 3] + lastRect[nnx + nny + 3] + lastRect[nnx + oy + 3] + lastRect[nnx + ny + 3]) / 9;
                            }
                        }
                    }
                }
                else if (wCap)
                {
                    for (int yy = 0; yy < h; yy++)
                    {
                        int yi = yy * stride;
                        int oy = yy * 2 * oldStride;
                        int ny = oy + oldStride;
                        bool addRow = hOdd && yy == h - 1;
                        for (int xx = 0; xx < w; xx++)
                        {
                            int i = yi + xx * 4;
                            int ox = xx * 4;

                            if (!addRow)
                            {
                                nextRect[i + 0] = (lastRect[oy + ox + 0] + lastRect[ny + ox + 0]) / 2;
                                nextRect[i + 1] = (lastRect[oy + ox + 1] + lastRect[ny + ox + 1]) / 2;
                                nextRect[i + 2] = (lastRect[oy + ox + 2] + lastRect[ny + ox + 2]) / 2;
                                nextRect[i + 3] = (lastRect[oy + ox + 3] + lastRect[ny + ox + 3]) / 2;
                            }
                            else //addRow
                            {
                                nextRect[i + 0] = (lastRect[oy + ox + 0] + lastRect[ny + ox + 0] + lastRect[ny + ox + oldStride + 0]) / 3;
                                nextRect[i + 1] = (lastRect[oy + ox + 1] + lastRect[ny + ox + 1] + lastRect[ny + ox + oldStride + 1]) / 3;
                                nextRect[i + 2] = (lastRect[oy + ox + 2] + lastRect[ny + ox + 2] + lastRect[ny + ox + oldStride + 2]) / 3;
                                nextRect[i + 3] = (lastRect[oy + ox + 3] + lastRect[ny + ox + 3] + lastRect[ny + ox + oldStride + 3]) / 3;
                            }
                        }
                    }
                }
                else //hCap
                {
                    for (int yy = 0; yy < h; yy++)
                    {
                        int yi = yy * stride;
                        int oy = yy * oldStride;
                        for (int xx = 0; xx < w; xx++)
                        {
                            int i = yi + xx * 4;
                            int ox = xx * 2 * 4;
                            int nx = ox + 4;
                            bool addCol = wOdd && xx == w - 1;

                            if (!addCol)
                            {
                                nextRect[i + 0] = (lastRect[oy + ox + 0] + lastRect[oy + nx + 0]) / 2;
                                nextRect[i + 1] = (lastRect[oy + ox + 1] + lastRect[oy + nx + 1]) / 2;
                                nextRect[i + 2] = (lastRect[oy + ox + 2] + lastRect[oy + nx + 2]) / 2;
                                nextRect[i + 3] = (lastRect[oy + ox + 3] + lastRect[oy + nx + 3]) / 2;
                            }
                            else //addCol
                            {
                                nextRect[i + 0] = (lastRect[oy + ox + 0] + lastRect[oy + nx + 0] + lastRect[oy + nx + 4 + 0]) / 3;
                                nextRect[i + 1] = (lastRect[oy + ox + 1] + lastRect[oy + nx + 1] + lastRect[oy + nx + 4 + 1]) / 3;
                                nextRect[i + 2] = (lastRect[oy + ox + 2] + lastRect[oy + nx + 2] + lastRect[oy + nx + 4 + 2]) / 3;
                                nextRect[i + 3] = (lastRect[oy + ox + 3] + lastRect[oy + nx + 3] + lastRect[oy + nx + 4 + 3]) / 3;
                            }
                        }
                    }
                }

                for (int t = 0; t < w * h * 4; t++)
                {
                    img[t] = converToSRgb(nextRect[t], t % 4);
                }

                DataStream s = new DataStream(w * h * 4, true, true);
                s.Write(img, 0, w * h * 4);

                streams[x] = s;
                data[x] = new DataRectangle(s.DataPointer, stride);

                wOdd = w % 2 == 1;
                hOdd = h % 2 == 1;
                w = w / 2;
                h = h / 2;
                lastRect = nextRect;
                oldStride = stride;
            }


            rawTex = new Texture2D(renderer.Device, desc, data);
            view = new ShaderResourceView(renderer.Device, rawTex);

            for (int x = 0; x < streams.Length; x++)
            {
                streams[x].Dispose();
            }
        }

        private static float convertToLinear(byte srgb, int channel)
        {
            float v = srgb / 255f;

            if (channel == 3)
                return v;

            if (v < 0.04045f)
                return v / 12.92f;
            return (float)Math.Pow((v + 0.055) / 1.055, 2.4);
        }

        private static byte converToSRgb(float linear, int channel)
        {
            if (channel == 3)
                return (byte)(linear * 255f);

            if (linear < 0.0031308f)
                return (byte)(linear * 12.92 * 255.0);
            return (byte)((Math.Pow(linear, 1.0 / 2.4) * 1.055 - 0.055) * 255.0);
        }

        public static Vector4 convertToLinear(SharpDX.Color color)
        {
            return new Vector4(convertToLinear(color.R, 0), convertToLinear(color.G, 1), convertToLinear(color.B, 2), convertToLinear(color.A, 3));
        }

        public static SharpDX.Color convertToSrgb(Vector4 linear)
        {
            return new SharpDX.Color(converToSRgb(linear.X, 0), converToSRgb(linear.Y, 1), converToSRgb(linear.Z, 2), converToSRgb(linear.W, 3));
        }

        public void Dispose()
        {
            view.Dispose();
            rawTex.Dispose();
        }
    }
}
