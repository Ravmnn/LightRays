using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;



using Latte.Core.Type;


namespace LightRays.Engine;




public readonly record struct PixelColor(Vec2f Position, NormalizedColorRGBA Color);




public class PathTracer(List<Object> objects, List<LightRaySource> lightSources)
{
    private readonly ParallelOptions _parallelOptions = new ParallelOptions
    {

#if DEBUG
        MaxDegreeOfParallelism = 1 // it's difficult to debug methods that uses parallelism
#else
        MaxDegreeOfParallelism = Environment.ProcessorCount
#endif

    };


    private readonly List<LightRay> _rays = [];


    public List<Object> Objects { get; set; } = objects;
    public List<LightRaySource> LightSources { get; set; } = lightSources;

    public uint Bounces { get; set; }




    public ImagePixels RenderPixels(Vec2u resolution, Vec2u viewport)
    {
        var pixels = new ImagePixels(resolution.X, resolution.Y);
        var intersections = TraceAll();
        var pixelColors = PixelColorsFromIntersections(intersections);

        Parallel.ForEach(pixelColors, _parallelOptions, pixelColor =>
        {
            RenderPixel(pixels, pixelColor, resolution, viewport);
        });

        return pixels;
    }


    private void RenderPixel(ImagePixels pixels, PixelColor intersection, Vec2u resolution, Vec2u viewport)
    {
        var roundedPosition = new Vec2f(MathF.Round(intersection.Position.X), MathF.Round(intersection.Position.Y));
        var normalizedDeviceCoordinate = MapToNormalizedDeviceCoordinate(viewport, roundedPosition);
        var imagePixel = MapNormalizedDeviceCoordinateToPixel(resolution, normalizedDeviceCoordinate);

        if (imagePixel.X < 0 || imagePixel.X >= pixels.Width ||
            imagePixel.Y < 0 || imagePixel.Y >= pixels.Height)
            return;

        pixels[(uint)imagePixel.X, (uint)imagePixel.Y] = intersection.Color;
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
        var intersections = new ConcurrentBag<LightRayIntersection>();

        GenerateRaysFromSources();

        Parallel.ForEach(_rays, _parallelOptions, ray =>
        {
            foreach (var intersection in Trace(ray))
                intersections.Add(intersection);
        });

        return intersections;
    }


    private void GenerateRaysFromSources()
    {
        _rays.Clear();

        foreach (var raySource in LightSources)
            _rays.AddRange(raySource.GenerateRays());
    }




    public IEnumerable<LightRayIntersection> Trace(LightRay lightRay)
    {
        var bounceIntersections = new List<LightRayIntersection>();
        var ray = lightRay;

        for (var i = 0; i <= Bounces; i++)
        {
            if (GetFirstLightRayIntersection(ray) is not { } intersection)
            {
                // first ray not intersecting means no bouncing at all
                if (i == 0)
                    break;

                continue;
            }

            bounceIntersections.Add(intersection);

            if (i + 1 > Bounces)
                continue;

            // TODO: don't forget about Material.Spreading

            // TODO: you can remove this after Latte start to use Vec2s as structs:
            ray = new LightRay(ray.Origin.Copy(), ray.Direction.Copy());
            ray.Reflect(intersection);
        }

        return bounceIntersections;
    }


    private LightRayIntersection? GetFirstLightRayIntersection(LightRay lightRay)
    {
        var intersectionsBag = new ConcurrentBag<LightRayIntersection>();

        Parallel.ForEach(Objects, _parallelOptions, @object =>
        {
            foreach (var segment in @object.Segments)
                if (lightRay.IntersectsSegment(segment, out var t, out var u))
                    intersectionsBag.Add(new LightRayIntersection(lightRay, segment, t, u));
        });

        if (intersectionsBag.IsEmpty)
            return null;

        var intersections = intersectionsBag.OrderBy(point => point.RayT);

        return intersections.First();
    }
}
