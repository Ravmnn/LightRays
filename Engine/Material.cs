using Latte.Core.Type;


namespace LightRays.Engine;




public struct Material(NormalizedColorRGBA? color = null, float spreading = 0.0f)
{
    public NormalizedColorRGBA Color { get; set; } = color ?? new NormalizedColorRGBA(0.95f, 0.95f, 0.95f);
    public float Spreading { get; set; } = spreading;
}
