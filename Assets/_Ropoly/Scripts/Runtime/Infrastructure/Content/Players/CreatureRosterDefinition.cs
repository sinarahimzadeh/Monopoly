using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ropoly.Infrastructure.Content.Players
{
    [CreateAssetMenu(
        fileName = "CreatureRoster",
        menuName = "Ropoly/Content/Players/Creature Roster",
        order = 21)]
    public sealed class CreatureRosterDefinition : ScriptableObject
    {
        [SerializeField]
        private List<CreatureDefinition> _creatures = new List<CreatureDefinition>();

        public IReadOnlyList<CreatureDefinition> Creatures => _creatures;

        public bool IsValid
        {
            get
            {
                if (_creatures == null || _creatures.Count < 4)
                {
                    return false;
                }

                HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
                foreach (CreatureDefinition creature in _creatures)
                {
                    if (creature == null || !creature.IsValid || !ids.Add(creature.CreatureId))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public bool TryGetCreature(string creatureId, out CreatureDefinition creature)
        {
            foreach (CreatureDefinition candidate in _creatures)
            {
                if (candidate != null &&
                    string.Equals(candidate.CreatureId, creatureId, StringComparison.Ordinal))
                {
                    creature = candidate;
                    return true;
                }
            }

            creature = null;
            return false;
        }
    }
}
