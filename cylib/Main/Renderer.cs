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
using Color = SharpDX.Color;

namespace cylib
{
    public class Renderer
    {
        private int resWidth;
        public int ResolutionWidth
        {
            get
            {
                return 0;
            }
        }

        private int resHeight;
        public int ResolutionHeight
        {
            get
            {
                return 0;
            }
        }

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

        public Renderer(Window window)
        {
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

                assetManager = new AssetManager(this, @"Content\blob.cy");
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
    }
}
