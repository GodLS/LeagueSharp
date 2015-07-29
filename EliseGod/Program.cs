using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Collision = LeagueSharp.Common.Collision;
using Color = System.Drawing.Color;

namespace EliseGod
{
    internal class Program
    {
        public static Spell Q, W, E, R, Q1, W1, E1;
        private static readonly Menu Config = new Menu("Elise God", "Elise.God", true);
        private static readonly Obj_AI_Hero Player = ObjectManager.Player;
        private static Orbwalking.Orbwalker _orbwalker;
        private static readonly float[] HumanQcd = { 6, 6, 6, 6, 6 };
        private static readonly float[] HumanWcd = { 12, 12, 12, 12, 12 };
        private static readonly float[] HumanEcd = { 14, 13, 12, 11, 10 };
        private static readonly float[] SpiderQcd = { 6, 6, 6, 6, 6 };
        private static readonly float[] SpiderWcd = { 12, 12, 12, 12, 12 };
        private static readonly float[] SpiderEcd = { 26, 23, 20, 17, 14 };
        private static float _qCd, _wCd, _eCd;
        private static float _q1Cd, _w1Cd, _e1Cd;
        private static float realcdQ, realcdW, realcdE, realcdSQ, realcdSW, realcdSE;
        //private static Obj_AI_Minion spider;

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += OnGameLoad;
        }

        private static bool Human()
        {
            return Player.Spellbook.GetSpell(SpellSlot.Q).Name == "EliseHumanQ";
        }

        private static void OnGameLoad(EventArgs args)
        {
            if (Player.CharData.BaseSkinName != "Elise" && Player.CharData.BaseSkinName != "elisespider") return;

            InitMenu();
            InitializeSpells();

            Game.OnUpdate += OnUpdate;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
            Orbwalking.OnAttack += OnAttack;
            AntiGapcloser.OnEnemyGapcloser += OnEnemyGapcloser;
            Orbwalking.BeforeAttack += BeforeAttack;
            Obj_AI_Base.OnPlayAnimation += OnPlayAnimation;
            Drawing.OnDraw += Drawing_OnDraw;
            //GameObject.OnCreate += OnCreateObject;
            //GameObject.OnDelete += OnDeleteObject;
            //Obj_AI_Base.OnAggro += OnAggro;
        }

        //private static void OnDeleteObject(GameObject sender, EventArgs args)
        //{
        //    if (sender.Type != GameObjectType.obj_AI_Minion) return;
        //    if (sender.Name == ("Spiderling"))
        //    {
        //        spider = null;
        //    }
        //}

        //private static void OnCreateObject(GameObject sender, EventArgs args)
        //{
        //    if (sender.Type != GameObjectType.obj_AI_Minion) return;
        //    if (sender.Name == ("Spiderling"))
        //    {
        //        spider = (Obj_AI_Minion)sender;
        //    }
        //}

        //private static void OnAggro(Obj_AI_Base sender, GameObjectAggroEventArgs args)
        //{
        //    if (sender == null || args == null) return;
        //    {
        //        if (args.NetworkId == ObjectManager.Player.NetworkId)
        //        {
        //            if (!Human() && !Player.IsWindingUp)
        //            {
        //                Player.IssueOrder(GameObjectOrder.MoveTo, Player.Position.Extend(sender.Position, -40));
        //            }
        //            // idk find out when spider is aggrod and run back in range of sender
        //        }
        //    }
        //}


        private static void OnUpdate(EventArgs args)
        {
            Killsteal();
            Cooldowns();
            Rappel();

            switch (_orbwalker.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.Combo:
                    Combo();
                    break;
                case Orbwalking.OrbwalkingMode.Mixed:
                    Harass();
                    break;
                case Orbwalking.OrbwalkingMode.LaneClear:
                    //LaneClear();
                    //JungleClear();
                    break;
            }
        }

        //private static void JungleClear()
        //{

        //}

        //private static void LaneClear()
        //{

        //}

        private static void Combo()
        {
            var target = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Magical);
            if (target == null) return;
            if (!target.IsValidTarget()) return;

            if (Human())
            {
                if (W.IsReady() && Config.Item("wComboH").GetValue<bool>() &&
                    target.Distance(Player.Position) <= W.Range)
                {
                    var wprediction = W.GetPrediction(target);

                    switch (wprediction.Hitchance)
                    {
                        case HitChance.Medium:
                        case HitChance.High:
                        case HitChance.VeryHigh:
                        case HitChance.Immobile:

                            W.Cast(wprediction.CastPosition);
                            break;

                        case HitChance.Collision:

                            var colliding = wprediction.CollisionObjects.OrderBy(o => o.Distance(Player, true)).ToList();
                            if (colliding.Count > 0)
                            {
                                if (colliding[0].Distance(target, true) <= 25000 ||
                                    colliding[0].Type == GameObjectType.obj_AI_Hero)
                                {
                                    W.Cast(wprediction.CastPosition);
                                }
                                else if (colliding[0].Type != GameObjectType.obj_AI_Hero &&
                                         colliding[0].Distance(target, true) > 25000 && R.IsReady() && realcdSQ <= 1 &&
                                         target.Distance(Player.Position) <= Q1.Range + 200)
                                {
                                    var playerPosition = ObjectManager.Player.Position.To2D();
                                    var direction = ObjectManager.Player.Direction.To2D().Perpendicular();
                                    const int distance = 600;
                                    const int stepSize = 40;

                                    for (var step = 0f; step < 360; step += stepSize)
                                    {
                                        var currentAngel = step * (float)Math.PI / 180;
                                        var currentCheckPoint = playerPosition +
                                                                distance * direction.Rotated(currentAngel);

                                        var collision =
                                            Collision.GetCollision(new List<Vector3> { currentCheckPoint.To3D() },
                                                new PredictionInput { Delay = 0.25f, Radius = 200, Speed = 1000 });

                                        if (collision.Count == 0)
                                        {
                                            Q.CastOnUnit(target);
                                            W.Cast(currentCheckPoint);
                                            R.Cast();
                                            //if (Q.IsReady() && Config.Item("qComboH").GetValue<bool>() &&
                                            //    target.Distance(Player.Position) <= Q.Range)
                                            //{
                                            //    
                                            //}
                                        }
                                    }
                                }
                            }
                            break;
                    }
                }


                if (E.IsReady() && Config.Item("eComboH").GetValue<bool>() &&
                    target.Distance(Player.Position) <= E.Range)
                {
                    var collision = E.GetCollision(Player.Position.To2D(), new List<Vector2> { target.Position.To2D() });

                    if (collision.Count <= 0)
                        E.Cast(target);

                    else if (collision.Count >= 1 && collision[0].Type == GameObjectType.obj_AI_Hero)
                        E.Cast(target);
                }

                if (Q.IsReady() && Config.Item("qComboH").GetValue<bool>() &&
                    target.Distance(Player.Position) <= Q.Range)
                    Q.CastOnUnit(target);

                if (Config.Item("rCombo").GetValue<bool>() && !Q.IsReady() && !W.IsReady() && !E.IsReady() &&
                    R.IsReady() && target.Distance(Player.Position) <= Q1.Range)
                    if (realcdSQ == 0 || realcdSW == 0 || realcdSE == 0)
                        R.Cast();
            }
            else
            {
                if (Q1.IsReady() && Config.Item("qCombo").GetValue<bool>() &&
                    target.Distance(Player.Position) <= Q1.Range)
                {
                    Q1.CastOnUnit(target);
                    Utility.DelayAction.Add(100, Orbwalking.ResetAutoAttackTimer);
                }

                if (E1.IsReady() && Config.Item("eCombo").GetValue<bool>() &&
                    target.Distance(Player.Position) <= E1.Range &&
                    target.Distance(Player.Position) >= Config.Item("eMin").GetValue<Slider>().Value)
                    E1.CastOnUnit(target);

                if (Config.Item("rCombo").GetValue<bool>() && !Q.IsReady() && !W.IsReady() && !E.IsReady() &&
                    R.IsReady() && !Player.HasBuff("EliseSpiderW") ||
                    Config.Item("rCombo").GetValue<bool>() && !Q.IsReady() && !W.IsReady() && !E.IsReady() &&
                    R.IsReady() && target.Distance(Player.Position) >= Player.AttackRange + 100)
                    if (realcdQ == 0 || realcdW == 0 || realcdE == 0)
                        R.Cast();
            }
        }

        private static void Harass()
        {
            if (Player.ManaPercent <= Config.Item("harassMana").GetValue<Slider>().Value) return;
            var target = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Magical);
            if (target == null) return;
            if (!target.IsValidTarget()) return;

            if (Human())
            {
                if (W.IsReady() && Config.Item("wHarassH").GetValue<bool>() &&
                    target.Distance(Player.Position) <= W.Range)
                {
                    var wprediction = W.GetPrediction(target);

                    switch (wprediction.Hitchance)
                    {
                        case HitChance.Medium:
                        case HitChance.High:
                        case HitChance.VeryHigh:
                        case HitChance.Immobile:

                            W.Cast(wprediction.CastPosition);
                            break;

                        case HitChance.Collision:

                            var colliding = wprediction.CollisionObjects.OrderBy(o => o.Distance(Player, true)).ToList();
                            if (colliding.Count > 0)
                            {
                                if (colliding[0].Distance(target, true) <= 25000 ||
                                    colliding[0].Type == GameObjectType.obj_AI_Hero)
                                {
                                    W.Cast(wprediction.CastPosition);
                                }
                                else if (colliding[0].Type != GameObjectType.obj_AI_Hero &&
                                         colliding[0].Distance(target, true) > 25000 && R.IsReady() && realcdSQ <= 1 &&
                                         target.Distance(Player.Position) <= Q1.Range + 200)
                                {
                                    var playerPosition = ObjectManager.Player.Position.To2D();
                                    var direction = ObjectManager.Player.Direction.To2D().Perpendicular();
                                    const int distance = 600;
                                    const int stepSize = 40;

                                    for (var step = 0f; step < 360; step += stepSize)
                                    {
                                        var currentAngel = step * (float)Math.PI / 180;
                                        var currentCheckPoint = playerPosition +
                                                                distance * direction.Rotated(currentAngel);

                                        var collision =
                                            Collision.GetCollision(new List<Vector3> { currentCheckPoint.To3D() },
                                                new PredictionInput { Delay = 0.25f, Radius = 200, Speed = 1000 });

                                        if (collision.Count == 0)
                                        {
                                            Q.CastOnUnit(target);
                                            W.Cast(currentCheckPoint);
                                            R.Cast();
                                            //if (Q.IsReady() && Config.Item("qHarassH").GetValue<bool>() &&
                                            //    target.Distance(Player.Position) <= Q.Range)
                                            //{
                                            //    
                                            //}
                                        }
                                    }
                                }
                            }
                            break;
                    }
                }


                if (E.IsReady() && Config.Item("eHarassH").GetValue<bool>() &&
                    target.Distance(Player.Position) <= E.Range)
                {
                    var collision = E.GetCollision(Player.Position.To2D(), new List<Vector2> { target.Position.To2D() });

                    if (collision.Count <= 0)
                        E.Cast(target);

                    else if (collision.Count >= 1 && collision[0].Type == GameObjectType.obj_AI_Hero)
                        E.Cast(target);
                }

                if (Q.IsReady() && Config.Item("qHarassH").GetValue<bool>() &&
                    target.Distance(Player.Position) <= Q.Range)
                    Q.CastOnUnit(target);

                if (Config.Item("rHarass").GetValue<bool>() && !Q.IsReady() && !W.IsReady() && !E.IsReady() &&
                    R.IsReady() && target.Distance(Player.Position) <= Q1.Range)
                    if (realcdSQ == 0 || realcdSW == 0 || realcdSE == 0)
                        R.Cast();
            }
            else
            {
                if (Q1.IsReady() && Config.Item("qHarass").GetValue<bool>() &&
                    target.Distance(Player.Position) <= Q1.Range)
                {
                    Q1.CastOnUnit(target);
                    Utility.DelayAction.Add(100, Orbwalking.ResetAutoAttackTimer);
                }

                if (E1.IsReady() && Config.Item("eHarass").GetValue<bool>() &&
                    target.Distance(Player.Position) <= E1.Range &&
                    target.Distance(Player.Position) >= Config.Item("eMinHarass").GetValue<Slider>().Value)
                    E1.CastOnUnit(target);

                if (Config.Item("rHarass").GetValue<bool>() && !Q.IsReady() && !W.IsReady() && !E.IsReady() &&
                    R.IsReady() && !Player.HasBuff("EliseSpiderW") ||
                    Config.Item("rHarass").GetValue<bool>() && !Q.IsReady() && !W.IsReady() && !E.IsReady() &&
                    R.IsReady() && target.Distance(Player.Position) >= Player.AttackRange + 100)
                    if (realcdQ == 0 || realcdW == 0 || realcdE == 0)
                        R.Cast();
            }
        }

        private static void Killsteal()
        {
            foreach (
                var enemy in HeroManager.Enemies.Where(e => e.IsValidTarget() && e.Distance(Player.Position) <= E.Range)
                )
            {
                if (Human())
                {
                    if (Config.Item("qKSH").GetValue<bool>())
                    {
                        if (Q.IsReady() && enemy.Distance(Player.Position) <= Q.Range &&
                            enemy.Health <= Q.GetDamage(enemy))
                        {
                            Q.CastOnUnit(enemy);
                        }
                    }

                    if (Config.Item("qKS").GetValue<bool>() && Config.Item("switchKS").GetValue<bool>())
                    {
                        if (realcdSQ == 0 && enemy.Distance(Player.Position) <= Q1.Range &&
                            enemy.Health <= Q1.GetDamage(enemy, 1))
                        {
                            if (R.IsReady())
                            {
                                R.Cast();
                                Q1.CastOnUnit(enemy);
                            }
                            return;
                        }
                    }
                }
                else if (!Human())
                {
                    if (Config.Item("qKS").GetValue<bool>())
                    {
                        if (Q1.IsReady() && enemy.Distance(Player.Position) <= Q1.Range &&
                            enemy.Health <= Q1.GetDamage(enemy, 1))
                        {
                            Q1.CastOnUnit(enemy);
                        }
                    }

                    if (Config.Item("qKSH").GetValue<bool>() && Config.Item("switchKS").GetValue<bool>())
                    {
                        if (realcdQ == 0 && enemy.Distance(Player.Position) <= Q.Range &&
                            enemy.Health <= Q.GetDamage(enemy))
                        {
                            if (R.IsReady())
                            {
                                R.Cast();
                                Q.CastOnUnit(enemy);
                            }
                            return;
                        }
                    }
                }
            }
        }

        private static void OnPlayAnimation(Obj_AI_Base sender, GameObjectPlayAnimationEventArgs args)
        {
            if (sender.IsMe)
            {
                if (args.Animation.Contains("Ult_E"))
                {
                    _e1Cd = Game.Time + CalculateCd(SpiderEcd[E.Level - 1]);
                    Utility.DelayAction.Add(100, Orbwalking.ResetAutoAttackTimer);
                }
            }
        }

        private static void BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            if (Config.Item("bAuto").GetValue<bool>() && Human() && Q.IsReady() &&
                Player.Distance(args.Target.Position) >= Player.AttackRange)
            {
                args.Process = false;
            }
        }

        private static void OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (E.IsReady() && Human() && gapcloser.Sender.IsValidTarget(E.Range) && Config.Item("hGC").GetValue<bool>())
            {
                E.Cast(gapcloser.Sender);
                return;
            }

            if (realcdSE == 0 && gapcloser.Sender.IsValidTarget(E1.Range) && Config.Item("fGC").GetValue<bool>() &&
                gapcloser.End.Distance(Player.Position) >= Config.Item("eMin").GetValue<Slider>().Value)
            {
                if (Human() && R.IsReady())
                {
                    R.Cast();
                    E1.Cast(gapcloser.Sender);
                }
                else if (!Human())
                    E1.Cast(gapcloser.Sender);
            }
        }

        private static void OnAttack(AttackableUnit unit, AttackableUnit target)
        {
            if (!unit.IsMe) return;
            var aaDelay = Player.AttackDelay * 100 + Game.Ping / 2f;

            if (Config.Item("wCombo").GetValue<bool>())
                if (target.Type == GameObjectType.obj_AI_Hero &&
                    _orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
                    Utility.DelayAction.Add((int)aaDelay, () => W1.Cast());

                else if (Config.Item("wHarass").GetValue<bool>())
                    if (target.Type == GameObjectType.obj_AI_Hero &&
                        _orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
                        Utility.DelayAction.Add((int)aaDelay, () => W1.Cast());
        }

        private static void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe)
            {
                GetCDs(args);
                if (args.SData.Name == "EliseHumanW")
                    Orbwalking.ResetAutoAttackTimer();
            }
        }

        private static void Rappel()
        {
            if (Config.Item("rappel").GetValue<KeyBind>().Active)
            {
                if (Human() && R.IsReady() && realcdSE == 0)
                {
                    R.Cast();
                    E1.Cast();
                }
                else if (!Human() && realcdSE == 0)
                    E1.Cast();
            }
        }

        private static float CalculateCd(float time)
        {
            return time + (time * Player.PercentCooldownMod);
        }

        private static void Cooldowns()
        {
            realcdQ = ((_qCd - Game.Time) > 0) ? (_qCd - Game.Time) : 0;
            realcdW = ((_wCd - Game.Time) > 0) ? (_wCd - Game.Time) : 0;
            realcdE = ((_eCd - Game.Time) > 0) ? (_eCd - Game.Time) : 0;
            realcdSQ = ((_q1Cd - Game.Time) > 0) ? (_q1Cd - Game.Time) : 0;
            realcdSW = ((_w1Cd - Game.Time) > 0) ? (_w1Cd - Game.Time) : 0;
            realcdSE = ((_e1Cd - Game.Time) > 0) ? (_e1Cd - Game.Time) : 0;
        }

        private static void GetCDs(GameObjectProcessSpellCastEventArgs spell)
        {
            if (Human())
            {
                if (spell.SData.Name == "EliseHumanQ")
                    _qCd = Game.Time + CalculateCd(HumanQcd[Q.Level - 1]);
                if (spell.SData.Name == "EliseHumanW")
                    _wCd = Game.Time + CalculateCd(HumanWcd[W.Level - 1]);
                if (spell.SData.Name == "EliseHumanE")
                    _eCd = Game.Time + CalculateCd(HumanEcd[E.Level - 1]);
            }
            else
            {
                if (spell.SData.Name == "EliseSpiderQCast")
                    _q1Cd = Game.Time + CalculateCd(SpiderQcd[Q.Level - 1]);
                if (spell.SData.Name == "EliseSpiderW")
                    _w1Cd = Game.Time + CalculateCd(SpiderWcd[W.Level - 1]);
            }
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            var wts = Drawing.WorldToScreen(Player.Position);
            if (!Human())
            {
                if (Config.Item("drawSQ").GetValue<bool>())
                    Drawing.DrawCircle(Player.Position, Q1.Range, Color.LightCoral);
                if (Config.Item("drawSE").GetValue<bool>())
                    Drawing.DrawCircle(Player.Position, E1.Range, Color.LightCoral);

                if (!Config.Item("drawSpellCDs").GetValue<bool>()) return;
                if (realcdQ == 0)
                    Drawing.DrawText(wts[0] - 120, wts[1] - 30, Color.White, "Q: Ready");
                else
                    Drawing.DrawText(wts[0] - 120, wts[1] - 30, Color.Orange, "Q: " + realcdQ.ToString("0.0"));
                if (realcdW == 0)
                    Drawing.DrawText(wts[0] - 40, wts[1] - 30, Color.White, "W: Ready");
                else
                    Drawing.DrawText(wts[0] - 40, wts[1] - 30, Color.Orange, "W: " + realcdW.ToString("0.0"));

                if (realcdE == 0)
                    Drawing.DrawText(wts[0] + 40, wts[1] - 30, Color.White, "E: Ready");
                else
                    Drawing.DrawText(wts[0] + 40, wts[1] - 30, Color.Orange, "E: " + realcdE.ToString("0.0"));
            }
            else
            {
                if (Config.Item("drawHQ").GetValue<bool>())
                    Drawing.DrawCircle(Player.Position, Q.Range, Color.LightCoral);
                if (Config.Item("drawHW").GetValue<bool>())
                    Drawing.DrawCircle(Player.Position, W.Range, Color.LightCoral);
                if (Config.Item("drawHE").GetValue<bool>())
                    Drawing.DrawCircle(Player.Position, E.Range, Color.LightCoral);

                if (!Config.Item("drawSpellCDs").GetValue<bool>()) return;
                if (realcdSQ == 0)
                    Drawing.DrawText(wts[0] - 120, wts[1] - 30, Color.White, "Q: Ready");
                else
                    Drawing.DrawText(wts[0] - 120, wts[1] - 30, Color.Orange, "Q: " + realcdSQ.ToString("0.0"));
                if (realcdSW == 0)
                    Drawing.DrawText(wts[0] - 40, wts[1] - 30, Color.White, "W: Ready");
                else
                    Drawing.DrawText(wts[0] - 40, wts[1] - 30, Color.Orange, "W: " + realcdSW.ToString("0.0"));

                if (realcdSE == 0)
                    Drawing.DrawText(wts[0] + 40, wts[1] - 30, Color.White, "E: Ready");
                else
                    Drawing.DrawText(wts[0] + 40, wts[1] - 30, Color.Orange, "E: " + realcdSE.ToString("0.0"));
            }
        }

        private static void InitializeSpells()
        {
            Q = new Spell(SpellSlot.Q, 625f);
            W = new Spell(SpellSlot.W, 950f);
            W.SetSkillshot(0.25f, 100f, 1500, true, SkillshotType.SkillshotLine);
            E = new Spell(SpellSlot.E, 1100f);
            E.SetSkillshot(0.25f, 55f, 1600, true, SkillshotType.SkillshotLine);

            Q1 = new Spell(SpellSlot.Q, 475f);
            W1 = new Spell(SpellSlot.W);
            E1 = new Spell(SpellSlot.E, 750f);
            R = new Spell(SpellSlot.R);
        }

        private static void InitMenu()
        {
            var orbwalkerMenu = new Menu("Orbwalker", "Orbwalker");
            _orbwalker = new Orbwalking.Orbwalker(orbwalkerMenu);
            Config.AddSubMenu(orbwalkerMenu);

            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            TargetSelector.AddToMenu(targetSelectorMenu);
            Config.AddSubMenu(targetSelectorMenu);

            var comboMenu = new Menu("Combo", "Combo settings");
            {
                comboMenu.AddItem(new MenuItem("qComboH", "Use Human Q").SetValue(true));
                comboMenu.AddItem(new MenuItem("qCombo", "Use Spider Q").SetValue(true));
                comboMenu.AddItem(new MenuItem("wComboH", "Use Human W").SetValue(true));
                comboMenu.AddItem(new MenuItem("wCombo", "Use Spider W").SetValue(true));
                comboMenu.AddItem(new MenuItem("eComboH", "Use Human E").SetValue(true));
                comboMenu.AddItem(new MenuItem("eCombo", "Use Spider E").SetValue(true));
                comboMenu.AddItem(new MenuItem("eMin", "E minimum range").SetValue(new Slider(400, 0, 750)));
                comboMenu.AddItem(new MenuItem("rCombo", "Use R").SetValue(true));
                Config.AddSubMenu(comboMenu);
            }

            var harassMenu = new Menu("Harass", "Harass settings");
            {
                harassMenu.AddItem(new MenuItem("qHarassH", "Use Human Q").SetValue(true));
                harassMenu.AddItem(new MenuItem("qHarass", "Use Spider Q").SetValue(true));
                harassMenu.AddItem(new MenuItem("wHarassH", "Use Human W").SetValue(true));
                harassMenu.AddItem(new MenuItem("wHarass", "Use Spider W").SetValue(true));
                harassMenu.AddItem(new MenuItem("eHarassH", "Use Human E").SetValue(true));
                harassMenu.AddItem(new MenuItem("eHarass", "Use Spider E").SetValue(true));
                harassMenu.AddItem(new MenuItem("eMinHarass", "E minimum range").SetValue(new Slider(400, 0, 750)));
                harassMenu.AddItem(new MenuItem("rComboHarass", "Use R").SetValue(true));
                harassMenu.AddItem(new MenuItem("harassMana", "Mana manager (%)").SetValue(new Slider(40, 1)));

                Config.AddSubMenu(harassMenu);
            }

            var killstealMenu = new Menu("Killsteal", "Killsteal settings");
            {
                killstealMenu.AddItem(new MenuItem("qKSH", "Use Human Q").SetValue(true));
                killstealMenu.AddItem(new MenuItem("qKS", "Use Spider Q").SetValue(true));
                killstealMenu.AddItem(new MenuItem("switchKS", "Switch forms to KS").SetValue(true));
                Config.AddSubMenu(killstealMenu);
            }

            var miscMenu = new Menu("Misc", "Misc. settings");
            {
                miscMenu.AddItem(new MenuItem("bAuto", "Block human auto if > Q range").SetValue(true));
                miscMenu.AddItem(
                    new MenuItem("rappel", "Instant rappel").SetValue(new KeyBind("Z".ToCharArray()[0],
                        KeyBindType.Press)));
                miscMenu.AddItem(new MenuItem("hGC", "Human anti-GC").SetValue(true));
                miscMenu.AddItem(new MenuItem("fGC", "Spider follow-GC").SetValue(true));
                Config.AddSubMenu(miscMenu);
            }

            var drawingMenu = new Menu("Drawing", "Drawing settings");
            {
                drawingMenu.AddItem(new MenuItem("drawSpellCDs", "Draw other form cooldowns").SetValue(true));
                drawingMenu.AddItem(new MenuItem("drawHQ", "Draw Human Q").SetValue(true));
                drawingMenu.AddItem(new MenuItem("drawHW", "Draw Human W").SetValue(true));
                drawingMenu.AddItem(new MenuItem("drawHE", "Draw Human E").SetValue(true));
                drawingMenu.AddItem(new MenuItem("drawSQ", "Draw Spider Q").SetValue(true));
                drawingMenu.AddItem(new MenuItem("drawSE", "Draw Spider E").SetValue(true));
                Config.AddSubMenu(drawingMenu);
            }

            Config.AddToMainMenu();
        }
    }
}