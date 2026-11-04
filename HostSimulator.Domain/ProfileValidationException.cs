using System;
using System.Collections.Generic;

namespace HostSimulator.Domain
{
    public sealed class ProfileValidationException : Exception
    {
        public IReadOnlyList<string> Errors { get; }

        public ProfileValidationException(IReadOnlyList<string> errors)
            : base("Equipment profile validation failed: " + string.Join("; ", errors))
        {
            Errors = errors;
        }
    }
}
