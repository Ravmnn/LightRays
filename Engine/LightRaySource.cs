using System;
using System.Collections.Generic;

using Latte.Core.Type;


namespace LightRays.Engine;




public class LightRaySource(Vec2f position, int rayCount, NormalizedColorRGBA? color = null)
{
    public Vec2f Position { get; set; } = position;
    public NormalizedColorRGBA Color { get; set; } = color ?? SFML.Graphics.Color.White;
    public int RayCount { get; set; } = rayCount;




    public IEnumerable<LightRay> GenerateRays()
    {
        var rays = new List<LightRay>();

        for (var i = 0; i < RayCount; i++)
            rays.Add(new LightRay(Position, RandomDirection(), Color));

        return rays;
    }


    private static Vec2f RandomDirection()
    {
        var generator = new Random();
        var direction = new Vec2f(generator.NextSingle(), generator.NextSingle());

        return direction * 2 - new Vec2f(1, 1);
    }
}
