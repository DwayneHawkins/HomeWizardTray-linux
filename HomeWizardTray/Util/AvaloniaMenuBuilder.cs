using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;

namespace HomeWizardTray.Util;

internal static class AvaloniaMenuBuilder
{
    public static NativeMenu Build(IEnumerable<MenuEntry> menuEntries)
    {
        var menu = new NativeMenu();

        foreach (var entry in menuEntries)
        {
            if (entry.Caption == "-")
            {
                menu.Add(new NativeMenuItemSeparator());
            }
            else
            {
                var item = new NativeMenuItem(entry.Caption);

                if (entry.OnClick is null && (entry.Children == null || entry.Children.Length == 0))
                {
                    item.IsEnabled = false;
                }
                else if (entry.OnClick is not null)
                {
                    var callback = entry.OnClick;
                    item.Click += async (_, _) => await callback();
                }

                if (entry.Children?.Length > 0)
                {
                    item.Menu = Build(entry.Children);
                }

                menu.Add(item);
            }
        }

        return menu;
    }
}

internal sealed class MenuEntry
{
    public string Caption { get; }
    public Func<Task> OnClick { get; }
    public MenuEntry[] Children { get; }

    public MenuEntry(string caption)
    {
        Caption = caption;
        Children = [];
    }

    public MenuEntry(string caption, Func<Task> onClick)
    {
        Caption = caption;
        OnClick = onClick;
        Children = [];
    }

    public MenuEntry(string caption, IEnumerable<MenuEntry> children)
    {
        Caption = caption;
        Children = children.ToArray();
    }
}
