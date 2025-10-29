using System;

using Latte.Core;
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




    // TODO: make reflections work
    public void Reflect(Vec2f normal)
    {
        var newDirection = Direction - normal * (2 * Direction.Dot(normal));

        Direction = newDirection;
    }




    public bool IntersectsSegment(Segment segment, out float t, out float u)
    {
        var segmentVector = segment.Vector;
        var raySegmentVector = segment.Start - Origin;

        var denom = Direction.Cross(segmentVector);

        if (Math.Abs(denom) < 1e-6f)
        {
            t = u = 0;
            return false;
        }

        t = Vector.Cross(raySegmentVector, segmentVector) / denom;
        u = Vector.Cross(raySegmentVector, Direction) / denom;

        return t >= 0.0f && u is >= 0.0f and <= 1.0f;
    }
}
