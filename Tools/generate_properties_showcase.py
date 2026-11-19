import os
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw, ImageFont

ARTIFACT_DIR = Path(r"C:\Users\Admin\.gemini\antigravity\brain\1ede9fe0-845a-447d-89c1-c412754fe314")
ICONS_DIR = Path("Assets/_Ropoly/Resources/Icons")
TILES_DIR = Path("Assets/_Ropoly/Resources/CityTiles")

CANVAS_W = 1640
CANVAS_H = 1160
canvas = Image.new("RGB", (CANVAS_W, CANVAS_H), (14, 13, 20))
draw = ImageDraw.Draw(canvas)

try:
    font_header = ImageFont.truetype("arialbd.ttf", 28)
    font_sub = ImageFont.truetype("arial.ttf", 15)
    font_section = ImageFont.truetype("arialbd.ttf", 18)
    font_title = ImageFont.truetype("arialbd.ttf", 15)
    font_price = ImageFont.truetype("arialbd.ttf", 15)
    font_tag = ImageFont.truetype("arialbd.ttf", 12)
    font_state = ImageFont.truetype("arialbd.ttf", 11)
except Exception:
    font_header = font_sub = font_section = font_title = font_price = font_tag = font_state = ImageFont.load_default()

# Header
draw.text((40, 22), "ALL PURCHASABLE PROPERTIES: UNIFIED PROPERTY CARD ARCHITECTURE", fill=(255, 255, 255), font=font_header)
draw.text((40, 58), "Airports ($200), Utilities ($150) & Cities - Soft Themed Backdrops, Straight Upright Icons & Dynamic Ownership Bands", fill=(175, 170, 205), font=font_sub)

# Section 1: All 4 Airports & 2 Utilities
draw.text((40, 98), "1. AIRPORTS & UTILITIES (All 6 Non-City Houses)", fill=(90, 195, 255), font=font_section)

# 6 properties with layout info
# (Title, Price, Icon, Panel, Edge tag, is_portrait, (x, y), is_owned, owner_color, owner_label)
PROPS_6 = [
    # Top Row: JFK, Paris, Electric (Portrait edge tiles: Bottom & Top)
    ("JFK AIRPORT", "$200", "Airport_White.png", "AirportPanel_Blur.png", "Tile 05 (Bottom Edge - JFK)", True, (50, 145), False, None, "UNOWNED"),
    ("PARIS AIRPORT", "$200", "Airport_White.png", "AirportPanel_Blur.png", "Tile 25 (Top Edge - CDG)", True, (280, 145), True, (56, 175, 245), "OWNED BY CYBER CAT (PLAYER 1)"),
    ("ELECTRIC NETWORK", "$150", "Electric_White.png", "ElectricPanel_Blur.png", "Tile 28 (Top Edge - Electric)", True, (510, 145), False, None, "UNOWNED"),
    
    # Left & Right Edge: Heathrow, Water, Beijing (Landscape edge tiles)
    ("HEATHROW AIRPORT", "$200", "Airport_White.png", "AirportPanel_Wide_Blur.png", "Tile 15 (Left Edge - London)", False, (770, 145), True, (235, 75, 95), "OWNED BY MECHA FOX (PLAYER 2)"),
    ("WATER SERVICE", "$150", "Water_White.png", "WaterPanel_Wide_Blur.png", "Tile 12 (Left Edge - Water)", False, (1180, 145), False, None, "UNOWNED"),
    ("BEIJING AIRPORT", "$200", "Airport_White.png", "AirportPanel_Wide_Blur.png", "Tile 35 (Right Edge - Beijing)", False, (770, 385), False, None, "UNOWNED"),
    ("ELECTRIC NETWORK (OWNED)", "$150", "Electric_White.png", "ElectricPanel_Wide_Blur.png", "Tile 28 (Dynamic Player Color)", False, (1180, 385), True, (72, 205, 135), "OWNED BY DINO DRAGON (PLAYER 3)"),
]

def render_property_card(title, price, icon_name, panel_name, is_portrait, is_owned, owner_color, card_w, card_h):
    card = Image.new("RGBA", (card_w, card_h), (22, 20, 32, 255))
    cdraw = ImageDraw.Draw(card)
    
    band_h = 44 if is_portrait else 36
    rule_h = 3
    panel_h = card_h - band_h - rule_h - 10
    panel_w = card_w - 10
    
    panel_x = 5
    panel_y = 5
    rule_y = panel_y + panel_h + 2
    band_y = rule_y + rule_h + 2
    
    # Backdrop panel
    panel_path = TILES_DIR / panel_name
    if panel_path.exists():
        panel_img = Image.open(panel_path).convert("RGBA").resize((panel_w, panel_h), Image.Resampling.LANCZOS)
        card.paste(panel_img, (panel_x, panel_y), panel_img)
    else:
        cdraw.rectangle([panel_x, panel_y, panel_x + panel_w, panel_y + panel_h], fill=(25, 40, 65))
    
    # Dark outer border
    cdraw.rectangle([0, 0, card_w - 1, card_h - 1], outline=(55, 50, 75), width=2)
    
    # Ownership band
    band_color = owner_color if (is_owned and owner_color is not None) else (42, 38, 58)
    cdraw.rectangle([panel_x, band_y, panel_x + panel_w, band_y + band_h], fill=band_color)
    
    # Metallic rule line
    cdraw.rectangle([panel_x, rule_y, panel_x + panel_w, rule_y + rule_h], fill=(225, 215, 185))
    
    # Price text in band
    p_bbox = cdraw.textbbox((0, 0), price, font=font_price)
    pw = p_bbox[2] - p_bbox[0]
    ph = p_bbox[3] - p_bbox[1]
    px = panel_x + (panel_w - pw) // 2
    py = band_y + (band_h - ph) // 2 - 2
    cdraw.text((px + 1, py + 1), price, fill=(10, 10, 20), font=font_price)
    cdraw.text((px, py), price, fill=(255, 255, 255), font=font_price)
    
    # Title parsing
    lines = title.replace(" (OWNED)", "").split("\n") if "\n" in title else [title.replace(" (OWNED)", "")]
    if len(lines) == 1 and len(lines[0]) > 9 and " " in lines[0]:
        lines = lines[0].split(" ", 1)

    # Icon
    if icon_name:
        icon_path = ICONS_DIR / icon_name
        if icon_path.exists():
            raw_icon = Image.open(icon_path).convert("RGBA")
            icon_sz = 48 if is_portrait else 36
            icon_res = raw_icon.resize((icon_sz, icon_sz), Image.Resampling.LANCZOS)
            
            # Ensure crisp white
            iarr = np.array(icon_res)
            warr = np.zeros_like(iarr)
            warr[..., :3] = 255
            warr[..., 3] = iarr[..., 3]
            wicon = Image.fromarray(warr, "RGBA")
            
            ix = panel_x + (panel_w - icon_sz) // 2
            iy = panel_y + (16 if is_portrait else 8)
            card.paste(wicon, (ix, iy), wicon)
            text_y = panel_y + (76 if is_portrait else 50)
    else:
        # Centered on flag face (e.g. city)
        text_y = panel_y + (panel_h - len(lines) * 20) // 2
        
    for l_idx, line in enumerate(lines):
        t_bbox = cdraw.textbbox((0, 0), line, font=font_title)
        tw = t_bbox[2] - t_bbox[0]
        tx = panel_x + (panel_w - tw) // 2
        ty = text_y + l_idx * 20
        cdraw.text((tx + 2, ty + 2), line, fill=(8, 8, 16), font=font_title)
        cdraw.text((tx, ty), line, fill=(255, 255, 255), font=font_title)
        
    return card

for title, price, icon_name, panel_name, tag, is_portrait, (x, y), is_owned, owner_color, state_label in PROPS_6:
    cw = 210 if is_portrait else 380
    ch = 300 if is_portrait else 195
    
    draw.text((x, y - 20), tag, fill=(185, 180, 215), font=font_tag)
    
    state_color = (120, 220, 160) if is_owned else (170, 165, 195)
    draw.text((x, y + ch + 5), state_label, fill=state_color, font=font_state)
    
    card = render_property_card(title, price, icon_name, panel_name, is_portrait, is_owned, owner_color, cw, ch)
    canvas.paste(card, (x, y))

# Section 2: Unified Design Across Cities vs Airports vs Utilities
draw.text((40, 620), "2. UNIFIED DESIGN SYSTEM: CITIES & NON-CITY HOUSES SIDE BY SIDE", fill=(255, 210, 80), font=font_section)

CITIES_COMPARE = [
    ("MILAN", "$160", "ItalyFlagPanel_Blur.png", "Tile 24 (Italy)", True, False, None, "UNOWNED CITY"),
    ("PARIS", "$260", "FranceFlagPanel_Blur.png", "Tile 27 (France)", True, True, (56, 175, 245), "PLAYER 1 OWNED"),
    ("JFK AIRPORT", "$200", "AirportPanel_Blur.png", "Tile 05 (Airport)", True, False, None, "UNOWNED AIRPORT"),
    ("ELECTRIC NETWORK", "$150", "ElectricPanel_Blur.png", "Tile 28 (Electric)", True, True, (72, 205, 135), "PLAYER 3 OWNED"),
    ("LONDON", "$320", "UnitedKingdomFlagPanel_Wide_Blur.png", "Tile 34 (UK)", False, True, (235, 75, 95), "PLAYER 2 OWNED"),
    ("WATER SERVICE", "$150", "WaterPanel_Wide_Blur.png", "Tile 12 (Water)", False, False, None, "UNOWNED UTILITY"),
]

for idx, (title, price, panel_name, tag, is_portrait, is_owned, owner_color, state_label) in enumerate(CITIES_COMPARE):
    col = idx
    if col < 4:
        # 4 portrait tiles
        x = 50 + col * 230
        y = 660
        cw = 210
        ch = 290
        icon_name = None
        if "AIRPORT" in title:
            icon_name = "Airport_White.png"
        elif "ELECTRIC" in title:
            icon_name = "Electric_White.png"
        
        draw.text((x, y - 18), tag, fill=(185, 180, 215), font=font_tag)
        draw.text((x, y + ch + 5), state_label, fill=(120, 220, 160) if is_owned else (170, 165, 195), font=font_state)
        card = render_property_card(title, price, icon_name, panel_name, True, is_owned, owner_color, cw, ch)
        canvas.paste(card, (x, y))
    else:
        # 2 landscape tiles
        col_l = col - 4
        x = 1000
        y = 660 + col_l * 205
        cw = 580
        ch = 145
        icon_name = "Water_White.png" if "WATER" in title else None
        
        draw.text((x, y - 18), tag, fill=(185, 180, 215), font=font_tag)
        draw.text((x, y + ch + 4), state_label, fill=(120, 220, 160) if is_owned else (170, 165, 195), font=font_state)
        card = render_property_card(title, price, icon_name, panel_name, False, is_owned, owner_color, cw, ch)
        canvas.paste(card, (x, y))

out_path = ARTIFACT_DIR / "AirportsAndUtilitiesShowcase.png"
canvas.save(out_path)
print("Saved comprehensive showcase to:", out_path)
