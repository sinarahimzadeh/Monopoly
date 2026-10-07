using System.Collections.Generic;
using Ropoly.Core.Board;
using Ropoly.Infrastructure.Content.Board;
using TMPro;
using UnityEngine;

namespace Ropoly.Presentation.Board
{
    [DisallowMultipleComponent]
    public sealed class BoardPreviewController : MonoBehaviour
    {
        [SerializeField]
        private BoardDefinition _boardDefinition;

        [SerializeField]
        private BoardPreviewTheme _theme;

        private readonly Dictionary<Color32, Material> _materials = new Dictionary<Color32, Material>();
        private Transform _generatedRoot;
        private TMP_FontAsset _font;

        public BoardDefinition BoardDefinition => _boardDefinition;
        public BoardPreviewTheme Theme => _theme;

        private void Awake()
        {
            RebuildPreview();
        }

        private void OnDestroy()
        {
            DestroyGeneratedObjects();
        }

        [ContextMenu("Rebuild Board Preview")]
        public void RebuildPreview()
        {
            DestroyGeneratedObjects();
            if (_boardDefinition == null || _theme == null)
            {
                Debug.LogError("Board preview requires both a board definition and a theme.", this);
                return;
            }

            BoardValidationResult validation = _boardDefinition.Validate();
            if (!validation.IsValid)
            {
                Debug.LogError("Board preview cannot render an invalid board definition.", _boardDefinition);
                return;
            }

            _font = _theme.Font;
            if (_font == null)
            {
                Debug.LogError("Board preview theme requires a TextMesh Pro font asset.", _theme);
                return;
            }
            GameObject generatedRoot = new GameObject("Generated Board Preview");
            generatedRoot.transform.SetParent(transform, false);
            _generatedRoot = generatedRoot.transform;

            BoardSnapshot board = _boardDefinition.CreateSnapshot();
            CreateBoardSurface(board);
            CreateTiles(board);
            CreateCenterBranding(board);
        }

        private void CreateBoardSurface(BoardSnapshot board)
        {
            float boardSize = _theme.BoardSize;
            CreateCube(
                "Board Base",
                Vector3.zero,
                new Vector3(boardSize + 0.35f, 0.18f, boardSize + 0.35f),
                _theme.BoardColor,
                includeCollider: false,
                _generatedRoot);

            float centerSize = boardSize - (_theme.CornerSize * 2f) - 0.18f;
            CreateCube(
                "Center Field",
                new Vector3(0f, 0.13f, 0f),
                new Vector3(centerSize, 0.12f, centerSize),
                _theme.CenterColor,
                includeCollider: false,
                _generatedRoot);

            float dashWidth = centerSize / Mathf.Max(1, board.Countries.Count) - 0.12f;
            float dashStart = -centerSize * 0.5f + dashWidth * 0.5f + 0.06f;
            for (int index = 0; index < board.Countries.Count; index++)
            {
                Color color = ParseColor(board.Countries[index].SetColorHex, _theme.CornerColor);
                CreateCube(
                    $"Country Tier {index + 1}",
                    new Vector3(dashStart + (index * (dashWidth + 0.12f)), 0.23f, 3.35f),
                    new Vector3(dashWidth, 0.05f, 0.15f),
                    color,
                    includeCollider: false,
                    _generatedRoot);
            }
        }

        private void CreateTiles(BoardSnapshot board)
        {
            Dictionary<string, CitySnapshot> cities = IndexCities(board);
            Dictionary<string, AirportSnapshot> airports = IndexAirports(board);
            Dictionary<string, UtilitySnapshot> utilities = IndexUtilities(board);
            Dictionary<string, SpecialSpaceSnapshot> specialSpaces = IndexSpecialSpaces(board);
            Dictionary<string, Color> countryColors = IndexCountryColors(board);

            foreach (BoardTileSnapshot tile in board.Tiles)
            {
                GetTileLayout(tile.Index, out Vector3 center, out Vector2 size, out Vector3 inward, out Vector3 textUp);
                // This is a screen-first board, so labels remain upright instead of
                // forcing the player to read the far and side edges upside down.
                textUp = Vector3.forward;
                Color tileColor = tile.Index % 2 == 0 ? _theme.TileColor : _theme.TileAlternateColor;
                GameObject tileGroup = new GameObject($"Tile {tile.Index:00} - {tile.ContentId}");
                tileGroup.transform.SetParent(_generatedRoot, false);
                GameObject tileObject = CreateCube(
                    "Surface",
                    new Vector3(center.x, 0.24f, center.z),
                    new Vector3(
                        Mathf.Max(0.1f, size.x - _theme.TileGap),
                        _theme.TileDepth,
                        Mathf.Max(0.1f, size.y - _theme.TileGap)),
                    tileColor,
                    includeCollider: true,
                    tileGroup.transform);

                BoardPreviewTileView tileView = tileObject.AddComponent<BoardPreviewTileView>();
                tileView.Initialize(tile.Index, tile.Kind, tile.ContentId);

                ResolveTilePresentation(
                    tile,
                    cities,
                    airports,
                    utilities,
                    specialSpaces,
                    countryColors,
                    out string label,
                    out string detail,
                    out Color accentColor);

                CreateAccent(tile.Index, center, size, inward, accentColor, tileGroup.transform);
                CreateTileText(label, detail, center, size, textUp, tileGroup.transform);
            }
        }

        private void CreateCenterBranding(BoardSnapshot board)
        {
            Quaternion rotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);
            CreateWorldText(
                "Ropoly Logo",
                "ROPOLY",
                new Vector3(0f, 0.34f, 0.95f),
                rotation,
                2.2f,
                FontStyles.Bold,
                _theme.PrimaryTextColor,
                new Vector2(8f, 2.3f),
                _generatedRoot);
            CreateWorldText(
                "Board Name",
                board.DisplayName.ToUpperInvariant(),
                new Vector3(0f, 0.34f, 0.05f),
                rotation,
                1.05f,
                FontStyles.Bold,
                _theme.PriceTextColor,
                new Vector2(8f, 0.9f),
                _generatedRoot);
            CreateWorldText(
                "Board Summary",
                $"{board.Cities.Count} CITIES   •   {board.Countries.Count} COUNTRY SETS\n" +
                $"{board.Airports.Count} AIRPORTS   •   {board.Utilities.Count} UTILITIES",
                new Vector3(0f, 0.34f, -1.0f),
                rotation,
                0.72f,
                FontStyles.Normal,
                _theme.SecondaryTextColor,
                new Vector2(9f, 1.0f),
                _generatedRoot);
            CreateWorldText(
                "Preview Status",
                "BOARD PREVIEW  •  GAMEPLAY SYSTEMS COMING NEXT",
                new Vector3(0f, 0.34f, -2.35f),
                rotation,
                0.50f,
                FontStyles.Bold,
                _theme.EventColor,
                new Vector2(10f, 0.7f),
                _generatedRoot);
        }

        private void ResolveTilePresentation(
            BoardTileSnapshot tile,
            IReadOnlyDictionary<string, CitySnapshot> cities,
            IReadOnlyDictionary<string, AirportSnapshot> airports,
            IReadOnlyDictionary<string, UtilitySnapshot> utilities,
            IReadOnlyDictionary<string, SpecialSpaceSnapshot> specialSpaces,
            IReadOnlyDictionary<string, Color> countryColors,
            out string label,
            out string detail,
            out Color accentColor)
        {
            switch (tile.Kind)
            {
                case BoardTileKind.City:
                    CitySnapshot city = cities[tile.ContentId];
                    label = FormatLabel(city.DisplayName);
                    detail = $"${city.PurchasePrice}";
                    accentColor = countryColors[tile.ContentId];
                    return;
                case BoardTileKind.Airport:
                    AirportSnapshot airport = airports[tile.ContentId];
                    label = FormatLabel(airport.DisplayName.Replace(" International", string.Empty));
                    detail = $"${airport.PurchasePrice}";
                    accentColor = _theme.AirportColor;
                    return;
                case BoardTileKind.Utility:
                    UtilitySnapshot utility = utilities[tile.ContentId];
                    label = FormatLabel(utility.DisplayName);
                    detail = $"${utility.PurchasePrice}";
                    accentColor = _theme.UtilityColor;
                    return;
                default:
                    SpecialSpaceSnapshot special = specialSpaces[tile.ContentId];
                    label = FormatLabel(special.DisplayName);
                    detail = GetSpecialDetail(special);
                    accentColor = GetSpecialColor(special.SpecialKind);
                    return;
            }
        }

        private void CreateAccent(
            int tileIndex,
            Vector3 center,
            Vector2 tileSize,
            Vector3 inward,
            Color color,
            Transform parent)
        {
            bool horizontalTile = tileIndex <= 10 || (tileIndex >= 20 && tileIndex <= 30);
            float inwardDimension = horizontalTile ? tileSize.y : tileSize.x;
            Vector3 position = center + (inward * ((inwardDimension * 0.5f) - (_theme.AccentDepth * 0.5f)));
            position.y = 0.36f;
            Vector3 scale = horizontalTile
                ? new Vector3(tileSize.x - _theme.TileGap, 0.06f, _theme.AccentDepth)
                : new Vector3(_theme.AccentDepth, 0.06f, tileSize.y - _theme.TileGap);
            CreateCube(
                "Accent",
                position,
                scale,
                color,
                includeCollider: false,
                parent);
        }

        private void CreateTileText(
            string label,
            string detail,
            Vector3 center,
            Vector2 tileSize,
            Vector3 textUp,
            Transform parent)
        {
            Quaternion rotation = Quaternion.LookRotation(Vector3.down, textUp);
            float labelSize = label.Length > 20 ? 0.32f : label.Length > 13 ? 0.38f : 0.46f;
            Vector2 labelBounds = new Vector2(Mathf.Max(0.8f, tileSize.x - 0.14f), 0.68f);
            CreateWorldText(
                "Name",
                label,
                center + (textUp * 0.13f) + new Vector3(0f, 0.39f, 0f),
                rotation,
                labelSize,
                FontStyles.Bold,
                _theme.PrimaryTextColor,
                labelBounds,
                parent);
            CreateWorldText(
                "Detail",
                detail,
                center - (textUp * 0.56f) + new Vector3(0f, 0.39f, 0f),
                rotation,
                0.24f,
                FontStyles.Bold,
                _theme.PriceTextColor,
                new Vector2(Mathf.Max(0.8f, tileSize.x - 0.14f), 0.30f),
                parent);
        }

        private TextMeshPro CreateWorldText(
            string name,
            string value,
            Vector3 position,
            Quaternion rotation,
            float fontSize,
            FontStyles style,
            Color color,
            Vector2 bounds,
            Transform parent)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshPro));
            textObject.transform.SetParent(parent, false);
            textObject.transform.SetPositionAndRotation(position, rotation);
            TextMeshPro text = textObject.GetComponent<TextMeshPro>();
            text.text = value;
            text.font = _font;
            text.fontSize = fontSize;
            text.enableAutoSizing = true;
            text.fontSizeMin = Mathf.Max(0.10f, fontSize * 0.35f);
            text.fontSizeMax = fontSize;
            text.fontStyle = style;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.isOrthographic = true;
            text.richText = false;
            text.rectTransform.sizeDelta = bounds;
            return text;
        }

        private GameObject CreateCube(
            string name,
            Vector3 position,
            Vector3 scale,
            Color color,
            bool includeCollider,
            Transform parent)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.position = position;
            cube.transform.localScale = scale;
            cube.GetComponent<MeshRenderer>().sharedMaterial = GetMaterial(color);
            if (!includeCollider)
            {
                BoxCollider collider = cube.GetComponent<BoxCollider>();
                if (UnityEngine.Application.isPlaying)
                {
                    Destroy(collider);
                }
                else
                {
                    DestroyImmediate(collider);
                }
            }

            return cube;
        }

        private Material GetMaterial(Color color)
        {
            Color32 key = color;
            if (_materials.TryGetValue(key, out Material existing))
            {
                return existing;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }

            Material material = new Material(shader)
            {
                name = $"Board Preview {ColorUtility.ToHtmlStringRGBA(color)}",
                enableInstancing = true,
            };
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            _materials.Add(key, material);
            return material;
        }

        private void GetTileLayout(
            int index,
            out Vector3 center,
            out Vector2 size,
            out Vector3 inward,
            out Vector3 textUp)
        {
            float half = _theme.BoardSize * 0.5f;
            float corner = _theme.CornerSize;
            float sideCenter = half - (corner * 0.5f);
            float edgeLength = (_theme.BoardSize - (corner * 2f)) / 9f;

            if (index == 0)
            {
                center = new Vector3(sideCenter, 0f, -sideCenter);
                size = new Vector2(corner, corner);
                inward = new Vector3(-1f, 0f, 1f).normalized;
                textUp = Vector3.forward;
                return;
            }

            if (index < 10)
            {
                center = new Vector3(
                    half - corner - ((index - 0.5f) * edgeLength),
                    0f,
                    -sideCenter);
                size = new Vector2(edgeLength, corner);
                inward = Vector3.forward;
                textUp = Vector3.forward;
                return;
            }

            if (index == 10)
            {
                center = new Vector3(-sideCenter, 0f, -sideCenter);
                size = new Vector2(corner, corner);
                inward = new Vector3(1f, 0f, 1f).normalized;
                textUp = Vector3.right;
                return;
            }

            if (index < 20)
            {
                center = new Vector3(
                    -sideCenter,
                    0f,
                    -half + corner + ((index - 10.5f) * edgeLength));
                size = new Vector2(corner, edgeLength);
                inward = Vector3.right;
                textUp = Vector3.right;
                return;
            }

            if (index == 20)
            {
                center = new Vector3(-sideCenter, 0f, sideCenter);
                size = new Vector2(corner, corner);
                inward = new Vector3(1f, 0f, -1f).normalized;
                textUp = Vector3.back;
                return;
            }

            if (index < 30)
            {
                center = new Vector3(
                    -half + corner + ((index - 20.5f) * edgeLength),
                    0f,
                    sideCenter);
                size = new Vector2(edgeLength, corner);
                inward = Vector3.back;
                textUp = Vector3.back;
                return;
            }

            if (index == 30)
            {
                center = new Vector3(sideCenter, 0f, sideCenter);
                size = new Vector2(corner, corner);
                inward = new Vector3(-1f, 0f, -1f).normalized;
                textUp = Vector3.left;
                return;
            }

            center = new Vector3(
                sideCenter,
                0f,
                half - corner - ((index - 30.5f) * edgeLength));
            size = new Vector2(corner, edgeLength);
            inward = Vector3.left;
            textUp = Vector3.left;
        }

        private static Dictionary<string, CitySnapshot> IndexCities(BoardSnapshot board)
        {
            Dictionary<string, CitySnapshot> values = new Dictionary<string, CitySnapshot>();
            foreach (CitySnapshot value in board.Cities)
            {
                values.Add(value.CityId, value);
            }

            return values;
        }

        private static Dictionary<string, AirportSnapshot> IndexAirports(BoardSnapshot board)
        {
            Dictionary<string, AirportSnapshot> values = new Dictionary<string, AirportSnapshot>();
            foreach (AirportSnapshot value in board.Airports)
            {
                values.Add(value.AirportId, value);
            }

            return values;
        }

        private static Dictionary<string, UtilitySnapshot> IndexUtilities(BoardSnapshot board)
        {
            Dictionary<string, UtilitySnapshot> values = new Dictionary<string, UtilitySnapshot>();
            foreach (UtilitySnapshot value in board.Utilities)
            {
                values.Add(value.UtilityId, value);
            }

            return values;
        }

        private static Dictionary<string, SpecialSpaceSnapshot> IndexSpecialSpaces(BoardSnapshot board)
        {
            Dictionary<string, SpecialSpaceSnapshot> values = new Dictionary<string, SpecialSpaceSnapshot>();
            foreach (SpecialSpaceSnapshot value in board.SpecialSpaces)
            {
                values.Add(value.SpaceId, value);
            }

            return values;
        }

        private static Dictionary<string, Color> IndexCountryColors(BoardSnapshot board)
        {
            Dictionary<string, Color> values = new Dictionary<string, Color>();
            foreach (CountrySnapshot country in board.Countries)
            {
                Color color = ParseColor(country.SetColorHex, Color.white);
                foreach (string cityId in country.CityIds)
                {
                    values.Add(cityId, color);
                }
            }

            return values;
        }

        private Color GetSpecialColor(SpecialSpaceKind kind)
        {
            return kind switch
            {
                SpecialSpaceKind.Event => _theme.EventColor,
                SpecialSpaceKind.Fee => _theme.FeeColor,
                _ => _theme.CornerColor,
            };
        }

        private static string GetSpecialDetail(SpecialSpaceSnapshot special)
        {
            return special.SpecialKind switch
            {
                SpecialSpaceKind.Start => "BEGIN",
                SpecialSpaceKind.Event => "DRAW A CARD",
                SpecialSpaceKind.Fee => $"PAY ${special.FeeAmount}",
                SpecialSpaceKind.Detention => "VISITING",
                SpecialSpaceKind.Vacation => "TAKE A BREAK",
                SpecialSpaceKind.GoToDetention => "MOVE NOW",
                _ => string.Empty,
            };
        }

        private static string FormatLabel(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= 12)
            {
                return value?.ToUpperInvariant();
            }

            int midpoint = value.Length / 2;
            int split = value.LastIndexOf(' ', midpoint);
            if (split <= 0)
            {
                split = value.IndexOf(' ', midpoint);
            }

            return split <= 0
                ? value.ToUpperInvariant()
                : (value.Substring(0, split) + "\n" + value.Substring(split + 1)).ToUpperInvariant();
        }

        private static Color ParseColor(string html, Color fallback)
        {
            return ColorUtility.TryParseHtmlString(html, out Color color) ? color : fallback;
        }

        private void DestroyGeneratedObjects()
        {
            if (_generatedRoot != null)
            {
                if (UnityEngine.Application.isPlaying)
                {
                    Destroy(_generatedRoot.gameObject);
                }
                else
                {
                    DestroyImmediate(_generatedRoot.gameObject);
                }

                _generatedRoot = null;
            }

            foreach (Material material in _materials.Values)
            {
                if (UnityEngine.Application.isPlaying)
                {
                    Destroy(material);
                }
                else
                {
                    DestroyImmediate(material);
                }
            }

            _materials.Clear();
        }
    }
}
