using System;

using Latte.Core;
using Latte.Core.Type;


namespace LightRays.Engine;




public struct LightRay(Vec2f origin, Vec2f direction, NormalizedColorRGBA? color = null)
{
    public Vec2f Origin { get; set; } = origin;
    public Vec2f Direction { get; set; } = direction;


    public NormalizedColorRGBA Color { get; set; } = color ?? SFML.Graphics.Color.White;




    public Vec2f At(float t)
        => Origin + Direction * t;




    public void Reflect(LightRayIntersection intersection)
    {
        var segment = intersection.Segment;
        var normal = segment.NormalOppositeTo(Direction);

        var originDisplacement = normal * 1e-4f;

        Origin = intersection.Point + originDisplacement;
        Direction -= normal * (float)(2 * Direction.Dot(normal));

        Color = intersection.FinalColor;
    }




    public bool IntersectsSegment(Segment segment, out float t, out float u)
    {
        const double Epsilon = 1e-7;

        var segmentVector = segment.Vector;
        var raySegmentVector = segment.Start - Origin;

        var denom = Direction.Cross(segmentVector);

        if (Math.Abs(denom) < Epsilon)
        {
            t = u = 0;
            return false;
        }

        t = (float)(Vector.Cross(raySegmentVector, segmentVector) / denom);
        u = (float)(Vector.Cross(raySegmentVector, Direction) / denom);

        return t >= 0.0f && u is >= 0.0f and <= 1.0f;
    }
}
