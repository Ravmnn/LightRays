using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using SFML.Graphics;

using Latte.Core.Type;



namespace LightRays.Engine;




public readonly record struct PixelColor(Vec2f Position, ColorRGBA Color);




public class SampleRenderedEventArgs(Image finalSample, Image sample) : EventArgs
{
    public Image FinalSample { get; } = finalSample;
    public Image Sample { get; } = sample;
}




public class PathTracer
{
    private Thread _renderThread;
    private CancellationTokenSource _renderThreadCancellationTokenSource;




    public List<Object> Objects { get; set; }
    public List<LightRaySource> RaySources { get; set; }

    public List<LightRay> Rays { get; set; } = [];


    public Vec2u SampleResolution { get; set; } = new Vec2u(1920, 1080);
    public Vec2u SampleViewport { get; set; } = new Vec2u(1920, 1080);

    public uint Samples { get; set; } = 1;


    public event EventHandler? RenderingStartedEvent;
    public event EventHandler? RenderingFinishedEvent;
    public event EventHandler<SampleRenderedEventArgs>? SampleRenderedEvent;




    public PathTracer(List<Object> objects, List<LightRaySource> raySources)
    {
        _renderThread = new Thread(RenderThread);
        _renderThreadCancellationTokenSource = new CancellationTokenSource();


        Objects = objects;
        RaySources = raySources;
    }


    // TODO: finish multithreading sample rendering

    public void RenderStart()
    {
        _renderThreadCancellationTokenSource = new CancellationTokenSource();
        _renderThread.Start();
    }


    public void RenderRestart()
    {
        RenderStopAndWait();

        _renderThread = new Thread(RenderThread);
        RenderStart();
    }


    public void RenderStopAndWait()
    {
        _renderThreadCancellationTokenSource.Cancel();

        if (_renderThread.ThreadState != ThreadState.Unstarted)
            _renderThread.Join();
    }




    private void RenderThread()
    {
        RenderingStartedEvent?.Invoke(this, EventArgs.Empty);


        var pixels = new Color[SampleResolution.X, SampleResolution.Y];

        for (var i = 0; i < Samples; i++)
        {
            var newPixels = RenderPixels(SampleResolution, SampleViewport);

            for (var y = 0; y < newPixels.GetLength(0); y++)
            for (var x = 0; x < newPixels.GetLength(1); x++)
            {
                ref var pixel = ref pixels[x, y];
                var newPixel = newPixels[x, y];

                pixel = AverageColor(pixel, newPixel);
            }

            var eventArgs = new SampleRenderedEventArgs(new Image(pixels), new Image(newPixels));
            SampleRenderedEvent?.Invoke(this, eventArgs);

            if (_renderThreadCancellationTokenSource.IsCancellationRequested)
                break;
        }


        RenderingFinishedEvent?.Invoke(this, EventArgs.Empty);
    }


    private Color AverageColor(Color left, Color right)
    {
        var average = left;
        average.R = (byte)(((float)average.R + right.R) / 2f);
        average.G = (byte)(((float)average.G + right.G) / 2f);
        average.B = (byte)(((float)average.B + right.B) / 2f);
        average.A = (byte)(((float)average.B + right.B) / 2f);

        return average;
    }




    public Color[,] RenderPixels(Vec2u resolution, Vec2u viewport)
    {
        var pixels = new Color[resolution.X, resolution.Y];
        var intersections = TraceAll();
        var pixelColors = PixelColorsFromIntersections(intersections);

        foreach (var pixelColor in pixelColors)
            RenderPixel(pixels, pixelColor, resolution, viewport);

        return pixels;
    }


    private void RenderPixel(Color[,] pixels, PixelColor pixelColor, Vec2u resolution, Vec2u viewport)
    {
        var roundedPosition = new Vec2f(MathF.Round(pixelColor.Position.X), MathF.Round(pixelColor.Position.Y));
        var normalizedDeviceCoordinate = MapToNormalizedDeviceCoordinate(viewport, roundedPosition);
        var imagePixel = MapNormalizedDeviceCoordinateToPixel(resolution, normalizedDeviceCoordinate);

        if (imagePixel.X < 0 || imagePixel.X >= pixels.GetLength(0) ||
            imagePixel.Y < 0 || imagePixel.Y >= pixels.GetLength(1))
            return;

        pixels[imagePixel.X, imagePixel.Y] = pixelColor.Color;
    }


    private IEnumerable<PixelColor> PixelColorsFromIntersections(IEnumerable<LightRayIntersection> intersections)
        => from intersection in intersections select new PixelColor(intersection.Point, intersection.FinalColor);


    private Vec2i MapNormalizedDeviceCoordinateToPixel(Vec2u imageResolution, Vec2f coordinate)
    {
        var unsignedCoordinate = (coordinate + new Vec2f(1, 1)) / 2;
        return imageResolution * unsignedCoordinate;
    }


    private Vec2f MapToNormalizedDeviceCoordinate(Vec2u viewport, Vec2f coordinate)
        => coordinate / (Vec2f)viewport * 2 - new Vec2f(1, 1);




    public IEnumerable<LightRayIntersection> TraceAll()
    {
        var intersections = new List<LightRayIntersection>();

        GenerateRaysFromSources();

        foreach (var ray in Rays)
            if (Trace(ray) is { } intersectionPoint)
                intersections.Add(intersectionPoint);

        return intersections;
    }


    private LightRayIntersection? Trace(LightRay lightRay)
    {
        var intersections = new List<LightRayIntersection>();

        // TODO: add ray bouncing and light energy loss
        foreach (var @object in Objects)
        foreach (var segment in @object.Segments)
            if (lightRay.IntersectsSegment(segment, out var t, out var u))
                intersections.Add(new LightRayIntersection(lightRay, segment, t, u));

        if (intersections.Count == 0)
            return null;

        intersections = intersections.OrderBy(point => point.RayT).ToList();

        return intersections.First();
    }


    private void GenerateRaysFromSources()
    {
        Rays.Clear();

        foreach (var raySource in RaySources)
            Rays.AddRange(raySource.GenerateRays());
    }
}
