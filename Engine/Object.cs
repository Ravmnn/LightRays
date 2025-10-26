namespace LightRays.Engine;




public abstract class Object(Material material)
{
    public Segment[] Segments { get; protected set; } = [];
    public Material Material { get; set; } = material;
}
