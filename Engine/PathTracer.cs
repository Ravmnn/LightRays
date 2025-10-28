using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

using SFML.Graphics;

using Latte.Core.Type;


using ThreadState = System.Threading.ThreadState;


namespace LightRays.Engine;




public readonly record struct PixelColor(Vec2f Position, ColorRGBA Color);




public class PathTracer
{
    private Thread _renderThread = null!;
    private CancellationTokenSource _renderThreadCancellationTokenSource;
    private readonly ManualResetEvent _renderThreadPauseState;


    private readonly List<LightRay> _rays;




    public List<Object> Objects { get; set; }
    public List<LightRaySource> LightSources { get; set; }



    public Vec2u SampleResolution { get; set; }
    public Vec2u SampleViewport { get; set; }

    public uint Samples { get; set; }
    public Image AccumulatedSample { get; private set; } = null!;
    public Image CurrentSample { get; private set; } = null!;
    public uint CurrentSampleCounter { get; private set; }
    public TimeSpan TimeSpentToRenderLastSample { get; private set; }

    public bool RenderingStarted => _renderThread.ThreadState.HasFlag(ThreadState.Running);
    public bool RenderingFinished => _renderThread.ThreadState.HasFlag(ThreadState.Stopped);
    public bool RenderingCancelled => _renderThreadCancellationTokenSource.IsCancellationRequested;

    public bool RenderingPaused
    {
        get => !_renderThreadPauseState.WaitOne(0);
        set
        {
            if (value)
                _renderThreadPauseState.Reset();
            else
                _renderThreadPauseState.Set();
        }
    }


    public event EventHandler? RenderingStartedEvent;
    public event EventHandler? RenderingFinishedEvent;
    public event EventHandler? SampleRenderedEvent;




    public PathTracer(List<Object> objects, List<LightRaySource> lightSources)
    {
        InitRenderingThread();
        _renderThreadCancellationTokenSource = new CancellationTokenSource();
        _renderThreadPauseState = new ManualResetEvent(false);


        Objects = objects;
        LightSources = lightSources;
        _rays = [];

        SampleResolution = SampleViewport = new Vec2u(1920, 1080);

        Samples = 1;
        CurrentSampleCounter = 1;

        InitSampleImageProperties();
    }


    private void InitRenderingThread()
        => _renderThread = new Thread(RenderThread)
        {
            IsBackground = true,
            Priority = ThreadPriority.Highest
        };


    private void InitSampleImageProperties()
        => AccumulatedSample = CurrentSample = new Image(SampleResolution.X, SampleResolution.Y);


    public void RenderStart()
    {
        _renderThreadCancellationTokenSource = new CancellationTokenSource();
        _renderThread.Start();
    }


    public void RenderRestart()
    {
        RenderCancelAndWaitFinish();

        InitRenderingThread();
        RenderStart();
    }


    public void RenderCancelAndWaitFinish()
    {
        RenderingPaused = false;

        _renderThreadCancellationTokenSource.Cancel();

        if (!_renderThread.ThreadState.HasFlag(ThreadState.Unstarted))
            _renderThread.Join();
    }




    private void RenderThread()
    {
        OnRenderStart();


        var accumulator = new Color[SampleResolution.X, SampleResolution.Y];

        for (var i = 0; i < Samples; i++)
        {
            _renderThreadPauseState.WaitOne();

            if (_renderThreadCancellationTokenSource.IsCancellationRequested)
                break;

            var stopwatch = Stopwatch.StartNew();

            RenderPixels(out var sample, SampleResolution, SampleViewport);

            for (var x = 0; x < sample.GetLength(0); x++)
            for (var y = 0; y < sample.GetLength(1); y++)
            {
                ref var pixel = ref accumulator[x, y];
                ref var newPixel = ref sample[x, y];

                pixel = AverageColor(ref pixel, ref newPixel);
            }

            TimeSpentToRenderLastSample = stopwatch.Elapsed;

            OnSampleRendered(ref accumulator, ref sample);
        }


        OnRenderFinish();
    }


    private static Color AverageColor(ref Color left, ref Color right)
    {
        if (right is { R: 0, G: 0, B: 0, A: 0 })
            return left;

        var average = left;
        average.R = (byte)(((float)average.R + right.R) / 2f);
        average.G = (byte)(((float)average.G + right.G) / 2f);
        average.B = (byte)(((float)average.B + right.B) / 2f);
        average.A = right.A;//(byte)(((float)average.A + right.A) / 2f);

        return average;
    }


    private void OnRenderStart()
    {
        InitSampleImageProperties();
        CurrentSampleCounter = 1;

        RenderingStartedEvent?.Invoke(this, EventArgs.Empty);
    }


    private void OnRenderFinish()
        => RenderingFinishedEvent?.Invoke(this, EventArgs.Empty);


    private void OnSampleRendered(ref Color[,] accumulator, ref Color[,] sample)
    {
        AccumulatedSample = new Image(accumulator);
        CurrentSample = new Image(sample);
        CurrentSampleCounter++;

        SampleRenderedEvent?.Invoke(this, EventArgs.Empty);
    }




    public void RenderPixels(out Color[,] pixels, Vec2u resolution, Vec2u viewport)
    {
        pixels = new Color[resolution.X, resolution.Y];
        var intersections = TraceAll();
        var pixelColors = PixelColorsFromIntersections(intersections);

        foreach (var pixelColor in pixelColors)
            RenderPixel(ref pixels, pixelColor, resolution, viewport);
    }


    private void RenderPixel(ref Color[,] pixels, PixelColor pixelColor, Vec2u resolution, Vec2u viewport)
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

        foreach (var ray in _rays)
            if (Trace(ray) is { } intersectionPoint)
                intersections.Add(intersectionPoint);

        _rays.Clear();

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
        _rays.Clear();

        foreach (var raySource in LightSources)
            _rays.AddRange(raySource.GenerateRays());
    }
}
