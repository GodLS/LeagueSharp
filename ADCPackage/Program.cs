using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace ADCPackage
{
    class Program
    {
        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            Menu.Initialize();
            PluginLoader.Load();

            Game.OnUpdate += Game_OnUpdate;
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            OrbwalkerSwitch.Update();
        }
    }
}
