using System;
using System.Collections.Generic;

using System.IO;
using System.Reflection;
using SharpDX.D3DCompiler;
using SharpDX.DXGI;

namespace cyasset
{
    class ShaderCompiler
    {
        class InputElementInformation
        {
            public string name;
            public int index;
            public Format format;
            public int offset;
            public int slot;

            public InputElementInformation(string name, int index, Format format, int offset, int slot)
            {
                this.name = name;
                this.index = index;
                this.format = format;
                this.offset = offset;
                this.slot = slot;
            }
        }

        public static void ProcessShader(string shader, Dictionary<string, string> opts, DateTime optsTime, string tempDir, out ContentHeaderInformation[] outInf, out DateTime latestDate)
        {
            var inf = new ContentHeaderInformation();
            inf.Name = "SH_" + Path.GetFileNameWithoutExtension(shader);
            inf.Path = tempDir + shader;
            inf.Type = AssetTypes.SHADER;

            //check timestamps here
            var inWritten = File.GetLastWriteTimeUtc(shader);
            var outWritten = DateTime.MinValue;
            if (File.Exists(inf.Path))
            {
                outWritten = File.GetLastWriteTimeUtc(inf.Path);
            }

            if (outWritten <= inWritten || outWritten <= optsTime)
            {//write output here
                //figure out vertex elements
                var inputElements = new List<InputElementInformation>();
                if (opts.TryGetValue("INPUTELEMENTS", out var inputElementString))
                {//we have elements in the opts, just parse it
                    var ele = inputElementString.Split(';');

                    if (ele.Length <= 1)
                    {
                        //if there are no arguments, assume thats the point
                    }
                    else
                    {
                        if (ele.Length % 5 != 0)
                        {
                            throw new Exception("INPUTELEMENTS arg in shader " + shader + " does not have the expected number of values: " + ele.Length + " " + inputElementString);
                        }

                        for (int i = 0; i < ele.Length; i += 5)
                        {
                            inputElements.Add(new InputElementInformation(
                                ele[i],
                                int.Parse(ele[i + 1]),
                                (Format)int.Parse(ele[i + 2]),
                                int.Parse(ele[i + 3]),
                                int.Parse(ele[i + 4]))
                                );
                        }
                    }
                }
                else
                {//attempt to parse the shader for the input element options automatically
                    //automatic-support is limited and I don't really care so deal with it
                    //we look for 'struct VS_IN' and then parse until '}'
                    //we are looking for exactly "POSITION" "COLOR" "NORMAL" "BINORMAL" "TANGENT" "TEXTURE", assumed to be input in that order
                    //these types will always be vec3, B8G8R8A8_UNorm, vec3, vec3, vec3, vec2, respectively
                    using (StreamReader sr = new StreamReader(new FileStream(shader, FileMode.Open, FileAccess.Read)))
                    {
                        string line;
                        bool foundVSIN = false;

                        bool foundPos = false;
                        bool foundColor = false;
                        bool foundNormal = false;
                        bool foundBinormal = false;
                        bool foundTangent = false;
                        bool foundTexture = false;
                        while ((line = sr.ReadLine()) != null)
                        {
                            if (foundVSIN)
                            {
                                if (line.Contains("POSITION"))
                                    foundPos = true;
                                if (line.Contains("COLOR"))
                                    foundColor = true;
                                if (line.Contains("NORMAL"))
                                    foundNormal = true;
                                if (line.Contains("BINORMAL"))
                                    foundBinormal = true;
                                if (line.Contains("TANGENT"))
                                    foundTangent = true;
                                if (line.Contains("TEXTURE"))
                                    foundTexture = true;

                                if (line.Contains('}'))
                                    break;
                            }

                            if (line.Contains("struct VS_IN"))
                            {
                                foundVSIN = true;
                            }
                        }

                        int offset = 0;
                        if (foundPos)
                        {
                            inputElements.Add(new InputElementInformation("POSITION", 0, Format.R32G32B32_Float, offset, 0));
                            offset += 12;
                        }

                        if (foundColor)
                        {
                            inputElements.Add(new InputElementInformation("COLOR", 0, Format.R8G8B8A8_UNorm, offset, 0));
                            offset += 4;
                        }

                        if (foundNormal)
                        {
                            inputElements.Add(new InputElementInformation("NORMAL", 0, Format.R32G32B32_Float, offset, 0));
                            offset += 12;
                        }

                        if (foundBinormal)
                        {
                            inputElements.Add(new InputElementInformation("BINORMAL", 0, Format.R32G32B32_Float, offset, 0));
                            offset += 12;
                        }

                        if (foundTangent)
                        {
                            inputElements.Add(new InputElementInformation("TANGENT", 0, Format.R32G32B32_Float, offset, 0));
                            offset += 12;
                        }

                        if (foundTexture)
                        {
                            inputElements.Add(new InputElementInformation("TEXTURE", 0, Format.R32G32_Float, offset, 0));
                            offset += 8;
                        }
                    }
                }

                var dir = Path.GetDirectoryName(inf.Path);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                Console.WriteLine("Compiling shader " + shader + " to " + inf.Path + " with asset name " + inf.Name);

                using (FileStream of = new FileStream(inf.Path, FileMode.Create, FileAccess.ReadWrite))
                {
                    CompileAndWriteToFile(shader, of);

                    using (BinaryWriter wr = new BinaryWriter(of, System.Text.Encoding.Unicode))
                    {
                        wr.Write(inputElements.Count);
                        foreach (var i in inputElements)
                        {
                            wr.Write(i.name);
                            wr.Write(i.index);
                            wr.Write((int)i.format);
                            wr.Write(i.offset);
                            wr.Write(i.slot);
                        }
                    }
                }

                //update write time
                outWritten = File.GetLastWriteTimeUtc(inf.Path);
            }

            FileInfo fileDetails = new FileInfo(inf.Path);
            inf.FileLength = fileDetails.Length;
            latestDate = outWritten;

            outInf = new ContentHeaderInformation[1];
            outInf[0] = inf;
        }

        public static void CompileAndWriteToFile(string shader, Stream outStream)
        {
            //write length of VS, VS, length of PS, PS, num input elements, input elements
            using (BinaryWriter shWr = new BinaryWriter(outStream, System.Text.Encoding.Unicode, true))
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
