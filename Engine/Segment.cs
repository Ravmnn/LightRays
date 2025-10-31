using System;
using Latte.Core;
using Latte.Core.Type;


namespace LightRays.Engine;




public readonly struct Segment(Object owner, Vec2f start, Vec2f end)
{
    public Object Owner { get; } = owner;

    public Vec2f Start { get; } = start;
    public Vec2f End { get; } = end;

    public Vec2f Vector => End - Start;




    public Vec2f At(float u)
        => Start + Vector * u; // u is normalized, 0 to 1




    public bool ContainsPoint(Vec2f point)
    {
        const double Epsilon = 1e-7;

        var pointStartVector = point - Start;

        var cross = Vector.Cross(pointStartVector);

        if (Math.Abs(cross) > Epsilon)
            return false; // not in the same line

        var dot = Vector.Dot(pointStartVector);

        if (dot < -Epsilon)
            return false; // before Start

        if (dot > Vector.Dot(Vector) + Epsilon)
            return false; // after End

        return true;
    }




    public Vec2f NormalOppositeTo(Vec2f direction)
    {
        var normal = LeftNormal();

        if (direction.Dot(normal) > 0)
            normal = RightNormal();

        return normal;
    }


    public Vec2f NormalSimilarTo(Vec2f direction)
    {
        var normal = LeftNormal();

        if (direction.Dot(normal) < 0)
            normal = RightNormal();

        return normal;
    }


    public Vec2f LeftNormal()
    {
        var t = Vector;
        var normal = new Vec2f(-t.Y, t.X);

        return normal.Normalized();
    }


    public Vec2f RightNormal()
    {
        var t = Vector;
        var normal = new Vec2f(t.Y, -t.X);

        return normal.Normalized();
    }
}
