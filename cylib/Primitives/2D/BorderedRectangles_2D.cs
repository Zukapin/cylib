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
    [StructLayout(LayoutKind.Explicit, Size = 64)]
    struct BorderedRectangleData
    {
        [FieldOffset(0)]
        public Vector2 pos;
        [FieldOffset(8)]
        public Vector2 widthHeight;
        [FieldOffset(16)]
        public Vector2 borderWidthHeight;
        [FieldOffset(24)]
        public Vector2 pad;
        [FieldOffset(32)]
        public Vector4 innerColor;
        [FieldOffset(48)]
        public Vector4 borderColor;
    }

    public class BorderedRectangles_2D : IDisposable
    {
        public Shader shader;
        private ConstBuffer<BorderedRectangleData> circleBuf;
        private ConstBuffer<ushort> indexBuffer;

        public List<(Vector2 position, float width, float height, float borderWidth, float borderHeight, 
            Color innerColor, Color borderColor)> Rectangles;

        Renderer renderer;
        EventManager em;

        public BorderedRectangles_2D(Renderer renderer, EventManager em, int priority)
        {
            this.renderer = renderer;
            this.em = em;

            shader = renderer.Assets.GetShader(Renderer.DefaultAssets.SH_BORDERED_RECTANGLE_2D);
            circleBuf = renderer.Assets.GetBuffer<BorderedRectangleData>(Renderer.DefaultAssets.BUF_BORDERED_RECTANGLE);
            indexBuffer = renderer.Assets.GetBuffer<ushort>(Renderer.DefaultAssets.BUF_QUAD_INDEX);

            Rectangles = new List<(Vector2 position, float width, float height, float borderWidth,
                float borderHeight, Color innerColor, Color borderColor)>();

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
                    circleBuf.dat[i].borderWidthHeight = new Vector2(r.borderWidth, r.borderHeight);
                    circleBuf.dat[i].innerColor = Texture.convertToLinear(r.innerColor);
                    circleBuf.dat[i].borderColor = Texture.convertToLinear(r.borderColor);
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