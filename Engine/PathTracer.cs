using System;
using System.Collections.Generic;
using System.Linq;

using SFML.Graphics;

using Latte.Core.Type;



namespace LightRays.Engine;




public readonly record struct PixelColor(Vec2f Position, ColorRGBA Color);




public class PathTracer()
{
    public List<Object> Objects { get; set; } = [];
    public List<LightRaySource> RaySources { get; set; } = [];

    public List<LightRay> Rays { get; set; } = [];




    public PathTracer(List<Object> objects, List<LightRaySource> raySources) : this()
    {
        Objects = objects;
        RaySources = raySources;
    }




    public Image Render(Vec2u resolution, Vec2u viewport)
    {
        var pixels = new Color[resolution.X, resolution.Y];
        var intersections = TraceAll();
        var pixelColors = PixelColorsFromIntersections(intersections);

        foreach (var pixelColor in pixelColors)
            RenderPixel(pixels, pixelColor, resolution, viewport);

        return new Image(pixels);
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
