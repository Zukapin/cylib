using System;
using System.Numerics;
using SharpDX.Direct3D11;

using BepuUtilities;

namespace cylib
{
    /// <summary>
    /// Assumes the buffer is a 3D circle with postex coords in the XY plane with radius = 1, facing positive Z.
    /// Position will translate the circle center.
    /// Scale will scale the XY-plane.
    /// Face will rotate the circle so that the face is pointing in the 'face' direction. Must be normalized.
    /// rot will rotate the circle along the other axis, 'spinning' the circle.
    /// </summary>
    public class TexturedCircle_MRT
    {
        public Shader shader;
        public Texture tex;
        public VertexBuffer buf;
        public SamplerState sampler;
        private ConstBuffer<Matrix> worldBuffer;

        public Vector3 position;
        public Vector2 scale;
        public Vector3 face;
        public float rot;

        Renderer renderer;
        EventManager em;

        public TexturedCircle_MRT(Renderer renderer, EventManager em, int priority, Texture tex)
        {
            this.renderer = renderer;
            this.em = em;
            this.tex = tex;

            shader = renderer.Assets.getAsset(ShaderAssets.POS_NORM_TEX);
            buf = renderer.Assets.getAsset(VertexBufferAssets.CIRCLE_POS_TEX_NORM_UNIT);
            sampler = renderer.samplerLinear;
            worldBuffer = renderer.Assets.getAsset<Matrix>(BufferAssets.WORLD);

            position = new Vector3();
            scale = new Vector2(1, 1);
            face = Vector3.UnitZ;
            rot = 0;

            em.addDrawMRT(priority, DrawMRT);
        }

        void DrawMRT()
        {
            shader.Bind(renderer.Context);
            renderer.Context.InputAssembler.SetVertexBuffers(0, buf.vbBinding);
            renderer.Context.PixelShader.SetShaderResource(0, tex.view);
            renderer.Context.PixelShader.SetSampler(0, sampler);

            Vector3 axis = Vector3.Cross(-Vector3.UnitZ, face);
            if (axis.LengthSquared() < 0.000001f) //if UnitZ and face are sufficiently close, just use UnitY as the axis of rotation
                axis = Vector3.UnitY;

            Matrix3x3.CreateScale(new Vector3(scale.X, scale.Y, 1), out Matrix3x3 scaleMat);
            Matrix3x3.CreateFromAxisAngle(Vector3.UnitZ, -rot, out Matrix3x3 yawMat);
            Matrix3x3.CreateFromAxisAngle(axis, (float)Math.Acos(Vector3.Dot(-Vector3.UnitZ, face)), out Matrix3x3 rotMat);
            Matrix.CreateRigid(scaleMat * yawMat * rotMat, position, out worldBuffer.dat[0]);
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