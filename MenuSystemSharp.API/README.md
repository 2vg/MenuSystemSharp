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
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using MenuSystemSharp.API;

public class MyPlugin : BasePlugin
{
    [ConsoleCommand("css_testmenu", "Opens a test menu")]
    public void OnTestMenuCommand(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (player == null || !player.IsValid || player.IsBot)
            return;

        // Check if MenuSystem is available
        if (!MenuSystemAPI.IsAvailable)
        {
            player.PrintToChat("MenuSystem is not available. Make sure MenuSystemSharp plugin is loaded.");
            return;
        }

        try
        {
            // Create a menu
            var menu = MenuSystemAPI.CreateMenu("Test Menu");

            // Add menu items with callbacks
            menu.AddItem("Option 1", (selectedPlayer, selectedMenu, itemIndex) =>
            {
                selectedPlayer?.PrintToChat($"You selected Option 1 (index: {itemIndex})");
            });

            menu.AddItem("Option 2", (selectedPlayer, selectedMenu, itemIndex) =>
            {
                selectedPlayer?.PrintToChat("You selected Option 2");
            });

            menu.AddItem("Close", (selectedPlayer, selectedMenu, itemIndex) =>
            {
                MenuSystemAPI.CloseMenu(selectedMenu);
                selectedPlayer?.PrintToChat("Menu closed");
            });

            // Display the menu to the player
            if (!MenuSystemAPI.DisplayMenu(menu, player))
            {
                player.PrintToChat("Failed to display menu");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            player.PrintToChat("Error creating menu");
        }
    }
}
```

### 3. Advanced Usage Examples

```csharp
// Create menu with custom profile
var menu = MenuSystemAPI.CreateMenu("Custom Menu", "my_profile");

// Add items with different styles
menu.AddItem("Normal Item", callback, MenuItemStyleFlags.Default);
menu.AddItem("Disabled Item", callback, MenuItemStyleFlags.Disabled);
menu.AddItem("Control Item", callback, MenuItemStyleFlags.Control);

// Display with custom parameters
MenuSystemAPI.DisplayMenu(menu, player, startItem: 2, displayTime: 15);

// Check active menu
var activeMenu = MenuSystemAPI.GetActiveMenu(player);
if (activeMenu != null)
{
    Console.WriteLine($"Player has active menu: {activeMenu.GetTitle()}");
}
```

### 4. Prerequisites

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
  - **Parameters**: `title` - The menu title
  - **Returns**: `IMenuAPI` - The created menu instance
  - **Throws**: `InvalidOperationException` if MenuSystem is not available

- `CreateMenu(string title, string profileName)`: Creates a menu with the specified profile
  - **Parameters**: `title` - The menu title, `profileName` - The profile name to use
  - **Returns**: `IMenuAPI` - The created menu instance
  - **Throws**: `InvalidOperationException` if MenuSystem or profile is not available

- `DisplayMenu(IMenuAPI menu, CCSPlayerController player, int startItem = 0, int displayTime = 0)`: Displays a menu to a player
  - **Parameters**: `menu` - The menu to display, `player` - The target player, `startItem` - Starting item index (default: 0), `displayTime` - Display duration in seconds (default: 0 = unlimited)
  - **Returns**: `bool` - True if displayed successfully

- `CloseMenu(IMenuAPI menu)`: Closes the specified menu
  - **Parameters**: `menu` - The menu to close
  - **Returns**: `bool` - True if closed successfully

- `GetActiveMenuIndex(CCSPlayerController player)`: Gets the active menu index for a player
  - **Parameters**: `player` - The target player
  - **Returns**: `int` - The active menu index, or -1 if no active menu

- `GetActiveMenu(CCSPlayerController player)`: Gets the active menu instance for a player
  - **Parameters**: `player` - The target player
  - **Returns**: `IMenuAPI?` - The active menu instance, or null if no active menu

### IMenuAPI

Interface for menu instances.

#### Methods

- `GetTitle()`: Gets the current menu title
  - **Returns**: `string` - The menu title

- `SetTitle(string title)`: Sets the menu title
  - **Parameters**: `title` - The new title

- `AddItem(string content, MenuItemSelectAction callback, MenuItemStyleFlags style = MenuItemStyleFlags.Default)`: Adds an item with a callback
  - **Parameters**: `content` - The item text, `callback` - The action to invoke when selected, `style` - The item style (default: Default)
  - **Returns**: `int` - The index of the added item

- `AddItem(string content, MenuItemStyleFlags style = MenuItemStyleFlags.Default)`: Adds a simple item without callback
  - **Parameters**: `content` - The item text, `style` - The item style (default: Default)
  - **Returns**: `int` - The index of the added item

- `GetCurrentPosition(int playerSlot)`: Gets the current position for a player
  - **Parameters**: `playerSlot` - The player slot
  - **Returns**: `int` - The current position

### MenuItemSelectAction

Delegate for menu item selection callbacks.

```csharp
public delegate void MenuItemSelectAction(CCSPlayerController? player, IMenuAPI menu, int itemIndex);
```

- **Parameters**: `player` - The player who selected the item, `menu` - The menu instance, `itemIndex` - The index of the selected item

### MenuItemStyleFlags

Enum for menu item style flags.

- `Disabled = 0`: Item is disabled and cannot be selected
- `Active = 1`: Item is active and can be selected
- `HasNumber = 2`: Item displays with a number
- `Control = 4`: Item is a control item
- `Default = Active | HasNumber`: Default style (active with number)
- `Full = Default | Control`: Full style (default with control)

## Notes

- Plugins using this API depend on the MenuSystemSharp plugin
- If the MenuSystemSharp plugin is not loaded, `MenuSystemAPI.IsAvailable` will return `false`
- Proper error handling should be implemented when creating or displaying menus, as exceptions may occur

## Troubleshooting

### Common Issues

**Q: `MenuSystemAPI.IsAvailable` returns `false`**
A: Check the following:
- MenuSystemSharp plugin is properly installed and loaded
- Wend4r's mms2-menu_system MetaMod plugin is installed
- Plugin load order (MenuSystemSharp should load before your plugin)

**Q: Menu doesn't display**
A: Verify:
- Player is valid and not a bot
- `MenuSystemAPI.IsAvailable` returns `true`
- `DisplayMenu` method returns `true`
- No exceptions are thrown during menu creation

**Q: Menu callbacks don't work**
A: Ensure:
- Callback functions are properly defined
- Menu items are added with the correct callback syntax
- Player is selecting items correctly

### Best Practices

- Always check `MenuSystemAPI.IsAvailable` before using the API
- Use try-catch blocks when creating or displaying menus
- Validate player objects before displaying menus
- Handle exceptions gracefully and provide user feedback

## License

MIT License
