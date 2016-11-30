using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ADCPackage.Plugins;
using LeagueSharp.Common;

namespace ADCPackage
{
    class OrbwalkerSwitch
    {
        public static void Update()
        {
            switch (Menu.Orbwalker.ActiveMode)
            {
                //case CustomOrbwalker.OrbwalkingMode.Combo:
                case Orbwalking.OrbwalkingMode.Combo:
                    switch (PluginLoader.Champname)
                    {
                        case "Jinx":
                            Jinx.Combo();
                            break;
                        case "Tristana":
                            Tristana.Combo();
                            break;
                        case "Corki":
                            Corki.Combo();
                            break;
                    }
                    break;
                //case CustomOrbwalker.OrbwalkingMode.Mixed:
                case Orbwalking.OrbwalkingMode.Mixed:
                    switch (PluginLoader.Champname)
                    {
                        case "Jinx":
                            Jinx.Harass();
                            break;
                        case "Tristana":
                            Tristana.Harass();
                            break;
                        case "Corki":
                            Corki.Harass();
                            break;
                    }
                    break;
                //case CustomOrbwalker.OrbwalkingMode.LaneClear:
                case Orbwalking.OrbwalkingMode.LaneClear:
                    switch (PluginLoader.Champname)
                    {
                        case "Jinx":
                            Jinx.LaneClear();
                            break;
                        case "Tristana":
                            Tristana.LaneClear();
                            break;
                        case "Corki":
                            Corki.LaneClear();
                            break;
                    }
                    break;
            }
        }
    }
}
