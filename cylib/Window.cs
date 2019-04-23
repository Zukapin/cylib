using System;
using SDL2;

namespace cylib
{
    public class Window
    {
        public static void Init()
        {
#if DEBUG
            SDL.SDL_SetHint(SDL.SDL_HINT_WINDOWS_DISABLE_THREAD_NAMING, "1");
#endif

            SDL.SDL_Init(SDL.SDL_INIT_EVENTS | SDL.SDL_INIT_VIDEO | SDL.SDL_INIT_GAMECONTROLLER | SDL.SDL_INIT_AUDIO);
            SDL_image.IMG_Init(SDL_image.IMG_InitFlags.IMG_INIT_PNG | SDL_image.IMG_InitFlags.IMG_INIT_JPG);

            SDL.SDL_CreateWindow("Test Window", 0, 0, 800, 600, SDL.SDL_WindowFlags.SDL_WINDOW_ALLOW_HIGHDPI);
        }
    }
}
