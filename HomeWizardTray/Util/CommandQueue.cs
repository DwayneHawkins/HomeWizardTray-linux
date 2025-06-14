using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace HomeWizardTray.Util;

internal class CommandQueue
{
    private readonly BlockingCollection<string> _commands;
    public event EventHandler<CommandQueueEventArgs> OnCommand;

    public CommandQueue()
    {
        _commands = new BlockingCollection<string>(1);

        Task.Run(() =>
        {
            while (!_commands.IsCompleted)
            {
                var command = _commands.Take(); // Blocking, waits for a new entry
                OnCommand?.Invoke(this, new CommandQueueEventArgs(command));
            }
        });
    }

    public void Add(string cmd)
    {
        _commands.Add(cmd);
    }
}

internal class CommandQueueEventArgs(string command) : EventArgs
{
    public string Command { get; } = command;
}