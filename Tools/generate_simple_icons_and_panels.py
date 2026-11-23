import math
import uuid
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parents[1]
OUT_TILES = ROOT / "Assets/_Ropoly/Resources/CityTiles"
OUT_ICONS_RES = ROOT / "Assets/_Ropoly/Resources/Icons"
OUT_ICONS_ART = ROOT / "Assets/_Ropoly/Art/Icons"

OUT_TILES.mkdir(parents=True, exist_ok=True)
OUT_ICONS_RES.mkdir(parents=True, exist_ok=True)
OUT_ICONS_ART.mkdir(parents=True, exist_ok=True)

META_TEMPLATE = """fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable:
  - first:
      213: -4171653571250100432
    second: {sprite_name}
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 1
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 1
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: 100
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: 8
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 50462976
  cookieLightType: 0
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  - serializedVersion: 4
    buildTarget: Standalone
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 1
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  - serializedVersion: 4
    buildTarget: WebGL
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 1
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites:
    - serializedVersion: 2
      name: {sprite_name}
      rect:
        serializedVersion: 2
        x: 0
        y: 0
        width: {width}
        height: {height}
      alignment: 0
      pivot: {{x: 0, y: 0}}
      border: {{x: 0, y: 0, z: 0, w: 0}}
      customData: 
      outline: []
      physicsShape: []
      tessellationDetail: -1
      bones: []
      spriteID: {sprite_id}
      internalID: -4171653571250100432
      vertices: []
      indices: 
      edges: []
      weights: []
    outline: []
    customData: 
    physicsShape: []
    bones: []
    spriteID: {sheet_id}
    internalID: 0
    vertices: []
    indices: 
    edges: []
    weights: []
    secondaryTextures: []
    spriteCustomMetadata:
      entries: []
    nameFileIdTable: {{}}
  mipmapLimitGroupName: 
  pSDRemoveMatte: 0
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""

def write_meta_if_missing(meta_path, sprite_name, width, height):
    if meta_path.exists():
        return
    g = uuid.uuid4().hex
    sid = uuid.uuid4().hex
    chid = uuid.uuid4().hex
    content = META_TEMPLATE.format(
        guid=g,
        sprite_name=sprite_name,
        width=width,
        height=height,
        sprite_id=sid,
        sheet_id=chid,
    )
    meta_path.write_text(content, encoding="utf-8")

def save_icon(img_512, base_name):
    final_img = img_512.resize((256, 256), Image.Resampling.LANCZOS)
    for folder in [OUT_ICONS_RES, OUT_ICONS_ART]:
        path = folder / f"{base_name}.png"
        final_img.save(path, optimize=True)
        write_meta_if_missing(path.with_suffix(".png.meta"), f"{base_name}_0", 256, 256)
    print(f"Saved {base_name}.png (256x256)")

# --- 1. SIMPLE WHITE ICONS (Supersampled at 1024x1024) ---
SS = 4
S = 256 * SS # 1024

def make_vacation_white():
    # Clean palm tree & sun silhouette
    img = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    
    # Sun behind palm
    draw.ellipse([S*0.58, S*0.14, S*0.86, S*0.42], fill=(255, 255, 255, 255))
    
    # Island mound at bottom
    draw.chord([S*0.10, S*0.75, S*0.90, S*1.15], start=180, end=0, fill=(255, 255, 255, 255))
    
    # Curved Trunk
    trunk_pts = [
        (S*0.50, S*0.80), (S*0.46, S*0.62), (S*0.42, S*0.46), (S*0.38, S*0.36)
    ]
    # Draw thick curved trunk
    for i in range(len(trunk_pts) - 1):
        w = int(S * (0.075 - i * 0.012))
        draw.line([trunk_pts[i], trunk_pts[i+1]], fill=(255, 255, 255, 255), width=w)
        
    # Fronds (palm leaves) radiating from (S*0.38, S*0.36)
    cx, cy = S*0.38, S*0.36
    fronds = [
        # (cp_x, cp_y, end_x, end_y, width)
        (cx - S*0.18, cy - S*0.20, cx - S*0.30, cy + S*0.02, 38),  # Left down
        (cx - S*0.12, cy - S*0.28, cx - S*0.24, cy - S*0.18, 42),  # Left high
        (cx + S*0.02, cy - S*0.32, cx - S*0.05, cy - S*0.26, 44),  # Top
        (cx + S*0.22, cy - S*0.22, cx + S*0.36, cy - S*0.12, 44),  # Right high
        (cx + S*0.26, cy - S*0.10, cx + S*0.42, cy + S*0.12, 40),  # Right down
    ]
    for cpx, cpy, ex, ey, w in fronds:
        # Approximate quadratic bezier curve
        pts = []
        for t in np.linspace(0, 1, 30):
            x = (1-t)**2 * cx + 2*(1-t)*t * cpx + t**2 * ex
            y = (1-t)**2 * cy + 2*(1-t)*t * cpy + t**2 * ey
            pts.append((x, y))
        draw.line(pts, fill=(255, 255, 255, 255), width=w)
        
    return img

def make_detention_white():
    # Clean prison bars silhouette
    img = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    
    # Outer frame arch
    pad_x = S * 0.16
    top_y = S * 0.14
    bot_y = S * 0.86
    w_frame = int(S * 0.055)
    
    # Top horizontal bar
    draw.rounded_rectangle([pad_x, top_y, S - pad_x, top_y + w_frame], radius=int(S*0.02), fill=(255, 255, 255, 255))
    # Bottom horizontal bar
    draw.rounded_rectangle([pad_x, bot_y - w_frame, S - pad_x, bot_y], radius=int(S*0.02), fill=(255, 255, 255, 255))
    # Middle horizontal brace
    mid_y = S * 0.50
    draw.rounded_rectangle([pad_x, mid_y - w_frame*0.5, S - pad_x, mid_y + w_frame*0.5], radius=int(S*0.015), fill=(255, 255, 255, 255))
    
    # 5 Vertical bars
    n_bars = 5
    xs = np.linspace(pad_x + w_frame*0.8, S - pad_x - w_frame*0.8, n_bars)
    for bx in xs:
        draw.line([(bx, top_y), (bx, bot_y)], fill=(255, 255, 255, 255), width=int(S * 0.045))
        
    # Center round padlock
    padlock_r = S * 0.11
    pcx, pcy = S * 0.50, mid_y
    draw.ellipse([pcx - padlock_r, pcy - padlock_r, pcx + padlock_r, pcy + padlock_r], fill=(255, 255, 255, 255))
    # Keyhole cutout
    draw.ellipse([pcx - S*0.022, pcy - S*0.035, pcx + S*0.022, pcy + S*0.01], fill=(0, 0, 0, 0))
    draw.polygon([(pcx - S*0.015, pcy), (pcx + S*0.015, pcy), (pcx + S*0.025, pcy + S*0.05), (pcx - S*0.025, pcy + S*0.05)], fill=(0, 0, 0, 0))
    
    return img

def make_opportunity_white():
    # Clean 4-point sparkle star with secondary accents
    img = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    
    cx, cy = S * 0.50, S * 0.50
    R_outer = S * 0.40
    R_inner = S * 0.09
    
    # 4-point star polygon
    pts = []
    for i in range(8):
        angle = i * (math.pi / 4.0)
        r = R_outer if (i % 2 == 0) else R_inner
        pts.append((cx + r * math.cos(angle - math.pi/2), cy + r * math.sin(angle - math.pi/2)))
    draw.polygon(pts, fill=(255, 255, 255, 255))
    
    # Diagonal sparkles
    for dx, dy in [(-S*0.28, -S*0.28), (S*0.28, -S*0.26), (-S*0.26, S*0.28), (S*0.28, S*0.26)]:
        r_sub = S * 0.075
        r_in = S * 0.022
        sub_pts = []
        for i in range(8):
            angle = i * (math.pi / 4.0)
            r = r_sub if (i % 2 == 0) else r_in
            sub_pts.append((cx + dx + r * math.cos(angle), cy + dy + r * math.sin(angle)))
        draw.polygon(sub_pts, fill=(255, 255, 255, 255))
        
    return img

def make_tax_white():
    # Clean percentage / coin tax badge silhouette
    img = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    
    cx, cy = S * 0.50, S * 0.50
    R = S * 0.38
    
    # Outer circle ring
    draw.ellipse([cx - R, cy - R, cx + R, cy + R], outline=(255, 255, 255, 255), width=int(S * 0.065))
    
    # Percentage slash
    p1 = (cx + S * 0.22, cy - S * 0.22)
    p2 = (cx - S * 0.22, cy + S * 0.22)
    draw.line([p1, p2], fill=(255, 255, 255, 255), width=int(S * 0.065))
    
    # Top-left circle
    c1x, c1y = cx - S * 0.13, cy - S * 0.13
    draw.ellipse([c1x - S*0.075, c1y - S*0.075, c1x + S*0.075, c1y + S*0.075], fill=(255, 255, 255, 255))
    
    # Bottom-right circle
    c2x, c2y = cx + S * 0.13, cy + S * 0.13
    draw.ellipse([c2x - S*0.075, c2y - S*0.075, c2x + S*0.075, c2y + S*0.075], fill=(255, 255, 255, 255))
    
    return img

def make_start_white():
    # Clean forward speed arrow / GO chevron
    img = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    
    cx, cy = S * 0.50, S * 0.50
    # Outer circle
    R = S * 0.38
    draw.ellipse([cx - R, cy - R, cx + R, cy + R], outline=(255, 255, 255, 255), width=int(S * 0.065))
    
    # Bold right arrow inside
    arrow_pts = [
        (cx - S * 0.20, cy - S * 0.08),
        (cx + S * 0.02, cy - S * 0.08),
        (cx + S * 0.02, cy - S * 0.18),
        (cx + S * 0.24, cy),
        (cx + S * 0.02, cy + S * 0.18),
        (cx + S * 0.02, cy + S * 0.08),
        (cx - S * 0.20, cy + S * 0.08),
    ]
    draw.polygon(arrow_pts, fill=(255, 255, 255, 255))
    return img

def make_gotodetention_white():
    # Clean alert hexagon with warning exclamation
    img = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    
    cx, cy = S * 0.50, S * 0.50
    R = S * 0.40
    
    # Regular hexagon
    hex_pts = []
    for i in range(6):
        angle = i * (math.pi / 3.0) - (math.pi / 6.0)
        hex_pts.append((cx + R * math.cos(angle), cy + R * math.sin(angle)))
    draw.polygon(hex_pts, outline=(255, 255, 255, 255), width=int(S * 0.075))
    
    # Exclamation mark
    # Upper bar
    draw.rounded_rectangle([cx - S*0.04, cy - S*0.22, cx + S*0.04, cy + S*0.05], radius=int(S*0.025), fill=(255, 255, 255, 255))
    # Dot
    draw.ellipse([cx - S*0.045, cy + S*0.13, cx + S*0.045, cy + S*0.22], fill=(255, 255, 255, 255))
    
    return img

# --- 2. THEMED PANELS (Square 1024x1024, Soft and Blur) ---
NEW_PANELS = [
    ("Opportunity", (45, 32, 12), (245, 180, 35)),   # Radiant golden amber
    ("Tax", (55, 16, 22), (235, 65, 80)),           # Crimson fee alert
]

def make_panel_tex(dark_c, light_c, w, h):
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

def generate_panels():
    for name, dark_c, light_c in NEW_PANELS:
        # Standard square (1024 x 1024)
        p_base = make_panel_tex(dark_c, light_c, 1024, 1024)
        base_path = OUT_TILES / f"{name}Panel.png"
        p_base.save(base_path, optimize=True)
        write_meta_if_missing(base_path.with_suffix(".png.meta"), f"{name}Panel_0", 1024, 1024)

        soft_path = OUT_TILES / f"{name}Panel_Soft.png"
        p_soft = p_base.filter(ImageFilter.GaussianBlur(radius=15))
        p_soft.save(soft_path, optimize=True)
        write_meta_if_missing(soft_path.with_suffix(".png.meta"), f"{name}Panel_Soft_0", 1024, 1024)

        blur_path = OUT_TILES / f"{name}Panel_Blur.png"
        p_blur = p_base.filter(ImageFilter.GaussianBlur(radius=35))
        p_blur.save(blur_path, optimize=True)
        write_meta_if_missing(blur_path.with_suffix(".png.meta"), f"{name}Panel_Blur_0", 1024, 1024)

        # Wide variant (1120 x 824)
        p_wide = make_panel_tex(dark_c, light_c, 1120, 824)
        wide_base_path = OUT_TILES / f"{name}Panel_Wide.png"
        p_wide.save(wide_base_path, optimize=True)
        write_meta_if_missing(wide_base_path.with_suffix(".png.meta"), f"{name}Panel_Wide_0", 1120, 824)

        wide_soft_path = OUT_TILES / f"{name}Panel_Wide_Soft.png"
        p_wide_soft = p_wide.filter(ImageFilter.GaussianBlur(radius=15))
        p_wide_soft.save(wide_soft_path, optimize=True)
        write_meta_if_missing(wide_soft_path.with_suffix(".png.meta"), f"{name}Panel_Wide_Soft_0", 1120, 824)

        wide_blur_path = OUT_TILES / f"{name}Panel_Wide_Blur.png"
        p_wide_blur = p_wide.filter(ImageFilter.GaussianBlur(radius=35))
        p_wide_blur.save(wide_blur_path, optimize=True)
        write_meta_if_missing(wide_blur_path.with_suffix(".png.meta"), f"{name}Panel_Wide_Blur_0", 1120, 824)

        print(f"Generated {name} panels.")

def main():
    print("Generating simple crisp white icons...")
    save_icon(make_vacation_white(), "Vacation_White")
    save_icon(make_detention_white(), "Detention_White")
    save_icon(make_opportunity_white(), "Opportunity_White")
    save_icon(make_tax_white(), "Tax_White")
    save_icon(make_start_white(), "Start_White")
    save_icon(make_gotodetention_white(), "GoToDetention_White")

    # Also overwrite the base colored icon files with clean white icons so ANY fallback gets the clean icon!
    save_icon(make_vacation_white(), "Vacation")
    save_icon(make_detention_white(), "Detention")
    save_icon(make_opportunity_white(), "Opportunity")
    save_icon(make_tax_white(), "Tax")
    save_icon(make_start_white(), "Start")
    save_icon(make_gotodetention_white(), "GoToDetention")

    print("Generating themed panels...")
    generate_panels()
    print("All simple icons & panels generated successfully!")

if __name__ == "__main__":
    main()
