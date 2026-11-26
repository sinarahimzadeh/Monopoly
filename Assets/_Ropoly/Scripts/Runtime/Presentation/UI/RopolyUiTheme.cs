using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Ropoly.Presentation.UI
{
    /// <summary>
    /// Shared procedural 9-sliced rounded UI sprites, color palette, and styling helpers
    /// that replace flat sharp-cornered rectangles with cohesive rounded cards and pill buttons.
    /// </summary>
    public static class RopolyUiTheme
    {
        public static readonly Color PanelBackground = new Color(0.082f, 0.058f, 0.165f, 0.96f);
        public static readonly Color SubPanelBackground = new Color(0.055f, 0.038f, 0.115f, 0.85f);
        public static readonly Color CardBackground = new Color(0.135f, 0.100f, 0.245f, 0.98f);
        public static readonly Color ActiveCardBackground = new Color(0.245f, 0.195f, 0.435f, 1f);
        public static readonly Color PrimaryActionColor = new Color(0.36f, 0.23f, 0.72f, 1f);
        public static readonly Color PositiveActionColor = new Color(0.15f, 0.62f, 0.48f, 1f);
        public static readonly Color SecondaryActionColor = new Color(0.19f, 0.15f, 0.34f, 1f);
        public static readonly Color TradeActionColor = new Color(0.16f, 0.25f, 0.46f, 1f);
        public static readonly Color DangerActionColor = new Color(0.76f, 0.20f, 0.34f, 1f);
        public static readonly Color MintAccent = new Color(0.50f, 0.91f, 0.84f, 1f);

        private static Sprite _panelSprite;
        private static Sprite _cardSprite;
        private static Sprite _buttonSprite;
        private static Sprite _pillSprite;

        public static Sprite PanelSprite =>
            _panelSprite ??= CreateRoundedSprite(
                size: 128,
                cornerRadius: 22f,
                borderThickness: 2.0f,
                topSheen: 1.04f,
                bottomShade: 0.90f,
                borderBoost: 1.28f,
                borderAlpha: 0.24f,
                sliceBorder: 26);

        public static Sprite CardSprite =>
            _cardSprite ??= CreateRoundedSprite(
                size: 96,
                cornerRadius: 16f,
                borderThickness: 1.6f,
                topSheen: 1.05f,
                bottomShade: 0.92f,
                borderBoost: 1.32f,
                borderAlpha: 0.26f,
                sliceBorder: 20);

        public static Sprite ButtonSprite =>
            _buttonSprite ??= CreateRoundedSprite(
                size: 96,
                cornerRadius: 16f,
                borderThickness: 1.8f,
                topSheen: 1.12f,
                bottomShade: 0.84f,
                borderBoost: 1.45f,
                borderAlpha: 0.34f,
                sliceBorder: 20);

        public static Sprite PillSprite =>
            _pillSprite ??= CreateRoundedSprite(
                size: 96,
                cornerRadius: 22f,
                borderThickness: 1.8f,
                topSheen: 1.12f,
                bottomShade: 0.84f,
                borderBoost: 1.45f,
                borderAlpha: 0.34f,
                sliceBorder: 24);

        public static void ApplyPanel(Image image, Color? tint = null, bool addShadow = true)
        {
            if (image == null)
            {
                return;
            }

            image.sprite = PanelSprite;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 1f;
            if (tint.HasValue)
            {
                image.color = tint.Value;
            }

            if (addShadow)
            {
                EnsureDropShadow(image.gameObject, new Color(0f, 0f, 0f, 0.45f), new Vector2(0f, -5f));
            }
        }

        public static void ApplyCard(Image image, Color? tint = null, bool addShadow = false)
        {
            if (image == null)
            {
                return;
            }

            image.sprite = CardSprite;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 1.15f;
            if (tint.HasValue)
            {
                image.color = tint.Value;
            }

            if (addShadow)
            {
                EnsureDropShadow(image.gameObject, new Color(0f, 0f, 0f, 0.35f), new Vector2(0f, -3f));
            }
        }

        public static void ApplyButton(
            Button button,
            Color? tint = null,
            bool usePill = false,
            bool addShadow = true)
        {
            if (button == null)
            {
                return;
            }

            Image image = button.targetGraphic as Image ?? button.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = usePill ? PillSprite : ButtonSprite;
                image.type = Image.Type.Sliced;
                image.pixelsPerUnitMultiplier = usePill ? 1.1f : 1.15f;
                if (tint.HasValue)
                {
                    image.color = tint.Value;
                }
            }

            button.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.94f, 0.94f, 0.98f, 1f);
            colors.highlightedColor = Color.white;
            colors.pressedColor = new Color(0.76f, 0.76f, 0.84f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.45f, 0.45f, 0.54f, 0.55f);
            colors.colorMultiplier = 1.12f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            if (addShadow && image != null)
            {
                EnsureDropShadow(button.gameObject, new Color(0f, 0f, 0f, 0.38f), new Vector2(0f, -3f));
            }
        }

        public static void StyleHierarchy(Transform root, bool includePanels = true)
        {
            if (root == null)
            {
                return;
            }

            Button[] buttons = root.GetComponentsInChildren<Button>(true);
            HashSet<Image> styledImages = new HashSet<Image>();
            for (int i = 0; i < buttons.Length; i++)
            {
                Button btn = buttons[i];
                if (btn == null)
                {
                    continue;
                }

                Image btnImage = btn.targetGraphic as Image ?? btn.GetComponent<Image>();
                if (btnImage != null)
                {
                    styledImages.Add(btnImage);
                }

                ApplyButton(btn);
            }

            if (!includePanels)
            {
                return;
            }

            Image[] images = root.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                Image img = images[i];
                if (img == null || styledImages.Contains(img) || img.sprite != null)
                {
                    continue;
                }

                // Skip full-screen dark dimmed backdrops and tiny decorative bars/dividers
                RectTransform rect = img.rectTransform;
                if (img.color.a <= 0.02f)
                {
                    continue;
                }

                if (rect.anchorMin == Vector2.zero && rect.anchorMax == Vector2.one &&
                    rect.sizeDelta.sqrMagnitude < 4f)
                {
                    continue;
                }

                if (rect.rect.width < 12f || rect.rect.height < 12f)
                {
                    continue;
                }

                bool isLargeShell = rect.rect.width >= 420f || rect.rect.height >= 420f;
                if (isLargeShell)
                {
                    ApplyPanel(img);
                }
                else
                {
                    ApplyCard(img);
                }
            }
        }

        private static void EnsureDropShadow(GameObject target, Color color, Vector2 offset)
        {
            Shadow shadow = target.GetComponent<Shadow>();
            if (shadow == null)
            {
                shadow = target.AddComponent<Shadow>();
            }

            shadow.effectColor = color;
            shadow.effectDistance = offset;
            shadow.useGraphicAlpha = true;
        }

        private static Sprite CreateRoundedSprite(
            int size,
            float cornerRadius,
            float borderThickness,
            float topSheen,
            float bottomShade,
            float borderBoost,
            float borderAlpha,
            int sliceBorder)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, mipChain: false)
            {
                name = $"RopolyUiRounded_{size}_{cornerRadius:F0}",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
            };

            Color[] pixels = new Color[size * size];
            float half = size * 0.5f;
            float extent = half - 1.5f;

            for (int y = 0; y < size; y++)
            {
                float py = y + 0.5f - half;
                float verticalT = Mathf.Clamp01(y / (float)(size - 1));
                float gradient = Mathf.Lerp(bottomShade, topSheen, verticalT);

                for (int x = 0; x < size; x++)
                {
                    float px = x + 0.5f - half;
                    float dx = Mathf.Max(Mathf.Abs(px) - (extent - cornerRadius), 0f);
                    float dy = Mathf.Max(Mathf.Abs(py) - (extent - cornerRadius), 0f);
                    float dist = Mathf.Sqrt((dx * dx) + (dy * dy)) - cornerRadius;

                    float alpha = Mathf.Clamp01(0.75f - dist);
                    if (alpha <= 0.001f)
                    {
                        pixels[(y * size) + x] = Color.clear;
                        continue;
                    }

                    // Crisp subtle edge rim so every dark/colored card & button reads cleanly
                    float edgeFactor = Mathf.Clamp01(1f - Mathf.Abs(dist + (borderThickness * 0.5f)) / (borderThickness * 0.7f));
                    float brightness = Mathf.Lerp(gradient, Mathf.Min(1.25f, gradient * borderBoost), edgeFactor * borderAlpha * 3f);
                    float finalRgb = Mathf.Clamp01(brightness * 0.90f);

                    pixels[(y * size) + x] = new Color(finalRgb, finalRgb, Mathf.Min(1f, finalRgb * 1.02f), alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(updateMipmaps: false, makeNoLongerReadable: true);

            return Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit: 100f,
                extrude: 0,
                meshType: SpriteMeshType.FullRect,
                border: new Vector4(sliceBorder, sliceBorder, sliceBorder, sliceBorder));
        }
    }
}
