using System;

namespace HomeWizardTray.DataProviders.HomeWizard;

public class HomeWizardInfo
{
    public uint Export { get; private set; }
    public uint Import { get; private set; }

    public HomeWizardInfo(decimal activePower)
    {
        Import = activePower < 0 ? 0 : (uint)activePower;
        Export = activePower > 0 ? 0 : (uint)Math.Abs((int)activePower);
    }
}