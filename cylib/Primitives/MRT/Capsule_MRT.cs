using System;
using System.Numerics;
using SharpDX.Direct3D11;
using Color = System.Drawing.Color;

using BepuUtilities;

namespace cylib
{
    public class Capsule_MRT
    {
        public Shader shader;
        public VertexBuffer buf;
        private ConstBuffer<Matrix> worldBuffer;
        ConstBuffer<ColorBuffer> colorBuf;

        public Vector3 position;
        public float scale;
        public Color color;
        public Matrix3x3 rotation;

        Renderer renderer;
        EventManager em;

        public Capsule_MRT(Renderer renderer, EventManager em, int priority, string VertexBuffer = Renderer.DefaultAssets.VB_CAPSULE_POS_NORM_UNIT)
        {
            this.renderer = renderer;
            this.em = em;

            shader = renderer.Assets.GetShader(Renderer.DefaultAssets.SH_POS_NORM_SCOLOR);
            buf = renderer.Assets.GetVertexBuffer(VertexBuffer);
            worldBuffer = renderer.Assets.GetBuffer<Matrix>(Renderer.DefaultAssets.BUF_WORLD);
            colorBuf = renderer.Assets.GetBuffer<ColorBuffer>(Renderer.DefaultAssets.BUF_COLOR);

            position = new Vector3();
            color = Color.DeepSkyBlue;
            scale = 1f;
            rotation = Matrix3x3.Identity;

            if (em != null)
                em.addDrawMRT(priority, DrawMRT);
        }

        public void DrawMRT()
        {
            shader.Bind(renderer.Context);
            renderer.Context.InputAssembler.SetVertexBuffers(0, buf.vbBinding);

            colorBuf.dat[0].color = Texture.convertToLinear(color);
            colorBuf.Write(renderer.Context);

            renderer.Context.PixelShader.SetConstantBuffer(2, colorBuf.buf);

            //we can't really do scale here for the capsule -- capsules are a line segment, defined by length and radius
            //there can't be a single vertex buffer that can capture both properties with a world transformation
            Matrix3x3.CreateScale(new Vector3(scale, scale, scale), out var s);
            var t = s * rotation;
            Matrix.CreateRigid(t, position, out worldBuffer.dat[0]);
            worldBuffer.Write(renderer.Context);

            renderer.Context.VertexShader.SetConstantBuffer(1, worldBuffer.buf);
            renderer.Context.Draw(buf.numVerts, 0);
        }

        public void Dispose()
        {
            if (em != null)
                em.removeMRT(DrawMRT);
        }
    }
}