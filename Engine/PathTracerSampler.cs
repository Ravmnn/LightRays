using System;
using System.Diagnostics;
using System.Threading.Tasks;

using SFML.Graphics;

using Latte.Core.Type;


namespace LightRays.Engine;




public class PathTracerSampler
{
    public PathTracer PathTracer { get; }


    public Vec2u SampleResolution { get; set; }
    public Vec2u SampleViewport { get; set; }

    public uint Samples { get; set; }
    public uint CurrentSampleCounter { get; private set; }
    public float SampleWeightDistribution { get; set; }


    private ImagePixels _sampleAccumulator;
    public Image Rendering { get; private set; } = null!;


    public TimeSpan TimeSpent { get; private set; }
    public TimeSpan TimeSpentTracing { get; private set; }
    public TimeSpan TimeSpentAveraging { get; private set; }
    public TimeSpan TimeSpentCreatingImage { get; private set; }


    public bool RenderingFinished => CurrentSampleCounter >= Samples;


    public event EventHandler? SampleRenderedEvent;




    public PathTracerSampler(PathTracer pathTracer, uint samples = 1)
    {
        PathTracer = pathTracer;

        SampleResolution = SampleViewport = new Vec2u(1920, 1080);
        Samples = samples;

        SampleWeightDistribution = 6f;

        ResetRender();
    }


    private void InitSampleImageProperties()
        => Rendering = new Image(SampleResolution.X, SampleResolution.Y);




    public void ResetRender()
    {
        _sampleAccumulator = new ImagePixels(SampleResolution.X, SampleResolution.Y);

        InitSampleImageProperties();
        CurrentSampleCounter = 1;
    }


    public void RenderNext()
    {
        if (RenderingFinished)
            return;

        var start = Stopwatch.GetTimestamp();
        var sample = PathTracer.RenderPixels(SampleResolution, SampleViewport);
        TimeSpentTracing = Stopwatch.GetElapsedTime(start);

        start = Stopwatch.GetTimestamp();
        AccumulateSample(_sampleAccumulator, sample);
        TimeSpentAveraging = Stopwatch.GetElapsedTime(start);

        start = Stopwatch.GetTimestamp();
        OnSampleRendered();
        TimeSpentCreatingImage = Stopwatch.GetElapsedTime(start);

        TimeSpent = TimeSpentTracing + TimeSpentAveraging + TimeSpentCreatingImage;
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

        average.R += (addition.R - average.R) / (CurrentSampleCounter / SampleWeightDistribution);
        average.G += (addition.G - average.G) / (CurrentSampleCounter / SampleWeightDistribution);
        average.B += (addition.B - average.B) / (CurrentSampleCounter / SampleWeightDistribution);
        average.A = addition.A;

        return average;
    }




    private void OnSampleRendered()
    {
        var bytes = _sampleAccumulator.GetBytes();

        Rendering = new Image(_sampleAccumulator.Width, _sampleAccumulator.Height, bytes);
        CurrentSampleCounter++;

        SampleRenderedEvent?.Invoke(this, EventArgs.Empty);
    }
}
