# MenuSystemSharp

**This project is currently under active development**

## Overview

MenuSystemSharp is a library and sample plugin designed to enable the use of [Wend4r's mms2-menu_system](https://github.com/Wend4r/mms2-menu_system), a MetaMod plugin for Counter-Strike 2 (CS2), from C#.
It is intended to run on the CounterStrikeSharp framework.

## Warning

Use my [mms2-menu_system](https://github.com/2vg/mms2-menu_system) for now.

Because there are some exports added to the original implementation, so MenuSystemSharp needs them.

## TODO

*  Implement resolution switch
*  Implement save&load resolution processing

## Project Structure

### MenuSystemSharp.API
- **Purpose**: API interface for external plugins
- **Distribution**: NuGet package
- **Dependencies**: CounterStrikeSharp.API only
- **Description**: External plugins reference this API to use MenuSystem functionality

### MenuSystemSharp
- **Purpose**: MenuSystem implementation plugin
- **Distribution**: Plugin DLL
- **Dependencies**: MenuSystemSharp.API, CounterStrikeSharp.API
- **Description**: Provides the actual menu system implementation and registers the implementation with the API interface

### ExamplePlugin
- **Purpose**: Sample plugin demonstrating API usage
- **Dependencies**: MenuSystemSharp.API
- **Description**: Shows how external plugins can use the API

## Architecture

```
External Plugin
    ↓ (references)
MenuSystemSharp.API (NuGet Package)
    ↑ (implementation registration)
MenuSystemSharp (Plugin)
    ↓ (native library calls)
Wend4r's MetaMod Menu System
```

## Usage

### Config

Please see: [Wend4r/mms2-menu_system](https://github.com/Wend4r/mms2-menu_system)

### 1. Installing MenuSystemSharp Plugin
1. Install [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp)
2. Install [My mms2-menu_system](https://github.com/2vg/mms2-menu_system)
3. Place the MenuSystemSharp plugin in your server's plugin folder

### 2. Using in External Plugins
1. Install the `MenuSystemSharp.API` NuGet package
2. Initialize the menu system in your plugin's `OnAllPluginsLoaded` method
3. Use the API to create and display menus

### 3. Example

```csharp
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using MenuSystemSharp.API;

public class MyPlugin : BasePlugin
{
    private IMenuSystem? _menuSystem;

    public override void OnAllPluginsLoaded(bool hotReload)
    {
        RegisterListener<Listeners.OnMetamodAllPluginsLoaded>(() =>
        {
            if (MenuSystem.Instance == null)
            {
                throw new InvalidOperationException("MenuSystemSharp plugin is not loaded. Please ensure it is installed and loaded before this plugin.");
            }
            else
            {
                _menuSystem = MenuSystem.Instance;
            }
        });
    }

    [ConsoleCommand("css_mymenu", "Opens a custom menu")]
    public void OnMyMenuCommand(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (player == null || !player.IsValid || player.IsBot)
            return;

        if (_menuSystem == null)
        {
            player.PrintToChat("MenuSystem is not available. Make sure MenuSystemSharp plugin is loaded.");
            return;
        }

        try
        {
            var menu = _menuSystem.CreateMenu();
            menu.Title = "My Custom Menu";

            // Add menu items with callbacks
            menu.AddItem(MenuItemStyleFlags.Active | MenuItemStyleFlags.HasNumber, "Option 1", (menuInstance, selectedPlayer, itemPosition, itemOnPage, data) =>
            {
                selectedPlayer.PrintToChat($"You selected Option 1 (position: {itemPosition})");
            });

            menu.AddItem(MenuItemStyleFlags.Active | MenuItemStyleFlags.HasNumber, "Option 2", (menuInstance, selectedPlayer, itemPosition, itemOnPage, data) =>
            {
                selectedPlayer.PrintToChat("You selected Option 2");
            });

            menu.AddItem(MenuItemStyleFlags.Active | MenuItemStyleFlags.HasNumber, "Submenu", (menuInstance, selectedPlayer, itemPosition, itemOnPage, data) =>
            {
                ShowSubmenu(selectedPlayer);
            });

            menu.AddItem(MenuItemStyleFlags.Active | MenuItemStyleFlags.HasNumber, "Close Menu", (menuInstance, selectedPlayer, itemPosition, itemOnPage, data) =>
            {
                menu.Close();
                selectedPlayer.PrintToChat("Menu closed");
            });

            // Display the menu to the player
            if (menu.DisplayToPlayer(player))
            {
                Console.WriteLine($"Menu displayed to {player.PlayerName}");
            }
            else
            {
                player.PrintToChat("Failed to display menu");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating menu: {ex.Message}");
            player.PrintToChat("Error creating menu");
        }
    }

    private void ShowSubmenu(CCSPlayerController player)
    {
        if (_menuSystem == null)
        {
            player.PrintToChat("Menu system is not available");
            return;
        }

        try
        {
            // Get the default profile
            var profile = _menuSystem.GetProfile("default");
            if (profile == null)
            {
                player.PrintToChat("Default menu profile not found");
                return;
            }

            var submenu = _menuSystem.CreateMenu(profile);
            submenu.Title = "Submenu";

            submenu.AddItem(MenuItemStyleFlags.Active | MenuItemStyleFlags.HasNumber, "Submenu Option 1", (menuInstance, selectedPlayer, itemPosition, itemOnPage, data) =>
            {
                selectedPlayer.PrintToChat("You selected Submenu Option 1");
            });

            submenu.AddItem(MenuItemStyleFlags.Active | MenuItemStyleFlags.HasNumber, "Submenu Option 2", (menuInstance, selectedPlayer, itemPosition, itemOnPage, data) =>
            {
                selectedPlayer.PrintToChat("You selected Submenu Option 2");
            });

            submenu.AddItem(MenuItemStyleFlags.Active | MenuItemStyleFlags.HasNumber, "Back", (menuInstance, selectedPlayer, itemPosition, itemOnPage, data) =>
            {
                submenu.Close();
            });

            submenu.DisplayToPlayer(player);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating submenu: {ex.Message}");
            player.PrintToChat("Error creating submenu");
        }
    }
}
```

### 4. Advanced Example

```csharp
// Get a custom profile
var customProfile = _menuSystem.GetProfile("my_profile");
if (customProfile != null)
{
    var menu = _menuSystem.CreateMenu(customProfile);
    menu.Title = "Custom Menu";

    // Add items with different styles
    menu.AddItem(MenuItemStyleFlags.Active | MenuItemStyleFlags.HasNumber, "Active Item", callback);
    menu.AddItem(MenuItemStyleFlags.Disabled, "Disabled Item", callback);
    menu.AddItem(MenuItemStyleFlags.Active | MenuItemStyleFlags.Control, "Control Item", callback);

    // Set item controls (back, next, exit buttons)
    menu.ItemControls = MenuItemControlFlags.Back | MenuItemControlFlags.Next | MenuItemControlFlags.Exit;

    // Display menu with custom parameters (auto-close after 10 seconds)
    menu.DisplayToPlayer(player, startItem: 0, displayTime: 10);

    // Get active menu information
    var activeMenu = _menuSystem.GetPlayerActiveMenu(player);
    if (activeMenu != null)
    {
        Console.WriteLine($"Player has active menu: {activeMenu.Title}");
    }
}
```

## Building

```bash
# Build everything
dotnet build

# Build API package only
dotnet build MenuSystemSharp.API

# Build main plugin only
dotnet build MenuSystemSharp

# Build example plugin only
dotnet build ExamplePlugin
```

## Creating NuGet Package

```bash
# Create API package
dotnet pack MenuSystemSharp.API -c Release

# Publish package (to NuGet.org or private feed)
dotnet nuget push MenuSystemSharp.API/bin/Release/MenuSystemSharp.API.1.0.0.nupkg -s https://api.nuget.org/v3/index.json
```

## Deployment

### For Server Administrators
1. Place `MenuSystemSharp.dll` in the plugins folder
2. Place `MenuSystemSharp.API.dll` in the shared folder

### For Plugin Developers
1. Install the `MenuSystemSharp.API` NuGet package
2. Develop plugins using the API

## License

MIT License

## Contributing

Pull requests and issue reports are welcome.

## Related Links

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp)
- [Wend4r's MetaMod Menu System](https://github.com/Wend4r/mms2-menu_system)
