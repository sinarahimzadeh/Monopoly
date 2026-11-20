import os
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw, ImageFont

ARTIFACT_DIR = Path(r"C:\Users\Admin\.gemini\antigravity\brain\1ede9fe0-845a-447d-89c1-c412754fe314")
TILES_DIR = Path("Assets/_Ropoly/Resources/CityTiles")
ICONS_DIR = Path("Assets/_Ropoly/Resources/Icons")

CANVAS_W = 1800
CANVAS_H = 1100

canvas = Image.new("RGB", (CANVAS_W, CANVAS_H), (14, 13, 22))
draw = ImageDraw.Draw(canvas)

try:
    font_header = ImageFont.truetype("arialbd.ttf", 28)
    font_sub = ImageFont.truetype("arial.ttf", 15)
    font_section = ImageFont.truetype("arialbd.ttf", 19)
    font_title = ImageFont.truetype("arialbd.ttf", 26)
    font_title_small = ImageFont.truetype("arialbd.ttf", 21)
    font_band = ImageFont.truetype("arialbd.ttf", 17)
    font_tag = ImageFont.truetype("arialbd.ttf", 12)
except Exception:
    font_header = font_sub = font_section = font_title = font_title_small = font_band = font_tag = ImageFont.load_default()

# Header
draw.text((40, 24), "UNIFIED BOARD TILE ARCHITECTURE: ALL 4 CORNERS & PROPERTY CARDS", fill=(255, 255, 255), font=font_header)
draw.text((40, 62), "Every tile now shares the exact same modern card format: Themed Blurred Panel, Bold Title w/ Drop Shadow, Upright Icon, Metallic Rule & Action Band", fill=(175, 170, 205), font=font_sub)

# Section 1: The 4 Corners (Square 400x400)
draw.text((40, 108), "1. THE 4 CORNER HOUSES (Start, Prison, Vacation & Go To Prison)", fill=(46, 213, 115), font=font_section)

CORNERS = [
    ("START", "COLLECT $200", "Start.png", "StartPanel_Blur.png", "Tile 00 - Start (Bottom-Right)"),
    ("PRISON", "JUST VISITING", "Detention.png", "DetentionPanel_Blur.png", "Tile 10 - Prison (Bottom-Left)"),
    ("VACATION", "FREE REST", "Vacation.png", "VacationPanel_Blur.png", "Tile 20 - Vacation (Top-Left)"),
    ("GO TO PRISON", "LOCK UP", "GoToDetention.png", "GoToDetentionPanel_Blur.png", "Tile 30 - Finish / Go To Prison (Top-Right)"),
]

CARD_SIZE = 400

def render_corner_card(title, band_text, icon_name, panel_name):
    card = Image.new("RGBA", (CARD_SIZE, CARD_SIZE), (24, 22, 34, 255))
    cdraw = ImageDraw.Draw(card)
    
    pad = 10
    band_h = 58
    rule_h = 3
    panel_h = CARD_SIZE - (pad * 2) - band_h - rule_h - 4
    panel_w = CARD_SIZE - (pad * 2)
    
    panel_x = pad
    panel_y = pad
    rule_y = panel_y + panel_h + 2
    band_y = rule_y + rule_h + 2
    
    # Backdrop panel
    panel_path = TILES_DIR / panel_name
    if panel_path.exists():
        panel_img = Image.open(panel_path).convert("RGBA").resize((panel_w, panel_h), Image.Resampling.LANCZOS)
        p_arr = np.array(panel_img).astype(np.float32)
        p_arr[..., 3] *= 0.65
        panel_tinted = Image.fromarray(np.uint8(p_arr), "RGBA")
        card.paste(panel_tinted, (panel_x, panel_y), panel_tinted)
        
    # Rule line
    cdraw.rectangle([panel_x, rule_y, panel_x + panel_w, rule_y + rule_h], fill=(225, 215, 185, 200))
    
    # Bottom band
    cdraw.rectangle([panel_x, band_y, panel_x + panel_w, band_y + band_h], fill=(42, 38, 58, 255))
    
    # Band text
    bbox = font_band.getbbox(band_text)
    tw = bbox[2] - bbox[0]
    th = bbox[3] - bbox[1]
    tx = panel_x + (panel_w - tw) // 2
    ty = band_y + (band_h - th) // 2
    cdraw.text((tx, ty), band_text, fill=(255, 255, 255), font=font_band)
    
    # Title
    f_title = font_title_small if len(title) > 10 else font_title
    t_bbox = f_title.getbbox(title)
    ttw = t_bbox[2] - t_bbox[0]
    tth = t_bbox[3] - t_bbox[1]
    
    title_y = panel_y + 16
    title_x = panel_x + (panel_w - ttw) // 2
    cdraw.text((title_x + 2, title_y + 2), title, fill=(10, 8, 16, 240), font=f_title)
    cdraw.text((title_x, title_y), title, fill=(255, 255, 255), font=f_title)
    
    # Center icon
    icon_path = ICONS_DIR / icon_name
    if icon_path.exists():
        icon_img = Image.open(icon_path).convert("RGBA")
        icon_size = 150
        ic_resized = icon_img.resize((icon_size, icon_size), Image.Resampling.LANCZOS)
        ic_x = panel_x + (panel_w - icon_size) // 2
        ic_y = panel_y + 16 + tth + (panel_h - (16 + tth) - icon_size) // 2
        card.paste(ic_resized, (ic_x, ic_y), ic_resized)
        
    return card

for i, (title, b_text, icon_n, panel_n, tag) in enumerate(CORNERS):
    x = 40 + i * (CARD_SIZE + 35)
    y = 145
    c_img = render_corner_card(title, b_text, icon_n, panel_n)
    canvas.paste(c_img, (x, y))
    
    # Tag
    draw.text((x, y + CARD_SIZE + 10), tag, fill=(160, 165, 195), font=font_tag)

# Section 2: Reference Cards (Airport, Utility, City, World Event)
draw.text((40, 600), "2. SAME FORMAT ACROSS ENTIRE BOARD (Airports, Utilities, Cities & World Event)", fill=(90, 195, 255), font=font_section)

REF_CARDS = [
    ("JFK AIRPORT", "$200", "Airport_White.png", "AirportPanel_Blur.png", "Tile 05 (Airport)", False),
    ("WATER SERVICE", "$150", "Water_White.png", "WaterPanel_Blur.png", "Tile 12 (Utility)", False),
    ("ROME", "$220", None, "ItalyFlagPanel_Blur.png", "Tile 23 (City Property)", True),
    ("WORLD EVENT", "DRAW CARD", None, "WorldEventPanel_Blur.png", "Tile 02 (World Event)", False, "?"),
]

for i, item in enumerate(REF_CARDS):
    title = item[0]
    b_text = item[1]
    icon_n = item[2]
    panel_n = item[3]
    tag = item[4]
    is_city = item[5]
    custom_mark = item[6] if len(item) > 6 else None
    
    x = 40 + i * (CARD_SIZE + 35)
    y = 635
    
    card = Image.new("RGBA", (CARD_SIZE, CARD_SIZE), (24, 22, 34, 255))
    cdraw = ImageDraw.Draw(card)
    
    pad = 10
    band_h = 58
    rule_h = 3
    panel_h = CARD_SIZE - (pad * 2) - band_h - rule_h - 4
    panel_w = CARD_SIZE - (pad * 2)
    
    panel_x = pad
    panel_y = pad
    rule_y = panel_y + panel_h + 2
    band_y = rule_y + rule_h + 2
    
    panel_path = TILES_DIR / panel_n
    if panel_path.exists():
        panel_img = Image.open(panel_path).convert("RGBA").resize((panel_w, panel_h), Image.Resampling.LANCZOS)
        p_arr = np.array(panel_img).astype(np.float32)
        p_arr[..., 3] *= 0.65
        panel_tinted = Image.fromarray(np.uint8(p_arr), "RGBA")
        card.paste(panel_tinted, (panel_x, panel_y), panel_tinted)
        
    cdraw.rectangle([panel_x, rule_y, panel_x + panel_w, rule_y + rule_h], fill=(225, 215, 185, 200))
    cdraw.rectangle([panel_x, band_y, panel_x + panel_w, band_y + band_h], fill=(42, 38, 58, 255))
    
    bbox = font_band.getbbox(b_text)
    tw = bbox[2] - bbox[0]
    th = bbox[3] - bbox[1]
    tx = panel_x + (panel_w - tw) // 2
    ty = band_y + (band_h - th) // 2
    cdraw.text((tx, ty), b_text, fill=(255, 255, 255), font=font_band)
    
    if custom_mark:
        # Question mark
        f_q = ImageFont.truetype("arialbd.ttf", 100)
        q_box = f_q.getbbox(custom_mark)
        qw = q_box[2] - q_box[0]
        qh = q_box[3] - q_box[1]
        qx = panel_x + (panel_w - qw) // 2
        qy = panel_y + (panel_h - qh) // 2 - 10
        cdraw.text((qx + 3, qy + 3), custom_mark, fill=(10, 8, 16, 240), font=f_q)
        cdraw.text((qx, qy), custom_mark, fill=(255, 255, 255), font=f_q)
    elif is_city:
        f_c = ImageFont.truetype("arialbd.ttf", 36)
        c_box = f_c.getbbox(title)
        cw = c_box[2] - c_box[0]
        ch = c_box[3] - c_box[1]
        cx = panel_x + (panel_w - cw) // 2
        cy = panel_y + (panel_h - ch) // 2
        cdraw.text((cx + 2, cy + 2), title, fill=(10, 8, 16, 240), font=f_c)
        cdraw.text((cx, cy), title, fill=(255, 255, 255), font=f_c)
    else:
        # Icon at top, title below
        if icon_n:
            icon_p = ICONS_DIR / icon_n
            if icon_p.exists():
                ic = Image.open(icon_p).convert("RGBA").resize((120, 120), Image.Resampling.LANCZOS)
                card.paste(ic, (panel_x + (panel_w - 120) // 2, panel_y + 24), ic)
                
        f_t = font_title_small if len(title) > 10 else font_title
        t_box = f_t.getbbox(title)
        tw = t_box[2] - t_box[0]
        th = t_box[3] - t_box[1]
        tx = panel_x + (panel_w - tw) // 2
        ty = panel_y + 160
        cdraw.text((tx + 2, ty + 2), title, fill=(10, 8, 16, 240), font=f_t)
        cdraw.text((tx, ty), title, fill=(255, 255, 255), font=f_t)
        
    canvas.paste(card, (x, y))
    draw.text((x, y + CARD_SIZE + 10), tag, fill=(160, 165, 195), font=font_tag)

out_file = ARTIFACT_DIR / "CornerHousesFinalUnifiedArchitecture.png"
canvas.save(out_file, optimize=True)
print(f"Generated final showcase: {out_file}")
