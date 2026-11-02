using System.IO;
using UnityEditor;
using UnityEngine;

namespace Ropoly.Editor
{
    public static class TileIconGenerator
    {
        public const string IconsFolder = "Assets/_Ropoly/Art/Icons";
        private const int Size = 256;

        [MenuItem("Ropoly/Visuals/Generate All Tile Icons")]
        public static void GenerateAllIcons()
        {
            if (!Directory.Exists(IconsFolder))
            {
                Directory.CreateDirectory(IconsFolder);
            }

            GenerateAirportIcon();
            GenerateElectricIcon();
            GenerateWaterIcon();
            GenerateWorldEventIcon();
            GenerateOpportunityIcon();
            GenerateTaxIcon();
            GenerateDetentionIcon();
            GenerateVacationIcon();
            GenerateGoToDetentionIcon();
            GenerateStartIcon();

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ConfigureAllIconImporters();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("Successfully generated all board tile visual icons in " + IconsFolder);
        }

        private static void SaveIcon(string fileName, Color32[] pixels)
        {
            Texture2D texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels);
            texture.Apply();

            string fullPath = Path.Combine(IconsFolder, fileName);
            File.WriteAllBytes(fullPath, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
        }

        private static void GenerateAirportIcon()
        {
            Color32[] pixels = new Color32[Size * Size];
            Color32 cyan = new Color32(140, 215, 255, 255);
            Color32 white = new Color32(255, 255, 255, 255);

            // Fuselage
            DrawCapsule(pixels, 128f, 50f, 128f, 215f, 12f, white);

            // Wings (swept)
            Vector2[] leftWing =
            {
                new Vector2(128f, 155f),
                new Vector2(24f, 110f),
                new Vector2(24f, 95f),
                new Vector2(128f, 120f)
            };
            FillPolygon(pixels, leftWing, cyan);

            Vector2[] rightWing =
            {
                new Vector2(128f, 155f),
                new Vector2(232f, 110f),
                new Vector2(232f, 95f),
                new Vector2(128f, 120f)
            };
            FillPolygon(pixels, rightWing, cyan);

            // Tail fins
            Vector2[] leftTail =
            {
                new Vector2(128f, 65f),
                new Vector2(68f, 42f),
                new Vector2(68f, 32f),
                new Vector2(128f, 45f)
            };
            FillPolygon(pixels, leftTail, cyan);

            Vector2[] rightTail =
            {
                new Vector2(128f, 65f),
                new Vector2(188f, 42f),
                new Vector2(188f, 32f),
                new Vector2(128f, 45f)
            };
            FillPolygon(pixels, rightTail, cyan);

            // Redraw central fuselage over wings
            DrawCapsule(pixels, 128f, 50f, 128f, 215f, 11f, white);
            FillCircle(pixels, 128f, 215f, 11f, white);

            SaveIcon("Airport.png", pixels);
        }

        private static void GenerateElectricIcon()
        {
            Color32[] pixels = new Color32[Size * Size];
            Color32 gold = new Color32(254, 202, 87, 255);
            Color32 white = new Color32(255, 250, 220, 255);

            Vector2[] bolt =
            {
                new Vector2(155f, 235f),
                new Vector2(85f, 135f),
                new Vector2(132f, 135f),
                new Vector2(98f, 22f),
                new Vector2(172f, 126f),
                new Vector2(125f, 126f)
            };
            FillPolygon(pixels, bolt, gold);

            // Subtle inner highlight
            Vector2[] inner =
            {
                new Vector2(150f, 220f),
                new Vector2(96f, 135f),
                new Vector2(132f, 135f),
                new Vector2(110f, 45f),
                new Vector2(160f, 126f),
                new Vector2(125f, 126f)
            };
            FillPolygon(pixels, inner, white);

            SaveIcon("Electric.png", pixels);
        }

        private static void GenerateWaterIcon()
        {
            Color32[] pixels = new Color32[Size * Size];
            Color32 cyan = new Color32(72, 219, 251, 255);
            Color32 blue = new Color32(10, 189, 227, 255);
            Color32 white = new Color32(240, 255, 255, 255);

            Vector2 center = new Vector2(128f, 100f);
            float radius = 54f;

            // Lower circular bulb
            FillCircle(pixels, center.x, center.y, radius, blue);

            // Upper conical tip
            Vector2[] tip =
            {
                new Vector2(128f, 230f),
                new Vector2(center.x - radius * 0.88f, center.y + radius * 0.48f),
                new Vector2(center.x + radius * 0.88f, center.y + radius * 0.48f)
            };
            FillPolygon(pixels, tip, blue);

            // Inner droplet highlight
            FillCircle(pixels, center.x - 14f, center.y + 12f, 22f, cyan);
            FillCircle(pixels, center.x - 18f, center.y + 18f, 10f, white);

            // Water ripple waves underneath
            DrawCapsule(pixels, 65f, 32f, 191f, 32f, 6f, cyan);
            DrawCapsule(pixels, 88f, 16f, 168f, 16f, 5f, blue);

            SaveIcon("Water.png", pixels);
        }

        private static void GenerateWorldEventIcon()
        {
            Color32[] pixels = new Color32[Size * Size];
            Color32 pink = new Color32(255, 107, 129, 255);
            Color32 purple = new Color32(200, 70, 180, 255);
            Color32 white = new Color32(255, 240, 250, 255);

            Vector2 center = new Vector2(128f, 128f);

            // Central globe sphere
            FillCircle(pixels, center.x, center.y, 58f, purple);
            DrawCircleRing(pixels, center.x, center.y, 58f, 6f, pink);

            // Globe equator ring
            DrawLine(pixels, center.x - 56f, center.y, center.x + 56f, center.y, 4f, white);

            // Globe latitude / meridian arc
            for (float deg = 0f; deg < 360f; deg += 1f)
            {
                float rad = deg * Mathf.Deg2Rad;
                float x = center.x + Mathf.Cos(rad) * 24f;
                float y = center.y + Mathf.Sin(rad) * 56f;
                SetPixelSafe(pixels, Mathf.RoundToInt(x), Mathf.RoundToInt(y), white);
            }

            // Orbit ring around globe
            for (float deg = 0f; deg < 360f; deg += 0.5f)
            {
                float rad = deg * Mathf.Deg2Rad;
                float x = center.x + Mathf.Cos(rad) * 88f;
                float y = center.y + Mathf.Sin(rad) * 28f - Mathf.Cos(rad) * 20f;
                SetPixelSafe(pixels, Mathf.RoundToInt(x), Mathf.RoundToInt(y), pink);
            }

            // Sparkling stars
            FillStar(pixels, 42f, 205f, 18f, 6f, 45f, white);
            FillStar(pixels, 218f, 195f, 14f, 5f, 45f, pink);
            FillStar(pixels, 210f, 60f, 12f, 4f, 45f, white);

            SaveIcon("WorldEvent.png", pixels);
        }

        private static void GenerateOpportunityIcon()
        {
            Color32[] pixels = new Color32[Size * Size];
            Color32 gold = new Color32(255, 159, 26, 255);
            Color32 yellow = new Color32(255, 210, 80, 255);
            Color32 white = new Color32(255, 255, 255, 255);

            // Large golden question mark
            Vector2 headCenter = new Vector2(128f, 170f);
            float headR = 48f;

            // Top loop of question mark
            DrawCircleRing(pixels, headCenter.x, headCenter.y, headR, 20f, gold);
            FillRect(pixels, 0, 0, (int)headCenter.x - 1, (int)headCenter.y - 10, new Color32(0, 0, 0, 0));
            FillRect(pixels, 0, 0, Size - 1, (int)headCenter.y - 20, new Color32(0, 0, 0, 0));

            // Neck down
            DrawCapsule(pixels, 128f, 130f, 128f, 98f, 10f, gold);

            // Bottom star dot
            FillStar(pixels, 128f, 52f, 18f, 7f, 90f, yellow);
            FillCircle(pixels, 128f, 52f, 7f, white);

            // Sparkle corner dots
            FillStar(pixels, 52f, 205f, 12f, 4f, 45f, gold);
            FillStar(pixels, 208f, 205f, 12f, 4f, 45f, gold);

            SaveIcon("Opportunity.png", pixels);
        }

        private static void GenerateTaxIcon()
        {
            Color32[] pixels = new Color32[Size * Size];
            Color32 coral = new Color32(255, 82, 82, 255);
            Color32 darkRed = new Color32(180, 40, 40, 255);
            Color32 white = new Color32(255, 255, 255, 255);

            Vector2 center = new Vector2(128f, 128f);

            // Diamond shield badge
            Vector2[] diamond =
            {
                new Vector2(center.x, 235f),
                new Vector2(215f, center.y),
                new Vector2(center.x, 21f),
                new Vector2(41f, center.y)
            };
            FillPolygon(pixels, diamond, coral);

            Vector2[] innerDiamond =
            {
                new Vector2(center.x, 218f),
                new Vector2(200f, center.y),
                new Vector2(center.x, 38f),
                new Vector2(56f, center.y)
            };
            FillPolygon(pixels, innerDiamond, darkRed);

            // Bold "$" in center
            DrawLine(pixels, center.x, 62f, center.x, 194f, 8f, white);

            // Top loop of $
            DrawCircleRing(pixels, center.x, 155f, 24f, 7f, white);
            FillRect(pixels, 0, 131, (int)center.x - 1, 160, darkRed);

            // Bottom loop of $
            DrawCircleRing(pixels, center.x, 105f, 24f, 7f, white);
            FillRect(pixels, (int)center.x + 1, 100, Size - 1, 128, darkRed);

            SaveIcon("Tax.png", pixels);
        }

        private static void GenerateDetentionIcon()
        {
            Color32[] pixels = new Color32[Size * Size];
            Color32 iron = new Color32(200, 214, 229, 255);
            Color32 darkIron = new Color32(87, 101, 116, 255);
            Color32 alert = new Color32(255, 107, 107, 255);

            // Prison cell window frame
            FillRect(pixels, 45, 35, 211, 220, darkIron);

            // Arched window top
            FillCircle(pixels, 128f, 175f, 65f, darkIron);

            // Inside window dark background
            FillRect(pixels, 55, 45, 201, 175, new Color32(24, 28, 40, 255));
            FillCircle(pixels, 128f, 175f, 55f, new Color32(24, 28, 40, 255));

            // Vertical steel bars
            float[] barX = { 80f, 112f, 144f, 176f };
            foreach (float bx in barX)
            {
                DrawLine(pixels, bx, 45f, bx, 215f, 8f, iron);
            }

            // Horizontal crossbar
            DrawLine(pixels, 50f, 110f, 206f, 110f, 10f, iron);

            // Handcuff lock circle in center
            DrawCircleRing(pixels, 128f, 110f, 22f, 6f, alert);

            SaveIcon("Detention.png", pixels);
        }

        private static void GenerateVacationIcon()
        {
            Color32[] pixels = new Color32[Size * Size];
            Color32 sun = new Color32(255, 210, 80, 255);
            Color32 teal = new Color32(29, 209, 161, 255);
            Color32 white = new Color32(255, 255, 255, 255);
            Color32 coral = new Color32(255, 121, 63, 255);

            // Sun in background
            FillCircle(pixels, 175f, 180f, 42f, sun);

            // Beach umbrella dome (semicircle)
            Vector2 umbCenter = new Vector2(110f, 120f);
            float umbR = 72f;

            for (int y = (int)umbCenter.y; y <= (int)(umbCenter.y + umbR); y++)
            {
                int row = y * Size;
                float dy = y - umbCenter.y;
                for (int x = (int)(umbCenter.x - umbR); x <= (int)(umbCenter.x + umbR); x++)
                {
                    float dx = x - umbCenter.x;
                    if ((dx * dx + dy * dy) <= umbR * umbR)
                    {
                        // Alternating teal and coral umbrella panels
                        float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
                        Color32 panel = ((int)(angle / 36f) % 2 == 0) ? coral : teal;
                        pixels[row + x] = panel;
                    }
                }
            }

            // Umbrella shaft
            DrawLine(pixels, umbCenter.x, umbCenter.y + umbR, umbCenter.x - 30f, 25f, 7f, white);

            // Sand mound base
            Vector2 moundCenter = new Vector2(100f, -20f);
            FillCircle(pixels, moundCenter.x, moundCenter.y, 75f, sun);

            SaveIcon("Vacation.png", pixels);
        }

        private static void GenerateGoToDetentionIcon()
        {
            Color32[] pixels = new Color32[Size * Size];
            Color32 orange = new Color32(255, 107, 107, 255);
            Color32 white = new Color32(255, 255, 255, 255);

            Vector2 center = new Vector2(128f, 128f);

            // Bold warning hexagon badge
            float hexR = 92f;
            Vector2[] hex = new Vector2[6];
            for (int i = 0; i < 6; i++)
            {
                float a = (i * 60f + 30f) * Mathf.Deg2Rad;
                hex[i] = center + new Vector2(Mathf.Cos(a) * hexR, Mathf.Sin(a) * hexR);
            }
            FillPolygon(pixels, hex, orange);

            // Inner dark hexagon
            Vector2[] innerHex = new Vector2[6];
            for (int i = 0; i < 6; i++)
            {
                float a = (i * 60f + 30f) * Mathf.Deg2Rad;
                innerHex[i] = center + new Vector2(Mathf.Cos(a) * (hexR - 12f), Mathf.Sin(a) * (hexR - 12f));
            }
            FillPolygon(pixels, innerHex, new Color32(35, 20, 25, 255));

            // Exclamation mark
            DrawCapsule(pixels, center.x, center.y + 45f, center.x, center.y - 12f, 12f, white);
            FillCircle(pixels, center.x, center.y - 45f, 12f, white);

            SaveIcon("GoToDetention.png", pixels);
        }

        private static void GenerateStartIcon()
        {
            Color32[] pixels = new Color32[Size * Size];
            Color32 green = new Color32(16, 172, 132, 255);
            Color32 neon = new Color32(46, 213, 115, 255);
            Color32 white = new Color32(255, 255, 255, 255);

            Vector2 center = new Vector2(128f, 128f);

            // Circular badge
            FillCircle(pixels, center.x, center.y, 88f, green);
            DrawCircleRing(pixels, center.x, center.y, 88f, 8f, neon);

            // Bold GO Arrow pointing forward/right
            Vector2[] arrow =
            {
                new Vector2(65f, 150f),
                new Vector2(130f, 150f),
                new Vector2(130f, 185f),
                new Vector2(195f, 128f),
                new Vector2(130f, 71f),
                new Vector2(130f, 106f),
                new Vector2(65f, 106f)
            };
            FillPolygon(pixels, arrow, white);

            SaveIcon("Start.png", pixels);
        }

        private static void ConfigureAllIconImporters()
        {
            string[] icons =
            {
                "Airport.png", "Electric.png", "Water.png", "WorldEvent.png",
                "Opportunity.png", "Tax.png", "Detention.png", "Vacation.png",
                "GoToDetention.png", "Start.png"
            };

            foreach (string file in icons)
            {
                string path = $"{IconsFolder}/{file}";
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.spritePixelsPerUnit = 100;
                    importer.mipmapEnabled = false;
                    importer.filterMode = FilterMode.Bilinear;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.alphaIsTransparency = true;
                    importer.SaveAndReimport();
                }
            }
        }

        #region Helpers

        private static void FillRect(Color32[] pixels, int x0, int y0, int x1, int y1, Color32 color)
        {
            x0 = Mathf.Clamp(x0, 0, Size - 1);
            x1 = Mathf.Clamp(x1, 0, Size - 1);
            y0 = Mathf.Clamp(y0, 0, Size - 1);
            y1 = Mathf.Clamp(y1, 0, Size - 1);

            for (int y = y0; y <= y1; y++)
            {
                int row = y * Size;
                for (int x = x0; x <= x1; x++)
                {
                    pixels[row + x] = color;
                }
            }
        }

        private static void FillCircle(Color32[] pixels, float cx, float cy, float radius, Color32 color)
        {
            int minX = Mathf.Max(0, Mathf.FloorToInt(cx - radius));
            int maxX = Mathf.Min(Size - 1, Mathf.CeilToInt(cx + radius));
            int minY = Mathf.Max(0, Mathf.FloorToInt(cy - radius));
            int maxY = Mathf.Min(Size - 1, Mathf.CeilToInt(cy + radius));
            float rSq = radius * radius;

            for (int y = minY; y <= maxY; y++)
            {
                int row = y * Size;
                float dy = y - cy;
                for (int x = minX; x <= maxX; x++)
                {
                    float dx = x - cx;
                    if ((dx * dx + dy * dy) <= rSq)
                    {
                        pixels[row + x] = color;
                    }
                }
            }
        }

        private static void DrawCircleRing(Color32[] pixels, float cx, float cy, float radius, float thickness, Color32 color)
        {
            int minX = Mathf.Max(0, Mathf.FloorToInt(cx - radius - thickness));
            int maxX = Mathf.Min(Size - 1, Mathf.CeilToInt(cx + radius + thickness));
            int minY = Mathf.Max(0, Mathf.FloorToInt(cy - radius - thickness));
            int maxY = Mathf.Min(Size - 1, Mathf.CeilToInt(cy + radius + thickness));
            float halfThick = thickness * 0.5f;

            for (int y = minY; y <= maxY; y++)
            {
                int row = y * Size;
                float dy = y - cy;
                for (int x = minX; x <= maxX; x++)
                {
                    float dx = x - cx;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (Mathf.Abs(dist - radius) <= halfThick)
                    {
                        pixels[row + x] = color;
                    }
                }
            }
        }

        private static void DrawLine(Color32[] pixels, float x0, float y0, float x1, float y1, float thickness, Color32 color)
        {
            Vector2 a = new Vector2(x0, y0);
            Vector2 b = new Vector2(x1, y1);
            float minX = Mathf.Max(0f, Mathf.Min(x0, x1) - thickness);
            float maxX = Mathf.Min(Size - 1f, Mathf.Max(x0, x1) + thickness);
            float minY = Mathf.Max(0f, Mathf.Min(y0, y1) - thickness);
            float maxY = Mathf.Min(Size - 1f, Mathf.Max(y0, y1) + thickness);
            float halfThick = thickness * 0.5f;

            for (int y = (int)minY; y <= (int)maxY; y++)
            {
                int row = y * Size;
                for (int x = (int)minX; x <= (int)maxX; x++)
                {
                    Vector2 p = new Vector2(x, y);
                    if (DistToSegment(p, a, b) <= halfThick)
                    {
                        pixels[row + x] = color;
                    }
                }
            }
        }

        private static void DrawCapsule(Color32[] pixels, float x0, float y0, float x1, float y1, float radius, Color32 color)
        {
            DrawLine(pixels, x0, y0, x1, y1, radius * 2f, color);
            FillCircle(pixels, x0, y0, radius, color);
            FillCircle(pixels, x1, y1, radius, color);
        }

        private static float DistToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float l2 = ab.sqrMagnitude;
            if (l2 < 0.0001f) return Vector2.Distance(p, a);
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / l2);
            Vector2 projection = a + t * ab;
            return Vector2.Distance(p, projection);
        }

        private static void FillStar(Color32[] pixels, float cx, float cy, float outerR, float innerR, float angleOffsetDeg, Color32 color)
        {
            Vector2[] pts = new Vector2[10];
            for (int i = 0; i < 10; i++)
            {
                float angle = (i * 36f + angleOffsetDeg) * Mathf.Deg2Rad;
                float r = (i % 2 == 0) ? outerR : innerR;
                pts[i] = new Vector2(cx + Mathf.Cos(angle) * r, cy + Mathf.Sin(angle) * r);
            }

            int minX = Mathf.Max(0, Mathf.FloorToInt(cx - outerR));
            int maxX = Mathf.Min(Size - 1, Mathf.CeilToInt(cx + outerR));
            int minY = Mathf.Max(0, Mathf.FloorToInt(cy - outerR));
            int maxY = Mathf.Min(Size - 1, Mathf.CeilToInt(cy + outerR));

            for (int y = minY; y <= maxY; y++)
            {
                int row = y * Size;
                for (int x = minX; x <= maxX; x++)
                {
                    if (IsPointInPolygon(new Vector2(x, y), pts))
                    {
                        pixels[row + x] = color;
                    }
                }
            }
        }

        private static void FillPolygon(Color32[] pixels, Vector2[] vertices, Color32 color)
        {
            float minX = Size;
            float maxX = 0;
            float minY = Size;
            float maxY = 0;

            foreach (Vector2 v in vertices)
            {
                if (v.x < minX) minX = v.x;
                if (v.x > maxX) maxX = v.x;
                if (v.y < minY) minY = v.y;
                if (v.y > maxY) maxY = v.y;
            }

            int iMinX = Mathf.Max(0, Mathf.FloorToInt(minX));
            int iMaxX = Mathf.Min(Size - 1, Mathf.CeilToInt(maxX));
            int iMinY = Mathf.Max(0, Mathf.FloorToInt(minY));
            int iMaxY = Mathf.Min(Size - 1, Mathf.CeilToInt(maxY));

            for (int y = iMinY; y <= iMaxY; y++)
            {
                int row = y * Size;
                for (int x = iMinX; x <= iMaxX; x++)
                {
                    if (IsPointInPolygon(new Vector2(x, y), vertices))
                    {
                        pixels[row + x] = color;
                    }
                }
            }
        }

        private static bool IsPointInPolygon(Vector2 p, Vector2[] vertices)
        {
            bool inside = false;
            int j = vertices.Length - 1;
            for (int i = 0; i < vertices.Length; i++)
            {
                if ((vertices[i].y > p.y) != (vertices[j].y > p.y) &&
                    (p.x < (vertices[j].x - vertices[i].x) * (p.y - vertices[i].y) / (vertices[j].y - vertices[i].y) + vertices[i].x))
                {
                    inside = !inside;
                }
                j = i;
            }
            return inside;
        }

        private static void SetPixelSafe(Color32[] pixels, int x, int y, Color32 color)
        {
            if (x >= 0 && x < Size && y >= 0 && y < Size)
            {
                pixels[y * Size + x] = color;
            }
        }

        #endregion
    }
}
