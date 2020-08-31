using System;
using System.Numerics;

using SharpDX.Direct3D11;

using BepuUtilities;

namespace cylib
{
    public class TexturedQuad_2D : IDisposable
    {
        public Shader shader;
        public Texture tex;
        public VertexBuffer buf;
        public SamplerState sampler;
        private ConstBuffer<Matrix> worldBuffer;

        public Vector2 position;
        public Vector2 scale;

        Renderer renderer;
        EventManager em;

        public TexturedQuad_2D(Renderer renderer, EventManager em, int priority, Texture tex)
        {
            this.renderer = renderer;
            this.em = em;
            this.tex = tex;

            shader = renderer.Assets.getAsset(ShaderAssets.POS_TEX);
            buf = renderer.Assets.getAsset(VertexBufferAssets.QUAD_POS_TEX_UNIT);
            sampler = renderer.samplerLinear;
            worldBuffer = renderer.Assets.getAsset<Matrix>(BufferAssets.WORLD);

            position = new Vector2();
            scale = new Vector2();

            em.addDraw2D(priority, Draw2D);
        }

        void Draw2D()
        {
            shader.Bind(renderer.Context);
            renderer.Context.InputAssembler.SetVertexBuffers(0, buf.vbBinding);
            renderer.Context.PixelShader.SetShaderResource(0, tex.view);
            renderer.Context.PixelShader.SetSampler(0, sampler);

            Matrix3x3.CreateScale(new Vector3(scale.X, scale.Y, 1), out Matrix3x3 scaleMat);
            Matrix.CreateRigid(scaleMat, new Vector3(position.X, position.Y, 0), out worldBuffer.dat[0]);
            worldBuffer.Write(renderer.Context);

            renderer.Context.VertexShader.SetConstantBuffer(1, worldBuffer.buf);
            renderer.Context.Draw(buf.numVerts, 0);
        }

        public void Dispose()
        {
            em.remove2D(Draw2D);
        }
    }
}
