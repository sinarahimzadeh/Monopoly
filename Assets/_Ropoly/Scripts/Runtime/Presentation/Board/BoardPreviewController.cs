using System.Collections.Generic;
using Ropoly.Core.Board;
using Ropoly.Infrastructure.Content.Board;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

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

        [Header("City Tiles Styling")]
        [Tooltip("Controls the opacity / transparency of the national flag texture over the dark tile base.")]
        [SerializeField]
        [FormerlySerializedAs("_italianFlagOpacity")]
        [Range(0.05f, 1f)]
        private float _cityFlagOpacity = 0.45f;

        [Tooltip("Controls the blur level of the national flag (0 = sharp fabric, 0.5 = soft blur, 1 = heavy blur).")]
        [SerializeField]
        [FormerlySerializedAs("_italianFlagBlur")]
        [Range(0f, 1f)]
        private float _cityFlagBlur = 0.65f;

        [Tooltip("Optional color tint applied to the flag texture.")]
        [SerializeField]
        [FormerlySerializedAs("_italianFlagTint")]
        private Color _cityFlagTint = Color.white;

        [Tooltip("Display the city price in the ownership band underneath the flag.")]
        [SerializeField]
        [FormerlySerializedAs("_italianShowPriceInBand")]
        private bool _cityShowPriceInBand = true;

        [Tooltip("Display the city price at the top of the flag face.")]
        [SerializeField]
        [FormerlySerializedAs("_italianShowPriceOnFlag")]
        private bool _cityShowPriceOnFlag = false;

        [Tooltip("Neutral background color of the ownership band when the city is unowned.")]
        [SerializeField]
        [FormerlySerializedAs("_italianBandNeutralColor")]
        private Color _cityBandNeutralColor = new Color(0.18f, 0.17f, 0.25f);

        [Tooltip("Text color of the price in the ownership band.")]
        [SerializeField]
        [FormerlySerializedAs("_italianBandTextColor")]
        private Color _cityBandTextColor = Color.white;

        [Header("House Dynamic Shake Physics")]
        [Tooltip("Whether houses physically react and shake when players move.")]
        [SerializeField]
        private bool _tileBounceEnabled = true;

        [Tooltip("Overall cushion intensity for smooth natural movement.")]
        [SerializeField]
        [Range(0.02f, 0.30f)]
        private float _tileShakeIntensity = 0.08f;

        [Tooltip("Natural oscillation speed (lower = smoother and softer, higher = snappier).")]
        [SerializeField]
        [Range(8f, 30f)]
        private float _tileShakeFrequency = 16f;

        [Tooltip("Decay speed of the cushion (higher settles faster).")]
        [SerializeField]
        [Range(4f, 15f)]
        private float _tileShakeDecay = 7.5f;

        [Tooltip("Vertical dip compression depth (meters).")]
        [SerializeField]
        [Range(0.01f, 0.15f)]
        private float _tileVerticalDip = 0.05f;

        public float CityFlagOpacity => _cityFlagOpacity;
        public float CityFlagBlur => _cityFlagBlur;
        public Color CityFlagTint => _cityFlagTint;
        public bool CityShowPriceInBand => _cityShowPriceInBand;
        public bool CityShowPriceOnFlag => _cityShowPriceOnFlag;
        public Color CityBandNeutralColor => _cityBandNeutralColor;
        public Color CityBandTextColor => _cityBandTextColor;
        public bool TileBounceEnabled => _tileBounceEnabled;
        public float TileShakeIntensity => _tileShakeIntensity;
        public float TileShakeFrequency => _tileShakeFrequency;
        public float TileShakeDecay => _tileShakeDecay;
        public float TileVerticalDip => _tileVerticalDip;

        private readonly Dictionary<Color32, Material> _materials = new Dictionary<Color32, Material>();
        private readonly Dictionary<Texture, Material> _texturedMaterials = new Dictionary<Texture, Material>();
        private readonly List<Material> _customFlagMaterials = new List<Material>();
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

        public bool BounceTile(int tileIndex, float strength = 1f)
        {
            if (!_tileBounceEnabled)
            {
                return false;
            }

            if (!_tileViews.TryGetValue(tileIndex, out BoardPreviewTileView tile) || tile == null)
            {
                return false;
            }

            tile.Bounce(strength);
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
            RebuildPreview();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
            {
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    if (this != null)
                    {
                        RebuildPreview();
                    }
                };
            }
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

#if UNITY_EDITOR
        [ContextMenu("Test Shake Tile 01")]
        public void TestShakeFirstTile()
        {
            BounceTile(1, 1.6f);
        }

        [ContextMenu("Test Shake All Tiles Wave")]
        public void TestShakeWave()
        {
            for (int i = 0; i < TileCount; i++)
            {
                BounceTile(i, 1.2f);
            }
        }
#endif

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
                CityDefinition cityDef = null;
                CountryDefinition countryDef = null;
                bool isCity = tile.Kind == BoardTileKind.City;
                bool isAirport = tile.Kind == BoardTileKind.Airport;
                bool isUtility = tile.Kind == BoardTileKind.Utility;
                bool isWorldEvent = tile.Kind == BoardTileKind.Special && tile.ContentId == "space-world-event";
                bool isCardVisual = isCity || isAirport || isUtility || isWorldEvent;
                bool isProperty = isCity || isAirport || isUtility;
                if (isCity)
                {
                    cityDefinitions.TryGetValue(tile.ContentId, out cityDef);
                    countryDefinitions.TryGetValue(tile.ContentId, out countryDef);
                }

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

                GameObject ownerMarker = null;
                PropertyStructureView structureView = null;
                TMP_Text upgradeText = null;

                if (!isCardVisual)
                {
                    CreateAccent(tile.Index, center, size, inward, accentColor, tileGroup.transform);

                    ownerMarker = CreateOwnerMarker(
                        tile.Index,
                        center,
                        size,
                        inward,
                        tileGroup.transform);

                    structureView = CreateStructureLot(tile.Index, center, size, inward, tileGroup.transform);
                }

                BoardPreviewTileView tileView = tileObject.AddComponent<BoardPreviewTileView>();
                tileView.Initialize(
                    tile.Index,
                    tile.Kind,
                    tile.ContentId,
                    ownerMarker,
                    ownerMarker != null ? ownerMarker.GetComponent<MeshRenderer>() : null,
                    upgradeText,
                    structureView,
                    tileGroup.transform,
                    inward);
                tileView.ConfigureBounce(_tileShakeIntensity, _tileShakeFrequency, _tileShakeDecay, _tileVerticalDip);
                _tileViews.Add(tile.Index, tileView);

                if (isCardVisual)
                {
                    CreatePropertyTileVisuals(
                        tile.Index,
                        tile.Kind,
                        tile.ContentId,
                        label,
                        detail,
                        center,
                        size,
                        inward,
                        cityDef,
                        countryDef,
                        tileView,
                        tileGroup.transform);
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

        private void CreatePropertyTileVisuals(
            int tileIndex,
            BoardTileKind kind,
            string contentId,
            string label,
            string priceText,
            Vector3 center,
            Vector2 tileSize,
            Vector3 inward,
            CityDefinition cityDef,
            CountryDefinition countryDef,
            BoardPreviewTileView tileView,
            Transform parent)
        {
            // Clean, modern property card architecture across all 28 purchasable properties
            // (22 Cities, 4 Airports, and 2 Utilities):
            // Soft, blurred themed backdrop panel on the card body with adjustable opacity and blur,
            // prominent bold property name, straight upright icons for non-city properties,
            // and the purchase price displayed in the neutral ownership band that updates when purchased.
            bool horizontalTile = tileSize.y > tileSize.x;
            float dimInward = horizontalTile ? tileSize.y : tileSize.x;
            float dimPerp = horizontalTile ? tileSize.x : tileSize.y;

            float bandDepth = horizontalTile ? 0.36f : 0.40f;
            float faceWidth = dimPerp - _theme.TileGap - 0.08f;
            float flagDepth = dimInward - _theme.TileGap - bandDepth - 0.10f;

            // Position of ownership band on the inner edge (facing board center along inward vector)
            Vector3 bandPosition = center + (inward * ((dimInward * 0.5f) - (_theme.TileGap * 0.5f) - (bandDepth * 0.5f) - 0.02f));
            bandPosition.y = 0.36f;

            Vector3 bandScale = horizontalTile
                ? new Vector3(faceWidth, 0.05f, bandDepth)
                : new Vector3(bandDepth, 0.05f, faceWidth);

            Material neutralBandMaterial = GetMaterial(_cityBandNeutralColor);

            GameObject band = CreateCube(
                $"{label} Owner Band",
                bandPosition,
                bandScale,
                _cityBandNeutralColor,
                false,
                parent);

            MeshRenderer bandRenderer = band.GetComponent<MeshRenderer>();
            bandRenderer.sharedMaterial = neutralBandMaterial;

            // Elegant rule line dividing flag/panel and ownership band
            Vector3 rulePosition = bandPosition - (inward * ((bandDepth * 0.5f) + 0.008f));
            rulePosition.y = bandPosition.y + 0.027f;
            Vector3 ruleScale = horizontalTile
                ? new Vector3(faceWidth, 0.005f, 0.016f)
                : new Vector3(0.016f, 0.005f, faceWidth);

            CreateCube(
                $"{label} Band Rule",
                rulePosition,
                ruleScale,
                new Color(0.88f, 0.84f, 0.74f, 0.75f),
                false,
                parent);

            // Backdrop panel quad on the outer portion of the tile
            Vector3 flagPosition = center - (inward * ((dimInward * 0.5f) - (_theme.TileGap * 0.5f) - (flagDepth * 0.5f) - 0.02f));
            flagPosition.y = 0.345f;

            bool isWide = !horizontalTile;
            Texture panelTexture = null;

            if (kind == BoardTileKind.City)
            {
                string countryId = countryDef != null ? countryDef.CountryId : string.Empty;
                panelTexture = GetCountryFlagTexture(countryId, _cityFlagBlur, countryDef, isWide);
            }
            else if (kind == BoardTileKind.Airport)
            {
                panelTexture = GetUtilityPanelTexture("Airport", _cityFlagBlur, isWide);
            }
            else if (kind == BoardTileKind.Utility)
            {
                string utilPrefix = contentId.Contains("water") ? "Water" : "Electric";
                panelTexture = GetUtilityPanelTexture(utilPrefix, _cityFlagBlur, isWide);
            }
            else if (contentId == "space-world-event")
            {
                panelTexture = GetUtilityPanelTexture("WorldEvent", _cityFlagBlur, isWide);
            }

            if (panelTexture != null)
            {
                Material flagMaterial = GetFlagMaterial(panelTexture, _cityFlagOpacity, _cityFlagTint);
                _customFlagMaterials.Add(flagMaterial);

                GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                quad.name = $"{label} Panel Face";
                quad.transform.SetParent(parent, false);
                quad.transform.position = flagPosition;
                // Panel is always straight upright from the camera/player view; never rotated based on tile position
                quad.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

                quad.transform.localScale = horizontalTile
                    ? new Vector3(faceWidth, flagDepth, 1f)
                    : new Vector3(flagDepth, faceWidth, 1f);

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

                quad.GetComponent<MeshRenderer>().sharedMaterial = flagMaterial;
            }

            Quaternion rotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);

            if (kind == BoardTileKind.City)
            {
                // Bold, clear City Name centered across the national flag
                Vector3 namePosition = flagPosition + new Vector3(0f, 0.035f, 0f);
                Vector2 nameBounds = horizontalTile
                    ? new Vector2(faceWidth - 0.06f, 0.55f)
                    : new Vector2(flagDepth - 0.10f, faceWidth - 0.10f);

                float nameFontSize = horizontalTile ? 0.46f : 0.44f;

                TextMeshPro shadowText = CreateWorldText(
                    $"{label} Name Shadow",
                    label.ToUpperInvariant(),
                    namePosition + new Vector3(0.02f, -0.005f, -0.02f),
                    rotation,
                    nameFontSize,
                    FontStyles.Bold,
                    new Color(0.04f, 0.04f, 0.08f, 0.95f),
                    nameBounds,
                    parent);
                shadowText.lineSpacing = -12f;
                shadowText.textWrappingMode = TextWrappingModes.Normal;
                shadowText.overflowMode = TextOverflowModes.Truncate;
                shadowText.enableAutoSizing = true;
                shadowText.fontSizeMin = 0.18f;
                shadowText.fontSizeMax = nameFontSize;

                TextMeshPro nameText = CreateWorldText(
                    $"{label} Name",
                    label.ToUpperInvariant(),
                    namePosition,
                    rotation,
                    nameFontSize,
                    FontStyles.Bold,
                    Color.white,
                    nameBounds,
                    parent);
                nameText.lineSpacing = -12f;
                nameText.textWrappingMode = TextWrappingModes.Normal;
                nameText.overflowMode = TextOverflowModes.Truncate;
                nameText.enableAutoSizing = true;
                nameText.fontSizeMin = 0.18f;
                nameText.fontSizeMax = nameFontSize;

                // Optional price lettering on the flag face (if enabled in inspector)
                if (_cityShowPriceOnFlag)
                {
                    Vector3 priceFlagOffset = horizontalTile
                        ? (inward * (flagDepth * 0.38f))
                        : (inward * (flagDepth * 0.32f));
                    Vector3 pricePosition = flagPosition - priceFlagOffset + new Vector3(0f, 0.035f, 0f);

                    CreateWorldText(
                        $"{label} Flag Price Shadow",
                        priceText,
                        pricePosition + new Vector3(0.015f, -0.005f, -0.012f),
                        rotation,
                        0.28f,
                        FontStyles.Bold,
                        new Color(0.04f, 0.04f, 0.08f, 0.95f),
                        new Vector2(faceWidth - 0.14f, 0.30f),
                        parent);
                    CreateWorldText(
                        $"{label} Flag Price",
                        priceText,
                        pricePosition,
                        rotation,
                        0.28f,
                        FontStyles.Bold,
                        Color.white,
                        new Vector2(faceWidth - 0.14f, 0.30f),
                        parent);
                }
            }
            else if (contentId == "space-world-event")
            {
                // World Event space: Big prominent '?' mark centered across the themed card panel
                Vector3 markPosition = flagPosition + new Vector3(0f, 0.035f, 0f);
                Vector2 markBounds = horizontalTile
                    ? new Vector2(faceWidth - 0.10f, flagDepth - 0.10f)
                    : new Vector2(flagDepth - 0.10f, faceWidth - 0.10f);

                float markFontSize = horizontalTile ? 1.05f : 0.95f;

                TextMeshPro markShadow = CreateWorldText(
                    $"{label} Question Mark Shadow",
                    "?",
                    markPosition + new Vector3(0.025f, -0.005f, -0.025f),
                    rotation,
                    markFontSize,
                    FontStyles.Bold,
                    new Color(0.04f, 0.04f, 0.08f, 0.95f),
                    markBounds,
                    parent);
                markShadow.alignment = TextAlignmentOptions.Center;

                TextMeshPro markText = CreateWorldText(
                    $"{label} Question Mark",
                    "?",
                    markPosition,
                    rotation,
                    markFontSize,
                    FontStyles.Bold,
                    Color.white,
                    markBounds,
                    parent);
                markText.alignment = TextAlignmentOptions.Center;
            }
            else
            {
                // Airport & Utility Tiles: Straight upright icon placed above bold property name
                Texture iconTexture = ResolvePropertyIconTexture(kind, contentId);
                if (iconTexture != null)
                {
                    Vector3 iconPosition = horizontalTile
                        ? flagPosition + new Vector3(0f, 0.030f, 0.30f)
                        : flagPosition + new Vector3(0f, 0.030f, 0.28f);

                    Vector2 iconSize = horizontalTile
                        ? new Vector2(0.58f, 0.58f)
                        : new Vector2(0.52f, 0.52f);

                    GameObject iconQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    iconQuad.name = $"{label} Icon";
                    iconQuad.transform.SetParent(parent, false);
                    iconQuad.transform.position = iconPosition;
                    iconQuad.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                    iconQuad.transform.localScale = new Vector3(iconSize.x, iconSize.y, 1f);

                    MeshCollider iconCol = iconQuad.GetComponent<MeshCollider>();
                    if (iconCol != null)
                    {
                        if (UnityEngine.Application.isPlaying)
                        {
                            Destroy(iconCol);
                        }
                        else
                        {
                            DestroyImmediate(iconCol);
                        }
                    }

                    Material iconMat = GetFlagMaterial(iconTexture, 1f, Color.white);
                    _customFlagMaterials.Add(iconMat);
                    iconQuad.GetComponent<MeshRenderer>().sharedMaterial = iconMat;
                }

                Vector3 propNamePosition = horizontalTile
                    ? flagPosition + new Vector3(0f, 0.035f, -0.32f)
                    : flagPosition + new Vector3(0f, 0.035f, -0.28f);

                Vector2 propNameBounds = horizontalTile
                    ? new Vector2(faceWidth - 0.08f, 0.56f)
                    : new Vector2(flagDepth - 0.10f, 0.54f);

                float propNameFontSize = horizontalTile ? 0.38f : 0.36f;

                TextMeshPro propShadowText = CreateWorldText(
                    $"{label} Name Shadow",
                    label.ToUpperInvariant(),
                    propNamePosition + new Vector3(0.02f, -0.005f, -0.02f),
                    rotation,
                    propNameFontSize,
                    FontStyles.Bold,
                    new Color(0.04f, 0.04f, 0.08f, 0.95f),
                    propNameBounds,
                    parent);
                propShadowText.lineSpacing = -12f;
                propShadowText.textWrappingMode = TextWrappingModes.Normal;
                propShadowText.overflowMode = TextOverflowModes.Truncate;
                propShadowText.enableAutoSizing = true;
                propShadowText.fontSizeMin = 0.18f;
                propShadowText.fontSizeMax = propNameFontSize;

                TextMeshPro propNameText = CreateWorldText(
                    $"{label} Name",
                    label.ToUpperInvariant(),
                    propNamePosition,
                    rotation,
                    propNameFontSize,
                    FontStyles.Bold,
                    Color.white,
                    propNameBounds,
                    parent);
                propNameText.lineSpacing = -12f;
                propNameText.textWrappingMode = TextWrappingModes.Normal;
                propNameText.overflowMode = TextOverflowModes.Truncate;
                propNameText.enableAutoSizing = true;
                propNameText.fontSizeMin = 0.18f;
                propNameText.fontSizeMax = propNameFontSize;
            }

            // Price lettering in the ownership band (default: ON)
            string bandDisplayText = _cityShowPriceInBand ? priceText : string.Empty;
            Vector2 bandTextBounds = horizontalTile
                ? new Vector2(faceWidth - 0.08f, bandDepth - 0.04f)
                : new Vector2(bandDepth - 0.04f, faceWidth - 0.08f);

            float bandFontSize = horizontalTile ? 0.24f : 0.22f;

            TMP_Text bandLabel = CreateWorldText(
                $"{label} Band Price",
                bandDisplayText,
                bandPosition + new Vector3(0f, 0.035f, 0f),
                rotation,
                bandFontSize,
                FontStyles.Bold,
                _cityBandTextColor,
                bandTextBounds,
                parent);
            bandLabel.enableAutoSizing = true;
            bandLabel.fontSizeMin = 0.14f;
            bandLabel.fontSizeMax = bandFontSize;

            tileView.SetOwnerBand(bandRenderer, neutralBandMaterial, bandLabel, _cityShowPriceInBand ? priceText : null);
        }

        private static string GetCountryFilePrefix(string countryId)
        {
            return countryId switch
            {
                "country-india" => "India",
                "country-brazil" => "Brazil",
                "country-china" => "China",
                "country-russia" => "Russia",
                "country-italy" => "Italy",
                "country-france" => "France",
                "country-united-kingdom" => "UnitedKingdom",
                "country-united-states" => "UnitedStates",
                _ => null,
            };
        }

        private Texture2D _proceduralFlag;

        private Texture GetCountryFlagTexture(string countryId, float blurLevel, CountryDefinition countryDef, bool isWide = false)
        {
            string prefix = GetCountryFilePrefix(countryId);
            if (string.IsNullOrEmpty(prefix) && countryDef != null)
            {
                prefix = countryDef.DisplayName.Replace(" ", "");
            }

            if (string.IsNullOrEmpty(prefix))
            {
                prefix = "Italy";
            }

            string wideSuffix = isWide ? "_Wide" : string.Empty;
            string blurSuffix = blurLevel > 0.55f ? "_Blur" : blurLevel > 0.20f ? "_Soft" : string.Empty;
            string textureName = $"CityTiles/{prefix}FlagPanel{wideSuffix}{blurSuffix}";

            Texture texture = null;

#if UNITY_EDITOR
            texture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(
                $"Assets/_Ropoly/Resources/{textureName}.png");
            if (texture == null && isWide)
            {
                texture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(
                    $"Assets/_Ropoly/Resources/CityTiles/{prefix}FlagPanel{blurSuffix}.png");
            }
            if (texture == null && prefix == "Italy")
            {
                texture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(
                    $"Assets/_Ropoly/Resources/CityTiles/MilanFlagPanel{blurSuffix}.png");
            }
#endif

            if (texture == null)
            {
                texture = Resources.Load<Texture2D>(textureName);
                if (texture == null && isWide)
                {
                    texture = Resources.Load<Texture2D>($"CityTiles/{prefix}FlagPanel{blurSuffix}");
                }
                if (texture == null && prefix == "Italy")
                {
                    texture = Resources.Load<Texture2D>($"CityTiles/MilanFlagPanel{blurSuffix}");
                }
            }

            if (texture == null)
            {
                Sprite sprite = Resources.Load<Sprite>(textureName)
                             ?? (isWide ? Resources.Load<Sprite>($"CityTiles/{prefix}FlagPanel{blurSuffix}") : null);
                if (sprite == null && prefix == "Italy")
                {
                    sprite = Resources.Load<Sprite>($"CityTiles/MilanFlagPanel{blurSuffix}");
                }

                if (sprite != null)
                {
                    texture = sprite.texture;
                }
            }

            if (texture == null && prefix == "Italy")
            {
                texture = Resources.Load<Texture2D>("CityTiles/MilanFlagPanel");
            }

            if (texture == null && countryDef != null && countryDef.Flag != null)
            {
                texture = countryDef.Flag.texture;
            }

            if (texture == null)
            {
                if (_proceduralFlag == null)
                {
                    _proceduralFlag = new Texture2D(3, 1, TextureFormat.RGB24, false);
                    _proceduralFlag.name = "Procedural_Fallback_Flag";
                    _proceduralFlag.SetPixel(0, 0, new Color(0.15f, 0.15f, 0.25f));
                    _proceduralFlag.SetPixel(1, 0, new Color(0.85f, 0.85f, 0.90f));
                    _proceduralFlag.SetPixel(2, 0, new Color(0.25f, 0.15f, 0.35f));
                    _proceduralFlag.Apply();
                }

                texture = _proceduralFlag;
            }

            return texture;
        }

        public Texture GetItalianFlagTexture(float blurLevel)
        {
            return GetCountryFlagTexture("country-italy", blurLevel, null);
        }

        private Texture GetUtilityPanelTexture(string utilityPrefix, float blurLevel, bool isWide = false)
        {
            string wideSuffix = isWide ? "_Wide" : string.Empty;
            string blurSuffix = blurLevel > 0.55f ? "_Blur" : blurLevel > 0.20f ? "_Soft" : string.Empty;
            string textureName = $"CityTiles/{utilityPrefix}Panel{wideSuffix}{blurSuffix}";

            Texture texture = null;

#if UNITY_EDITOR
            texture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(
                $"Assets/_Ropoly/Resources/{textureName}.png");
            if (texture == null && isWide)
            {
                texture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(
                    $"Assets/_Ropoly/Resources/CityTiles/{utilityPrefix}Panel{blurSuffix}.png");
            }
#endif

            if (texture == null)
            {
                texture = Resources.Load<Texture2D>(textureName);
                if (texture == null && isWide)
                {
                    texture = Resources.Load<Texture2D>($"CityTiles/{utilityPrefix}Panel{blurSuffix}");
                }
            }

            return texture;
        }

        private static Texture LoadTileIconTexture(string iconName)
        {
            Texture texture = null;

#if UNITY_EDITOR
            texture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/_Ropoly/Resources/Icons/{iconName}.png");
            if (texture == null)
            {
                texture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/_Ropoly/Art/Icons/{iconName}.png");
            }
#endif

            if (texture == null)
            {
                texture = Resources.Load<Texture2D>($"Icons/{iconName}");
            }

            return texture;
        }

        private static Texture ResolvePropertyIconTexture(BoardTileKind kind, string contentId)
        {
            if (kind == BoardTileKind.Airport)
            {
                return LoadTileIconTexture("Airport_White") ?? LoadTileIconTexture("Airport");
            }

            if (kind == BoardTileKind.Utility)
            {
                string iconName = contentId.Contains("water") ? "Water" : "Electric";
                return LoadTileIconTexture($"{iconName}_White") ?? LoadTileIconTexture(iconName);
            }

            if (contentId == "space-world-event")
            {
                return LoadTileIconTexture("WorldEvent_White") ?? LoadTileIconTexture("WorldEvent");
            }

            return null;
        }

        private Material GetFlagMaterial(Texture texture, float opacity, Color tint)
        {
            if (texture == null)
            {
                return GetMaterial(new Color(tint.r, tint.g, tint.b, opacity));
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Transparent");
            }
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Texture");
            }

            Material material = new Material(shader)
            {
                name = $"Mat_Flag_{texture.name}_{opacity:F2}",
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

            Color finalColor = new Color(tint.r, tint.g, tint.b, opacity);

            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 1f); // Transparent
            }
            if (material.HasProperty("_Blend"))
            {
                material.SetFloat("_Blend", 0f); // Alpha
            }
            if (material.HasProperty("_SrcBlend"))
            {
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            }
            if (material.HasProperty("_DstBlend"))
            {
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            }
            if (material.HasProperty("_ZWrite"))
            {
                material.SetInt("_ZWrite", 0);
            }
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            material.SetOverrideTag("RenderType", "Transparent");

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", finalColor);
            }
            else if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", finalColor);
            }

            return material;
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
                case "ELECTRIC NETWORK":
                    return "ELECTRIC\nNETWORK";
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

            Transform orphan = transform.Find("Generated Board Preview");
            if (orphan != null)
            {
                if (UnityEngine.Application.isPlaying)
                {
                    Destroy(orphan.gameObject);
                }
                else
                {
                    DestroyImmediate(orphan.gameObject);
                }

                _statusText = null;
            }

            _tileViews.Clear();

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

            foreach (Material material in _customFlagMaterials)
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
            _customFlagMaterials.Clear();
            _tileViews.Clear();
        }
    }
}
