namespace GameFramework.Platform
{
    /// <summary>
    /// <see cref="NotDetermined"/> is honest, not exhaustive: Unity's own
    /// <see cref="UnityEngine.Application.HasUserAuthorization"/> only distinguishes granted from
    /// not-granted - on iOS in particular, "denied" and "never asked" are indistinguishable through
    /// this API before a request is made. <see cref="PermissionService"/> reports
    /// <see cref="NotDetermined"/> for that pre-request state and <see cref="Denied"/> only after an
    /// actual request comes back refused.
    /// </summary>
    public enum PermissionStatus
    {
        NotDetermined,
        Granted,
        Denied
    }
}
