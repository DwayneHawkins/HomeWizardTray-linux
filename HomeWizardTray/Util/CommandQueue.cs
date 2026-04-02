using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace HomeWizardTray.Util;

/// <summary>
/// Decouples GTK menu callbacks from async device communication.
/// GTK's main loop requires event handlers to be synchronous. The common workarounds don't work:
/// - async void swallows exceptions and can crash the application.
/// - sync-over-async (.Result / .GetAwaiter().GetResult()) deadlocks the GTK main loop thread,
///   because the async continuation needs that same thread to resume.
/// This queue sidesteps both issues: GTK callbacks stay fully synchronous (just Add() a command),
/// while a background thread picks up commands and runs async device work on its own thread,
/// free from GTK's SynchronizationContext. Capacity is limited to 1 to ensure sequential processing.
/// </summary>
internal class CommandQueue : IDisposable
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
                if (_commands.TryTake(out var command, Timeout.Infinite))
                {
                    OnCommand?.Invoke(this, new CommandQueueEventArgs(command));
                }
            }
        });
    }

    public void Add(string cmd)
    {
        _commands.Add(cmd);
    }

    public void Dispose()
    {
        _commands.CompleteAdding();
        _commands.Dispose();
    }
}