using System;

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
    private bool _useMouseLight;


    public Vec2u Resolution => new Vec2u(16 * 120, 9 * 120) / 2;
    public Vec2u Viewport => new Vec2u(16 * 120, 9 * 120);

    public Vec2f Scale => (Vec2f)Viewport / (Vec2f)Resolution;


    public PathTracerSampler Sampler { get; set; }
    public PathTracer PathTracer { get; set; }
    public bool ShouldRestartRendering { get; set; }

    public bool Paused { get; set; }


    public bool DebugDrawSegmentLines { get; set; }
    public bool DebugDrawInfo { get; set; }




    public MainSection()
    {
        _mouseLight = new LightRaySource(new Vec2f(), 1024)
        {
            Color = Color.White
        };
        _useMouseLight = true;


        PathTracer = new PathTracer([], [_mouseLight]);

        var generator = new Random();
        for (var i = 0; i < 100; i++)
        {
            var position = new Vec2f(generator.Next(0, (int)Viewport.X), generator.Next(0, (int)Viewport.Y));
            var size = new Vec2f(generator.Next(20, 200), generator.Next(20, 200));

            PathTracer.Objects.Add(new RectangleObject(position, size));
        }


        Sampler = new PathTracerSampler(PathTracer, 1024)
        {
            SampleResolution = Resolution,
            SampleViewport = Viewport
        };


        ShouldRestartRendering = true;


        DebugDrawSegmentLines = false;
        DebugDrawInfo = true;


        MouseInput.ButtonUpEvent += ProcessMouseInput;
    }




    public override void Update()
    {
        _mouseLight.Position = MouseInput.PositionInView;

        if (_useMouseLight && MouseInput.MouseMoved)
            ShouldRestartRendering = true;

        ProcessKeyInput();

        base.Update();
    }


    private void ProcessMouseInput(object? _, MouseButtonEventArgs args)
    {
        if (_useMouseLight && args.Button == Mouse.Button.Left)
            PathTracer.LightSources.Add(new LightRaySource(_mouseLight.Position, _mouseLight.RayCount, _mouseLight.Color));
    }


    private void ProcessKeyInput()
    {
        if (KeyboardInput.ReleasedKeyCode == Keyboard.Scancode.Num1)
            DebugDrawSegmentLines = !DebugDrawSegmentLines;

        if (KeyboardInput.ReleasedKeyCode == Keyboard.Scancode.Num2)
            DebugDrawInfo = !DebugDrawInfo;


        if (KeyboardInput.ReleasedKeyCode == Keyboard.Scancode.NumpadPlus)
            _mouseLight.RayCount *= 2;

        if (KeyboardInput.ReleasedKeyCode == Keyboard.Scancode.NumpadMinus)
            _mouseLight.RayCount /= 2;


        if (KeyboardInput.ReleasedKeyCode == Keyboard.Scancode.Enter)
            Sampler.ResetRender();


        if (KeyboardInput.ReleasedKeyCode == Keyboard.Scancode.Q)
        {
            _useMouseLight = !_useMouseLight;
            ShouldRestartRendering = true;

            if (_useMouseLight)
                PathTracer.LightSources.Add(_mouseLight);
            else
                PathTracer.LightSources.Remove(_mouseLight);
        }
    }




    public override void Draw(IRenderer renderer)
    {
        RestartRenderingIfRequested();

        Sampler.RenderNext();
        DrawPathTracerAccumulatedSample(renderer);

        DrawRaySources(renderer);
        DebugDraw(renderer);

        base.Draw(renderer);
    }


    private void RestartRenderingIfRequested()
    {
        if (!ShouldRestartRendering)
            return;

        Sampler.ResetRender();

        ShouldRestartRendering = false;
    }


    private void DrawPathTracerAccumulatedSample(IRenderer renderer)
    {
        var texture = new Texture(Sampler.Rendering);
        var sprite = new Sprite(texture) { Scale = Scale };

        renderer.Render(sprite);
    }


    private void DrawRaySources(IRenderer renderer)
    {
        foreach (var raySource in PathTracer.LightSources)
            Latte.Debugging.Draw.Point(renderer, raySource.Position);
    }


    private void DebugDraw(IRenderer renderer)
    {
        if (DebugDrawSegmentLines)
            DebugSegments(renderer);

        if (DebugDrawInfo)
            DebugInfo(renderer);
    }


    private void DebugSegments(IRenderer renderer)
    {
        foreach (var @object in PathTracer.Objects)
            foreach (var segment in @object.Segments)
                Latte.Debugging.Draw.Line(renderer, segment.Start, segment.End, Color.White);
    }


    private void DebugInfo(IRenderer renderer)
    {
        var stateIndicator = GetPathTracerRenderingStateStringIndicator();
        var info =
            $"""
            Time Spent Rendering Last: {Sampler.TimeSpent.TotalMilliseconds:N0}ms | {(int)DeltaTime.FPSFromDeltaTime(Sampler.TimeSpent.TotalSeconds)} FPS
            - Tracing: {Sampler.TimeSpentTracing.TotalMilliseconds:N0}ms
            - Averaging: {Sampler.TimeSpentAveraging.TotalMilliseconds:N0}ms
            - Creating Image: {Sampler.TimeSpentCreatingImage.TotalMilliseconds:N0}ms

            Current Light Source Rays: {_mouseLight.RayCount}
            Current Sample: {Sampler.CurrentSampleCounter}/{Sampler.Samples} {stateIndicator}
            """;

        Latte.Debugging.Draw.Text(renderer, new Vec2f(), info, 12, Color.White);
    }


    private string GetPathTracerRenderingStateStringIndicator()
    {
        if (Paused)
            return "paused";

        if (Sampler.RenderingFinished)
            return "finished";

        return string.Empty;
    }
}
