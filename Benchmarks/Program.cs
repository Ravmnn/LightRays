using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

using Latte.Core.Type;

using LightRays.Engine;


namespace LightRays.Benchmarks;




public class PathTracerBenchmarks
{
    private readonly PathTracer _pathTracer = new PathTracer(
        [new RectangleObject(new Vec2f(500, 300), new Vec2f(200, 200))],
        [new LightRaySource(new Vec2f(10, 10), 8192)]
    );


    private readonly ParallelOptions _parallelOptions = new ParallelOptions
    {
        MaxDegreeOfParallelism = Environment.ProcessorCount
    };




    [Benchmark]
    public void TraceAll()
    {
        var rays = new List<LightRay>();
        var intersections = new List<LightRayIntersection>(rays.Count / 2);

        foreach (var raySource in _pathTracer.LightSources)
            rays.AddRange(raySource.GenerateRays());

        foreach (var ray in rays)
            if (_pathTracer.Trace(ray) is { } intersectionPoint)
                intersections.Add(intersectionPoint);
    }


    [Benchmark]
    public void ParallelTraceAll()
    {
        var rays = new List<LightRay>();
        var intersections = new ConcurrentBag<LightRayIntersection>();

        foreach (var raySource in _pathTracer.LightSources)
            rays.AddRange(raySource.GenerateRays());

        Parallel.ForEach(rays, new ParallelOptions(), ray =>
        {
            if (_pathTracer.Trace(ray) is { } intersectionPoint)
                intersections.Add(intersectionPoint);
        });
    }
}




public static class Program
{
    public static void Main()
        => BenchmarkRunner.Run<PathTracerBenchmarks>();
}
