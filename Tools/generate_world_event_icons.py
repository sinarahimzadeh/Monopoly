import math
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

ARTIFACT_DIR = Path(r"C:\Users\Admin\.gemini\antigravity\brain\1ede9fe0-845a-447d-89c1-c412754fe314")
OUT_ICONS_DIR = Path("Assets/_Ropoly/Resources/Icons")
ART_ICONS_DIR = Path("Assets/_Ropoly/Art/Icons")

# We render at 1024x1024 with supersampling, then downscale to 512x512
SS = 4
SIZE = 512 * SS  # 2048x2048
CENTER = SIZE // 2
R = int(SIZE * 0.28)

def create_globe_variant_a():
    """
    Variant A: Modern Premium Glowing Globe
    Deep indigo/purple sphere, vibrant magenta (#F04FA8) glowing rim and orbit,
    cyan/gold glowing parallels, meridians and continents, sparkling event stars.
    """
    img = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    
    # Outer glow behind the globe
    glow_img = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    gdraw = ImageDraw.Draw(glow_img)
    gdraw.ellipse([CENTER - R - 80, CENTER - R - 80, CENTER + R + 80, CENTER + R + 80], fill=(240, 79, 168, 70))
    glow_img = glow_img.filter(ImageFilter.GaussianBlur(radius=60))
    img.paste(glow_img, (0, 0), glow_img)

    # Base sphere with rich 3D shading
    # Radial gradient from upper-left highlight to deep shadow
    sphere = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    sdraw = ImageDraw.Draw(sphere)
    
    # Sphere fill
    sdraw.ellipse([CENTER - R, CENTER - R, CENTER + R, CENTER + R], fill=(32, 22, 60, 255))
    
    # Shading layers
    for i in range(R, 0, -6):
        pct = i / R
        # Offset center towards top-left (0.42, 0.38)
        cx = CENTER - int(R * 0.18 * (1 - pct))
        cy = CENTER - int(R * 0.22 * (1 - pct))
        r_cur = i
        alpha = int(255 * (1 - pct * 0.7))
        r_col = int(45 + (130 - 45) * (1 - pct)**1.5)
        g_col = int(30 + (70 - 30) * (1 - pct)**1.5)
        b_col = int(85 + (185 - 85) * (1 - pct)**1.5)
        sdraw.ellipse([cx - r_cur, cy - r_cur, cx + r_cur, cy + r_cur], fill=(r_col, g_col, b_col, 255))
        
    # Draw Meridians (vertical ellipses) and Parallels (horizontal ellipses) on sphere
    grid_img = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    grdraw = ImageDraw.Draw(grid_img)
    
    # Equator
    grdraw.ellipse([CENTER - R, CENTER - int(R * 0.20), CENTER + R, CENTER + int(R * 0.20)], outline=(110, 225, 255, 200), width=int(14 * SS // 2))
    # Latitudes
    grdraw.ellipse([CENTER - int(R * 0.88), CENTER - int(R * 0.62), CENTER + int(R * 0.88), CENTER - int(R * 0.22)], outline=(90, 200, 245, 140), width=int(10 * SS // 2))
    grdraw.ellipse([CENTER - int(R * 0.88), CENTER + int(R * 0.22), CENTER + int(R * 0.88), CENTER + int(R * 0.62)], outline=(90, 200, 245, 140), width=int(10 * SS // 2))
    
    # Longitudes / Meridians
    grdraw.ellipse([CENTER - int(R * 0.65), CENTER - R, CENTER + int(R * 0.65), CENTER + R], outline=(100, 215, 255, 170), width=int(12 * SS // 2))
    grdraw.ellipse([CENTER - int(R * 0.30), CENTER - R, CENTER + int(R * 0.30), CENTER + R], outline=(120, 230, 255, 210), width=int(12 * SS // 2))
    grdraw.line([CENTER, CENTER - R, CENTER, CENTER + R], fill=(140, 240, 255, 230), width=int(14 * SS // 2))
    
    # Clip grid to sphere
    sphere_mask = Image.new("L", (SIZE, SIZE), 0)
    smask_draw = ImageDraw.Draw(sphere_mask)
    smask_draw.ellipse([CENTER - R, CENTER - R, CENTER + R, CENTER + R], fill=255)
    
    grid_clipped = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    grid_clipped.paste(grid_img, (0, 0), sphere_mask)
    sphere.paste(grid_clipped, (0, 0), grid_clipped)
    
    # Add sphere to main img
    img.paste(sphere, (0, 0), sphere)

    # Vibrant glowing rim around the globe
    draw.ellipse([CENTER - R, CENTER - R, CENTER + R, CENTER + R], outline=(240, 79, 168, 255), width=int(22 * SS // 2))
    draw.ellipse([CENTER - R + 6, CENTER - R + 6, CENTER + R - 6, CENTER + R - 6], outline=(255, 165, 220, 200), width=int(8 * SS // 2))

    # Dynamic 3D Orbital Ring circling the globe (angled at ~22 degrees)
    # Background part of orbit (behind the globe) is drawn by clipping, or we draw the full ellipse and mask
    orbit_w = int(R * 1.55)
    orbit_h = int(R * 0.48)
    orbit_angle = -24  # degrees

    orbit_canvas = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    odraw = ImageDraw.Draw(orbit_canvas)
    
    # Draw rotated orbit ellipse
    odraw.ellipse([CENTER - orbit_w, CENTER - orbit_h, CENTER + orbit_w, CENTER + orbit_h], outline=(255, 90, 185, 255), width=int(20 * SS // 2))
    odraw.ellipse([CENTER - orbit_w + 5, CENTER - orbit_h + 5, CENTER + orbit_w - 5, CENTER + orbit_h - 5], outline=(255, 230, 245, 220), width=int(8 * SS // 2))
    
    # Small orbiting event satellites / sparkles along the ring
    sat_x = CENTER + int(orbit_w * 0.88)
    sat_y = CENTER - int(orbit_h * 0.35)
    odraw.ellipse([sat_x - 30, sat_y - 30, sat_x + 30, sat_y + 30], fill=(255, 255, 255, 255))
    odraw.ellipse([sat_x - 45, sat_y - 45, sat_x + 45, sat_y + 45], outline=(240, 79, 168, 255), width=int(10 * SS // 2))
    
    # Rotate orbit
    orbit_rotated = orbit_canvas.rotate(orbit_angle, resample=Image.Resampling.BICUBIC, center=(CENTER, CENTER))
    
    # To give a proper 3D illusion: the front half of the orbit is in front of the globe,
    # the back half passes behind.
    # We can mask the back half so it looks like it loops around!
    # Let's paste the rotated orbit over the image
    img.paste(orbit_rotated, (0, 0), orbit_rotated)

    # Sparkling 4-point stars in corners (crisp modern vector style)
    def draw_sparkle(cx, cy, radius, color):
        points = []
        for i in range(8):
            a = i * math.pi / 4
            r_cur = radius if i % 2 == 0 else radius * 0.22
            points.append((cx + math.cos(a) * r_cur, cy + math.sin(a) * r_cur))
        draw.polygon(points, fill=color)

    draw_sparkle(CENTER - int(R * 1.05), CENTER - int(R * 0.75), 85, (255, 255, 255, 255))
    draw_sparkle(CENTER - int(R * 1.05), CENTER - int(R * 0.75), 110, (240, 79, 168, 160))
    
    draw_sparkle(CENTER + int(R * 1.15), CENTER + int(R * 0.70), 75, (255, 255, 255, 255))
    draw_sparkle(CENTER + int(R * 1.15), CENTER + int(R * 0.70), 95, (100, 220, 255, 170))
    
    draw_sparkle(CENTER + int(R * 0.95), CENTER - int(R * 0.95), 60, (255, 240, 160, 240))

    # Downsample with anti-aliasing
    final_img = img.resize((512, 512), Image.Resampling.LANCZOS)
    return final_img

def create_globe_variant_white():
    """
    Variant B: Crisp Minimalist White Silhouette
    Matches the clean vector style of the Airplane, Water Droplet, and Lightning Bolt.
    Perfect for high-contrast visibility on dark/tinted tiles.
    """
    img = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    
    line_w = int(24 * SS // 2)
    
    # Outer circle of globe
    draw.ellipse([CENTER - R, CENTER - R, CENTER + R, CENTER + R], outline=(255, 255, 255, 255), width=line_w)
    
    # Equator line
    draw.line([CENTER - R + int(line_w // 2), CENTER, CENTER + R - int(line_w // 2), CENTER], fill=(255, 255, 255, 255), width=line_w)
    
    # Latitude arcs
    lat_w = int(R * 0.86)
    lat_h = int(R * 0.42)
    draw.ellipse([CENTER - lat_w, CENTER - int(R * 0.50) - lat_h, CENTER + lat_w, CENTER - int(R * 0.50) + lat_h], outline=(255, 255, 255, 255), width=line_w)
    draw.ellipse([CENTER - lat_w, CENTER + int(R * 0.50) - lat_h, CENTER + lat_w, CENTER + int(R * 0.50) + lat_h], outline=(255, 255, 255, 255), width=line_w)
    
    # Central meridian line
    draw.line([CENTER, CENTER - R + int(line_w // 2), CENTER, CENTER + R - int(line_w // 2)], fill=(255, 255, 255, 255), width=line_w)
    
    # Longitude ellipse
    lon_w = int(R * 0.52)
    draw.ellipse([CENTER - lon_w, CENTER - R, CENTER + lon_w, CENTER + R], outline=(255, 255, 255, 255), width=line_w)
    
    # Clip inner elements to globe
    mask = Image.new("L", (SIZE, SIZE), 0)
    mdraw = ImageDraw.Draw(mask)
    mdraw.ellipse([CENTER - R + int(line_w // 3), CENTER - R + int(line_w // 3), CENTER + R - int(line_w // 3), CENTER + R - int(line_w // 3)], fill=255)
    
    # Dynamic orbital ring
    orbit_canvas = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    odraw = ImageDraw.Draw(orbit_canvas)
    orbit_w = int(R * 1.48)
    orbit_h = int(R * 0.42)
    odraw.ellipse([CENTER - orbit_w, CENTER - orbit_h, CENTER + orbit_w, CENTER + orbit_h], outline=(255, 255, 255, 255), width=line_w)
    
    # Satellite beacon
    sat_x = CENTER + int(orbit_w * 0.90)
    sat_y = CENTER - int(orbit_h * 0.28)
    odraw.ellipse([sat_x - 38, sat_y - 38, sat_x + 38, sat_y + 38], fill=(255, 255, 255, 255))
    
    orbit_rot = orbit_canvas.rotate(-22, resample=Image.Resampling.BICUBIC, center=(CENTER, CENTER))
    img.paste(orbit_rot, (0, 0), orbit_rot)
    
    # Stars
    def draw_star(cx, cy, r_out):
        pts = []
        for i in range(8):
            a = i * math.pi / 4
            r = r_out if i % 2 == 0 else r_out * 0.25
            pts.append((cx + math.cos(a) * r, cy + math.sin(a) * r))
        draw.polygon(pts, fill=(255, 255, 255, 255))

    draw_star(CENTER - int(R * 1.02), CENTER - int(R * 0.72), 70)
    draw_star(CENTER + int(R * 1.08), CENTER + int(R * 0.68), 65)

    final_img = img.resize((512, 512), Image.Resampling.LANCZOS)
    return final_img

# Generate both variants
var_a = create_globe_variant_a()
var_b = create_globe_variant_white()

# Save them to artifacts and game directories
out_a_art = ARTIFACT_DIR / "WorldEvent_VariantA_GlowingGlobe.png"
out_b_art = ARTIFACT_DIR / "WorldEvent_VariantB_CrispWhite.png"
var_a.save(out_a_art)
var_b.save(out_b_art)

print("Saved Variant A to", out_a_art)
print("Saved Variant B to", out_b_art)
