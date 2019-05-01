using System;
using System.Text;

using SharpDX;
using SharpDX.Direct3D11;

#if DEBUG
using SharpDX.D3DCompiler;
#endif

using System.IO;

namespace cylib
{
    public class Shader : IDisposable
    {
        readonly VertexShader vs;
        readonly PixelShader ps;
        readonly InputLayout layout;

#if DEBUG
        public Shader(Renderer renderer, string fileName, InputElement[] inputFormat)
        {//Assuming the shader is using a vertex shader and a pixel shader, named VS and PS respectivley
            Compile(fileName, true, out CompilationResult vertShader, out CompilationResult pixelShader);

            //should output errors/warnings here but whatever
            vs = new VertexShader(renderer.Device, vertShader);

            if (inputFormat != null)
                layout = new InputLayout(renderer.Device, vertShader, inputFormat);
            else
                layout = null;

            ps = new PixelShader(renderer.Device, pixelShader);

            vertShader.Dispose();
            pixelShader.Dispose();
        }

        private static void CompileAndWriteToFile(string shader, string outFile)
        {
            using (BinaryWriter shWr = new BinaryWriter(new FileStream(outFile, FileMode.Create)))
            {
                Compile(shader, false, out CompilationResult vs, out CompilationResult ps);

                byte[] vsBytes = vs.Bytecode.Data;
                byte[] psBytes = ps.Bytecode.Data;

                shWr.Write(vsBytes.Length);
                shWr.Write(vsBytes);
                shWr.Write(psBytes.Length);
                shWr.Write(psBytes);
            }
        }

        private static void Compile(string shader, bool debug, out CompilationResult vertexShader, out CompilationResult pixelShader)
        {
            vertexShader = ShaderBytecode.CompileFromFile(shader, "VS", "vs_5_0", debug ? ShaderFlags.Debug : ShaderFlags.OptimizationLevel3, EffectFlags.None);
            pixelShader = ShaderBytecode.CompileFromFile(shader, "PS", "ps_5_0", debug ? ShaderFlags.Debug : ShaderFlags.OptimizationLevel3, EffectFlags.None);
        }
#endif

        public Shader(Renderer renderer, Stream str, InputElement[] inputFormat)
        {
            BinaryReader fr = new BinaryReader(str, Encoding.Unicode, true);

            int vsLen = fr.ReadInt32();
            byte[] vsBytes = fr.ReadBytes(vsLen);

            int psLen = fr.ReadInt32();
            byte[] psBytes = fr.ReadBytes(psLen);

            vs = new VertexShader(renderer.Device, vsBytes);

            if (inputFormat != null)
                layout = new InputLayout(renderer.Device, vsBytes, inputFormat);
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
