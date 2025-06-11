using CounterStrikeSharp.API.Core;

namespace MenuSystemSharp.API;

/// <summary>
/// Delegate for menu item selection callbacks
/// </summary>
/// <param name="player">The player who selected the item</param>
/// <param name="menu">The menu instance</param>
/// <param name="itemIndex">The index of the selected item</param>
public delegate void MenuItemSelectAction(CCSPlayerController? player, IMenuAPI menu, int itemIndex);

/// <summary>
/// Menu item style flags
/// </summary>
[Flags]
public enum MenuItemStyleFlags : byte
{
    Disabled = 0,
    Active = 1 << 0,
    HasNumber = 1 << 1,
    Control = 1 << 2,
    Default = Active | HasNumber,
    Full = Default | Control
}

/// <summary>
/// API interface for menu instances
/// </summary>
public interface IMenuAPI
{
    /// <summary>
    /// Gets the title of the menu
    /// </summary>
    string GetTitle();

    /// <summary>
    /// Sets the title of the menu
    /// </summary>
    /// <param name="title">The new title</param>
    void SetTitle(string title);

    /// <summary>
    /// Adds a menu item with a callback action
    /// </summary>
    /// <param name="content">The item text</param>
    /// <param name="onSelectCallback">The action to invoke when selected</param>
    /// <param name="style">The item style</param>
    /// <returns>The index of the added item</returns>
    int AddItem(string content, MenuItemSelectAction onSelectCallback, MenuItemStyleFlags style = MenuItemStyleFlags.Default);

    /// <summary>
    /// Adds a simple menu item without a callback
    /// </summary>
    /// <param name="content">The item text</param>
    /// <param name="style">The item style</param>
    /// <returns>The index of the added item</returns>
    int AddItem(string content, MenuItemStyleFlags style = MenuItemStyleFlags.Default);

    /// <summary>
    /// Gets the current position for a player
    /// </summary>
    /// <param name="playerSlot">The player slot</param>
    /// <returns>The current position</returns>
    int GetCurrentPosition(int playerSlot);
}

/// <summary>
/// Main API interface for the MenuSystem
/// </summary>
public interface IMenuSystemAPI
{
    /// <summary>
    /// Checks whether the MenuSystem is available
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Creates a menu using the default profile
    /// </summary>
    /// <param name="title">The title of the menu</param>
    /// <returns>The created menu instance</returns>
    /// <exception cref="InvalidOperationException">Thrown if the MenuSystem is not available</exception>
    IMenuAPI CreateMenu(string title);

    /// <summary>
    /// Creates a menu using the specified profile
    /// </summary>
    /// <param name="title">The title of the menu</param>
    /// <param name="profileName">The name of the profile to use</param>
    /// <returns>The created menu instance</returns>
    /// <exception cref="InvalidOperationException">Thrown if the MenuSystem or specified profile is not available</exception>
    IMenuAPI CreateMenu(string title, string profileName);

    /// <summary>
    /// Displays the menu to a player
    /// </summary>
    /// <param name="menu">The menu to display</param>
    /// <param name="player">The target player</param>
    /// <param name="startItem">The starting item index (default: 0)</param>
    /// <param name="displayTime">The display duration in seconds (default: 0 = unlimited)</param>
    /// <returns>True if displayed successfully</returns>
    bool DisplayMenu(IMenuAPI menu, CCSPlayerController player, int startItem = 0, int displayTime = 0);

    /// <summary>
    /// Closes the specified menu
    /// </summary>
    /// <param name="menu">The menu to close</param>
    /// <returns>True if closed successfully</returns>
    bool CloseMenu(IMenuAPI menu);

    /// <summary>
    /// Gets the active menu index for a player
    /// </summary>
    /// <param name="player">The target player</param>
    /// <returns>The active menu index, or -1 if no active menu</returns>
    int GetActiveMenuIndex(CCSPlayerController player);

    /// <summary>
    /// Gets the active menu instance for a player
    /// </summary>
    /// <param name="player">The target player</param>
    /// <returns>The active menu instance, or null if no active menu</returns>
    IMenuAPI? GetActiveMenu(CCSPlayerController player);
}

/// <summary>
/// Static API provider for accessing the MenuSystem
/// </summary>
public static class MenuSystemAPI
{
    private static IMenuSystemAPI? _instance;

    /// <summary>
    /// Gets the current MenuSystem API instance
    /// </summary>
    public static IMenuSystemAPI? Instance => _instance;

    /// <summary>
    /// Registers the MenuSystem API implementation (called by the MenuSystemSharp plugin)
    /// </summary>
    /// <param name="implementation">The implementation to register</param>
    public static void RegisterImplementation(IMenuSystemAPI implementation)
    {
        _instance = implementation;
    }

    /// <summary>
    /// Unregisters the MenuSystem API implementation (called by the MenuSystemSharp plugin)
    /// </summary>
    public static void UnregisterImplementation()
    {
        _instance = null;
    }

    /// <summary>
    /// Checks whether the MenuSystem is available
    /// </summary>
    public static bool IsAvailable => _instance?.IsAvailable ?? false;

    /// <summary>
    /// Creates a menu using the default profile
    /// </summary>
    /// <param name="title">The title of the menu</param>
    /// <returns>The created menu instance</returns>
    /// <exception cref="InvalidOperationException">Thrown if the MenuSystem is not available</exception>
    public static IMenuAPI CreateMenu(string title)
    {
        if (_instance == null)
            throw new InvalidOperationException("MenuSystem API is not available. Make sure the MenuSystemSharp plugin is loaded.");

        return _instance.CreateMenu(title);
    }

    /// <summary>
    /// Creates a menu using the specified profile
    /// </summary>
    /// <param name="title">The title of the menu</param>
    /// <param name="profileName">The name of the profile to use</param>
    /// <returns>The created menu instance</returns>
    /// <exception cref="InvalidOperationException">Thrown if the MenuSystem or specified profile is not available</exception>
    public static IMenuAPI CreateMenu(string title, string profileName)
    {
        if (_instance == null)
            throw new InvalidOperationException("MenuSystem API is not available. Make sure the MenuSystemSharp plugin is loaded.");

        return _instance.CreateMenu(title, profileName);
    }

    /// <summary>
    /// Displays the menu to a player
    /// </summary>
    /// <param name="menu">The menu to display</param>
    /// <param name="player">The target player</param>
    /// <param name="startItem">The starting item index (default: 0)</param>
    /// <param name="displayTime">The display duration in seconds (default: 0 = unlimited)</param>
    /// <returns>True if displayed successfully</returns>
    public static bool DisplayMenu(IMenuAPI menu, CCSPlayerController player, int startItem = 0, int displayTime = 0)
    {
        return _instance?.DisplayMenu(menu, player, startItem, displayTime) ?? false;
    }

    /// <summary>
    /// Closes the specified menu
    /// </summary>
    /// <param name="menu">The menu to close</param>
    /// <returns>True if closed successfully</returns>
    public static bool CloseMenu(IMenuAPI menu)
    {
        return _instance?.CloseMenu(menu) ?? false;
    }

    /// <summary>
    /// Gets the active menu index for a player
    /// </summary>
    /// <param name="player">The target player</param>
    /// <returns>The active menu index, or -1 if no active menu</returns>
    public static int GetActiveMenuIndex(CCSPlayerController player)
    {
        return _instance?.GetActiveMenuIndex(player) ?? -1;
    }

    /// <summary>
    /// Gets the active menu instance for a player
    /// </summary>
    /// <param name="player">The target player</param>
    /// <returns>The active menu instance, or null if no active menu</returns>
    public static IMenuAPI? GetActiveMenu(CCSPlayerController player)
    {
        return _instance?.GetActiveMenu(player);
    }
}
