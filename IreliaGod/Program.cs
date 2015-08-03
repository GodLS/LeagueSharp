using System;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;

namespace IreliaGod
{
    internal class Program
    {
        private static readonly Obj_AI_Hero Player = ObjectManager.Player;
        public static Orbwalking.Orbwalker Orbwalker;
        private static int lastsheenproc;
        private static int rcount;

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += OnGameLoad;
        }

        private static void OnGameLoad(EventArgs args)
        {
            // Only load on Irelia, silly
            if (Player.CharData.BaseSkinName != "Irelia") return;

            // Say hello
            Game.PrintChat("[Irelia<font color='#79BAEC'>God</font>]: <font color='#FFFFFF'>" + "Loaded!</font>");

            // Initialize our menu
            IreliaMenu.Initialize();

            // Initialize our spells
            Spells.Initialize();

            // Subscribe to our events
            Game.OnUpdate += OnUpdate;
            Orbwalking.BeforeAttack += BeforeAttack;
            Drawing.OnDraw += OnDraw;
            Obj_AI_Base.OnBuffRemove += OnBuffRemove; // Sheen buff workaround
            AntiGapcloser.OnEnemyGapcloser += OnEnemyGapcloser;
            Interrupter2.OnInterruptableTarget += OnInterruptableTarget;
        }

        private static void OnInterruptableTarget(Obj_AI_Hero sender, Interrupter2.InterruptableTargetEventArgs args)
        {
            if (sender == null) return;
            if (IreliaMenu.Config.Item("misc.interrupt").GetValue<bool>() && sender.IsValidTarget(Spells.E.Range) &&
                Spells.E.IsReady() && Player.HealthPercent <= sender.HealthPercent)
                Spells.E.CastOnUnit(sender);
        }

        private static void OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (gapcloser.Sender == null) return;
            if (IreliaMenu.Config.Item("misc.age").GetValue<bool>() && Spells.E.IsReady() &&
                gapcloser.Sender.IsValidTarget())
            {
                Spells.E.Cast(gapcloser.Sender);
            }
        }

        public static float ComboDamage(Obj_AI_Hero hero) // Thanks honda
        {
            var result = 0d;

            if (Spells.Q.IsReady())
            {
                result += QDamage(hero) + ExtraWDamage() + SheenDamage(hero);
            }
            if (Spells.W.IsReady() || Player.HasBuff("ireliahitenstylecharged"))
            {
                result += (ExtraWDamage() +
                           Player.CalcDamage(hero, Damage.DamageType.Physical, Player.TotalAttackDamage))*3; // 3 autos
            }
            if (Spells.E.IsReady())
            {
                result += Spells.E.GetDamage(hero);
            }
            if (Spells.R.IsReady())
            {
                result += Spells.R.GetDamage(hero)*rcount;
            }

            return (float) result;
        }

        private static void OnBuffRemove(Obj_AI_Base sender, Obj_AI_BaseBuffRemoveEventArgs args)
        {
            if (sender.IsMe && args.Buff.Name == "sheen")
                lastsheenproc = Utils.TickCount;
        }

        private static void RCount()
        {
            if (rcount == 0 && Spells.R.IsReady())
                rcount = 4;

            if (rcount == 1 && !Spells.R.IsReady())
                rcount = 0;

            foreach (
                var buff in
                    Player.Buffs.Where(b => b.Name == "ireliatranscendentbladesspell" && b.IsValid && b.IsActive))
            {
                rcount = buff.Count;
            }
        }

        private static void OnUpdate(EventArgs args)
        {
            Killsteal();
            switch (Orbwalker.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.Combo:
                    Combo();
                    break;
                case Orbwalking.OrbwalkingMode.Mixed:
                    Harass();
                    break;
                case Orbwalking.OrbwalkingMode.LaneClear:
                    Laneclear();
                    Jungleclear();
                    break;
                case Orbwalking.OrbwalkingMode.LastHit:
                    break;
                case Orbwalking.OrbwalkingMode.None:
                    if (IreliaMenu.Config.Item("flee").GetValue<KeyBind>().Active)
                    {
                        if (Spells.E.IsReady() && IreliaMenu.Config.Item("flee.e").GetValue<bool>())
                        {
                            var etarget =
                                HeroManager.Enemies
                                    .FindAll(
                                        enemy =>
                                            enemy.IsValidTarget() && Player.Distance(enemy.Position) <= Spells.E.Range)
                                    .OrderBy(e => e.Distance(Player));

                            if (etarget.FirstOrDefault() != null)
                                Spells.E.CastOnUnit(etarget.FirstOrDefault());
                        }

                        if (Spells.R.IsReady() && IreliaMenu.Config.Item("flee.r").GetValue<bool>())
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

                        if (Spells.Q.IsReady() && IreliaMenu.Config.Item("flee.q").GetValue<bool>())
                        {
                            var target =
                                HeroManager.Enemies
                                    .FindAll(
                                        enemy =>
                                            enemy.IsValidTarget() && Player.Distance(enemy.Position) <= Spells.R.Range)
                                    .MinOrDefault(e => e.Distance(Player) <= Spells.R.Range);

                            if (target == null) goto WALK;

                            var qminion =
                                MinionManager
                                    .GetMinions(Spells.Q.Range, MinionTypes.All, MinionTeam.NotAlly)
                                    .Where(
                                        m =>
                                            m.Distance(Player) <= Spells.Q.Range &&
                                            m.Distance(target) > Player.Distance(target) && m.IsValidTarget())
                                    .MaxOrDefault(m => m.Distance(target) <= Spells.Q.Range);

                            if (qminion != null)
                                Spells.Q.CastOnUnit(qminion);
                        }

                        WALK:
                        Orbwalking.MoveTo(Game.CursorPos);
                    }
                    break;
            }
            RCount();
        }

        private static void Combo()
        {
            var gctarget = TargetSelector.GetTarget(Spells.Q.Range*2.5f, TargetSelector.DamageType.Physical);
            var target = TargetSelector.GetTarget(Spells.Q.Range, TargetSelector.DamageType.Physical);
            if (gctarget == null) return;

            var qminion =
                MinionManager
                    .GetMinions(Spells.Q.Range + 350, MinionTypes.All, MinionTeam.NotAlly) //added 350 range, bad?
                    .Where(
                        m =>
                            m.Distance(Player) <= Spells.Q.Range &&
                            m.Health <= QDamage(m) + ExtraWDamage() + SheenDamage(m) && m.IsValidTarget())
                    .OrderBy(m => m.Distance(gctarget.Position) <= Spells.Q.Range + 350)
                    .FirstOrDefault();


            if (Spells.Q.IsReady())
            {
                if (IreliaMenu.Config.Item("combo.q.gc").GetValue<bool>() &&
                    gctarget.Distance(Player.Position) >= Orbwalking.GetRealAutoAttackRange(gctarget) && qminion != null &&
                    qminion.Distance(gctarget.Position) <= Player.Distance(gctarget.Position) &&
                    qminion.Distance(Player.Position) <= Spells.Q.Range)
                {
                    Spells.Q.CastOnUnit(qminion);
                }

                if (IreliaMenu.Config.Item("combo.q").GetValue<bool>() && target != null &&
                    target.Distance(Player.Position) <= Spells.Q.Range &&
                    target.Distance(Player.Position) >=
                    IreliaMenu.Config.Item("combo.q.minrange").GetValue<Slider>().Value)
                {
                    if (IreliaMenu.Config.Item("combo.w").GetValue<bool>())
                        Spells.W.Cast();

                    Spells.Q.CastOnUnit(target);
                }
            }

            if (Spells.E.IsReady() && IreliaMenu.Config.Item("combo.e").GetValue<bool>() && target != null)
            {
                if (IreliaMenu.Config.Item("combo.e.logic").GetValue<bool>() &&
                    target.Distance(Player.Position) <= Spells.E.Range)
                {
                    if (target.HealthPercent >= Player.HealthPercent)
                    {
                        Spells.E.CastOnUnit(target);
                    }
                    else if (!target.IsFacing(Player) && target.Distance(Player.Position) >= Spells.E.Range/2)
                    {
                        Spells.E.CastOnUnit(target);
                    }
                }
                else if (target.Distance(Player.Position) <= Spells.E.Range)
                {
                    Spells.E.CastOnUnit(target);
                }
            }

            if (Spells.R.IsReady() && IreliaMenu.Config.Item("combo.r").GetValue<bool>())
            {
                if (IreliaMenu.Config.Item("combo.r.weave").GetValue<bool>())
                {
                    if (target != null && !Player.HasBuff("sheen") &&
                        target.Distance(Player.Position) <= Orbwalking.GetRealAutoAttackRange(target) + 100 &&
                        Utils.TickCount - lastsheenproc >= 1500)
                    {
                        Spells.R.Cast(target, false, true);
                    }
                    else if (gctarget.Distance(Player.Position) <= Spells.R.Range)
                    {
                        Spells.R.Cast(gctarget, false, true);
                    }
                }
                else
                {
                    Spells.R.Cast(target, false, true);
                        // Set to Q range because we are already going to combo them at this point most likely, no stupid long range R initiations
                }
            }
        }

        private static void Harass()
        {
            if (Player.ManaPercent <= IreliaMenu.Config.Item("harass.mana").GetValue<Slider>().Value) return;
            var gctarget = TargetSelector.GetTarget(Spells.Q.Range*2.5f, TargetSelector.DamageType.Physical);
            var target = TargetSelector.GetTarget(Spells.Q.Range*2, TargetSelector.DamageType.Physical);
            if (gctarget == null) return;

            var qminion =
                MinionManager
                    .GetMinions(Spells.Q.Range, MinionTypes.All, MinionTeam.NotAlly)
                    .Where(
                        m =>
                            m.Distance(Player) <= Spells.Q.Range && m.Health <= Spells.Q.GetDamage(m) &&
                            m.IsValidTarget())
                    .MinOrDefault(m => m.Distance(target) <= Spells.Q.Range);

            if (Spells.Q.IsReady() && target.Distance(Player.Position) >= Orbwalking.GetRealAutoAttackRange(target))
            {
                if (IreliaMenu.Config.Item("harass.q.gc").GetValue<bool>() && qminion != null &&
                    qminion.Distance(target) <= Player.Distance(target))
                {
                    Spells.Q.CastOnUnit(qminion);
                }

                if (IreliaMenu.Config.Item("harass.q").GetValue<bool>() && target != null &&
                    target.Distance(Player.Position) <= Spells.Q.Range &&
                    target.Distance(Player.Position) >=
                    IreliaMenu.Config.Item("harass.q.minrange").GetValue<Slider>().Value)
                {
                    Spells.Q.CastOnUnit(target);
                }
            }

            if (Spells.E.IsReady() && IreliaMenu.Config.Item("harass.e").GetValue<bool>() && target != null)
            {
                if (IreliaMenu.Config.Item("harass.e.logic").GetValue<bool>() &&
                    target.Distance(Player.Position) <= Spells.E.Range)
                {
                    if (target.HealthPercent >= Player.HealthPercent)
                    {
                        Spells.E.CastOnUnit(target);
                    }
                    else if (!target.IsFacing(Player) && target.Distance(Player.Position) >= Spells.E.Range/2)
                    {
                        Spells.E.CastOnUnit(target);
                    }
                }
                else if (target.Distance(Player.Position) <= Spells.E.Range)
                {
                    Spells.E.CastOnUnit(target);
                }
            }

            if (Spells.R.IsReady() && IreliaMenu.Config.Item("harass.r").GetValue<bool>())
            {
                if (IreliaMenu.Config.Item("harass.r.weave").GetValue<bool>())
                {
                    if (target != null && !Player.HasBuff("sheen") &&
                        target.Distance(Player.Position) <= Orbwalking.GetRealAutoAttackRange(target) + 100 &&
                        Utils.TickCount - lastsheenproc >= 1500)
                    {
                        Spells.R.Cast(target, false, true);
                    }
                    else if (gctarget.Distance(Player.Position) <= Spells.R.Range)
                    {
                        Spells.R.Cast(gctarget, false, true);
                    }
                }
                else
                {
                    Spells.R.Cast(target, false, true);
                        // Set to Q range because we are already going to combo them at this point most likely, no stupid long range R initiations
                }
            }
        }

        private static void Killsteal()
        {
            foreach (
                var enemy in
                    HeroManager.Enemies.Where(e => e.Distance(Player.Position) <= Spells.R.Range && e.IsValidTarget()))
            {
                if (enemy == null) return;

                if (IreliaMenu.Config.Item("misc.ks.q").GetValue<bool>() && Spells.Q.IsReady() &&
                    QDamage(enemy) + ExtraWDamage() + SheenDamage(enemy) >= enemy.Health &&
                    enemy.Distance(Player.Position) <= Spells.Q.Range)
                {
                    Spells.Q.CastOnUnit(enemy);
                    return;
                }

                if (IreliaMenu.Config.Item("misc.ks.e").GetValue<bool>() && Spells.E.IsReady() &&
                    Spells.E.GetDamage(enemy) >= enemy.Health && enemy.Distance(Player.Position) <= Spells.E.Range)
                {
                    Spells.E.CastOnUnit(enemy);
                    return;
                }

                if (IreliaMenu.Config.Item("misc.ks.r").GetValue<bool>() && Spells.R.IsReady() &&
                    Spells.R.GetDamage(enemy)*rcount >= enemy.Health)
                {
                    Spells.R.Cast(enemy, false, true);
                    return;
                }
            }
        }

        private static void Laneclear()
        {
            if (Player.ManaPercent <= IreliaMenu.Config.Item("laneclear.mana").GetValue<Slider>().Value) return;

            var qminion =
                MinionManager
                    .GetMinions(
                        Spells.Q.Range, MinionTypes.All, MinionTeam.NotAlly)
                    .FirstOrDefault(
                        m =>
                            m.Distance(Player) <= Spells.Q.Range &&
                            m.Health <= QDamage(m) + ExtraWDamage() + SheenDamage(m) - 15 &&
                            m.IsValidTarget());


            if (Spells.Q.IsReady() && IreliaMenu.Config.Item("laneclear.q").GetValue<bool>() && qminion != null)
            {
                Spells.Q.CastOnUnit(qminion);
            }

            var rminions = MinionManager.GetMinions(Player.Position, Spells.R.Range);
            if (Spells.R.IsReady() && IreliaMenu.Config.Item("laneclear.r").GetValue<bool>() && rminions.Count != 0)
            {
                var location = Spells.R.GetLineFarmLocation(rminions);

                if (location.MinionsHit >=
                    IreliaMenu.Config.Item("laneclear.r.minimum").GetValue<Slider>().Value)
                    Spells.R.Cast(location.Position);
            }
        }

        private static void Jungleclear()
        {
        }

        private static void BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            if (IreliaMenu.Config.Item("combo.w").GetValue<bool>() &&
                Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo &&
                args.Target != null &&
                args.Target.Type == GameObjectType.obj_AI_Hero &&
                args.Target.IsValidTarget() ||
                IreliaMenu.Config.Item("harass.w").GetValue<bool>() &&
                Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed &&
                args.Target != null &&
                args.Target.Type == GameObjectType.obj_AI_Hero &&
                args.Target.IsValidTarget())
                Spells.W.Cast();
        }

        private static void OnDraw(EventArgs args)
        {
            if (Player.IsDead) return;
            if (IreliaMenu.Config.Item("drawings.q").GetValue<bool>())
            {
                if (Spells.Q.IsReady())
                    Drawing.DrawCircle(Player.Position, Spells.Q.Range, Color.LightCoral);
                else
                    Drawing.DrawCircle(Player.Position, Spells.Q.Range, Color.Maroon);
            }
            if (IreliaMenu.Config.Item("drawings.e").GetValue<bool>())
            {
                if (Spells.E.IsReady())
                    Drawing.DrawCircle(Player.Position, Spells.E.Range, Color.LightCoral);
                else
                    Drawing.DrawCircle(Player.Position, Spells.E.Range, Color.Maroon);
            }
            if (IreliaMenu.Config.Item("drawings.r").GetValue<bool>())
            {
                if (Spells.R.IsReady())
                    Drawing.DrawCircle(Player.Position, Spells.R.Range, Color.LightCoral);
                else
                    Drawing.DrawCircle(Player.Position, Spells.R.Range, Color.Maroon);
            }
        }

        private static double SheenDamage(Obj_AI_Base target) // Thanks princer007
        {
            var result = 0d;
            foreach (var item in Player.InventoryItems)
                switch ((int) item.Id)
                {
                    case 3057: //Sheen
                        if (Utils.TickCount - lastsheenproc > 1500 + Game.Ping)
                            result += Player.CalcDamage(target, Damage.DamageType.Physical, Player.BaseAttackDamage);
                        break;
                    case 3078: //Triforce
                        if (Utils.TickCount - lastsheenproc > 1500 + Game.Ping)
                            result += Player.CalcDamage(target, Damage.DamageType.Physical, Player.TotalAttackDamage*1.5);
                        break;
                }
            return result;
        }

        private static double ExtraWDamage()
        {
            var extra = 0d;
            if (Player.HasBuff("ireliahitenstylecharged"))
                extra += new double[] {15, 30, 45, 60, 75}[Spells.W.Level - 1];

            return extra;
        }

        private static double QDamage(Obj_AI_Base target)
        {
            return Spells.Q.IsReady()
                ? Player.CalcDamage(
                    target,
                    Damage.DamageType.Physical,
                    new double[] {20, 50, 80, 110, 140}[Spells.Q.Level - 1]
                    + Player.TotalAttackDamage)
                //- 25) // Safety net, for some reason the damage is never exact ): why?
                : 0d;
        }
    }
}