using System;
using System.Diagnostics;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;

namespace ExecutionTimer
{
    internal class Program
    {
        internal static Stopwatch stop_watch = new Stopwatch();
        internal static Obj_AI_Base last_hit_player;
        internal static string text = "Executed";
        internal static Menu menu = new Menu("Execution Timer", "Execution Timer", true);

        private static void Main(string[] args) 
        {
            CustomEvents.Game.OnGameLoad += eventArgs =>
            {
                AttackableUnit.OnDamage += OnDamage;
                Drawing.OnDraw += OnDraw;
                Game.PrintChat("[Execution<font color='#79BAEC'>Timer</font>]: <font color='#FFFFFF'>" + "Loaded!</font>");

                menu.AddItem(new MenuItem("enabled", "Enabled", false).SetValue(true));
                menu.AddItem(new MenuItem("xpos", "X Position").SetValue(new Slider(100, Drawing.Width)));
                menu.AddItem(new MenuItem("ypos", "Y Position").SetValue(new Slider(100, Drawing.Height)));

                menu.AddToMainMenu();
            };
        }

        private static void OnDraw(EventArgs args)
        {
            if (!menu.Item("enabled").IsActive() || ObjectManager.Player.IsDead)
                return;

            if (stop_watch.ElapsedMilliseconds > 14000)       
                text = "Executed";
   
            else if (last_hit_player != null)
                text = last_hit_player.CharData.BaseSkinName + " gets the kill for " + (14 - stop_watch.Elapsed.Seconds) + " more second(s)";

            Drawing.DrawText(menu.Item("xpos").GetValue<Slider>().Value, menu.Item("ypos").GetValue<Slider>().Value, System.Drawing.Color.White, text);
        }

        private static void OnDamage(AttackableUnit sender, AttackableUnitDamageEventArgs args)
        {
            if (!menu.Item("enabled").IsActive() || ObjectManager.Player.IsDead)
                return;

            if (!sender.IsMe)
                return;

            foreach (var e in HeroManager.Enemies.Where(e => e.IsValid && e.NetworkId == args.SourceNetworkId))
            {
                stop_watch.Restart();
                last_hit_player = e;
            }
        }
    }
}
