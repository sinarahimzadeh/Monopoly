from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = Path(".")
OUT_DIR = Path("Assets/_Ropoly/Resources/CityTiles")
ICONS_DIR = Path("Assets/_Ropoly/Resources/Icons")

# We create textured panels for Airport, Water, and Electric
THEMES = [
    ("Airport", (24, 60, 110), (50, 140, 235), "Icons/Airport.png"),
    ("Water", (14, 75, 95), (35, 185, 200), "Icons/Water.png"),
    ("Electric", (35, 30, 85), (240, 180, 50), "Icons/Electric.png"),
]

def make_panel(base_dark, accent_light, icon_path, width, height):
    canvas = Image.new("RGB", (width, height), base_dark)
    yy, xx = np.mgrid[0:height, 0:width].astype(np.float32)
    x = xx / (width - 1)
    y = yy / (height - 1)

    # Silk fabric weave
    silk = 0.95 + 0.05 * np.sin(x * 24.0 + y * 3.0) + 0.02 * np.sin(x * 52.0 - y * 4.0)
    
    # Center ambient radial glow
    r = np.sqrt((x - 0.5)**2 + (y - 0.45)**2)
    glow = np.exp(- (r**2) / 0.18)
    vignette = 1.0 - 0.35 * np.maximum(np.abs(x - 0.5) * 1.8, np.abs(y - 0.5) * 1.6)**1.8

    color_arr = np.zeros((height, width, 3), dtype=np.float32)
    for c in range(3):
        color_arr[..., c] = base_dark[c] + (accent_light[c] - base_dark[c]) * glow * 0.45
        color_arr[..., c] = color_arr[..., c] * silk * vignette

    color_arr = np.uint8(np.clip(color_arr, 0, 255))
    panel = Image.fromarray(color_arr, "RGB")
    return panel

print("Testing panel generation...")
p = make_panel((24, 60, 110), (50, 140, 235), None, 768, 1120)
print("Panel OK, size:", p.size)
