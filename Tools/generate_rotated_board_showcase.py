import os
from PIL import Image, ImageDraw, ImageFont

def render_showcase():
    output_path = r"C:\Users\Admin\.gemini\antigravity\brain\1ede9fe0-845a-447d-89c1-c412754fe314\RotatedUniformTilesJapanShowcase.png"
    w, h = 1600, 1600
    im = Image.new("RGBA", (w, h), (14, 10, 26, 255))
    draw = ImageDraw.Draw(im)

    try:
        font_large = ImageFont.truetype("arialbd.ttf", 36)
        font_med = ImageFont.truetype("arialbd.ttf", 22)
        font_small = ImageFont.truetype("arial.ttf", 16)
        font_bold_small = ImageFont.truetype("arialbd.ttf", 16)
    except:
        font_large = ImageFont.load_default()
        font_med = ImageFont.load_default()
        font_small = ImageFont.load_default()
        font_bold_small = ImageFont.load_default()

    # Board dimensions in pixels
    # 18m board -> 1360px (margin 120px)
    board_px = 1360
    ox, oy = 120, 120
    corner_px = int(board_px * (2.4 / 18.0)) # ~181px
    edge_slot = (board_px - 2 * corner_px) / 9.0 # ~110.88px

    # Draw board backing
    draw.rectangle([ox, oy, ox + board_px, oy + board_px], fill=(22, 17, 36, 255), outline=(50, 42, 75, 255), width=3)
    inner_ox = ox + corner_px
    inner_oy = oy + corner_px
    inner_w = board_px - 2 * corner_px
    draw.rectangle([inner_ox, inner_oy, inner_ox + inner_w, inner_oy + inner_w], fill=(12, 9, 22, 255), outline=(40, 32, 60, 255), width=2)

    # Center branding
    cx, cy = ox + board_px // 2, oy + board_px // 2
    draw.text((cx, cy - 60), "ROPOLY", font=font_large, fill=(255, 255, 255, 240), anchor="mm")
    draw.text((cx, cy - 15), "CLASSIC WORLD", font=font_med, fill=(80, 230, 210, 230), anchor="mm")
    draw.text((cx, cy + 25), "22 CITIES  •  8 COUNTRY SETS  •  ROTATED CARDS", font=font_small, fill=(180, 175, 205, 200), anchor="mm")

    # Tile Data: (index, side, name, price, flag_color, band_color, is_corner, is_special)
    # Side: 0=Bottom, 1=Left, 2=Top, 3=Right
    # Flag color for Japan: Red sun disc on white
    jp_blue = (52, 152, 219, 255) # Blue set color

    # Edge and corner tiles definition
    # 0: Start (Corner, Bottom-Right)
    # 1: Delhi (India), 2: Bengaluru (India), 3: World Event, 4: Mumbai (India), 5: JFK Airport, 6: Fee, 7: Local Opp, 8: Brasilia (Brazil), 9: Rio (Brazil)
    # 10: Detention (Corner, Bottom-Left)
    # 11: Sao Paulo (Brazil), 12: Water, 13: Beijing (China), 14: Shenzhen (China), 15: Heathrow, 16: Shanghai (China), 17: Local Opp, 18: Kyoto (Japan), 19: Osaka (Japan)
    # 20: Vacation (Corner, Top-Left)
    # 21: Tokyo (Japan), 22: World Event, 23: Rome (Italy), 24: Milan (Italy), 25: Paris Airport, 26: Fee, 27: Marseille (France), 28: Paris (France), 29: Local Opp
    # 30: Go To Detention (Corner, Top-Right)
    # 31: Liverpool (UK), 32: Manchester (UK), 33: London (UK), 34: Beijing Airport, 35: Electric, 36: Fee, 37: SF (USA), 38: LA (USA), 39: NY (USA)

    def draw_corner(x, y, size, title, icon_symbol, band_text):
        draw.rectangle([x + 3, y + 3, x + size - 3, y + size - 3], fill=(28, 22, 46, 255), outline=(65, 55, 95, 255), width=2)
        # Band at bottom
        band_h = 36
        draw.rectangle([x + 6, y + size - 6 - band_h, x + size - 6, y + size - 6], fill=(45, 42, 62, 255))
        draw.text((x + size // 2, y + size - 6 - band_h // 2), band_text, font=font_bold_small, fill=(240, 240, 240, 255), anchor="mm")
        # Rule
        draw.line([x + 6, y + size - 6 - band_h, x + size - 6, y + size - 6 - band_h], fill=(220, 210, 180, 180), width=1)
        # Title
        draw.text((x + size // 2, y + 26), title, font=font_med, fill=(255, 255, 255, 255), anchor="mm")
        # Center Icon
        draw.text((x + size // 2, y + 80), icon_symbol, font=font_large, fill=(240, 240, 255, 240), anchor="mm")

    # Corner 0: Start (Bottom-Right)
    draw_corner(ox + board_px - corner_px, oy + board_px - corner_px, corner_px, "START", "▶", "COLLECT $200")
    # Corner 10: Detention (Bottom-Left)
    draw_corner(ox, oy + board_px - corner_px, corner_px, "PRISON", "⚑", "JUST VISITING")
    # Corner 20: Vacation (Top-Left)
    draw_corner(ox, oy, corner_px, "VACATION", "☀", "FREE REST")
    # Corner 30: Go To Detention (Top-Right)
    draw_corner(ox + board_px - corner_px, oy, corner_px, "LOCK UP", "⚠", "DETENTION")

    # Draw edge card
    def draw_edge_card(side, slot_idx, name, price, set_color, is_japan=False, icon_symbol=None):
        card_w = edge_slot - 4
        card_len = corner_px - 4
        band_len = 34
        flag_len = card_len - band_len - 4

        # Card local canvas
        card_im = Image.new("RGBA", (int(card_w), int(card_len)), (28, 22, 46, 255))
        cdraw = ImageDraw.Draw(card_im)
        cdraw.rectangle([0, 0, int(card_w) - 1, int(card_len) - 1], fill=(26, 20, 42, 255), outline=(58, 48, 85, 255), width=1)

        # Flag area (top of card in local space)
        flag_y0 = 2
        flag_y1 = flag_y0 + int(flag_len)
        cdraw.rectangle([2, flag_y0, int(card_w) - 3, flag_y1], fill=(18, 14, 30, 255))

        # Flag texture representation
        if is_japan:
            # Japanese flag: white field with red sun disc
            cdraw.rectangle([4, flag_y0 + 2, int(card_w) - 5, flag_y1 - 2], fill=(235, 235, 240, 180))
            fcx = int(card_w) // 2
            fcy = (flag_y0 + flag_y1) // 2 - 12
            r = 22
            cdraw.ellipse([fcx - r, fcy - r, fcx + r, fcy + r], fill=(188, 0, 45, 230))
        elif set_color:
            # Soft national color band
            cdraw.rectangle([4, flag_y0 + 2, int(card_w) - 5, flag_y1 - 2], fill=(set_color[0]//3, set_color[1]//3, set_color[2]//3, 160))

        if icon_symbol:
            cdraw.text((int(card_w) // 2, (flag_y0 + flag_y1) // 2 - 14), icon_symbol, font=font_large, fill=(255, 255, 255, 230), anchor="mm")

        # Name text
        ny = (flag_y0 + flag_y1) // 2 + 20 if (is_japan or icon_symbol) else (flag_y0 + flag_y1) // 2
        cdraw.text((int(card_w) // 2, ny), name, font=font_bold_small, fill=(255, 255, 255, 255), anchor="mm")

        # Divider rule
        rule_y = flag_y1 + 1
        cdraw.line([2, rule_y, int(card_w) - 3, rule_y], fill=(220, 210, 180, 180), width=1)

        # Ownership band (bottom of card in local space = inner edge)
        band_y0 = rule_y + 2
        band_y1 = int(card_len) - 2
        band_fill = set_color if set_color else (45, 42, 62, 255)
        cdraw.rectangle([2, band_y0, int(card_w) - 3, band_y1], fill=band_fill)
        cdraw.text((int(card_w) // 2, (band_y0 + band_y1) // 2), price, font=font_bold_small, fill=(255, 255, 255, 255), anchor="mm")

        # Rotate card based on side:
        # Side 0 (Bottom): faces North (+Z). Local top is towards table rim, band is towards center.
        # Wait, band must face board center!
        # In our local card, band is at bottom.
        # Bottom edge: band is at inner edge (towards top/North), flag is at outer edge (towards bottom/South).
        # So Bottom edge card is rotated 180 deg so band is at inner edge!
        if side == 0: # Bottom edge
            # Rotate 180 so band faces inward (+Z/North)
            rotated = card_im.rotate(180, expand=True)
            px = ox + board_px - corner_px - int((slot_idx + 1) * edge_slot) + 2
            py = oy + board_px - corner_px + 2
        elif side == 1: # Left edge
            # Inward is East (+X). Rotated 90 deg clockwise so band is on right side (inward)
            rotated = card_im.rotate(270, expand=True)
            px = ox + 2
            py = oy + board_px - corner_px - int((slot_idx + 1) * edge_slot) + 2
        elif side == 2: # Top edge
            # Inward is South (-Z). Band is at bottom side (inward)
            rotated = card_im # band is already at bottom
            px = ox + corner_px + int(slot_idx * edge_slot) + 2
            py = oy + 2
        elif side == 3: # Right edge
            # Inward is West (-X). Rotated 90 deg counter-clockwise so band is on left side (inward)
            rotated = card_im.rotate(90, expand=True)
            px = ox + board_px - corner_px + 2
            py = oy + corner_px + int(slot_idx * edge_slot) + 2

        im.paste(rotated, (int(px), int(py)), rotated)

    # Bottom edge (1..9, index 0..8 from right to left)
    draw_edge_card(0, 0, "DELHI", "$60", (160, 120, 50, 255))
    draw_edge_card(0, 1, "EVENT", "DRAW", None, icon_symbol="?")
    draw_edge_card(0, 2, "BENGALURU", "$70", (160, 120, 50, 255))
    draw_edge_card(0, 3, "TAX", "$200", None, icon_symbol="⚙")
    draw_edge_card(0, 4, "JFK AIRPORT", "$200", None, icon_symbol="✈")
    draw_edge_card(0, 5, "MUMBAI", "$80", (160, 120, 50, 255))
    draw_edge_card(0, 6, "OPPORTUNITY", "DRAW", None, icon_symbol="★")
    draw_edge_card(0, 7, "BRASÍLIA", "$100", (220, 160, 30, 255))
    draw_edge_card(0, 8, "RIO", "$110", (220, 160, 30, 255))

    # Left edge (11..19, index 0..8 from bottom to top)
    draw_edge_card(1, 0, "SÃO PAULO", "$120", (220, 160, 30, 255))
    draw_edge_card(1, 1, "WATER", "$150", None, icon_symbol="💧")
    draw_edge_card(1, 2, "BEIJING", "$140", (210, 60, 50, 255))
    draw_edge_card(1, 3, "SHENZHEN", "$150", (210, 60, 50, 255))
    draw_edge_card(1, 4, "HEATHROW", "$200", None, icon_symbol="✈")
    draw_edge_card(1, 5, "SHANGHAI", "$160", (210, 60, 50, 255))
    draw_edge_card(1, 6, "EVENT", "DRAW", None, icon_symbol="?")
    draw_edge_card(1, 7, "KYOTO", "$180", jp_blue, is_japan=True)
    draw_edge_card(1, 8, "OSAKA", "$190", jp_blue, is_japan=True)

    # Top edge (21..29, index 0..8 from left to right)
    draw_edge_card(2, 0, "TOKYO", "$200", jp_blue, is_japan=True)
    draw_edge_card(2, 1, "OPPORTUNITY", "DRAW", None, icon_symbol="★")
    draw_edge_card(2, 2, "ROME", "$220", (39, 174, 96, 255))
    draw_edge_card(2, 3, "MILAN", "$230", (39, 174, 96, 255))
    draw_edge_card(2, 4, "CDG AIRPORT", "$200", None, icon_symbol="✈")
    draw_edge_card(2, 5, "MARSEILLE", "$240", (41, 128, 185, 255))
    draw_edge_card(2, 6, "PARIS", "$260", (41, 128, 185, 255))
    draw_edge_card(2, 7, "ELECTRIC", "$150", None, icon_symbol="⚡")
    draw_edge_card(2, 8, "EVENT", "DRAW", None, icon_symbol="?")

    # Right edge (31..39, index 0..8 from top to bottom)
    draw_edge_card(3, 0, "LIVERPOOL", "$280", (155, 89, 182, 255))
    draw_edge_card(3, 1, "MANCHESTER", "$290", (155, 89, 182, 255))
    draw_edge_card(3, 2, "OPPORTUNITY", "DRAW", None, icon_symbol="★")
    draw_edge_card(3, 3, "LONDON", "$310", (155, 89, 182, 255))
    draw_edge_card(3, 4, "PEK AIRPORT", "$200", None, icon_symbol="✈")
    draw_edge_card(3, 5, "TAX", "$100", None, icon_symbol="⚙")
    draw_edge_card(3, 6, "SAN FRANCISCO", "$330", (52, 73, 94, 255))
    draw_edge_card(3, 7, "LOS ANGELES", "$380", (52, 73, 94, 255))
    draw_edge_card(3, 8, "NEW YORK", "$400", (52, 73, 94, 255))

    im.save(output_path, "PNG")
    print(f"Showcase saved to {output_path}")

if __name__ == "__main__":
    render_showcase()
