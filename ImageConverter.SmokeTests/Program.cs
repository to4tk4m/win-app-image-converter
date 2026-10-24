using System.Drawing;
using System.Drawing.Imaging;
using ImageConverter;
using ImageMagick;

var directory = Path.Combine(Path.GetTempPath(), "ImageConverter-tests-" + Guid.NewGuid());
Directory.CreateDirectory(directory);
try
{
    var pngPath = Path.Combine(directory, "sample.png");
    using (var bitmap = new Bitmap(20, 12))
    {
        bitmap.SetPixel(10, 6, Color.FromArgb(128, 255, 0, 0));
        bitmap.Save(pngPath, ImageFormat.Png);
    }

    var original = File.ReadAllBytes(pngPath);
    Check(ImageConversion.Convert(pngPath, OutputFormat.Png).Skipped, "same PNG is skipped");
    Check(Directory.GetFiles(directory).Length == 1, "skip creates no files");

    var jpeg = ImageConversion.Convert(pngPath, OutputFormat.Jpeg);
    Check(jpeg.OutputPath == Path.Combine(directory, "sample.jpg"), "JPEG saved beside input");
    using (var image = new Bitmap(jpeg.OutputPath!))
    {
        Check(image.RawFormat.Guid == ImageFormat.Jpeg.Guid, "output is JPEG");
        Check(image.Width == 20 && image.Height == 12, "dimensions preserved");
        var pixel = image.GetPixel(0, 0);
        Check(pixel.R > 240 && pixel.G > 240 && pixel.B > 240, "transparency becomes white");
    }
    Check(ImageConversion.Convert(jpeg.OutputPath!, OutputFormat.Jpeg).Skipped, "same JPEG is skipped");
    var jpegBytes = File.ReadAllBytes(jpeg.OutputPath!);
    var second = ImageConversion.Convert(pngPath, OutputFormat.Jpeg);
    Check(second.OutputPath == Path.Combine(directory, "sample (1).jpg"), "collision uses suffix");
    Check(File.ReadAllBytes(jpeg.OutputPath!).SequenceEqual(jpegBytes), "existing output is unchanged");

    var png = ImageConversion.Convert(jpeg.OutputPath!, OutputFormat.Png);
    using (var image = Image.FromFile(png.OutputPath!))
        Check(image.RawFormat.Guid == ImageFormat.Png.Guid, "JPEG converts to PNG");
    Check(File.ReadAllBytes(pngPath).SequenceEqual(original), "source is unchanged");

    var renamed = Path.Combine(directory, "renamed.jpg");
    File.Copy(pngPath, renamed);
    Check(ImageConversion.Convert(renamed, OutputFormat.Png).Skipped, "skip uses content, not extension");
    var invalid = Path.Combine(directory, "invalid.png");
    File.WriteAllText(invalid, "not an image");
    var count = Directory.GetFiles(directory).Length;
    var rejected = false;
    try { ImageConversion.Convert(invalid, OutputFormat.Jpeg); }
    catch (ArgumentException) { rejected = true; }
    catch (OutOfMemoryException) { rejected = true; }
    catch (MagickException) { rejected = true; }
    Check(rejected && Directory.GetFiles(directory).Length == count, "invalid input creates no output");

    foreach (var inputFormat in new[] { MagickFormat.Bmp, MagickFormat.Gif, MagickFormat.Tiff, MagickFormat.WebP, MagickFormat.Avif })
    {
        var sourcePath = Path.Combine(directory, "input." + inputFormat.ToString().ToLowerInvariant());
        using (var image = new MagickImage(MagickColors.CornflowerBlue, 32, 24))
            image.Write(sourcePath, inputFormat);
        VerifyConversions(sourcePath, 32, 24);
    }

    // HEIC は同梱ライブラリで読み込みのみ対応。外部の実画像を指定して検証する。
    if (args.Length > 0)
    {
        var fixture = Path.Combine(directory, "photo.heic");
        File.Copy(args[0], fixture);
        var info = new MagickImageInfo(fixture);
        Check(info.Format == MagickFormat.Heic, "fixture is a HEIC image");
        VerifyConversions(fixture, (int)info.Width, (int)info.Height);
    }
    else
        Console.WriteLine("HEIC fixture not supplied. Pass a HEIC file path to verify HEIC decoding.");

    // PNG の透過は PNG 出力でも維持される。
    var transparentWebp = Path.Combine(directory, "transparent.webp");
    using (var image = new MagickImage(MagickColors.Transparent, 16, 16))
        image.Write(transparentWebp, MagickFormat.WebP);
    var transparentPng = ImageConversion.Convert(transparentWebp, OutputFormat.Png);
    using (var image = new Bitmap(transparentPng.OutputPath!))
        Check(image.GetPixel(0, 0).A == 0, "WebP transparency survives PNG conversion");

    var disguisedWebp = Path.Combine(directory, "webp-with-jpeg-extension.jpg");
    File.Copy(transparentWebp, disguisedWebp);
    Check(!ImageConversion.Convert(disguisedWebp, OutputFormat.Jpeg).Skipped,
        "WebP with JPEG extension is decoded by content and converted");
    Console.WriteLine("All conversion checks passed.");
}
finally
{
    // このテストで作成した一意の一時フォルダー内のファイルだけを削除する。
    foreach (var path in Directory.GetFiles(directory)) File.Delete(path);
    Directory.Delete(directory);
}

static void Check(bool condition, string description)
{
    if (!condition) throw new InvalidOperationException(description);
    Console.WriteLine("PASS: " + description);
}

static void VerifyConversions(string path, int width, int height)
{
    var original = File.ReadAllBytes(path);
    foreach (var outputFormat in new[] { OutputFormat.Jpeg, OutputFormat.Png })
    {
        var converted = ImageConversion.Convert(path, outputFormat);
        Check(!converted.Skipped && Path.GetDirectoryName(converted.OutputPath) == Path.GetDirectoryName(path),
            $"{Path.GetExtension(path)} -> {outputFormat} saved beside source");
        using var image = new Bitmap(converted.OutputPath!);
        Check(image.Width == width && image.Height == height, "converted image dimensions preserved");
        Check(image.RawFormat.Guid == (outputFormat == OutputFormat.Jpeg ? ImageFormat.Jpeg : ImageFormat.Png).Guid,
            "output format matches selection");
    }
    Check(File.ReadAllBytes(path).SequenceEqual(original), "input bytes preserved");
}
