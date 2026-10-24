using ImageMagick;

namespace ImageConverter;

public enum OutputFormat { Jpeg, Png }

public sealed record ConversionResult(string SourcePath, string? OutputPath)
{
    public bool Skipped => OutputPath is null;
}

public static class ImageConversion
{
    public static ConversionResult Convert(string sourcePath, OutputFormat format)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        if (!Enum.IsDefined(format))
            throw new ArgumentOutOfRangeException(nameof(format));

        sourcePath = Path.GetFullPath(sourcePath);
        // ストリームで開き、拡張子や ImageMagick の特殊なファイル名構文に依存しない。
        using var input = File.OpenRead(sourcePath);
        using var source = new MagickImage(input, new MagickReadSettings { FrameIndex = 0, FrameCount = 1 });
        var targetFormat = format == OutputFormat.Jpeg ? MagickFormat.Jpeg : MagickFormat.Png;

        // 拡張子ではなく画像の実際の形式で判定する。
        if (source.Format == targetFormat)
            return new ConversionResult(sourcePath, null);

        // スマートフォン写真の向きを画素に反映してからメタデータを除去する。
        source.AutoOrient();
        source.ColorSpace = ColorSpace.sRGB;
        source.Strip();
        if (format == OutputFormat.Jpeg)
        {
            source.BackgroundColor = MagickColors.White;
            source.Alpha(AlphaAction.Remove);
            source.Quality = 90;
        }

        var extension = format == OutputFormat.Jpeg ? ".jpg" : ".png";
        var directory = Path.GetDirectoryName(sourcePath)!;
        var stem = Path.GetFileNameWithoutExtension(sourcePath);
        for (var suffix = 0; ; suffix++)
        {
            var name = suffix == 0 ? stem + extension : $"{stem} ({suffix}){extension}";
            var outputPath = Path.Combine(directory, name);
            FileStream output;
            try
            {
                // CreateNew により、同時にファイルが作成されても上書きしない。
                output = new FileStream(outputPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            }
            catch (IOException) when (File.Exists(outputPath) || Directory.Exists(outputPath))
            {
                continue;
            }

            try
            {
                using (output)
                {
                    source.Write(output, targetFormat);
                }
                return new ConversionResult(sourcePath, outputPath);
            }
            catch
            {
                // 保存失敗時は、この処理で作成した不完全なファイルだけを削除する。
                try { File.Delete(outputPath); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
                throw;
            }
        }
    }
}
