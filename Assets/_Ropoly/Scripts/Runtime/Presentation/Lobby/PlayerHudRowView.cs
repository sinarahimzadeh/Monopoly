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

            _playerName.text = player.DisplayName;
            _cashLabel.text = $"${player.Cash:N0}";
        }
    }
}
