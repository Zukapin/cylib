using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;

using SDL2;

namespace cylib
{
    public class Image : IDisposable
    {
        private IntPtr surface;
        public int Width;
        public int Height;

        public Image(string filename)
        {
            surface = SDL_image.IMG_Load(filename);

            Init();
        }

        public Image(Stream stream)
        {
            //okay we do some Stuff here
            //it's not ideal but we just load the entire image into memory, then create a SDL stream from a memory handle
            //we can probably somehow interop better -- SDL stream should translate to the C# stream, but it doesn't seem worth dealing with
            int len = (int)stream.Length;
            byte[] mem = new byte[len];
            stream.Read(mem, 0, len);

            var memHandle = GCHandle.Alloc(mem, GCHandleType.Pinned);
            IntPtr memPtr = memHandle.AddrOfPinnedObject();

            var RWop = SDL.SDL_RWFromMem(memPtr, len);
            surface = SDL_image.IMG_Load_RW(RWop, 1);

            memHandle.Free();

            Init();
        }

        private void Init()
        {
            //set Width/Height from the surface
            var surfDat = Marshal.PtrToStructure<SDL.SDL_Surface>(surface);

            Width = surfDat.w;
            Height = surfDat.h;
        }

        public byte[] GetRGBA()
        {
            var toRet = new byte[Width * Height * 4];
            var memHandle = GCHandle.Alloc(toRet, GCHandleType.Pinned);
            var memPtr = memHandle.AddrOfPinnedObject();

            var surfDat = Marshal.PtrToStructure<SDL.SDL_Surface>(surface);

            var pixelFormat = Marshal.PtrToStructure<SDL.SDL_PixelFormat>(surfDat.format);

            SDL.SDL_ConvertPixels(Width, Height, pixelFormat.format, surfDat.pixels, pixelFormat.BytesPerPixel * Width, SDL.SDL_PIXELFORMAT_ABGR8888, memPtr, Width * 4);

            memHandle.Free();

            return toRet;
        }

        public void Dispose()
        {
            SDL.SDL_FreeSurface(surface);
        }
    }
}
