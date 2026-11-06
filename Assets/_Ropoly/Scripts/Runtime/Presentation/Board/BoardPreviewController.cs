using System.Collections.Generic;
using Ropoly.Core.Board;
using Ropoly.Infrastructure.Content.Board;
using TMPro;
using UnityEngine;

namespace Ropoly.Presentation.Board
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class BoardPreviewController : MonoBehaviour
    {
        [SerializeField]
        private BoardDefinition _boardDefinition;

        [SerializeField]
        private BoardPreviewTheme _theme;

        [Header("Player tokens")]
        [SerializeField]
        [Min(0f)]
        private float _tokenElevation = 0.66f;

        [SerializeField]
        [Range(0f, 0.6f)]
        private float _tokenSlotOffset = 0.34f;

        private readonly Dictionary<Color32, Material> _materials = new Dictionary<Color32, Material>();
        private readonly Dictionary<Texture, Material> _texturedMaterials = new Dictionary<Texture, Material>();
        private readonly Dictionary<int, BoardPreviewTileView> _tileViews =
            new Dictionary<int, BoardPreviewTileView>();
        private Transform _generatedRoot;
        private TMP_FontAsset _font;
        private TMP_Text _statusText;

        public BoardDefinition BoardDefinition => _boardDefinition;
        public BoardPreviewTheme Theme => _theme;
        public int TileCount => _boardDefinition == null
            ? 0
            : _boardDefinition.CreateSnapshot().Tiles.Count;

        public void SetStatusText(string value)
        {
            if (_statusText != null)
            {
                _statusText.text = value;
            }
        }

        public bool SetTileOwnerColor(int tileIndex, Color color)
        {
            if (!_tileViews.TryGetValue(tileIndex, out BoardPreviewTileView tile))
            {
                return false;
            }

            tile.SetOwnerMaterial(GetMaterial(color));
            return true;
        }

        public bool ClearTileOwner(int tileIndex)
        {
            if (!_tileViews.TryGetValue(tileIndex, out BoardPreviewTileView tile))
            {
                return false;
            }

            tile.ClearOwner();
            return true;
        }

        public bool SetTileUpgradeLevel(int tileIndex, int level)
        {
            if (!_tileViews.TryGetValue(tileIndex, out BoardPreviewTileView tile))
            {
                return false;
            }

            tile.SetUpgradeLevel(level);
            return true;
        }

        public bool TryGetTokenPosition(int tileIndex, int playerIndex, out Vector3 position)
        {
            position = default;
            if (_boardDefinition == null ||
                tileIndex < 0 ||
                tileIndex >= TileCount ||
                playerIndex < 0 ||
                playerIndex >= 4)
            {
                return false;
            }

            GetTileLayout(tileIndex, out Vector3 center, out _, out _, out _);
            float x = playerIndex % 2 == 0 ? -_tokenSlotOffset : _tokenSlotOffset;
            float z = playerIndex < 2 ? _tokenSlotOffset : -_tokenSlotOffset;
            position = center + new Vector3(x, _tokenElevation, z);
            return true;
        }

        public string GetTileDisplayName(int tileIndex)
        {
            if (_boardDefinition == null)
            {
                return string.Empty;
            }

            BoardSnapshot board = _boardDefinition.CreateSnapshot();
            if (tileIndex < 0 || tileIndex >= board.Tiles.Count)
            {
                return string.Empty;
            }

            BoardTileSnapshot tile = board.Tiles[tileIndex];
            switch (tile.Kind)
            {
                case BoardTileKind.City:
                    foreach (CitySnapshot city in board.Cities)
                    {
                        if (city.CityId == tile.ContentId)
                        {
                            return city.DisplayName;
                        }
                    }

                    break;
                case BoardTileKind.Airport:
                    foreach (AirportSnapshot airport in board.Airports)
                    {
                        if (airport.AirportId == tile.ContentId)
                        {
                            return airport.DisplayName;
                        }
                    }

                    break;
                case BoardTileKind.Utility:
                    foreach (UtilitySnapshot utility in board.Utilities)
                    {
                        if (utility.UtilityId == tile.ContentId)
                        {
                            return utility.DisplayName;
                        }
                    }

                    break;
                default:
                    foreach (SpecialSpaceSnapshot special in board.SpecialSpaces)
                    {
                        if (special.SpaceId == tile.ContentId)
                        {
                            return special.DisplayName;
                        }
                    }

                    break;
            }

            return tile.ContentId;
        }

        private void Awake()
        {
            if (_generatedRoot == null)
            {
                RebuildPreview();
            }
        }

        private void OnEnable()
        {
            if (_generatedRoot == null)
            {
                RebuildPreview();
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Keep preview tiles in clean initial state
        }
#endif

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

        [ContextMenu("Show Demo Structures")]
        public void PopulateDemoStructures()
        {
            Color green = new Color(0.20f, 0.85f, 0.40f);  // Pip
            Color blue = new Color(0.20f, 0.60f, 1.00f);   // Bubu
            Color yellow = new Color(1.00f, 0.85f, 0.20f); // Sunny
            Color pink = new Color(1.00f, 0.45f, 0.65f);   // Peach
            Color purple = new Color(0.70f, 0.35f, 0.95f); // Mochi

            // Tile 1: Delhi (Base Ownership Metallic Flagpole & Waving Pennant)
            SetTileOwnerColor(1, green);
            SetTileUpgradeLevel(1, 0);

            // Tile 9: Rio de Janeiro (1 3D House + Waving Flag)
            SetTileOwnerColor(9, green);
            SetTileUpgradeLevel(9, 1);

            // Tile 27: Paris (2 3D Houses + Waving Flag)
            SetTileOwnerColor(27, blue);
            SetTileUpgradeLevel(27, 2);

            // Tile 23: Rome (3 3D Houses + Waving Flag)
            SetTileOwnerColor(23, yellow);
            SetTileUpgradeLevel(23, 3);

            // Tile 24: Milan (the new ownership band sample)
            SetTileOwnerColor(24, green);
            SetTileUpgradeLevel(24, 0);

            // Tile 34: London (4 3D Houses + Waving Flag)
            SetTileOwnerColor(34, pink);
            SetTileUpgradeLevel(34, 4);

            // Tile 39: New York (Grand Luxury Hotel Landmark + Rooftop Flag)
            SetTileOwnerColor(39, purple);
            SetTileUpgradeLevel(39, 5);

            // Tile 14: Heathrow Airport (Owner Flag claim)
            SetTileOwnerColor(14, blue);
        }

        [ContextMenu("Clear Demo Structures")]
        public void ClearDemoStructures()
        {
            for (int i = 0; i < TileCount; i++)
            {
                ClearTileOwner(i);
                SetTileUpgradeLevel(i, 0);
            }
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
            Dictionary<string, CityDefinition> cityDefinitions = IndexCityDefinitions(_boardDefinition);
            Dictionary<string, CountryDefinition> countryDefinitions = IndexCountryDefinitions(_boardDefinition);

            foreach (BoardTileSnapshot tile in board.Tiles)
            {
                bool isMilanSample = tile.Kind == BoardTileKind.City && tile.ContentId == "city-milan";
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

                if (!isMilanSample)
                {
                    CreateAccent(tile.Index, center, size, inward, accentColor, tileGroup.transform);
                }
                GameObject ownerMarker = CreateOwnerMarker(
                    tile.Index,
                    center,
                    size,
                    inward,
                    tileGroup.transform);

                PropertyStructureView structureView = isMilanSample
                    ? null
                    : CreateStructureLot(tile.Index, center, size, inward, tileGroup.transform);

                BoardPreviewTileView tileView = tileObject.AddComponent<BoardPreviewTileView>();
                TMP_Text upgradeText = null;
                if (tile.Kind == BoardTileKind.City && !isMilanSample)
                {
                    Quaternion textRotation = Quaternion.LookRotation(Vector3.down, textUp);
                    upgradeText = CreateWorldText(
                        "Upgrade Text",
                        string.Empty,
                        ownerMarker.transform.position + (Vector3.up * 0.08f),
                        textRotation,
                        0.45f,
                        FontStyles.Bold,
                        Color.white,
                        new Vector2(1f, 0.5f),
                        tileGroup.transform);
                }

                tileView.Initialize(
                    tile.Index,
                    tile.Kind,
                    tile.ContentId,
                    ownerMarker,
                    ownerMarker.GetComponent<MeshRenderer>(),
                    upgradeText,
                    structureView);
                _tileViews.Add(tile.Index, tileView);

                CityDefinition cityDef = null;
                CountryDefinition countryDef = null;
                if (tile.Kind == BoardTileKind.City)
                {
                    cityDefinitions.TryGetValue(tile.ContentId, out cityDef);
                    countryDefinitions.TryGetValue(tile.ContentId, out countryDef);
                }

                if (isMilanSample)
                {
                    CreateMilanTileVisuals(center, size, detail, tileView, tileGroup.transform);
                }
                else
                {
                    CreateTileVisuals(
                        tile.Index,
                        tile.Kind,
                        tile.ContentId,
                        label,
                        detail,
                        center,
                        size,
                        textUp,
                        cityDef,
                        countryDef,
                        tileGroup.transform);
                }
            }
        }

        private void CreateMilanTileVisuals(
            Vector3 center,
            Vector2 tileSize,
            string detail,
            BoardPreviewTileView tileView,
            Transform parent)
        {
            // Milan is the first city-tile design sample. The flag fills the face,
            // while the separate band can change color without changing the art.
            const float bandDepth = 0.34f;
            float faceWidth = tileSize.x - _theme.TileGap - 0.10f;
            float flagDepth = tileSize.y - _theme.TileGap - bandDepth - 0.12f;
            Vector3 flagPosition = center + new Vector3(0f, 0.35f, bandDepth * 0.5f + 0.01f);
            Sprite flagArt = Resources.Load<Sprite>("CityTiles/MilanFlagPanel");
            if (flagArt != null)
            {
                CreateCardQuad("Soft Italian Flag", flagPosition,
                    new Vector2(faceWidth, flagDepth), flagArt.texture, Color.white, parent);
            }

            Quaternion rotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);
            Vector3 titlePosition = center + new Vector3(0f, 0.405f, 0.13f);
            CreateWorldText("Milan Name Shadow", "MILAN",
                titlePosition + new Vector3(0.025f, -0.005f, -0.022f), rotation,
                0.43f, FontStyles.Bold, new Color(0.05f, 0.06f, 0.11f, 0.95f),
                new Vector2(faceWidth - 0.08f, 0.55f), parent);
            CreateWorldText("Milan Name", "MILAN", titlePosition, rotation,
                0.43f, FontStyles.Bold, Color.white,
                new Vector2(faceWidth - 0.08f, 0.55f), parent);

            Vector3 pricePosition = center + new Vector3(0f, 0.405f, 0.88f);
            CreateWorldText("Milan Price Shadow", detail,
                pricePosition + new Vector3(0.015f, -0.005f, -0.012f), rotation,
                0.28f, FontStyles.Bold, new Color(0.05f, 0.06f, 0.11f, 0.95f),
                new Vector2(faceWidth - 0.18f, 0.30f), parent);
            CreateWorldText("Milan Price", detail, pricePosition, rotation,
                0.28f, FontStyles.Bold, Color.white,
                new Vector2(faceWidth - 0.18f, 0.30f), parent);

            Vector3 bandPosition = center + new Vector3(0f, 0.37f,
                -(tileSize.y * 0.5f) + (_theme.TileGap * 0.5f) + (bandDepth * 0.5f) + 0.03f);
            Material neutralBandMaterial = GetMaterial(new Color(0.17f, 0.16f, 0.24f));
            GameObject band = CreateCube("Owner Color Band", bandPosition,
                new Vector3(faceWidth, 0.06f, bandDepth),
                new Color(0.17f, 0.16f, 0.24f), false, parent);
            MeshRenderer bandRenderer = band.GetComponent<MeshRenderer>();
            bandRenderer.sharedMaterial = neutralBandMaterial;

            CreateCube("Band Top Rule", bandPosition + new Vector3(0f, 0.032f, bandDepth * 0.5f),
                new Vector3(faceWidth, 0.005f, 0.018f),
                new Color(0.86f, 0.82f, 0.71f), false, parent);

            TMP_Text upgradeLabel = CreateWorldText("Milan Upgrade", string.Empty,
                bandPosition + new Vector3(0f, 0.045f, 0f), rotation,
                0.18f, FontStyles.Bold, Color.white,
                new Vector2(faceWidth - 0.08f, bandDepth - 0.03f), parent);
            tileView.SetOwnerBand(bandRenderer, neutralBandMaterial, upgradeLabel);
        }

        private GameObject CreateOwnerMarker(
            int tileIndex,
            Vector3 center,
            Vector2 tileSize,
            Vector3 inward,
            Transform parent)
        {
            bool horizontalTile = tileIndex <= 10 || (tileIndex >= 20 && tileIndex <= 30);
            float inwardDimension = horizontalTile ? tileSize.y : tileSize.x;
            Vector3 position = center - (inward * ((inwardDimension * 0.5f) - 0.18f));
            position.y = 0.39f;
            return CreateCube(
                "Owner Marker",
                position,
                new Vector3(0.23f, 0.08f, 0.23f),
                Color.white,
                includeCollider: false,
                parent);
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
            _statusText = CreateWorldText(
                "Preview Status",
                "LOCAL LOBBY  •  CHOOSE YOUR CREATURE",
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
            // Corners do not have country accent color stripes
            if (tileIndex == 0 || tileIndex == 10 || tileIndex == 20 || tileIndex == 30)
            {
                return;
            }

            bool horizontalTile = tileIndex < 10 || (tileIndex > 20 && tileIndex < 30);
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

        private PropertyStructureView CreateStructureLot(
            int tileIndex,
            Vector3 center,
            Vector2 tileSize,
            Vector3 inward,
            Transform parent)
        {
            // Corners do not have houses/flags
            if (tileIndex == 0 || tileIndex == 10 || tileIndex == 20 || tileIndex == 30)
            {
                return null;
            }

            bool horizontalTile = tileIndex < 10 || (tileIndex > 20 && tileIndex < 30);

            // Compute center of property building lot in middle of tile
            Vector3 lotOffset;
            if (tileIndex < 10)
            {
                // Bottom edge (inward is +Z)
                lotOffset = new Vector3(0f, 0.33f, -0.05f);
            }
            else if (tileIndex > 20 && tileIndex < 30)
            {
                // Top edge (inward is -Z)
                lotOffset = new Vector3(0f, 0.33f, 0.05f);
            }
            else if (tileIndex > 10 && tileIndex < 20)
            {
                // Left edge (inward is +X): Lot on inner right side
                lotOffset = new Vector3(0.38f, 0.33f, 0f);
            }
            else
            {
                // Right edge (inward is -X): Lot on inner left side
                lotOffset = new Vector3(-0.38f, 0.33f, 0f);
            }

            GameObject lotObject = new GameObject("PropertyStructures");
            PropertyStructureView structureView = lotObject.AddComponent<PropertyStructureView>();
            structureView.Initialize(horizontalTile, parent, center + lotOffset);
            return structureView;
        }

        private void CreateTileVisuals(
            int tileIndex,
            BoardTileKind kind,
            string contentId,
            string label,
            string detail,
            Vector3 center,
            Vector2 tileSize,
            Vector3 textUp,
            CityDefinition cityDef,
            CountryDefinition countryDef,
            Transform parent)
        {
            Quaternion rotation = Quaternion.LookRotation(Vector3.down, textUp);
            bool isCorner = tileSize.x > 2.0f && tileSize.y > 2.0f;
            bool horizontalTile = tileSize.y > tileSize.x;

            if (kind == BoardTileKind.City && cityDef != null)
            {
                // City Tile with Landmark Photo and Country Flag
                Vector3 photoPos;
                Vector2 photoSize;
                Vector3 flagPos;
                Vector2 flagSize;
                Vector3 namePos;
                Vector2 nameBounds;
                Vector3 detailPos;
                Vector2 detailBounds;

                if (horizontalTile)
                {
                    bool isBottom = center.z < 0;
                    photoSize = new Vector2(1.26f, 1.04f);
                    flagSize = new Vector2(0.48f, 0.30f);

                    if (isBottom)
                    {
                        // Bottom edge: inward is +Z
                        flagPos = center + new Vector3(0f, 0.40f, 1.03f);
                        photoPos = center + new Vector3(0f, 0.34f, 0.22f);
                        namePos = center + new Vector3(0f, 0.39f, -0.50f);
                        detailPos = center + new Vector3(0f, 0.39f, -0.88f);
                    }
                    else
                    {
                        // Top edge: inward is -Z
                        flagPos = center + new Vector3(0f, 0.40f, -1.03f);
                        photoPos = center + new Vector3(0f, 0.34f, -0.22f);
                        namePos = center + new Vector3(0f, 0.39f, 0.50f);
                        detailPos = center + new Vector3(0f, 0.39f, 0.88f);
                    }

                    nameBounds = new Vector2(1.30f, 0.44f);
                    detailBounds = new Vector2(1.30f, 0.28f);
                }
                else
                {
                    // Vertical tiles (Left / Right edge)
                    bool isLeft = center.x < 0;
                    photoSize = new Vector2(1.16f, 1.16f);
                    flagSize = new Vector2(0.48f, 0.30f);

                    if (isLeft)
                    {
                        photoPos = center + new Vector3(-0.52f, 0.34f, 0f);
                        flagPos = center + new Vector3(0.36f, 0.40f, 0.40f);
                        namePos = center + new Vector3(0.36f, 0.39f, 0.02f);
                        detailPos = center + new Vector3(0.36f, 0.39f, -0.40f);
                    }
                    else
                    {
                        photoPos = center + new Vector3(0.52f, 0.34f, 0f);
                        flagPos = center + new Vector3(-0.36f, 0.40f, 0.40f);
                        namePos = center + new Vector3(-0.36f, 0.39f, 0.02f);
                        detailPos = center + new Vector3(-0.36f, 0.39f, -0.40f);
                    }

                    nameBounds = new Vector2(1.15f, label.Contains("\n") ? 0.52f : 0.28f);
                    detailBounds = new Vector2(1.15f, 0.28f);
                }

                // Render Landmark Photo
                if (cityDef.Artwork != null && cityDef.Artwork.texture != null)
                {
                    CreateCube(
                        "Photo Frame",
                        photoPos - new Vector3(0f, 0.02f, 0f),
                        new Vector3(photoSize.x + 0.04f, 0.02f, photoSize.y + 0.04f),
                        new Color(0.06f, 0.04f, 0.10f),
                        includeCollider: false,
                        parent);

                    CreateCardQuad(
                        "Landmark Photo",
                        photoPos,
                        photoSize,
                        cityDef.Artwork.texture,
                        Color.white,
                        parent);
                }

                // Render Country Flag
                if (countryDef != null && countryDef.Flag != null && countryDef.Flag.texture != null)
                {
                    CreateCube(
                        "Flag Frame",
                        flagPos - new Vector3(0f, 0.02f, 0f),
                        new Vector3(flagSize.x + 0.04f, 0.02f, flagSize.y + 0.04f),
                        new Color(0.06f, 0.04f, 0.10f),
                        includeCollider: false,
                        parent);

                    CreateCardQuad(
                        "Country Flag",
                        flagPos,
                        flagSize,
                        countryDef.Flag.texture,
                        Color.white,
                        parent);
                }

                // Typography
                TextMeshPro nameText = CreateWorldText(
                    "Name",
                    label,
                    namePos,
                    rotation,
                    0.32f,
                    FontStyles.Bold,
                    _theme.PrimaryTextColor,
                    nameBounds,
                    parent);
                nameText.lineSpacing = -12f;
                nameText.textWrappingMode = label.Contains("\n") ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
                nameText.overflowMode = TextOverflowModes.Truncate;
                nameText.enableAutoSizing = true;
                nameText.fontSizeMin = 0.16f;
                nameText.fontSizeMax = 0.32f;

                TextMeshPro detailText = CreateWorldText(
                    "Detail",
                    detail,
                    detailPos,
                    rotation,
                    0.28f,
                    FontStyles.Bold,
                    _theme.PriceTextColor,
                    detailBounds,
                    parent);
                detailText.textWrappingMode = TextWrappingModes.NoWrap;
                detailText.enableAutoSizing = true;
                detailText.fontSizeMin = 0.16f;
                detailText.fontSizeMax = 0.28f;
                return;
            }

            // Non-City Tiles (Airports, Utilities, Special Spaces, Corners)
            Sprite tileIcon = ResolveTileIcon(kind, contentId);

            Vector3 standardNamePos;
            Vector3 standardDetailPos;
            Vector2 standardNameBounds;
            Vector2 standardDetailBounds;

            if (isCorner)
            {
                if (tileIcon != null && tileIcon.texture != null)
                {
                    Vector3 iconPos = center + new Vector3(0f, 0.35f, 0f);
                    Vector2 iconSize = new Vector2(1.05f, 1.05f);

                    CreateCube(
                        "Icon Frame",
                        iconPos - new Vector3(0f, 0.02f, 0f),
                        new Vector3(iconSize.x + 0.04f, 0.02f, iconSize.y + 0.04f),
                        new Color(0.06f, 0.04f, 0.10f),
                        includeCollider: false,
                        parent);

                    CreateCardQuad(
                        "Space Icon",
                        iconPos,
                        iconSize,
                        tileIcon.texture,
                        Color.white,
                        parent);
                }

                standardNamePos = center + new Vector3(0f, 0.39f, 0.74f);
                standardDetailPos = center + new Vector3(0f, 0.39f, -0.74f);
                standardNameBounds = new Vector2(tileSize.x - 0.20f, 0.48f);
                standardDetailBounds = new Vector2(tileSize.x - 0.20f, 0.36f);
            }
            else if (horizontalTile)
            {
                bool isBottom = center.z < 0;

                if (tileIcon != null && tileIcon.texture != null)
                {
                    Vector3 iconPos = isBottom
                        ? center + new Vector3(0f, 0.35f, 0.18f)
                        : center + new Vector3(0f, 0.35f, -0.18f);
                    Vector2 iconSize = new Vector2(0.82f, 0.82f);

                    CreateCube(
                        "Icon Frame",
                        iconPos - new Vector3(0f, 0.02f, 0f),
                        new Vector3(iconSize.x + 0.04f, 0.02f, iconSize.y + 0.04f),
                        new Color(0.06f, 0.04f, 0.10f),
                        includeCollider: false,
                        parent);

                    CreateCardQuad(
                        "Space Icon",
                        iconPos,
                        iconSize,
                        tileIcon.texture,
                        Color.white,
                        parent);
                }

                standardNamePos = isBottom
                    ? center + new Vector3(0f, 0.39f, -0.48f)
                    : center + new Vector3(0f, 0.39f, 0.48f);

                standardDetailPos = isBottom
                    ? center + new Vector3(0f, 0.39f, -0.86f)
                    : center + new Vector3(0f, 0.39f, 0.86f);

                standardNameBounds = new Vector2(Mathf.Max(0.9f, tileSize.x - 0.10f), 0.50f);
                standardDetailBounds = new Vector2(Mathf.Max(0.9f, tileSize.x - 0.10f), 0.30f);
            }
            else
            {
                bool isLeft = center.x < 0;

                if (tileIcon != null && tileIcon.texture != null)
                {
                    Vector3 iconPos = isLeft
                        ? center + new Vector3(-0.52f, 0.35f, 0f)
                        : center + new Vector3(0.52f, 0.35f, 0f);
                    Vector2 iconSize = new Vector2(1.02f, 1.02f);

                    CreateCube(
                        "Icon Frame",
                        iconPos - new Vector3(0f, 0.02f, 0f),
                        new Vector3(iconSize.x + 0.04f, 0.02f, iconSize.y + 0.04f),
                        new Color(0.06f, 0.04f, 0.10f),
                        includeCollider: false,
                        parent);

                    CreateCardQuad(
                        "Space Icon",
                        iconPos,
                        iconSize,
                        tileIcon.texture,
                        Color.white,
                        parent);
                }

                standardNamePos = isLeft
                    ? new Vector3(center.x + 0.36f, 0.39f, center.z + 0.20f)
                    : new Vector3(center.x - 0.36f, 0.39f, center.z + 0.20f);

                standardDetailPos = isLeft
                    ? new Vector3(center.x + 0.36f, 0.39f, center.z - 0.28f)
                    : new Vector3(center.x - 0.36f, 0.39f, center.z - 0.28f);

                standardNameBounds = new Vector2(1.15f, label.Contains("\n") ? 0.50f : 0.28f);
                standardDetailBounds = new Vector2(1.15f, 0.30f);
            }

            TextMeshPro stdNameText = CreateWorldText(
                "Name",
                label,
                standardNamePos,
                rotation,
                0.32f,
                FontStyles.Bold,
                _theme.PrimaryTextColor,
                standardNameBounds,
                parent);
            stdNameText.lineSpacing = -12f;
            stdNameText.textWrappingMode = label.Contains("\n") ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            stdNameText.overflowMode = TextOverflowModes.Truncate;
            stdNameText.enableAutoSizing = true;
            stdNameText.fontSizeMin = 0.16f;
            stdNameText.fontSizeMax = 0.32f;

            TextMeshPro stdDetailText = CreateWorldText(
                "Detail",
                detail,
                standardDetailPos,
                rotation,
                0.28f,
                FontStyles.Bold,
                _theme.PriceTextColor,
                standardDetailBounds,
                parent);
            stdDetailText.textWrappingMode = TextWrappingModes.NoWrap;
            stdDetailText.enableAutoSizing = true;
            stdDetailText.fontSizeMin = 0.16f;
            stdDetailText.fontSizeMax = 0.28f;
        }

        private static Sprite LoadTileIcon(string iconName)
        {
            Sprite sprite = Resources.Load<Sprite>($"Icons/{iconName}");
            if (sprite != null)
            {
                return sprite;
            }

#if UNITY_EDITOR
            sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/_Ropoly/Art/Icons/{iconName}.png");
#endif
            return sprite;
        }

        private static Sprite ResolveTileIcon(BoardTileKind kind, string contentId)
        {
            if (kind == BoardTileKind.Airport)
            {
                return LoadTileIcon("Airport");
            }

            if (kind == BoardTileKind.Utility)
            {
                return contentId.Contains("water") ? LoadTileIcon("Water") : LoadTileIcon("Electric");
            }

            if (kind == BoardTileKind.Special)
            {
                switch (contentId)
                {
                    case "space-start":
                        return LoadTileIcon("Start");
                    case "space-detention":
                        return LoadTileIcon("Detention");
                    case "space-vacation":
                        return LoadTileIcon("Vacation");
                    case "space-go-to-detention":
                        return LoadTileIcon("GoToDetention");
                    case "space-world-event":
                        return LoadTileIcon("WorldEvent");
                    case "space-local-opportunity":
                        return LoadTileIcon("Opportunity");
                    case "space-income-tax":
                    case "space-income-fee":
                    case "space-premium-fee":
                        return LoadTileIcon("Tax");
                }
            }

            return null;
        }

        private GameObject CreateCardQuad(
            string name,
            Vector3 position,
            Vector2 size,
            Texture texture,
            Color tint,
            Transform parent)
        {
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = name;
            quad.transform.SetParent(parent, false);
            quad.transform.position = position;
            quad.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            quad.transform.localScale = new Vector3(size.x, size.y, 1f);

            MeshCollider collider = quad.GetComponent<MeshCollider>();
            if (collider != null)
            {
                if (UnityEngine.Application.isPlaying)
                {
                    Destroy(collider);
                }
                else
                {
                    DestroyImmediate(collider);
                }
            }

            quad.GetComponent<MeshRenderer>().sharedMaterial = GetTexturedMaterial(texture, tint);
            return quad;
        }

        private Material GetTexturedMaterial(Texture texture, Color tint)
        {
            if (texture == null)
            {
                return GetMaterial(tint);
            }

            if (_texturedMaterials.TryGetValue(texture, out Material existing) && existing != null)
            {
                return existing;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Texture");
            }

            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }

            Material material = new Material(shader)
            {
                name = $"Mat_{texture.name}",
                enableInstancing = true
            };

            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", texture);
            }
            else if (material.HasProperty("_MainTex"))
            {
                material.SetTexture("_MainTex", texture);
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", tint);
            }
            else if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", tint);
            }

            _texturedMaterials[texture] = material;
            return material;
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
            text.fontSizeMin = Mathf.Max(0.18f, fontSize * 0.70f);
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

        private static int GetLongestLineLength(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return 0;
            }

            int longest = 0;
            int current = 0;
            foreach (char character in value)
            {
                if (character == '\n')
                {
                    longest = Mathf.Max(longest, current);
                    current = 0;
                    continue;
                }

                current++;
            }

            return Mathf.Max(longest, current);
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

        private static Dictionary<string, CityDefinition> IndexCityDefinitions(BoardDefinition boardDefinition)
        {
            Dictionary<string, CityDefinition> values = new Dictionary<string, CityDefinition>();
            if (boardDefinition == null || boardDefinition.Countries == null)
            {
                return values;
            }

            foreach (CountryDefinition country in boardDefinition.Countries)
            {
                if (country == null || country.Cities == null)
                {
                    continue;
                }

                foreach (CityDefinition city in country.Cities)
                {
                    if (city != null && !string.IsNullOrEmpty(city.ContentId))
                    {
                        values[city.ContentId] = city;
                    }
                }
            }

            return values;
        }

        private static Dictionary<string, CountryDefinition> IndexCountryDefinitions(BoardDefinition boardDefinition)
        {
            Dictionary<string, CountryDefinition> values = new Dictionary<string, CountryDefinition>();
            if (boardDefinition == null || boardDefinition.Countries == null)
            {
                return values;
            }

            foreach (CountryDefinition country in boardDefinition.Countries)
            {
                if (country == null || country.Cities == null)
                {
                    continue;
                }

                foreach (CityDefinition city in country.Cities)
                {
                    if (city != null && !string.IsNullOrEmpty(city.ContentId))
                    {
                        values[city.ContentId] = country;
                    }
                }
            }

            return values;
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
                SpecialSpaceKind.Event => "DRAW CARD",
                SpecialSpaceKind.Fee => $"PAY ${special.FeeAmount}",
                SpecialSpaceKind.Detention => "VISITING",
                SpecialSpaceKind.Vacation => "REST",
                SpecialSpaceKind.GoToDetention => "MOVE NOW",
                _ => string.Empty,
            };
        }

        private static string FormatLabel(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            string upper = value.ToUpperInvariant();

            // Handcrafted multi-line wraps for common board spaces to prevent ugly truncation
            switch (upper)
            {
                case "SAINT PETERSBURG":
                    return "SAINT\nPETERSBURG";
                case "SAN FRANCISCO":
                    return "SAN\nFRANCISCO";
                case "LOS ANGELES":
                    return "LOS\nANGELES";
                case "RIO DE JANEIRO":
                    return "RIO DE\nJANEIRO";
                case "ELECTRIC COMPANY":
                    return "ELECTRIC\nCOMPANY";
                case "WATER SERVICE":
                    return "WATER\nSERVICE";
                case "LOCAL OPPORTUNITY":
                    return "LOCAL\nOPPORTUNITY";
                case "WORLD EVENT":
                    return "WORLD\nEVENT";
                case "INCOME TAX":
                    return "INCOME\nTAX";
                case "PREMIUM FEE":
                    return "PREMIUM\nFEE";
                case "DETENTION / VISITING":
                    return "DETENTION /\nVISITING";
                case "GO TO DETENTION":
                    return "GO TO\nDETENTION";
                case "BEIJING CAPITAL AIRPORT":
                    return "BEIJING\nAIRPORT";
                case "CHARLES DE GAULLE AIRPORT":
                    return "PARIS\nAIRPORT";
                case "HEATHROW AIRPORT":
                    return "LONDON\nAIRPORT";
                case "JFK AIRPORT":
                    return "JFK\nAIRPORT";
            }

            if (upper.Length <= 11)
            {
                return upper;
            }

            int midpoint = upper.Length / 2;
            int split = upper.LastIndexOf(' ', midpoint);
            if (split <= 0)
            {
                split = upper.IndexOf(' ', midpoint);
            }

            return split <= 0
                ? upper
                : upper.Substring(0, split) + "\n" + upper.Substring(split + 1);
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
                _statusText = null;
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

            foreach (Material material in _texturedMaterials.Values)
            {
                if (material != null)
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
            }

            _materials.Clear();
            _texturedMaterials.Clear();
            _tileViews.Clear();
        }
    }
}
