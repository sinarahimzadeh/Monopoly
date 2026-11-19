from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ARTIFACT_DIR = Path(r"C:\Users\Admin\.gemini\antigravity\brain\1ede9fe0-845a-447d-89c1-c412754fe314")
TILES_DIR = Path("Assets/_Ropoly/Resources/CityTiles")

TILE_W, TILE_H = 420, 260 # Left/Right edge tile is wider than tall in 3D (2.4 x 1.467)
PADDING = 40
CANVAS_W = 2 * TILE_W + 3 * PADDING
CANVAS_H = TILE_H + 2 * PADDING + 60

canvas = Image.new("RGB", (CANVAS_W, CANVAS_H), (16, 14, 26))
draw = ImageDraw.Draw(canvas)

try:
    font_title = ImageFont.truetype("arialbd.ttf", 22)
    font_name = ImageFont.truetype("arialbd.ttf", 22)
    font_price = ImageFont.truetype("arialbd.ttf", 18)
    font_price_small = ImageFont.truetype("arialbd.ttf", 14)
    font_sub = ImageFont.truetype("arial.ttf", 15)
except Exception:
    font_title = font_name = font_price = font_price_small = font_sub = ImageFont.load_default()

draw.text((PADDING, 18), "LEFT EDGE TILE COMPARISON (BEIJING, CHINA)", fill=(255, 255, 255), font=font_title)

# Flag panel
flag_raw = Image.open(TILES_DIR / "ChinaFlagPanel_Soft.png").convert("RGBA")

# Variant 1: Rotated price along inner vertical band
x0 = PADDING
y0 = 65 + PADDING
draw.text((x0, y0 - 24), "Variant 1: Price rotated along inner band (Full size)", fill=(180, 180, 210), font=font_sub)
draw.rectangle([x0, y0, x0 + TILE_W, y0 + TILE_H], fill=(24, 22, 34), outline=(50, 48, 66), width=2)

band_w = 65
flag_w = TILE_W - band_w - 18
flag_h = TILE_H - 12

# Left side: Flag
flag_crop = flag_raw.resize((flag_w, flag_h), Image.Resampling.LANCZOS)
alpha_mask = Image.new("L", flag_crop.size, int(255 * 0.45))
tile_crop = canvas.crop((x0 + 6, y0 + 6, x0 + 6 + flag_w, y0 + 6 + flag_h))
blended = Image.composite(flag_crop.convert("RGB"), tile_crop, alpha_mask)
canvas.paste(blended, (x0 + 6, y0 + 6))

# Right side: Inner band (facing board center)
band_x = x0 + 6 + flag_w + 6
draw.rectangle([band_x, y0 + 6, band_x + band_w, y0 + 6 + flag_h], fill=(46, 43, 64))
# Rule
draw.rectangle([band_x - 3, y0 + 6, band_x, y0 + 6 + flag_h], fill=(225, 215, 190))

# City Name horizontal
bbox = draw.textbbox((0, 0), "BEIJING", font=font_name)
lw = bbox[2] - bbox[0]
lh = bbox[3] - bbox[1]
lx = x0 + 6 + (flag_w - lw) // 2
ly = y0 + 6 + (flag_h - lh) // 2
draw.text((lx + 2, ly + 2), "BEIJING", fill=(10, 10, 18), font=font_name)
draw.text((lx, ly), "BEIJING", fill=(255, 255, 255), font=font_name)

# Price text rotated 90 degrees in band
price_img = Image.new("RGBA", (flag_h, band_w), (0, 0, 0, 0))
p_draw = ImageDraw.Draw(price_img)
p_bbox = p_draw.textbbox((0, 0), "$140", font=font_price)
pw = p_bbox[2] - p_bbox[0]
ph = p_bbox[3] - p_bbox[1]
p_draw.text(((flag_h - pw) // 2, (band_w - ph) // 2 - 2), "$140", fill=(255, 255, 255), font=font_price)
rotated_price = price_img.rotate(90, expand=True)
canvas.paste(rotated_price, (band_x + (band_w - rotated_price.width) // 2, y0 + 6 + (flag_h - rotated_price.height) // 2), rotated_price)


# Variant 2: Horizontal price in inner vertical band
x1 = PADDING + TILE_W + PADDING
draw.text((x1, y1 := y0 - 24), "Variant 2: Horizontal price in inner band (Compact)", fill=(180, 180, 210), font=font_sub)
draw.rectangle([x1, y0, x1 + TILE_W, y0 + TILE_H], fill=(24, 22, 34), outline=(50, 48, 66), width=2)

tile_crop2 = canvas.crop((x1 + 6, y0 + 6, x1 + 6 + flag_w, y0 + 6 + flag_h))
blended2 = Image.composite(flag_crop.convert("RGB"), tile_crop2, alpha_mask)
canvas.paste(blended2, (x1 + 6, y0 + 6))

band_x2 = x1 + 6 + flag_w + 6
draw.rectangle([band_x2, y0 + 6, band_x2 + band_w, y0 + 6 + flag_h], fill=(46, 43, 64))
draw.rectangle([band_x2 - 3, y0 + 6, band_x2, y0 + 6 + flag_h], fill=(225, 215, 190))

lx2 = x1 + 6 + (flag_w - lw) // 2
draw.text((lx2 + 2, ly + 2), "BEIJING", fill=(10, 10, 18), font=font_name)
draw.text((lx2, ly), "BEIJING", fill=(255, 255, 255), font=font_name)

# Price horizontal
p_bbox2 = draw.textbbox((0, 0), "$140", font=font_price_small)
pw2 = p_bbox2[2] - p_bbox2[0]
ph2 = p_bbox2[3] - p_bbox2[1]
px2 = band_x2 + (band_w - pw2) // 2
py2 = y0 + 6 + (flag_h - ph2) // 2
draw.text((px2, py2), "$140", fill=(255, 255, 255), font=font_price_small)

out_comp = ARTIFACT_DIR / "LeftEdgeComparison.png"
canvas.save(out_comp)
print(f"Saved comparison to {out_comp}")
