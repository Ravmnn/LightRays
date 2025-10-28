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


    public bool DebugDrawSegmentLines { get; set; }
    public bool DebugDrawInfo { get; set; }




    public MainSection()
    {
        _mouseLight = new LightRaySource(new Vec2f(), 1024)
        {
            Color = Color.Red
        };
        _useMouseLight = true;


        PathTracer = new PathTracer(
            [new RectangleObject(new Vec2f(1300, 300), new Vec2f(200, 200), Color.White)],
            [_mouseLight, new LightRaySource(new Vec2f(700, 200), 1024, Color.Blue)]
        );

        Sampler = new PathTracerSampler(PathTracer, 32)
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


        if (KeyboardInput.ReleasedKeyCode == Keyboard.Scancode.Space)
            Sampler.RenderingPaused = !Sampler.RenderingPaused;

        if (KeyboardInput.ReleasedKeyCode == Keyboard.Scancode.Enter)
            Sampler.RenderRestart();


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

        DrawPathTracerAccumulatedSample(renderer);

        DrawRaySources(renderer);
        DebugDraw(renderer);

        base.Draw(renderer);
    }


    private void RestartRenderingIfRequested()
    {
        if (!ShouldRestartRendering)
            return;

        Sampler.RenderRestart();

        ShouldRestartRendering = false;
    }


    private void DrawPathTracerAccumulatedSample(IRenderer renderer)
    {
        var texture = new Texture(Sampler.AccumulatedSample);
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
        var sampleDeltaTime = Sampler.TimeSpentToRenderLastSample;
        var info =
            $"""
             Time Spent Rendering Last: {sampleDeltaTime.TotalMilliseconds:N0}ms | {(int)DeltaTime.FPSFromDeltaTime(sampleDeltaTime.TotalSeconds)} FPS
             Current Light Source Rays: {_mouseLight.RayCount}
             Current Sample: {Sampler.CurrentSampleCounter}/{Sampler.Samples} {stateIndicator}
             """;

        Latte.Debugging.Draw.Text(renderer, new Vec2f(), info, 12, Color.White);
    }


    private string GetPathTracerRenderingStateStringIndicator()
    {
        if (Sampler.RenderingPaused)
            return "paused";

        if (Sampler.RenderingFinished)
            return "finished";

        if (Sampler.RenderingCancelled)
            return "cancelled";

        return string.Empty;
    }
}
