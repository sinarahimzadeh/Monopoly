using System;
using System.Collections.Generic;
using Ropoly.Core.Board;
using Ropoly.Core.Match;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ropoly.Presentation.Dice
{
    [DisallowMultipleComponent]
    public sealed class DebtResolutionView : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text _summaryLabel;

        [SerializeField]
        private TMP_Text _propertyLabel;

        [SerializeField]
        private Button _previousButton;

        [SerializeField]
        private Button _nextButton;

        [SerializeField]
        private Button _mortgageButton;

        [SerializeField]
        private TMP_Text _mortgageButtonLabel;

        [SerializeField]
        private Button _sellButton;

        [SerializeField]
        private TMP_Text _sellButtonLabel;

        [SerializeField]
        private Button _bankruptcyButton;

        private readonly List<PropertyState> _assets = new List<PropertyState>();
        private LocalMatchSession _session;
        private Action<int> _mortgageAction;
        private Action<int> _sellAction;
        private Action _bankruptcyAction;
        private int _selectedIndex;

        public bool IsVisible => gameObject.activeSelf;

        private void Awake()
        {
            _previousButton.onClick.AddListener(SelectPrevious);
            _nextButton.onClick.AddListener(SelectNext);
            _mortgageButton.onClick.AddListener(MortgageSelected);
            _sellButton.onClick.AddListener(SellSelected);
            _bankruptcyButton.onClick.AddListener(DeclareBankruptcy);
        }

        public void Show(
            LocalMatchSession session,
            Action<int> mortgageAction,
            Action<int> sellAction,
            Action bankruptcyAction)
        {
            _session = session;
            _mortgageAction = mortgageAction;
            _sellAction = sellAction;
            _bankruptcyAction = bankruptcyAction;
            _selectedIndex = 0;
            gameObject.SetActive(true);
            Refresh();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
            _session = null;
            _assets.Clear();
        }

        public void Refresh()
        {
            _assets.Clear();
            if (_session == null ||
                _session.State.Turn == null ||
                _session.State.Turn.RentPayment == null)
            {
                Hide();
                return;
            }

            RentPayment payment = _session.State.Turn.RentPayment;
            PlayerState payer = _session.State.Players[payment.PayerPlayerIndex];
            string ownerName = payment.OwnerPlayerIndex >= 0 && payment.OwnerPlayerIndex < _session.State.Players.Count
                ? _session.State.Players[payment.OwnerPlayerIndex].DisplayName
                : "THE BANK";
            TimeSpan? remaining = _session.CurrentTurnRemainingTime;
            string timerText = remaining.HasValue
                ? $"\nTIME REMAINING  {Mathf.CeilToInt((float)remaining.Value.TotalSeconds)}s"
                : string.Empty;

            _summaryLabel.text =
                $"{payer.DisplayName} OWES {ownerName}  ${payment.Amount:N0}\n" +
                $"CASH AVAILABLE  ${payer.Cash:N0}{timerText}";

            IReadOnlyList<PropertyState> properties =
                _session.GetPropertiesOwnedBy(payment.PayerPlayerIndex);
            foreach (PropertyState property in properties)
            {
                _assets.Add(property);
            }

            _selectedIndex = Mathf.Clamp(_selectedIndex, 0, Mathf.Max(0, _assets.Count - 1));
            RefreshSelectedAsset();
        }

        public void UpdateCountdown(int remainingSeconds)
        {
            if (_session == null ||
                _session.State.Turn == null ||
                _session.State.Turn.RentPayment == null)
            {
                return;
            }

            RentPayment payment = _session.State.Turn.RentPayment;
            PlayerState payer = _session.State.Players[payment.PayerPlayerIndex];
            string ownerName = payment.OwnerPlayerIndex >= 0 && payment.OwnerPlayerIndex < _session.State.Players.Count
                ? _session.State.Players[payment.OwnerPlayerIndex].DisplayName
                : "THE BANK";
            _summaryLabel.text =
                $"{payer.DisplayName} OWES {ownerName}  ${payment.Amount:N0}\n" +
                $"CASH AVAILABLE  ${payer.Cash:N0}\n" +
                $"TIME REMAINING  {remainingSeconds}s";
        }

        private void RefreshSelectedAsset()
        {
            bool hasAssets = _assets.Count > 0;
            _previousButton.interactable = _assets.Count > 1;
            _nextButton.interactable = _assets.Count > 1;
            _bankruptcyButton.interactable = true;

            if (!hasAssets)
            {
                _propertyLabel.text = "NO ASSETS AVAILABLE\nDECLARE BANKRUPTCY TO LEAVE THE MATCH";
                _mortgageButtonLabel.text = "NO PROPERTY";
                _sellButtonLabel.text = "NO PROPERTY";
                _mortgageButton.interactable = false;
                _sellButton.interactable = false;
                return;
            }

            PropertyState property = _assets[_selectedIndex];
            bool hasBuildingsInSet =
                property.Kind == BoardTileKind.City &&
                !string.IsNullOrEmpty(property.CountryId) &&
                _session.HasUpgradesInCountrySet(property.CountryId);

            string status;
            if (property.UpgradeLevel > 0)
            {
                status = $"LEVEL {property.UpgradeLevel} BUILDINGS";
            }
            else
            {
                status = property.IsMortgaged ? "MORTGAGED" : "ACTIVE";
            }

            _propertyLabel.text =
                $"{_selectedIndex + 1} / {_assets.Count}  •  {property.DisplayName.ToUpperInvariant()}\n" +
                $"{status}  •  BANK VALUE ${property.MortgageValue:N0}";

            if (property.UpgradeLevel > 0)
            {
                _mortgageButtonLabel.text = "SELL ALL BUILDINGS FIRST";
                _mortgageButton.interactable = false;

                bool canDowngrade = _session.CanDowngradeProperty(property.TileIndex, out _);
                _sellButtonLabel.text = canDowngrade
                    ? $"SELL BUILDING +${property.DowngradeRefund:N0}"
                    : "SELL EVENLY FIRST";
                _sellButton.interactable = canDowngrade;
            }
            else if (hasBuildingsInSet)
            {
                _mortgageButtonLabel.text = "SELL BUILDINGS IN SET FIRST";
                _mortgageButton.interactable = false;
                _sellButtonLabel.text = "SELL BUILDINGS IN SET FIRST";
                _sellButton.interactable = false;
            }
            else
            {
                _mortgageButtonLabel.text = property.IsMortgaged
                    ? "MORTGAGED"
                    : $"MORTGAGE +${property.MortgageValue:N0}";
                _sellButtonLabel.text = property.BankSaleValue > 0
                    ? $"SELL +${property.BankSaleValue:N0}"
                    : "NO SALE VALUE";
                _mortgageButton.interactable =
                    _session.MortgageEnabled &&
                    !property.IsMortgaged &&
                    property.MortgageValue > 0;
                _sellButton.interactable = property.BankSaleValue > 0;
            }
        }

        private void SelectPrevious()
        {
            if (_assets.Count <= 1)
            {
                return;
            }

            _selectedIndex = (_selectedIndex - 1 + _assets.Count) % _assets.Count;
            RefreshSelectedAsset();
        }

        private void SelectNext()
        {
            if (_assets.Count <= 1)
            {
                return;
            }

            _selectedIndex = (_selectedIndex + 1) % _assets.Count;
            RefreshSelectedAsset();
        }

        private void MortgageSelected()
        {
            if (_assets.Count > 0)
            {
                _mortgageAction?.Invoke(_assets[_selectedIndex].TileIndex);
            }
        }

        private void SellSelected()
        {
            if (_assets.Count > 0)
            {
                _sellAction?.Invoke(_assets[_selectedIndex].TileIndex);
            }
        }

        private void DeclareBankruptcy()
        {
            _bankruptcyAction?.Invoke();
        }
    }
}
