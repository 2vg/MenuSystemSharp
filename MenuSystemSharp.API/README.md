# MenuSystemSharp.API

MenuSystemSharp.API is a menu system API interface for CounterStrikeSharp plugins.

## Overview

This package provides an API interface for external plugins to use MenuSystemSharp plugin functionality.

## Usage

### 1. Package Installation

```xml
<PackageReference Include="MenuSystemSharp.API" Version="1.0.0" />
```

### 2. Basic Usage Example

```csharp
using MenuSystemSharp.API;

// Check if MenuSystem is available
if (!MenuSystemAPI.IsAvailable)
{
    Console.WriteLine("MenuSystem is not available");
    return;
}

// Create a menu
var menu = MenuSystemAPI.CreateMenu("My Menu");

// Add menu items
menu.AddItem("Option 1", (player, menu, itemIndex) =>
{
    player?.PrintToChat("Option 1 selected");
});

// Display the menu
MenuSystemAPI.DisplayMenu(menu, player);
```

### 3. Prerequisites

- MenuSystemSharp plugin must be installed on the server
- MenuSystemSharp plugin must be loaded before your plugin

## API Reference

### MenuSystemAPI

Static class that provides access to the menu system.

#### Properties

- `IsAvailable`: Indicates whether MenuSystem is available
- `Instance`: Gets the MenuSystemAPI instance

#### Methods

- `CreateMenu(string title)`: Creates a menu with the default profile
- `CreateMenu(string title, string profileName)`: Creates a menu with the specified profile
- `DisplayMenu(IMenuAPI menu, CCSPlayerController player, int startItem = 0, int displayTime = 0)`: Displays a menu to a player
- `CloseMenu(IMenuAPI menu)`: Closes a menu

### IMenuAPI

Interface for menu instances.

#### Methods

- `GetTitle()`: Gets the menu title
- `SetTitle(string title)`: Sets the menu title
- `AddItem(string content, MenuItemSelectAction callback, MenuItemStyleFlags style)`: Adds an item with a callback
- `AddItem(string content, MenuItemStyleFlags style)`: Adds a simple item
- `GetCurrentPosition(int playerSlot)`: Gets the current position for a player

## Notes

- Plugins using this API depend on the MenuSystemSharp plugin
- If the MenuSystemSharp plugin is not loaded, `MenuSystemAPI.IsAvailable` will return `false`
- Proper error handling should be implemented when creating or displaying menus, as exceptions may occur

## License

MIT License
