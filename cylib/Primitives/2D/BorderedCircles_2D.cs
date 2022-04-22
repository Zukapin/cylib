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
    [StructLayout(LayoutKind.Explicit, Size = 48)]
    struct BorderedCircleData
    {
        [FieldOffset(0)]
        public Vector2 pos;
        [FieldOffset(8)]
        public Vector2 radiusBorder;
        [FieldOffset(16)]
        public Vector4 centerColor;
        [FieldOffset(32)]
        public Vector4 borderColor;
    }

    public class BorderedCircles_2D : IDisposable
    {
        public Shader shader;
        private ConstBuffer<BorderedCircleData> circleBuf;
        private ConstBuffer<ushort> indexBuffer;

        public List<(Vector2 position, float radius, float border, Color centerColor, Color borderColor)> Circles;

        Renderer renderer;
        EventManager em;

        public BorderedCircles_2D(Renderer renderer, EventManager em, int priority)
        {
            this.renderer = renderer;
            this.em = em;

            shader = renderer.Assets.GetShader(Renderer.DefaultAssets.SH_BORDERED_CIRCLE_2D);
            circleBuf = renderer.Assets.GetBuffer<BorderedCircleData>(Renderer.DefaultAssets.BUF_BORDERED_CIRCLE);
            indexBuffer = renderer.Assets.GetBuffer<ushort>(Renderer.DefaultAssets.BUF_QUAD_INDEX);

            Circles = new List<(Vector2 position, float radius, float border, Color centerColor, Color borderColor)>();

            em.addDraw2D(priority, Draw2D);
        }

        void Draw2D()
        {
            shader.Bind(renderer.Context);
            renderer.Context.VertexShader.SetShaderResource(0, circleBuf.srv);
            renderer.Context.InputAssembler.SetIndexBuffer(indexBuffer.buf, Format.R16_UInt, 0);

            int index = 0;

            while (index < Circles.Count)
            {
                int loops = Math.Min(index + circleBuf.numElements, Circles.Count) - index;

                for (int i = 0; i < loops; i++)
                {
                    var c = Circles[i + index];
                    circleBuf.dat[i].pos = c.position;
                    circleBuf.dat[i].radiusBorder = new Vector2(c.radius, c.border);
                    circleBuf.dat[i].centerColor = Texture.convertToLinear(c.centerColor);
                    circleBuf.dat[i].borderColor = Texture.convertToLinear(c.borderColor);
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