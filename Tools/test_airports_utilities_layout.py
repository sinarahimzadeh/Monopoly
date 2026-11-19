from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw, ImageFont

ARTIFACT_DIR = Path(r"C:\Users\Admin\.gemini\antigravity\brain\1ede9fe0-845a-447d-89c1-c412754fe314")
ICONS_DIR = Path("Assets/_Ropoly/Resources/Icons")

# The 6 purchasable properties with their exact edge and aspect ratio
PROPS = [
    ("JFK AIRPORT", "$200", "Airport.png", "bottom", (22, 55, 105), (55, 145, 235), "Tile 05 (Bottom)"),
    ("WATER SERVICE", "$150", "Water.png", "left", (14, 65, 85), (30, 175, 190), "Tile 12 (Left)"),
    ("HEATHROW AIRPORT", "$200", "Airport.png", "left", (22, 55, 105), (55, 145, 235), "Tile 15 (Left)"),
    ("PARIS AIRPORT", "$200", "Airport.png", "top", (22, 55, 105), (55, 145, 235), "Tile 25 (Top)"),
    ("ELECTRIC COMPANY", "$150", "Electric.png", "top", (38, 30, 85), (245, 185, 45), "Tile 28 (Top)"),
    ("BEIJING AIRPORT", "$200", "Airport.png", "right", (22, 55, 105), (55, 145, 235), "Tile 35 (Right)"),
]

# We render each tile with its realistic aspect ratio
# Portrait tiles: 240w x 360h
# Landscape tiles: 360w x 240h
# To present cleanly in a composite, we can place them on a dark canvas
CANVAS_W = 1200
CANVAS_H = 800
canvas = Image.new("RGB", (CANVAS_W, CANVAS_H), (14, 12, 22))
draw = ImageDraw.Draw(canvas)

try:
    font_main = ImageFont.truetype("arialbd.ttf", 26)
    font_sub = ImageFont.truetype("arial.ttf", 15)
    font_title = ImageFont.truetype("arialbd.ttf", 16)
    font_price = ImageFont.truetype("arialbd.ttf", 16)
    font_tag = ImageFont.truetype("arialbd.ttf", 13)
except Exception:
    font_main = font_sub = font_title = font_price = font_tag = ImageFont.load_default()

draw.text((40, 24), "AIRPORTS & UTILITIES IN CITY SQUARE ARCHITECTURE", fill=(255, 255, 255), font=font_main)
draw.text((40, 60), "All 6 Non-City Properties with Unified Ownership Bands, Themed Panels & Straight Upright Icons", fill=(170, 165, 205), font=font_sub)

def make_panel(dark_c, light_c, w, h):
    yy, xx = np.mgrid[0:h, 0:w].astype(np.float32)
    x = xx / (w - 1)
    y = yy / (h - 1)
    silk = 0.95 + 0.05 * np.sin(x * 24.0 + y * 3.0) + 0.02 * np.sin(x * 52.0 - y * 4.0)
    r = np.sqrt((x - 0.5)**2 + (y - 0.45)**2)
    glow = np.exp(- (r**2) / 0.18)
    vignette = 1.0 - 0.35 * np.maximum(np.abs(x - 0.5) * 1.8, np.abs(y - 0.5) * 1.6)**1.8
    color_arr = np.zeros((h, w, 3), dtype=np.float32)
    for c in range(3):
        color_arr[..., c] = dark_c[c] + (light_c[c] - dark_c[c]) * glow * 0.50
        color_arr[..., c] = color_arr[..., c] * silk * vignette
    return Image.fromarray(np.uint8(np.clip(color_arr, 0, 255)), "RGB")

# Positions on canvas
# Row 1: JFK (portrait), Paris (portrait), Electric (portrait)
# Row 2: Water (landscape), Heathrow (landscape), Beijing (landscape)
POSITIONS = [
    (60, 130, 220, 320, True),   # JFK (portrait)
    (320, 130, 220, 320, True),  # Paris (portrait)
    (580, 130, 220, 320, True),  # Electric (portrait)
    (60, 500, 320, 220, False),  # Water (landscape)
    (420, 500, 320, 220, False), # Heathrow (landscape)
    (780, 500, 320, 220, False), # Beijing (landscape)
]

for idx, (title, price, icon_file, edge, dark_c, light_c, tag) in enumerate(PROPS):
    x0, y0, tw, th, is_portrait = POSITIONS[idx]

    draw.text((x0, y0 - 22), tag, fill=(185, 180, 215), font=font_tag)
    draw.rectangle([x0, y0, x0 + tw, y0 + th], fill=(22, 20, 32), outline=(48, 44, 68), width=2)

    band_w = tw - 8
    band_h = 44 if is_portrait else 40
    rule_h = 3

    if is_portrait:
        # If Bottom edge (JFK), band is at top (facing center)
        # If Top edge (Paris, Electric), band is at bottom (facing center)
        if edge == "bottom":
            band_y = y0 + 4
            rule_y = band_y + band_h
            panel_y = rule_y + rule_h + 3
            panel_h = th - band_h - rule_h - 11
        else:
            panel_y = y0 + 4
            panel_h = th - band_h - rule_h - 11
            rule_y = panel_y + panel_h + 3
            band_y = rule_y + rule_h
        panel_w = band_w
    else:
        # Landscape tile: band is at bottom of card or inner edge
        # To match the city squares, band is at bottom of card
        panel_y = y0 + 4
        panel_h = th - band_h - rule_h - 11
        rule_y = panel_y + panel_h + 3
        band_y = rule_y + rule_h
        panel_w = band_w

    # Background panel
    panel = make_panel(dark_c, light_c, panel_w, panel_h)
    canvas.paste(panel, (x0 + 4, panel_y))

    # Ownership band
    draw.rectangle([x0 + 4, band_y, x0 + 4 + band_w, band_y + band_h], fill=(44, 40, 62))
    # Rule
    draw.rectangle([x0 + 4, rule_y, x0 + 4 + band_w, rule_y + rule_h], fill=(220, 212, 185))

    # Icon
    icon_path = ICONS_DIR / icon_file
    if icon_path.exists():
        raw_icon = Image.open(icon_path).convert("RGBA")
        icon_sz = 52 if is_portrait else 44
        icon_resized = raw_icon.resize((icon_sz, icon_sz), Image.Resampling.LANCZOS)
        
        # Invert/tint to clean white
        icon_arr = np.array(icon_resized)
        icon_alpha = icon_arr[..., 3]
        white_icon = np.zeros_like(icon_arr)
        white_icon[..., :3] = 255
        white_icon[..., 3] = icon_alpha
        clean_icon = Image.fromarray(white_icon, "RGBA")

        icon_x = x0 + 4 + (panel_w - icon_sz) // 2
        icon_y = panel_y + (16 if is_portrait else 10)
        canvas.paste(clean_icon, (icon_x, icon_y), clean_icon)

    # Title
    lines = title.split("\n") if "\n" in title else [title]
    if len(title) > 9 and " " in title and "\n" not in title:
        lines = title.split(" ", 1)

    text_y = panel_y + (76 if is_portrait else 58)
    for l_idx, line in enumerate(lines):
        bbox = draw.textbbox((0, 0), line, font=font_title)
        lw = bbox[2] - bbox[0]
        lx = x0 + 4 + (panel_w - lw) // 2
        ly = text_y + l_idx * 20
        draw.text((lx + 2, ly + 2), line, fill=(8, 8, 16), font=font_title)
        draw.text((lx, ly), line, fill=(255, 255, 255), font=font_title)

    # Price in ownership band
    p_bbox = draw.textbbox((0, 0), price, font=font_price)
    pw = p_bbox[2] - p_bbox[0]
    ph = p_bbox[3] - p_bbox[1]
    px = x0 + 4 + (band_w - pw) // 2
    py = band_y + (band_h - ph) // 2 - 2
    draw.text((px, py), price, fill=(255, 255, 255), font=font_price)

out_f = ARTIFACT_DIR / "AirportsAndUtilitiesLayout.png"
canvas.save(out_f)
print("Saved layout to", out_f)
