import os
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw, ImageFont

ARTIFACT_DIR = Path(r"C:\Users\Admin\.gemini\antigravity\brain\1ede9fe0-845a-447d-89c1-c412754fe314")
TILES_DIR = Path("Assets/_Ropoly/Resources/CityTiles")
ICONS_DIR = Path("Assets/_Ropoly/Resources/Icons")

CARD_SIZE = 500
CANVAS_W = CARD_SIZE * 4 + 100
CANVAS_H = CARD_SIZE * 2 + 220

canvas = Image.new("RGB", (CANVAS_W, CANVAS_H), (16, 15, 24))
draw = ImageDraw.Draw(canvas)

try:
    font_header = ImageFont.truetype("arialbd.ttf", 26)
    font_sub = ImageFont.truetype("arial.ttf", 15)
    font_section = ImageFont.truetype("arialbd.ttf", 18)
    font_title = ImageFont.truetype("arialbd.ttf", 26)
    font_title_small = ImageFont.truetype("arialbd.ttf", 22)
    font_band = ImageFont.truetype("arialbd.ttf", 18)
except Exception:
    font_header = font_sub = font_section = font_title = font_title_small = font_band = ImageFont.load_default()

draw.text((40, 20), "CORNER HOUSES REDESIGN: 4 CORNER CARDS IN EXACT SAME FORMAT", fill=(255, 255, 255), font=font_header)
draw.text((40, 56), "Unified Card Architecture: Themed Blurred Panel, Bold Title w/ Drop Shadow, Upright Icon, Metallic Rule & Action Band", fill=(175, 170, 205), font=font_sub)

CORNERS = [
    ("START", "COLLECT $200", "Start", "StartPanel_Blur.png"),
    ("PRISON", "JUST VISITING", "Detention", "DetentionPanel_Blur.png"),
    ("VACATION", "FREE REST", "Vacation", "VacationPanel_Blur.png"),
    ("GO TO PRISON", "LOCK UP", "GoToDetention", "GoToDetentionPanel_Blur.png"),
]

def render_corner_card(title, band_text, icon_base, panel_name, use_white_icon=True):
    card = Image.new("RGBA", (CARD_SIZE, CARD_SIZE), (24, 22, 34, 255))
    cdraw = ImageDraw.Draw(card)
    
    pad = 12
    band_h = 68
    rule_h = 4
    panel_h = CARD_SIZE - (pad * 2) - band_h - rule_h - 4
    panel_w = CARD_SIZE - (pad * 2)
    
    panel_x = pad
    panel_y = pad
    rule_y = panel_y + panel_h + 2
    band_y = rule_y + rule_h + 2
    
    # 1. Backdrop panel
    panel_path = TILES_DIR / panel_name
    if panel_path.exists():
        panel_img = Image.open(panel_path).convert("RGBA").resize((panel_w, panel_h), Image.Resampling.LANCZOS)
        p_arr = np.array(panel_img).astype(np.float32)
        p_arr[..., 3] *= 0.65
        panel_tinted = Image.fromarray(np.uint8(p_arr), "RGBA")
        card.paste(panel_tinted, (panel_x, panel_y), panel_tinted)
        
    # 2. Rule line (cream metallic)
    cdraw.rectangle([panel_x, rule_y, panel_x + panel_w, rule_y + rule_h], fill=(225, 215, 185, 200))
    
    # 3. Bottom band (slate)
    cdraw.rectangle([panel_x, band_y, panel_x + panel_w, band_y + band_h], fill=(42, 38, 58, 255))
    
    # Band text
    bbox = font_band.getbbox(band_text)
    tw = bbox[2] - bbox[0]
    th = bbox[3] - bbox[1]
    tx = panel_x + (panel_w - tw) // 2
    ty = band_y + (band_h - th) // 2
    cdraw.text((tx, ty), band_text, fill=(255, 255, 255), font=font_band)
    
    # 4. Title at Top
    f_title = font_title_small if len(title) > 10 else font_title
    t_bbox = f_title.getbbox(title)
    ttw = t_bbox[2] - t_bbox[0]
    tth = t_bbox[3] - t_bbox[1]
    
    title_y = panel_y + 20
    title_x = panel_x + (panel_w - ttw) // 2
    # Drop shadow
    cdraw.text((title_x + 2, title_y + 2), title, fill=(10, 8, 16, 240), font=f_title)
    cdraw.text((title_x, title_y), title, fill=(255, 255, 255), font=f_title)
    
    # 5. Icon in Center
    icon_filename = f"{icon_base}_White.png" if use_white_icon else f"{icon_base}.png"
    icon_path = ICONS_DIR / icon_filename
    if icon_path.exists():
        icon_img = Image.open(icon_path).convert("RGBA")
        icon_size = 190
        ic_resized = icon_img.resize((icon_size, icon_size), Image.Resampling.LANCZOS)
        ic_x = panel_x + (panel_w - icon_size) // 2
        ic_y = panel_y + 20 + tth + (panel_h - (20 + tth) - icon_size) // 2
        card.paste(ic_resized, (ic_x, ic_y), ic_resized)
        
    return card

# Row 1: Crisp White Silhouette Icons
draw.text((40, 96), "OPTION 1: CRISP WHITE SILHOUETTE ICONS (Matches Airports & Utilities)", fill=(90, 195, 255), font=font_section)
for i, (title, b_text, icon_b, panel_n) in enumerate(CORNERS):
    c_img = render_corner_card(title, b_text, icon_b, panel_n, use_white_icon=True)
    pos = (40 + i * (CARD_SIZE + 20), 130)
    canvas.paste(c_img, pos)

# Row 2: Full-Color Original Icons
draw.text((40, 130 + CARD_SIZE + 24), "OPTION 2: FULL-COLOR ORIGINAL ICONS (Vibrant Themed Accents)", fill=(245, 185, 55), font=font_section)
for i, (title, b_text, icon_b, panel_n) in enumerate(CORNERS):
    c_img = render_corner_card(title, b_text, icon_b, panel_n, use_white_icon=False)
    pos = (40 + i * (CARD_SIZE + 20), 130 + CARD_SIZE + 56)
    canvas.paste(c_img, pos)

out_path = ARTIFACT_DIR / "CornerHousesUnifiedShowcase.png"
canvas.save(out_path, optimize=True)
print(f"Saved corner showcase comparison to {out_path}")
