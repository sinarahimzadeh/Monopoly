using System;
using Ropoly.Infrastructure.Content.Players;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ropoly.Presentation.Lobby
{
    [DisallowMultipleComponent]
    public sealed class CreatureChoiceView : MonoBehaviour
    {
        [SerializeField]
        private Button _button;

        [SerializeField]
        private Image _background;

        [SerializeField]
        private Image _body;

        [SerializeField]
        private Image[] _eyes;

        [SerializeField]
        private TMP_Text _nameLabel;

        [SerializeField]
        private TMP_Text _stateLabel;

        [SerializeField]
        private Color _availableColor = new Color(0.105f, 0.075f, 0.18f, 1f);

        [SerializeField]
        private Color _selectedColor = new Color(0.30f, 0.19f, 0.56f, 1f);

        [SerializeField]
        private Color _unavailableColor = new Color(0.055f, 0.043f, 0.085f, 0.82f);

        private CreatureDefinition _definition;

        public CreatureDefinition Definition => _definition;
        public Button Button => _button;

        public void Bind(CreatureDefinition definition, Action<CreatureDefinition> selected)
        {
            _definition = definition;
            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(() => selected?.Invoke(_definition));
            _body.color = definition.BodyColor;
            foreach (Image eye in _eyes)
            {
                eye.color = definition.PupilColor;
            }

            _nameLabel.text = definition.DisplayName.ToUpperInvariant();
        }

        public void Refresh(bool selectedByActivePlayer, bool selectedByAnotherPlayer)
        {
            _button.interactable = !selectedByAnotherPlayer;
            if (selectedByActivePlayer)
            {
                _background.color = _selectedColor;
                _stateLabel.text = "SELECTED";
            }
            else if (selectedByAnotherPlayer)
            {
                _background.color = _unavailableColor;
                _stateLabel.text = "TAKEN";
            }
            else
            {
                _background.color = _availableColor;
                _stateLabel.text = "AVAILABLE";
            }

            Color bodyColor = _definition.BodyColor;
            bodyColor.a = selectedByAnotherPlayer ? 0.34f : 1f;
            _body.color = bodyColor;
        }
    }
}
