using System;
using System.Collections.Generic;
using System.IO;
using Ropoly.Infrastructure.Content.Board;
using Ropoly.Presentation.Board;
using UnityEditor;
using UnityEngine;

namespace Ropoly.Editor
{
    public static class BoardVisualsGenerator
    {
        private const string MapsFolder = "Assets/_Ropoly/Art/Maps";
        private const string CitiesFolder = "Assets/_Ropoly/Art/Cities";
        private const string BoardAssetPath = "Assets/_Ropoly/Content/Boards/ClassicWorld/ClassicWorldBoard.asset";

        private struct CityInfo
        {
            public string Id;
            public string DisplayName;
            public string CountryId;
            public Vector2 NormalizedCityCoord; // [0..1, 0..1] on country map
            public string LandmarkName;
            public string AssetFile;
        }

        private struct CountryInfo
        {
            public string Id;
            public string DisplayName;
            public string Code;
            public Color Color;
            public Vector2[] Polygon;
        }

        private static readonly Dictionary<string, CountryInfo> Countries = new()
        {
            ["country-france"] = new CountryInfo
            {
                Id = "country-france",
                DisplayName = "FRANCE",
                Code = "FR",
                Color = new Color(0.16f, 0.50f, 0.73f), // Royal Blue
                Polygon = new[]
                {
                    new Vector2(0.38f, 0.88f), new Vector2(0.52f, 0.86f), new Vector2(0.70f, 0.72f),
                    new Vector2(0.84f, 0.58f), new Vector2(0.78f, 0.38f), new Vector2(0.72f, 0.14f),
                    new Vector2(0.56f, 0.12f), new Vector2(0.36f, 0.18f), new Vector2(0.24f, 0.36f),
                    new Vector2(0.26f, 0.58f), new Vector2(0.12f, 0.70f), new Vector2(0.24f, 0.76f)
                }
            },
            ["country-united-kingdom"] = new CountryInfo
            {
                Id = "country-united-kingdom",
                DisplayName = "UNITED KINGDOM",
                Code = "GB",
                Color = new Color(0.91f, 0.26f, 0.58f), // Deep Pink
                Polygon = new[]
                {
                    new Vector2(0.44f, 0.94f), new Vector2(0.54f, 0.92f), new Vector2(0.60f, 0.76f),
                    new Vector2(0.54f, 0.66f), new Vector2(0.68f, 0.58f), new Vector2(0.78f, 0.44f),
                    new Vector2(0.86f, 0.30f), new Vector2(0.76f, 0.18f), new Vector2(0.54f, 0.14f),
                    new Vector2(0.28f, 0.16f), new Vector2(0.38f, 0.32f), new Vector2(0.36f, 0.44f),
                    new Vector2(0.42f, 0.58f), new Vector2(0.32f, 0.74f), new Vector2(0.34f, 0.88f)
                }
            },
            ["country-united-states"] = new CountryInfo
            {
                Id = "country-united-states",
                DisplayName = "UNITED STATES",
                Code = "US",
                Color = new Color(0.20f, 0.29f, 0.37f), // Slate Blue
                Polygon = new[]
                {
                    new Vector2(0.06f, 0.82f), new Vector2(0.36f, 0.84f), new Vector2(0.68f, 0.84f),
                    new Vector2(0.82f, 0.76f), new Vector2(0.94f, 0.80f), new Vector2(0.90f, 0.58f),
                    new Vector2(0.82f, 0.38f), new Vector2(0.84f, 0.22f), new Vector2(0.76f, 0.26f),
                    new Vector2(0.68f, 0.34f), new Vector2(0.56f, 0.22f), new Vector2(0.48f, 0.30f),
                    new Vector2(0.34f, 0.38f), new Vector2(0.14f, 0.36f), new Vector2(0.08f, 0.54f),
                    new Vector2(0.04f, 0.72f)
                }
            },
            ["country-italy"] = new CountryInfo
            {
                Id = "country-italy",
                DisplayName = "ITALY",
                Code = "IT",
                Color = new Color(0.15f, 0.68f, 0.38f), // Emerald Green
                Polygon = new[]
                {
                    new Vector2(0.18f, 0.90f), new Vector2(0.42f, 0.90f), new Vector2(0.56f, 0.82f),
                    new Vector2(0.48f, 0.68f), new Vector2(0.58f, 0.56f), new Vector2(0.72f, 0.44f),
                    new Vector2(0.86f, 0.36f), new Vector2(0.78f, 0.28f), new Vector2(0.66f, 0.32f),
                    new Vector2(0.62f, 0.16f), new Vector2(0.52f, 0.18f), new Vector2(0.48f, 0.32f),
                    new Vector2(0.40f, 0.48f), new Vector2(0.28f, 0.62f), new Vector2(0.18f, 0.76f)
                }
            },
            ["country-china"] = new CountryInfo
            {
                Id = "country-china",
                DisplayName = "CHINA",
                Code = "CN",
                Color = new Color(0.91f, 0.30f, 0.24f), // Imperial Red
                Polygon = new[]
                {
                    new Vector2(0.14f, 0.62f), new Vector2(0.26f, 0.78f), new Vector2(0.48f, 0.72f),
                    new Vector2(0.66f, 0.84f), new Vector2(0.86f, 0.90f), new Vector2(0.82f, 0.70f),
                    new Vector2(0.76f, 0.62f), new Vector2(0.88f, 0.52f), new Vector2(0.82f, 0.36f),
                    new Vector2(0.74f, 0.22f), new Vector2(0.60f, 0.20f), new Vector2(0.54f, 0.30f),
                    new Vector2(0.38f, 0.34f), new Vector2(0.22f, 0.42f), new Vector2(0.12f, 0.50f)
                }
            },
            ["country-brazil"] = new CountryInfo
            {
                Id = "country-brazil",
                DisplayName = "BRAZIL",
                Code = "BR",
                Color = new Color(0.95f, 0.61f, 0.07f), // Warm Gold
                Polygon = new[]
                {
                    new Vector2(0.36f, 0.90f), new Vector2(0.56f, 0.88f), new Vector2(0.78f, 0.76f),
                    new Vector2(0.92f, 0.62f), new Vector2(0.82f, 0.46f), new Vector2(0.72f, 0.32f),
                    new Vector2(0.58f, 0.22f), new Vector2(0.48f, 0.12f), new Vector2(0.42f, 0.24f),
                    new Vector2(0.46f, 0.40f), new Vector2(0.32f, 0.48f), new Vector2(0.16f, 0.58f),
                    new Vector2(0.20f, 0.74f)
                }
            },
            ["country-russia"] = new CountryInfo
            {
                Id = "country-russia",
                DisplayName = "RUSSIA",
                Code = "RU",
                Color = new Color(0.56f, 0.27f, 0.68f), // Royal Violet
                Polygon = new[]
                {
                    new Vector2(0.12f, 0.78f), new Vector2(0.28f, 0.86f), new Vector2(0.52f, 0.86f),
                    new Vector2(0.76f, 0.84f), new Vector2(0.92f, 0.80f), new Vector2(0.88f, 0.62f),
                    new Vector2(0.94f, 0.48f), new Vector2(0.82f, 0.42f), new Vector2(0.66f, 0.52f),
                    new Vector2(0.48f, 0.54f), new Vector2(0.34f, 0.56f), new Vector2(0.20f, 0.48f),
                    new Vector2(0.14f, 0.60f)
                }
            },
            ["country-india"] = new CountryInfo
            {
                Id = "country-india",
                DisplayName = "INDIA",
                Code = "IN",
                Color = new Color(0.95f, 0.77f, 0.06f), // Amber Gold
                Polygon = new[]
                {
                    new Vector2(0.44f, 0.94f), new Vector2(0.54f, 0.86f), new Vector2(0.68f, 0.78f),
                    new Vector2(0.84f, 0.66f), new Vector2(0.72f, 0.56f), new Vector2(0.68f, 0.42f),
                    new Vector2(0.58f, 0.24f), new Vector2(0.48f, 0.10f), new Vector2(0.40f, 0.20f),
                    new Vector2(0.32f, 0.36f), new Vector2(0.26f, 0.48f), new Vector2(0.18f, 0.58f),
                    new Vector2(0.30f, 0.66f), new Vector2(0.36f, 0.82f)
                }
            }
        };

        private static readonly CityInfo[] Cities =
        {
            // France
            new() { Id = "city-paris", DisplayName = "Paris", CountryId = "country-france", NormalizedCityCoord = new Vector2(0.48f, 0.68f), LandmarkName = "Eiffel Tower", AssetFile = "Paris" },
            new() { Id = "city-marseille", DisplayName = "Marseille", CountryId = "country-france", NormalizedCityCoord = new Vector2(0.68f, 0.16f), LandmarkName = "Notre-Dame de la Garde", AssetFile = "Marseille" },

            // UK
            new() { Id = "city-london", DisplayName = "London", CountryId = "country-united-kingdom", NormalizedCityCoord = new Vector2(0.70f, 0.26f), LandmarkName = "Big Ben", AssetFile = "London" },
            new() { Id = "city-liverpool", DisplayName = "Liverpool", CountryId = "country-united-kingdom", NormalizedCityCoord = new Vector2(0.48f, 0.50f), LandmarkName = "Royal Liver Building", AssetFile = "Liverpool" },
            new() { Id = "city-manchester", DisplayName = "Manchester", CountryId = "country-united-kingdom", NormalizedCityCoord = new Vector2(0.56f, 0.52f), LandmarkName = "Manchester Town Hall", AssetFile = "Manchester" },

            // USA
            new() { Id = "city-new-york", DisplayName = "New York", CountryId = "country-united-states", NormalizedCityCoord = new Vector2(0.86f, 0.68f), LandmarkName = "Statue of Liberty", AssetFile = "NewYork" },
            new() { Id = "city-los-angeles", DisplayName = "Los Angeles", CountryId = "country-united-states", NormalizedCityCoord = new Vector2(0.12f, 0.42f), LandmarkName = "Hollywood Skyline", AssetFile = "LosAngeles" },
            new() { Id = "city-san-francisco", DisplayName = "San Francisco", CountryId = "country-united-states", NormalizedCityCoord = new Vector2(0.08f, 0.56f), LandmarkName = "Golden Gate Bridge", AssetFile = "SanFrancisco" },

            // Italy
            new() { Id = "city-rome", DisplayName = "Rome", CountryId = "country-italy", NormalizedCityCoord = new Vector2(0.48f, 0.50f), LandmarkName = "Colosseum", AssetFile = "Rome" },
            new() { Id = "city-milan", DisplayName = "Milan", CountryId = "country-italy", NormalizedCityCoord = new Vector2(0.30f, 0.84f), LandmarkName = "Duomo di Milano", AssetFile = "Milan" },

            // China
            new() { Id = "city-beijing", DisplayName = "Beijing", CountryId = "country-china", NormalizedCityCoord = new Vector2(0.68f, 0.74f), LandmarkName = "Temple of Heaven", AssetFile = "Beijing" },
            new() { Id = "city-shanghai", DisplayName = "Shanghai", CountryId = "country-china", NormalizedCityCoord = new Vector2(0.84f, 0.50f), LandmarkName = "Oriental Pearl Tower", AssetFile = "Shanghai" },
            new() { Id = "city-shenzhen", DisplayName = "Shenzhen", CountryId = "country-china", NormalizedCityCoord = new Vector2(0.72f, 0.26f), LandmarkName = "Ping An Tower", AssetFile = "Shenzhen" },

            // Brazil
            new() { Id = "city-brasilia", DisplayName = "Brasilia", CountryId = "country-brazil", NormalizedCityCoord = new Vector2(0.58f, 0.52f), LandmarkName = "Cathedral of Brasilia", AssetFile = "Brasilia" },
            new() { Id = "city-rio-de-janeiro", DisplayName = "Rio de Janeiro", CountryId = "country-brazil", NormalizedCityCoord = new Vector2(0.68f, 0.34f), LandmarkName = "Christ the Redeemer", AssetFile = "RioDeJaneiro" },
            new() { Id = "city-sao-paulo", DisplayName = "Sao Paulo", CountryId = "country-brazil", NormalizedCityCoord = new Vector2(0.60f, 0.30f), LandmarkName = "Octavio Frias Bridge", AssetFile = "SaoPaulo" },

            // Russia
            new() { Id = "city-moscow", DisplayName = "Moscow", CountryId = "country-russia", NormalizedCityCoord = new Vector2(0.24f, 0.66f), LandmarkName = "Saint Basil's Cathedral", AssetFile = "Moscow" },
            new() { Id = "city-saint-petersburg", DisplayName = "Saint Petersburg", CountryId = "country-russia", NormalizedCityCoord = new Vector2(0.20f, 0.76f), LandmarkName = "Spilled Blood Cathedral", AssetFile = "SaintPetersburg" },
            new() { Id = "city-kazan", DisplayName = "Kazan", CountryId = "country-russia", NormalizedCityCoord = new Vector2(0.32f, 0.64f), LandmarkName = "Qol Sharif Mosque", AssetFile = "Kazan" },

            // India
            new() { Id = "city-delhi", DisplayName = "Delhi", CountryId = "country-india", NormalizedCityCoord = new Vector2(0.44f, 0.74f), LandmarkName = "India Gate", AssetFile = "Delhi" },
            new() { Id = "city-mumbai", DisplayName = "Mumbai", CountryId = "country-india", NormalizedCityCoord = new Vector2(0.28f, 0.44f), LandmarkName = "Gateway of India", AssetFile = "Mumbai" },
            new() { Id = "city-bengaluru", DisplayName = "Bengaluru", CountryId = "country-india", NormalizedCityCoord = new Vector2(0.44f, 0.24f), LandmarkName = "Vidhana Soudha", AssetFile = "Bengaluru" }
        };

        [MenuItem("Ropoly/Visuals/Generate All Board Visuals (Maps & Cities)")]
        public static void GenerateAll()
        {
            EnsureFolder("Assets/_Ropoly/Art");
            EnsureFolder(MapsFolder);
            EnsureFolder(CitiesFolder);

            GenerateAllCountryMaps();
            GenerateMissingCityArtworks();
            BindVisualsToContentDefinitions();

            AssetDatabase.Refresh();
            Debug.Log("Successfully generated all board city photos, country maps, and updated board definitions.");
        }

        public static void GenerateAllCountryMaps()
        {
            int size = 512;
            foreach (CityInfo city in Cities)
            {
                if (!Countries.TryGetValue(city.CountryId, out CountryInfo country))
                {
                    continue;
                }

                Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                FillBackground(texture, new Color(0.045f, 0.035f, 0.085f, 1f));

                // Draw tactical grid
                DrawGrid(texture, new Color(0.09f, 0.07f, 0.16f, 0.7f), 32);

                // Draw country silhouette with theme color
                Color fillColor = new Color(country.Color.r, country.Color.g, country.Color.b, 0.28f);
                Color outlineColor = new Color(country.Color.r, country.Color.g, country.Color.b, 0.95f);
                DrawPolygon(texture, country.Polygon, fillColor, outlineColor, 3f);

                // Draw other cities in country as subtle small dots
                foreach (CityInfo other in Cities)
                {
                    if (other.CountryId == city.CountryId && other.Id != city.Id)
                    {
                        Vector2 otherPixel = new Vector2(other.NormalizedCityCoord.x * size, other.NormalizedCityCoord.y * size);
                        DrawGlowCircle(texture, (int)otherPixel.x, (int)otherPixel.y, 4, new Color(0.6f, 0.6f, 0.8f, 0.5f), Color.clear);
                    }
                }

                // Draw active city highlight beacon
                Vector2 targetPixel = new Vector2(city.NormalizedCityCoord.x * size, city.NormalizedCityCoord.y * size);
                DrawCityBeacon(texture, (int)targetPixel.x, (int)targetPixel.y, country.Color);

                // Draw sleek border card
                DrawCardBorder(texture, new Color(0.18f, 0.14f, 0.30f, 1f), 3);

                texture.Apply();

                string filePath = $"{MapsFolder}/{city.AssetFile}_Map.png";
                File.WriteAllBytes(filePath, texture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(texture);

                ConfigureSpriteImporter(filePath);
            }
        }

        public static void GenerateMissingCityArtworks()
        {
            int size = 512;
            foreach (CityInfo city in Cities)
            {
                string existingJpg = $"{CitiesFolder}/{city.AssetFile}.jpg";
                string existingPng = $"{CitiesFolder}/{city.AssetFile}.png";

                if (File.Exists(existingJpg))
                {
                    ConfigureSpriteImporter(existingJpg);
                    continue;
                }

                if (File.Exists(existingPng))
                {
                    ConfigureSpriteImporter(existingPng);
                    continue;
                }

                // Generate rich vector artwork for this city
                Countries.TryGetValue(city.CountryId, out CountryInfo country);
                Texture2D texture = GenerateProceduralCityArtwork(city, country, size);

                string outPath = $"{CitiesFolder}/{city.AssetFile}.png";
                File.WriteAllBytes(outPath, texture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(texture);

                ConfigureSpriteImporter(outPath);
            }
        }

        private static Texture2D GenerateProceduralCityArtwork(CityInfo city, CountryInfo country, int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);

            // Sky gradient (twilight / sunset glow)
            Color skyTop = new Color(0.04f, 0.03f, 0.09f, 1f);
            Color skyBottom = Color.Lerp(new Color(0.12f, 0.08f, 0.22f), country.Color, 0.40f);
            for (int y = 0; y < size; y++)
            {
                float t = (float)y / size;
                Color col = Color.Lerp(skyBottom, skyTop, t);
                for (int x = 0; x < size; x++)
                {
                    tex.SetPixel(x, y, col);
                }
            }

            // Distant skyline silhouettes
            DrawCitySkyline(tex, size, country.Color);

            // Iconic landmark silhouette & glowing accents
            DrawLandmarkFeature(tex, city.AssetFile, size, country.Color);

            // Card frame
            DrawCardBorder(tex, new Color(country.Color.r, country.Color.g, country.Color.b, 0.75f), 4);

            tex.Apply();
            return tex;
        }

        private static void DrawCitySkyline(Texture2D tex, int size, Color accent)
        {
            Color buildingColor = new Color(0.08f, 0.06f, 0.15f, 0.95f);
            Color windowColor = new Color(0.95f, 0.85f, 0.55f, 0.85f);

            // 10 background buildings
            int[] heights = { 180, 260, 210, 320, 280, 350, 230, 290, 310, 190 };
            int bWidth = size / heights.Length;

            for (int i = 0; i < heights.Length; i++)
            {
                int startX = i * bWidth;
                int h = heights[i];
                for (int x = startX + 2; x < startX + bWidth - 2 && x < size; x++)
                {
                    for (int y = 0; y < h; y++)
                    {
                        tex.SetPixel(x, y, buildingColor);
                        // Windows
                        if (y > 60 && y < h - 20 && (x % 12 > 4) && (y % 16 > 6))
                        {
                            if ((x * 7 + y * 13) % 5 > 1)
                            {
                                tex.SetPixel(x, y, windowColor);
                            }
                        }
                    }
                }
            }
        }

        private static void DrawLandmarkFeature(Texture2D tex, string cityFile, int size, Color accent)
        {
            Color structureColor = new Color(0.92f, 0.88f, 0.98f, 1f);
            Color accentGlow = Color.Lerp(accent, Color.white, 0.5f);

            int cx = size / 2;

            switch (cityFile)
            {
                case "Marseille": // Notre-Dame de la Garde on hill
                    DrawHill(tex, cx, 140, 220, new Color(0.10f, 0.08f, 0.18f));
                    DrawCathedral(tex, cx, 140, 180, structureColor, accentGlow);
                    break;
                case "Liverpool": // Royal Liver Building with twin towers
                    DrawTwinTowerBuilding(tex, cx, 60, 260, 140, structureColor, accentGlow);
                    break;
                case "Manchester": // Gothic Town Hall clock tower
                    DrawClockTower(tex, cx, 60, 310, 80, structureColor, accentGlow);
                    break;
                case "Shenzhen": // Ping An Finance Centre supertall spire
                    DrawSuperTallSpire(tex, cx, 60, 380, 70, structureColor, accentGlow);
                    break;
                case "SaoPaulo": // Octavio Frias cable-stayed X bridge
                    DrawCableBridge(tex, cx, 60, 280, 180, structureColor, accentGlow);
                    break;
                case "Brasilia": // Cathedral of Brasilia crown
                    DrawBrasiliaCathedral(tex, cx, 80, 180, 190, structureColor, accentGlow);
                    break;
                case "Kazan": // Qol Sharif Mosque minarets & blue domes
                    DrawMosque(tex, cx, 60, 240, 160, structureColor, accentGlow);
                    break;
                case "Mumbai": // Gateway of India triumphal arch
                    DrawTriumphalArch(tex, cx, 60, 210, 170, structureColor, accentGlow);
                    break;
                case "Bengaluru": // Vidhana Soudha dome & pillars
                    DrawPalace(tex, cx, 60, 190, 220, structureColor, accentGlow);
                    break;
            }
        }

        private static void DrawHill(Texture2D tex, int cx, int cy, int radius, Color color)
        {
            for (int x = cx - radius; x <= cx + radius; x++)
            {
                if (x < 0 || x >= tex.width) continue;
                float dx = (float)(x - cx) / radius;
                int topY = (int)(cy * (1f - dx * dx));
                for (int y = 0; y <= topY; y++)
                {
                    tex.SetPixel(x, y, color);
                }
            }
        }

        private static void DrawCathedral(Texture2D tex, int cx, int baseY, int height, Color body, Color glow)
        {
            // Base nave
            DrawRect(tex, cx - 40, baseY, 80, height / 2, body);
            // Bell tower & spire
            DrawRect(tex, cx - 18, baseY + height / 2, 36, height / 3, body);
            DrawTriangle(tex, cx, baseY + height, 24, glow);
        }

        private static void DrawTwinTowerBuilding(Texture2D tex, int cx, int baseY, int height, int width, Color body, Color glow)
        {
            // Main hall
            DrawRect(tex, cx - width / 2, baseY, width, height * 2 / 3, body);
            // Left tower
            DrawRect(tex, cx - width / 2 + 10, baseY + height * 2 / 3, 30, height / 3, body);
            DrawTriangle(tex, cx - width / 2 + 25, baseY + height + 15, 20, glow);
            // Right tower
            DrawRect(tex, cx + width / 2 - 40, baseY + height * 2 / 3, 30, height / 3, body);
            DrawTriangle(tex, cx + width / 2 - 25, baseY + height + 15, 20, glow);
        }

        private static void DrawClockTower(Texture2D tex, int cx, int baseY, int height, int width, Color body, Color glow)
        {
            DrawRect(tex, cx - width / 2, baseY, width, height * 3 / 4, body);
            // Clock face
            DrawGlowCircle(tex, cx, baseY + height / 2, 16, glow, body);
            // Spire
            DrawTriangle(tex, cx, baseY + height, width, glow);
        }

        private static void DrawSuperTallSpire(Texture2D tex, int cx, int baseY, int height, int width, Color body, Color glow)
        {
            for (int y = baseY; y < baseY + height; y++)
            {
                float t = (float)(y - baseY) / height;
                int w = (int)Mathf.Lerp(width / 2, 2, t);
                for (int x = cx - w; x <= cx + w; x++)
                {
                    Color c = Color.Lerp(body, glow, t);
                    tex.SetPixel(x, y, c);
                }
            }
        }

        private static void DrawCableBridge(Texture2D tex, int cx, int baseY, int height, int width, Color body, Color glow)
        {
            // X tower
            DrawLine(tex, cx - 40, baseY, cx + 40, baseY + height, 6, body);
            DrawLine(tex, cx + 40, baseY, cx - 40, baseY + height, 6, body);
            // Roadway
            DrawRect(tex, cx - width / 2, baseY + height / 3, width, 12, body);
            // Cables
            for (int i = 0; i < 6; i++)
            {
                float t = (float)i / 6f;
                int rx = (int)Mathf.Lerp(cx - width / 2, cx - 20, t);
                DrawLine(tex, cx, baseY + height * 3 / 4, rx, baseY + height / 3, 2, glow);
                int lx = (int)Mathf.Lerp(cx + 20, cx + width / 2, t);
                DrawLine(tex, cx, baseY + height * 3 / 4, lx, baseY + height / 3, 2, glow);
            }
        }

        private static void DrawBrasiliaCathedral(Texture2D tex, int cx, int baseY, int height, int width, Color body, Color glow)
        {
            int colCount = 12;
            for (int i = 0; i < colCount; i++)
            {
                float angle = ((float)i / colCount) * Mathf.PI;
                int startX = cx + (int)(Mathf.Cos(angle) * width * 0.5f);
                int endX = cx + (int)(Mathf.Cos(angle) * width * 0.25f);
                DrawLine(tex, startX, baseY, cx, baseY + height / 2, 4, body);
                DrawLine(tex, cx, baseY + height / 2, endX, baseY + height, 4, glow);
            }
        }

        private static void DrawMosque(Texture2D tex, int cx, int baseY, int height, int width, Color body, Color glow)
        {
            // Central dome
            DrawGlowCircle(tex, cx, baseY + height / 2, 45, glow, body);
            // Main hall
            DrawRect(tex, cx - width / 3, baseY, width * 2 / 3, height / 2, body);
            // 4 Minarets
            int mWidth = 14;
            int[] mPositions = { cx - width / 2, cx - width / 4, cx + width / 4, cx + width / 2 - mWidth };
            foreach (int mx in mPositions)
            {
                DrawRect(tex, mx, baseY, mWidth, height * 4 / 5, body);
                DrawTriangle(tex, mx + mWidth / 2, baseY + height, mWidth, glow);
            }
        }

        private static void DrawTriumphalArch(Texture2D tex, int cx, int baseY, int height, int width, Color body, Color glow)
        {
            // Left & Right pillars
            DrawRect(tex, cx - width / 2, baseY, width / 3, height, body);
            DrawRect(tex, cx + width / 6, baseY, width / 3, height, body);
            // Top lintel
            DrawRect(tex, cx - width / 2 - 10, baseY + height * 3 / 4, width + 20, height / 4, body);
            // Domes / finials
            DrawGlowCircle(tex, cx, baseY + height + 10, 18, glow, body);
        }

        private static void DrawPalace(Texture2D tex, int cx, int baseY, int height, int width, Color body, Color glow)
        {
            // Base long facade
            DrawRect(tex, cx - width / 2, baseY, width, height / 2, body);
            // Pillars
            for (int x = cx - width / 2 + 10; x < cx + width / 2 - 10; x += 20)
            {
                DrawRect(tex, x, baseY, 6, height / 2, glow);
            }
            // Central grand dome
            DrawGlowCircle(tex, cx, baseY + height * 3 / 4, 35, glow, body);
            DrawTriangle(tex, cx, baseY + height + 10, 14, glow);
        }

        private static void DrawCityBeacon(Texture2D tex, int cx, int cy, Color themeColor)
        {
            Color glowCore = Color.white;
            Color haloColor = Color.Lerp(themeColor, Color.white, 0.65f);

            // Large outer pulsating ring
            DrawRing(tex, cx, cy, 38, 3, new Color(haloColor.r, haloColor.g, haloColor.b, 0.40f));
            DrawRing(tex, cx, cy, 26, 3, new Color(haloColor.r, haloColor.g, haloColor.b, 0.75f));
            DrawRing(tex, cx, cy, 14, 4, haloColor);

            // Solid inner dot
            DrawGlowCircle(tex, cx, cy, 8, glowCore, haloColor);

            // Crosshair tick marks
            DrawLine(tex, cx - 48, cy, cx - 18, cy, 2, haloColor);
            DrawLine(tex, cx + 18, cy, cx + 48, cy, 2, haloColor);
            DrawLine(tex, cx, cy - 48, cx, cy - 18, 2, haloColor);
            DrawLine(tex, cx, cy + 18, cx, cy + 48, 2, haloColor);
        }

        private static void BindVisualsToContentDefinitions()
        {
            BoardDefinition board = AssetDatabase.LoadAssetAtPath<BoardDefinition>(BoardAssetPath);
            if (board == null)
            {
                Debug.LogWarning($"Board definition not found at {BoardAssetPath}");
                return;
            }

            foreach (CountryDefinition country in board.Countries)
            {
                if (country == null) continue;

                foreach (CityDefinition city in country.Cities)
                {
                    if (city == null) continue;

                    string cityArtworkPath = FindCityArtworkPath(city.ContentId);
                    string mapPath = $"{MapsFolder}/{GetCityAssetFile(city.ContentId)}_Map.png";

                    SerializedObject so = new SerializedObject(city);
                    if (!string.IsNullOrEmpty(cityArtworkPath))
                    {
                        Sprite art = AssetDatabase.LoadAssetAtPath<Sprite>(cityArtworkPath);
                        so.FindProperty("_artwork").objectReferenceValue = art;
                    }

                    if (File.Exists(mapPath))
                    {
                        Sprite map = AssetDatabase.LoadAssetAtPath<Sprite>(mapPath);
                        SerializedProperty mapProp = so.FindProperty("_countryMap");
                        if (mapProp != null)
                        {
                            mapProp.objectReferenceValue = map;
                        }
                    }

                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(city);
                }

                EditorUtility.SetDirty(country);
            }

            AssetDatabase.SaveAssets();
        }

        private static string FindCityArtworkPath(string contentId)
        {
            string baseName = GetCityAssetFile(contentId);
            string png = $"{CitiesFolder}/{baseName}.png";
            if (File.Exists(png)) return png;
            string jpg = $"{CitiesFolder}/{baseName}.jpg";
            if (File.Exists(jpg)) return jpg;
            return null;
        }

        private static string GetCityAssetFile(string contentId)
        {
            foreach (CityInfo c in Cities)
            {
                if (c.Id == contentId) return c.AssetFile;
            }
            return contentId.Replace("city-", string.Empty);
        }

        private static void ConfigureSpriteImporter(string path)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.maxTextureSize = 512;
                importer.SaveAndReimport();
            }
        }

        // --- Drawing Helpers ---

        private static void FillBackground(Texture2D tex, Color color)
        {
            Color[] pixels = new Color[tex.width * tex.height];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
            tex.SetPixels(pixels);
        }

        private static void DrawGrid(Texture2D tex, Color color, int spacing)
        {
            for (int x = 0; x < tex.width; x += spacing)
            {
                for (int y = 0; y < tex.height; y++) tex.SetPixel(x, y, color);
            }
            for (int y = 0; y < tex.height; y += spacing)
            {
                for (int x = 0; x < tex.width; x++) tex.SetPixel(x, y, color);
            }
        }

        private static void DrawCardBorder(Texture2D tex, Color color, int thickness)
        {
            for (int i = 0; i < thickness; i++)
            {
                for (int x = 0; x < tex.width; x++)
                {
                    tex.SetPixel(x, i, color);
                    tex.SetPixel(x, tex.height - 1 - i, color);
                }
                for (int y = 0; y < tex.height; y++)
                {
                    tex.SetPixel(i, y, color);
                    tex.SetPixel(tex.width - 1 - i, y, color);
                }
            }
        }

        private static void DrawPolygon(Texture2D tex, Vector2[] normPolygon, Color fillColor, Color outlineColor, float outlineWidth)
        {
            int w = tex.width;
            int h = tex.height;
            Vector2[] points = new Vector2[normPolygon.Length];
            for (int i = 0; i < points.Length; i++)
            {
                points[i] = new Vector2(normPolygon[i].x * w, normPolygon[i].y * h);
            }

            // Fill polygon (Ray casting)
            for (int y = 0; y < h; y++)
            {
                List<float> nodeX = new List<float>();
                int j = points.Length - 1;
                for (int i = 0; i < points.Length; i++)
                {
                    if ((points[i].y < y && points[j].y >= y) || (points[j].y < y && points[i].y >= y))
                    {
                        nodeX.Add(points[i].x + (y - points[i].y) / (points[j].y - points[i].y) * (points[j].x - points[i].x));
                    }
                    j = i;
                }

                nodeX.Sort();
                for (int i = 0; i < nodeX.Count; i += 2)
                {
                    if (i + 1 >= nodeX.Count) break;
                    int startX = Mathf.Clamp((int)nodeX[i], 0, w - 1);
                    int endX = Mathf.Clamp((int)nodeX[i + 1], 0, w - 1);
                    for (int x = startX; x <= endX; x++)
                    {
                        tex.SetPixel(x, y, fillColor);
                    }
                }
            }

            // Draw outline
            for (int i = 0; i < points.Length; i++)
            {
                int next = (i + 1) % points.Length;
                DrawLine(tex, (int)points[i].x, (int)points[i].y, (int)points[next].x, (int)points[next].y, (int)outlineWidth, outlineColor);
            }
        }

        private static void DrawLine(Texture2D tex, int x0, int y0, int x1, int y1, int thickness, Color color)
        {
            int dx = Math.Abs(x1 - x0);
            int dy = Math.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            int rad = thickness / 2;

            while (true)
            {
                for (int rx = -rad; rx <= rad; rx++)
                {
                    for (int ry = -rad; ry <= rad; ry++)
                    {
                        int px = x0 + rx;
                        int py = y0 + ry;
                        if (px >= 0 && px < tex.width && py >= 0 && py < tex.height)
                        {
                            tex.SetPixel(px, py, color);
                        }
                    }
                }

                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x0 += sx; }
                if (e2 < dx) { err += dx; y0 += sy; }
            }
        }

        private static void DrawRect(Texture2D tex, int x, int y, int width, int height, Color color)
        {
            for (int px = x; px < x + width; px++)
            {
                for (int py = y; py < y + height; py++)
                {
                    if (px >= 0 && px < tex.width && py >= 0 && py < tex.height)
                    {
                        tex.SetPixel(px, py, color);
                    }
                }
            }
        }

        private static void DrawTriangle(Texture2D tex, int cx, int topY, int width, Color color)
        {
            int halfW = width / 2;
            int baseY = topY - width;
            for (int y = baseY; y <= topY; y++)
            {
                float t = (float)(y - baseY) / (topY - baseY + 1);
                int span = (int)((1f - t) * halfW);
                for (int x = cx - span; x <= cx + span; x++)
                {
                    if (x >= 0 && x < tex.width && y >= 0 && y < tex.height)
                    {
                        tex.SetPixel(x, y, color);
                    }
                }
            }
        }

        private static void DrawRing(Texture2D tex, int cx, int cy, int radius, int thickness, Color color)
        {
            int r2Inner = (radius - thickness) * (radius - thickness);
            int r2Outer = radius * radius;
            for (int x = cx - radius; x <= cx + radius; x++)
            {
                for (int y = cy - radius; y <= cy + radius; y++)
                {
                    if (x >= 0 && x < tex.width && y >= 0 && y < tex.height)
                    {
                        int d2 = (x - cx) * (x - cx) + (y - cy) * (y - cy);
                        if (d2 >= r2Inner && d2 <= r2Outer)
                        {
                            tex.SetPixel(x, y, color);
                        }
                    }
                }
            }
        }

        private static void DrawGlowCircle(Texture2D tex, int cx, int cy, int radius, Color coreColor, Color edgeColor)
        {
            int r2 = radius * radius;
            for (int x = cx - radius; x <= cx + radius; x++)
            {
                for (int y = cy - radius; y <= cy + radius; y++)
                {
                    if (x >= 0 && x < tex.width && y >= 0 && y < tex.height)
                    {
                        int d2 = (x - cx) * (x - cx) + (y - cy) * (y - cy);
                        if (d2 <= r2)
                        {
                            float t = Mathf.Sqrt(d2) / radius;
                            Color c = Color.Lerp(coreColor, edgeColor, t);
                            tex.SetPixel(x, y, c);
                        }
                    }
                }
            }
        }

        private static void EnsureFolder(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
    }
}
