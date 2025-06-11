# MenuSystemSharp

**This project is currently under active development**

## Overview

MenuSystemSharp is a library and sample plugin designed to enable the use of [Wend4r's mms2-menu_system](https://github.com/Wend4r/mms2-menu_system), a MetaMod plugin for Counter-Strike 2 (CS2), from C#.
It is intended to run on the CounterStrikeSharp framework.

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
2. Install [Wend4r's mms2-menu_system](https://github.com/Wend4r/mms2-menu_system)
3. Place the MenuSystemSharp plugin in your server's plugin folder

### 2. Using in External Plugins
1. Install the `MenuSystemSharp.API` NuGet package
2. Use the API to create and display menus

### 3. Example

```csharp
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using MenuSystemSharp.API;

[ConsoleCommand("css_mymenu", "Opens a custom menu")]
public void OnMyMenuCommand(CCSPlayerController? player, CommandInfo commandInfo)
{
    if (player == null || !player.IsValid || player.IsBot)
        return;

    if (!MenuSystemAPI.IsAvailable)
    {
        player.PrintToChat("MenuSystem is not available. Make sure MenuSystemSharp plugin is loaded.");
        return;
    }

    try
    {
        var menu = MenuSystemAPI.CreateMenu("My Custom Menu");

        menu.AddItem("Option 1", (selectedPlayer, selectedMenu, itemIndex) =>
        {
            selectedPlayer?.PrintToChat($"You selected Option 1 (index: {itemIndex})");
        });

        menu.AddItem("Option 2", (selectedPlayer, selectedMenu, itemIndex) =>
        {
            selectedPlayer?.PrintToChat("You selected Option 2");
        });

        menu.AddItem("Submenu", (selectedPlayer, selectedMenu, itemIndex) =>
        {
            if (selectedPlayer != null)
            {
                ShowSubmenu(selectedPlayer);
            }
        });

        menu.AddItem("Close Menu", (selectedPlayer, selectedMenu, itemIndex) =>
        {
            MenuSystemAPI.CloseMenu(selectedMenu);
            selectedPlayer?.PrintToChat("Menu closed");
        });

        if (MenuSystemAPI.DisplayMenu(menu, player))
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
    try
    {
        var submenu = MenuSystemAPI.CreateMenu("Submenu");

        submenu.AddItem("Submenu Option 1", (selectedPlayer, selectedMenu, itemIndex) =>
        {
            selectedPlayer?.PrintToChat("You selected Submenu Option 1");
        });

        submenu.AddItem("Submenu Option 2", (selectedPlayer, selectedMenu, itemIndex) =>
        {
            selectedPlayer?.PrintToChat("You selected Submenu Option 2");
        });

        MenuSystemAPI.DisplayMenu(submenu, player);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error creating submenu: {ex.Message}");
        player.PrintToChat("Error creating submenu");
    }
}
```

### 4. Advanced Example

```csharp
// Create a menu with custom profile
var menu = MenuSystemAPI.CreateMenu("Custom Menu", "my_profile");

// Add items with different styles
menu.AddItem("Active Item", callback, MenuItemStyleFlags.Active);
menu.AddItem("Disabled Item", callback, MenuItemStyleFlags.Disabled);
menu.AddItem("Control Item", callback, MenuItemStyleFlags.Control);

// Display menu with custom parameters (auto-close after 10 seconds)
MenuSystemAPI.DisplayMenu(menu, player, startItem: 0, displayTime: 10);

// Get active menu information
int activeMenuIndex = MenuSystemAPI.GetActiveMenuIndex(player);
var activeMenu = MenuSystemAPI.GetActiveMenu(player);
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
