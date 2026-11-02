using System;
using System.IO;
using Ropoly.Infrastructure.Content.Board;
using UnityEditor;
using UnityEngine;

namespace Ropoly.Editor
{
    public static class FlagGenerator
    {
        public const string FlagsFolder = "Assets/_Ropoly/Art/Flags";
        public const string CountriesFolder = "Assets/_Ropoly/Content/Countries/ClassicWorld";

        private const int Width = 512;
        private const int Height = 320;

        [MenuItem("Ropoly/Visuals/Generate All Country Flags")]
        public static void GenerateAllFlags()
        {
            if (!Directory.Exists(FlagsFolder))
            {
                Directory.CreateDirectory(FlagsFolder);
            }

            GenerateIndiaFlag();
            GenerateItalyFlag();
            GenerateFranceFlag();
            GenerateRussiaFlag();
            GenerateBrazilFlag();
            GenerateChinaFlag();
            GenerateUnitedKingdomFlag();
            GenerateUnitedStatesFlag();

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            ConfigureAllFlagImporters();
            AssignFlagsToCountryDefinitions();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("Successfully generated all 8 national country flags and assigned them to CountryDefinitions.");
        }

        private static void SaveFlag(string fileName, Color32[] pixels)
        {
            Texture2D texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels);
            texture.Apply();

            string fullPath = Path.Combine(FlagsFolder, fileName);
            File.WriteAllBytes(fullPath, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
        }

        private static void GenerateIndiaFlag()
        {
            Color32[] pixels = new Color32[Width * Height];
            Color32 saffron = new Color32(255, 153, 51, 255);
            Color32 white = new Color32(255, 255, 255, 255);
            Color32 green = new Color32(19, 136, 8, 255);
            Color32 navy = new Color32(0, 0, 128, 255);

            FillRect(pixels, 0, 0, Width - 1, 106, green);
            FillRect(pixels, 0, 107, Width - 1, 212, white);
            FillRect(pixels, 0, 213, Width - 1, Height - 1, saffron);

            // Ashoka Chakra
            Vector2 center = new Vector2(Width * 0.5f, Height * 0.5f);
            float outerR = 38f;
            DrawCircleRing(pixels, center.x, center.y, outerR, 3.5f, navy);
            FillCircle(pixels, center.x, center.y, 7f, navy);

            for (int i = 0; i < 24; i++)
            {
                float angle = (i * 15f) * Mathf.Deg2Rad;
                Vector2 end = center + new Vector2(Mathf.Cos(angle) * (outerR - 1f), Mathf.Sin(angle) * (outerR - 1f));
                DrawLine(pixels, center.x, center.y, end.x, end.y, 1.8f, navy);
            }

            SaveFlag("India.png", pixels);
        }

        private static void GenerateItalyFlag()
        {
            Color32[] pixels = new Color32[Width * Height];
            Color32 green = new Color32(0, 140, 69, 255);
            Color32 white = new Color32(255, 255, 255, 255);
            Color32 red = new Color32(205, 33, 42, 255);

            int bandWidth = Width / 3;
            FillRect(pixels, 0, 0, bandWidth, Height - 1, green);
            FillRect(pixels, bandWidth + 1, 0, bandWidth * 2, Height - 1, white);
            FillRect(pixels, bandWidth * 2 + 1, 0, Width - 1, Height - 1, red);

            SaveFlag("Italy.png", pixels);
        }

        private static void GenerateFranceFlag()
        {
            Color32[] pixels = new Color32[Width * Height];
            Color32 blue = new Color32(0, 38, 84, 255);
            Color32 white = new Color32(255, 255, 255, 255);
            Color32 red = new Color32(237, 41, 57, 255);

            int bandWidth = Width / 3;
            FillRect(pixels, 0, 0, bandWidth, Height - 1, blue);
            FillRect(pixels, bandWidth + 1, 0, bandWidth * 2, Height - 1, white);
            FillRect(pixels, bandWidth * 2 + 1, 0, Width - 1, Height - 1, red);

            SaveFlag("France.png", pixels);
        }

        private static void GenerateRussiaFlag()
        {
            Color32[] pixels = new Color32[Width * Height];
            Color32 red = new Color32(213, 43, 30, 255);
            Color32 blue = new Color32(0, 57, 166, 255);
            Color32 white = new Color32(255, 255, 255, 255);

            FillRect(pixels, 0, 0, Width - 1, 106, red);
            FillRect(pixels, 0, 107, Width - 1, 212, blue);
            FillRect(pixels, 0, 213, Width - 1, Height - 1, white);

            SaveFlag("Russia.png", pixels);
        }

        private static void GenerateBrazilFlag()
        {
            Color32[] pixels = new Color32[Width * Height];
            Color32 green = new Color32(0, 156, 59, 255);
            Color32 yellow = new Color32(255, 223, 0, 255);
            Color32 blue = new Color32(0, 39, 118, 255);
            Color32 white = new Color32(255, 255, 255, 255);

            Fill(pixels, green);

            // Large yellow rhombus
            Vector2[] rhombus =
            {
                new Vector2(Width * 0.5f, 26f),
                new Vector2(Width - 28f, Height * 0.5f),
                new Vector2(Width * 0.5f, Height - 26f),
                new Vector2(28f, Height * 0.5f)
            };
            FillPolygon(pixels, rhombus, yellow);

            // Blue celestial circle
            Vector2 center = new Vector2(Width * 0.5f, Height * 0.5f);
            float radius = 70f;
            FillCircle(pixels, center.x, center.y, radius, blue);

            // White curved banner
            for (float x = center.x - radius + 8f; x <= center.x + radius - 8f; x += 0.5f)
            {
                float normalizedX = (x - center.x) / radius;
                float yCurve = center.y + 12f + (normalizedX * 18f) - (normalizedX * normalizedX * 8f);
                for (float y = yCurve - 4f; y <= yCurve + 4f; y += 0.5f)
                {
                    if (Vector2.Distance(new Vector2(x, y), center) <= radius - 2f)
                    {
                        SetPixelSafe(pixels, Mathf.RoundToInt(x), Mathf.RoundToInt(y), white);
                    }
                }
            }

            // White stars
            Vector2[] starPositions =
            {
                center + new Vector2(-15f, -25f),
                center + new Vector2(10f, -22f),
                center + new Vector2(2f, -38f),
                center + new Vector2(22f, -32f),
                center + new Vector2(-28f, -12f),
                center + new Vector2(30f, 18f)
            };
            foreach (Vector2 sp in starPositions)
            {
                FillCircle(pixels, sp.x, sp.y, 2.5f, white);
            }

            SaveFlag("Brazil.png", pixels);
        }

        private static void GenerateChinaFlag()
        {
            Color32[] pixels = new Color32[Width * Height];
            Color32 red = new Color32(222, 41, 16, 255);
            Color32 yellow = new Color32(255, 222, 0, 255);

            Fill(pixels, red);

            // Large star
            Vector2 bigStarCenter = new Vector2(96f, 224f);
            FillStar(pixels, bigStarCenter.x, bigStarCenter.y, 44f, 17f, 90f, yellow);

            // 4 smaller stars in an arc pointing towards big star
            Vector2[] smallStars =
            {
                new Vector2(172f, 276f),
                new Vector2(210f, 240f),
                new Vector2(210f, 196f),
                new Vector2(172f, 160f)
            };

            foreach (Vector2 pos in smallStars)
            {
                Vector2 dir = bigStarCenter - pos;
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                FillStar(pixels, pos.x, pos.y, 15f, 6f, angle, yellow);
            }

            SaveFlag("China.png", pixels);
        }

        private static void GenerateUnitedKingdomFlag()
        {
            Color32[] pixels = new Color32[Width * Height];
            Color32 navy = new Color32(1, 33, 105, 255);
            Color32 white = new Color32(255, 255, 255, 255);
            Color32 red = new Color32(200, 16, 46, 255);

            Fill(pixels, navy);

            // St. Andrew broad white diagonals
            DrawLine(pixels, 0f, 0f, Width - 1f, Height - 1f, 54f, white);
            DrawLine(pixels, 0f, Height - 1f, Width - 1f, 0f, 54f, white);

            // St. Patrick red diagonals
            DrawLine(pixels, 0f, 0f, Width - 1f, Height - 1f, 18f, red);
            DrawLine(pixels, 0f, Height - 1f, Width - 1f, 0f, 18f, red);

            // St. George white cross backing
            float cx = Width * 0.5f;
            float cy = Height * 0.5f;
            FillRect(pixels, Mathf.RoundToInt(cx - 40f), 0, Mathf.RoundToInt(cx + 40f), Height - 1, white);
            FillRect(pixels, 0, Mathf.RoundToInt(cy - 40f), Width - 1, Mathf.RoundToInt(cy + 40f), white);

            // St. George red cross
            FillRect(pixels, Mathf.RoundToInt(cx - 24f), 0, Mathf.RoundToInt(cx + 24f), Height - 1, red);
            FillRect(pixels, 0, Mathf.RoundToInt(cy - 24f), Width - 1, Mathf.RoundToInt(cy + 24f), red);

            SaveFlag("UnitedKingdom.png", pixels);
        }

        private static void GenerateUnitedStatesFlag()
        {
            Color32[] pixels = new Color32[Width * Height];
            Color32 red = new Color32(178, 34, 52, 255);
            Color32 white = new Color32(255, 255, 255, 255);
            Color32 blue = new Color32(60, 59, 110, 255);

            float stripeHeight = (float)Height / 13f;
            for (int i = 0; i < 13; i++)
            {
                Color32 color = (i % 2 == 0) ? red : white;
                int y0 = Mathf.RoundToInt(i * stripeHeight);
                int y1 = Mathf.RoundToInt((i + 1) * stripeHeight);
                FillRect(pixels, 0, y0, Width - 1, y1, color);
            }

            // Canton (blue field, top 7 stripes = y from 6 * stripeHeight to Height)
            int cantonWidth = Mathf.RoundToInt(Width * 0.40f);
            int cantonY0 = Mathf.RoundToInt(6 * stripeHeight);
            FillRect(pixels, 0, cantonY0, cantonWidth, Height - 1, blue);

            // 50 Stars in 9 alternating rows (6, 5, 6, 5, 6, 5, 6, 5, 6)
            float cantonHeight = Height - cantonY0;
            float rowStep = cantonHeight / 10f;
            float colStep = (float)cantonWidth / 12f;

            for (int row = 0; row < 9; row++)
            {
                bool isEvenRow = (row % 2 == 0);
                int count = isEvenRow ? 6 : 5;
                float y = cantonY0 + cantonHeight - ((row + 1) * rowStep);

                for (int col = 0; col < count; col++)
                {
                    float x = isEvenRow ? (col * 2 + 1) * colStep : (col * 2 + 2) * colStep;
                    FillStar(pixels, x, y, 5.5f, 2.2f, 90f, white);
                }
            }

            SaveFlag("UnitedStates.png", pixels);
        }

        private static void ConfigureAllFlagImporters()
        {
            string[] flags =
            {
                "India.png", "Italy.png", "France.png", "Russia.png",
                "Brazil.png", "China.png", "UnitedKingdom.png", "UnitedStates.png"
            };

            foreach (string file in flags)
            {
                string path = $"{FlagsFolder}/{file}";
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

        private static void AssignFlagsToCountryDefinitions()
        {
            AssignFlagToCountry("India", "India.png");
            AssignFlagToCountry("Italy", "Italy.png");
            AssignFlagToCountry("France", "France.png");
            AssignFlagToCountry("Russia", "Russia.png");
            AssignFlagToCountry("Brazil", "Brazil.png");
            AssignFlagToCountry("China", "China.png");
            AssignFlagToCountry("UnitedKingdom", "UnitedKingdom.png");
            AssignFlagToCountry("UnitedStates", "UnitedStates.png");
        }

        private static void AssignFlagToCountry(string countryName, string flagFileName)
        {
            string countryPath = $"{CountriesFolder}/{countryName}.asset";
            string flagPath = $"{FlagsFolder}/{flagFileName}";

            CountryDefinition country = AssetDatabase.LoadAssetAtPath<CountryDefinition>(countryPath);
            Sprite flagSprite = AssetDatabase.LoadAssetAtPath<Sprite>(flagPath);

            if (country != null && flagSprite != null)
            {
                SerializedObject serialized = new SerializedObject(country);
                serialized.FindProperty("_flag").objectReferenceValue = flagSprite;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(country);
            }
        }

        #region Drawing Helpers

        private static void Fill(Color32[] pixels, Color32 color)
        {
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }
        }

        private static void FillRect(Color32[] pixels, int x0, int y0, int x1, int y1, Color32 color)
        {
            x0 = Mathf.Clamp(x0, 0, Width - 1);
            x1 = Mathf.Clamp(x1, 0, Width - 1);
            y0 = Mathf.Clamp(y0, 0, Height - 1);
            y1 = Mathf.Clamp(y1, 0, Height - 1);

            for (int y = y0; y <= y1; y++)
            {
                int row = y * Width;
                for (int x = x0; x <= x1; x++)
                {
                    pixels[row + x] = color;
                }
            }
        }

        private static void FillCircle(Color32[] pixels, float cx, float cy, float radius, Color32 color)
        {
            int minX = Mathf.Max(0, Mathf.FloorToInt(cx - radius));
            int maxX = Mathf.Min(Width - 1, Mathf.CeilToInt(cx + radius));
            int minY = Mathf.Max(0, Mathf.FloorToInt(cy - radius));
            int maxY = Mathf.Min(Height - 1, Mathf.CeilToInt(cy + radius));
            float rSq = radius * radius;

            for (int y = minY; y <= maxY; y++)
            {
                int row = y * Width;
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
            int maxX = Mathf.Min(Width - 1, Mathf.CeilToInt(cx + radius + thickness));
            int minY = Mathf.Max(0, Mathf.FloorToInt(cy - radius - thickness));
            int maxY = Mathf.Min(Height - 1, Mathf.CeilToInt(cy + radius + thickness));
            float halfThick = thickness * 0.5f;

            for (int y = minY; y <= maxY; y++)
            {
                int row = y * Width;
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
            float maxX = Mathf.Min(Width - 1f, Mathf.Max(x0, x1) + thickness);
            float minY = Mathf.Max(0f, Mathf.Min(y0, y1) - thickness);
            float maxY = Mathf.Min(Height - 1f, Mathf.Max(y0, y1) + thickness);
            float halfThick = thickness * 0.5f;

            for (int y = (int)minY; y <= (int)maxY; y++)
            {
                int row = y * Width;
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
            int maxX = Mathf.Min(Width - 1, Mathf.CeilToInt(cx + outerR));
            int minY = Mathf.Max(0, Mathf.FloorToInt(cy - outerR));
            int maxY = Mathf.Min(Height - 1, Mathf.CeilToInt(cy + outerR));

            for (int y = minY; y <= maxY; y++)
            {
                int row = y * Width;
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
            float minX = Width;
            float maxX = 0;
            float minY = Height;
            float maxY = 0;

            foreach (Vector2 v in vertices)
            {
                if (v.x < minX) minX = v.x;
                if (v.x > maxX) maxX = v.x;
                if (v.y < minY) minY = v.y;
                if (v.y > maxY) maxY = v.y;
            }

            int iMinX = Mathf.Max(0, Mathf.FloorToInt(minX));
            int iMaxX = Mathf.Min(Width - 1, Mathf.CeilToInt(maxX));
            int iMinY = Mathf.Max(0, Mathf.FloorToInt(minY));
            int iMaxY = Mathf.Min(Height - 1, Mathf.CeilToInt(maxY));

            for (int y = iMinY; y <= iMaxY; y++)
            {
                int row = y * Width;
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
            if (x >= 0 && x < Width && y >= 0 && y < Height)
            {
                pixels[y * Width + x] = color;
            }
        }

        #endregion
    }
}
