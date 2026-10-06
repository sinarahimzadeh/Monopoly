using System.Collections.Generic;

namespace Ropoly.Core.Rules
{
    public sealed class GameRulesetValidationResult
    {
        private readonly List<RulesetValidationError> _errors = new List<RulesetValidationError>();

        public bool IsValid => _errors.Count == 0;

        public IReadOnlyList<RulesetValidationError> Errors => _errors;

        public bool Contains(RulesetValidationErrorCode code)
        {
            foreach (RulesetValidationError error in _errors)
            {
                if (error.Code == code)
                {
                    return true;
                }
            }

            return false;
        }

        internal void Add(RulesetValidationErrorCode code, string message)
        {
            _errors.Add(new RulesetValidationError(code, message));
        }
    }
}
