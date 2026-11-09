"""Generate visual preview of the Italian city tiles (Rome and Milan)."""

from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
TEXTURE_PATH = ROOT / "Assets/_Ropoly/Resources/CityTiles/MilanFlagPanel.png"
OUTPUT_PATH = ROOT / "ItalyTilesConcept.png"
ARTIFACT_DIR = Path("C:/Users/Admin/.gemini/antigravity/brain/1ede9fe0-845a-447d-89c1-c412754fe314")
FONT_BOLD = Path("C:/Windows/Fonts/arialbd.ttf")
FONT_REGULAR = Path("C:/Windows/Fonts/arial.ttf")


def draw_centered(draw, xy, text, font, fill, stroke_width=0, stroke_fill=None):
    draw.text(
        xy, text, font=font, anchor="mm", fill=fill,
        stroke_width=stroke_width, stroke_fill=stroke_fill,
    )


def draw_card(canvas, origin, city_name, price, owned, owner_color, texture):
    x0, y0 = origin
    width, height = 300, 518
    draw = ImageDraw.Draw(canvas)

    # Dark outer border / card housing
    draw.rounded_rectangle(
        (x0 - 4, y0 - 4, x0 + width + 4, y0 + height + 4),
        radius=14, fill=(18, 14, 30)
    )
    draw.rounded_rectangle(
        (x0, y0, x0 + width, y0 + height),
        radius=10, fill=(31, 27, 47)
    )

    # Flag panel
    flag_h = 432
    flag_w = width - 16
    panel = texture.resize((flag_w, flag_h), Image.Resampling.LANCZOS)
    canvas.paste(panel, (x0 + 8, y0 + 8))
    draw = ImageDraw.Draw(canvas)
    draw.rectangle((x0 + 8, y0 + 8, x0 + width - 8, y0 + 8 + flag_h), outline=(192, 166, 126), width=1)

    # Price lettering (top of flag)
    draw_centered(
        draw, (x0 + width / 2, y0 + 55), price,
        ImageFont.truetype(str(FONT_BOLD), 32), (255, 252, 240),
        stroke_width=2, stroke_fill=(16, 17, 28)
    )

    # City name lettering (centered on flag)
    font_size = 52 if len(city_name) <= 5 else 44
    draw_centered(
        draw, (x0 + width / 2, y0 + 225), city_name,
        ImageFont.truetype(str(FONT_BOLD), font_size), (255, 252, 240),
        stroke_width=3, stroke_fill=(16, 17, 28)
    )

    # Ownership band underneath flag
    band_y0 = y0 + 8 + flag_h + 8
    band_y1 = y0 + height - 8
    band_fill = owner_color if owned else (40, 39, 53)
    draw.rounded_rectangle((x0 + 8, band_y0, x0 + width - 8, band_y1), radius=6, fill=band_fill)

    # Elegant divider rule line
    draw.line((x0 + 8, band_y0, x0 + width - 8, band_y0), fill=(225, 215, 190), width=2)

    # Upgrade text on band if owned
    if owned:
        draw_centered(
            draw, (x0 + width / 2, (band_y0 + band_y1) / 2), "OWNED",
            ImageFont.truetype(str(FONT_BOLD), 18), (255, 255, 255)
        )


def main():
    if not TEXTURE_PATH.exists():
        print(f"Missing {TEXTURE_PATH}")
        return

    texture = Image.open(TEXTURE_PATH)
    canvas = Image.new("RGB", (1360, 680), (15, 12, 26))
    draw = ImageDraw.Draw(canvas)

    draw.text(
        (60, 30), "ITALY CITY SQUARES  ·  INITIAL VISUALS (ROME & MILAN)",
        font=ImageFont.truetype(str(FONT_BOLD), 26), fill=(247, 245, 253)
    )
    draw.text(
        (60, 68), "Full Italian Flag Face  ·  Live Bold Name  ·  Top Price  ·  Dynamic Bottom Ownership Band",
        font=ImageFont.truetype(str(FONT_REGULAR), 17), fill=(169, 166, 192)
    )

    # 1. Rome Unowned
    draw_card(canvas, (60, 110), "ROME", "$220", False, None, texture)
    draw.text((60, 640), "TILE 23 · ROME (UNOWNED)", font=ImageFont.truetype(str(FONT_BOLD), 16), fill=(160, 155, 180))

    # 2. Milan Unowned
    draw_card(canvas, (380, 110), "MILAN", "$230", False, None, texture)
    draw.text((380, 640), "TILE 24 · MILAN (UNOWNED)", font=ImageFont.truetype(str(FONT_BOLD), 16), fill=(160, 155, 180))

    # 3. Rome Owned
    draw_card(canvas, (700, 110), "ROME", "$220", True, (245, 190, 40), texture)
    draw.text((700, 640), "ROME (OWNED · SUNNY YELLOW)", font=ImageFont.truetype(str(FONT_BOLD), 16), fill=(245, 190, 40))

    # 4. Milan Owned
    draw_card(canvas, (1020, 110), "MILAN", "$230", True, (45, 205, 125), texture)
    draw.text((1020, 640), "MILAN (OWNED · PIP GREEN)", font=ImageFont.truetype(str(FONT_BOLD), 16), fill=(45, 205, 125))

    canvas.save(OUTPUT_PATH, optimize=True)
    if ARTIFACT_DIR.exists():
        canvas.save(ARTIFACT_DIR / "ItalyTilesConcept.png", optimize=True)
    print(f"Generated {OUTPUT_PATH}")


if __name__ == "__main__":
    main()
