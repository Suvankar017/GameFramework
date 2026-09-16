namespace GameFramework.Gameplay.Commands
{
    /// <summary>Predictable outcome of an <see cref="IGameplayCommand"/> — normal gameplay
    /// rejection is expressed here, never via an exception.</summary>
    public readonly struct CommandResult
    {
        public readonly CommandResultStatus Status;
        public readonly string Message;

        private CommandResult(CommandResultStatus status, string message)
        {
            Status = status;
            Message = message;
        }

        public bool IsSuccess => Status == CommandResultStatus.Success;

        public static CommandResult Success(string message = null) => new CommandResult(CommandResultStatus.Success, message);
        public static CommandResult Failure(string message = null) => new CommandResult(CommandResultStatus.Failure, message);
        public static CommandResult Rejected(string message = null) => new CommandResult(CommandResultStatus.Rejected, message);
    }
}
