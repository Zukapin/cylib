using System;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Collections.Generic;

using BepuUtilities;

using SharpDX.Direct3D11;
using SharpDX.DXGI;

namespace cylib
{
    [StructLayout(LayoutKind.Explicit, Size = 32)]
    public struct RectangleData
    {
        [FieldOffset(0)]
        public Vector2 pos;
        [FieldOffset(8)]
        public Vector2 widthHeight;
        [FieldOffset(16)]
        public Vector4 color;

        public RectangleData(Vector2 pos, float width, float height, Color color)
        {
            this.pos = pos;
            this.widthHeight = new Vector2(width, height);
            this.color = Texture.convertToLinear(color);
        }
    }

    public class Rectangles_2D : IDisposable
    {
        public Shader shader;
        private ConstBuffer<RectangleData> rectBuf;
        private ConstBuffer<ushort> indexBuffer;

        IEnumerable<RectangleData> Rectangles;

        Renderer renderer;
        EventManager em;

        public Rectangles_2D(Renderer renderer, EventManager em, int priority, IEnumerable<RectangleData> Rectangles)
        {
            this.renderer = renderer;
            this.em = em;

            shader = renderer.Assets.GetShader(Renderer.DefaultAssets.SH_RECTANGLE_2D);
            rectBuf = renderer.Assets.GetBuffer<RectangleData>(Renderer.DefaultAssets.BUF_RECTANGLE);
            indexBuffer = renderer.Assets.GetBuffer<ushort>(Renderer.DefaultAssets.BUF_QUAD_INDEX);

            this.Rectangles = Rectangles;

            em.addDraw2D(priority, Draw2D);
        }

        void Draw2D()
        {
            shader.Bind(renderer.Context);
            renderer.Context.VertexShader.SetShaderResource(0, rectBuf.srv);
            renderer.Context.InputAssembler.SetIndexBuffer(indexBuffer.buf, Format.R16_UInt, 0);

            int index = 0;

            foreach (var c in Rectangles)
            {
                rectBuf.dat[index++] = c;

                if (index == rectBuf.numElements)
                {
                    rectBuf.Write(renderer.Context, 0, index);
                    renderer.Context.DrawIndexed(index * 6, 0, 0);
                    index = 0;
                }
            }

            if (index != 0)
            {
                rectBuf.Write(renderer.Context, 0, index);
                renderer.Context.DrawIndexed(index * 6, 0, 0);
            }
        }

        public void Dispose()
        {
            em.remove2D(Draw2D);
        }
    }
}