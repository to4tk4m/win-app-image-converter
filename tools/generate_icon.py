"""Rebuild the application icon and preview with Python and Pillow."""

from pathlib import Path

from PIL import Image, ImageDraw


ASSETS = Path(__file__).resolve().parents[1] / "ImageConverter" / "Assets"
SCALE = 4


def render(size):
    image = Image.new("RGBA", (256 * SCALE, 256 * SCALE))
    draw = ImageDraw.Draw(image)

    def box(bounds, radius, fill):
        draw.rounded_rectangle(tuple(v * SCALE for v in bounds), radius * SCALE, fill)

    def polygon(points, fill):
        draw.polygon([(x * SCALE, y * SCALE) for x, y in points], fill)

    box((8, 8, 248, 248), 52, "#1764ce")
    box((40, 44, 208, 184), 18, "#ffffff")
    box((52, 56, 196, 172), 9, "#d9eeff")
    draw.ellipse((144 * SCALE, 70 * SCALE, 174 * SCALE, 100 * SCALE), fill="#ffbd4b")
    polygon([(52, 150), (99, 96), (152, 160), (180, 123), (196, 144), (196, 172), (52, 172)], "#3986db")
    polygon([(52, 161), (88, 125), (130, 172), (52, 172)], "#215caf")
    # Two opposed arrows express conversion, even at small taskbar sizes.
    box((112, 152, 244, 240), 24, "#113f85")
    box((128, 172, 207, 184), 6, "#ffffff")
    polygon([(201, 160), (225, 178), (201, 196)], "#ffffff")
    box((148, 208, 226, 220), 6, "#63e3cf")
    polygon([(153, 196), (129, 214), (153, 232)], "#63e3cf")
    return image.resize((size, size), Image.Resampling.LANCZOS)


if __name__ == "__main__":
    ASSETS.mkdir(parents=True, exist_ok=True)
    render(256).save(ASSETS / "App.png")
    sizes = [16, 20, 24, 32, 40, 48, 64, 128, 256]
    render(256).save(
        ASSETS / "App.ico",
        sizes=[(size, size) for size in sizes],
        append_images=[render(size) for size in sizes[:-1]],
    )
