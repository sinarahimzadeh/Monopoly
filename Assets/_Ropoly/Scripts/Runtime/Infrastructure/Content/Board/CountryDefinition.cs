using System.Collections.Generic;
using Ropoly.Core.Board;
using UnityEngine;

namespace Ropoly.Infrastructure.Content.Board
{
    [CreateAssetMenu(fileName = "Country", menuName = "Ropoly/Board/Country", order = 10)]
    public sealed class CountryDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField]
        [Tooltip("Stable lowercase ID used by saves, networking, and board references.")]
        private string _countryId;

        [SerializeField]
        private string _displayName;

        [Header("Set")]
        [SerializeField]
        [Range(BoardContentLimits.MinimumCitiesPerCountry, BoardContentLimits.MaximumCitiesPerCountry)]
        private int _expectedCityCount = BoardContentLimits.MaximumCitiesPerCountry;

        [SerializeField]
        private Color _setColor = Color.white;

        [SerializeField]
        private Sprite _flag;

        [SerializeField]
        private List<CityDefinition> _cities = new List<CityDefinition>();

        [Header("Economic ordering")]
        [SerializeField]
        [Min(1)]
        [Tooltip("Nominal GDP per capita in USD, used to order country value tiers.")]
        private int _gdpPerCapitaUsd = 1;

        [SerializeField]
        [Range(BoardContentLimits.MinimumEconomicDataYear, BoardContentLimits.MaximumEconomicDataYear)]
        private int _economicDataYear = 2025;

        [SerializeField]
        [Tooltip("Human-readable source name, such as IMF World Economic Outlook.")]
        private string _economicDataSource = "Unspecified";

        public string CountryId => _countryId;
        public string DisplayName => _displayName;
        public int ExpectedCityCount => _expectedCityCount;
        public Color SetColor => _setColor;
        public Sprite Flag => _flag;
        public IReadOnlyList<CityDefinition> Cities => _cities;

        public CountrySnapshot CreateSnapshot()
        {
            string[] cityIds = new string[_cities.Count];
            for (int index = 0; index < _cities.Count; index++)
            {
                cityIds[index] = _cities[index] == null ? null : _cities[index].ContentId;
            }

            return new CountrySnapshot(
                _countryId,
                _displayName,
                _expectedCityCount,
                _gdpPerCapitaUsd,
                _economicDataYear,
                _economicDataSource,
                $"#{ColorUtility.ToHtmlStringRGBA(_setColor)}",
                cityIds);
        }

        private void OnValidate()
        {
            _expectedCityCount = Mathf.Clamp(
                _expectedCityCount,
                BoardContentLimits.MinimumCitiesPerCountry,
                BoardContentLimits.MaximumCitiesPerCountry);
            _gdpPerCapitaUsd = Mathf.Clamp(
                _gdpPerCapitaUsd,
                1,
                BoardContentLimits.MaximumMoneyValue);
            _economicDataYear = Mathf.Clamp(
                _economicDataYear,
                BoardContentLimits.MinimumEconomicDataYear,
                BoardContentLimits.MaximumEconomicDataYear);
        }
    }
}
