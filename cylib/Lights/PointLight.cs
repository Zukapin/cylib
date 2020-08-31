using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using System.Numerics;
using System.Runtime.InteropServices;

using Color = SharpDX.Color;
namespace cylib
{
    [StructLayout(LayoutKind.Explicit, Size = 32)]
    struct PointLightBuffer
    {
        [FieldOffset(0)]
        public Vector4 posRadius;
        [FieldOffset(16)]
        public Vector4 colorIntensity;
        internal PointLightBuffer(Vector3 pos, Color color, float radius, float intensity)
        {
            posRadius = new Vector4(pos.X, pos.Y, pos.Z, radius);
            colorIntensity = Texture.convertToLinear(color);
            colorIntensity.W = intensity;
        }
    }

    public class PointLight : IDisposable
    {
        PointLightBuffer myDat;
        ConstBuffer<PointLightBuffer> buf;

        public Vector3 pos
        {
            get
            {
                return new Vector3(myDat.posRadius.X, myDat.posRadius.Y, myDat.posRadius.Z);
            }
            set
            {
                myDat.posRadius.X = value.X;
                myDat.posRadius.Y = value.Y;
                myDat.posRadius.Z = value.Z;
            }
        }

        public float radius
        {
            get
            {
                return myDat.posRadius.W;
            }
            set
            {
                myDat.posRadius.W = value;
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

        public PointLight(Renderer renderer, EventManager em, Vector3 pos, Color color, float radius, float intensity)
        {
            this.renderer = renderer;
            this.em = em;

            myDat = new PointLightBuffer(pos, color, radius, intensity);
            buf = renderer.Assets.GetBuffer<PointLightBuffer>(BufferAssets.POINT_LIGHT);

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
