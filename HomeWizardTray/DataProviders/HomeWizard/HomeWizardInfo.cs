using System;

namespace HomeWizardTray.DataProviders.HomeWizard;

public class HomeWizardInfo(decimal activePower)
{
    public uint Export { get; private set; } = activePower > 0 ? 0 : (uint)Math.Abs((int)activePower);
    public uint Import { get; private set; } = activePower < 0 ? 0 : (uint)activePower;
}