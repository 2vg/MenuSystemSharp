using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using CounterStrikeSharp.API.Modules.Memory;
using Microsoft.Extensions.Logging;
using MenuSystemSharp.API;
using System.Collections.Concurrent;
using System.Linq;

namespace MenuSystemSharp;

public interface IWrapperWithInstancePtr { IntPtr InstancePtr { get; } }

internal class MenuProfile : IMenuProfile
{
    public string Name { get; }
    public IntPtr NativePtr { get; }

    public MenuProfile(string name, IntPtr nativePtr)
    {
        Name = name;
        NativePtr = nativePtr;
    }
}

internal class MenuInstance : IMenuInstance
{
    private readonly MenuSystemImpl _menuSystem;
    private readonly ConcurrentDictionary<int, MenuItemHandler> _itemHandlers = new();
    private bool _disposed = false;

    public IntPtr NativePtr { get; }

    public MenuInstance(MenuSystemImpl menuSystem, IntPtr nativePtr)
    {
        _menuSystem = menuSystem;
        NativePtr = nativePtr;
    }

    public string Title
    {
        get
        {
            if (_disposed) throw new ObjectDisposedException(nameof(MenuInstance));
            return _menuSystem.GetMenuTitle(NativePtr);
        }
        set
        {
            if (_disposed) throw new ObjectDisposedException(nameof(MenuInstance));
            _menuSystem.SetMenuTitle(NativePtr, value);
        }
    }

    public MenuItemControlFlags ItemControls
    {
        get
        {
            if (_disposed) throw new ObjectDisposedException(nameof(MenuInstance));
            return _menuSystem.GetMenuItemControls(NativePtr);
        }
        set
        {
            if (_disposed) throw new ObjectDisposedException(nameof(MenuInstance));
            _menuSystem.SetMenuItemControls(NativePtr, value);
        }
    }

    public int AddItem(MenuItemStyleFlags styleFlags, string content, MenuItemHandler? handler = null, IntPtr data = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(MenuInstance));

        int position;
        if (handler != null)
        {
            position = _menuSystem.AddMenuItemWithHandler(NativePtr, styleFlags, content, handler, data);
            _itemHandlers[position] = handler;
        }
        else
        {
            position = _menuSystem.AddMenuItem(NativePtr, styleFlags, content, data);
        }

        return position;
    }

    public void RemoveItem(int itemPosition)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(MenuInstance));
        _menuSystem.RemoveMenuItem(NativePtr, itemPosition);
        _itemHandlers.TryRemove(itemPosition, out _);
    }

    public MenuItemStyleFlags GetItemStyles(int itemPosition)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(MenuInstance));
        return _menuSystem.GetMenuItemStyles(NativePtr, itemPosition);
    }

    public string GetItemContent(int itemPosition)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(MenuInstance));
        return _menuSystem.GetMenuItemContent(NativePtr, itemPosition);
    }

    public int GetCurrentPosition(CCSPlayerController player)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(MenuInstance));
        return _menuSystem.GetMenuCurrentPosition(NativePtr, player.Slot);
    }

    public bool DisplayToPlayer(CCSPlayerController player, int startItem = 0, int displayTime = 0)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(MenuInstance));
        return _menuSystem.DisplayMenuToPlayer(NativePtr, player.Slot, startItem, displayTime);
    }

    public bool Close()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(MenuInstance));
        return _menuSystem.CloseMenu(NativePtr);
    }

    internal MenuItemHandler? GetItemHandler(int itemPosition)
    {
        _itemHandlers.TryGetValue(itemPosition, out var handler);
        return handler;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Close();
            _itemHandlers.Clear();
            _disposed = true;
        }
    }
}

internal class MenuSystemImpl : IMenuSystem
{
    private readonly IntPtr _menuSystemPtr;
    private readonly IntPtr _profileSystemPtr;
    private readonly ConcurrentDictionary<IntPtr, MenuInstance> _menuInstances = new();
    private readonly ConcurrentDictionary<IntPtr, List<GCHandle>> _callbackHandles = new();

    // Native function delegates
    private delegate IntPtr MenuSystemDelegate();
    private delegate IntPtr MenuSystemGetProfilesDelegate(IntPtr pSystem);
    private delegate IntPtr MenuSystemCreateInstanceDelegate(IntPtr pSystem, IntPtr pProfile);
    private delegate bool MenuSystemDisplayInstanceToPlayerDelegate(IntPtr pSystem, IntPtr pMenuInstance, int aSlot, int iStartItem, int nManyTimes);
    private delegate bool MenuSystemCloseInstanceDelegate(IntPtr pSystem, IntPtr pMenuInstance);
    private delegate IntPtr MenuProfileSystemGetDelegate(IntPtr pProfileSystem, IntPtr pszName);
    private delegate IntPtr MenuGetTitleDelegate(IntPtr pMenu);
    private delegate void MenuSetTitleDelegate(IntPtr pMenu, IntPtr pszNewText);
    private delegate MenuItemStyleFlags MenuGetItemStylesDelegate(IntPtr pMenu, int iItem);
    private delegate IntPtr MenuGetItemContentDelegate(IntPtr pMenu, int iItem);
    private delegate int MenuAddItemDelegate(IntPtr pMenu, MenuItemStyleFlags eFlags, IntPtr pszContent, IntPtr pfnItemHandler, IntPtr pData);
    private delegate void MenuRemoveItemDelegate(IntPtr pMenu, int iItem);
    private delegate MenuItemControlFlags MenuGetItemControlsDelegate(IntPtr pMenu);
    private delegate void MenuSetItemControlsDelegate(IntPtr pMenu, MenuItemControlFlags eNewControls);
    private delegate int MenuGetCurrentPositionDelegate(IntPtr pMenu, int aSlot);
    private delegate IntPtr MenuGetPlayerActiveMenuDelegate(IntPtr pSystem, int aSlot);

    // C# callback support delegates
    private delegate void CSharpMenuItemHandlerDelegate(IntPtr pMenu, int aSlot, int iItem, int iItemOnPage, IntPtr pData);
    private delegate int MenuAddItemWithCSharpHandlerDelegate(IntPtr pMenu, MenuItemStyleFlags eFlags, IntPtr pszContent, CSharpMenuItemHandlerDelegate pfnCSharpHandler, IntPtr pData);
    private delegate void MenuRemoveCSharpHandlersDelegate(IntPtr pMenu);

    // Native function instances
    private readonly MenuSystemDelegate _menuSystem;
    private readonly MenuSystemGetProfilesDelegate _menuSystemGetProfiles;
    private readonly MenuSystemCreateInstanceDelegate _menuSystemCreateInstance;
    private readonly MenuSystemDisplayInstanceToPlayerDelegate _menuSystemDisplayInstanceToPlayer;
    private readonly MenuSystemCloseInstanceDelegate _menuSystemCloseInstance;
    private readonly MenuProfileSystemGetDelegate _menuProfileSystemGet;
    private readonly MenuGetTitleDelegate _menuGetTitle;
    private readonly MenuSetTitleDelegate _menuSetTitle;
    private readonly MenuGetItemStylesDelegate _menuGetItemStyles;
    private readonly MenuGetItemContentDelegate _menuGetItemContent;
    private readonly MenuAddItemDelegate _menuAddItem;
    private readonly MenuRemoveItemDelegate _menuRemoveItem;
    private readonly MenuGetItemControlsDelegate _menuGetItemControls;
    private readonly MenuSetItemControlsDelegate _menuSetItemControls;
    private readonly MenuGetCurrentPositionDelegate _menuGetCurrentPosition;
    private readonly MenuGetPlayerActiveMenuDelegate _menuGetPlayerActiveMenu;

    // C# callback support function instances
    private readonly MenuAddItemWithCSharpHandlerDelegate _menuAddItemWithCSharpHandler;
    private readonly MenuRemoveCSharpHandlersDelegate _menuRemoveCSharpHandlers;

    public bool IsAvailable => _menuSystemPtr != IntPtr.Zero;

    public MenuSystemImpl(IntPtr libraryHandle)
    {
        // Load native functions
        _menuSystem = Marshal.GetDelegateForFunctionPointer<MenuSystemDelegate>(
            NativeLibrary.GetExport(libraryHandle, "MenuSystem"));
        _menuSystemGetProfiles = Marshal.GetDelegateForFunctionPointer<MenuSystemGetProfilesDelegate>(
            NativeLibrary.GetExport(libraryHandle, "MenuSystem_GetProfiles"));
        _menuSystemCreateInstance = Marshal.GetDelegateForFunctionPointer<MenuSystemCreateInstanceDelegate>(
            NativeLibrary.GetExport(libraryHandle, "MenuSystem_CreateInstance"));
        _menuSystemDisplayInstanceToPlayer = Marshal.GetDelegateForFunctionPointer<MenuSystemDisplayInstanceToPlayerDelegate>(
            NativeLibrary.GetExport(libraryHandle, "MenuSystem_DisplayInstanceToPlayer"));
        _menuSystemCloseInstance = Marshal.GetDelegateForFunctionPointer<MenuSystemCloseInstanceDelegate>(
            NativeLibrary.GetExport(libraryHandle, "MenuSystem_CloseInstance"));
        _menuProfileSystemGet = Marshal.GetDelegateForFunctionPointer<MenuProfileSystemGetDelegate>(
            NativeLibrary.GetExport(libraryHandle, "MenuProfileSystem_Get"));
        _menuGetTitle = Marshal.GetDelegateForFunctionPointer<MenuGetTitleDelegate>(
            NativeLibrary.GetExport(libraryHandle, "Menu_GetTitle"));
        _menuSetTitle = Marshal.GetDelegateForFunctionPointer<MenuSetTitleDelegate>(
            NativeLibrary.GetExport(libraryHandle, "Menu_SetTitle"));
        _menuGetItemStyles = Marshal.GetDelegateForFunctionPointer<MenuGetItemStylesDelegate>(
            NativeLibrary.GetExport(libraryHandle, "Menu_GetItemStyles"));
        _menuGetItemContent = Marshal.GetDelegateForFunctionPointer<MenuGetItemContentDelegate>(
            NativeLibrary.GetExport(libraryHandle, "Menu_GetItemContent"));
        _menuAddItem = Marshal.GetDelegateForFunctionPointer<MenuAddItemDelegate>(
            NativeLibrary.GetExport(libraryHandle, "Menu_AddItem"));
        _menuRemoveItem = Marshal.GetDelegateForFunctionPointer<MenuRemoveItemDelegate>(
            NativeLibrary.GetExport(libraryHandle, "Menu_RemoveItem"));
        _menuGetItemControls = Marshal.GetDelegateForFunctionPointer<MenuGetItemControlsDelegate>(
            NativeLibrary.GetExport(libraryHandle, "Menu_GetItemControls"));
        _menuSetItemControls = Marshal.GetDelegateForFunctionPointer<MenuSetItemControlsDelegate>(
            NativeLibrary.GetExport(libraryHandle, "Menu_SetItemControls"));
        _menuGetCurrentPosition = Marshal.GetDelegateForFunctionPointer<MenuGetCurrentPositionDelegate>(
            NativeLibrary.GetExport(libraryHandle, "Menu_GetCurrentPosition"));
        _menuGetPlayerActiveMenu = Marshal.GetDelegateForFunctionPointer<MenuGetPlayerActiveMenuDelegate>(
            NativeLibrary.GetExport(libraryHandle, "Menu_GetPlayerActiveMenu"));

        // Load C# callback support functions
        _menuAddItemWithCSharpHandler = Marshal.GetDelegateForFunctionPointer<MenuAddItemWithCSharpHandlerDelegate>(
            NativeLibrary.GetExport(libraryHandle, "Menu_AddItemWithCSharpHandler"));
        _menuRemoveCSharpHandlers = Marshal.GetDelegateForFunctionPointer<MenuRemoveCSharpHandlersDelegate>(
            NativeLibrary.GetExport(libraryHandle, "Menu_RemoveCSharpHandlers"));

        // Initialize menu system
        _menuSystemPtr = _menuSystem();
        if (_menuSystemPtr != IntPtr.Zero)
        {
            _profileSystemPtr = _menuSystemGetProfiles(_menuSystemPtr);
        }
    }

    public IMenuProfile? GetProfile(string profileName = "default")
    {
        if (!IsAvailable) return null;

        var namePtr = Marshal.StringToHGlobalAnsi(profileName);
        try
        {
            var profilePtr = _menuProfileSystemGet(_profileSystemPtr, namePtr);
            return profilePtr != IntPtr.Zero ? new MenuProfile(profileName, profilePtr) : null;
        }
        finally
        {
            Marshal.FreeHGlobal(namePtr);
        }
    }

    public IMenuInstance CreateMenu()
    {
        var profile = GetProfile("default") ?? throw new InvalidOperationException("Default menu profile not found");
        if (!IsAvailable) throw new InvalidOperationException("Menu system is not available");

        var menuPtr = _menuSystemCreateInstance(_menuSystemPtr, profile.NativePtr);
        if (menuPtr == IntPtr.Zero)
            throw new InvalidOperationException("Failed to create menu instance");

        var menuInstance = new MenuInstance(this, menuPtr);
        _menuInstances[menuPtr] = menuInstance;
        return menuInstance;
    }

    public IMenuInstance CreateMenu(IMenuProfile profile)
    {
        if (!IsAvailable) throw new InvalidOperationException("Menu system is not available");

        var menuPtr = _menuSystemCreateInstance(_menuSystemPtr, profile.NativePtr);
        if (menuPtr == IntPtr.Zero)
            throw new InvalidOperationException("Failed to create menu instance");

        var menuInstance = new MenuInstance(this, menuPtr);
        _menuInstances[menuPtr] = menuInstance;
        return menuInstance;
    }

    public IMenuInstance? GetPlayerActiveMenu(CCSPlayerController player)
    {
        if (!IsAvailable) return null;

        var menuPtr = _menuGetPlayerActiveMenu(_menuSystemPtr, player.Slot);
        if (menuPtr == IntPtr.Zero) return null;

        _menuInstances.TryGetValue(menuPtr, out var menuInstance);
        return menuInstance;
    }

    // Internal methods for MenuInstance
    internal string GetMenuTitle(IntPtr menuPtr)
    {
        var titlePtr = _menuGetTitle(menuPtr);
        return titlePtr != IntPtr.Zero ? Marshal.PtrToStringAnsi(titlePtr) ?? "" : "";
    }

    internal void SetMenuTitle(IntPtr menuPtr, string title)
    {
        var titlePtr = Marshal.StringToHGlobalAnsi(title);
        try
        {
            _menuSetTitle(menuPtr, titlePtr);
        }
        finally
        {
            Marshal.FreeHGlobal(titlePtr);
        }
    }

    internal MenuItemControlFlags GetMenuItemControls(IntPtr menuPtr)
    {
        return _menuGetItemControls(menuPtr);
    }

    internal void SetMenuItemControls(IntPtr menuPtr, MenuItemControlFlags controls)
    {
        _menuSetItemControls(menuPtr, controls);
    }

    internal int AddMenuItem(IntPtr menuPtr, MenuItemStyleFlags styleFlags, string content, IntPtr data)
    {
        var contentPtr = Marshal.StringToHGlobalAnsi(content);
        try
        {
            return _menuAddItem(menuPtr, styleFlags, contentPtr, IntPtr.Zero, data);
        }
        finally
        {
            Marshal.FreeHGlobal(contentPtr);
        }
    }

    internal int AddMenuItemWithHandler(IntPtr menuPtr, MenuItemStyleFlags styleFlags, string content, MenuItemHandler handler, IntPtr data)
    {
        var contentPtr = Marshal.StringToHGlobalAnsi(content);
        try
        {
            // Create a native callback that will call our C# handler
            CSharpMenuItemHandlerDelegate nativeCallback = (pMenu, aSlot, iItem, iItemOnPage, pData) =>
            {
                try
                {
                    // Find the menu instance
                    if (_menuInstances.TryGetValue(pMenu, out var menuInstance))
                    {
                        // Convert slot to player controller
                        var players = Utilities.GetPlayers();
                        var player = players.FirstOrDefault(p => p.Slot == aSlot);
                        if (player != null)
                        {
                            handler(menuInstance, player, iItem, iItemOnPage, pData);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[C# DEBUG] Error in menu item handler: {ex.Message}");
                    Console.WriteLine($"[C# DEBUG] Stack trace: {ex.StackTrace}");
                }
            };

            // Pin the delegate to prevent garbage collection
            var gcHandle = GCHandle.Alloc(nativeCallback);
            if (!_callbackHandles.ContainsKey(menuPtr))
            {
                _callbackHandles[menuPtr] = new List<GCHandle>();
            }
            _callbackHandles[menuPtr].Add(gcHandle);

            var result = _menuAddItemWithCSharpHandler(menuPtr, styleFlags, contentPtr, nativeCallback, data);

            return result;
        }
        finally
        {
            Marshal.FreeHGlobal(contentPtr);
        }
    }

    internal void RemoveMenuItem(IntPtr menuPtr, int itemPosition)
    {
        _menuRemoveItem(menuPtr, itemPosition);
    }

    internal MenuItemStyleFlags GetMenuItemStyles(IntPtr menuPtr, int itemPosition)
    {
        return _menuGetItemStyles(menuPtr, itemPosition);
    }

    internal string GetMenuItemContent(IntPtr menuPtr, int itemPosition)
    {
        var contentPtr = _menuGetItemContent(menuPtr, itemPosition);
        return contentPtr != IntPtr.Zero ? Marshal.PtrToStringAnsi(contentPtr) ?? "" : "";
    }

    internal int GetMenuCurrentPosition(IntPtr menuPtr, int playerSlot)
    {
        return _menuGetCurrentPosition(menuPtr, playerSlot);
    }

    internal bool DisplayMenuToPlayer(IntPtr menuPtr, int playerSlot, int startItem, int displayTime)
    {
        return _menuSystemDisplayInstanceToPlayer(_menuSystemPtr, menuPtr, playerSlot, startItem, displayTime);
    }

    internal bool CloseMenu(IntPtr menuPtr)
    {
        // Clean up C# handlers first
        _menuRemoveCSharpHandlers(menuPtr);

        // Clean up GC handles
        if (_callbackHandles.TryRemove(menuPtr, out var handles))
        {
            foreach (var handle in handles)
            {
                if (handle.IsAllocated)
                {
                    handle.Free();
                }
            }
        }

        var result = _menuSystemCloseInstance(_menuSystemPtr, menuPtr);
        _menuInstances.TryRemove(menuPtr, out _);
        return result;
    }
}

public class MenuSystemSharp : BasePlugin
{
    private const string MENU_SYSTEM_VERSION = "Menu System v1.0.0";
    private const string MENU_LIBRARY_PATH = "/csgo/addons/menu_system/bin/menu";
    internal static string StaticModuleName => "MenuSystemSharp";
    public override string ModuleName => StaticModuleName;
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "uru";
    public override string ModuleDescription => "C# implementation for Wend4r's MetaMod Menu System";

    private static MenuSystemSharp? _instance;
    private MenuSystemImpl? _menuSystemImpl;
    private IntPtr _libraryHandle = IntPtr.Zero;

    public static MenuSystemSharp? Instance => _instance;

    // when plugin load
    public override void Load(bool hotReload)
    {
        _instance = this;

        RegisterListener<Listeners.OnMetamodAllPluginsLoaded>(() =>
        {
            try
            {
                // Try MetaFactory first
                var menuSystemPtr = Utilities.MetaFactory(MENU_SYSTEM_VERSION);
                if (menuSystemPtr.HasValue && menuSystemPtr.Value != IntPtr.Zero)
                {
                    Logger.LogInformation("Menu system found.");
                }

                // Load native library
                string menuPath = $"{Server.GameDirectory}{MENU_LIBRARY_PATH}";
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    menuPath += ".dll";
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    menuPath += ".so";

                if (File.Exists(menuPath))
                {
                    if (NativeLibrary.TryLoad(menuPath, out _libraryHandle))
                    {
                        Logger.LogDebug("Successfully loaded library '{MenuPath}'. Handle: 0x{Handle:X}", menuPath, _libraryHandle);
                    }
                    else
                    {
                        // If TryLoad fails, _nativeLibraryHandle will be IntPtr.Zero or an invalid handle.
                        // No further action like TryGetHandle as it's not available or not deemed necessary here.
                        // The existing _nativeLibraryHandle (which is likely IntPtr.Zero if TryLoad failed) will be passed to InitializeStaticExports.
                        // InitializeStaticExports already has a check for IntPtr.Zero.
                        Logger.LogError("FAILED to load library '{MenuPath}' using TryLoad. _nativeLibraryHandle is 0x{Handle:X}. Exported functions will likely not work if handle is zero", menuPath, _libraryHandle);
                    }

                    _menuSystemImpl = new MenuSystemImpl(_libraryHandle);

                    if (_menuSystemImpl.IsAvailable)
                    {
                        MenuSystem.SetInstance(_menuSystemImpl);
                        Logger.LogInformation("Menu system loaded successfully via native library");
                    }
                    else
                    {
                        Logger.LogError("Failed to initialize menu system");
                    }
                }
                else
                {
                    Logger.LogError($"Menu system library not found at: {menuPath}");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to load menu system");
            }
        });
    }

    // when plugin unload
    public override void Unload(bool hotReload)
    {
        MenuSystem.SetInstance(null);
        _menuSystemImpl = null;

        if (_libraryHandle != IntPtr.Zero)
        {
            try
            {
                NativeLibrary.Free(_libraryHandle);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to free native library");
            }
            finally
            {
                _libraryHandle = IntPtr.Zero;
            }
        }

        _instance = null;
    }
}
