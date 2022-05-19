using System;
using System.IO;
using System.Reflection;
using cylib;

namespace coretest
{
    class Program
    {
        static void Main(string[] args)
        {
            //This is a workaround for VS and dotnet CLI consistency
            //By default, VS sets the working directory to be the bin directory, while CLI sets it to be the project directory.
            //This is apparently a contentious issue about web dev or something.
            Environment.CurrentDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

            CyLib.Init();

            var window = new Window("CoreTest", 1920, 1080, WindowFlags.NONE);
            var rend = new Renderer(window);
            rend.Assets.AddAssetBlob("coretest.blob");
            var stage = new GameStage(rend, ActionTypes.AllActions, "binds.cyb");
            var scene = new TestScene(stage);

            stage.switchToScene(scene);
            
            while (stage.Update())
            {
                stage.Draw();
            }

            //Console.ReadLine();
        }
    }
}
