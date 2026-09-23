using System;

namespace GameFramework.Runtime.Bootstrap
{
    /// <summary>One service whose <c>Initialize</c> threw during <see cref="GameBootstrapper"/>
    /// startup - see <see cref="GameBootstrapper.InitializationFailures"/>. Diagnostic data for the
    /// game/developer, never meant to be shown to a player verbatim.</summary>
    public readonly struct ServiceInitializationFailure
    {
        public readonly Type ServiceType;
        public readonly Exception Exception;

        public ServiceInitializationFailure(Type serviceType, Exception exception)
        {
            ServiceType = serviceType;
            Exception = exception;
        }

        public override string ToString() =>
            $"{ServiceType?.Name ?? "<unknown>"}: {Exception?.GetType().Name ?? "<no exception>"}";
    }
}
