using System;
using System.Threading.Tasks;

namespace HomeWizardTray.Util;

internal class CommandQueueEventArgs(Func<Task> action) : EventArgs
{
    public Func<Task> Action { get; } = action;
}