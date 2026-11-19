from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw, ImageFont

ARTIFACT_DIR = Path(r"C:\Users\Admin\.gemini\antigravity\brain\1ede9fe0-845a-447d-89c1-c412754fe314")
ICONS_DIR = Path("Assets/_Ropoly/Resources/Icons")

# Define the 6 utility and airport properties
PROPERTIES = [
    ("JFK AIRPORT", "$200", "Airport.png", (22, 55, 105), (55, 145, 235), "Bottom (Tile 05)", "Airport"),
    ("HEATHROW AIRPORT", "$200", "Airport.png", (22, 55, 105), (55, 145, 235), "Left (Tile 15)", "Airport"),
    ("PARIS AIRPORT", "$200", "Airport.png", (22, 55, 105), (55, 145, 235), "Top (Tile 25)", "Airport"),
    ("BEIJING AIRPORT", "$200", "Airport.png", (22, 55, 105), (55, 145, 235), "Right (Tile 35)", "Airport"),
    ("WATER SERVICE", "$150", "Water.png", (14, 65, 85), (30, 175, 190), "Left (Tile 12)", "Utility"),
    ("ELECTRIC COMPANY", "$150", "Electric.png", (38, 30, 85), (245, 185, 45), "Top (Tile 28)", "Utility"),
]

TILE_W, TILE_H = 240, 360
PADDING = 30
COLS = 3
ROWS = 2
HEADER_H = 90

CANVAS_W = COLS * TILE_W + (COLS + 1) * PADDING
CANVAS_H = ROWS * TILE_H + (ROWS + 1) * PADDING + HEADER_H

canvas = Image.new("RGB", (CANVAS_W, CANVAS_H), (14, 12, 22))
draw = ImageDraw.Draw(canvas)

try:
    font_main = ImageFont.truetype("arialbd.ttf", 26)
    font_sub_head = ImageFont.truetype("arial.ttf", 15)
    font_card_head = ImageFont.truetype("arialbd.ttf", 13)
    font_title = ImageFont.truetype("arialbd.ttf", 17)
    font_price = ImageFont.truetype("arialbd.ttf", 16)
except Exception:
    font_main = font_sub_head = font_card_head = font_title = font_price = ImageFont.load_default()

draw.text((PADDING, 20), "ROPOLY — AIRPORTS & UTILITIES («HOUSES») IN THE NEW FORMAT", fill=(255, 255, 255), font=font_main)
draw.text((PADDING, 54), "Clean Themed Backdrops  •  Crisp Centered Icons & Titles  •  Bottom Ownership Price Bands", fill=(170, 165, 205), font=font_sub_head)

def create_themed_panel(base_dark, accent_light, width, height):
    yy, xx = np.mgrid[0:height, 0:width].astype(np.float32)
    x = xx / (width - 1)
    y = yy / (height - 1)

    silk = 0.95 + 0.05 * np.sin(x * 24.0 + y * 3.0) + 0.02 * np.sin(x * 52.0 - y * 4.0)
    r = np.sqrt((x - 0.5)**2 + (y - 0.45)**2)
    glow = np.exp(- (r**2) / 0.18)
    vignette = 1.0 - 0.35 * np.maximum(np.abs(x - 0.5) * 1.8, np.abs(y - 0.5) * 1.6)**1.8

    color_arr = np.zeros((height, width, 3), dtype=np.float32)
    for c in range(3):
        color_arr[..., c] = base_dark[c] + (accent_light[c] - base_dark[c]) * glow * 0.50
        color_arr[..., c] = color_arr[..., c] * silk * vignette

    return Image.fromarray(np.uint8(np.clip(color_arr, 0, 255)), "RGB")

for idx, (title, price, icon_file, dark_c, light_c, edge_info, kind) in enumerate(PROPERTIES):
    col = idx % COLS
    row = idx // COLS
    x0 = PADDING + col * (TILE_W + PADDING)
    y0 = HEADER_H + PADDING + row * (TILE_H + PADDING)

    draw.text((x0, y0 - 20), f"{kind} • {edge_info}", fill=(185, 180, 215), font=font_card_head)
    draw.rectangle([x0, y0, x0 + TILE_W, y0 + TILE_H], fill=(22, 20, 32), outline=(48, 44, 68), width=2)

    band_h = 50
    rule_h = 3
    is_bottom = "Bottom" in edge_info

    if is_bottom:
        band_y = y0 + 4
        rule_y = band_y + band_h
        flag_y = rule_y + rule_h + 3
        flag_h = TILE_H - band_h - rule_h - 11
    else:
        flag_y = y0 + 4
        flag_h = TILE_H - band_h - rule_h - 11
        rule_y = flag_y + flag_h + 3
        band_y = rule_y + rule_h

    flag_w = TILE_W - 8

    # Themed background panel
    panel = create_themed_panel(dark_c, light_c, flag_w, flag_h)
    canvas.paste(panel, (x0 + 4, flag_y))

    # Ownership band
    draw.rectangle([x0 + 4, band_y, x0 + 4 + flag_w, band_y + band_h], fill=(44, 40, 62))
    # Divider rule
    draw.rectangle([x0 + 4, rule_y, x0 + 4 + flag_w, rule_y + rule_h], fill=(220, 212, 185))

    # Render Icon
    icon_path = ICONS_DIR / icon_file
    if icon_path.exists():
        raw_icon = Image.open(icon_path).convert("RGBA")
        icon_size = 64
        icon_resized = raw_icon.resize((icon_size, icon_size), Image.Resampling.LANCZOS)

        # Tint icon white/crisp
        icon_x = x0 + 4 + (flag_w - icon_size) // 2
        icon_y = flag_y + 36
        canvas.paste(icon_resized, (icon_x, icon_y), icon_resized)

    # Title below or above icon
    lines = title.split("\n") if "\n" in title else [title]
    if len(title) > 10 and " " in title and "\n" not in title:
        lines = title.split(" ", 1)

    text_y_start = flag_y + 115
    for l_idx, line in enumerate(lines):
        bbox = draw.textbbox((0, 0), line, font=font_title)
        lw = bbox[2] - bbox[0]
        lx = x0 + 4 + (flag_w - lw) // 2
        ly = text_y_start + l_idx * 22
        draw.text((lx + 2, ly + 2), line, fill=(8, 8, 16), font=font_title)
        draw.text((lx, ly), line, fill=(255, 255, 255), font=font_title)

    # Price in ownership band
    p_bbox = draw.textbbox((0, 0), price, font=font_price)
    pw = p_bbox[2] - p_bbox[0]
    ph = p_bbox[3] - p_bbox[1]
    px = x0 + 4 + (flag_w - pw) // 2
    py = band_y + (band_h - ph) // 2 - 2
    draw.text((px, py), price, fill=(255, 255, 255), font=font_price)

out_file = ARTIFACT_DIR / "AirportsAndUtilitiesShowcase.png"
canvas.save(out_file)
print("Saved showcase to", out_file)
