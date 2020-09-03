using System;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Drawing;

using BepuUtilities;

namespace cylib
{
    [StructLayout(LayoutKind.Explicit, Size = 112)]
    struct RoundedRectData
    {
        [FieldOffset(0)]
        internal Matrix world;
        [FieldOffset(64)]
        internal Vector2 scale;
        [FieldOffset(72)]
        internal Vector2 radius; //x = rectangle corner radius, y = border width
        [FieldOffset(80)]
        internal Vector4 mainColor;
        [FieldOffset(96)]
        internal Vector4 borderColor;

        internal RoundedRectData(Matrix world, Vector2 scale, Vector2 radius, Color mainColor, Color borderColor)
        {
            this.world = world;
            this.scale = scale;
            this.radius = radius;
            this.mainColor = Texture.convertToLinear(mainColor);
            this.borderColor = Texture.convertToLinear(borderColor);
        }
    }

    public class RoundedRectangle_2D : IDisposable
    {
        public Shader shader;
        public VertexBuffer buf;
        private ConstBuffer<RoundedRectData> buffer;

        public Vector2 position;
        public Vector2 scale;
        public float radius;
        public float borderThickness;
        public Color mainColor;
        public Color borderColor;
        public bool enabled = true;

        Renderer renderer;
        EventManager em;

        public RoundedRectangle_2D(Renderer renderer, EventManager em, int priority)
        {
            this.renderer = renderer;
            this.em = em;

            shader = renderer.Assets.GetShader(Renderer.DefaultAssets.SH_ROUNDED_RECTANGLE_2D);
            buf = renderer.Assets.GetVertexBuffer(Renderer.DefaultAssets.VB_QUAD_POS_TEX_UNIT);
            buffer = renderer.Assets.GetBuffer<RoundedRectData>(Renderer.DefaultAssets.BUF_ROUNDED_RECT);

            position = new Vector2();
            scale = new Vector2();
            radius = 20;
            borderThickness = 2;

            mainColor = Color.Black;
            borderColor = Color.White;

            em.addDraw2D(priority, Draw2D);
        }

        void Draw2D()
        {
            if (!enabled)
                return;

            shader.Bind(renderer.Context);
            renderer.Context.InputAssembler.SetVertexBuffers(0, buf.vbBinding);

            Matrix3x3.CreateScale(new Vector3(scale.X * 1.2f, scale.Y * 1.2f, 1), out Matrix3x3 rotMat);
            Vector3 transl = new Vector3(position.X - scale.X * 0.1f, position.Y - scale.Y * 0.1f, 0);
            Matrix.CreateRigid(rotMat, transl, out buffer.dat[0].world);

            buffer.dat[0].scale = scale;
            buffer.dat[0].radius.X = radius;
            buffer.dat[0].radius.Y = borderThickness;
            buffer.dat[0].mainColor = Texture.convertToLinear(mainColor);
            buffer.dat[0].borderColor = Texture.convertToLinear(borderColor);

            buffer.Write(renderer.Context);
            renderer.Context.VertexShader.SetConstantBuffer(1, buffer.buf);
            renderer.Context.PixelShader.SetConstantBuffer(1, buffer.buf);
            renderer.Context.Draw(buf.numVerts, 0);
        }

        public void Dispose()
        {
            em.remove2D(Draw2D);
        }
    }
}
