﻿using System;
using System.Text;

using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

using System.IO;

namespace cylib
{
    public class Shader : IDisposable
    {
        readonly VertexShader vs;
        readonly PixelShader ps;
        readonly InputLayout layout;

        public Shader(Renderer renderer, Stream str)
        {
            BinaryReader fr = new BinaryReader(str, Encoding.Unicode, true);

            int vsLen = fr.ReadInt32();
            byte[] vsBytes = fr.ReadBytes(vsLen);

            int psLen = fr.ReadInt32();
            byte[] psBytes = fr.ReadBytes(psLen);

            vs = new VertexShader(renderer.Device, vsBytes);

            int numElements = fr.ReadInt32();

            if (numElements > 0)
            {
                InputElement[] inputFormat = new InputElement[numElements];
                for (int i = 0; i < numElements; i++)
                {
                    string name = fr.ReadString();
                    int index = fr.ReadInt32();
                    Format format = (Format)fr.ReadInt32();
                    int offset = fr.ReadInt32();
                    int slot = fr.ReadInt32();

                    inputFormat[i] = new InputElement(name, index, format, offset, slot);
                }

                layout = new InputLayout(renderer.Device, vsBytes, inputFormat);
            }
            else
                layout = null;

            ps = new PixelShader(renderer.Device, psBytes);

            fr.Dispose();
        }

        public void Bind(DeviceContext context)
        {
            context.InputAssembler.InputLayout = layout;
            context.VertexShader.Set(vs);
            context.PixelShader.Set(ps);
        }

        public void Dispose()
        {
            vs.Dispose();
            ps.Dispose();

            if (layout != null)
                layout.Dispose();
        }
    }
}
