using System;
using Ropoly.Core.Match;
using Ropoly.Infrastructure.Content.Players;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ropoly.Presentation.Lobby
{
    [DisallowMultipleComponent]
    public sealed class LocalPlayerSlotView : MonoBehaviour
    {
        [SerializeField]
        private Button _button;

        [SerializeField]
        private Image _background;

        [SerializeField]
        private Image _creatureBody;

        [SerializeField]
        private Image[] _creatureEyes;

        [SerializeField]
        private TMP_Text _playerName;

        [SerializeField]
        private TMP_Text _selectionName;

        [SerializeField]
        private Color _idleColor = new Color(0.11f, 0.08f, 0.19f, 1f);

        [SerializeField]
        private Color _activeColor = new Color(0.29f, 0.18f, 0.53f, 1f);

        private int _playerIndex;

        public int PlayerIndex => _playerIndex;
        public Button Button => _button;

        public void Bind(int playerIndex, Action<int> selected)
        {
            _playerIndex = playerIndex;
            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(() => selected?.Invoke(_playerIndex));
        }

        public void Refresh(
            bool isIncluded,
            bool isActive,
            PlayerState player,
            CreatureDefinition creature)
        {
            gameObject.SetActive(isIncluded);
            if (!isIncluded)
            {
                return;
            }

            _background.color = isActive ? _activeColor : _idleColor;
            _playerName.text = player.DisplayName;
            bool hasCreature = creature != null;
            _creatureBody.gameObject.SetActive(hasCreature);
            foreach (Image eye in _creatureEyes)
            {
                eye.gameObject.SetActive(hasCreature);
                if (hasCreature)
                {
                    eye.color = creature.PupilColor;
                }
            }

            if (hasCreature)
            {
                _creatureBody.color = creature.BodyColor;
                _selectionName.text = creature.DisplayName.ToUpperInvariant();
            }
            else
            {
                _selectionName.text = "CHOOSE A CREATURE";
            }
        }
    }
}
