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
        font_price = ImageFont.truetype("arialbd.ttf", 15)
    except:
        font_large = ImageFont.load_default()
        font_med = ImageFont.load_default()
        font_small = ImageFont.load_default()
        font_bold_small = ImageFont.load_default()
        font_price = ImageFont.load_default()

    # Board dimensions in pixels
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
    draw.text((cx, cy - 65), "ROPOLY", font=font_large, fill=(255, 255, 255, 240), anchor="mm")
    draw.text((cx, cy - 20), "CLASSIC WORLD", font=font_med, fill=(80, 230, 210, 230), anchor="mm")
    draw.text((cx, cy + 18), "22 CITIES  •  8 COUNTRY SETS  •  ZERO SPLIT COUNTRIES", font=font_small, fill=(180, 175, 205, 200), anchor="mm")
    draw.text((cx, cy + 42), "UPPER SIDE FACES PLAYER  •  LEFT PRICE ROTATED 180°", font=font_small, fill=(240, 180, 80, 220), anchor="mm")

    jp_blue = (52, 152, 219, 255)
    it_orange = (243, 156, 18, 255)
    fr_pink = (232, 67, 147, 255)
    cn_red = (192, 57, 43, 255)
    br_gold = (22, 160, 133, 255)
    in_amber = (180, 130, 40, 255)
    uk_purple = (155, 89, 182, 255)
    us_blue = (52, 73, 94, 255)

    def draw_corner(x, y, size, title, icon_symbol, band_text):
        draw.rectangle([x + 3, y + 3, x + size - 3, y + size - 3], fill=(28, 22, 46, 255), outline=(65, 55, 95, 255), width=2)
        band_h = 36
        draw.rectangle([x + 6, y + size - 6 - band_h, x + size - 6, y + size - 6], fill=(45, 42, 62, 255))
        draw.text((x + size // 2, y + size - 6 - band_h // 2), band_text, font=font_bold_small, fill=(240, 240, 240, 255), anchor="mm")
        draw.line([x + 6, y + size - 6 - band_h, x + size - 6, y + size - 6 - band_h], fill=(220, 210, 180, 180), width=1)
        draw.text((x + size // 2, y + 26), title, font=font_med, fill=(255, 255, 255, 255), anchor="mm")
        draw.text((x + size // 2, y + 80), icon_symbol, font=font_large, fill=(240, 240, 255, 240), anchor="mm")

    # Corner 0: Start (Bottom-Right)
    draw_corner(ox + board_px - corner_px, oy + board_px - corner_px, corner_px, "START", "▶", "COLLECT $200")
    # Corner 10: Detention (Bottom-Left)
    draw_corner(ox, oy + board_px - corner_px, corner_px, "PRISON", "⚑", "JUST VISITING")
    # Corner 20: Vacation (Top-Left)
    draw_corner(ox, oy, corner_px, "VACATION", "☀", "FREE REST")
    # Corner 30: Go To Detention (Top-Right)
    draw_corner(ox + board_px - corner_px, oy, corner_px, "LOCK UP", "⚠", "DETENTION")

    def draw_edge_card(side, slot_idx, name, price, set_color, is_japan=False, icon_symbol=None):
        card_w = edge_slot - 4
        card_len = corner_px - 4
        band_len = 34
        flag_len = card_len - band_len - 4

        card_im = Image.new("RGBA", (int(card_w), int(card_len)), (28, 22, 46, 255))
        cdraw = ImageDraw.Draw(card_im)
        cdraw.rectangle([0, 0, int(card_w) - 1, int(card_len) - 1], fill=(26, 20, 42, 255), outline=(58, 48, 85, 255), width=1)

        if side == 2:
            # Top edge: flag on top (outer rim), band on bottom (inner rim)
            flag_y0 = 2
            flag_y1 = flag_y0 + int(flag_len)
            cdraw.rectangle([2, flag_y0, int(card_w) - 3, flag_y1], fill=(18, 14, 30, 255))
            if set_color:
                cdraw.rectangle([4, flag_y0 + 2, int(card_w) - 5, flag_y1 - 2], fill=(set_color[0]//3, set_color[1]//3, set_color[2]//3, 160))

            if icon_symbol:
                cdraw.text((int(card_w) // 2, flag_y0 + 35), icon_symbol, font=font_large, fill=(255, 255, 255, 230), anchor="mm")
                cdraw.text((int(card_w) // 2, flag_y0 + 82), name, font=font_bold_small, fill=(255, 255, 255, 255), anchor="mm")
            else:
                cdraw.text((int(card_w) // 2, (flag_y0 + flag_y1) // 2), name, font=font_bold_small, fill=(255, 255, 255, 255), anchor="mm")

            rule_y = flag_y1 + 1
            cdraw.line([2, rule_y, int(card_w) - 3, rule_y], fill=(220, 210, 180, 180), width=1)
            band_y0 = rule_y + 2
            band_y1 = int(card_len) - 2
            band_fill = set_color if set_color else (45, 42, 62, 255)
            cdraw.rectangle([2, band_y0, int(card_w) - 3, band_y1], fill=band_fill)
            # Price faces player directly upright
            cdraw.text((int(card_w) // 2, (band_y0 + band_y1) // 2), price, font=font_price, fill=(255, 255, 255, 255), anchor="mm")

            px = ox + corner_px + int(slot_idx * edge_slot) + 2
            py = oy + 2
            im.paste(card_im, (int(px), int(py)), card_im)
            return

        flag_y0 = 2
        flag_y1 = flag_y0 + int(flag_len)
        cdraw.rectangle([2, flag_y0, int(card_w) - 3, flag_y1], fill=(18, 14, 30, 255))

        if is_japan:
            cdraw.rectangle([4, flag_y0 + 2, int(card_w) - 5, flag_y1 - 2], fill=(235, 235, 240, 180))
            fcx = int(card_w) // 2
            fcy = (flag_y0 + flag_y1) // 2 - 12
            r = 22
            cdraw.ellipse([fcx - r, fcy - r, fcx + r, fcy + r], fill=(188, 0, 45, 230))
        elif set_color:
            cdraw.rectangle([4, flag_y0 + 2, int(card_w) - 5, flag_y1 - 2], fill=(set_color[0]//3, set_color[1]//3, set_color[2]//3, 160))

        if icon_symbol:
            cdraw.text((int(card_w) // 2, (flag_y0 + flag_y1) // 2 - 14), icon_symbol, font=font_large, fill=(255, 255, 255, 230), anchor="mm")

        ny = (flag_y0 + flag_y1) // 2 + 20 if (is_japan or icon_symbol) else (flag_y0 + flag_y1) // 2
        cdraw.text((int(card_w) // 2, ny), name, font=font_bold_small, fill=(255, 255, 255, 255), anchor="mm")

        rule_y = flag_y1 + 1
        cdraw.line([2, rule_y, int(card_w) - 3, rule_y], fill=(220, 210, 180, 180), width=1)

        band_y0 = rule_y + 2
        band_y1 = int(card_len) - 2
        band_fill = set_color if set_color else (45, 42, 62, 255)
        cdraw.rectangle([2, band_y0, int(card_w) - 3, band_y1], fill=band_fill)

        # Left edge: price is rotated 180 degrees
        if side == 1:
            pw = int(card_w) - 6
            ph = band_y1 - band_y0
            price_im = Image.new("RGBA", (pw, ph), (0, 0, 0, 0))
            pdraw = ImageDraw.Draw(price_im)
            pdraw.text((pw // 2, ph // 2), price, font=font_price, fill=(255, 255, 255, 255), anchor="mm")
            price_rot = price_im.rotate(180)
            card_im.paste(price_rot, (3, band_y0), price_rot)
        else:
            cdraw.text((int(card_w) // 2, (band_y0 + band_y1) // 2), price, font=font_price, fill=(255, 255, 255, 255), anchor="mm")

        if side == 0: # Bottom edge
            rotated = card_im.rotate(180, expand=True)
            px = ox + board_px - corner_px - int((slot_idx + 1) * edge_slot) + 2
            py = oy + board_px - corner_px + 2
        elif side == 1: # Left edge
            rotated = card_im.rotate(270, expand=True)
            px = ox + 2
            py = oy + board_px - corner_px - int((slot_idx + 1) * edge_slot) + 2
        elif side == 3: # Right edge
            rotated = card_im.rotate(90, expand=True)
            px = ox + board_px - corner_px + 2
            py = oy + corner_px + int(slot_idx * edge_slot) + 2

        im.paste(rotated, (int(px), int(py)), rotated)

    # 1. Bottom edge (India 3 cities, Brazil 2 cities)
    draw_edge_card(0, 0, "DELHI", "$60", in_amber)
    draw_edge_card(0, 1, "EVENT", "DRAW", None, icon_symbol="?")
    draw_edge_card(0, 2, "BENGALURU", "$70", in_amber)
    draw_edge_card(0, 3, "TAX", "$200", None, icon_symbol="⚙")
    draw_edge_card(0, 4, "JFK AIRPORT", "$200", None, icon_symbol="✈")
    draw_edge_card(0, 5, "MUMBAI", "$80", in_amber)
    draw_edge_card(0, 6, "OPPORTUNITY", "DRAW", None, icon_symbol="★")
    draw_edge_card(0, 7, "BRASÍLIA", "$100", br_gold)
    draw_edge_card(0, 8, "RIO", "$110", br_gold)

    # 2. Left edge (Water Service, China 3 cities, Airport, Japan 3 cities)
    draw_edge_card(1, 0, "WATER", "$150", None, icon_symbol="💧")
    draw_edge_card(1, 1, "BEIJING", "$140", cn_red)
    draw_edge_card(1, 2, "SHENZHEN", "$150", cn_red)
    draw_edge_card(1, 3, "HEATHROW", "$200", None, icon_symbol="✈")
    draw_edge_card(1, 4, "SHANGHAI", "$160", cn_red)
    draw_edge_card(1, 5, "EVENT", "DRAW", None, icon_symbol="?")
    draw_edge_card(1, 6, "KYOTO", "$180", jp_blue, is_japan=True)
    draw_edge_card(1, 7, "OSAKA", "$190", jp_blue, is_japan=True)
    draw_edge_card(1, 8, "TOKYO", "$200", jp_blue, is_japan=True)

    # 3. Top edge (Italy 3 cities, France 2 cities) - Facing player upright!
    draw_edge_card(2, 0, "OPPORTUNITY", "DRAW", None, icon_symbol="★")
    draw_edge_card(2, 1, "ROME", "$220", it_orange)
    draw_edge_card(2, 2, "MILAN", "$230", it_orange)
    draw_edge_card(2, 3, "VENICE", "$240", it_orange)
    draw_edge_card(2, 4, "CDG AIRPORT", "$200", None, icon_symbol="✈")
    draw_edge_card(2, 5, "ELECTRIC", "$150", None, icon_symbol="⚡")
    draw_edge_card(2, 6, "MARSEILLE", "$250", fr_pink)
    draw_edge_card(2, 7, "PARIS", "$260", fr_pink)
    draw_edge_card(2, 8, "EVENT", "DRAW", None, icon_symbol="?")

    # 4. Right edge (UK 3 cities, USA 3 cities)
    draw_edge_card(3, 0, "LIVERPOOL", "$280", uk_purple)
    draw_edge_card(3, 1, "MANCHESTER", "$290", uk_purple)
    draw_edge_card(3, 2, "OPPORTUNITY", "DRAW", None, icon_symbol="★")
    draw_edge_card(3, 3, "LONDON", "$310", uk_purple)
    draw_edge_card(3, 4, "PEK AIRPORT", "$200", None, icon_symbol="✈")
    draw_edge_card(3, 5, "TAX", "$100", None, icon_symbol="⚙")
    draw_edge_card(3, 6, "SAN FRANCISCO", "$330", us_blue)
    draw_edge_card(3, 7, "LOS ANGELES", "$380", us_blue)
    draw_edge_card(3, 8, "NEW YORK", "$400", us_blue)

    im.save(output_path, "PNG")
    print(f"Showcase saved to {output_path}")

if __name__ == "__main__":
    render_showcase()
