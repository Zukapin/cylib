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
    public struct BorderedRectangleData
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

        public BorderedRectangleData(Vector2 pos, float width, float height, float borderWidth, float borderHeight, Color innerColor, Color borderColor)
        {
            this.pos = pos;
            this.widthHeight = new Vector2(width, height);
            this.borderWidthHeight = new Vector2(borderWidth, borderHeight);
            pad = new Vector2();
            this.innerColor = Texture.convertToLinear(innerColor);
            this.borderColor = Texture.convertToLinear(borderColor);
        }
    }

    public class BorderedRectangles_2D : IDisposable
    {
        public Shader shader;
        private ConstBuffer<BorderedRectangleData> rectBuf;
        private ConstBuffer<ushort> indexBuffer;

        IEnumerable<BorderedRectangleData> Rectangles;

        Renderer renderer;
        EventManager em;

        public BorderedRectangles_2D(Renderer renderer, EventManager em, int priority, IEnumerable<BorderedRectangleData> Rectangles)
        {
            this.renderer = renderer;
            this.em = em;

            shader = renderer.Assets.GetShader(Renderer.DefaultAssets.SH_BORDERED_RECTANGLE_2D);
            rectBuf = renderer.Assets.GetBuffer<BorderedRectangleData>(Renderer.DefaultAssets.BUF_BORDERED_RECTANGLE);
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