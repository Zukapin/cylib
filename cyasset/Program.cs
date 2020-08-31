using System;
using SDL2;

namespace cyasset
{
    class Program
    {
        const string supportedChars = "abcdefghijklmnopqrstuvwxyz ABCDEFGHIJKLMNOPQRSTUVWXYZ`1234567890-=[];',./\\~!@#$%^&*()_+{}:\"<>?|";

        static void Main(string[] args)
        {
            SDL_image.IMG_Init(SDL_image.IMG_InitFlags.IMG_INIT_PNG | SDL_image.IMG_InitFlags.IMG_INIT_JPG);

            string fontSource = @"C:\Users\Owner\Source\Repos\cypherfunk\assetproc\Content\Fonts\segoeui.ttf";
            string fontOut = @"C:\Users\Owner\Desktop\work\test.cyf";

            int maxAtlasWidth = 1024;
            int packingBuffer = 4;

            var p = new FontProcessor(false, supportedChars, maxAtlasWidth, packingBuffer, 128, 16, 64f);
            p.ProcessFont(fontSource, fontOut);
        }
    }
}
