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
        const float timeStep = (float)(1 / 60.0);

        Device device;
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
        private readonly RenderTargetView renderView;
        private readonly DepthStencilState stencilDefault;

        public Renderer(Window window)
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
            var desc = new SwapChainDescription()
            {
                BufferCount = 2,
                ModeDescription = new ModeDescription(w, w, new Rational(60, 1), Format.R8G8B8A8_UNorm_SRgb),
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

            //Init primary render target
            var backBuffer = Resource.FromSwapChain<Texture2D>(swapChain, 0);
            renderView = new RenderTargetView(Device, backBuffer);

            stencilDefault = new DepthStencilState(Device, new DepthStencilStateDescription()
            {
                IsStencilEnabled = false,
                StencilReadMask = 0,
                StencilWriteMask = 0,
                DepthComparison = Comparison.Greater,
                DepthWriteMask = DepthWriteMask.All,
                IsDepthEnabled = true
            });

            //Init context
            context = Device.ImmediateContext;
        }

        public void Update()
        {
            SDL.SDL_Event ev;

            while (SDL2.SDL.SDL_PollEvent(out ev) != 0)
            {
                Console.WriteLine("event? : " + ev.type);
            }
        }

        public void Draw()
        {


            //SDL.SDL_SetRenderDrawColor(r, 255, 0, 0, 0);
            //SDL.SDL_RenderClear(r);

            context.Rasterizer.SetViewport(0, 0, 800, 600);
            context.ClearRenderTargetView(renderView, Color.Blue);

            swapChain.Present(1, PresentFlags.None);

            //SDL.SDL_RenderPresent(r);
        }
    }
}
