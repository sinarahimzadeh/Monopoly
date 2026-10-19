using Ropoly.Core.Match;
using Ropoly.Infrastructure.Content.Players;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ropoly.Presentation.Lobby
{
    [DisallowMultipleComponent]
    public sealed class PlayerHudRowView : MonoBehaviour
    {
        [SerializeField]
        private Image _creatureBody;

        [SerializeField]
        private Image[] _creatureEyes;

        [SerializeField]
        private TMP_Text _playerName;

        [SerializeField]
        private TMP_Text _cashLabel;

        public void Refresh(PlayerState player, CreatureDefinition creature)
        {
            _creatureBody.color = creature.BodyColor;
            foreach (Image eye in _creatureEyes)
            {
                eye.color = creature.PupilColor;
            }

            if (player.IsEliminated)
            {
                _playerName.text = $"{player.DisplayName}  •  OUT";
            }
            else if (player.IsDisconnected)
            {
                _playerName.text = $"{player.DisplayName}  •  AWAY";
            }
            else
            {
                _playerName.text = player.DisplayName;
            }

            _cashLabel.text = player.IsEliminated
                ? "OUT"
                : $"${player.Cash:N0}";

            Color bodyColor = creature.BodyColor;
            if (player.IsEliminated)
            {
                bodyColor.a = 0.35f;
            }

            _creatureBody.color = bodyColor;
        }

        private Button _button;
        private System.Action<int> _onClick;
        private int _playerIndex = -1;

        public void BindClick(int playerIndex, System.Action<int> onClick)
        {
            _playerIndex = playerIndex;
            _onClick = onClick;

            if (_button == null)
            {
                _button = GetComponent<Button>();
                if (_button == null)
                {
                    _button = gameObject.AddComponent<Button>();
                    var colors = _button.colors;
                    colors.normalColor = Color.white;
                    colors.highlightedColor = new Color(1f, 1f, 1f, 0.85f);
                    colors.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
                    _button.colors = colors;
                }

                _button.onClick.RemoveAllListeners();
                _button.onClick.AddListener(() =>
                {
                    if (_playerIndex >= 0)
                    {
                        _onClick?.Invoke(_playerIndex);
                    }
                });
            }
        }
    }
}
