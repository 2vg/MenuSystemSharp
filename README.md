# MenuSystemSharp

**This project is currently under active development**

## Overview

MenuSystemSharp is a library and sample plugin designed to enable the use of [Wend4r's mms2-menu_system](https://github.com/Wend4r/mms2-menu_system), a MetaMod plugin for Counter-Strike 2 (CS2), from C#.
It is intended to run on the CounterStrikeSharp framework.

## Purpose

The primary goal of this project is to make the powerful C++ based menu system easily accessible to plugin developers who use C#.
This will allow for more efficient development of in-game menus with complex interactions in C#.

## Key Features

*   **C# Interface Definitions**: Provides C# interfaces corresponding to the original C++ interfaces (`IMenuSystem`, `IMenuProfileSystem`, `IMenu`, etc.).
*   **Wrapper Classes**: Wraps C++ instance pointers, allowing safe and intuitive method calls from C#.
    *   `MenuSystemSharpWrapper`
    *   `MenuProfileSystemSharpWrapper`
    *   `MenuSharpWrapper`
    *   `MenuProfileSharpWrapper`
*   **Native Function Calls**: Utilizes `NativeLibrary` to call core menu system functionalities (like adding items, setting titles) as C functions.
*   **Callback Handling**: Enables C# implementation of menu item selection callbacks using C# delegates and the `[UnmanagedCallersOnly]` attribute.
*   **Test Command**: The `css_csmenu_test` command allows testing basic menu creation, display, and item interaction.

## Required

*  [roflmuffin/CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp).
*  [Wend4r/mms2-menu_system](https://github.com/Wend4r/mms2-menu_system)

## Build

There is no release for now, sorry.
Just clone the repository, then please build it.

## Config

Please see: [Wend4r/mms2-menu_system](https://github.com/Wend4r/mms2-menu_system)

## Usage (Conceptual)

Below is a conceptual code example of how to create and display a basic menu using MenuSystemSharp.
For detailed API, please refer to [`MenuSystemSharp/MenuSystemSharp.cs`](MenuSystemSharp/MenuSystemSharp.cs:0) and the associated wrapper classes.
This plugin code comes with test menus and commands.
It cannot be used as is, so please edit it as necessary.

```csharp
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;

public class MyAwesomePlugin : BasePlugin
{
    private IMenuSystemSharp? _menuSystem;

    public override void Load(bool hotReload)
    {
        // Obtain an instance of MenuSystemSharp (see MenuSystemCSharp.cs for actual implementation)
        // _menuSystem = ...; 
    }

    public void ShowMyMenu(CCSPlayerController player)
    {
        if (_menuSystem == null || player == null || !player.IsValid) return;

        // Get the default profile (or create a custom one)
        IMenuProfileSharp? defaultProfile = _menuSystem.GetProfiles()?.GetProfile("default");
        if (defaultProfile == null)
        {
            player.PrintToChat("Default menu profile not found.");
            return;
        }

        // Create a menu instance
        IMenuSharp menu = _menuSystem.CreateInstance(defaultProfile, null); // Second argument is IMenuHandlerSharp (optional)

        menu.SetTitle("My C# Menu");
        menu.AddItem("Option 1", (selectedPlayer, menuInstance, itemIndex) =>
        {
            selectedPlayer?.PrintToChat($"You selected Option 1 from '{menuInstance.GetTitle()}'!");
        });
        menu.AddItem("Option 2 (No Action)", MenuItemStyleFlags.Active); // Style can be specified
        menu.AddItem("Exit", MenuItemStyleFlags.Control | MenuItemStyleFlags.HasNumber);

        // Display the menu to the player
        _menuSystem.DisplayInstanceToPlayer(menu, player.Slot);
    }
}
```

## Special Thanks

* [Wend4r](https://github.com/Wend4r)
