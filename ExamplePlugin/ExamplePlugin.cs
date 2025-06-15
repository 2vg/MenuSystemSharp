using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using MenuSystemSharp.API;

namespace ExamplePlugin;

public class ExamplePlugin : BasePlugin
{
    public override string ModuleName => "ExamplePlugin";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "Example";
    public override string ModuleDescription => "Example plugin using MenuSystemSharp.API";

    private IMenuSystem? _menuSystem;

    public override void Load(bool hotReload)
    {
    }

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

    [ConsoleCommand("css_examplemenu", "Opens an example menu")]
    public void OnExampleMenuCommand(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (player == null || !player.IsValid || player.IsBot)
            return;

        if (_menuSystem == null)
        {
            player.PrintToChat("Menu system is not available");
            return;
        }

        try
        {
            var menu = _menuSystem.CreateMenu();
            menu.Title = "Example Menu";

            // Add menu items with callbacks
            menu.AddItem(MenuItemStyleFlags.Active | MenuItemStyleFlags.HasNumber, "Option 1", (menuInstance, selectedPlayer, itemPosition, itemOnPage, data) =>
            {
                selectedPlayer.PrintToChat($"You selected Option 1");
            });

            menu.AddItem(MenuItemStyleFlags.Active | MenuItemStyleFlags.HasNumber, "Option 2", (menuInstance, selectedPlayer, itemPosition, itemOnPage, data) =>
            {
                selectedPlayer.PrintToChat($"You selected Option 2");
            });

            menu.AddItem(MenuItemStyleFlags.Active | MenuItemStyleFlags.HasNumber, "Option 3", (menuInstance, selectedPlayer, itemPosition, itemOnPage, data) =>
            {
                selectedPlayer.PrintToChat($"You selected Option 3");
            });

            menu.AddItem(MenuItemStyleFlags.Active | MenuItemStyleFlags.HasNumber, "Option 4", (menuInstance, selectedPlayer, itemPosition, itemOnPage, data) =>
            {
                selectedPlayer.PrintToChat($"You selected Option 4");
            });

            menu.AddItem(MenuItemStyleFlags.Active | MenuItemStyleFlags.HasNumber, "Option 5", (menuInstance, selectedPlayer, itemPosition, itemOnPage, data) =>
            {
                selectedPlayer.PrintToChat($"You selected Option 5");
            });

            menu.AddItem(MenuItemStyleFlags.Active | MenuItemStyleFlags.HasNumber, "Submenu", (menuInstance, selectedPlayer, itemPosition, itemOnPage, data) =>
            {
                ShowSubmenu(selectedPlayer);
            });

            menu.AddItem(MenuItemStyleFlags.Active | MenuItemStyleFlags.HasNumber, "Close Menu", (menuInstance, selectedPlayer, itemPosition, itemOnPage, data) =>
            {
                menu.Close();
                selectedPlayer.PrintToChat("Menu closed");
            });

            // Display the menu to the player
            menu.DisplayToPlayer(player);
            Console.WriteLine($"[{ModuleName}] Menu displayed to {player.PlayerName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{ModuleName}] Error creating menu: {ex.Message}");
            player.PrintToChat("Error creating menu");
        }
    }

    private void ShowSubmenu(CCSPlayerController player)
    {
        if (_menuSystem == null)
        {
            player.PrintToChat("Menu system is not available");
            return;
        }

        try
        {
            // Get the default profile
            var profile = _menuSystem.GetProfile("default");
            if (profile == null)
            {
                player.PrintToChat("Default menu profile not found");
                return;
            }

            var submenu = _menuSystem.CreateMenu(profile);
            submenu.Title = "Submenu";

            submenu.AddItem(MenuItemStyleFlags.Active | MenuItemStyleFlags.HasNumber, "Submenu Option 1", (menuInstance, selectedPlayer, itemPosition, itemOnPage, data) =>
            {
                selectedPlayer.PrintToChat($"You selected Submenu Option 1");
            });

            submenu.AddItem(MenuItemStyleFlags.Active | MenuItemStyleFlags.HasNumber, "Submenu Option 2", (menuInstance, selectedPlayer, itemPosition, itemOnPage, data) =>
            {
                selectedPlayer.PrintToChat($"You selected Submenu Option 2");
            });

            submenu.AddItem(MenuItemStyleFlags.Active | MenuItemStyleFlags.HasNumber, "Submenu Option 3", (menuInstance, selectedPlayer, itemPosition, itemOnPage, data) =>
            {
                selectedPlayer.PrintToChat($"You selected Submenu Option 3");
            });

            submenu.AddItem(MenuItemStyleFlags.Active | MenuItemStyleFlags.HasNumber, "Submenu Option 4", (menuInstance, selectedPlayer, itemPosition, itemOnPage, data) =>
            {
                selectedPlayer.PrintToChat($"You selected Submenu Option 4");
            });

            submenu.AddItem(MenuItemStyleFlags.Active | MenuItemStyleFlags.HasNumber, "Back", (menuInstance, selectedPlayer, itemPosition, itemOnPage, data) =>
            {
                submenu.Close();
            });

            submenu.DisplayToPlayer(player);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{ModuleName}] Error creating submenu: {ex.Message}");
            player.PrintToChat("Error creating submenu");
        }
    }

    [ConsoleCommand("css_menutest", "Tests menu availability")]
    public void OnMenuTestCommand(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (player == null || !player.IsValid || player.IsBot)
            return;

        if (_menuSystem != null)
        {
            player.PrintToChat("MenuSystem is available!");
        }
        else
        {
            player.PrintToChat("MenuSystem is not available. Make sure MenuSystemSharp plugin is loaded.");
        }
    }
}