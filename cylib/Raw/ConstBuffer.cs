using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;
using MapFlags = SharpDX.Direct3D11.MapFlags;

namespace cylib
{
    public class ConstBuffer<T> : IDisposable where T : struct
    {
        internal readonly T[] dat;
        internal readonly Buffer buf;
        internal readonly int numElements;

        /// <summary>
        /// Shader resource view for the buffer, if created with flags where that makes sense. Otherwise null.
        /// </summary>
        internal readonly ShaderResourceView srv;

        internal ConstBuffer(Renderer renderer, int numElements, ResourceUsage resUsage, BindFlags bindFlags, CpuAccessFlags cpuFlags, ResourceOptionFlags resOptFlags)
        {
            this.numElements = numElements;

            if (resUsage == ResourceUsage.Dynamic)
            {
                dat = new T[numElements];

                int siz = Utilities.SizeOf<T>();
                buf = new Buffer(renderer.Device, siz * numElements, resUsage, bindFlags, cpuFlags, resOptFlags, siz);

                if (bindFlags == BindFlags.ShaderResource)
                    srv = new ShaderResourceView(renderer.Device, buf);
            }
            else
                throw new InvalidOperationException("Can't create a non-dynamic buffer inside of this constuctor");
        }

        internal ConstBuffer(Renderer renderer, int numElements, ResourceUsage resUsage, BindFlags bindFlags, CpuAccessFlags cpuFlags, ResourceOptionFlags resOptFlags, T[] data)
        {
            this.numElements = numElements;

            if (resUsage == ResourceUsage.Dynamic)
            {
                dat = new T[numElements];

                int siz = Utilities.SizeOf<T>();
                buf = new Buffer(renderer.Device, siz * numElements, resUsage, bindFlags, cpuFlags, resOptFlags, siz);

                if (bindFlags == BindFlags.ShaderResource)
                    srv = new ShaderResourceView(renderer.Device, buf);
            }
            else if (resUsage == ResourceUsage.Immutable)
            {
                //We don't need a cpu copy of this immutable buffer.
                dat = null;

                buf = Buffer.Create(renderer.Device, data, new BufferDescription()
                {
                    BindFlags = bindFlags,
                    CpuAccessFlags = cpuFlags,
                    Usage = resUsage,
                    OptionFlags = resOptFlags,
                    SizeInBytes = numElements * sizeof(ushort)
                });
            }
            else
                throw new InvalidOperationException("Don't know how to create this");
        }

        internal void Write(DeviceContext context)
        {
            var dataBox = context.MapSubresource(buf, 0, MapMode.WriteDiscard, MapFlags.None);
            Utilities.Write(dataBox.DataPointer, dat, 0, numElements);
            context.UnmapSubresource(buf, 0);
        }

        internal void Write(DeviceContext context, int offset, int num)
        {
            var dataBox = context.MapSubresource(buf, 0, MapMode.WriteDiscard, MapFlags.None);
            Utilities.Write(dataBox.DataPointer, dat, offset, num);
            context.UnmapSubresource(buf, 0);
        }

        public void Dispose()
        {
            buf.Dispose();

            if (srv != null)
                srv.Dispose();
        }
    }
}
