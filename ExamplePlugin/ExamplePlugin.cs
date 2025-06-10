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

    public override void Load(bool hotReload)
    {
        Console.WriteLine($"[{ModuleName}] Plugin loaded");
    }

    [ConsoleCommand("css_examplemenu", "Opens an example menu")]
    public void OnExampleMenuCommand(CCSPlayerController? player, CommandInfo commandInfo)
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
            // Create a menu using the API
            var menu = MenuSystemAPI.CreateMenu("Example Menu");

            // Add menu items with callbacks
            menu.AddItem("Option 1", (selectedPlayer, selectedMenu, itemIndex) =>
            {
                selectedPlayer?.PrintToChat($"You selected Option 1 (index: {itemIndex})");
            });

            menu.AddItem("Option 2", (selectedPlayer, selectedMenu, itemIndex) =>
            {
                selectedPlayer?.PrintToChat($"You selected Option 2 (index: {itemIndex})");
            });

            menu.AddItem("Submenu", (selectedPlayer, selectedMenu, itemIndex) =>
            {
                if (selectedPlayer != null)
                {
                    ShowSubmenu(selectedPlayer);
                }
            });

            menu.AddItem("Close Menu", (selectedPlayer, selectedMenu, itemIndex) =>
            {
                if (selectedPlayer != null)
                {
                    MenuSystemAPI.CloseMenu(selectedMenu);
                    selectedPlayer.PrintToChat("Menu closed");
                }
            });

            // Display the menu to the player
            if (MenuSystemAPI.DisplayMenu(menu, player))
            {
                Console.WriteLine($"[{ModuleName}] Menu displayed to {player.PlayerName}");
            }
            else
            {
                player.PrintToChat("Failed to display menu");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{ModuleName}] Error creating menu: {ex.Message}");
            player.PrintToChat("Error creating menu");
        }
    }

    private void ShowSubmenu(CCSPlayerController player)
    {
        try
        {
            var submenu = MenuSystemAPI.CreateMenu("Submenu");

            submenu.AddItem("Submenu Option 1", (selectedPlayer, selectedMenu, itemIndex) =>
            {
                selectedPlayer?.PrintToChat("You selected Submenu Option 1");
            });

            submenu.AddItem("Submenu Option 2", (selectedPlayer, selectedMenu, itemIndex) =>
            {
                selectedPlayer?.PrintToChat("You selected Submenu Option 2");
            });

            MenuSystemAPI.DisplayMenu(submenu, player);
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

        if (MenuSystemAPI.IsAvailable)
        {
            player.PrintToChat("MenuSystem is available!");
        }
        else
        {
            player.PrintToChat("MenuSystem is not available. Make sure MenuSystemSharp plugin is loaded.");
        }
    }
}