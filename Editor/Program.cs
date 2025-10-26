using System.Reflection;

using SFML.Window;

using Latte.Core;
using Latte.Application;


namespace LightRays.Editor;




class Program
{
    private static void Main()
    {
        var settings = AppInitializationSettings.Default;
        var contextSettings = AppInitializationSettings.DefaultContextSettings with { AntialiasingLevel = 4 };

        settings = settings with { ContextSettings = contextSettings };


        App.Init(VideoMode.DesktopMode, "2D Path Tracer", settings);


        App.Section = new MainSection();


        while (!App.ShouldQuit)
        {
            App.Update();
            App.Draw();
        }


        App.Deinit();
    }
}
