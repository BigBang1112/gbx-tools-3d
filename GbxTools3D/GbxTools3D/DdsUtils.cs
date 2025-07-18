using Pfim;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace GbxTools3D;

public static class DdsUtils
{
    public static Image ToImageSharp(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 32768);
        return ToImageSharp(stream);
    }

    public static Image ToImageSharp(Stream stream)
    {
        using var image = Pfimage.FromStream(stream);

        byte[] newData;

        var tightStride = image.Width * image.BitsPerPixel / 8;
        if (image.Stride != tightStride)
        {
            newData = new byte[image.Height * tightStride];
            for (var i = 0; i < image.Height; i++)
            {
                Buffer.BlockCopy(image.Data, i * image.Stride, newData, i * tightStride, tightStride);
            }
        }
        else
        {
            newData = image.Data;
        }

        switch (image.Format)
        {
            case ImageFormat.Rgba32:
                return Image.LoadPixelData<Bgra32>(newData, image.Width, image.Height);
            case ImageFormat.Rgb24:
                return Image.LoadPixelData<Bgr24>(newData, image.Width, image.Height);
            case ImageFormat.Rgba16:
                return Image.LoadPixelData<Bgra4444>(newData, image.Width, image.Height);
            case ImageFormat.R5g5b5:
                for (var i = 1; i < newData.Length; i += 2)
                {
                    newData[i] |= 128;
                }
                return Image.LoadPixelData<Bgra5551>(newData, image.Width, image.Height);
            case ImageFormat.R5g5b5a1:
                return Image.LoadPixelData<Bgra5551>(newData, image.Width, image.Height);
            case ImageFormat.R5g6b5:
                return Image.LoadPixelData<Bgr565>(newData, image.Width, image.Height);
            case ImageFormat.Rgb8:
                return Image.LoadPixelData<L8>(newData, image.Width, image.Height);
            default:
                throw new Exception($"ImageSharp does not recognize image format: {image.Format}");
        }
    }
}
