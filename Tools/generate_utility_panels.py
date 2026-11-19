import uuid
from pathlib import Path
import numpy as np
from PIL import Image, ImageFilter

ROOT = Path(__file__).resolve().parents[1]
OUT_DIR = ROOT / "Assets/_Ropoly/Resources/CityTiles"
OUT_DIR.mkdir(parents=True, exist_ok=True)

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

PANEL_TYPES = [
    ("Airport", (22, 55, 105), (55, 145, 235)),
    ("Water", (14, 65, 85), (30, 175, 190)),
    ("Electric", (38, 30, 85), (245, 185, 45)),
]

def make_panel(dark_c, light_c, w, h):
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

def main():
    for name, dark_c, light_c in PANEL_TYPES:
        # 1. Standard portrait (768 x 1120)
        p_port = make_panel(dark_c, light_c, 768, 1120)
        base_path = OUT_DIR / f"{name}Panel.png"
        p_port.save(base_path, optimize=True)
        write_meta_if_missing(base_path.with_suffix(".png.meta"), f"{name}Panel_0", 768, 1120)

        soft_path = OUT_DIR / f"{name}Panel_Soft.png"
        p_soft = p_port.filter(ImageFilter.GaussianBlur(radius=15))
        p_soft.save(soft_path, optimize=True)
        write_meta_if_missing(soft_path.with_suffix(".png.meta"), f"{name}Panel_Soft_0", 768, 1120)

        blur_path = OUT_DIR / f"{name}Panel_Blur.png"
        p_blur = p_port.filter(ImageFilter.GaussianBlur(radius=35))
        p_blur.save(blur_path, optimize=True)
        write_meta_if_missing(blur_path.with_suffix(".png.meta"), f"{name}Panel_Blur_0", 768, 1120)

        # 2. Wide landscape (1120 x 824)
        p_wide = make_panel(dark_c, light_c, 1120, 824)
        wide_base_path = OUT_DIR / f"{name}Panel_Wide.png"
        p_wide.save(wide_base_path, optimize=True)
        write_meta_if_missing(wide_base_path.with_suffix(".png.meta"), f"{name}Panel_Wide_0", 1120, 824)

        wide_soft_path = OUT_DIR / f"{name}Panel_Wide_Soft.png"
        p_wide_soft = p_wide.filter(ImageFilter.GaussianBlur(radius=15))
        p_wide_soft.save(wide_soft_path, optimize=True)
        write_meta_if_missing(wide_soft_path.with_suffix(".png.meta"), f"{name}Panel_Wide_Soft_0", 1120, 824)

        wide_blur_path = OUT_DIR / f"{name}Panel_Wide_Blur.png"
        p_wide_blur = p_wide.filter(ImageFilter.GaussianBlur(radius=35))
        p_wide_blur.save(wide_blur_path, optimize=True)
        write_meta_if_missing(wide_blur_path.with_suffix(".png.meta"), f"{name}Panel_Wide_Blur_0", 1120, 824)

        print(f"Generated {name} panels successfully.")

if __name__ == "__main__":
    main()
