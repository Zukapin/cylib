using System;

using System.IO;
using SharpDX.D3DCompiler;

namespace cyasset
{
    class ShaderCompiler
    {
        public static void CompileAndWriteToFile(string shader, string outFile)
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

        public static void Compile(string shader, bool debug, out CompilationResult vertexShader, out CompilationResult pixelShader)
        {
            vertexShader = ShaderBytecode.CompileFromFile(shader, "VS", "vs_5_0", debug ? ShaderFlags.Debug : ShaderFlags.OptimizationLevel3, EffectFlags.None);
            pixelShader = ShaderBytecode.CompileFromFile(shader, "PS", "ps_5_0", debug ? ShaderFlags.Debug : ShaderFlags.OptimizationLevel3, EffectFlags.None);
        }
    }
}
