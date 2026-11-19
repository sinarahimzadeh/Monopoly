from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ARTIFACT_DIR = Path(r"C:\Users\Admin\.gemini\antigravity\brain\1ede9fe0-845a-447d-89c1-c412754fe314")
TILES_DIR = Path("Assets/_Ropoly/Resources/CityTiles")

ALL_22_CITIES = [
    # India (Bottom edge) - Portrait, Straight
    ("DELHI", "$60", "IndiaFlagPanel_Soft.png", "India", "Bottom (Tile 01)"),
    ("BENGALURU", "$60", "IndiaFlagPanel_Soft.png", "India", "Bottom (Tile 03)"),
    ("MUMBAI", "$80", "IndiaFlagPanel_Soft.png", "India", "Bottom (Tile 06)"),
    # Brazil (Bottom & Left edge) - Straight
    ("BRASILIA", "$100", "BrazilFlagPanel_Soft.png", "Brazil", "Bottom (Tile 08)"),
    ("RIO DE JANEIRO", "$120", "BrazilFlagPanel_Soft.png", "Brazil", "Bottom (Tile 09)"),
    ("SAO PAULO", "$120", "BrazilFlagPanel_Wide_Soft.png", "Brazil", "Left (Tile 11)"),
    # China (Left edge) - Straight Landscape
    ("BEIJING", "$140", "ChinaFlagPanel_Wide_Soft.png", "China", "Left (Tile 13)"),
    ("SHENZHEN", "$140", "ChinaFlagPanel_Wide_Soft.png", "China", "Left (Tile 14)"),
    ("SHANGHAI", "$160", "ChinaFlagPanel_Wide_Soft.png", "China", "Left (Tile 16)"),
    # Russia (Left & Top edge) - Straight
    ("KAZAN", "$180", "RussiaFlagPanel_Wide_Soft.png", "Russia", "Left (Tile 18)"),
    ("SAINT PETERSBURG", "$200", "RussiaFlagPanel_Wide_Soft.png", "Russia", "Left (Tile 19)"),
    ("MOSCOW", "$220", "RussiaFlagPanel_Soft.png", "Russia", "Top (Tile 21)"),
    # Italy (Top edge) - Straight Portrait
    ("ROME", "$220", "ItalyFlagPanel_Soft.png", "Italy", "Top (Tile 23)"),
    ("MILAN", "$240", "ItalyFlagPanel_Soft.png", "Italy", "Top (Tile 24)"),
    # France (Top edge) - Straight Portrait
    ("MARSEILLE", "$260", "FranceFlagPanel_Soft.png", "France", "Top (Tile 26)"),
    ("PARIS", "$280", "FranceFlagPanel_Soft.png", "France", "Top (Tile 27)"),
    # United Kingdom (Right edge) - Straight Landscape
    ("LIVERPOOL", "$300", "UnitedKingdomFlagPanel_Wide_Soft.png", "UK", "Right (Tile 31)"),
    ("MANCHESTER", "$300", "UnitedKingdomFlagPanel_Wide_Soft.png", "UK", "Right (Tile 32)"),
    ("LONDON", "$320", "UnitedKingdomFlagPanel_Wide_Soft.png", "UK", "Right (Tile 34)"),
    # United States (Right edge) - Straight Landscape
    ("SAN FRANCISCO", "$350", "UnitedStatesFlagPanel_Wide_Soft.png", "USA", "Right (Tile 37)"),
    ("LOS ANGELES", "$350", "UnitedStatesFlagPanel_Wide_Soft.png", "USA", "Right (Tile 38)"),
    ("NEW YORK", "$400", "UnitedStatesFlagPanel_Wide_Soft.png", "USA", "Right (Tile 39)"),
]

# Layout: 6 columns x 4 rows (last row has 4 cards)
COLS = 6
ROWS = 4
TILE_W, TILE_H = 210, 310
PAD_X, PAD_Y = 24, 32
HEADER_H = 90

CANVAS_W = COLS * TILE_W + (COLS + 1) * PAD_X
CANVAS_H = ROWS * TILE_H + (ROWS + 1) * PAD_Y + HEADER_H

canvas = Image.new("RGB", (CANVAS_W, CANVAS_H), (14, 12, 22))
draw = ImageDraw.Draw(canvas)

try:
    font_main = ImageFont.truetype("arialbd.ttf", 28)
    font_sub_head = ImageFont.truetype("arial.ttf", 16)
    font_card_head = ImageFont.truetype("arialbd.ttf", 13)
    font_city = ImageFont.truetype("arialbd.ttf", 17)
    font_price = ImageFont.truetype("arialbd.ttf", 15)
except Exception:
    font_main = font_sub_head = font_card_head = font_city = font_price = ImageFont.load_default()

# Header
draw.text((PAD_X, 22), "ROPOLY — ALL 22 CITY PROPERTIES («HOUSES»)", fill=(255, 255, 255), font=font_main)
draw.text((PAD_X, 58), "All Flags Straight & Upright (Never Rotated by Position)  •  Centered Bold City Names  •  Ownership Price Bands", fill=(170, 165, 205), font=font_sub_head)

for idx, (city, price, flag_file, country, edge_info) in enumerate(ALL_22_CITIES):
    col = idx % COLS
    row = idx // COLS
    x0 = PAD_X + col * (TILE_W + PAD_X)
    y0 = HEADER_H + PAD_Y + row * (TILE_H + PAD_Y)

    # Label above tile
    draw.text((x0, y0 - 18), f"{country} • {edge_info}", fill=(185, 180, 215), font=font_card_head)

    # Dark tile base
    draw.rectangle([x0, y0, x0 + TILE_W, y0 + TILE_H], fill=(22, 20, 32), outline=(48, 44, 68), width=2)

    flag_path = TILES_DIR / flag_file
    raw_flag = Image.open(flag_path).convert("RGBA") if flag_path.exists() else Image.new("RGBA", (TILE_W, TILE_H), (60, 60, 80))

    band_h = 44
    rule_h = 3

    # On Bottom edge tiles (India, Brazil 8,9), the band borders the board center (top of card)
    is_bottom_edge = "Bottom" in edge_info
    if is_bottom_edge:
        band_y = y0 + 4
        rule_y = band_y + band_h
        flag_y = rule_y + rule_h + 3
        flag_h = TILE_H - band_h - rule_h - 11
    else:
        # Top, Left, and Right edges: band is underneath flag
        flag_y = y0 + 4
        flag_h = TILE_H - band_h - rule_h - 11
        rule_y = flag_y + flag_h + 3
        band_y = rule_y + rule_h

    flag_w = TILE_W - 8
    flag_resized = raw_flag.resize((flag_w, flag_h), Image.Resampling.LANCZOS)
    
    # 0.45 opacity blend over dark background
    alpha_mask = Image.new("L", flag_resized.size, int(255 * 0.45))
    tile_crop = canvas.crop((x0 + 4, flag_y, x0 + 4 + flag_w, flag_y + flag_h))
    blended = Image.composite(flag_resized.convert("RGB"), tile_crop, alpha_mask)
    canvas.paste(blended, (x0 + 4, flag_y))

    # Ownership band
    draw.rectangle([x0 + 4, band_y, x0 + 4 + flag_w, band_y + band_h], fill=(44, 40, 62))

    # Divider rule line
    draw.rectangle([x0 + 4, rule_y, x0 + 4 + flag_w, rule_y + rule_h], fill=(220, 212, 185))

    # City Name centered
    lines = [city]
    if len(city) > 10 and " " in city:
        lines = city.split(" ", 1)
    
    line_h = 20
    total_h = len(lines) * line_h
    start_y = flag_y + (flag_h - total_h) // 2

    for l_idx, line in enumerate(lines):
        bbox = draw.textbbox((0, 0), line, font=font_city)
        lw = bbox[2] - bbox[0]
        lx = x0 + 4 + (flag_w - lw) // 2
        ly = start_y + l_idx * line_h
        draw.text((lx + 2, ly + 2), line, fill=(8, 8, 16), font=font_city)
        draw.text((lx, ly), line, fill=(255, 255, 255), font=font_city)

    # Price in ownership band
    p_bbox = draw.textbbox((0, 0), price, font=font_price)
    pw = p_bbox[2] - p_bbox[0]
    ph = p_bbox[3] - p_bbox[1]
    px = x0 + 4 + (flag_w - pw) // 2
    py = band_y + (band_h - ph) // 2 - 2
    draw.text((px, py), price, fill=(255, 255, 255), font=font_price)

out_file = ARTIFACT_DIR / "All22CitiesVisualShowcase.png"
canvas.save(out_file)
print(f"Saved complete showcase to {out_file}")
