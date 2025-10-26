using Latte.Core.Type;


namespace LightRays.Engine;




public readonly struct LightRayIntersection(LightRay lightRay, Segment segment, float t, float u)
{
    public LightRay LightRay { get; init; } = lightRay;
    public Segment Segment { get; init; } = segment;
    public Object Object => Segment.Owner;

    public float RayT { get; init; } = t;
    public float SegmentU { get; init; } = u;

    public Vec2f Point => LightRay.At(RayT);


    public NormalizedColorRGBA FinalColor => LightRay.Color * Object.Material.Color;
}
