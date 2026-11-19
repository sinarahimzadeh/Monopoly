from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ARTIFACT_DIR = Path(r"C:\Users\Admin\.gemini\antigravity\brain\1ede9fe0-845a-447d-89c1-c412754fe314")
TILES_DIR = Path("Assets/_Ropoly/Resources/CityTiles")

# Cities to showcase
SAMPLES = [
    ("DELHI", "$60", "IndiaFlagPanel_Soft.png", "bottom", "India (Bottom)"),
    ("RIO DE JANEIRO", "$120", "BrazilFlagPanel_Soft.png", "bottom", "Brazil (Bottom)"),
    ("BEIJING", "$140", "ChinaFlagPanel_Soft.png", "left", "China (Left)"),
    ("SAINT PETERSBURG", "$200", "RussiaFlagPanel_Soft.png", "left", "Russia (Left)"),
    ("MILAN", "$240", "ItalyFlagPanel_Soft.png", "top", "Italy (Top)"),
    ("PARIS", "$280", "FranceFlagPanel_Soft.png", "top", "France (Top)"),
    ("LONDON", "$320", "UnitedKingdomFlagPanel_Soft.png", "right", "UK (Right)"),
    ("NEW YORK", "$400", "UnitedStatesFlagPanel_Soft.png", "right", "USA (Right)"),
]

# Create a composite canvas (4 columns x 2 rows)
TILE_W, TILE_H = 260, 420
PADDING = 40
CANVAS_W = 4 * TILE_W + 5 * PADDING
CANVAS_H = 2 * TILE_H + 3 * PADDING + 60

canvas = Image.new("RGB", (CANVAS_W, CANVAS_H), (16, 14, 26))
draw = ImageDraw.Draw(canvas)

# Fonts
try:
    font_title = ImageFont.truetype("arialbd.ttf", 26)
    font_name = ImageFont.truetype("arialbd.ttf", 22)
    font_price = ImageFont.truetype("arialbd.ttf", 19)
    font_sub = ImageFont.truetype("arial.ttf", 15)
except Exception:
    font_title = font_name = font_price = font_sub = ImageFont.load_default()

draw.text((PADDING, 18), "ROPOLY - ALL CITY TILES (HOUSES) VISUAL ARCHITECTURE", fill=(255, 255, 255), font=font_title)

for idx, (city, price, flag_file, edge, header) in enumerate(SAMPLES):
    col = idx % 4
    row = idx // 4
    x0 = PADDING + col * (TILE_W + PADDING)
    y0 = 65 + PADDING + row * (TILE_H + PADDING)

    # Sub-header
    draw.text((x0, y0 - 24), header, fill=(180, 180, 210), font=font_sub)

    # Dark tile base
    draw.rectangle([x0, y0, x0 + TILE_W, y0 + TILE_H], fill=(24, 22, 34), outline=(50, 48, 66), width=2)

    # Load flag panel
    flag_path = TILES_DIR / flag_file
    if flag_path.exists():
        raw_flag = Image.open(flag_path).convert("RGBA")
    else:
        raw_flag = Image.new("RGBA", (TILE_W, TILE_H), (60, 60, 80, 255))

    band_h = 60
    rule_h = 3

    # On Top Edge: Band is at BOTTOM of tile (facing board center)
    # On Bottom Edge: Band is at TOP of tile (facing board center)
    if edge == "top":
        flag_y = y0 + 6
        flag_h = TILE_H - band_h - 14
        band_y = y0 + TILE_H - band_h - 6
        rule_y = band_y - rule_h
    elif edge == "bottom":
        band_y = y0 + 6
        rule_y = band_y + band_h
        flag_y = rule_y + rule_h + 4
        flag_h = TILE_H - band_h - 18
    else: # Left or Right edge (vertical on board)
        # We also illustrate radial inner band or uniform bottom band
        flag_y = y0 + 6
        flag_h = TILE_H - band_h - 14
        band_y = y0 + TILE_H - band_h - 6
        rule_y = band_y - rule_h

    # Resize flag and apply opacity (e.g. 0.45 over dark tile base)
    flag_w = TILE_W - 12
    flag_resized = raw_flag.resize((flag_w, flag_h), Image.Resampling.LANCZOS)
    
    # Composite flag with 0.45 opacity
    alpha_mask = Image.new("L", flag_resized.size, int(255 * 0.45))
    tile_crop = canvas.crop((x0 + 6, flag_y, x0 + 6 + flag_w, flag_y + flag_h))
    blended = Image.composite(flag_resized.convert("RGB"), tile_crop, alpha_mask)
    canvas.paste(blended, (x0 + 6, flag_y))

    # Ownership band
    draw.rectangle([x0 + 6, band_y, x0 + 6 + flag_w, band_y + band_h], fill=(46, 43, 64))

    # Divider rule line
    draw.rectangle([x0 + 6, rule_y, x0 + 6 + flag_w, rule_y + rule_h], fill=(225, 215, 190))

    # City Name centered on flag
    lines = city.split("\n") if "\n" in city else [city]
    if len(city) > 11 and " " in city and "\n" not in city:
        parts = city.split(" ", 1)
        lines = parts

    total_text_h = len(lines) * 26
    start_text_y = flag_y + (flag_h - total_text_h) // 2

    for l_idx, line in enumerate(lines):
        bbox = draw.textbbox((0, 0), line, font=font_name)
        lw = bbox[2] - bbox[0]
        lx = x0 + 6 + (flag_w - lw) // 2
        ly = start_text_y + l_idx * 26
        # Drop shadow
        draw.text((lx + 2, ly + 2), line, fill=(10, 10, 18), font=font_name)
        draw.text((lx, ly), line, fill=(255, 255, 255), font=font_name)

    # Price in ownership band
    p_bbox = draw.textbbox((0, 0), price, font=font_price)
    pw = p_bbox[2] - p_bbox[0]
    ph = p_bbox[3] - p_bbox[1]
    px = x0 + 6 + (flag_w - pw) // 2
    py = band_y + (band_h - ph) // 2 - 2
    draw.text((px, py), price, fill=(255, 255, 255), font=font_price)

out_path = ARTIFACT_DIR / "AllHousesPreview.png"
canvas.save(out_path)
print(f"Saved preview artifact to {out_path}")
