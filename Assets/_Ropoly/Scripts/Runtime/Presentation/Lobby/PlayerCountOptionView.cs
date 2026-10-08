using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ropoly.Presentation.Lobby
{
    [DisallowMultipleComponent]
    public sealed class PlayerCountOptionView : MonoBehaviour
    {
        [SerializeField]
        private int _playerCount = 2;

        [SerializeField]
        private Button _button;

        [SerializeField]
        private Image _background;

        [SerializeField]
        private TMP_Text _label;

        [SerializeField]
        private Color _idleColor = new Color(0.11f, 0.08f, 0.19f, 1f);

        [SerializeField]
        private Color _selectedColor = new Color(0.29f, 0.18f, 0.53f, 1f);

        public int PlayerCount => _playerCount;

        public void Bind(Action<int> selected)
        {
            _label.text = _playerCount.ToString();
            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(() => selected?.Invoke(_playerCount));
        }

        public void Refresh(bool isSelected)
        {
            _background.color = isSelected ? _selectedColor : _idleColor;
        }
    }
}
