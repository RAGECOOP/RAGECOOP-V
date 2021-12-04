using LemonUI.Menus;

namespace CoopClient.Menus.Sub
{
    /// <summary>
    /// Don't use it!
    /// </summary>
    public class Servers
    {
        internal NativeMenu MainMenu = new NativeMenu("GTACOOP:R", "Servers", "Go to the server list")
        {
            UseMouse = false,
            Alignment = Main.MainSettings.FlipMenu ? GTA.UI.Alignment.Right : GTA.UI.Alignment.Left
        };
        internal readonly NativeItem AddRandom = new NativeItem("Add Item to Submenu", "Adds a random item to the submenu.");
        internal readonly NativeItem RemoveRandom = new NativeItem("Remove Items of Submenu", "Removes all of the random items on the submenu.");

        /// <summary>
        /// Don't use it!
        /// </summary>
        public Servers()
        {
            AddRandom.Activated += AddRandom_Activated;
            RemoveRandom.Activated += RemoveRandom_Activated;

            MainMenu.Add(AddRandom);
            MainMenu.Add(RemoveRandom);
        }

        private void AddRandom_Activated(object sender, System.EventArgs e)
        {
            MainMenu.Add(new NativeItem("Random", "This is a random item that we added."));
        }

        private void RemoveRandom_Activated(object sender, System.EventArgs e)
        {
            MainMenu.Remove(item => item != AddRandom && item != RemoveRandom);
        }
    }
}
