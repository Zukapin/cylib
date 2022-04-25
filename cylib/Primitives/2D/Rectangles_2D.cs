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
    struct RectangleData
    {
        [FieldOffset(0)]
        public Vector2 pos;
        [FieldOffset(8)]
        public Vector2 widthHeight;
        [FieldOffset(16)]
        public Vector4 color;
    }

    public class Rectangles_2D : IDisposable
    {
        public Shader shader;
        private ConstBuffer<RectangleData> circleBuf;
        private ConstBuffer<ushort> indexBuffer;

        public List<(Vector2 position, float width, float height, Color color)> Rectangles;

        Renderer renderer;
        EventManager em;

        public Rectangles_2D(Renderer renderer, EventManager em, int priority)
        {
            this.renderer = renderer;
            this.em = em;

            shader = renderer.Assets.GetShader(Renderer.DefaultAssets.SH_RECTANGLE_2D);
            circleBuf = renderer.Assets.GetBuffer<RectangleData>(Renderer.DefaultAssets.BUF_RECTANGLE);
            indexBuffer = renderer.Assets.GetBuffer<ushort>(Renderer.DefaultAssets.BUF_QUAD_INDEX);

            Rectangles = new List<(Vector2 position, float width, float height, Color color)>();

            em.addDraw2D(priority, Draw2D);
        }

        void Draw2D()
        {
            shader.Bind(renderer.Context);
            renderer.Context.VertexShader.SetShaderResource(0, circleBuf.srv);
            renderer.Context.InputAssembler.SetIndexBuffer(indexBuffer.buf, Format.R16_UInt, 0);

            int index = 0;

            while (index < Rectangles.Count)
            {
                int loops = Math.Min(index + circleBuf.numElements, Rectangles.Count) - index;

                for (int i = 0; i < loops; i++)
                {
                    var r = Rectangles[i + index];
                    circleBuf.dat[i].pos = r.position;
                    circleBuf.dat[i].widthHeight = new Vector2(r.width, r.height);
                    circleBuf.dat[i].color = Texture.convertToLinear(r.color);
                }

                circleBuf.Write(renderer.Context, 0, loops);

                renderer.Context.DrawIndexed(loops * 6, 0, 0);

                index += loops;
            }
        }

        public void Dispose()
        {
            em.remove2D(Draw2D);
        }
    }
}