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
    
    /*
     * 

        public static InputElement[] GetVertexElements(ShaderAssets shader)
        {
            switch (shader)
            {
                case ShaderAssets.COMPILE:
                    return VertexPositionTexture.vertexElements;
                case ShaderAssets.DEBUG_COLOR:
                    return VertexPositionTexture.vertexElements;
                case ShaderAssets.DEBUG_DEPTH:
                    return VertexPositionTexture.vertexElements;
                case ShaderAssets.DEBUG_LIGHT:
                    return VertexPositionTexture.vertexElements;
                case ShaderAssets.DEBUG_NORMAL:
                    return VertexPositionTexture.vertexElements;
                case ShaderAssets.DISPLACEMENT_MAP:
                    return VertexPositionNormalMapTexture.vertexElements;
                case ShaderAssets.LIGHT_POINT:
                    return VertexPositionTexture.vertexElements;
                case ShaderAssets.LIGHT_DIRECTIONAL:
                    return VertexPositionTexture.vertexElements;
                case ShaderAssets.POS_NORM_MAP_TEX:
                    return VertexPositionNormalMapTexture.vertexElements;
                case ShaderAssets.POS_NORM_TEX:
                    return VertexPositionNormalTexture.vertexElements;
                case ShaderAssets.POS_NORM_SCOLOR:
                    return VertexPositionNormal.vertexElements;
                case ShaderAssets.POS_TEX:
                    return VertexPositionTexture.vertexElements;
                case ShaderAssets.FONT_BITMAP:
                    return null;
                case ShaderAssets.FONT_SDF:
                    return null;
                case ShaderAssets.LINE:
                    return null;
                case ShaderAssets.LINE_COLORED:
                    return null;
                case ShaderAssets.GRADIENT_CIRCLE:
                    return null;
                case ShaderAssets.DASHED_CIRCLE:
                    return null;
                case ShaderAssets.BORDERED_CIRCLE:
                    return null;
                case ShaderAssets.ROUNDED_RECTANGLE_3D:
                    return null;
                case ShaderAssets.ROUNDED_RECTANGLE_2D:
                    return VertexPositionTexture.vertexElements;
                case ShaderAssets.TEST:
                    return VertexPositionTexture.vertexElements;
                default:
                    throw new Exception("Missing shader vert ele case! " + shader);
            }
        }
     */
}
