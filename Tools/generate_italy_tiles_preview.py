"""Generate visual preview of the Italian city tiles (Rome and Milan) with updated styling."""

from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
BLUR_TEXTURE = ROOT / "Assets/_Ropoly/Resources/CityTiles/MilanFlagPanel_Blur.png"
OUTPUT_PATH = ROOT / "ItalyTilesConcept.png"
ARTIFACT_DIR = Path("C:/Users/Admin/.gemini/antigravity/brain/1ede9fe0-845a-447d-89c1-c412754fe314")
FONT_BOLD = Path("C:/Windows/Fonts/arialbd.ttf")
FONT_REGULAR = Path("C:/Windows/Fonts/arial.ttf")


def draw_centered(draw, xy, text, font, fill, stroke_width=0, stroke_fill=None):
    draw.text(
        xy, text, font=font, anchor="mm", fill=fill,
        stroke_width=stroke_width, stroke_fill=stroke_fill,
    )


def draw_card(canvas, origin, city_name, price, owned, owner_color, flag_img, opacity=0.45):
    x0, y0 = origin
    width, height = 300, 520
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

    # Flag panel with opacity
    flag_h = 440
    flag_w = width - 16
    panel = flag_img.resize((flag_w, flag_h), Image.Resampling.LANCZOS).convert("RGBA")
    r, g, b, a = panel.split()
    a = a.point(lambda p: int(p * opacity))
    panel.putalpha(a)

    canvas.paste(panel, (x0 + 8, y0 + 8), mask=panel)

    draw = ImageDraw.Draw(canvas)
    draw.rectangle(
        (x0 + 8, y0 + 8, x0 + width - 8, y0 + 8 + flag_h),
        outline=(192, 166, 126, int(160 * opacity)),
        width=1
    )

    # City name lettering (centered on flag)
    font_size = 50 if len(city_name) <= 5 else 44
    draw_centered(
        draw, (x0 + width / 2, y0 + 225), city_name,
        ImageFont.truetype(str(FONT_BOLD), font_size), (255, 255, 255),
        stroke_width=3, stroke_fill=(16, 17, 28)
    )

    # Ownership band underneath flag
    band_y0 = y0 + 8 + flag_h + 6
    band_y1 = y0 + height - 8
    band_fill = owner_color if owned else (44, 42, 61)
    draw.rounded_rectangle((x0 + 8, band_y0, x0 + width - 8, band_y1), radius=6, fill=band_fill)

    # Elegant divider rule line
    draw.line((x0 + 8, band_y0, x0 + width - 8, band_y0), fill=(225, 215, 190, 200), width=2)

    # Price written in the band underneath (NO 'OWNED' text!)
    draw_centered(
        draw, (x0 + width / 2, (band_y0 + band_y1) / 2), price,
        ImageFont.truetype(str(FONT_BOLD), 24), (255, 255, 255),
        stroke_width=1, stroke_fill=(16, 17, 28)
    )


def main():
    if not BLUR_TEXTURE.exists():
        print(f"Missing {BLUR_TEXTURE}")
        return

    blur_texture = Image.open(BLUR_TEXTURE)
    canvas = Image.new("RGB", (1360, 680), (15, 12, 26))
    draw = ImageDraw.Draw(canvas)

    draw.text(
        (60, 30), "ITALY CITY SQUARES  ·  UPDATED VISUALS (ROME & MILAN)",
        font=ImageFont.truetype(str(FONT_BOLD), 26), fill=(247, 245, 253)
    )
    draw.text(
        (60, 68), "Blurred Flag Face (45% Opacity)  ·  Centered City Name  ·  Price in Bottom Band  ·  No 'OWNED' Text",
        font=ImageFont.truetype(str(FONT_REGULAR), 17), fill=(169, 166, 192)
    )

    # 1. Rome Unowned
    draw_card(canvas, (60, 110), "ROME", "$220", False, None, blur_texture, opacity=0.45)
    draw.text((60, 640), "TILE 23 · ROME (UNOWNED)", font=ImageFont.truetype(str(FONT_BOLD), 16), fill=(160, 155, 180))

    # 2. Milan Unowned
    draw_card(canvas, (380, 110), "MILAN", "$230", False, None, blur_texture, opacity=0.45)
    draw.text((380, 640), "TILE 24 · MILAN (UNOWNED)", font=ImageFont.truetype(str(FONT_BOLD), 16), fill=(160, 155, 180))

    # 3. Rome Owned
    draw_card(canvas, (700, 110), "ROME", "$220", True, (245, 190, 40), blur_texture, opacity=0.45)
    draw.text((700, 640), "ROME (OWNED · SUNNY YELLOW)", font=ImageFont.truetype(str(FONT_BOLD), 16), fill=(245, 190, 40))

    # 4. Milan Owned
    draw_card(canvas, (1020, 110), "MILAN", "$230", True, (41, 205, 146), blur_texture, opacity=0.45)
    draw.text((1020, 640), "MILAN (OWNED · PIP GREEN)", font=ImageFont.truetype(str(FONT_BOLD), 16), fill=(41, 205, 146))

    canvas.save(OUTPUT_PATH, optimize=True)
    if ARTIFACT_DIR.exists():
        canvas.save(ARTIFACT_DIR / "ItalyTilesConcept.png", optimize=True)
    print(f"Generated {OUTPUT_PATH}")


if __name__ == "__main__":
    main()
