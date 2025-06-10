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
MenuSystemAPI.Instance.AddItem(menu, "Option 1", (player, menu, itemIndex) =>
{
    player?.PrintToChat("Option 1 selected");
});

// Display the menu
MenuSystemAPI.DisplayMenu(menu, player);
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
