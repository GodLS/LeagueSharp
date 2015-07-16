using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

namespace Varus_God
{
    internal class Program
    {
        public static readonly Menu Config = new Menu("Varus God", "VarusGod", true);
        private static readonly Obj_AI_Hero Player = ObjectManager.Player;
        private static Orbwalking.Orbwalker Orbwalker;

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += OnGameLoad;
        }

        private static void OnGameLoad(EventArgs args)
        {
            if (Player.CharData.BaseSkinName != "Varus") return;

            InitMenu();
            Spells.Initialize();

            Game.OnUpdate += OnUpdate;
            Drawing.OnDraw += OnDraw;
            AntiGapcloser.OnEnemyGapcloser += OnEnemyGapcloser;
        }

        private static void OnUpdate(EventArgs args)
        {
            Killsteal();
            FireR();
            ChainCC();

            switch (Orbwalker.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.Combo:
                    if (!Spells.Q.IsCharging)
                        Orbwalker.SetAttack(true);
                    Combo();
                    break;
                case Orbwalking.OrbwalkingMode.Mixed:
                    Orbwalker.SetAttack(!Spells.Q.IsCharging);
                    Harass();
                    break;
                case Orbwalking.OrbwalkingMode.LaneClear:
                    Orbwalker.SetAttack(!Spells.Q.IsCharging);
                    Laneclear();
                    Jungleclear();
                    break;
                case Orbwalking.OrbwalkingMode.LastHit:
                    Orbwalker.SetAttack(!Spells.Q.IsCharging);
                    break;
                case Orbwalking.OrbwalkingMode.None:
                    if (Config.Item("flee").GetValue<KeyBind>().Active)
                    {
                        if (Spells.E.IsReady() && Config.Item("fleeE").GetValue<bool>())
                        {
                            var etarget =
                                HeroManager.Enemies
                                    .FindAll(
                                        enemy =>
                                            enemy.IsValidTarget() && Player.Distance(enemy.Position) <= Spells.E.Range)
                                    .OrderBy(e => e.Distance(Player));
                            if (etarget.FirstOrDefault() == null) goto BEFOREWALK;
                            var eprediction = Prediction.GetPrediction(etarget.FirstOrDefault(), Spells.E.Delay,
                                Spells.E.Width, Spells.E.Speed);

                            Spells.E.Cast(eprediction.CastPosition);
                        }

                    BEFOREWALK:
                        if (Spells.E.IsReady() && Config.Item("fleeR").GetValue<bool>())
                        {
                            var rtarget =
                                HeroManager.Enemies
                                    .FindAll(
                                        enemy =>
                                            enemy.IsValidTarget() && Player.Distance(enemy.Position) <= Spells.R.Range)
                                    .OrderBy(e => e.Distance(Player));
                            if (rtarget.FirstOrDefault() == null) goto WALK;
                            var rprediction = Prediction.GetPrediction(rtarget.FirstOrDefault(), Spells.R.Delay,
                                Spells.R.Width, Spells.R.Speed);

                            Spells.R.Cast(rprediction.CastPosition);
                        }
                    WALK:
                        Orbwalking.MoveTo(Game.CursorPos);
                    }
                    break;
            }
        }

        private static void Combo()
        {
            var target = TargetSelector.GetTarget(Spells.Q.ChargedMaxRange, TargetSelector.DamageType.Physical);
            var etarget = TargetSelector.GetTarget(Spells.E.Range, TargetSelector.DamageType.Physical);
            var t = HeroManager.Enemies.FirstOrDefault(x => x.IsValidTarget(Spells.Q.ChargedMaxRange));
            if (target == null) return;
            if (Player.IsWindingUp) return;
            var comboEStacks = Config.Item("comboWStacksE").GetValue<Slider>().Value;
            var comboE = Config.Item("comboE").GetValue<bool>();
            var comboEAoE = Config.Item("comboEAoE").GetValue<bool>();
            if (Spells.Q.IsReady())
            {
                if (Spells.Q.IsCharging)
                {
                    var damage = qDamage(target);

                    Orbwalker.SetAttack(false);
                    if (!Spells.Q.IsInRange(target)) return;
                    if (Spells.Q.Range >= Spells.Q.ChargedMaxRange)
                        Spells.Q.Cast(target);

                    else if (damage >= target.Health)
                        Spells.Q.Cast(target);
                }

                else if (Config.Item("comboQ").GetValue<bool>())
                    if (Spells.W.Level == 0 || t != null &&
                        t.GetBlightStacks() >= Config.Item("comboWStacksQ").GetValue<Slider>().Value)
                        Spells.Q.StartCharging();
            }

            if (Spells.Q.IsCharging) return;

            if (!Spells.E.IsReady()) return;
            if (etarget == null) return;

            if (comboE &&
                !comboEAoE)
            {
                if (etarget.GetBlightStacks() >= comboEStacks)
                    if (Player.Distance(etarget) <= Spells.E.Range)
                        Spells.E.Cast(etarget);
            }
            else if (comboE)
            {
                if (Spells.W.Level == 0 || etarget.GetBlightStacks() >= comboEStacks)
                {
                    if (Player.Distance(etarget) <= Spells.E.Range)
                        Spells.E.Cast(etarget);
                }

                foreach (var enemy in HeroManager.Enemies.Where(e => e.IsValidTarget(Spells.E.Range)))
                    Spells.E.CastIfWillHit(enemy, Config.Item("comboEAoEAmount").GetValue<Slider>().Value, true);
            }

            else if (comboEAoE)
            {
                foreach (var enemy in HeroManager.Enemies.Where(e => e.IsValidTarget(Spells.E.Range)))
                    Spells.E.CastIfWillHit(enemy, Config.Item("comboEAoEAmount").GetValue<Slider>().Value, true);
            }

            if (Spells.R.IsReady() && Config.Item("comboR").GetValue<bool>() &&
                Player.CountEnemiesInRange(Spells.R.Range) >= Config.Item("comboRAoE").GetValue<Slider>().Value)
            {
                Spells.R.CastOnBestTarget();
            }
        }

        private static void Harass()
        {
            if (Player.ManaPercent <= Config.Item("harassMana").GetValue<Slider>().Value) return;
            var target = TargetSelector.GetTarget(Spells.Q.ChargedMaxRange, TargetSelector.DamageType.Physical);
            var etarget = TargetSelector.GetTarget(Spells.E.Range, TargetSelector.DamageType.Physical);
            var t = HeroManager.Enemies.FirstOrDefault(x => x.IsValidTarget(Spells.Q.ChargedMaxRange));
            if (target == null) return;
            if (Player.IsWindingUp) return;
            var harassEStacks = Config.Item("harassWStacksE").GetValue<Slider>().Value;
            var harassE = Config.Item("harassE").GetValue<bool>();
            var harassEAoE = Config.Item("harassEAoE").GetValue<bool>();
            if (Spells.Q.IsReady())
            {
                if (Spells.Q.IsCharging)
                {
                    var damage = qDamage(target);

                    Orbwalker.SetAttack(false);
                    if (!Spells.Q.IsInRange(target)) return;
                    if (Spells.Q.Range >= Spells.Q.ChargedMaxRange)
                        Spells.Q.Cast(target);

                    else if (damage >= target.Health)
                        Spells.Q.Cast(target);
                }

                else if (Config.Item("harassQ").GetValue<bool>())
                    if (Spells.W.Level == 0 || t != null &&
                        t.GetBlightStacks() >= Config.Item("harassWStacksQ").GetValue<Slider>().Value)
                        Spells.Q.StartCharging();
            }

            if (Spells.Q.IsCharging) return;

            if (!Spells.E.IsReady()) return;
            if (etarget == null) return;

            if (harassE &&
                !harassEAoE)
            {
                if (etarget.GetBlightStacks() >= harassEStacks)
                    if (Player.Distance(etarget) <= Spells.E.Range)
                        Spells.E.Cast(etarget);
            }
            else if (harassE)
            {
                if (etarget.GetBlightStacks() >= harassEStacks)
                {
                    if (Player.Distance(etarget) <= Spells.E.Range)
                        Spells.E.Cast(etarget);
                }

                foreach (var enemy in HeroManager.Enemies.Where(e => e.IsValidTarget(Spells.E.Range)))
                    Spells.E.CastIfWillHit(enemy, Config.Item("harassEAoEAmount").GetValue<Slider>().Value, true);
            }

            else if (harassEAoE)
            {
                foreach (var enemy in HeroManager.Enemies.Where(e => e.IsValidTarget(Spells.E.Range)))
                    Spells.E.CastIfWillHit(enemy, Config.Item("harassEAoEAmount").GetValue<Slider>().Value, true);
            }
        }

        private static void Killsteal()
        {
            if (Config.Item("killstealQ").GetValue<bool>() && Spells.Q.IsReady())
            {
                foreach (
                    var target in
                        HeroManager.Enemies.Where(
                            enemy =>
                                enemy.IsValidTarget() && Spells.Q.GetSpellDamage(enemy) > enemy.Health &&
                                Player.Distance(enemy.Position) <= Spells.Q.ChargedMaxRange))
                {
                    Spells.Q.StartCharging();

                    if (Spells.Q.IsCharging)
                    {
                        var damage = qDamage(target);

                        Orbwalker.SetAttack(false);
                        if (damage >= target.Health && !target.IsInvulnerable)
                            Spells.Q.Cast(target);
                    }
                }
            }

            if (Spells.Q.IsCharging) return;
            if (Config.Item("killstealE").GetValue<bool>() && Spells.E.IsReady())
                foreach (
                    var target in
                        HeroManager.Enemies.Where(
                            enemy =>
                                enemy.IsValidTarget() && Spells.E.GetSpellDamage(enemy) > enemy.Health &&
                                Player.Distance(enemy.Position) <= Spells.E.Range))
                {
                    Spells.E.Cast(target);
                }

            if (Config.Item("killstealR").GetValue<bool>() && Spells.R.IsReady())
                foreach (
                    var target in
                        HeroManager.Enemies.Where(
                            enemy =>
                                enemy.IsValidTarget() && Spells.R.GetSpellDamage(enemy) > enemy.Health &&
                                Player.Distance(enemy.Position) <= Spells.R.Range))
                {
                    Spells.R.Cast(target);
                }
        }

        private static void ChainCC()
        {
            if (!Config.Item("chainCC").GetValue<bool>()) return;
            foreach (var target in HeroManager.Enemies.Where(t => t.IsValidTarget(Spells.R.Range)))
            {
                foreach (var buff in target.Buffs)
                {
                    if (buff.Type == BuffType.Charm || buff.Type == BuffType.Fear ||
                        buff.Type == BuffType.Stun || buff.Type == BuffType.Taunt ||
                        buff.Type == BuffType.Flee || buff.Type == BuffType.Knockup ||
                        buff.Type == BuffType.Polymorph || buff.Type == BuffType.Suppression ||
                        buff.Type == BuffType.Snare)
                    {
                        var buffEndTime = buff.EndTime - (target.PercentCCReduction * (buff.EndTime - buff.StartTime));
                        var cctimeleft = buffEndTime - Game.Time;
                        var speed = target.Position.Distance(Player.Position) / Spells.R.Speed;
                        if (cctimeleft <= speed)
                        {
                            if (Spells.R.IsReady())
                            {
                                Spells.R.Cast(target);
                            }
                        }
                    }
                }
            }
        }

        private static void Laneclear()
        {
            if (Player.ManaPercent <= Config.Item("laneclearMana").GetValue<Slider>().Value) return;

            if (Spells.Q.IsReady() && Config.Item("laneclearQ").GetValue<bool>())
            {
                var hitCount = 0;

                var allMinions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Spells.Q.ChargedMaxRange);
                {
                    foreach (var minion in allMinions.Where(m => m.IsValidTarget()))
                    {
                        hitCount++;
                        if (hitCount >= Config.Item("laneclearQMin").GetValue<Slider>().Value)
                        {
                            if (!Spells.Q.IsCharging)
                                Spells.Q.StartCharging();

                            if (Spells.Q.IsCharging && Spells.Q.Range >= Spells.Q.ChargedMaxRange)
                                Spells.Q.Cast(minion);
                        }
                    }
                }
            }

            if (Spells.Q.IsCharging) return;

            if (Spells.E.IsReady() && Config.Item("laneclearE").GetValue<bool>())
            {
                var hitCount = 0;

                var allMinions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Spells.E.Range);
                {
                    foreach (var minion in allMinions.Where(m => m.IsValidTarget()))
                    {
                        hitCount++;
                        if (hitCount >= Config.Item("laneclearEMin").GetValue<Slider>().Value)
                            Spells.E.Cast(minion.Position);
                    }
                }
            }
        }

        private static void Jungleclear()
        {
            if (Player.ManaPercent <= Config.Item("laneclearMana").GetValue<Slider>().Value) return;

            if (Spells.Q.IsReady() && Config.Item("laneclearQ").GetValue<bool>())
            {
                var hitCount = 0;

                var allMinions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Spells.Q.ChargedMaxRange,
                    MinionTypes.All, MinionTeam.Neutral);
                {
                    foreach (
                        var minion in
                            allMinions.Where(m => m.IsValid)
                                .OrderBy(m => m.Distance(Player.Position))
                                .ThenBy(m => m.Health))
                    {
                        hitCount++;
                        if (hitCount >= 1)
                        {
                            if (!Spells.Q.IsCharging)
                                Spells.Q.StartCharging();

                            if (Spells.Q.IsCharging && Spells.Q.Range >= Spells.Q.ChargedMaxRange)
                                Spells.Q.Cast(minion);
                        }
                    }
                }
            }

            if (Spells.Q.IsCharging) return;

            if (Spells.E.IsReady() && Config.Item("laneclearE").GetValue<bool>())
            {
                var hitCount = 0;

                var allMinions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Spells.E.Range,
                    MinionTypes.All, MinionTeam.Neutral);
                {
                    foreach (
                        var minion in
                            allMinions.Where(m => m.IsValid)
                                .OrderBy(m => m.Distance(Player.Position))
                                .ThenBy(m => m.Health))
                    {
                        hitCount++;
                        if (hitCount >= 1)
                            Spells.E.Cast(minion.Position);
                    }
                }
            }
        }

        private static void FireR()
        {
            if (Spells.R.IsReady() && Config.Item("combofireR").GetValue<KeyBind>().Active)
                Spells.R.CastOnBestTarget();
        }

        private static void OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (Spells.Q.IsCharging) return;

            if (!Spells.E.IsReady() || !Config.Item("antigcE").GetValue<bool>() || !gapcloser.Sender.IsEnemy) return;
            Utility.DelayAction.Add(500, () => Spells.E.Cast(gapcloser.End));

            if (!Spells.R.IsReady() || !Config.Item("antigcR").GetValue<bool>() || !gapcloser.Sender.IsEnemy) return;
            Utility.DelayAction.Add(500, () => Spells.R.Cast(gapcloser.Sender));
        }

        private static void OnDraw(EventArgs args)
        {
            if (Config.Item("drawQ").GetValue<bool>() && Spells.Q.IsReady())
                Drawing.DrawCircle(Player.Position, Spells.Q.ChargedMaxRange, Color.LightCoral);
            else if (Config.Item("drawQ").GetValue<bool>() && !Spells.Q.IsReady())
                Drawing.DrawCircle(Player.Position, Spells.Q.ChargedMaxRange, Color.Maroon);


            if (Config.Item("drawE").GetValue<bool>() && Spells.E.IsReady())
                Drawing.DrawCircle(Player.Position, Spells.E.Range, Color.LightCoral);
            else if (Config.Item("drawE").GetValue<bool>() && !Spells.E.IsReady())
                Drawing.DrawCircle(Player.Position, Spells.E.Range, Color.Maroon);

            if (Config.Item("drawR").GetValue<bool>() && Spells.R.IsReady())
                Drawing.DrawCircle(Player.Position, Spells.R.Range, Color.LightCoral);
            else if (Config.Item("drawR").GetValue<bool>() && !Spells.R.IsReady())
                Drawing.DrawCircle(Player.Position, Spells.R.Range, Color.Maroon);
        }

        private static float qDamage(Obj_AI_Hero hero)
        {
            float dmg;

            if (Spells.Q.IsReady())
            {
                var damage = Spells.Q.GetSpellDamage(hero);
                var qprediction = Prediction.GetPrediction(hero, Spells.Q.Delay, Spells.Q.Width, Spells.Q.Speed);
                var positions = new List<Vector2>
                    {
                        (Vector2) qprediction.UnitPosition,
                        (Vector2) qprediction.CastPosition
                    }; //not sure if correctly done.. but it works
                var count = Spells.Q.GetCollision(Player.Position.To2D(), positions).Count(); //same here

                if (count >= 6)
                    damage = damage * 0.33f;
                else
                    damage -= count * (damage * 0.15f);

                dmg = damage;
            }
            else
                dmg = 0;

            return dmg;
        }

        private static void InitMenu()
        {
            var orbwalkerMenu = new Menu("Orbwalker", "Orbwalker");
            {
                Orbwalker = new Orbwalking.Orbwalker(orbwalkerMenu);
                Config.AddSubMenu(orbwalkerMenu);
            }

            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            {
                TargetSelector.AddToMenu(targetSelectorMenu);
                Config.AddSubMenu(targetSelectorMenu);
            }

            var comboMenu = new Menu("Combo", "Combo settings");
            {
                comboMenu.AddItem(new MenuItem("comboQ", "Use Q").SetValue(true));
                comboMenu.AddItem(
                    new MenuItem("comboWStacksQ", "W stacks needed to start Q charge").SetValue(new Slider(0, 0, 3)));
                comboMenu.AddItem(new MenuItem("comboE", "Use E on stack #").SetValue(true));
                comboMenu.AddItem(
                    new MenuItem("comboWStacksE", "W stacks needed to cast E").SetValue(new Slider(0, 0, 3)));
                comboMenu.AddItem(new MenuItem("comboEAoE", "Use E AoE").SetValue(true));
                comboMenu.AddItem(
                    new MenuItem("comboEAoEAmount", "Minimum targets to cast E").SetValue(new Slider(2, 2, 5)));
                comboMenu.AddItem(new MenuItem("comboR", "Use R").SetValue(true));
                comboMenu.AddItem(new MenuItem("comboRAoE", "Minimum targets to cast R").SetValue(new Slider(2, 2, 5)));
                comboMenu.AddItem(
                    new MenuItem("combofireR", "Fire R to target on keypress").SetValue(new KeyBind(
                        "A".ToCharArray()[0], KeyBindType.Press)));
                Config.AddSubMenu(comboMenu);
            }
            var harassMenu = new Menu("Harass", "Harass settings");
            {
                harassMenu.AddItem(new MenuItem("harassQ", "Use Q").SetValue(true));
                harassMenu.AddItem(
                    new MenuItem("harassWStacksQ", "W stacks needed to start Q charge").SetValue(new Slider(0, 0, 3)));
                harassMenu.AddItem(new MenuItem("harassE", "Use E on stack #").SetValue(true));
                harassMenu.AddItem(
                    new MenuItem("harassWStacksE", "W stacks needed to cast E").SetValue(new Slider(0, 0, 3)));
                harassMenu.AddItem(new MenuItem("harassEAoE", "Use E AoE").SetValue(true));
                harassMenu.AddItem(
                    new MenuItem("harassEAoEAmount", "Minimum targets to cast E").SetValue(new Slider(2, 2, 5)));
                harassMenu.AddItem(new MenuItem("harassMana", "Mana manager (%)").SetValue(new Slider(40, 1, 100)));
                Config.AddSubMenu(harassMenu);
            }
            var laneclearMenu = new Menu("Laneclear", "Laneclear settings");
            {
                laneclearMenu.AddItem(new MenuItem("laneclearQ", "Use Q").SetValue(true));
                laneclearMenu.AddItem(
                    new MenuItem("laneclearQMin", "Minimum minions hit to cast Q").SetValue(new Slider(4, 1, 6)));
                laneclearMenu.AddItem(new MenuItem("laneclearE", "Use E").SetValue(true));
                laneclearMenu.AddItem(
                    new MenuItem("laneclearEMin", "Minimum minions hit to cast E").SetValue(new Slider(4, 1, 6)));
                laneclearMenu.AddItem(new MenuItem("laneclearMana", "Mana manager (%)").SetValue(new Slider(40, 1, 100)));
                Config.AddSubMenu(laneclearMenu);
            }
            var drawingsMenu = new Menu("Drawings", "Drawings settings");
            {
                drawingsMenu.AddItem(new MenuItem("drawQ", "Draw Q").SetValue(true));
                drawingsMenu.AddItem(new MenuItem("drawE", "Draw E").SetValue(true));
                drawingsMenu.AddItem(new MenuItem("drawR", "Draw R").SetValue(true));
                var dmgAfterQ = new MenuItem("dmgAfterCombo", "Draw Q damage on target").SetValue(true);
                Utility.HpBarDamageIndicator.DamageToUnit = qDamage;
                Utility.HpBarDamageIndicator.Enabled = dmgAfterQ.GetValue<bool>();
                drawingsMenu.AddItem(dmgAfterQ);
                Config.AddSubMenu(drawingsMenu);
            }
            var miscMenu = new Menu("Misc", "Misc. settings");
            {
                miscMenu.AddItem(new MenuItem("antigcE", "Anti-Gapclose E").SetValue(true));
                miscMenu.AddItem(new MenuItem("antigcR", "Anti-Gapclose R").SetValue(true));
                miscMenu.AddItem(new MenuItem("killstealQ", "Killsteal Q").SetValue(true));
                miscMenu.AddItem(new MenuItem("killstealE", "Killsteal E").SetValue(true));
                miscMenu.AddItem(new MenuItem("killstealR", "Killsteal R").SetValue(false));
                miscMenu.AddItem(new MenuItem("chainCC", "Chain CC with R").SetValue(false));
                Config.AddSubMenu(miscMenu);
            }
            var fleeMEnu = new Menu("Flee", "Flee settings");
            {
                fleeMEnu.AddItem(new MenuItem("fleeE", "Use E").SetValue(true));
                fleeMEnu.AddItem(new MenuItem("fleeR", "Use R").SetValue(true));
                fleeMEnu.AddItem(
                    new MenuItem("flee", "Flee!").SetValue(new KeyBind("Z".ToCharArray()[0], KeyBindType.Press)));
                Config.AddSubMenu(fleeMEnu);
            }

            Config.AddToMainMenu();
        }
    }
}