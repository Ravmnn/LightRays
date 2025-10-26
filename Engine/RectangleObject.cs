using SFML.Graphics;

using Latte.Core.Type;


namespace LightRays.Engine;




public class RectangleObject : Object
{
    public RectangleObject(Vec2f position, Vec2f size, NormalizedColorRGBA? color = null)
        : base(new Material(color ?? Color.White))
    {
        Segments = [
            new Segment(this, position, position + new Vec2f(size.X, 0)),
            new Segment(this, position + new Vec2f(size.X, 0), position + size),
            new Segment(this, position + size, position + new Vec2f(0, size.Y)),
            new Segment(this, position + new Vec2f(0, size.Y), position)
        ];
    }
}
