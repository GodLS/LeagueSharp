using LeagueSharp;
using LeagueSharp.Common;

namespace ADCPackage.Plugins
{
    internal class Ezreal
    {
        private static Spell Q, W, E, R;

        private static Obj_AI_Hero Player => ObjectManager.Player;

        public static void Load()
        {
            Game.PrintChat("[<font color='#F8F46D'>ADC Package</font>] by <font color='#79BAEC'>God</font> - <font color='#FFFFFF'>Ezreal</font> loaded (incomplete)");

            InitSpells();
            InitMenu();
        }

        private static void InitSpells()
        {

        }

        public static void Combo()
        {
        }

        public static void LaneClear()
        {
        }

        private static void InitMenu()
        {
            Menu.Config.AddSubMenu(new LeagueSharp.Common.Menu("Ezreal", "adcpackage.ezreal"));
        }
    }
}