using Avalonia;
using Avalonia.Skia;
using AvReckoner;
using System;
namespace AvReckoner
{

    internal class Program
    {
        [STAThread]
        public static void Main(string[] args)
            => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .With(new SkiaOptions
                {
                    // Optional: tune memory limits, rendering behavior
                    MaxGpuResourceSizeBytes = 256 * 1024 * 1024,
                    // Optional: if you want to restrict GPU context behavior
                    // FramesPerSecond = 60,
                    // EnableVulkan = true  // (works only in 11.1+)
                })
                .LogToTrace();
    }
}
