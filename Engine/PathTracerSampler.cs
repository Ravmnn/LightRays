using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

using SFML.Graphics;

using Latte.Core.Type;


using ThreadState = System.Threading.ThreadState;


namespace LightRays.Engine;




public class PathTracerSampler
{
    private Thread _renderThread = null!;
    private CancellationTokenSource _renderThreadCancellationTokenSource;
    private readonly ManualResetEvent _renderThreadPauseState;




    public PathTracer PathTracer { get; }


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




    public PathTracerSampler(PathTracer pathTracer, uint samples = 1)
    {
        PathTracer = pathTracer;

        InitRenderingThread();
        _renderThreadCancellationTokenSource = new CancellationTokenSource();
        _renderThreadPauseState = new ManualResetEvent(false);

        SampleResolution = SampleViewport = new Vec2u(1920, 1080);

        Samples = samples;
        CurrentSampleCounter = 0;

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


        var accumulator = new NormalizedColorRGBA[SampleResolution.X, SampleResolution.Y];

        for (var i = 0; i < Samples; i++)
        {
            _renderThreadPauseState.WaitOne();

            if (_renderThreadCancellationTokenSource.IsCancellationRequested)
                break;

            var stopwatch = Stopwatch.StartNew();

            var sample = PathTracer.RenderPixels(SampleResolution, SampleViewport);

            for (var x = 0; x < sample.GetLength(0); x++)
            for (var y = 0; y < sample.GetLength(1); y++)
            {
                ref var pixel = ref accumulator[x, y];
                var newPixel = sample[x, y];

                pixel = AverageColor(pixel, newPixel);
            }

            TimeSpentToRenderLastSample = stopwatch.Elapsed;

            OnSampleRendered(accumulator, sample);
        }


        OnRenderFinish();
    }


    private NormalizedColorRGBA AverageColor(NormalizedColorRGBA average, NormalizedColorRGBA addition)
    {
        if (addition is { R: 0, G: 0, B: 0, A: 0 })
            return average;

        // colorAverage += (newSample - colorAverage) / sampleCount;

        average.R += (addition.R - average.R) / Samples;
        average.G += (addition.G - average.G) / Samples;
        average.B += (addition.B - average.B) / Samples;
        average.A = addition.A;

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


    // TODO: 
    private void OnSampleRendered(NormalizedColorRGBA[,] accumulator, NormalizedColorRGBA[,] sample)
    {
        var width = (uint)accumulator.GetLength(0);
        var height = (uint)accumulator.GetLength(1);

        AccumulatedSample = new Image(width, height, NormalizedColorMatrixToBytes(accumulator));
        CurrentSample = new Image(width, height, NormalizedColorMatrixToBytes(sample));
        CurrentSampleCounter++;

        SampleRenderedEvent?.Invoke(this, EventArgs.Empty);
    }


    private byte[] NormalizedColorMatrixToBytes(NormalizedColorRGBA[,] colors)
    {
        var bytes = new byte[colors.Length * 4];

        var width = (uint)colors.GetLength(0);
        var height  = (uint)colors.GetLength(1);

        for (var x = 0; x < colors.GetLength(0); x++)
        for (var y = 0; y < colors.GetLength(1); y++)
        {
            var baseIndex = (y * width + x) * 4;

            bytes[baseIndex + 0] = (byte)(colors[x, y].R * 255);
            bytes[baseIndex + 1] = (byte)(colors[x, y].G * 255);
            bytes[baseIndex + 2] = (byte)(colors[x, y].B * 255);
            bytes[baseIndex + 3] = (byte)(colors[x, y].A * 255);
        }

        return bytes.ToArray();
    }
}
