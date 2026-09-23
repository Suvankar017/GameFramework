namespace GameFramework.RemoteConfig
{
    /// <summary>
    /// See CLAUDE.md's Phase 17 brief, sections 14/18/56. A single aggregate failure reason for the
    /// whole candidate snapshot - validation stops at the first violation, since the outcome
    /// (reject the entire snapshot) is the same regardless of how many keys are invalid.
    /// </summary>
    public readonly struct RemoteConfigValidationResult
    {
        public readonly bool IsValid;
        public readonly string FailureDetail;

        private RemoteConfigValidationResult(bool isValid, string failureDetail)
        {
            IsValid = isValid;
            FailureDetail = failureDetail;
        }

        public static RemoteConfigValidationResult Valid() => new RemoteConfigValidationResult(true, null);
        public static RemoteConfigValidationResult Invalid(string detail) => new RemoteConfigValidationResult(false, detail);
    }
}
