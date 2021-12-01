using System;
using SDL2;
using System.Runtime.InteropServices;

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
            var err = SDL.SDL_Init(SDL.SDL_INIT_EVENTS | SDL.SDL_INIT_VIDEO | SDL.SDL_INIT_GAMECONTROLLER | SDL.SDL_INIT_AUDIO);
            if (err != 0)
                throw new Exception("?? Init Error ??");
        }
        
        public static void InitImages()
        {
            var err = SDL_image.IMG_Init(SDL_image.IMG_InitFlags.IMG_INIT_PNG | SDL_image.IMG_InitFlags.IMG_INIT_JPG);
            if (err != (int)(SDL_image.IMG_InitFlags.IMG_INIT_PNG | SDL_image.IMG_InitFlags.IMG_INIT_JPG))
            {
                var msg = SDL.SDL_GetError();
                throw new Exception("SDL Error during init: " + msg);
            }
        }

        //okay this is very jank
        private static int FREQUENCY = 44100;
        private static double AMPLITUDE = 1;
        private static float[] AudioBuffer;
        private static GCHandle AudioBufferHandle;
        private static uint AudioDevice;
        public static unsafe void InitSound()
        {

            SDL.SDL_AudioSpec desiredSpec = new SDL.SDL_AudioSpec();

            desiredSpec.freq = FREQUENCY;
            desiredSpec.format = SDL.AUDIO_F32SYS;
            desiredSpec.channels = 2;
            desiredSpec.samples = 2048;
            //desiredSpec.callback = audio_callback; this instantly crashes every time?
            desiredSpec.userdata = (IntPtr)0xAAAAAAAA;

            SDL.SDL_AudioSpec obtainedSpec;

            // you might want to look for errors here
            //okay so this is very dumb -- if you want the default audio device you should pass in null for the device name
            //the bindings don't allow that -- it tries to deref null grumbutt
            //but i can't easily change it, would have to recompile the bindings
            //just searching for the specific name of my audio dev...
            var numDev = SDL.SDL_GetNumAudioDevices(0);
            var name = SDL.SDL_GetAudioDeviceName(2, 0);
            for (int i = 0; i < numDev; i++)
            {
                var c = SDL.SDL_GetAudioDeviceName(i, 0);
                if (c.StartsWith("Speakers (Real"))
                {
                    name = c;
                }
                
            }
            AudioDevice = SDL.SDL_OpenAudioDevice(name, 0, ref desiredSpec, out obtainedSpec, (int)SDL.SDL_AUDIO_ALLOW_ANY_CHANGE);

            AudioBuffer = new float[obtainedSpec.freq * obtainedSpec.channels]; //can play 1sec
            AudioBufferHandle = GCHandle.Alloc(AudioBuffer, GCHandleType.Pinned);
            SDL.SDL_PauseAudioDevice(AudioDevice, 0);
        }

        public static unsafe void Beep(double freq, int duration)
        {
            uint numSamples = Math.Min((uint)(duration * FREQUENCY * 2 / 1000), (uint)(AudioBuffer.Length / 2));
            int wI = 0;
            double v = 0;
            for (int i = 0; i < numSamples; i++)
            {
                var amp = (float)(AMPLITUDE * Math.Sin(v * 2 * Math.PI / FREQUENCY));
                AudioBuffer[wI++] = amp; //2 channels
                AudioBuffer[wI++] = amp;

                v += freq;
            }

            fixed (float* ptr = AudioBuffer)
            {
                var err = SDL.SDL_QueueAudio(AudioDevice, (IntPtr)ptr, (uint)numSamples * 8);
                if (err != 0)
                {
                    throw new Exception("Error queing audio: " + SDL.SDL_GetError());
                }
            }
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

        public IntPtr OSHandle
        {
            get
            {
                SDL.SDL_SysWMinfo info = default;
                SDL.SDL_GetWindowWMInfo(windowHandle, ref info);
                return info.info.win.window; //only works on windows
            }
        }

        public string Title
        {
            set
            {
                SDL.SDL_SetWindowTitle(windowHandle, value);
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
