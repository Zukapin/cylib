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
    public struct BorderedCircleData
    {
        [FieldOffset(0)]
        public Vector2 pos;
        [FieldOffset(8)]
        public float radius;
        [FieldOffset(12)]
        public float border;
        [FieldOffset(16)]
        public Vector4 innerColor;
        [FieldOffset(32)]
        public Vector4 borderColor;

        public BorderedCircleData(Vector2 pos, float radius, float border, Color innerColor, Color borderColor)
        {
            this.pos = pos;
            this.radius = radius;
            this.border = border;
            this.innerColor = Texture.convertToLinear(innerColor);
            this.borderColor = Texture.convertToLinear(borderColor);
        }
    }

    public class BorderedCircles_2D : IDisposable
    {
        Shader shader;
        ConstBuffer<BorderedCircleData> circleBuf;
        ConstBuffer<ushort> indexBuffer;

        IEnumerable<BorderedCircleData> Circles;

        Renderer renderer;
        EventManager em;

        public BorderedCircles_2D(Renderer renderer, EventManager em, int priority, IEnumerable<BorderedCircleData> Circles)
        {
            this.renderer = renderer;
            this.em = em;

            shader = renderer.Assets.GetShader(Renderer.DefaultAssets.SH_BORDERED_CIRCLE_2D);
            circleBuf = renderer.Assets.GetBuffer<BorderedCircleData>(Renderer.DefaultAssets.BUF_BORDERED_CIRCLE);
            indexBuffer = renderer.Assets.GetBuffer<ushort>(Renderer.DefaultAssets.BUF_QUAD_INDEX);

            this.Circles = Circles;

            em.addDraw2D(priority, Draw2D);
        }

        void Draw2D()
        {
            shader.Bind(renderer.Context);
            renderer.Context.VertexShader.SetShaderResource(0, circleBuf.srv);
            renderer.Context.InputAssembler.SetIndexBuffer(indexBuffer.buf, Format.R16_UInt, 0);

            int index = 0;

            foreach (var c in Circles)
            {
                circleBuf.dat[index++] = c;

                if (index == circleBuf.numElements)
                {
                    circleBuf.Write(renderer.Context, 0, index);
                    renderer.Context.DrawIndexed(index * 6, 0, 0);
                    index = 0;
                }
            }

            if (index != 0)
            {
                circleBuf.Write(renderer.Context, 0, index);
                renderer.Context.DrawIndexed(index * 6, 0, 0);
            }
        }

        public void Dispose()
        {
            em.remove2D(Draw2D);
        }
    }
}