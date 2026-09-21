using GameFramework.GameFlow;
using GameFramework.Input;
using GameFramework.Runtime.Events;
using GameFramework.Runtime.Services;
using GameFramework.UI;

namespace GameFramework.UI.Navigation.Tests
{
    /// <summary>See <c>GameFramework.GameFlow.Tests.TestRegistryFactory</c>'s remarks - same
    /// pattern, duplicated per test assembly since these are test-only internal helpers.</summary>
    internal static class TestRegistryFactory
    {
        public static ServiceRegistry Build(
            out UIService ui,
            out EventService events,
            out FakeInputService input,
            out FakeGameFlowService gameFlow)
        {
            var registry = new ServiceRegistry();

            ui = new UIService();
            registry.Register<IUIService>(ui);
            ui.Initialize(registry);
            registry.MarkInitialized(typeof(IUIService));

            events = new EventService();
            registry.Register<IEventService>(events);
            events.Initialize(registry);
            registry.MarkInitialized(typeof(IEventService));

            input = new FakeInputService();
            registry.Register<IInputService>(input);
            registry.MarkInitialized(typeof(IInputService));

            gameFlow = new FakeGameFlowService();
            registry.Register<IGameFlowService>(gameFlow);
            registry.MarkInitialized(typeof(IGameFlowService));

            return registry;
        }
    }
}
