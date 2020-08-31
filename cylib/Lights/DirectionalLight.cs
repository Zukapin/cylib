using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using System.Numerics;
using System.Runtime.InteropServices;

using BepuUtilities;
using Color = SharpDX.Color;

namespace cylib
{
    [StructLayout(LayoutKind.Explicit, Size = 32)]
    struct DirectionalLightBuffer
    {
        [FieldOffset(0)]
        public Vector4 dir; //w is nothing
        [FieldOffset(16)]
        public Vector4 colorIntensity;
        internal DirectionalLightBuffer(Vector3 pos, Color color, float intensity)
        {
            dir = new Vector4(pos.X, pos.Y, pos.Z, 1f);
            colorIntensity = Texture.convertToLinear(color);
            colorIntensity.W = intensity;
        }
    }

    public class DirectionalLight : IDisposable
    {
        DirectionalLightBuffer myDat;
        ConstBuffer<DirectionalLightBuffer> buf;

        public Vector3 dir
        {
            get
            {
                return new Vector3(myDat.dir.X, myDat.dir.Y, myDat.dir.Z);
            }
            set
            {
                myDat.dir.X = value.X;
                myDat.dir.Y = value.Y;
                myDat.dir.Z = value.Z;
            }
        }

        public Color color
        {
            get
            {
                return new Color(myDat.colorIntensity.X, myDat.colorIntensity.Y, myDat.colorIntensity.Z);
            }
            set
            {
                myDat.colorIntensity.X = value.R / 255f;
                myDat.colorIntensity.Y = value.G / 255f;
                myDat.colorIntensity.Z = value.B / 255f;
            }
        }

        public float intensity
        {
            get
            {
                return myDat.colorIntensity.W;
            }
            set
            {
                myDat.colorIntensity.W = value;
            }
        }

        Renderer renderer;
        EventManager em;

        public DirectionalLight(Renderer renderer, EventManager em, Vector3 dir, Color color, float intensity)
        {
            this.renderer = renderer;
            this.em = em;

            myDat = new DirectionalLightBuffer(dir, color, intensity);
            buf = renderer.Assets.GetBuffer<DirectionalLightBuffer>(BufferAssets.DIRECTIONAL_LIGHT);

            em.addLight(this);
        }

        //we could probably batch the hell out of this somehow, rather than 1 light at a time, but deal with that when it's a problem
        public void Draw()
        {
            buf.dat[0] = myDat;
            buf.Write(renderer.Context);

            renderer.Context.PixelShader.SetConstantBuffer(3, buf.buf);
            renderer.Context.Draw(6, 0);
        }

        public void Dispose()
        {
            em.removeLight(this);
        }
    }
}
