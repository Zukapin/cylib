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
    struct CircleData
    {
        [FieldOffset(0)]
        public Vector3 pos;
        [FieldOffset(12)]
        public float radius;
        [FieldOffset(16)]
        public Vector4 color;
    }

    public class Circles_2D : IDisposable
    {
        public Shader shader;
        private ConstBuffer<CircleData> circleBuf;
        private ConstBuffer<ushort> indexBuffer;

        public List<(Vector2 position, float radius, Color color)> Circles;

        Renderer renderer;
        EventManager em;

        public Circles_2D(Renderer renderer, EventManager em, int priority)
        {
            this.renderer = renderer;
            this.em = em;

            shader = renderer.Assets.GetShader(Renderer.DefaultAssets.SH_CIRCLE_2D);
            circleBuf = renderer.Assets.GetBuffer<CircleData>(Renderer.DefaultAssets.BUF_CIRCLE);
            indexBuffer = renderer.Assets.GetBuffer<ushort>(Renderer.DefaultAssets.BUF_QUAD_INDEX);

            Circles = new List<(Vector2 position, float radius, Color color)>();

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
                    circleBuf.dat[i].pos = new Vector3(Circles[i + index].position, 0f);
                    circleBuf.dat[i].radius = Circles[i + index].radius;
                    circleBuf.dat[i].color = Texture.convertToLinear(Circles[i + index].color);
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