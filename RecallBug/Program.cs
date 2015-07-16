using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using Microsoft.Win32.SafeHandles;
using SharpDX;

namespace RecallBug
{
    internal class Program
    {
        private static readonly Obj_AI_Hero Player = ObjectManager.Player;
        public static readonly Menu Config = new Menu("Recall Bug", "RecallBugExploitLegitHax", true);
        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += OnGameLoad;
        }

        private static void OnGameLoad(EventArgs args)
        {
            InitMenu();
            Spells.Initialize();

            Game.OnUpdate += OnUpdate;
        }

        private static void InitMenu()
        {
            Config.AddItem(new MenuItem("spell1", "Spell 1").SetValue(new StringList(new[] { "Q", "W", "E", "R" }, 0)));
            Config.AddItem(new MenuItem("spell2", "Spell 2").SetValue(new StringList(new[] { "Q", "W", "E", "R" }, 1)));
            Config.AddItem(new MenuItem("spell3", "Spell 3 (currently not available)").SetValue(new StringList(new[] { "Off", "Off", "Off", "Off", "Off" }, 0)));
            Config.AddItem(new MenuItem("extrad", "Extra Delay").SetValue(new Slider(0, -50, 100)));
                Config.AddItem(
                    new MenuItem("recall", "Recall (QUICK TAP)").SetValue(new KeyBind("A".ToCharArray()[0],
                        KeyBindType.Press)));
            Config.AddToMainMenu();
        }

        private static void OnUpdate(EventArgs args)
        {
            var minion = MinionManager.GetMinions(Player.AttackRange, MinionTypes.All, MinionTeam.NotAlly).FirstOrDefault();
            var enemy = HeroManager.Enemies.FirstOrDefault(e => e.IsValidTarget(Player.AttackRange));
            var spell1 = Config.Item("spell1").GetValue<StringList>();
            var spell2 = Config.Item("spell2").GetValue<StringList>();
            var extrad = Config.Item("extrad").GetValue<Slider>().Value;
           
            if (Config.Item("recall").GetValue<KeyBind>().Active)
            {
                if (!Player.HasBuff("recallimproved"))
                {
                    Spells.B.Cast();
                    switch (spell1.SelectedIndex)
                    {
                        case 0:
                            Utility.DelayAction.Add(30 + extrad, () => Spells.Q.Cast(Game.CursorPos));
                            break;
                        case 1:
                            Utility.DelayAction.Add(30 + extrad, () => Spells.W.Cast(Game.CursorPos));
                            break;
                        case 2:
                            Utility.DelayAction.Add(30 + extrad, () => Spells.E.Cast(Game.CursorPos));
                            break;
                        case 3:
                            Utility.DelayAction.Add(30 + extrad, () => Spells.R.Cast(Game.CursorPos));
                            break;
                    }
                    if (minion != null)
                    Utility.DelayAction.Add(80 + extrad, () => Player.IssueOrder(GameObjectOrder.AttackUnit, minion));
                    else if (enemy != null)
                    Utility.DelayAction.Add(80 + extrad, () => Player.IssueOrder(GameObjectOrder.AttackUnit, enemy));
                }
            }
            if (Player.HasBuff("recallimproved"))
            {
                switch (spell2.SelectedIndex)
                {
                    case 0:
                        Utility.DelayAction.Add(100 + extrad, () => Spells.Q.Cast(Game.CursorPos));
                        break;
                    case 1:
                        Utility.DelayAction.Add(100 + extrad, () => Spells.W.Cast(Game.CursorPos));
                        break;
                    case 2:
                        Utility.DelayAction.Add(100 + extrad, () => Spells.E.Cast(Game.CursorPos));
                        break;
                    case 3:
                        Utility.DelayAction.Add(100 + extrad, () => Spells.R.Cast(Game.CursorPos));
                        break;
                }
            }
        }
    }

    internal class Spells
    {
        public static void Initialize()
        {

            Q = new Spell(SpellSlot.Q, 999);
            W = new Spell(SpellSlot.W, 999);
            E = new Spell(SpellSlot.E, 999);
            R = new Spell(SpellSlot.R, 999);
            B = new Spell(SpellSlot.Recall);
        }

        public static Spell B { get; set; }
        public static Spell Q { get; set; }
        public static Spell W { get; set; }
        public static Spell E { get; set; }
        public static Spell R { get; set; }
    }
}
