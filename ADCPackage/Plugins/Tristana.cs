﻿using System;
using System.Drawing;
using System.Linq;
using System.Net.Mime;
using LeagueSharp;
using LeagueSharp.Common;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Xsl;
using SharpDX;
using Color = System.Drawing.Color;

namespace ADCPackage.Plugins
{
    /* 
    CREDITS TO:

        - ScienceARK - spell values + range updater, estacks
        - endif - e explosion radius (kind of kek)
    */

    internal static class Tristana
    {
        private static Obj_AI_Hero _target;
        private static Spell Q, W, E, R;
        private static Obj_AI_Hero Player => ObjectManager.Player;
        private static float delay;
        private static Vector3 startpos;

        //private static int oldkillvalue;

        public static void Load()
        {
            Game.PrintChat("[<font color='#F8F46D'>ADC Package</font>] by <font color='#79BAEC'>God</font> - <font color='#FFFFFF'>Tristana</font> loaded");
            Obj_AI_Base.OnLevelUp += Obj_AI_Base_OnLevelUp;
            CustomOrbwalker.BeforeAttack += CustomOrbwalker_BeforeAttack;
            Game.OnUpdate += PermaActive;
            Drawing.OnDraw += Drawing_OnDraw;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Interrupter2.OnInterruptableTarget += Interrupter2_OnInterruptableTarget;
            Obj_AI_Base.OnDoCast += Obj_AI_Base_OnDoCast;

            InitSpells();
            InitMenu();
        }

        private static void Obj_AI_Base_OnDoCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsMe) return;
            if (args.Slot != SpellSlot.W) return;
            startpos = args.Start;

        }


        private static void Interrupter2_OnInterruptableTarget(Obj_AI_Hero sender,
            Interrupter2.InterruptableTargetEventArgs args)
        {
            if (R.IsReady())
            {
                foreach (var champ in from interrupter in HeroManager.Enemies.SelectMany(
                    enemy => Interrupter.Spells.Where(g => g.ChampionName == enemy.ChampionName))
                    where Menu.Config.Item(interrupter.ChampionName).IsActive()
                    select HeroManager.Enemies.Find(c => c.ChampionName == interrupter.ChampionName))
                {
                    R.CastOnUnit(champ);
                }
            }
        }

        private static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (Menu.Config.Item(gapcloser.Sender.ChampionName).IsActive() && R.IsReady())
            {
                R.CastOnUnit(gapcloser.Sender);
            }
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (Menu.Config.Item("draw.explosion").IsActive())
            {
                var eturret =
                    ObjectManager.Get<Obj_AI_Turret>().FirstOrDefault(t => !t.IsDead && t.HasBuff("tristanaecharge"));
                var etarget = HeroManager.Enemies.FirstOrDefault(e => !e.IsDead && e.HasBuff("tristanaecharge"));

                if (etarget != null)
                {
                    var etargetstacks = etarget.Buffs.Find(buff => buff.Name == "tristanaecharge").Count;

                    if (etarget.Health < (E.GetDamage(etarget) + (((etargetstacks*0.30))*E.GetDamage(etarget))))
                    {
                        Drawing.DrawCircle(etarget.Position, 150 + etarget.BoundingRadius, Color.Red);
                    }
                    else if (etarget.Health > (E.GetDamage(etarget) + (((etargetstacks*0.30))*E.GetDamage(etarget))))
                    {
                        Drawing.DrawCircle(etarget.Position, 150 + etarget.BoundingRadius, Color.Sienna);
                    }
                }

                if (eturret != null)
                {
                    var eturretstacks = eturret.Buffs.Find(buff => buff.Name == "tristanaecharge").Count;

                    if (eturret.Health < (E.GetDamage(eturret) + (((eturretstacks*0.30))*E.GetDamage(eturret))))
                    {
                        Drawing.DrawCircle(eturret.Position, 300 + eturret.BoundingRadius, Color.Red);
                    }
                    else if (eturret.Health > (E.GetDamage(eturret) + (((eturretstacks*0.30))*E.GetDamage(eturret))))
                    {
                        Drawing.DrawCircle(eturret.Position, 300 + eturret.BoundingRadius, Color.Sienna);
                    }
                }
            }
        }

        private static void PermaActive(EventArgs args)
        {
            // also need a check to jump with W if target is killable by E already and its on them, and or if R is ready ---- everything works when all in seperate foreach's..
            if (R.IsReady())
            {
                /* E+R ks */
                if (Menu.Config.Item("ks.er").GetValue<bool>())
                {
                    foreach (var enemy in from enemy in HeroManager.Enemies.Where(e => R.CanCast(e))
                        let etargetstacks = enemy.Buffs.Find(buff => buff.Name == "tristanaecharge")
                        where R.GetDamage(enemy) + E.GetDamage(enemy) + etargetstacks?.Count*0.30*E.GetDamage(enemy) >=
                              enemy.Health
                        select enemy)
                    {
                        R.CastOnUnit(enemy);
                        return;
                    }
                }
                /* R KS */
                if (Menu.Config.Item("ks.r").GetValue<bool>())
                {
                    foreach (
                        var enemy in
                            HeroManager.Enemies.Where(e => R.CanCast(e))
                                .Where(enemy => R.GetDamage(enemy) >= enemy.Health))
                    {
                        R.CastOnUnit(enemy);
                        return;
                    }
                }
            }

            if (W.IsReady() && Menu.Config.Item("ks.w").IsActive())
            {
                if (Menu.Config.Item("ks.w.setting1").GetValue<bool>() && Player.Mana < W.ManaCost*2) return;
                if (Menu.Config.Item("ks.w.setting2").GetValue<bool>() &&
                    Player.CountAlliesInRange(1000) < Player.CountEnemiesInRange(1500) - 1) return;
                if (Menu.Config.Item("ks.w.setting3").GetValue<Slider>().Value > Player.HealthPercent) return;
                if (Menu.Config.Item("ks.w.setting5").GetValue<bool>() && Menu.Config.Item("ks.r").GetValue<bool>() && R.IsReady() && Utils.GameTimeTickCount -R.LastCastAttemptT > 1000) return;

                /* solo W damage with E buff amp*/
                foreach (var enemy in from enemy in HeroManager.Enemies.Where(e => W.CanCast(e))
                                      where enemy.HasBuff("tristanaecharge")
                                      let etargetbuff = enemy.Buffs.Find(buff => buff.Name == "tristanaecharge")
                                      where (W.GetDamage(enemy) + ((etargetbuff.Count * 0.25) * W.GetDamage(enemy)) >=
                                             enemy.Health)
                                      select enemy)
                {

                    if (Menu.Config.Item("ks.w.setting0").GetValue<bool>() && enemy.UnderTurret(true)) return;

                    var etargetstacks = enemy.Buffs.Find(buff => buff.Name == "tristanaecharge");
                    if (Menu.Config.Item("ks.w.setting4").GetValue<bool>() && E.GetDamage(enemy) + etargetstacks?.Count*0.30*E.GetDamage(enemy) > enemy.Health) return;

                    delay = ((1000 * Player.ServerPosition.Distance(enemy.ServerPosition)) / 1500) + 500 +
                            Game.Ping;

                    if (W.Cast(enemy).IsCasted())
                    {
                        switch (Menu.Config.Item("ks.w.jumpmouse").GetValue<StringList>().SelectedIndex)
                        {
                            case 1:
                                Utility.DelayAction.Add((int)delay, () => W.Cast(Game.CursorPos));
                                return;
                            case 2:
                                Utility.DelayAction.Add((int)delay, () => W.Cast(startpos));
                                return;
                        }
                    }
                }

                /* W dmg + E dmg*/
                foreach (var enemy in from enemy in HeroManager.Enemies.Where(e => W.CanCast(e))
                                      where enemy.HasBuff("tristanaecharge")
                                      let etargetbuff = enemy.Buffs.Find(buff => buff.Name == "tristanaecharge")
                                      let timedbuff = enemy.Buffs.Find(buff => buff.Name == "tristanaechargesound")
                                      where (Math.Abs(timedbuff.EndTime - Game.Time) * 1000 <= delay ||
                            etargetbuff.Count == 4)
                                      where ((W.GetDamage(enemy) + ((etargetbuff.Count * 0.25) * W.GetDamage(enemy)))
                                + (E.GetDamage(enemy) + ((etargetbuff.Count * 0.30) * E.GetDamage(enemy)))
                                >= enemy.Health)
                                      select enemy)
                {
                    if (Menu.Config.Item("ks.w.setting0").GetValue<bool>() && enemy.UnderTurret(true)) return;

                    var etargetstacks = enemy.Buffs.Find(buff => buff.Name == "tristanaecharge");
                    if (Menu.Config.Item("ks.w.setting4").GetValue<bool>() && E.GetDamage(enemy) + etargetstacks?.Count * 0.30 * E.GetDamage(enemy) > enemy.Health) return;

                    delay = ((1000 * Player.ServerPosition.Distance(enemy.ServerPosition)) / 1500) + 500 + Game.Ping;

                    if (W.Cast(enemy).IsCasted())
                    {
                        switch (Menu.Config.Item("ks.w.jumpmouse").GetValue<StringList>().SelectedIndex)
                        {
                            case 1:
                                Utility.DelayAction.Add((int)delay, () => W.Cast(Game.CursorPos));
                                return;
                            case 2:
                                Utility.DelayAction.Add((int)delay, () => W.Cast(startpos));
                                return;
                        }
                    }

                }

                /* solo W damage no E */
                foreach (var enemy in HeroManager.Enemies.Where(e => W.CanCast(e)).Where(enemy => enemy.Health < W.GetDamage(enemy)))
                {
                    if (Menu.Config.Item("ks.w.setting0").GetValue<bool>() && enemy.UnderTurret(true)) return;

                    var etargetstacks = enemy.Buffs.Find(buff => buff.Name == "tristanaecharge");
                    if (Menu.Config.Item("ks.w.setting4").GetValue<bool>() && E.GetDamage(enemy) + etargetstacks?.Count * 0.30 * E.GetDamage(enemy) > enemy.Health) return;

                    delay = ((1000 * Player.ServerPosition.Distance(enemy.ServerPosition)) / 1500) + 500 + Game.Ping;

                    if (W.Cast(enemy).IsCasted())
                    {
                        switch (Menu.Config.Item("ks.w.jumpmouse").GetValue<StringList>().SelectedIndex)
                        {
                            case 1:
                                Utility.DelayAction.Add((int)delay, () => W.Cast(Game.CursorPos));
                                return;
                            case 2:
                                Utility.DelayAction.Add((int)delay, () => W.Cast(startpos));
                                return;
                        }
                    }
                }


            }

            //foreach (var enemy in HeroManager.Enemies.Where(e => e.Distance(Player) <= W.Range && e != null && e.IsValidTarget()))
            //{
            //    if (R.IsReady())
            //    {
            //        if (Menu.Config.Item("ks.er").GetValue<bool>())
            //        {
            //            var etargetstacks = enemy.Buffs.Find(buff => buff.Name == "tristanaecharge");
            //            if (etargetstacks == null) continue;
            //            if (R.GetDamage(enemy) + E.GetDamage(enemy) + (((etargetstacks.Count*0.30))*E.GetDamage(enemy)) >=
            //                enemy.Health)
            //            {
            //                Game.PrintChat("RE Cast");
            //                R.CastOnUnit(enemy);
            //                break;
            //            }
            //        }
            //         //only hits here if ks.re is off, probably same issue for W ksing. mess with continue/break/return
            //        if (Menu.Config.Item("ks.r").GetValue<bool>())
            //        {
            //            if (R.GetDamage(enemy) >= enemy.Health)
            //            {
            //                Game.PrintChat("R cast");
            //                R.CastOnUnit(enemy);
            //                break;
            //            }
            //        }
            //    }

            //    if (W.IsReady())
            //    {
            //        if (Menu.Config.Item("ks.w").IsActive())
            //        {
            //            if (Menu.Config.Item("ks.w.setting1").IsActive() && Player.Mana < W.ManaCost*2) continue;
            //            if (Player.HealthPercent < Menu.Config.Item("ks.w.setting3").GetValue<Slider>().Value) continue;

            //            if (enemy.HasBuff("tristanaecharge"))
            //            {
            //                var etarget = enemy;

            //                var etargetbuff = etarget?.Buffs.Find(buff => buff.Name == "tristanaecharge");
            //                var timedbuff = etarget?.Buffs.Find(buff => buff.Name == "tristanaechargesound");

            //                if (etarget != null)
            //                {
            //                    if (Menu.Config.Item("ks.w.setting2").IsActive() &&
            //                        Player.CountAlliesInRange(1000) < etarget.CountEnemiesInRange(1000))
            //                        break;

            //                    if ((W.GetDamage(etarget) + ((etargetbuff.Count * 0.25) * W.GetDamage(etarget)) >=
            //                         etarget.Health))
            //                    {
            //                        delay = ((1000 * Player.ServerPosition.Distance(etarget.ServerPosition)) / 1500) + 500 +
            //                                Game.Ping;
            //                        if (W.Cast(etarget).IsCasted())
            //                        {
            //                            switch (Menu.Config.Item("ks.w.jumpmouse").GetValue<StringList>().SelectedIndex)
            //                            {
            //                                case 1:
            //                                    Utility.DelayAction.Add((int)delay, () => W.Cast(Game.CursorPos));
            //                                    break;
            //                                case 2:
            //                                    Utility.DelayAction.Add((int)delay, () => W.Cast(startpos));
            //                                    break;
            //                            }
            //                        }
            //                    }

            //                    if ((W.GetDamage(etarget) + ((etargetbuff.Count * 0.25) * W.GetDamage(etarget)))
            //                        + (E.GetDamage(etarget) + ((etargetbuff.Count * 0.30) * E.GetDamage(etarget)))
            //                        >= etarget.Health)
            //                    {
            //                        delay = ((1000 * Player.ServerPosition.Distance(etarget.ServerPosition)) / 1500) + 500 +
            //                                Game.Ping;
            //                        if (System.Math.Abs(timedbuff.EndTime - Game.Time) * 1000 <= delay ||
            //                            etargetbuff.Count == 4)
            //                        {
            //                            if (W.Cast(etarget).IsCasted())
            //                            {
            //                                switch (Menu.Config.Item("ks.w.jumpmouse").GetValue<StringList>().SelectedIndex)
            //                                {
            //                                    case 1:
            //                                        Utility.DelayAction.Add((int)delay, () => W.Cast(Game.CursorPos));
            //                                        break;
            //                                    case 2:
            //                                        Utility.DelayAction.Add((int)delay, () => W.Cast(startpos));
            //                                        break;
            //                                }
            //                            }
            //                        }
            //                    }
            //                }
            //            }


            //            if (enemy != null)
            //            {
            //                if (Menu.Config.Item("ks.w.setting2").IsActive() &&
            //                    Player.CountAlliesInRange(1000) < enemy.CountEnemiesInRange(1000))
            //                    return;

            //                if ((W.GetDamage(enemy) >= enemy.Health))
            //                {
            //                    if (W.Cast(enemy).IsCasted())
            //                    {
            //                        delay = ((1000*Player.ServerPosition.Distance(enemy.ServerPosition))/1500) + 500 +
            //                                Game.Ping;
            //                        switch (Menu.Config.Item("ks.w.jumpmouse").GetValue<StringList>().SelectedIndex)
            //                        {
            //                            case 0:
            //                                return;
            //                            case 1:
            //                                Utility.DelayAction.Add((int) delay, () => W.Cast(Game.CursorPos));
            //                                return;
            //                            case 2:
            //                                Utility.DelayAction.Add((int) delay, () => W.Cast(startpos));
            //                                return;
            //                        }
            //                    }
            //                }
            //            }
            //        }
            //    }
            //}
        }

        private static void CustomOrbwalker_BeforeAttack(CustomOrbwalker.BeforeAttackEventArgs args)
        {
            _target = args.Target.Type == GameObjectType.obj_AI_Hero ? (Obj_AI_Hero) args.Target : null;

            if (Menu.Orbwalker.ActiveMode == CustomOrbwalker.OrbwalkingMode.Combo)
            {
                if (_target != null && E.IsReady() && Menu.Config.Item(_target.ChampionName + "e").GetValue<bool>() &&
                    _target.Distance(Player) >= Player.AttackRange)
                {
                    args.Process = false;
                }


                if (_target != null && Q.IsReady() && Menu.Config.Item(_target.ChampionName + "q").GetValue<bool>())
                {
                    if (Menu.Config.Item("q.onlye").GetValue<bool>() && !_target.HasBuff("tristanaechargesound"))
                    {
                        return;
                    }

                    Q.Cast();
                }
            }
            else if (Menu.Orbwalker.ActiveMode == CustomOrbwalker.OrbwalkingMode.Mixed)
            {
                if (_target != null && E.IsReady() && Menu.Config.Item(_target.ChampionName + "e.harass").GetValue<bool>() &&
                    _target.Distance(Player) >= Player.AttackRange)
                {
                    args.Process = false;
                }


                if (_target != null && Q.IsReady() && Menu.Config.Item(_target.ChampionName + "q.harass").GetValue<bool>())
                {
                    if (Menu.Config.Item("q.onlye").GetValue<bool>() && !_target.HasBuff("tristanaechargesound"))
                    {
                        return;
                    }

                    Q.Cast();
                }
            }


            if (Menu.Config.Item("e.force.target").GetValue<bool>() &&
                Menu.Orbwalker.ActiveMode == CustomOrbwalker.OrbwalkingMode.Combo ||
                Menu.Config.Item("e.force.target.harass").GetValue<bool>() &&
                Menu.Orbwalker.ActiveMode == CustomOrbwalker.OrbwalkingMode.Mixed)
            {
                foreach (var enemy in HeroManager.Enemies.Where(enemy => enemy.HasBuff("tristanaechargesound")))
                {
                    TargetSelector.SetTarget(enemy);
                    return;
                }
                TargetSelector.SetTarget(TargetSelector.GetTarget(Orbwalking.GetRealAutoAttackRange(Player),
                    TargetSelector.DamageType.Physical));
            }
        }

        private static void Obj_AI_Base_OnLevelUp(Obj_AI_Base sender, EventArgs args)
        {
            if (!sender.IsMe) return;
            var lvl = (7*(Player.Level - 1));
            Q.Range = 605 + lvl;
            E.Range = 635 + lvl;
            R.Range = 635 + lvl;
        }

        private static void InitSpells()
        {
            Q = new Spell(SpellSlot.Q);
            W = new Spell(SpellSlot.W, 1035);
            E = new Spell(SpellSlot.E, 630);
            R = new Spell(SpellSlot.R, 630);

            W.SetSkillshot(0.50f, 270, 1500, false, SkillshotType.SkillshotCircle);
        }

        public static void Combo()
        {
            var target = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical);
            if (target == null || !target.IsValidTarget()) return;

            if (E.IsReady() && E.IsInRange(target) && Menu.Config.Item(target.ChampionName + "e").GetValue<bool>())
            {
                E.CastOnUnit(target);
            }

            if (R.IsReady())
            {
                if (Menu.Config.Item("r.selfpeel").GetValue<Slider>().Value != 0 &&
                    Player.HealthPercent <= Menu.Config.Item("r.selfpeel").GetValue<Slider>().Value)
                {
                    var peel =
                        HeroManager.Enemies.Where(e => R.CanCast(e))
                            .OrderBy(enemy => enemy.Distance(Player))
                            .FirstOrDefault();
                    if (peel != null)
                    {
                        R.CastOnUnit(peel);
                    }
                }
            }
        }

        public static void LaneClear()
        {
            if (Menu.Config.Item("e.tower.mana").GetValue<Slider>().Value > Player.ManaPercent) return;
            var tower = (Obj_AI_Base) ObjectManager.Get<Obj_AI_Turret>().FirstOrDefault(E.CanCast);
            if (tower == null) return;
            {
                E.CastOnUnit(tower);
            }
        }

        public static void Harass()
        {
            if (Menu.Config.Item("e.quick.harass").IsActive())
            {
                foreach (
                    var minion in
                        MinionManager.GetMinions(E.Range)
                            .Where(
                                m =>
                                    E.CanCast(m) && m.Health < Player.GetAutoAttackDamage(m) &&
                                    m.CountEnemiesInRange(m.BoundingRadius + 150) >= 1))
                {
                    var etarget = E.GetTarget();
                    if (etarget != null) return;

                    E.CastOnUnit(minion);
                    Menu.Orbwalker.ForceTarget(minion);
                }
            }

            var target = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical);
            if (target == null || !target.IsValidTarget()) return;

            if (E.IsReady() && E.IsInRange(target) && Menu.Config.Item(target.ChampionName + "e.harass").GetValue<bool>())
            {
                E.CastOnUnit(target);
            }

        }

        private static void InitMenu()
        {
            Menu.Config.AddSubMenu(new LeagueSharp.Common.Menu("Tristana", "adcpackage.tristana"));


            //
            // todo: COMBO
            //

            var comboMenu =
                Menu.Config.SubMenu("adcpackage.tristana")
                    .AddSubMenu(new LeagueSharp.Common.Menu("Combo Menu", "combo"));
            {
                comboMenu.Color = SharpDX.Color.MediumVioletRed;
            }

            comboMenu.AddItem(new MenuItem("q.settings", "Q Settings"))
                .SetFontStyle(FontStyle.Regular, SharpDX.Color.Yellow);
            comboMenu.AddItem(new MenuItem("q.attack.list", "Use Q when attacking:"));
            foreach (var enemy in HeroManager.Enemies)
            {
                comboMenu.AddItem(new MenuItem(enemy.ChampionName + "q", "   " + enemy.ChampionName).SetValue(true));
            }
            comboMenu.AddItem(new MenuItem(Environment.TickCount.ToString(), "Show list for Q"))
                .SetValue(true)
                .ValueChanged +=
                delegate(object sender, OnValueChangeEventArgs args)
                {
                    if (args.GetNewValue<bool>())
                    {
                        foreach (var enemy in HeroManager.Enemies)
                        {
                            comboMenu.Item(enemy.ChampionName + "q").Show();
                        }
                    }
                    else
                    {
                        foreach (var enemy in HeroManager.Enemies)
                        {
                            comboMenu.Item(enemy.ChampionName + "q").Show(false);
                        }
                    }
                };
            comboMenu.AddItem(new MenuItem("q.onlye", "Use Q only against E").SetValue(false));


            comboMenu.AddItem(new MenuItem("e.settings", "E Settings"))
                .SetFontStyle(FontStyle.Regular, SharpDX.Color.Yellow);
            comboMenu.AddItem(new MenuItem("e.attack.list", "Use E against:"));
            foreach (var enemy in HeroManager.Enemies)
            {
                comboMenu.AddItem(new MenuItem(enemy.ChampionName + "e", "   " + enemy.ChampionName).SetValue(true));
            }
            comboMenu.AddItem(new MenuItem(Environment.TickCount.ToString() + 1, "Show list for E"))
                .SetValue(true)
                .ValueChanged +=
                delegate(object sender, OnValueChangeEventArgs args)
                {
                    if (args.GetNewValue<bool>())
                    {
                        foreach (var enemy in HeroManager.Enemies)
                        {
                            comboMenu.Item(enemy.ChampionName + "e").Show();
                        }
                    }
                    else
                    {
                        foreach (var enemy in HeroManager.Enemies)
                        {
                            comboMenu.Item(enemy.ChampionName + "e").Show(false);
                        }
                    }
                };
            comboMenu.AddItem(new MenuItem("e.force.target", "Force focus E target").SetValue(true));


            comboMenu.AddItem(new MenuItem("r.settings", "R Settings"))
                .SetFontStyle(FontStyle.Regular, SharpDX.Color.Yellow);
            comboMenu.AddItem(new MenuItem("r.selfpeel", "Self peel with R when health < X%").SetValue(new Slider(35)))
                .SetTooltip("Set to 0 to disable", SharpDX.Color.Yellow);

            // r agc, interrupt, + e ks, ks, gapclose


            //
            // todo: HARASS
            //


            var harassMenu =
                Menu.Config.SubMenu("adcpackage.tristana")
                    .AddSubMenu(new LeagueSharp.Common.Menu("Harass Menu", "harass"));
            {
                harassMenu.Color = SharpDX.Color.MediumVioletRed;
            }

            harassMenu.AddItem(new MenuItem("q.settings.harass", "Q Settings"))
                .SetFontStyle(FontStyle.Regular, SharpDX.Color.Yellow);
            harassMenu.AddItem(new MenuItem("q.attack.list.harass", "Use Q when attacking:"));
            foreach (var enemy in HeroManager.Enemies)
            {
                harassMenu.AddItem(new MenuItem(enemy.ChampionName + "q.harass", "   " + enemy.ChampionName).SetValue(true));
            }
            harassMenu.AddItem(new MenuItem(Environment.TickCount.ToString() + 2, "Show list for Q"))
                .SetValue(true)
                .ValueChanged +=
                delegate (object sender, OnValueChangeEventArgs args)
                {
                    if (args.GetNewValue<bool>())
                    {
                        foreach (var enemy in HeroManager.Enemies)
                        {
                            harassMenu.Item(enemy.ChampionName + "q.harass").Show();
                        }
                    }
                    else
                    {
                        foreach (var enemy in HeroManager.Enemies)
                        {
                            harassMenu.Item(enemy.ChampionName + "q.harass").Show(false);
                        }
                    }
                };
            harassMenu.AddItem(new MenuItem("q.onlye.harass", "Use Q only against E").SetValue(false));


            harassMenu.AddItem(new MenuItem("e.settings.harass", "E Settings"))
                .SetFontStyle(FontStyle.Regular, SharpDX.Color.Yellow);
            harassMenu.AddItem(new MenuItem("e.attack.list.harass", "Use E against:"));
            foreach (var enemy in HeroManager.Enemies)
            {
                harassMenu.AddItem(new MenuItem(enemy.ChampionName + "e.harass", "   " + enemy.ChampionName).SetValue(true));
            }
            harassMenu.AddItem(new MenuItem(Environment.TickCount.ToString() + 3, "Show list for E"))
                .SetValue(true)
                .ValueChanged +=
                delegate (object sender, OnValueChangeEventArgs args)
                {
                    if (args.GetNewValue<bool>())
                    {
                        foreach (var enemy in HeroManager.Enemies)
                        {
                            harassMenu.Item(enemy.ChampionName + "e.harass").Show();
                        }
                    }
                    else
                    {
                        foreach (var enemy in HeroManager.Enemies)
                        {
                            harassMenu.Item(enemy.ChampionName + "e.harass").Show(false);
                        }
                    }
                };
            harassMenu.AddItem(new MenuItem("e.quick.harass", "E Quick Harass").SetValue(true))
                .SetTooltip("Will E minion one auto from dying if enemy in explosion radius, then auto minion");
            harassMenu.AddItem(new MenuItem("e.force.target.harass", "Force focus E target").SetValue(true));


            //
            // todo: LANECLEAR
            //

            var laneclearMenu =
                Menu.Config.SubMenu("adcpackage.tristana")
                    .AddSubMenu(new LeagueSharp.Common.Menu("Laneclear Menu", "laneclear"));

            laneclearMenu.AddItem(new MenuItem("e.tower", "E Turrets").SetValue(true));
            laneclearMenu.AddItem(new MenuItem("e.tower.mana", "Minimum mana").SetValue(new Slider(35)));
            {
                laneclearMenu.Color = SharpDX.Color.LightGoldenrodYellow;
            }

            //
            // todo: EXTRAS
            //

            var extrasMenu =
                Menu.Config.SubMenu("adcpackage.tristana")
                    .AddSubMenu(new LeagueSharp.Common.Menu("Extras Menu", "extras"));
            {
                extrasMenu.Color = SharpDX.Color.Aquamarine;
            }
            foreach (
                var gapcloser in
                    HeroManager.Enemies.SelectMany(
                        enemy => AntiGapcloser.Spells.Where(g => g.ChampionName == enemy.ChampionName)))
            {
                extrasMenu.SubMenu("Anti-Gapcloser on:")
                    .AddItem(new MenuItem(gapcloser.ChampionName, gapcloser.ChampionName)).SetValue(true);
            }
            foreach (
                var interrupter in
                    HeroManager.Enemies.SelectMany(
                        enemy => Interrupter.Spells.Where(g => g.ChampionName == enemy.ChampionName)))
            {
                extrasMenu.SubMenu("Interrupt:")
                    .AddItem(new MenuItem(interrupter.ChampionName, interrupter.ChampionName)).SetValue(true);
            }

            extrasMenu.AddItem(new MenuItem("ks.settings", "Killsteal settings"))
                .SetFontStyle(FontStyle.Regular, SharpDX.Color.Yellow);
            extrasMenu.AddItem(new MenuItem("ks.w", "KS with W")).SetValue(false);
            extrasMenu.AddItem(new MenuItem("ks.w.setting0", "   if not under tower")).SetValue(true);
            extrasMenu.AddItem(new MenuItem("ks.w.setting1", "   if mana to jump out")).SetValue(true);
            extrasMenu.AddItem(new MenuItem("ks.w.setting2", "   if ally >= enemy count")).SetValue(true);
            extrasMenu.AddItem(new MenuItem("ks.w.setting3", "   if hp % above")).SetValue(new Slider(50)).SetTooltip("Set to 0 to disable", SharpDX.Color.Yellow);
            extrasMenu.AddItem(new MenuItem("ks.w.setting4", "   if E will not already kill")).SetValue(true);
            extrasMenu.AddItem(new MenuItem("ks.w.setting5", "   if R KS is ON and R on CD")).SetValue(true);
            extrasMenu.AddItem(new MenuItem("ks.w.jumpmouse", "         Jump to X after:"))
                .SetValue(new StringList(new[] {"None", "Mouse", "Previous location"}, 1));
            extrasMenu.AddItem(new MenuItem("ks.er", "KS with R + E damage")).SetValue(true);
            extrasMenu.AddItem(new MenuItem("ks.r", "KS with R")).SetValue(true);


            //
            // todo: DRAWINGS
            //


            var drawingsMenu =
                Menu.Config.SubMenu("adcpackage.tristana")
                    .AddSubMenu(new LeagueSharp.Common.Menu("Drawings Menu", "drawings"));
            drawingsMenu.AddItem(new MenuItem("draw.explosion", "Draw E explosion radius"))
                .SetTooltip("On champs and towers only.")
                .SetValue(true);
        }
    }
}