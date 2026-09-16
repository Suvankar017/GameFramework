using GameFramework.Runtime.Diagnostics;

namespace GameFramework.Gameplay.Commands
{
    /// <summary>Validates then executes an <see cref="IGameplayCommand"/>, logging rejections and
    /// failures through the framework logger. Stateless — not a queue or a bus; see
    /// <see cref="GameplayCommandQueue"/> for the optional deferred-execution case.</summary>
    public static class GameplayCommandInvoker
    {
        private const string LogCategory = "Commands";

        public static CommandResult Invoke(IGameplayCommand command)
        {
            if (command == null)
            {
                return CommandResult.Rejected("Command was null.");
            }

            if (!command.CanExecute())
            {
                string message = $"{command.GetType().Name} rejected by CanExecute.";
                Log.Debug(LogCategory, message);
                return CommandResult.Rejected(message);
            }

            CommandResult result = command.Execute();
            if (result.Status == CommandResultStatus.Failure)
            {
                Log.Warning(LogCategory, $"{command.GetType().Name} failed: {result.Message}");
            }

            return result;
        }
    }
}
