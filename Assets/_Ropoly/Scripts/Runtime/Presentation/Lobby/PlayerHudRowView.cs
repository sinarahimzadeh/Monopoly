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
    }
}
