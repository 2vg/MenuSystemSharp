using CounterStrikeSharp.API.Core;

namespace MenuSystemSharp.API;

/// <summary>
/// Menu item style flags
/// </summary>
[Flags]
public enum MenuItemStyleFlags : byte
{
    /// <summary>
    /// Item is disabled and cannot be selected
    /// </summary>
    Disabled = 0,

    /// <summary>
    /// Item is active and can be selected
    /// </summary>
    Active = 1 << 0,

    /// <summary>
    /// Item has a number prefix
    /// </summary>
    HasNumber = 1 << 1,

    /// <summary>
    /// Item is a control item
    /// </summary>
    Control = 1 << 2
}

/// <summary>
/// Menu item control flags
/// </summary>
[Flags]
public enum MenuItemControlFlags : int
{
    /// <summary>
    /// Panel control
    /// </summary>
    Panel = 0,

    /// <summary>
    /// Back button
    /// </summary>
    Back = 1 << 0,

    /// <summary>
    /// Next button
    /// </summary>
    Next = 1 << 1,

    /// <summary>
    /// Exit button
    /// </summary>
    Exit = 1 << 2
}

/// <summary>
/// Menu item handler delegate
/// </summary>
/// <param name="menu">The menu instance</param>
/// <param name="player">The player who selected the item</param>
/// <param name="itemPosition">The position of the selected item</param>
/// <param name="itemOnPage">The position of the item on the current page</param>
/// <param name="data">Custom data associated with the item</param>
public delegate void MenuItemHandler(IMenuInstance menu, CCSPlayerController player, int itemPosition, int itemOnPage, IntPtr data);

/// <summary>
/// Interface for menu profiles
/// </summary>
public interface IMenuProfile
{
    /// <summary>
    /// Gets the profile name
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the native pointer to the profile
    /// </summary>
    IntPtr NativePtr { get; }
}

/// <summary>
/// Interface for menu instances
/// </summary>
public interface IMenuInstance : IDisposable
{
    /// <summary>
    /// Gets or sets the menu title
    /// </summary>
    string Title { get; set; }

    /// <summary>
    /// Gets the native pointer to the menu instance
    /// </summary>
    IntPtr NativePtr { get; }

    /// <summary>
    /// Gets or sets the item control flags
    /// </summary>
    MenuItemControlFlags ItemControls { get; set; }

    /// <summary>
    /// Adds an item to the menu
    /// </summary>
    /// <param name="styleFlags">Style flags for the item</param>
    /// <param name="content">The text content of the item</param>
    /// <param name="handler">Handler to call when item is selected</param>
    /// <param name="data">Custom data to associate with the item</param>
    /// <returns>The position of the added item</returns>
    int AddItem(MenuItemStyleFlags styleFlags, string content, MenuItemHandler? handler = null, IntPtr data = default);

    /// <summary>
    /// Removes an item from the menu
    /// </summary>
    /// <param name="itemPosition">The position of the item to remove</param>
    void RemoveItem(int itemPosition);

    /// <summary>
    /// Gets the style flags of an item
    /// </summary>
    /// <param name="itemPosition">The position of the item</param>
    /// <returns>The style flags of the item</returns>
    MenuItemStyleFlags GetItemStyles(int itemPosition);

    /// <summary>
    /// Gets the content of an item
    /// </summary>
    /// <param name="itemPosition">The position of the item</param>
    /// <returns>The content of the item</returns>
    string GetItemContent(int itemPosition);

    /// <summary>
    /// Gets the current position for a player
    /// </summary>
    /// <param name="player">The player</param>
    /// <returns>The current position</returns>
    int GetCurrentPosition(CCSPlayerController player);

    /// <summary>
    /// Displays the menu to a player
    /// </summary>
    /// <param name="player">The player to display the menu to</param>
    /// <param name="startItem">The starting item position</param>
    /// <param name="displayTime">How long to display the menu (0 = forever)</param>
    /// <returns>True if the menu was displayed successfully</returns>
    bool DisplayToPlayer(CCSPlayerController player, int startItem = 0, int displayTime = 0);

    /// <summary>
    /// Closes the menu
    /// </summary>
    /// <returns>True if the menu was closed successfully</returns>
    bool Close();
}

/// <summary>
/// Interface for the menu system
/// </summary>
public interface IMenuSystem
{
    /// <summary>
    /// Gets a menu profile by name
    /// </summary>
    /// <param name="profileName">The name of the profile (default: "default")</param>
    /// <returns>The menu profile, or null if not found</returns>
    IMenuProfile? GetProfile(string profileName = "default");

    /// <summary>
    /// Creates a new menu instance, using the default profile
    /// </summary>
    /// <returns>A new menu instance</returns>
    IMenuInstance CreateMenu();

    /// <summary>
    /// Creates a new menu instance
    /// </summary>
    /// <param name="profile">The profile to use for the menu</param>
    /// <returns>A new menu instance</returns>
    IMenuInstance CreateMenu(IMenuProfile profile);

    /// <summary>
    /// Gets the currently active menu for a player
    /// </summary>
    /// <param name="player">The player</param>
    /// <returns>The active menu instance, or null if no menu is active</returns>
    IMenuInstance? GetPlayerActiveMenu(CCSPlayerController player);

    /// <summary>
    /// Checks if the menu system is available
    /// </summary>
    bool IsAvailable { get; }
}

/// <summary>
/// Static accessor for the menu system
/// </summary>
public static class MenuSystem
{
    private static IMenuSystem? _instance;

    /// <summary>
    /// Gets the menu system instance
    /// </summary>
    public static IMenuSystem? Instance => _instance;

    /// <summary>
    /// Sets the menu system instance (internal use only)
    /// </summary>
    /// <param name="instance">The menu system instance</param>
    public static void SetInstance(IMenuSystem? instance)
    {
        _instance = instance;
    }
}
