"""Generate the Milan tile's flag texture and a review image.

Run with: py Tools/generate_milan_tile_art.py
Requires Pillow and NumPy. The texture deliberately contains no lettering or
owner color: Unity renders both sharply and can update ownership at runtime.
"""

from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont


ROOT = Path(__file__).resolve().parents[1]
ART = ROOT / "Assets/_Ropoly/Resources/CityTiles/MilanFlagPanel.png"
PREVIEW = ROOT / "MilanTileConcept.png"
FONT_BOLD = Path("C:/Windows/Fonts/arialbd.ttf")
FONT_REGULAR = Path("C:/Windows/Fonts/arial.ttf")


def flag_texture(width=768, height=1120):
    yy, xx = np.mgrid[0:height, 0:width].astype(np.float32)
    x = xx / (width - 1)
    y = yy / (height - 1)

    # The edges of the three fields curve very slightly, like stretched fabric.
    fold = 0.014 * np.sin(y * 12.4 + 0.4) + 0.007 * np.sin(y * 28.0)
    boundary = x + fold
    green = np.array([24.0, 143.0, 95.0])
    ivory = np.array([244.0, 239.0, 222.0])
    red = np.array([205.0, 45.0, 57.0])
    colors = np.where(
        (boundary < 1.0 / 3.0)[..., None],
        green,
        np.where((boundary < 2.0 / 3.0)[..., None], ivory, red),
    )

    # Broad light folds, a central glow, and a vignette soften the flat flag.
    silk = 0.96 + 0.075 * np.sin(x * 26.0 + y * 2.7) + 0.025 * np.sin(x * 58.0 - y * 5.0)
    center_light = 0.10 * np.exp(-((x - 0.5) ** 2 / 0.18 + (y - 0.38) ** 2 / 0.14))
    edge_shade = 0.24 * np.maximum(np.abs(x - 0.5) * 2.0, np.abs(y - 0.5) * 1.65) ** 1.7
    top_bottom = 0.16 * (1.0 - y) ** 5 + 0.28 * y**4
    light = silk + center_light - edge_shade - top_bottom
    pixels = colors * light[..., None]

    # This middle fade is the quiet stage for live TextMesh Pro lettering.
    title_fade = 0.28 * np.exp(-((y - 0.49) / 0.22) ** 2)
    ink = np.array([16.0, 17.0, 29.0])
    pixels = pixels * (1 - title_fade[..., None]) + ink * title_fade[..., None]

    # Fine deterministic grain prevents the fabric effect looking plastic.
    grain = np.random.default_rng(21).normal(0, 1.6, (height, width, 1))
    pixels = np.uint8(np.clip(pixels + grain, 0, 255))
    return Image.fromarray(pixels, "RGB")


def draw_centered(draw, xy, text, font, fill, stroke_width=0, stroke_fill=None):
    draw.text(
        xy, text, font=font, anchor="mm", fill=fill,
        stroke_width=stroke_width, stroke_fill=stroke_fill,
    )


def draw_card(canvas, origin, owned, texture):
    x0, y0 = origin
    width, height = 350, 604
    draw = ImageDraw.Draw(canvas)
    draw.rounded_rectangle((x0 - 8, y0 - 8, x0 + width + 8, y0 + height + 8),
                           radius=22, fill=(7, 9, 22))
    draw.rounded_rectangle((x0, y0, x0 + width, y0 + height), radius=15,
                           fill=(31, 27, 47))

    panel = texture.resize((330, 516), Image.Resampling.LANCZOS)
    canvas.paste(panel, (x0 + 10, y0 + 10))
    draw = ImageDraw.Draw(canvas)
    draw.rectangle((x0 + 10, y0 + 10, x0 + 340, y0 + 526), outline=(192, 166, 126), width=2)
    draw_centered(draw, (x0 + width / 2, y0 + 85), "$230",
                  ImageFont.truetype(str(FONT_BOLD), 38), (252, 249, 237),
                  stroke_width=2, stroke_fill=(19, 20, 32))
    draw_centered(draw, (x0 + width / 2, y0 + 285), "MILAN",
                  ImageFont.truetype(str(FONT_BOLD), 67), (255, 252, 240),
                  stroke_width=3, stroke_fill=(17, 19, 32))
    band = (44, 42, 61) if not owned else (41, 205, 146)
    draw.rounded_rectangle((x0 + 10, y0 + 536, x0 + 340, y0 + 594),
                           radius=8, fill=band)
    draw.line((x0 + 10, y0 + 536, x0 + 340, y0 + 536),
              fill=(255, 245, 223), width=2)


def main():
    ART.parent.mkdir(parents=True, exist_ok=True)
    texture = flag_texture()
    texture.save(ART, optimize=True)

    preview = Image.new("RGB", (1040, 860), (15, 12, 31))
    draw = ImageDraw.Draw(preview)
    draw.text((110, 51), "MILAN  /  CITY TILE STUDY",
              font=ImageFont.truetype(str(FONT_BOLD), 33), fill=(247, 245, 253))
    draw.text((110, 104), "Full Italian flag  ·  live city name  ·  player-color band",
              font=ImageFont.truetype(str(FONT_REGULAR), 23), fill=(169, 166, 192))
    draw_card(preview, (110, 178), False, texture)
    draw_card(preview, (580, 178), True, texture)
    draw.text((110, 810), "UNOWNED", font=ImageFont.truetype(str(FONT_BOLD), 24),
              fill=(169, 166, 192))
    draw.text((580, 810), "OWNED  ·  SAMPLE PLAYER COLOR",
              font=ImageFont.truetype(str(FONT_BOLD), 24), fill=(80, 224, 166))
    preview.save(PREVIEW, optimize=True)
    print(f"Wrote {ART}")
    print(f"Wrote {PREVIEW}")


if __name__ == "__main__":
    main()
