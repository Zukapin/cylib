﻿using System;
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
using Device1 = SharpDX.Direct3D11.Device1;
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

        private readonly Device1 device;
        public Device1 Device
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

        private readonly SwapChain1 swapChain;
        public SwapChain1 SwapChain
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
            public const string VB_SPHERE_POS_NORM_UNIT = "VB_SPHERE_POS_NORM_UNIT";
            public const string VB_BOX_POS_NORM_UNIT = "VB_BOX_POS_NORM_UNIT";
            public const string VB_CYLINDER_POS_NORM_UNIT = "VB_CYLINDER_POS_NORM_UNIT";
            public const string VB_CAPSULE_POS_NORM_UNIT = "VB_CAPSULE_POS_NORM_UNIT";
            public const string VB_CAPSULE_POS_NORM_HALFRAD = "VB_CAPSULE_POS_NORM_HALFRAD";

            public const string BUF_WORLD = "BUF_WORLD";
            public const string BUF_CAM_VIEWPROJ = "BUF_CAM_VIEWPROJ";
            public const string BUF_CAM_INVVIEWPROJ = "BUF_CAM_INVVIEWPROJ";
            public const string BUF_FONT = "BUF_FONT";
            public const string BUF_POINT_LIGHT = "BUF_POINT_LIGHT";
            public const string BUF_DIRECTIONAL_LIGHT = "BUF_DIRECTIONAL_LIGHT";
            public const string BUF_COLOR = "BUF_COLOR";
            public const string BUF_ROUNDED_RECT = "BUF_ROUNDED_RECT";
            public const string BUF_CIRCLE = "BUF_CIRCLE";
            public const string BUF_BORDERED_CIRCLE = "BUF_BORDERED_CIRCLE";
            public const string BUF_RECTANGLE = "BUF_RECTANGLE";
            public const string BUF_BORDERED_RECTANGLE = "BUF_BORDERED_RECTANGLE";
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
            public const string SH_CIRCLE_2D = "SH_Circle2D";
            public const string SH_BORDERED_CIRCLE_2D = "SH_BorderedCircle2D";
            public const string SH_RECTANGLE_2D = "SH_Rectangle2D";
            public const string SH_BORDERED_RECTANGLE_2D = "SH_BorderedRectangle2D";

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


#if DEBUG
                using (var tempDevice = new SharpDX.Direct3D11.Device(DriverType.Hardware, DeviceCreationFlags.Debug, new[] { FeatureLevel.Level_11_0, FeatureLevel.Level_11_1 }))
#else
                using (var tempDevice = new SharpDX.Direct3D11.Device(DriverType.Hardware, DeviceCreationFlags.None, new[] { FeatureLevel.Level_11_1 }))
#endif
                {
                    device = tempDevice.QueryInterfaceOrNull<Device1>();

                    if (device == null)
                        throw new Exception("Couldn't create a DX11 device");
                }

                using (var dxgi = device.QueryInterface<SharpDX.DXGI.Device2>()) //i have no clue why this is device2
                using (var adapter = dxgi.Adapter)
                using (var factory = adapter.GetParent<Factory2>())
                {
                    var desc = new SwapChainDescription1()
                    {
                        BufferCount = 2,
                        Width = winWidth,
                        Height = winHeight,
                        AlphaMode = AlphaMode.Unspecified,
                        Flags = SwapChainFlags.None,
                        Format = Format.R8G8B8A8_UNorm,
                        Stereo = false,
                        Scaling = Scaling.Stretch,
                        SampleDescription = new SampleDescription(1, 0),
                        SwapEffect = SwapEffect.FlipDiscard,
                        Usage = Usage.RenderTargetOutput
                    };

                    swapChain = new SwapChain1(factory, device, window.OSHandle, ref desc);

                    factory.MakeWindowAssociation(window.Handle, WindowAssociationFlags.IgnoreAltEnter);
                }

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
                    (DefaultAssets.VB_CYLINDER_POS_NORM_UNIT, (Renderer r) => { return VertexBuffer.CreatePosNormCylinder(r, Vector3.Zero, 1f, 1f, Vector3.UnitY, 36); }),
                    (DefaultAssets.VB_SPHERE_POS_NORM_UNIT, (Renderer r) => { return VertexBuffer.CreatePosNormSphere(r, 1f); }),
                    (DefaultAssets.VB_CAPSULE_POS_NORM_UNIT, (Renderer r) => { return VertexBuffer.CreatePosNormCapsule(r, 1f, 1f); }),
                    (DefaultAssets.VB_CAPSULE_POS_NORM_HALFRAD, (Renderer r) => { return VertexBuffer.CreatePosNormCapsule(r, 0.5f, 1f); }),
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
                    (DefaultAssets.BUF_CIRCLE, (Renderer r) => { return new ConstBuffer<CircleData>(r, 128, ResourceUsage.Dynamic, BindFlags.ShaderResource, CpuAccessFlags.Write, ResourceOptionFlags.BufferStructured); }),
                    (DefaultAssets.BUF_BORDERED_CIRCLE, (Renderer r) => { return new ConstBuffer<BorderedCircleData>(r, 128, ResourceUsage.Dynamic, BindFlags.ShaderResource, CpuAccessFlags.Write, ResourceOptionFlags.BufferStructured); }),
                    (DefaultAssets.BUF_RECTANGLE, (Renderer r) => { return new ConstBuffer<RectangleData>(r, 128, ResourceUsage.Dynamic, BindFlags.ShaderResource, CpuAccessFlags.Write, ResourceOptionFlags.BufferStructured); }),
                    (DefaultAssets.BUF_BORDERED_RECTANGLE, (Renderer r) => { return new ConstBuffer<BorderedRectangleData>(r, 128, ResourceUsage.Dynamic, BindFlags.ShaderResource, CpuAccessFlags.Write, ResourceOptionFlags.BufferStructured); }),
                    (DefaultAssets.BUF_QUAD_INDEX, (Renderer r) => {
                        int ibLen = 128 * 6; //this should match the number of primitaives stored in buffers than use this
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

        internal void ChangeResolution(int NewResX, int NewResY)
        {
            SDL.SDL_SetWindowSize(window.Handle, NewResX, NewResY);
            ResolutionChanged();
        }

        private void ResolutionChanged()
        {
            SDL.SDL_GetWindowSize(window.Handle, out int winWidth, out int winHeight);
            resWidth = winWidth;
            resHeight = winHeight;

            swapChain.ResizeBuffers(2, winWidth, winHeight, Format.Unknown, SwapChainFlags.None);
        }

        public void Dispose()
        {//I dont actually know what this needs to do
            context.Dispose();
            device.Dispose();
        }
    }
}
