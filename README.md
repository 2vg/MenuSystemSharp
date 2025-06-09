# MenuSystemSharp

**This project is currently under active development**

## Overview

MenuSystemSharp is a library and sample plugin designed to enable the use of [Wend4r's mms2-menu_system](https://github.com/Wend4r/mms2-menu_system), a MetaMod plugin for Counter-Strike 2 (CS2), from C#.
It is intended to run on the CounterStrikeSharp framework.

## TODO

*  Implement resolution switch
*  Implement save&load resolution processing

## Required

*  [roflmuffin/CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp).
*  [Wend4r/mms2-menu_system](https://github.com/Wend4r/mms2-menu_system)

## Build

There is no release for now, sorry.
Just clone the repository, then please build it.

## Config

Please see: [Wend4r/mms2-menu_system](https://github.com/Wend4r/mms2-menu_system)

## Installation

1. Install [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp)
2. Install [Wend4r's mms2-menu_system](https://github.com/Wend4r/mms2-menu_system)
3. Build or download MenuSystemSharp plugin
4. Place the compiled plugin in your CounterStrikeSharp plugins directory
5. Load the plugin and ensure it detects the menu system

## Usage for External Plugins

MenuSystemSharp is designed to be used as a dependency by other CounterStrikeSharp plugins. Here's how to use it:

### Basic Usage with MenuSystemHelper

```csharp
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using MenuSystemSharp;

public class MyAwesomePlugin : BasePlugin
{
    public override void Load(bool hotReload)
    {
        AddCommand("css_mymenu", "Show my custom menu", OnMyMenuCommand);
    }

    private void OnMyMenuCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid || player.IsBot)
            return;

        // Check if MenuSystem is available
        if (!MenuSystemHelper.IsAvailable)
        {
            player.PrintToChat("Menu system is not available.");
            return;
        }

        try
        {
            ShowMainMenu(player);
        }
        catch (Exception ex)
        {
            player.PrintToChat($"Error creating menu: {ex.Message}");
        }
    }

    private void ShowMainMenu(CCSPlayerController player)
    {
        // Create menu using helper
        var menu = MenuSystemHelper.CreateMenu("My Plugin Menu");

        // Add menu items with callbacks
        MenuSystemHelper.AddItem(menu, "Player Info", (selectedPlayer, menuInstance, itemIndex) =>
        {
            selectedPlayer?.PrintToChat($"Your name: {selectedPlayer.PlayerName}");
            selectedPlayer?.PrintToChat($"Your SteamID: {selectedPlayer.SteamID}");
        });

        MenuSystemHelper.AddItem(menu, "Server Info", (selectedPlayer, menuInstance, itemIndex) =>
        {
            selectedPlayer?.PrintToChat($"Map: {Server.MapName}");
        });

        MenuSystemHelper.AddItem(menu, "Sub Menu", (selectedPlayer, menuInstance, itemIndex) =>
        {
            ShowSubMenu(selectedPlayer);
        });

        // Add simple item without callback
        MenuSystemHelper.AddItem(menu, "Disabled Option", MenuItemStyleFlags.Active);

        // Display the menu
        MenuSystemHelper.DisplayMenu(menu, player);
    }

    private void ShowSubMenu(CCSPlayerController? player)
    {
        if (player == null) return;

        var subMenu = MenuSystemHelper.CreateMenu("Sub Menu");

        MenuSystemHelper.AddItem(subMenu, "Option A", (selectedPlayer, menuInstance, itemIndex) =>
        {
            selectedPlayer?.PrintToChat("You selected Option A!");
        });

        MenuSystemHelper.AddItem(subMenu, "Option B", (selectedPlayer, menuInstance, itemIndex) =>
        {
            selectedPlayer?.PrintToChat("You selected Option B!");
        });

        MenuSystemHelper.AddItem(subMenu, "Back to Main", (selectedPlayer, menuInstance, itemIndex) =>
        {
            ShowMainMenu(selectedPlayer);
        });

        MenuSystemHelper.DisplayMenu(subMenu, player);
    }
}
```

### Advanced Usage with Direct API

For more control, you can use the direct API:

```csharp
using MenuSystemSharp;

public class AdvancedMenuPlugin : BasePlugin
{
    private IMenuSystem? _menuSystem;

    public override void Load(bool hotReload)
    {
        // Get the menu system instance
        _menuSystem = MenuSystemCSharp.GetMenuSystemInstance();
        
        if (_menuSystem == null)
        {
            Console.WriteLine("MenuSystem not available");
            return;
        }

        AddCommand("css_advanced_menu", "Show advanced menu", OnAdvancedMenuCommand);
    }

    private void OnAdvancedMenuCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid || _menuSystem == null)
            return;

        // Get profile system
        var profileSystem = _menuSystem.GetProfiles();
        
        // Use specific profile (or default)
        var profile = profileSystem.GetProfile("default");
        if (profile == null)
        {
            player.PrintToChat("Menu profile not found.");
            return;
        }

        // Create menu instance
        var menu = _menuSystem.CreateInstance(profile, null);
        menu.SetTitle("Advanced Menu");

        // Add items with different styles
        menu.AddItem("Normal Item", (selectedPlayer, menuInstance, itemIndex) =>
        {
            selectedPlayer?.PrintToChat("Normal item selected!");
        }, MenuItemStyleFlags.Default);

        menu.AddItem("Control Item", (selectedPlayer, menuInstance, itemIndex) =>
        {
            selectedPlayer?.PrintToChat("Control item selected!");
        }, MenuItemStyleFlags.Control | MenuItemStyleFlags.HasNumber);

        // Display with custom parameters
        _menuSystem.DisplayInstanceToPlayer(menu, player.Slot, 0, 30); // 30 seconds timeout
    }
}
```

### Menu Item Styles

Available `MenuItemStyleFlags`:

- `Disabled`: no selectable item
- `Active`: Standard selectable item
- `HasNumber`: Shows item number
- `Control`: Control item (like Back/Exit)
- `Default`: Combination of `Active | HasNumber`
- `Full`: Combination of `Default | Control`

## Special Thanks

* [Wend4r](https://github.com/Wend4r)
