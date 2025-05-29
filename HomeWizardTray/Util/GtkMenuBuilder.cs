using System;
using System.Collections.Generic;
using System.Linq;
using GtkMenu = Gtk.Menu;
using GtkMenuItem = Gtk.MenuItem;
using GtkMenuSeperator = Gtk.SeparatorMenuItem;

namespace HomeWizardTray.Util;

internal static class GtkMenuBuilder
{
    public static GtkMenu Build(IEnumerable<MenuEntry> menuEntries)
    {
        var menu = new GtkMenu();

        foreach (var menuEntry in menuEntries)
        {
            if (menuEntry.Caption == "-")
            {
                var gtpSeparator = new GtkMenuSeperator();
                menu.Append(gtpSeparator);
            }
            else
            {
                var gtkMenuItem = new GtkMenuItem(menuEntry.Caption);
                menu.Append(gtkMenuItem);

                if (menuEntry.OnClick is not null)
                {
                    gtkMenuItem.Activated += menuEntry.OnClick;
                }

                if (menuEntry.Children?.Length > 0)
                {
                    gtkMenuItem.Submenu = Build(menuEntry.Children);
                }
            }
        }

        menu.ShowAll();
        return menu;
    }
}

internal sealed class MenuEntry
{
    public string Caption { get; set; }
    public EventHandler OnClick { get; set; }
    public MenuEntry[] Children { get; set; } = [];

    public MenuEntry(string caption)
    {
        Caption = caption;
    }

    public MenuEntry(string caption, EventHandler onClick)
    {
        Caption = caption;
        OnClick = onClick;
    }

    public MenuEntry(string caption, IEnumerable<MenuEntry> children)
    {
        Caption = caption;
        Children = children.ToArray();
    }
}