using System;
using System.Numerics;
using SharpDX.Direct3D11;
using Color = System.Drawing.Color;

using BepuUtilities;

namespace cylib
{
    public class Cylinder_MRT
    {
        public Shader shader;
        public VertexBuffer buf;
        private ConstBuffer<Matrix> worldBuffer;
        ConstBuffer<ColorBuffer> colorBuf;

        public Vector3 position;
        public Color color;

        Renderer renderer;
        EventManager em;

        public Cylinder_MRT(Renderer renderer, EventManager em, int priority)
        {
            this.renderer = renderer;
            this.em = em;

            shader = renderer.Assets.GetShader(Renderer.DefaultAssets.SH_POS_NORM_SCOLOR);
            buf = renderer.Assets.GetVertexBuffer(Renderer.DefaultAssets.VB_CYLINDER_POS_NORM_UNIT);
            worldBuffer = renderer.Assets.GetBuffer<Matrix>(Renderer.DefaultAssets.BUF_WORLD);
            colorBuf = renderer.Assets.GetBuffer<ColorBuffer>(Renderer.DefaultAssets.BUF_COLOR);

            position = new Vector3();
            color = Color.Green;

            em.addDrawMRT(priority, DrawMRT);
        }

        void DrawMRT()
        {
            shader.Bind(renderer.Context);
            renderer.Context.InputAssembler.SetVertexBuffers(0, buf.vbBinding);

            colorBuf.dat[0].color = Texture.convertToLinear(color);
            colorBuf.Write(renderer.Context);

            renderer.Context.PixelShader.SetConstantBuffer(2, colorBuf.buf);

            Matrix.CreateRigid(Matrix3x3.Identity, position, out worldBuffer.dat[0]);
            worldBuffer.Write(renderer.Context);

            renderer.Context.VertexShader.SetConstantBuffer(1, worldBuffer.buf);
            renderer.Context.Draw(buf.numVerts, 0);
        }

        public void Dispose()
        {
            em.removeMRT(DrawMRT);
        }
    }
}