using System;
using System.Collections.Generic;
using System.Linq;

using Latte.Core.Type;


namespace LightRays.Engine;




public readonly record struct PixelColor(Vec2f Position, NormalizedColorRGBA Color);




public class PathTracer(List<Object> objects, List<LightRaySource> lightSources)
{
    private readonly List<LightRay> _rays = [];


    public List<Object> Objects { get; set; } = objects;
    public List<LightRaySource> LightSources { get; set; } = lightSources;


    public NormalizedColorRGBA[,] RenderPixels(Vec2u resolution, Vec2u viewport)
    {
        var pixels = new NormalizedColorRGBA[resolution.X, resolution.Y];
        var intersections = TraceAll();
        var pixelColors = PixelColorsFromIntersections(intersections);

        foreach (var pixelColor in pixelColors)
            RenderPixel(ref pixels, pixelColor, resolution, viewport);

        return pixels;
    }


    // TODO: remove ref and check if it works
    private void RenderPixel(ref NormalizedColorRGBA[,] pixels, PixelColor pixelColor, Vec2u resolution, Vec2u viewport)
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
