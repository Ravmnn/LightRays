using System;

using Latte.Core.Type;


namespace LightRays.Engine;




public struct LightRay(Vec2f origin, Vec2f direction, NormalizedColorRGBA? color = null, float startEnergy = 1.0f)
{
    public Vec2f Origin { get; set; } = origin;
    public Vec2f Direction { get; set; } = direction;


    public NormalizedColorRGBA RawColor { get; set; } = color ?? SFML.Graphics.Color.White;
    public NormalizedColorRGBA Color => RawColor * new NormalizedColorRGBA(Energy, Energy, Energy);

    public float Energy { get; set; } = startEnergy;




    public Vec2f At(float t)
        => Origin + Direction * t;




    public bool IntersectsSegment(Segment segment, out float t, out float u)
    {
        var segmentVector = segment.Vector;
        var raySegmentVector = segment.Start - Origin;

        var denom = Cross(Direction, segmentVector);

        if (Math.Abs(denom) < 1e-6f)
        {
            t = u = 0;
            return false;
        }

        t = Cross(raySegmentVector, segmentVector) / denom;
        u = Cross(raySegmentVector, Direction) / denom;

        return t >= 0.0f && u is >= 0.0f and <= 1.0f;
    }




    private static float Cross(Vec2f a, Vec2f b)
        => a.X * b.Y - a.Y * b.X;
}
