using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Avalonia;
using Avalonia.Platform;
using Clowd.Clipboard;
using Bitmap = Avalonia.Media.Imaging.Bitmap;

namespace FortnitePorting.Models.Clipboard;

public class AvaloniaClipboardHandle : ClipboardHandleGdiBase, IClipboardHandlePlatform<Bitmap>
{
    public virtual Bitmap GetImage()
    {
        using var imageImpl = GetImageImpl();
        if (imageImpl == null)
            return null;
        
        var bitmapdata = imageImpl.LockBits(new Rectangle(0, 0, imageImpl.Width, imageImpl.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        var image = new Bitmap(PixelFormats.Bgra8888, AlphaFormat.Unpremul, bitmapdata.Scan0, new PixelSize(bitmapdata.Width, bitmapdata.Height), new Vector(imageImpl.HorizontalResolution, imageImpl.VerticalResolution), bitmapdata.Stride);
        imageImpl.UnlockBits(bitmapdata);
        return image;
    }

    public virtual void SetImage(Bitmap bitmap)
    {
        using MemoryStream memoryStream = new MemoryStream();
        bitmap.Save(memoryStream);

        using System.Drawing.Bitmap outBitmap = new System.Drawing.Bitmap(memoryStream);
        SetImageImpl(outBitmap);
    }
}