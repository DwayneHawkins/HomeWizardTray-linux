using System.Collections.Generic;

namespace HomeWizardTray.DataProviders.Sma;

internal enum UserType
{
    usr,
    istl,
    svc,
    dvlp,
}

internal sealed class UserInfo
{
    public UserType User { get; set; }
    public int Tag { get; set; }
    public int LoginLevel { get; set; }

    public static UserInfo Get(UserType user)
    {
        return Info[user];
    }

    private static readonly Dictionary<UserType, UserInfo> Info = new()
    {
        { UserType.usr,  new UserInfo { User = UserType.usr,  Tag = 861, LoginLevel = 1 } },
        { UserType.istl, new UserInfo { User = UserType.istl, Tag = 862, LoginLevel = 2 } },
        { UserType.svc,  new UserInfo { User = UserType.svc,  Tag = 863, LoginLevel = 3 } },
        { UserType.dvlp, new UserInfo { User = UserType.dvlp, Tag = 864, LoginLevel = 4 } }
    };
}