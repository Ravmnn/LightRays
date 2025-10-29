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
