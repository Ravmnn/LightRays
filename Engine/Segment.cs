using Latte.Core.Type;


namespace LightRays.Engine;




public readonly struct Segment(Object owner, Vec2f start, Vec2f end)
{
    public Object Owner { get; } = owner;

    public Vec2f Start { get; } = start;
    public Vec2f End { get; } = end;

    public Vec2f Vector => End - Start;




    public Vec2f At(float u)
        => Start + (End - Start) * u; // u is normalized, 0 to 1
}
