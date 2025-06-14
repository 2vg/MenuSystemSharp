using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using CounterStrikeSharp.API.Modules.Memory;
using Microsoft.Extensions.Logging;
using MenuSystemSharp.API;
using CounterStrikeSharp.API.Core.Attributes.Registration;

namespace MenuSystemSharp;

public interface IWrapperWithInstancePtr { IntPtr InstancePtr { get; } }

public class MenuSystemCSharp : BasePlugin
{
    private const string MENU_SYSTEM_VERSION = "Menu System v1.0.0";
    private const string MENU_LIBRARY_PATH = "/csgo/addons/menu_system/bin/menu";
    internal static string StaticModuleName => "MenuSystemCSharp";
    public override string ModuleName => StaticModuleName;
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "uru";
    public override string ModuleDescription => "C# implementation for Wend4r's MetaMod Menu System";

    private IMenuSystem? _menuSystemInstance;
    private static IntPtr _nativeLibraryHandle = IntPtr.Zero;

    private void LoadMenuLibrary(IntPtr menuSystemPtr)
    {
        try
        {
            string menuPath = $"{Server.GameDirectory}{MENU_LIBRARY_PATH}";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) menuPath += ".dll";
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) menuPath += ".so";

            if (!string.IsNullOrEmpty(menuPath))
            {
                Logger.LogDebug("Attempting to load native library: {MenuPath}", menuPath);
                if (NativeLibrary.TryLoad(menuPath, out _nativeLibraryHandle))
                {
                    Logger.LogDebug("Successfully loaded library '{MenuPath}'. Handle: 0x{Handle:X}", menuPath, _nativeLibraryHandle);
                }
                else
                {
                    // If TryLoad fails, _nativeLibraryHandle will be IntPtr.Zero or an invalid handle.
                    // No further action like TryGetHandle as it's not available or not deemed necessary here.
                    // The existing _nativeLibraryHandle (which is likely IntPtr.Zero if TryLoad failed) will be passed to InitializeStaticExports.
                    // InitializeStaticExports already has a check for IntPtr.Zero.
                    Logger.LogError("FAILED to load library '{MenuPath}' using TryLoad. _nativeLibraryHandle is 0x{Handle:X}. Exported functions will likely not work if handle is zero", menuPath, _nativeLibraryHandle);
                }
            }
            else
            {
                Logger.LogError("OS platform not supported for determining native library name, or menuPath is empty. _nativeLibraryHandle remains IntPtr.Zero. Exported functions will likely not work");
            }

            MenuWrapper.InitializeStaticExports(_nativeLibraryHandle);
            _menuSystemInstance = new MenuSystemWrapper(menuSystemPtr);
            Logger.LogDebug("Wrappers initialized");

            Logger.LogDebug("Plugin loaded successfully");
        }
        catch (Exception ex) { Logger.LogError(ex, "Exception during load"); }
    }

    public static IMenuSystem? GetMenuSystemInstance()
    {
        return Instance?._menuSystemInstance;
    }

    public static MenuSystemCSharp? Instance { get; private set; }

    public override void Load(bool hotReload)
    {
        Instance = this;

        RegisterListener<Listeners.OnMetamodAllPluginsLoaded>(() =>
        {
            IntPtr? menuSystemPtr = Utilities.MetaFactory(MENU_SYSTEM_VERSION);
            if (!menuSystemPtr.HasValue || menuSystemPtr.Value == IntPtr.Zero)
            {
                Logger.LogError("ERROR: {MenuSystemVersion} not found", MENU_SYSTEM_VERSION);
                return;
            }
            Logger.LogDebug("{MenuSystemVersion} found: 0x{Pointer:X}", MENU_SYSTEM_VERSION, menuSystemPtr.Value);

            LoadMenuLibrary(menuSystemPtr.Value);

            // Register API implementation
            var apiImplementation = new MenuSystemAPIImplementation();
            MenuSystemAPI.RegisterImplementation(apiImplementation);
            Logger.LogDebug("MenuSystem API implementation registered");
        });
    }

    [GameEventHandler]
    public HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid) return HookResult.Continue;

        var menu = _menuSystemInstance?.GetActiveMenu(player.Slot);
        while (menu != null)
        {
            _menuSystemInstance?.CloseInstance(menu);
            // Get the next active menu, if any
            menu = _menuSystemInstance?.GetActiveMenu(player.Slot);
        }

        return HookResult.Continue;
    }

    public override void Unload(bool hotReload)
    {
        // Unregister API implementation
        MenuSystemAPI.UnregisterImplementation();
        Logger.LogDebug("MenuSystem API implementation unregistered");

        Instance = null;
        _nativeLibraryHandle = IntPtr.Zero;
        if (MenuWrapper._staticNativeCallbackDelegateHandle.IsAllocated)
        {
            MenuWrapper._staticNativeCallbackDelegateHandle.Free();
        }
    }
}

public interface IMenuSystem
{
    IntPtr GetPlayer(int playerSlot);
    IMenuProfileSystem GetProfiles();
    IMenu CreateInstance(IMenuProfile? profile, IMenuHandler? handler);
    bool DisplayInstanceToPlayer(IMenu menu, int playerSlot, int startItem = 0, int displayTime = 0);
    bool CloseInstance(IMenu menu);

    int GetActiveMenuIndex(int playerSlot);
    IMenu? GetActiveMenu(int playerSlot);
}

public interface IMenuProfileSystem
{
    IMenuProfile? GetProfile(string name = "default");
    void AddOrReplaceProfile(string name, IntPtr profileDataPtr);
    IntPtr GetEntityKeyValuesAllocator();
}

public class MenuProfileSystemWrapper : IMenuProfileSystem
{
    internal readonly IntPtr InstancePtr;

    private readonly Func<IntPtr, string, IntPtr> _getProfileFunction;
    private readonly Action<IntPtr, string, IntPtr> _addOrReplaceProfileFunction;
    private readonly Func<IntPtr, IntPtr> _getEntityKeyValuesAllocatorFunction;

    public MenuProfileSystemWrapper(IntPtr instancePtr)
    {
        InstancePtr = instancePtr;
        if (InstancePtr == IntPtr.Zero)
        {
            throw new ArgumentNullException(nameof(instancePtr), "MenuProfileSystem instance pointer cannot be null.");
        }

        _getProfileFunction = VirtualFunction.Create<IntPtr, string, IntPtr>(InstancePtr, 0);
        _addOrReplaceProfileFunction = VirtualFunction.CreateVoid<IntPtr, string, IntPtr>(InstancePtr, 1);
        _getEntityKeyValuesAllocatorFunction = VirtualFunction.Create<IntPtr, IntPtr>(InstancePtr, 2);
    }

    public IMenuProfile? GetProfile(string name = "default")
    {
        IntPtr profilePtr = _getProfileFunction.Invoke(InstancePtr, name);
        if (profilePtr == IntPtr.Zero)
            return null;

        return new MenuProfileWrapper(profilePtr);
    }

    public void AddOrReplaceProfile(string name, IntPtr profileDataPtr)
    {
        _addOrReplaceProfileFunction.Invoke(InstancePtr, name, profileDataPtr);
    }

    public IntPtr GetEntityKeyValuesAllocator()
    {
        return _getEntityKeyValuesAllocatorFunction.Invoke(InstancePtr);
    }
}

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

public delegate void MenuItemSelectAction(CCSPlayerController? player, IMenu menu, int itemIndex);

public interface IMenu : IWrapperWithInstancePtr
{
    IMenuProfile? GetProfile();
    bool ApplyProfile(int playerSlot, IMenuProfile profile);
    IMenuHandler? GetHandler();
    string GetTitle();
    void SetTitle(string title);
    int AddItem(string content, MenuItemStyleFlags style = MenuItemStyleFlags.Default, IntPtr itemHandler = default, IntPtr data = default);
    int AddItem(string content, MenuItemSelectAction onSelectCallback, MenuItemStyleFlags style = MenuItemStyleFlags.Default);
    int GetCurrentPosition(int playerSlot);
}

internal class MenuItemCallbackContext(MenuItemSelectAction callback, IMenu menuInstance)
{
    public MenuItemSelectAction Callback { get; } = callback;
    public IMenu MenuInstance { get; } = menuInstance;
}

public class MenuWrapper : IMenu, IDisposable
{
    public IntPtr InstancePtr { get; private set; }
    private readonly List<GCHandle> _allocatedHandles = new();
    private bool _disposed = false;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int MenuAddItemDelegate(IntPtr menuPtr, MenuItemStyleFlags flags, string content, IntPtr itemHandler, IntPtr data);
    private static MenuAddItemDelegate? _menuAddItemNative;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate IntPtr MenuGetTitleDelegate(IntPtr menuPtr);
    private static MenuGetTitleDelegate? _menuGetTitleNative;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void MenuSetTitleDelegate(IntPtr menuPtr, string newTitle);
    private static MenuSetTitleDelegate? _menuSetTitleNative;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate IntPtr MenuGetPlayerActiveMenuDelegate(IntPtr menuSystemPtr, int playerSlot);
    internal static MenuGetPlayerActiveMenuDelegate? MenuGetPlayerActiveMenuNative { get; private set; }

    private static bool _exportsInitialized = false;

    public static void InitializeStaticExports(IntPtr libraryHandle)
    {
        if (_exportsInitialized)
            return;

        _exportsInitialized = true;

        if (libraryHandle == IntPtr.Zero)
        {
            MenuSystemCSharp.Instance?.Logger.LogError("Invalid library handle. Exports not loaded");
            _menuAddItemNative = null;
            _menuGetTitleNative = null;
            _menuSetTitleNative = null;
            return;
        }

        LoadMenuAddItemExport(libraryHandle);
        LoadMenuGetTitleExport(libraryHandle);
        LoadMenuSetTitleExport(libraryHandle);
        LoadMenuGetPlayerActiveMenuExport(libraryHandle);
    }

    private static void LoadMenuAddItemExport(IntPtr libraryHandle)
    {
        try
        {
            _menuAddItemNative = NativeLibrary.GetExport(libraryHandle, "Menu_AddItem").MarshalTo<MenuAddItemDelegate>();
            MenuSystemCSharp.Instance?.Logger.LogDebug("Menu_AddItem delegate initialized");
        }
        catch (Exception ex)
        {
            MenuSystemCSharp.Instance?.Logger.LogError(ex, "Error loading Menu_AddItem");
            _menuAddItemNative = null;
        }
    }

    private static void LoadMenuGetTitleExport(IntPtr libraryHandle)
    {
        try
        {
            _menuGetTitleNative = NativeLibrary.GetExport(libraryHandle, "Menu_GetTitle").MarshalTo<MenuGetTitleDelegate>();
            MenuSystemCSharp.Instance?.Logger.LogDebug("Menu_GetTitle delegate initialized");
        }
        catch (Exception ex)
        {
            MenuSystemCSharp.Instance?.Logger.LogError(ex, "Error loading Menu_GetTitle");
            _menuGetTitleNative = null;
        }
    }

    private static void LoadMenuSetTitleExport(IntPtr libraryHandle)
    {
        try
        {
            _menuSetTitleNative = NativeLibrary.GetExport(libraryHandle, "Menu_SetTitle").MarshalTo<MenuSetTitleDelegate>();
            MenuSystemCSharp.Instance?.Logger.LogDebug("Menu_SetTitle delegate initialized");
        }
        catch (Exception ex)
        {
            MenuSystemCSharp.Instance?.Logger.LogError(ex, "Error loading Menu_SetTitle");
            _menuSetTitleNative = null;
        }
    }

    private static void LoadMenuGetPlayerActiveMenuExport(IntPtr libraryHandle)
    {
        try
        {
            MenuGetPlayerActiveMenuNative = NativeLibrary.GetExport(libraryHandle, "Menu_GetPlayerActiveMenu").MarshalTo<MenuGetPlayerActiveMenuDelegate>();
            MenuSystemCSharp.Instance?.Logger.LogDebug("Menu_GetPlayerActiveMenu delegate initialized");
        }
        catch (Exception ex)
        {
            MenuSystemCSharp.Instance?.Logger.LogError(ex, "Error loading Menu_GetPlayerActiveMenu");
            MenuGetPlayerActiveMenuNative = null;
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void OnNativeMenuItemSelectCallback(IntPtr menuInstance, int playerSlot, int itemIndex, int itemOnPage, IntPtr callbackContextHandle)
    {
        GCHandle contextHandle = GCHandle.FromIntPtr(callbackContextHandle);
        try
        {
            if (!contextHandle.IsAllocated || contextHandle.Target is not MenuItemCallbackContext context)
                return;

            CCSPlayerController? player = Utilities.GetPlayerFromSlot(playerSlot);
            context.Callback(player, context.MenuInstance, itemIndex);
        }
        catch (Exception ex)
        {
            MenuSystemCSharp.Instance?.Logger.LogError(ex, "Error in OnNativeMenuItemSelectCallback");
        }
        // NOTE: GCHandle is no longer freed here - it will be freed when the menu is disposed
        // This allows the callback to be invoked multiple times
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void NativeItemSelectCallbackDelegate(IntPtr menuInstance, int playerSlot, int itemIndex, int itemOnPage, IntPtr callbackContextHandle);

    private static readonly IntPtr _staticNativeCallbackPointer;
    internal static GCHandle _staticNativeCallbackDelegateHandle;

    static MenuWrapper()
    {
        unsafe
        {
            _staticNativeCallbackPointer = (IntPtr)(delegate* unmanaged[Cdecl]<IntPtr, int, int, int, IntPtr, void>)&OnNativeMenuItemSelectCallback;
        }
    }

    private readonly Func<IntPtr, IntPtr> _getProfileFunction;
    private readonly Func<IntPtr, int, IntPtr, bool> _applyProfileFunction;
    private readonly Func<IntPtr, IntPtr> _getHandlerFunction;
    private readonly Func<IntPtr, int, int> _getCurrentPositionFunction;

    public MenuWrapper(IntPtr instancePtr)
    {
        InstancePtr = instancePtr;
        if (InstancePtr == IntPtr.Zero)
            throw new ArgumentNullException(nameof(instancePtr));

        _getProfileFunction = VirtualFunction.Create<IntPtr, IntPtr>(InstancePtr, 0);
        _applyProfileFunction = VirtualFunction.Create<IntPtr, int, IntPtr, bool>(InstancePtr, 1);
        _getHandlerFunction = VirtualFunction.Create<IntPtr, IntPtr>(InstancePtr, 2);
        _getCurrentPositionFunction = VirtualFunction.Create<IntPtr, int, int>(InstancePtr, 9);
    }

    public IMenuProfile? GetProfile()
    {
        IntPtr profilePtr = _getProfileFunction.Invoke(InstancePtr);
        if (profilePtr == IntPtr.Zero)
            return null;

        return new MenuProfileWrapper(profilePtr);
    }

    public bool ApplyProfile(int playerSlot, IMenuProfile profile)
    {
        IntPtr profilePtr = (profile as IWrapperWithInstancePtr)?.InstancePtr ?? IntPtr.Zero;
        return _applyProfileFunction.Invoke(InstancePtr, playerSlot, profilePtr);
    }

    public IMenuHandler? GetHandler()
    {
        IntPtr handlerPtr = _getHandlerFunction.Invoke(InstancePtr);
        if (handlerPtr == IntPtr.Zero)
            return null;

        throw new NotImplementedException("Menu.GetHandler needs MenuHandlerWrapper and callback strategy.");
    }

    public string GetTitle()
    {
        if (_menuGetTitleNative == null) throw new InvalidOperationException("Menu_GetTitle (native) not initialized.");
        IntPtr titlePtr = _menuGetTitleNative(InstancePtr);
        return Marshal.PtrToStringUTF8(titlePtr) ?? string.Empty;
    }

    public void SetTitle(string title)
    {
        if (_menuSetTitleNative == null) throw new InvalidOperationException("Menu_SetTitle (native) not initialized.");
        _menuSetTitleNative(InstancePtr, title);
    }

    public int AddItem(string content, MenuItemStyleFlags style = MenuItemStyleFlags.Default, IntPtr pfnItemHandler = default, IntPtr pData = default)
    {
        if (_menuAddItemNative == null) throw new InvalidOperationException("Menu_AddItem (native) not initialized.");
        return _menuAddItemNative(InstancePtr, style, content, pfnItemHandler, pData);
    }

    public int AddItem(string content, MenuItemSelectAction onSelectCallback, MenuItemStyleFlags style = MenuItemStyleFlags.Default)
    {
        if (_menuAddItemNative == null) throw new InvalidOperationException("Menu_AddItem (native) not initialized.");
        var context = new MenuItemCallbackContext(onSelectCallback, this);
        GCHandle contextHandle = GCHandle.Alloc(context, GCHandleType.Normal);

        _allocatedHandles.Add(contextHandle);

        IntPtr pContextGCHandle = GCHandle.ToIntPtr(contextHandle);
        int itemId = _menuAddItemNative(InstancePtr, style, content, _staticNativeCallbackPointer, pContextGCHandle);

        if (itemId < 0)
        {
            _allocatedHandles.Remove(contextHandle);
            if (contextHandle.IsAllocated)
                contextHandle.Free();
        }

        return itemId;
    }

    public int GetCurrentPosition(int playerSlot)
    {
        return _getCurrentPositionFunction.Invoke(InstancePtr, playerSlot);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                FreeAllocatedHandles();
            }
            _disposed = true;
        }
    }

    ~MenuWrapper()
    {
        Dispose(false);
    }

    private void FreeAllocatedHandles()
    {
        foreach (var handle in _allocatedHandles)
        {
            if (handle.IsAllocated)
            {
                handle.Free();
            }
        }
        _allocatedHandles.Clear();
    }
}

public interface IMenuProfile : IWrapperWithInstancePtr
{
    // string GetDisplayName();
    // string GetDescription();
}

public class MenuProfileWrapper : IMenuProfile
{
    public IntPtr InstancePtr { get; private set; }
    // private readonly Func<IntPtr, IntPtr> _getDisplayNameFunction;
    // private readonly Func<IntPtr, IntPtr> _getDescriptionFunction;

    public MenuProfileWrapper(IntPtr instancePtr)
    {
        InstancePtr = instancePtr;
        if (InstancePtr == IntPtr.Zero)
            throw new ArgumentNullException(nameof(instancePtr));

        // IMenuProfile vtable:
        // 0: GetDisplayName() const CUtlString&
        // 1: GetDescription() const CUtlString&
        // _getDisplayNameFunction = VirtualFunction.Create<IntPtr, IntPtr>(InstancePtr, 0);
        // _getDescriptionFunction = VirtualFunction.Create<IntPtr, IntPtr>(InstancePtr, 1);
    }

    /*
    public string GetDisplayName()
    {
        Console.WriteLine($"[{MenuSystemCSharp.StaticModuleName}] MenuProfileWrapper.GetDisplayName() called. Temporarily returning fixed string.");
        return "[DisplayName Temporarily Disabled]";
    }

    public string GetDescription()
    {
        Console.WriteLine($"[{MenuSystemCSharp.StaticModuleName}] MenuProfileWrapper.GetDescription() called. Temporarily returning fixed string.");
        return "[Description Temporarily Disabled]";
    }
    */
}

// TODO: Implement IMenuHandler
public interface IMenuHandler { /* ... */ }

public class MenuSystemWrapper : IMenuSystem
{
    private readonly IntPtr _instancePtr;
    private readonly Func<IntPtr, int, IntPtr> _getPlayerFunction;
    private readonly Func<IntPtr, IntPtr> _getProfilesSystemFunction;
    private readonly Func<IntPtr, IntPtr, IntPtr, IntPtr> _createInstanceFunction;
    private readonly Func<IntPtr, IntPtr, int, int, int, bool> _displayInstanceToPlayerFunction;
    private readonly Func<IntPtr, IntPtr, bool> _closeInstanceFunction;

    public MenuSystemWrapper(IntPtr instancePtr)
    {
        _instancePtr = instancePtr;
        if (_instancePtr == IntPtr.Zero)
            throw new ArgumentNullException(nameof(instancePtr));

        _getPlayerFunction = VirtualFunction.Create<IntPtr, int, IntPtr>(_instancePtr, 10);
        _getProfilesSystemFunction = VirtualFunction.Create<IntPtr, IntPtr>(_instancePtr, 11);
        _createInstanceFunction = VirtualFunction.Create<IntPtr, IntPtr, IntPtr, IntPtr>(_instancePtr, 12);
        _displayInstanceToPlayerFunction = VirtualFunction.Create<IntPtr, IntPtr, int, int, int, bool>(_instancePtr, 13);
        _closeInstanceFunction = VirtualFunction.Create<IntPtr, IntPtr, bool>(_instancePtr, 14);
    }

    public IntPtr GetPlayer(int playerSlot) => _getPlayerFunction.Invoke(_instancePtr, playerSlot);

    public IMenuProfileSystem GetProfiles()
    {
        IntPtr profileSystemPtr = _getProfilesSystemFunction.Invoke(_instancePtr);
        if (profileSystemPtr == IntPtr.Zero)
            throw new InvalidOperationException("GetProfiles returned null.");

        return new MenuProfileSystemWrapper(profileSystemPtr);
    }

    public IMenu CreateInstance(IMenuProfile? profile, IMenuHandler? handler)
    {
        IntPtr profilePtr = (profile as IWrapperWithInstancePtr)?.InstancePtr ?? IntPtr.Zero;
        IntPtr handlerPtr = (handler as IWrapperWithInstancePtr)?.InstancePtr ?? IntPtr.Zero;
        IntPtr menuPtr = _createInstanceFunction.Invoke(_instancePtr, profilePtr, handlerPtr);

        if (menuPtr == IntPtr.Zero)
            throw new InvalidOperationException("Native CreateInstance returned null.");

        return new MenuWrapper(menuPtr);
    }

    public bool DisplayInstanceToPlayer(IMenu menu, int playerSlot, int startItem = 0, int displayTime = 0)
    {
        IntPtr menuPtr = (menu as IWrapperWithInstancePtr)?.InstancePtr ?? IntPtr.Zero;
        if (menuPtr == IntPtr.Zero && menu != null)
            return false;

        return _displayInstanceToPlayerFunction.Invoke(_instancePtr, menuPtr, playerSlot, startItem, displayTime);
    }

    public bool CloseInstance(IMenu menu)
    {
        IntPtr menuPtr = (menu as IWrapperWithInstancePtr)?.InstancePtr ?? IntPtr.Zero;
        if (menuPtr == IntPtr.Zero && menu != null)
            return false;

        bool result = _closeInstanceFunction.Invoke(_instancePtr, menuPtr);

        // Dispose the menu wrapper to free GCHandles
        if (result && menu is IDisposable disposableMenu)
        {
            disposableMenu.Dispose();
        }

        return result;
    }

    public int GetActiveMenuIndex(int playerSlot)
    {
        IntPtr playerPtr = _getPlayerFunction.Invoke(_instancePtr, playerSlot);
        if (playerPtr == IntPtr.Zero)
            return -1; // MENU_INVALID_INDEX

        var getActiveMenuIndexFunction = VirtualFunction.Create<IntPtr, int>(playerPtr, 0);
        return getActiveMenuIndexFunction.Invoke(playerPtr);
    }

    public IMenu? GetActiveMenu(int playerSlot)
    {
        if (MenuWrapper.MenuGetPlayerActiveMenuNative == null)
            throw new InvalidOperationException("Menu_GetPlayerActiveMenu (native) not initialized.");

        IntPtr menuInstancePtr = MenuWrapper.MenuGetPlayerActiveMenuNative(_instancePtr, playerSlot);

        if (menuInstancePtr == IntPtr.Zero)
            return null;

        return new MenuWrapper(menuInstancePtr);
    }
}
// Extension method for NativeLibrary.GetExport to simplify marshalling
internal static class NativeLibraryExtensions
{
    public static T MarshalTo<T>(this IntPtr funcPtr) where T : Delegate
    {
        if (funcPtr == IntPtr.Zero)
        {
            throw new ArgumentNullException(nameof(funcPtr), $"Function pointer for {typeof(T).Name} is null.");
        }
        return Marshal.GetDelegateForFunctionPointer<T>(funcPtr);
    }
}

/// <summary>
/// Helper class to simplify the use of the MenuSystem from external plugins
/// </summary>
public static class MenuSystemHelper
{
    /// <summary>
    /// Checks whether the MenuSystem is available
    /// </summary>
    public static bool IsAvailable => MenuSystemCSharp.GetMenuSystemInstance() != null;

    /// <summary>
    /// Creates a menu using the default profile
    /// </summary>
    /// <param name="title">The title of the menu</param>
    /// <returns>The created menu instance</returns>
    /// <exception cref="InvalidOperationException">Thrown if the MenuSystem is not available</exception>
    public static IMenu CreateMenu(string title)
    {
        return CreateMenu(title, "default");
    }

    /// <summary>
    /// Creates a menu using the specified profile
    /// </summary>
    /// <param name="title">The title of the menu</param>
    /// <param name="profileName">The name of the profile to use</param>
    /// <returns>The created menu instance</returns>
    /// <exception cref="InvalidOperationException">Thrown if the MenuSystem or specified profile is not available</exception>
    public static IMenu CreateMenu(string title, string profileName)
    {
        var menuSystem = MenuSystemCSharp.GetMenuSystemInstance() ?? throw new InvalidOperationException("MenuSystem is not available. Make sure the MenuSystemCSharp plugin is loaded.");
        var profileSystem = menuSystem.GetProfiles();
        var profile = profileSystem.GetProfile(profileName) ?? throw new InvalidOperationException($"Menu profile '{profileName}' not found.");
        var menu = menuSystem.CreateInstance(profile, null);
        menu.SetTitle(title);
        return menu;
    }

    /// <summary>
    /// Displays the menu to a player
    /// </summary>
    /// <param name="menu">The menu to display</param>
    /// <param name="player">The target player</param>
    /// <param name="startItem">The starting item index (default: 0)</param>
    /// <param name="displayTime">The display duration in seconds (default: 0 = unlimited)</param>
    /// <returns>True if displayed successfully</returns>
    public static bool DisplayMenu(IMenu menu, CCSPlayerController player, int startItem = 0, int displayTime = 0)
    {
        var menuSystem = MenuSystemCSharp.GetMenuSystemInstance();
        if (menuSystem == null || player == null || !player.IsValid || player.IsBot)
            return false;

        return menuSystem.DisplayInstanceToPlayer(menu, player.Slot, startItem, displayTime);
    }

    /// <summary>
    /// Closes the specified menu
    /// </summary>
    /// <param name="menu">The menu to close</param>
    /// <returns>True if closed successfully</returns>
    public static bool CloseMenu(IMenu menu)
    {
        var menuSystem = MenuSystemCSharp.GetMenuSystemInstance();
        if (menuSystem == null)
            return false;

        return menuSystem.CloseInstance(menu);
    }

    /// <summary>
    /// Adds a menu item with a callback action
    /// </summary>
    /// <param name="menu">The target menu</param>
    /// <param name="text">The item text</param>
    /// <param name="callback">The action to invoke when selected</param>
    /// <param name="style">The item style (default: Default)</param>
    /// <returns>The index of the added item</returns>
    public static int AddItem(IMenu menu, string text, MenuItemSelectAction callback, MenuItemStyleFlags style = MenuItemStyleFlags.Default)
    {
        return menu.AddItem(text, callback, style);
    }

    /// <summary>
    /// Adds a simple menu item without a callback
    /// </summary>
    /// <param name="menu">The target menu</param>
    /// <param name="text">The item text</param>
    /// <param name="style">The item style (default: Default)</param>
    /// <returns>The index of the added item</returns>
    public static int AddItem(IMenu menu, string text, MenuItemStyleFlags style = MenuItemStyleFlags.Default)
    {
        return menu.AddItem(text, style);
    }

    /// <summary>
    /// Retrieves the available profile system (for debugging)
    /// </summary>
    /// <returns>The menu profile system instance, or null if not available</returns>
    public static IMenuProfileSystem? GetProfileSystem()
    {
        var menuSystem = MenuSystemCSharp.GetMenuSystemInstance();
        return menuSystem?.GetProfiles();
    }
}

/// <summary>
/// Implementation of IMenuAPI that wraps the internal IMenu interface
/// </summary>
internal class MenuAPIWrapper(IMenu internalMenu) : IMenuAPI
{
    private readonly IMenu _internalMenu = internalMenu ?? throw new ArgumentNullException(nameof(internalMenu));

    public string GetTitle()
    {
        return _internalMenu.GetTitle();
    }

    public void SetTitle(string title)
    {
        _internalMenu.SetTitle(title);
    }

    public int AddItem(string content, API.MenuItemSelectAction onSelectCallback, API.MenuItemStyleFlags style = API.MenuItemStyleFlags.Default)
    {
        // Convert API callback to internal callback
        void internalCallback(CCSPlayerController? player, IMenu menu, int itemIndex)
        {
            var apiMenu = new MenuAPIWrapper(menu);
            onSelectCallback(player, apiMenu, itemIndex);
        }

        // Convert API style flags to internal style flags
        var internalStyle = (MenuItemStyleFlags)style;

        return _internalMenu.AddItem(content, internalCallback, internalStyle);
    }

    public int AddItem(string content, API.MenuItemStyleFlags style = API.MenuItemStyleFlags.Default)
    {
        // Convert API style flags to internal style flags
        var internalStyle = (MenuItemStyleFlags)style;

        return _internalMenu.AddItem(content, internalStyle);
    }

    public int GetCurrentPosition(int playerSlot)
    {
        return _internalMenu.GetCurrentPosition(playerSlot);
    }

    // Internal access for the implementation
    internal IMenu InternalMenu => _internalMenu;
}

/// <summary>
/// Implementation of IMenuSystemAPI that provides the API interface
/// </summary>
internal class MenuSystemAPIImplementation : IMenuSystemAPI
{
    public bool IsAvailable => MenuSystemCSharp.GetMenuSystemInstance() != null;

    public IMenuAPI CreateMenu(string title)
    {
        return CreateMenu(title, "default");
    }

    public IMenuAPI CreateMenu(string title, string profileName)
    {
        var menuSystem = MenuSystemCSharp.GetMenuSystemInstance()
            ?? throw new InvalidOperationException("MenuSystem is not available. Make sure the MenuSystemCSharp plugin is loaded.");

        var profileSystem = menuSystem.GetProfiles();
        var profile = profileSystem.GetProfile(profileName)
            ?? throw new InvalidOperationException($"Menu profile '{profileName}' not found.");

        var menu = menuSystem.CreateInstance(profile, null);
        menu.SetTitle(title);

        return new MenuAPIWrapper(menu);
    }

    public bool DisplayMenu(IMenuAPI menu, CCSPlayerController player, int startItem = 0, int displayTime = 0)
    {
        var menuSystem = MenuSystemCSharp.GetMenuSystemInstance();
        if (menuSystem == null || player == null || !player.IsValid || player.IsBot)
            return false;

        // Extract the internal menu from the API wrapper
        if (menu is MenuAPIWrapper wrapper)
        {
            return menuSystem.DisplayInstanceToPlayer(wrapper.InternalMenu, player.Slot, startItem, displayTime);
        }

        return false;
    }

    public bool CloseMenu(IMenuAPI menu)
    {
        var menuSystem = MenuSystemCSharp.GetMenuSystemInstance();
        if (menuSystem == null)
            return false;

        // Extract the internal menu from the API wrapper
        if (menu is MenuAPIWrapper wrapper)
        {
            return menuSystem.CloseInstance(wrapper.InternalMenu);
        }

        return false;
    }

    public int GetActiveMenuIndex(CCSPlayerController player)
    {
        var menuSystem = MenuSystemCSharp.GetMenuSystemInstance();
        if (menuSystem == null || player == null || !player.IsValid || player.IsBot)
            return -1;

        return menuSystem.GetActiveMenuIndex(player.Slot);
    }

    public IMenuAPI? GetActiveMenu(CCSPlayerController player)
    {
        var menuSystem = MenuSystemCSharp.GetMenuSystemInstance();
        if (menuSystem == null || player == null || !player.IsValid || player.IsBot)
            return null;

        var activeMenu = menuSystem.GetActiveMenu(player.Slot);
        if (activeMenu == null)
            return null;

        return new MenuAPIWrapper(activeMenu);
    }
}
