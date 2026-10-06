using System.Collections.Generic;

namespace Ropoly.Core.Board
{
    public sealed class BoardValidationResult
    {
        private readonly List<BoardValidationError> _errors = new List<BoardValidationError>();

        public bool IsValid => _errors.Count == 0;
        public IReadOnlyList<BoardValidationError> Errors => _errors;

        public bool Contains(BoardValidationErrorCode code)
        {
            foreach (BoardValidationError error in _errors)
            {
                if (error.Code == code)
                {
                    return true;
                }
            }

            return false;
        }

        internal void Add(BoardValidationErrorCode code, string message)
        {
            _errors.Add(new BoardValidationError(code, message));
        }
    }
}
