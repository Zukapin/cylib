using System;
using SDL2;

namespace cylib
{
    [Flags]
    public enum WindowFlags : uint
    {
        NONE = 0,
        FULLSCREEN_DESKTOP = SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN_DESKTOP,
        FULLSCREEN = SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN
    }

    public static class CyLib
    {
        /// <summary>
        /// Initializes all of the expected elements for a game.
        /// Use other initialization methods for more fine-grained control.
        /// 
        /// Must be called before trying to create windows, graphics devices, loading images, etc.
        /// </summary>
        public static void Init()
        {
            InitUI();
            InitImages();
        }

        public static void InitUI()
        {
            SDL.SDL_Init(SDL.SDL_INIT_EVENTS | SDL.SDL_INIT_VIDEO | SDL.SDL_INIT_GAMECONTROLLER | SDL.SDL_INIT_AUDIO);
        }
        
        public static void InitImages()
        {
            SDL_image.IMG_Init(SDL_image.IMG_InitFlags.IMG_INIT_PNG | SDL_image.IMG_InitFlags.IMG_INIT_JPG);
        }
    }

    public class Window
    {

        private readonly IntPtr windowHandle;

        public IntPtr Handle
        {
            get
            {
                return windowHandle;
            }
        }

        public Window(string title, int resX, int resY, WindowFlags flags)
        {
            windowHandle = SDL.SDL_CreateWindow(title, 50, 50, resX, resY, SDL.SDL_WindowFlags.SDL_WINDOW_ALLOW_HIGHDPI | (SDL.SDL_WindowFlags)flags);

            if (windowHandle == IntPtr.Zero)
            {
                throw new Exception("Failed to create a window: " + SDL.SDL_GetError());
            }
        }
    }
}
