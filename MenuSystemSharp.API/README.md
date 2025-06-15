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

    [ConsoleCommand("css_testmenu", "Opens a test menu")]
    public void OnTestMenuCommand(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (player == null || !player.IsValid || player.IsBot)
            return;

        // Check if MenuSystem is available
        if (_menuSystem == null)
        {
            player.PrintToChat("MenuSystem is not available. Make sure MenuSystemSharp plugin is loaded.");
            return;
        }

        try
        {
            var menu = _menuSystem.CreateMenu();
            menu.Title = "Test Menu";

            // Add menu items with callbacks
            menu.AddItem(MenuItemStyleFlags.Active | MenuItemStyleFlags.HasNumber, "Option 1", (menuInstance, selectedPlayer, itemPosition, itemOnPage, data) =>
            {
                selectedPlayer.PrintToChat($"You selected Option 1 (position: {itemPosition})");
            });

            menu.AddItem(MenuItemStyleFlags.Active | MenuItemStyleFlags.HasNumber, "Option 2", (menuInstance, selectedPlayer, itemPosition, itemOnPage, data) =>
            {
                selectedPlayer.PrintToChat("You selected Option 2");
            });

            menu.AddItem(MenuItemStyleFlags.Active | MenuItemStyleFlags.HasNumber, "Close", (menuInstance, selectedPlayer, itemPosition, itemOnPage, data) =>
            {
                menu.Close();
                selectedPlayer.PrintToChat("Menu closed");
            });

            // Display the menu to the player
            if (!menu.DisplayToPlayer(player))
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
// Get a custom profile
var customProfile = _menuSystem.GetProfile("my_profile");
if (customProfile != null)
{
    // Create menu with custom profile
    var menu = _menuSystem.CreateMenu(customProfile);
    menu.Title = "Custom Menu";

    // Add items with different styles
    menu.AddItem(MenuItemStyleFlags.Active | MenuItemStyleFlags.HasNumber, "Normal Item", callback);
    menu.AddItem(MenuItemStyleFlags.Disabled, "Disabled Item", callback);
    menu.AddItem(MenuItemStyleFlags.Active | MenuItemStyleFlags.Control, "Control Item", callback);

    // Set item controls
    menu.ItemControls = MenuItemControlFlags.Back | MenuItemControlFlags.Next | MenuItemControlFlags.Exit;

    // Display with custom parameters
    menu.DisplayToPlayer(player, startItem: 2, displayTime: 15);

    // Check active menu
    var activeMenu = _menuSystem.GetPlayerActiveMenu(player);
    if (activeMenu != null)
    {
        Console.WriteLine($"Player has active menu: {activeMenu.Title}");
    }
}
```

### 4. Prerequisites

- MenuSystemSharp plugin must be installed on the server
- MenuSystemSharp plugin must be loaded before your plugin

## API Reference

### MenuSystem

Static class that provides access to the menu system instance.

#### Properties

- `Instance`: Gets the menu system instance (`IMenuSystem?`)

#### Methods

- `SetInstance(IMenuSystem? instance)`: Sets the menu system instance (internal use only)

### IMenuSystem

Interface for the menu system.

#### Properties

- `IsAvailable`: Indicates whether the menu system is available

#### Methods

- `GetProfile(string profileName = "default")`: Gets a menu profile by name
  - **Parameters**: `profileName` - The name of the profile (default: "default")
  - **Returns**: `IMenuProfile?` - The menu profile, or null if not found

-  `CreateMenu()`: Creates a new menu instance, using the default profile
  - **Returns**: `IMenuInstance` - A new menu instance

- `CreateMenu(IMenuProfile profile)`: Creates a new menu instance
  - **Parameters**: `profile` - The profile to use for the menu
  - **Returns**: `IMenuInstance` - A new menu instance

- `GetPlayerActiveMenu(CCSPlayerController player)`: Gets the currently active menu for a player
  - **Parameters**: `player` - The player
  - **Returns**: `IMenuInstance?` - The active menu instance, or null if no menu is active

### IMenuProfile

Interface for menu profiles.

#### Properties

- `Name`: Gets the profile name
- `NativePtr`: Gets the native pointer to the profile

### IMenuInstance

Interface for menu instances.

#### Properties

- `Title`: Gets or sets the menu title
- `NativePtr`: Gets the native pointer to the menu instance
- `ItemControls`: Gets or sets the item control flags

#### Methods

- `AddItem(MenuItemStyleFlags styleFlags, string content, MenuItemHandler? handler = null, IntPtr data = default)`: Adds an item to the menu
  - **Parameters**: `styleFlags` - Style flags for the item, `content` - The text content of the item, `handler` - Handler to call when item is selected, `data` - Custom data to associate with the item
  - **Returns**: `int` - The position of the added item

- `RemoveItem(int itemPosition)`: Removes an item from the menu
  - **Parameters**: `itemPosition` - The position of the item to remove

- `GetItemStyles(int itemPosition)`: Gets the style flags of an item
  - **Parameters**: `itemPosition` - The position of the item
  - **Returns**: `MenuItemStyleFlags` - The style flags of the item

- `GetItemContent(int itemPosition)`: Gets the content of an item
  - **Parameters**: `itemPosition` - The position of the item
  - **Returns**: `string` - The content of the item

- `GetCurrentPosition(CCSPlayerController player)`: Gets the current position for a player
  - **Parameters**: `player` - The player
  - **Returns**: `int` - The current position

- `DisplayToPlayer(CCSPlayerController player, int startItem = 0, int displayTime = 0)`: Displays the menu to a player
  - **Parameters**: `player` - The player to display the menu to, `startItem` - The starting item position, `displayTime` - How long to display the menu (0 = forever)
  - **Returns**: `bool` - True if the menu was displayed successfully

- `Close()`: Closes the menu
  - **Returns**: `bool` - True if the menu was closed successfully

- `Dispose()`: Disposes the menu instance

### MenuItemHandler

Delegate for menu item handlers.

```csharp
public delegate void MenuItemHandler(IMenuInstance menu, CCSPlayerController player, int itemPosition, int itemOnPage, IntPtr data);
```

- **Parameters**: `menu` - The menu instance, `player` - The player who selected the item, `itemPosition` - The position of the selected item, `itemOnPage` - The position of the item on the current page, `data` - Custom data associated with the item

### MenuItemStyleFlags

Enum for menu item style flags.

- `Disabled = 0`: Item is disabled and cannot be selected
- `Active = 1`: Item is active and can be selected
- `HasNumber = 2`: Item has a number prefix
- `Control = 4`: Item is a control item

### MenuItemControlFlags

Enum for menu item control flags.

- `Panel = 0`: Panel control
- `Back = 1`: Back button
- `Next = 2`: Next button
- `Exit = 4`: Exit button

## Notes

- Plugins using this API depend on the MenuSystemSharp plugin
- If the MenuSystemSharp plugin is not loaded, `MenuSystem.Instance` will be `null`
- Proper error handling should be implemented when creating or displaying menus, as exceptions may occur
- Menu instances implement `IDisposable` and should be properly disposed when no longer needed

## Troubleshooting

### Common Issues

**Q: `MenuSystem.Instance` is `null`**
A: Check the following:
- MenuSystemSharp plugin is properly installed and loaded
- Wend4r's mms2-menu_system MetaMod plugin is installed
- Plugin load order (MenuSystemSharp should load before your plugin)
- Use `OnAllPluginsLoaded` event to ensure MenuSystemSharp is loaded

**Q: Menu doesn't display**
A: Verify:
- Player is valid and not a bot
- `MenuSystem.Instance` is not null
- Menu profile exists and is valid
- `DisplayToPlayer` method returns `true`
- No exceptions are thrown during menu creation

**Q: Menu callbacks don't work**
A: Ensure:
- Callback functions are properly defined with correct signature
- Menu items are added with the correct handler syntax
- Player is selecting items correctly

**Q: Profile not found**
A: Check:
- Profile name is correct (default profile is "default")
- MenuSystemSharp plugin configuration includes the required profiles

### Best Practices

- Always check `MenuSystem.Instance` is not null before using the API
- Use `OnAllPluginsLoaded` event to initialize menu system reference
- Use try-catch blocks when creating or displaying menus
- Validate player objects before displaying menus
- Handle exceptions gracefully and provide user feedback
- Dispose menu instances when no longer needed

## License

MIT License
