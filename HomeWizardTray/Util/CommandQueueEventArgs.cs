using System;

namespace HomeWizardTray.Util;

internal class CommandQueueEventArgs(string command) : EventArgs
{
    public string Command { get; } = command;
}