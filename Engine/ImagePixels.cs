using System;
using System.Threading.Tasks;

using Latte.Core.Type;


namespace LightRays.Engine;




// linear image pixel matrix representation
public struct ImagePixels(NormalizedColorRGBA[] pixels, uint width, uint height)
{
    public NormalizedColorRGBA[] Pixels { get; } = pixels;
    public uint Width { get; } = width;
    public uint Height { get; } = height;




    public ImagePixels(uint width, uint height)
        : this(new NormalizedColorRGBA[width * height], width, height)
    {}




    public ref NormalizedColorRGBA this[uint x, uint y]
        => ref Pixels[y * Width + x];




    public byte[] GetBytes()
    {
        var bytes = new byte[Width * Height * 4];
        var _this = this; // cannot use "this" inside lambdas

        Parallel.For(0, Height, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, y =>
        {
            for (var x = 0u; x < _this.Width; x++)
            {
                var baseIndex = (y * _this.Width + x) * 4;
                ref var color = ref _this[x, (uint)y];

                bytes[baseIndex + 0] = (byte)(color.R * 255);
                bytes[baseIndex + 1] = (byte)(color.G * 255);
                bytes[baseIndex + 2] = (byte)(color.B * 255);
                bytes[baseIndex + 3] = (byte)(color.A * 255);
            }
        });


        return bytes;
    }
}
