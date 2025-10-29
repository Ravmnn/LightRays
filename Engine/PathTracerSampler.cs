using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

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
    public uint CurrentSampleCounter { get; private set; }


    public TimeSpan TimeSpent { get; private set; }
    public TimeSpan TimeSpentTracing { get; private set; }
    public TimeSpan TimeSpentAveraging { get; private set; }
    public TimeSpan TimeSpentCreatingImage { get; private set; }


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
        => AccumulatedSample = new Image(SampleResolution.X, SampleResolution.Y);




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


        var accumulator = new ImagePixels(SampleResolution.X, SampleResolution.Y);

        for (var i = 0; i < Samples; i++)
        {
            _renderThreadPauseState.WaitOne();

            if (_renderThreadCancellationTokenSource.IsCancellationRequested)
                break;


            var start = Stopwatch.GetTimestamp();
            var sample = PathTracer.RenderPixels(SampleResolution, SampleViewport);
            TimeSpentTracing = Stopwatch.GetElapsedTime(start);

            start = Stopwatch.GetTimestamp();
            AccumulateSample(accumulator, sample);
            TimeSpentAveraging = Stopwatch.GetElapsedTime(start);

            start = Stopwatch.GetTimestamp();
            OnSampleRendered(accumulator);
            TimeSpentCreatingImage = Stopwatch.GetElapsedTime(start);

            TimeSpent = TimeSpentTracing + TimeSpentAveraging + TimeSpentCreatingImage;
        }


        OnRenderFinish();
    }


    private void AccumulateSample(ImagePixels accumulator, ImagePixels sample)
    {
        Parallel.For(0, sample.Height, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, y =>
        {
            for (var x = 0u; x < sample.Width; x++)
            {
                ref var pixel = ref accumulator[x, (uint)y];
                var newPixel = sample[x, (uint)y];

                pixel = AverageColor(pixel, newPixel);
            }
        });
    }


    private NormalizedColorRGBA AverageColor(NormalizedColorRGBA average, NormalizedColorRGBA addition)
    {
        if (addition is { R: 0, G: 0, B: 0, A: 0 })
            return average;

        // colorAverage += (newSample - colorAverage) / sampleCount;

        average.R += (addition.R - average.R) / CurrentSampleCounter;
        average.G += (addition.G - average.G) / CurrentSampleCounter;
        average.B += (addition.B - average.B) / CurrentSampleCounter;
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


    private void OnSampleRendered(ImagePixels accumulator)
    {
        var bytes = accumulator.GetBytes();

        AccumulatedSample = new Image(accumulator.Width, accumulator.Height, bytes);
        CurrentSampleCounter++;

        SampleRenderedEvent?.Invoke(this, EventArgs.Empty);
    }
}
