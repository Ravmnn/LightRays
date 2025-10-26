using SFML.Window;
using SFML.Graphics;

using Latte.Core.Type;
using Latte.Rendering;
using Latte.Application;

using LightRays.Engine;


using MouseButtonEventArgs = Latte.Application.MouseButtonEventArgs;


namespace LightRays.Editor;




public sealed class MainSection : Section
{
    private readonly LightRaySource _mouseLight;


    public Vec2u Resolution => new Vec2u(16 * 120, 9 * 120) / 2;
    public Vec2u Viewport => new Vec2u(16 * 120, 9 * 120);

    public Vec2f Scale => (Vec2f)Viewport / (Vec2f)Resolution;


    public PathTracer PathTracer { get; set; }


    public bool DebugDrawRayLines { get; set; }
    public bool DebugDrawSegmentLines { get; set; }
    public bool DebugDrawInfo { get; set; }




    public MainSection()
    {
        _mouseLight = new LightRaySource(new Vec2f(), 512);


        PathTracer = new PathTracer(
            [new RectangleObject(new Vec2f(1300, 300), new Vec2f(200, 200), Color.Green)],
            [_mouseLight]
        );


        DebugDrawRayLines = false;
        DebugDrawSegmentLines = false;
        DebugDrawInfo = true;


        MouseInput.ButtonUpEvent += ProcessMouseInput;
    }




    public override void Update()
    {
        _mouseLight.Position = MouseInput.PositionInView;

        ProcessKeyInput();

        base.Update();
    }


    private void ProcessMouseInput(object? _, MouseButtonEventArgs args)
    {
        if (args.Button == Mouse.Button.Left)
            PathTracer.RaySources.Add(new LightRaySource(_mouseLight.Position, _mouseLight.RayCount));
    }


    private void ProcessKeyInput()
    {
        if (KeyboardInput.ReleasedKeyCode == Keyboard.Scancode.Num1)
            DebugDrawRayLines = !DebugDrawRayLines;

        if (KeyboardInput.ReleasedKeyCode == Keyboard.Scancode.Num2)
            DebugDrawSegmentLines = !DebugDrawSegmentLines;

        if (KeyboardInput.ReleasedKeyCode == Keyboard.Scancode.Num3)
            DebugDrawInfo = !DebugDrawInfo;


        if (KeyboardInput.ReleasedKeyCode == Keyboard.Scancode.NumpadPlus)
            _mouseLight.RayCount *= 2;

        if (KeyboardInput.ReleasedKeyCode == Keyboard.Scancode.NumpadMinus)
            _mouseLight.RayCount /= 2;
    }




    public override void Draw(IRenderer renderer)
    {
        var scene = PathTracer.Render(Resolution, Viewport);
        var sprite = new Sprite(new Texture(scene));
        sprite.Scale = Scale;

        renderer.Render(sprite);


        DrawRaySources(renderer);
        DebugDraw(renderer);

        base.Draw(renderer);
    }


    private void DrawRaySources(IRenderer renderer)
    {
        foreach (var raySource in PathTracer.RaySources)
            Latte.Debugging.Draw.Point(renderer, raySource.Position);
    }


    private void DebugDraw(IRenderer renderer)
    {
        if (DebugDrawRayLines)
            DebugRayLines(renderer);

        if (DebugDrawSegmentLines)
            DebugSegments(renderer);

        if (DebugDrawInfo)
            DebugInfo(renderer);
    }


    private void DebugRayLines(IRenderer renderer)
    {
        var intersectionPoints = PathTracer.TraceAll();

        foreach (var ray in PathTracer.Rays)
            Latte.Debugging.Draw.Line(renderer, ray.Origin, ray.At(10000), Color.Red);

        foreach (var intersectionPoint in intersectionPoints)
            Latte.Debugging.Draw.Line(renderer, intersectionPoint.LightRay.Origin, intersectionPoint.Point, Color.Blue);
    }


    private void DebugSegments(IRenderer renderer)
    {
        foreach (var @object in PathTracer.Objects)
            foreach (var segment in @object.Segments)
                Latte.Debugging.Draw.Line(renderer, segment.Start, segment.End, Color.White);
    }


    private void DebugInfo(IRenderer renderer)
    {
        var info = $"FPS: {(int)DeltaTime.FramesPerSecond}\n" +
                   $"Current Rays: {_mouseLight.RayCount}";

        Latte.Debugging.Draw.Text(renderer, new Vec2f(), info, 12, Color.White);
    }
}
