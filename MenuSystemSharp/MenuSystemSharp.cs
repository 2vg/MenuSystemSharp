using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using CounterStrikeSharp.API.Modules.Memory;

public interface IWrapperWithInstancePtr { IntPtr InstancePtr { get; } }

public class MenuSystemCSharp : BasePlugin
{
    internal static string StaticModuleName => "MenuSystemCSharp";
    public override string ModuleName => StaticModuleName;
    public override string ModuleVersion => "0.0.1";
    public override string ModuleAuthor => "uru";
    public override string ModuleDescription => "C# implementation for Wend4r's MetaMod Menu System";

    private IMenuSystemSharp? _menuSystemInstance;
    private static IntPtr _nativeLibraryHandle = IntPtr.Zero;

    public override void Load(bool hotReload)
    {
        Console.WriteLine($"[{ModuleName}] Plugin loading...");
        try
        {
            IntPtr? pMenuSystemNullable = Utilities.MetaFactory("Menu System v1.0.0");
            if (!pMenuSystemNullable.HasValue || pMenuSystemNullable.Value == IntPtr.Zero)
            {
                Server.PrintToConsole($"[{ModuleName}] ERROR: Menu System v1.0.0 not found.");
                return;
            }
            Console.WriteLine($"[{ModuleName}] Menu System v1.0.0 found: 0x{pMenuSystemNullable.Value:X}.");
            IntPtr pMenuSystem = pMenuSystemNullable.Value;

            string menuPath = $"{Server.GameDirectory}/csgo/addons/menu_system/bin/menu";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) menuPath += ".dll";
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) menuPath += ".so";

            if (!string.IsNullOrEmpty(menuPath))
            {
                Console.WriteLine($"[{ModuleName}] Attempting to load native library: {menuPath}");
                if (NativeLibrary.TryLoad(menuPath, out _nativeLibraryHandle))
                {
                    Console.WriteLine($"[{ModuleName}] Successfully loaded library '{menuPath}'. Handle: 0x{_nativeLibraryHandle:X}.");
                }
                else
                {
                    // If TryLoad fails, _nativeLibraryHandle will be IntPtr.Zero or an invalid handle.
                    // No further action like TryGetHandle as it's not available or not deemed necessary here.
                    // The existing _nativeLibraryHandle (which is likely IntPtr.Zero if TryLoad failed) will be passed to InitializeStaticExports.
                    // InitializeStaticExports already has a check for IntPtr.Zero.
                    Console.WriteLine($"[{ModuleName}] FAILED to load library '{menuPath}' using TryLoad. _nativeLibraryHandle is 0x{_nativeLibraryHandle:X}. Exported functions will likely not work if handle is zero.");
                }
            }
            else
            {
                Console.WriteLine($"[{ModuleName}] OS platform not supported for determining native library name, or menuPath is empty. _nativeLibraryHandle remains IntPtr.Zero. Exported functions will likely not work.");
            }

            MenuSharpWrapper.InitializeStaticExports(_nativeLibraryHandle);
            _menuSystemInstance = new MenuSystemSharpWrapper(pMenuSystem);
            Console.WriteLine($"[{ModuleName}] Wrappers initialized.");

            RegisterCommands();
            Console.WriteLine($"[{ModuleName}] Plugin loaded successfully.");
        }
        catch (Exception ex) { Server.PrintToConsole($"[{ModuleName}] Exception during load: {ex}"); }
    }

    private void RegisterCommands()
    {
        AddCommand("css_csmenu_test", "Test C# Menu System", OnTestMenuCommand);
    }

    private void OnTestMenuCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid || player.IsBot)
        {
            player?.PrintToChat("Invalid player for menu test.");
            return;
        }
        if (_menuSystemInstance == null)
        {
            player.PrintToChat("MenuSystem (C#) is not available.");
            return;
        }
        try
        {
            player.PrintToChat("Creating C# menu...");

            IMenuProfileSystemSharp? profileSystem = _menuSystemInstance.GetProfiles();
            if (profileSystem == null)
            {
                player.PrintToChat("MenuProfileSystem is not available.");
                Server.PrintToConsole($"[{ModuleName}] MenuProfileSystem is null.");
                return;
            }

            IMenuProfileSharp? defaultProfile = profileSystem.GetProfile("default");
            if (defaultProfile == null)
            {
                player.PrintToChat("Default menu profile ('default') not found.");
                Server.PrintToConsole($"[{ModuleName}] Default menu profile ('default') not found.");

                // TODO: fallback to a default profile or implement something
                // Menu should be created with a something profile, otherwise it will crash because of null profile C++ side
                return;
            }
            Console.WriteLine($"[{ModuleName}] Default profile instance: {defaultProfile.InstancePtr:X}");

            IMenuSharp testMenu = _menuSystemInstance.CreateInstance(defaultProfile, null);
            Console.WriteLine($"[{ModuleName}] Test menu created with profile: {testMenu.InstancePtr:X}");

            // check if the menu profile is applied correctly
            // but this is crashing absolutely, because of the C++ <-> C# interop problem
            /*
            Console.WriteLine($"[{ModuleName}] Attempting to call testMenu.GetProfile(). Menu InstancePtr: {testMenu.InstancePtr:X}");
            IMenuProfileSharp? appliedProfile = null;
            try
            {
                // here is crashing point.
                appliedProfile = testMenu.GetProfile();
                Console.WriteLine($"[{ModuleName}] testMenu.GetProfile() call completed. Returned profilePtr: {(appliedProfile == null ? "null" : appliedProfile.InstancePtr.ToString("X"))}");

                if (appliedProfile != null)
                {
                    Console.WriteLine($"[{ModuleName}] Applied profile to menu: {appliedProfile.InstancePtr:X}. Attempting to get DisplayName...");
                    // also crashing here
                    string displayName = appliedProfile.GetDisplayName();
                    Console.WriteLine($"[{ModuleName}] Profile DisplayName: {displayName}");
                }
                else
                {
                    Console.WriteLine($"[{ModuleName}] No profile applied to menu after creation or GetProfile returned null.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{ModuleName}] Exception during GetProfile() or processing its result: {ex}");
            }
            */

            testMenu.SetTitle("C# Test Menu!");
            Console.WriteLine($"[{ModuleName}] Successfully called SetTitle. Next is GetTitle.");
            player.PrintToChat($"Menu title set to: {testMenu.GetTitle()}");

            testMenu.AddItem("Item 1 (Callback)", (selectedPlayer, menuInstance, itemIndex) =>
            {
                selectedPlayer?.PrintToChat($"Item 1 selected on menu '{menuInstance.GetTitle()}'! Index: {itemIndex}");
                Console.WriteLine($"Player {selectedPlayer?.PlayerName} selected Item 1 (Index {itemIndex}) on menu {menuInstance.GetTitle()}");
            });
            testMenu.AddItem("Item 2 (No Callback)", MenuItemStyleFlags.Active);
            testMenu.AddItem("Exit", MenuItemStyleFlags.Control | MenuItemStyleFlags.HasNumber);
            player.PrintToChat("Items added. Displaying menu...");

            if (_menuSystemInstance.DisplayInstanceToPlayer(testMenu, player.Slot))
                player.PrintToChat("Test menu displayed!");
            else
                player.PrintToChat("Failed to display test menu.");
        }
        catch (Exception ex)
        {
            player.PrintToChat($"Error during menu test: {ex.Message}");
            Server.PrintToConsole($"[{ModuleName}] Exception in OnTestMenuCommand: {ex}");
        }
    }

    public override void Unload(bool hotReload)
    {
        Console.WriteLine($"[{ModuleName}] Plugin unloading...");
        // If _nativeLibraryHandle was loaded by this plugin using NativeLibrary.Load, it should be freed.
        // Since we are using TryGetHandle or relying on it being pre-loaded, we don't explicitly free it here.
        _nativeLibraryHandle = IntPtr.Zero;
        // Free the GCHandle for the static callback delegate
        // Ensure the GCHandle is allocated before trying to free it.
        // Note: The GCHandle's role needs re-evaluation with UnmanagedCallersOnly.
        // If _staticNativeCallbackDelegateHandle was for _staticNativeCallbackDelegate and that delegate is no longer pinned this way,
        // this might need adjustment or removal.
        // For now, we assume it might still be used for context or other purposes if it's properly initialized.
        // However, the original _staticNativeCallbackDelegateHandle was static and GCHandle.Alloc(_staticNativeCallbackDelegate)
        // which is problematic if _staticNativeCallbackDelegate = OnNativeMenuItemSelectCallback (CS8902).
        // If _pfnStaticNativeCallback is used directly, _staticNativeCallbackDelegateHandle might be for something else or unused.
        // Let's assume it's still relevant and just check IsAllocated.
        // Only attempt to free if it was ever allocated and is still allocated.
        // The logic for _staticNativeCallbackDelegateHandle needs to be consistent.
        if (MenuSharpWrapper._staticNativeCallbackDelegateHandle.IsAllocated)
        {
            MenuSharpWrapper._staticNativeCallbackDelegateHandle.Free();
        }
    }
}

public interface IMenuSystemSharp
{
    IntPtr GetPlayer(int playerSlot);
    IMenuProfileSystemSharp GetProfiles();
    IMenuSharp CreateInstance(IMenuProfileSharp? profile, IMenuHandlerSharp? handler);
    bool DisplayInstanceToPlayer(IMenuSharp menu, int playerSlot, int startItem = 0, int displayTime = 0);
    bool CloseInstance(IMenuSharp menu);
}

public interface IMenuProfileSystemSharp
{
    IMenuProfileSharp? GetProfile(string name = "default");
    void AddOrReplaceProfile(string name, IntPtr profileDataPtr);
    IntPtr GetEntityKeyValuesAllocator();
}

public class MenuProfileSystemSharpWrapper : IMenuProfileSystemSharp
{
    internal readonly IntPtr InstancePtr;

    private readonly Func<IntPtr, string, IntPtr> _getProfile;
    private readonly Action<IntPtr, string, IntPtr> _addOrReplaceProfile;
    private readonly Func<IntPtr, IntPtr> _getEntityKeyValuesAllocator;

    public MenuProfileSystemSharpWrapper(IntPtr instancePtr)
    {
        InstancePtr = instancePtr;
        if (InstancePtr == IntPtr.Zero)
        {
            throw new ArgumentNullException(nameof(instancePtr), "MenuProfileSystem instance pointer cannot be null.");
        }
        _getProfile = VirtualFunction.Create<IntPtr, string, IntPtr>(InstancePtr, 0);
        _addOrReplaceProfile = VirtualFunction.CreateVoid<IntPtr, string, IntPtr>(InstancePtr, 1);
        _getEntityKeyValuesAllocator = VirtualFunction.Create<IntPtr, IntPtr>(InstancePtr, 2);
    }

    public IMenuProfileSharp? GetProfile(string name = "default")
    {
        IntPtr profilePtr = _getProfile.Invoke(InstancePtr, name);
        if (profilePtr == IntPtr.Zero) return null;
        return new MenuProfileSharpWrapper(profilePtr);
    }

    public void AddOrReplaceProfile(string name, IntPtr profileDataPtr)
    {
        _addOrReplaceProfile.Invoke(InstancePtr, name, profileDataPtr);
    }

    public IntPtr GetEntityKeyValuesAllocator()
    {
        return _getEntityKeyValuesAllocator.Invoke(InstancePtr);
    }
}

[Flags]
public enum MenuItemStyleFlags : byte
{
    Active = 1 << 0,
    HasNumber = 1 << 1,
    Control = 1 << 2,
    Default = Active | HasNumber,
    Full = Default | Control
}

public delegate void MenuItemSelectAction(CCSPlayerController? player, IMenuSharp menu, int itemIndex);

public interface IMenuSharp : IWrapperWithInstancePtr
{
    IMenuProfileSharp? GetProfile();
    bool ApplyProfile(int playerSlot, IMenuProfileSharp profile);
    IMenuHandlerSharp? GetHandler();
    string GetTitle();
    void SetTitle(string title);
    int AddItem(string content, MenuItemStyleFlags style = MenuItemStyleFlags.Default, IntPtr pfnItemHandler = default, IntPtr pData = default);
    int AddItem(string content, MenuItemSelectAction onSelectCallback, MenuItemStyleFlags style = MenuItemStyleFlags.Default);
    int GetCurrentPosition(int playerSlot);
}

internal class MenuItemCallbackContext
{
    public MenuItemSelectAction Callback { get; }
    public IMenuSharp MenuInstance { get; }
    public MenuItemCallbackContext(MenuItemSelectAction callback, IMenuSharp menuInstance)
    {
        Callback = callback;
        MenuInstance = menuInstance;
    }
}

public class MenuSharpWrapper : IMenuSharp
{
    public IntPtr InstancePtr { get; private set; }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int MenuAddItemDelegateNative(IntPtr pMenu, MenuItemStyleFlags eFlags, string pszContent, IntPtr pfnItemHandler, IntPtr pData);
    private static MenuAddItemDelegateNative? _menuAddItemNative;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate IntPtr MenuGetTitleDelegateNative(IntPtr pMenu);
    private static MenuGetTitleDelegateNative? _menuGetTitleNative;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void MenuSetTitleDelegateNative(IntPtr pMenu, string pszNewText);
    private static MenuSetTitleDelegateNative? _menuSetTitleNative;

    private static bool _exportsInitialized = false;

    public static void InitializeStaticExports(IntPtr libraryHandle)
    {
        if (_exportsInitialized) return;
        _exportsInitialized = true;

        if (libraryHandle == IntPtr.Zero)
        {
            Console.WriteLine("[MenuSharpWrapper.InitializeStaticExports] Invalid library handle. Exports not loaded.");
            _menuAddItemNative = null;
            _menuGetTitleNative = null;
            _menuSetTitleNative = null;
            return;
        }

        try
        {
            _menuAddItemNative = NativeLibrary.GetExport(libraryHandle, "Menu_AddItem").MarshalTo<MenuAddItemDelegateNative>();
            Console.WriteLine("[MenuSharpWrapper.InitializeStaticExports] Menu_AddItem delegate initialized.");
        }
        catch (Exception ex) { Console.WriteLine($"[MenuSharpWrapper.InitializeStaticExports] Error loading Menu_AddItem: {ex.Message}"); _menuAddItemNative = null; }

        try
        {
            _menuGetTitleNative = NativeLibrary.GetExport(libraryHandle, "Menu_GetTitle").MarshalTo<MenuGetTitleDelegateNative>();
            Console.WriteLine("[MenuSharpWrapper.InitializeStaticExports] Menu_GetTitle delegate initialized.");
        }
        catch (Exception ex) { Console.WriteLine($"[MenuSharpWrapper.InitializeStaticExports] Error loading Menu_GetTitle: {ex.Message}"); _menuGetTitleNative = null; }

        try
        {
            _menuSetTitleNative = NativeLibrary.GetExport(libraryHandle, "Menu_SetTitle").MarshalTo<MenuSetTitleDelegateNative>();
            Console.WriteLine("[MenuSharpWrapper.InitializeStaticExports] Menu_SetTitle delegate initialized.");
        }
        catch (Exception ex) { Console.WriteLine($"[MenuSharpWrapper.InitializeStaticExports] Error loading Menu_SetTitle: {ex.Message}"); _menuSetTitleNative = null; }
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void OnNativeMenuItemSelectCallback(IntPtr pMenuActualInstance, int playerSlot, int itemIndex, int itemOnPage, IntPtr pCallbackContextGCHandle)
    {
        GCHandle contextHandle = GCHandle.FromIntPtr(pCallbackContextGCHandle);
        try
        {
            if (!contextHandle.IsAllocated || !(contextHandle.Target is MenuItemCallbackContext context)) return;
            CCSPlayerController? player = Utilities.GetPlayerFromSlot(playerSlot);
            context.Callback(player, context.MenuInstance, itemIndex);
        }
        catch (Exception ex) { Console.WriteLine($"[OnNativeMenuItemSelectCallback] Error: {ex}"); }
        finally { if (contextHandle.IsAllocated) contextHandle.Free(); }
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void NativeItemSelectCallbackUnmanagedDel(IntPtr pMenuInstance, int playerSlot, int itemIndex, int itemOnPage, IntPtr pCallbackContextGCHandle);
    // OnNativeMenuItemSelectCallback is UnmanagedCallersOnly, so we get its function pointer directly.
    // No GCHandle needed for the delegate itself if we are using function pointers from UnmanagedCallersOnly methods,
    // as they are not GC-managed in the same way typical delegates are.
    // However, the GCHandle was likely for the _staticNativeCallbackDelegate instance if it were a normal delegate.
    // Since we are now aiming to use the function pointer directly, the original GCHandle's purpose might change or become obsolete
    // depending on how pData (context) is managed for the callback.
    // For now, we assume the GCHandle was for the delegate, which is no longer needed in this exact form.
    // private static readonly NativeItemSelectCallbackUnmanagedDel _staticNativeCallbackDelegate = OnNativeMenuItemSelectCallback; // CS8902: This is incorrect for UnmanagedCallersOnly.
    // The function pointer is obtained below.
    private static readonly IntPtr _pfnStaticNativeCallback;
    internal static GCHandle _staticNativeCallbackDelegateHandle; // Its role needs to be clear. If it was for _staticNativeCallbackDelegate, it might be obsolete.

    static MenuSharpWrapper() // Static constructor to initialize static readonly fields that require unsafe context
    {
        unsafe
        {
            _pfnStaticNativeCallback = (IntPtr)(delegate* unmanaged[Cdecl]<IntPtr, int, int, int, IntPtr, void>)&OnNativeMenuItemSelectCallback;
        }
        // Initialize _staticNativeCallbackDelegateHandle if it's still needed for some GCHandle pinning logic,
        // though its original purpose (pinning _staticNativeCallbackDelegate) is gone.
        // For now, let's assume it's not initialized here unless a clear purpose is defined.
    }

    private readonly Func<IntPtr, IntPtr> _getProfileIMenu;
    private readonly Func<IntPtr, int, IntPtr, bool> _applyProfile;
    private readonly Func<IntPtr, IntPtr> _getHandler;
    // GetTitleRef is not used directly for GetTitle/SetTitle if using exported C functions
    // private readonly Func<IntPtr, IntPtr> _getTitleRef;
    private readonly Func<IntPtr, int, int> _getCurrentPosition;

    public MenuSharpWrapper(IntPtr instancePtr)
    {
        InstancePtr = instancePtr;
        if (InstancePtr == IntPtr.Zero) throw new ArgumentNullException(nameof(instancePtr));
        _getProfileIMenu = VirtualFunction.Create<IntPtr, IntPtr>(InstancePtr, 0);
        _applyProfile = VirtualFunction.Create<IntPtr, int, IntPtr, bool>(InstancePtr, 1);
        _getHandler = VirtualFunction.Create<IntPtr, IntPtr>(InstancePtr, 2);
        // _getTitleRef = VirtualFunction.Create<IntPtr, IntPtr>(InstancePtr, 6); // Not using if relying on exported Menu_Get/SetTitle
        _getCurrentPosition = VirtualFunction.Create<IntPtr, int, int>(InstancePtr, 9);
    }

    public IMenuProfileSharp? GetProfile()
    {
        IntPtr profilePtr = _getProfileIMenu.Invoke(InstancePtr);
        if (profilePtr == IntPtr.Zero) return null;
        return new MenuProfileSharpWrapper(profilePtr);
    }

    public bool ApplyProfile(int playerSlot, IMenuProfileSharp profile)
    {
        IntPtr pProfile = (profile as IWrapperWithInstancePtr)?.InstancePtr ?? IntPtr.Zero;
        return _applyProfile.Invoke(InstancePtr, playerSlot, pProfile);
    }

    public IMenuHandlerSharp? GetHandler()
    {
        IntPtr handlerPtr = _getHandler.Invoke(InstancePtr);
        if (handlerPtr == IntPtr.Zero) return null;
        throw new NotImplementedException("MenuSharp.GetHandler needs MenuHandlerSharpWrapper and callback strategy.");
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
        IntPtr pContextGCHandle = GCHandle.ToIntPtr(contextHandle);
        int itemId = _menuAddItemNative(InstancePtr, style, content, _pfnStaticNativeCallback, pContextGCHandle);
        if (itemId < 0) { if (contextHandle.IsAllocated) contextHandle.Free(); }
        return itemId;
    }

    public int GetCurrentPosition(int playerSlot)
    {
        return _getCurrentPosition.Invoke(InstancePtr, playerSlot);
    }
}

public interface IMenuProfileSharp : IWrapperWithInstancePtr
{
    string GetDisplayName();
    string GetDescription();
}

public class MenuProfileSharpWrapper : IMenuProfileSharp
{
    public IntPtr InstancePtr { get; private set; }
    private readonly Func<IntPtr, IntPtr> _getDisplayName;
    private readonly Func<IntPtr, IntPtr> _getDescription;

    public MenuProfileSharpWrapper(IntPtr instancePtr)
    {
        InstancePtr = instancePtr;
        if (InstancePtr == IntPtr.Zero) throw new ArgumentNullException(nameof(instancePtr));
        // Assuming IMenuProfile vtable:
        // 0: GetDisplayName() const CUtlString&
        // 1: GetDescription() const CUtlString&
        _getDisplayName = VirtualFunction.Create<IntPtr, IntPtr>(InstancePtr, 0);
        _getDescription = VirtualFunction.Create<IntPtr, IntPtr>(InstancePtr, 1);
    }

    public string GetDisplayName()
    {
        Console.WriteLine($"[{MenuSystemCSharp.StaticModuleName}] MenuProfileSharpWrapper.GetDisplayName() called. Temporarily returning fixed string.");
        return "[DisplayName Temporarily Disabled]";
    }

    public string GetDescription()
    {
        Console.WriteLine($"[{MenuSystemCSharp.StaticModuleName}] MenuProfileSharpWrapper.GetDescription() called. Temporarily returning fixed string.");
        return "[Description Temporarily Disabled]";
    }
}

// TODO: Implement IMenuHandlerSharp
public interface IMenuHandlerSharp { /* ... */ }

public class MenuSystemSharpWrapper : IMenuSystemSharp
{
    private readonly IntPtr _instancePtr;
    private readonly Func<IntPtr, int, IntPtr> _getPlayer;
    private readonly Func<IntPtr, IntPtr> _getProfilesSystem;
    private readonly Func<IntPtr, IntPtr, IntPtr, IntPtr> _createInstance;
    private readonly Func<IntPtr, IntPtr, int, int, int, bool> _displayInstanceToPlayer;
    private readonly Func<IntPtr, IntPtr, bool> _closeInstance;

    public MenuSystemSharpWrapper(IntPtr instancePtr)
    {
        _instancePtr = instancePtr;
        if (_instancePtr == IntPtr.Zero) throw new ArgumentNullException(nameof(instancePtr));
        _getPlayer = VirtualFunction.Create<IntPtr, int, IntPtr>(_instancePtr, 10);
        _getProfilesSystem = VirtualFunction.Create<IntPtr, IntPtr>(_instancePtr, 11);
        _createInstance = VirtualFunction.Create<IntPtr, IntPtr, IntPtr, IntPtr>(_instancePtr, 12);
        _displayInstanceToPlayer = VirtualFunction.Create<IntPtr, IntPtr, int, int, int, bool>(_instancePtr, 13);
        _closeInstance = VirtualFunction.Create<IntPtr, IntPtr, bool>(_instancePtr, 14);
    }

    public IntPtr GetPlayer(int playerSlot) => _getPlayer.Invoke(_instancePtr, playerSlot);
    public IMenuProfileSystemSharp GetProfiles()
    {
        IntPtr ptr = _getProfilesSystem.Invoke(_instancePtr);
        if (ptr == IntPtr.Zero) throw new InvalidOperationException("GetProfiles returned null.");
        return new MenuProfileSystemSharpWrapper(ptr);
    }
    public IMenuSharp CreateInstance(IMenuProfileSharp? profile, IMenuHandlerSharp? handler)
    {
        IntPtr pProfile = (profile as IWrapperWithInstancePtr)?.InstancePtr ?? IntPtr.Zero;
        IntPtr pHandler = (handler as IWrapperWithInstancePtr)?.InstancePtr ?? IntPtr.Zero;
        IntPtr menuPtr = _createInstance.Invoke(_instancePtr, pProfile, pHandler);
        if (menuPtr == IntPtr.Zero) throw new InvalidOperationException("Native CreateInstance returned null.");
        return new MenuSharpWrapper(menuPtr);
    }
    public bool DisplayInstanceToPlayer(IMenuSharp menu, int playerSlot, int startItem = 0, int displayTime = 0)
    {
        IntPtr pMenu = (menu as IWrapperWithInstancePtr)?.InstancePtr ?? IntPtr.Zero;
        if (pMenu == IntPtr.Zero && menu != null) return false;
        return _displayInstanceToPlayer.Invoke(_instancePtr, pMenu, playerSlot, startItem, displayTime);
    }
    public bool CloseInstance(IMenuSharp menu)
    {
        IntPtr pMenu = (menu as IWrapperWithInstancePtr)?.InstancePtr ?? IntPtr.Zero;
        if (pMenu == IntPtr.Zero && menu != null) return false;
        return _closeInstance.Invoke(_instancePtr, pMenu);
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
