using System;
using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using SDL2;

using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;
using Resource = SharpDX.Direct3D11.Resource;
using MapFlags = SharpDX.Direct3D11.MapFlags;

using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;
using BepuUtilities;

namespace cylib
{
    public class Renderer
    {
        private int resWidth;
        public int ResolutionWidth
        {
            get
            {
                return resWidth;
            }
        }

        private int resHeight;
        public int ResolutionHeight
        {
            get
            {
                return resHeight;
            }
        }

        public bool VSync { get; set; } = true;

        private readonly Device device;
        public Device Device
        {
            get
            {
                return device;
            }
        }

        private readonly DeviceContext context;
        public DeviceContext Context
        {
            get
            {
                return context;
            }
        }

        private readonly SwapChain swapChain;
        public SwapChain SwapChain
        {
            get
            {
                return swapChain;
            }
        }

        #region Samplers
        SamplerState mySamplerAnisotropy;
        public SamplerState samplerAnisotropy
        {
            get
            {
                return mySamplerAnisotropy;
            }
        }
        SamplerState mySamplerLinear;
        public SamplerState samplerLinear
        {
            get
            {
                return mySamplerLinear;
            }
        }
        SamplerState mySamplerPoint;
        public SamplerState samplerPoint
        {
            get
            {
                return mySamplerPoint;
            }
        }
        #endregion
        #region Rasters
        RasterizerState myRasterDebug;
        public RasterizerState rasterDebug
        {
            get
            {
                return myRasterDebug;
            }
        }
        RasterizerState myRasterNormal;
        public RasterizerState rasterNormal
        {
            get
            {
                return myRasterNormal;
            }
        }
        RasterizerState myRasterNormalScissor;
        public RasterizerState rasterNormalScissor
        {
            get
            {
                return myRasterNormalScissor;
            }
        }
        #endregion
        #region Blend States
        BlendState myBlendLight;
        public BlendState blendLight
        {
            get
            {
                return myBlendLight;
            }
        }
        BlendState myBlendDefault;
        public BlendState blendDefault
        {
            get
            {
                return myBlendDefault;
            }
        }
        BlendState myBlendTransparent;
        public BlendState blendTransparent
        {
            get
            {
                return myBlendTransparent;
            }
        }
        BlendState myBlendAlphaTest; //not really used right now. turns on alpha-to-coverage, so low-alpha pixels get culled
        public BlendState blendAlphaTest
        {
            get
            {
                return myBlendAlphaTest;
            }
        }
        #endregion


        AssetManager assetManager;
        public AssetManager Assets
        {
            get
            {
                return assetManager;
            }
        }

        public Window window { get; }

        public static class DefaultAssets
        {
            public const string VB_QUAD_POS_TEX_UNIT = "VB_QUAD_POS_TEX_UNIT";
            public const string VB_CIRCLE_POS_TEX_UNIT = "VB_CIRCLE_POS_TEX_UNIT";
            public const string VB_CIRCLE_POS_TEX_NORM_UNIT = "VB_CIRCLE_POS_TEX_NORM_UNIT";
            public const string VB_BOX_POS_NORM_UNIT = "VB_BOX_POS_NORM_UNIT";
            public const string VB_CYLINDER_POS_NORM_UNIT = "VB_CYLINDER_POS_NORM_UNIT";

            public const string BUF_WORLD = "BUF_WORLD";
            public const string BUF_CAM_VIEWPROJ = "BUF_CAM_VIEWPROJ";
            public const string BUF_CAM_INVVIEWPROJ = "BUF_CAM_INVVIEWPROJ";
            public const string BUF_FONT = "BUF_FONT";
            public const string BUF_POINT_LIGHT = "BUF_POINT_LIGHT";
            public const string BUF_DIRECTIONAL_LIGHT = "BUF_DIRECTIONAL_LIGHT";
            public const string BUF_COLOR = "BUF_COLOR";
            public const string BUF_ROUNDED_RECT = "BUF_ROUNDED_RECT";
            public const string BUF_QUAD_INDEX = "BUF_QUAD_INDEX";

            public const string FONT_DEFAULT = "FONT_DEFAULT";

            public const string SH_COMPILE = "SH_Compile";
            public const string SH_LIGHT_DIRECTIONAL = "SH_DirectionalLight";
            public const string SH_FONT_BITMAP = "SH_FontBitmap";
            public const string SH_FONT_SDF = "SH_FontSDF";
            public const string SH_LIGHT_POINT = "SH_PointLight";
            public const string SH_ROUNDED_RECTANGLE_2D = "SH_RoundedRectangle2D";
            public const string SH_POS_TEX = "SH_PosTex";
            public const string SH_POS_NORM_TEX = "SH_PosNormTex";
            public const string SH_POS_NORM_MAPTEX = "SH_PosNormMapTex";
            public const string SH_POS_NORM_SCOLOR = "SH_PosNormSColor";

            public const string SH_DEBUG_DEPTH = "SH_DepthDebug";
            public const string SH_DEBUG_NORMAL = "SH_NormalDebug";
            public const string SH_DEBUG_LIGHT = "SH_LightDebug";
            public const string SH_DEBUG_COLOR = "SH_ColorDebug";
        }


        public Renderer(Window window)
        {
            this.window = window;
            {
#if WINDOWS
                //This only works on windows -- the SysWMInfo will exist, but 'info.win.window' is a windows-only pointer to the window handle
                //other platforms require other shenanigans -- we can directly create a vulkan/opengl context for linux
                //but osx is a bit weird -- can directly create an opengl context, but vulkan isn't supported. can get other window handles using WMinf

                //just ifdefing windows here so this throws a hard compiler exception
                SDL.SDL_SysWMinfo winInfo = new SDL.SDL_SysWMinfo();
                SDL.SDL_GetWindowWMInfo(window.Handle, ref winInfo);
#endif

                //creates the base device, device context, and swap chain
                //the swapchain needs to be resized when the window is resized
                SDL.SDL_GetWindowSize(window.Handle, out int winWidth, out int winHeight);
                resWidth = winWidth;
                resHeight = winHeight;

                var desc = new SwapChainDescription()
                {
                    BufferCount = 2,
                    ModeDescription = new ModeDescription(winWidth, winHeight, new Rational(60, 1), Format.R8G8B8A8_UNorm_SRgb),
                    IsWindowed = true,
                    OutputHandle = winInfo.info.win.window,
                    SampleDescription = new SampleDescription(1, 0),
                    SwapEffect = SwapEffect.Discard,
                    Usage = Usage.RenderTargetOutput
                };

#if DEBUG
                Device.CreateWithSwapChain(DriverType.Hardware, DeviceCreationFlags.Debug, desc, out device, out swapChain);
#else
            Device.CreateWithSwapChain(DriverType.Hardware, DeviceCreationFlags.None, desc, out device, out swapChain);
#endif

                var factory = swapChain.GetParent<Factory>();
                factory.MakeWindowAssociation(window.Handle, WindowAssociationFlags.IgnoreAltEnter);

                factory.Dispose();

                //Init context
                context = Device.ImmediateContext;

                assetManager = new AssetManager(this);
                assetManager.AddAssetBlob("cylibassets.blob");

                assetManager.AddVertexBuffers(new List<(string, VBLoader)>()
                {
                    (DefaultAssets.VB_QUAD_POS_TEX_UNIT, (Renderer r) => { return VertexBuffer.CreatePosTexQuad(r, Vector2.Zero, new Vector2(1, 1)); }),
                    (DefaultAssets.VB_CIRCLE_POS_TEX_UNIT, (Renderer r) => { return VertexBuffer.CreatePosTexCircle(r, Vector3.Zero, new Vector3(0, 0.5f, 0), new Vector3(0, 0, -1), 36); }),
                    (DefaultAssets.VB_CIRCLE_POS_TEX_NORM_UNIT, (Renderer r) => { return VertexBuffer.CreatePosTexNormCircle(r, Vector3.Zero, new Vector3(0, 0.5f, 0), new Vector3(0, 0, -1), 36); }),
                    (DefaultAssets.VB_BOX_POS_NORM_UNIT, (Renderer r) => { return VertexBuffer.CreatePosNormBox(r, Vector3.Zero, new Vector3(1, 1, 1)); }),
                    (DefaultAssets.VB_CYLINDER_POS_NORM_UNIT, (Renderer r) => { return VertexBuffer.CreatePosNormCylinder(r, Vector3.Zero, 0.5f, 1f, Vector3.UnitY, 36); }),
                });

                assetManager.AddBufferLoaders(new List<(string, AssetLoader)>()
                {
                    (DefaultAssets.BUF_WORLD, (Renderer r) => { return new ConstBuffer<Matrix>(r, 1, ResourceUsage.Dynamic, BindFlags.ConstantBuffer, CpuAccessFlags.Write, ResourceOptionFlags.None); }),
                    (DefaultAssets.BUF_CAM_VIEWPROJ, (Renderer r) => { return new ConstBuffer<CameraBuffer>(r, 1, ResourceUsage.Dynamic, BindFlags.ConstantBuffer, CpuAccessFlags.Write, ResourceOptionFlags.None); }),
                    (DefaultAssets.BUF_CAM_INVVIEWPROJ, (Renderer r) => { return new ConstBuffer<Matrix>(r, 1, ResourceUsage.Dynamic, BindFlags.ConstantBuffer, CpuAccessFlags.Write, ResourceOptionFlags.None); }),
                    (DefaultAssets.BUF_FONT, (Renderer r) => { return new ConstBuffer<FontGlyphBuffer>(r, 6 * 32, ResourceUsage.Dynamic, BindFlags.ShaderResource, CpuAccessFlags.Write, ResourceOptionFlags.BufferStructured); }),
                    (DefaultAssets.BUF_POINT_LIGHT, (Renderer r) => { return new ConstBuffer<PointLightBuffer>(r, 1, ResourceUsage.Dynamic, BindFlags.ConstantBuffer, CpuAccessFlags.Write, ResourceOptionFlags.None); }),
                    (DefaultAssets.BUF_DIRECTIONAL_LIGHT, (Renderer r) => { return new ConstBuffer<DirectionalLightBuffer>(r, 1, ResourceUsage.Dynamic, BindFlags.ConstantBuffer, CpuAccessFlags.Write, ResourceOptionFlags.None); }),
                    (DefaultAssets.BUF_COLOR, (Renderer r) => { return new ConstBuffer<ColorBuffer>(r, 1, ResourceUsage.Dynamic, BindFlags.ConstantBuffer, CpuAccessFlags.Write, ResourceOptionFlags.None); }),
                    (DefaultAssets.BUF_ROUNDED_RECT, (Renderer r) => { return new ConstBuffer<RoundedRectData>(r, 1, ResourceUsage.Dynamic, BindFlags.ConstantBuffer, CpuAccessFlags.Write, ResourceOptionFlags.None); }),
                    (DefaultAssets.BUF_QUAD_INDEX, (Renderer r) => {
                        int ibLen = 128 * 6;
                        ushort[] ib = new ushort[ibLen];
                        for (int i = 0; i < ib.Length / 6; i++)
                        {
                            ib[i * 6 + 0] = (ushort)(i * 4 + 0);
                            ib[i * 6 + 1] = (ushort)(i * 4 + 1);
                            ib[i * 6 + 2] = (ushort)(i * 4 + 2);
                            ib[i * 6 + 3] = (ushort)(i * 4 + 2);
                            ib[i * 6 + 4] = (ushort)(i * 4 + 1);
                            ib[i * 6 + 5] = (ushort)(i * 4 + 3);
                        }
                        return new ConstBuffer<ushort>(r, ibLen, ResourceUsage.Immutable, BindFlags.IndexBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, ib);
                    }),
                });
            }

            #region Raster Init
            myRasterDebug = new RasterizerState(Device, new RasterizerStateDescription()
            {
                CullMode = CullMode.None,
                FillMode = FillMode.Solid
            });
            myRasterNormal = new RasterizerState(Device, new RasterizerStateDescription()
            {
                CullMode = CullMode.Back,
                FillMode = FillMode.Solid
            });
            myRasterNormalScissor = new RasterizerState(Device, new RasterizerStateDescription()
            {
                CullMode = CullMode.Back,
                FillMode = FillMode.Solid,
                IsScissorEnabled = true
            });
            Context.Rasterizer.State = rasterNormal;
            #endregion
            #region Sampler Init
            mySamplerAnisotropy = new SamplerState(Device, new SamplerStateDescription()
            {
                AddressU = TextureAddressMode.Wrap,
                AddressV = TextureAddressMode.Wrap,
                AddressW = TextureAddressMode.Wrap,
                Filter = Filter.Anisotropic,
                MaximumAnisotropy = 16
            });

            mySamplerLinear = new SamplerState(Device, new SamplerStateDescription()
            {
                AddressU = TextureAddressMode.Wrap,
                AddressV = TextureAddressMode.Wrap,
                AddressW = TextureAddressMode.Wrap,
                Filter = Filter.MinMagMipLinear,
                MaximumLod = float.MaxValue,
                MinimumLod = 0,
                MipLodBias = 0
            });

            mySamplerPoint = new SamplerState(Device, new SamplerStateDescription()
            {
                AddressU = TextureAddressMode.Wrap,
                AddressV = TextureAddressMode.Wrap,
                AddressW = TextureAddressMode.Wrap,
                Filter = Filter.MinMagMipPoint
            });
            #endregion
            #region Blend State Setup
            {
                BlendStateDescription desc = new BlendStateDescription();
                desc.AlphaToCoverageEnable = false;
                desc.IndependentBlendEnable = false;
                desc.RenderTarget[0].BlendOperation = BlendOperation.Add;
                desc.RenderTarget[0].AlphaBlendOperation = BlendOperation.Add;
                desc.RenderTarget[0].DestinationBlend = BlendOption.One;
                desc.RenderTarget[0].SourceBlend = BlendOption.One;
                desc.RenderTarget[0].SourceAlphaBlend = BlendOption.One;
                desc.RenderTarget[0].DestinationAlphaBlend = BlendOption.One;
                desc.RenderTarget[0].RenderTargetWriteMask = ColorWriteMaskFlags.All;
                desc.RenderTarget[0].IsBlendEnabled = true;
                myBlendLight = new BlendState(Device, desc);

                desc.RenderTarget[0].IsBlendEnabled = false;
                myBlendDefault = new BlendState(Device, desc);

                desc.AlphaToCoverageEnable = false;
                desc.IndependentBlendEnable = false;
                desc.RenderTarget[0].BlendOperation = BlendOperation.Add;
                desc.RenderTarget[0].AlphaBlendOperation = BlendOperation.Add;
                desc.RenderTarget[0].DestinationBlend = BlendOption.InverseSourceAlpha;
                desc.RenderTarget[0].SourceBlend = BlendOption.SourceAlpha;
                desc.RenderTarget[0].SourceAlphaBlend = BlendOption.SourceAlpha;
                desc.RenderTarget[0].DestinationAlphaBlend = BlendOption.DestinationAlpha;
                desc.RenderTarget[0].RenderTargetWriteMask = ColorWriteMaskFlags.All;
                desc.RenderTarget[0].IsBlendEnabled = true;
                myBlendTransparent = new BlendState(Device, desc);

                desc.AlphaToCoverageEnable = true;
                desc.IndependentBlendEnable = false;
                desc.RenderTarget[0].BlendOperation = BlendOperation.Add;
                desc.RenderTarget[0].AlphaBlendOperation = BlendOperation.Add;
                desc.RenderTarget[0].DestinationBlend = BlendOption.InverseSourceAlpha;
                desc.RenderTarget[0].SourceBlend = BlendOption.SourceAlpha;
                desc.RenderTarget[0].SourceAlphaBlend = BlendOption.SourceAlpha;
                desc.RenderTarget[0].DestinationAlphaBlend = BlendOption.DestinationAlpha;
                desc.RenderTarget[0].RenderTargetWriteMask = ColorWriteMaskFlags.All;
                desc.RenderTarget[0].IsBlendEnabled = true;
                myBlendAlphaTest = new BlendState(Device, desc);
            }
            #endregion
        }

        public void Dispose()
        {//I dont actually know what this needs to do
            context.Dispose();
            device.Dispose();
        }
    }
}
