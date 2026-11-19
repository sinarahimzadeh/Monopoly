"""Generate flag panels and blur variants for all 8 countries in Ropoly."""

import uuid
from pathlib import Path
import numpy as np
from PIL import Image, ImageFilter

ROOT = Path(__file__).resolve().parents[1]
FLAGS_SRC = ROOT / "Assets/_Ropoly/Art/Flags"
OUT_DIR = ROOT / "Assets/_Ropoly/Resources/CityTiles"
OUT_DIR.mkdir(parents=True, exist_ok=True)

COUNTRIES = [
    ("Brazil", "country-brazil"),
    ("China", "country-china"),
    ("France", "country-france"),
    ("India", "country-india"),
    ("Italy", "country-italy"),
    ("Russia", "country-russia"),
    ("UnitedKingdom", "country-united-kingdom"),
    ("UnitedStates", "country-united-states"),
]

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
        width: 768
        height: 1120
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


def write_meta_if_missing(meta_path, sprite_name):
    if meta_path.exists():
        return
    g = uuid.uuid4().hex
    sid = uuid.uuid4().hex
    chid = uuid.uuid4().hex
    content = META_TEMPLATE.format(
        guid=g,
        sprite_name=sprite_name,
        sprite_id=sid,
        sheet_id=chid,
    )
    meta_path.write_text(content, encoding="utf-8")


def create_card_panel(src_img, width=768, height=1120):
    # Fit src_img (512x320) over width x height
    scale = max(width / src_img.width, height / src_img.height)
    new_w = int(src_img.width * scale)
    new_h = int(src_img.height * scale)
    scaled = src_img.resize((new_w, new_h), Image.Resampling.LANCZOS)
    x0 = (new_w - width) // 2
    y0 = (new_h - height) // 2
    cropped = scaled.crop((x0, y0, x0 + width, y0 + height)).convert("RGB")

    # Add subtle fabric lighting/vignette
    yy, xx = np.mgrid[0:height, 0:width].astype(np.float32)
    x = xx / (width - 1)
    y = yy / (height - 1)

    silk = 0.96 + 0.05 * np.sin(x * 24.0 + y * 3.0) + 0.02 * np.sin(x * 52.0 - y * 4.0)
    center_light = 0.08 * np.exp(-((x - 0.5) ** 2 / 0.18 + (y - 0.45) ** 2 / 0.14))
    edge_shade = 0.18 * np.maximum(np.abs(x - 0.5) * 2.0, np.abs(y - 0.5) * 1.6) ** 1.8
    light = np.clip(silk + center_light - edge_shade, 0.6, 1.25)

    pixels = np.array(cropped, dtype=np.float32) * light[..., None]
    pixels = np.uint8(np.clip(pixels, 0, 255))
    return Image.fromarray(pixels, "RGB")


def main():
    for name, cid in COUNTRIES:
        src = FLAGS_SRC / f"{name}.png"
        if not src.exists():
            print(f"Skipping {name}: {src} not found")
            continue

        raw = Image.open(src)
        panel = create_card_panel(raw)

        # Standard portrait panel (768 x 1120)
        base_path = OUT_DIR / f"{name}FlagPanel.png"
        if not base_path.exists() or name != "Italy":
            panel.save(base_path, optimize=True)
            print(f"Saved {base_path.name}")
        write_meta_if_missing(base_path.with_suffix(".png.meta"), f"{name}FlagPanel_0")

        # Soft blur portrait (radius 15)
        soft_path = OUT_DIR / f"{name}FlagPanel_Soft.png"
        soft = panel.filter(ImageFilter.GaussianBlur(radius=15))
        soft.save(soft_path, optimize=True)
        write_meta_if_missing(soft_path.with_suffix(".png.meta"), f"{name}FlagPanel_Soft_0")

        # Heavy blur portrait (radius 35)
        blur_path = OUT_DIR / f"{name}FlagPanel_Blur.png"
        blur = panel.filter(ImageFilter.GaussianBlur(radius=35))
        blur.save(blur_path, optimize=True)
        write_meta_if_missing(blur_path.with_suffix(".png.meta"), f"{name}FlagPanel_Blur_0")

        # Wide landscape panel (1120 x 824) for vertical tiles (Left / Right edges)
        # Keeps flag straight upright with perfect 1.36:1 landscape proportions
        panel_wide = create_card_panel(raw, width=1120, height=824)

        wide_base_path = OUT_DIR / f"{name}FlagPanel_Wide.png"
        panel_wide.save(wide_base_path, optimize=True)
        write_meta_if_missing(wide_base_path.with_suffix(".png.meta"), f"{name}FlagPanel_Wide_0")

        wide_soft_path = OUT_DIR / f"{name}FlagPanel_Wide_Soft.png"
        wide_soft = panel_wide.filter(ImageFilter.GaussianBlur(radius=15))
        wide_soft.save(wide_soft_path, optimize=True)
        write_meta_if_missing(wide_soft_path.with_suffix(".png.meta"), f"{name}FlagPanel_Wide_Soft_0")

        wide_blur_path = OUT_DIR / f"{name}FlagPanel_Wide_Blur.png"
        wide_blur = panel_wide.filter(ImageFilter.GaussianBlur(radius=35))
        wide_blur.save(wide_blur_path, optimize=True)
        write_meta_if_missing(wide_blur_path.with_suffix(".png.meta"), f"{name}FlagPanel_Wide_Blur_0")

        print(f"Done {name}")


if __name__ == "__main__":
    main()
