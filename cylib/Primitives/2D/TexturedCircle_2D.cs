using System;
using System.Numerics;
using System.Runtime.InteropServices;

using Color = SharpDX.Color;
using BepuUtilities;

using SharpDX.Direct3D11;

namespace cylib
{
    /// <summary>
    /// Assumes the buffer is a ortho circle
    /// Position will translate the circle center.
    /// Scale will scale the XY-plane.
    /// rot will rotate the circle along the other axis, 'spinning' the circle.
    /// </summary>
    public class TexturedCircle_2D : IDisposable
    {
        public Shader shader;
        public Texture tex;
        public VertexBuffer buf;
        public SamplerState sampler;
        private ConstBuffer<Matrix> worldBuffer;

        public Vector2 position;
        public Vector2 scale;
        public float rot;

        Renderer renderer;
        EventManager em;

        public TexturedCircle_2D(Renderer renderer, EventManager em, int priority, Texture tex)
        {
            this.renderer = renderer;
            this.em = em;
            this.tex = tex;

            shader = renderer.Assets.GetShader(ShaderAssets.POS_TEX);
            buf = renderer.Assets.GetVertexBuffer(VertexBufferAssets.CIRCLE_POS_TEX_UNIT);
            sampler = renderer.samplerLinear;
            worldBuffer = renderer.Assets.GetBuffer<Matrix>(BufferAssets.WORLD);

            position = new Vector2();
            scale = new Vector2();
            rot = 0;

            em.addDraw2D(priority, Draw2D);
        }

        void Draw2D()
        {
            shader.Bind(renderer.Context);
            renderer.Context.InputAssembler.SetVertexBuffers(0, buf.vbBinding);
            renderer.Context.PixelShader.SetShaderResource(0, tex.view);
            renderer.Context.PixelShader.SetSampler(0, sampler);

            Matrix3x3.CreateScale(new Vector3(scale.X, scale.Y, 1), out Matrix3x3 scaleMat);
            Matrix3x3.CreateFromAxisAngle(Vector3.UnitZ, rot, out Matrix3x3 rotMat);
            Matrix.CreateRigid(scaleMat * rotMat, new Vector3(position.X, position.Y, 0), out worldBuffer.dat[0]);

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