using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace Nami
{
    class Program
    {
        private static Obj_AI_Hero Player;
        private static Menu Menu;
        private static Orbwalking.Orbwalker Orbwalker;
        private static Spell Q, W, E, R;

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            Player = ObjectManager.Player;
            if (Player.ChampionName != "Nami") return;

            Menu = new Menu("Namik", "Namik", true);

            var SimpleTS = new Menu("Target Selector", "Target Selector");
            SimpleTs.AddToMenu(SimpleTS);
            Menu.AddSubMenu(SimpleTS);

            Menu.AddSubMenu(new Menu("Orbwalker", "Orbwalker"));
            Orbwalker = new Orbwalking.Orbwalker(Menu.SubMenu("Orbwalker"));

            Menu.AddSubMenu(new Menu("Combo", "combo"));
            Menu.SubMenu("combo").AddItem(new MenuItem("comboQ", "Use Q").SetValue(true));
            Menu.SubMenu("combo").AddItem(new MenuItem("comboW", "Use W").SetValue(new StringList(new[] { "Don't Cast", "Force Bounce", "Heal", "Damage" }, 0)));
            Menu.SubMenu("combo").AddItem(new MenuItem("comboE", "Use E").SetValue(new StringList(new[] { "Don't Cast", "Most AD", "Any Ally", "Self" }, 1)));
            Menu.SubMenu("combo").AddItem(new MenuItem("comboR", "Use R (Enemies)").SetValue(new Slider(3, 0, 5)));

            Menu.AddSubMenu(new Menu("Harass", "harass"));
            Menu.SubMenu("harass").AddItem(new MenuItem("harassQ", "Use Q (Max Chance)").SetValue(true));
            Menu.SubMenu("harass").AddItem(new MenuItem("harassW", "Use W").SetValue(new StringList(new[] { "Don't Cast", "Force Bounce", "Heal", "Damage" }, 0)));
            Menu.SubMenu("harass").AddItem(new MenuItem("harassFarm", "Block farm").SetValue(true));

            Menu.AddSubMenu(new Menu("Passive", "passive"));
            Menu.SubMenu("passive").AddItem(new MenuItem("passiveHeal", "Auto Heal (%)").SetValue(new Slider(40, 0, 100)));
            Menu.SubMenu("passive").AddItem(new MenuItem("passiveAutoQ", "Auto Q (Max Chance)").SetValue(true));

            Menu.AddSubMenu(new Menu("Misc", "misc"));
            Menu.SubMenu("misc").AddItem(new MenuItem("antiGapcloser", "Anti Gapcloser").SetValue(true));
            Menu.SubMenu("misc").AddItem(new MenuItem("autoInterruptQ", "Auto Interrupt with Q").SetValue(true));
            Menu.SubMenu("misc").AddItem(new MenuItem("autoInterruptR", "Auto Interrupt with R").SetValue(true));

            Menu.AddSubMenu(new Menu("Drawings", "drawings"));
            Menu.SubMenu("drawings").AddItem(new MenuItem("drawQ", "Draw Q").SetValue(new Circle(true, System.Drawing.Color.White)));
            Menu.SubMenu("drawings").AddItem(new MenuItem("drawW", "Draw W").SetValue(new Circle(false, System.Drawing.Color.White)));
            Menu.SubMenu("drawings").AddItem(new MenuItem("drawE", "Draw E").SetValue(new Circle(false, System.Drawing.Color.White)));
            Menu.SubMenu("drawings").AddItem(new MenuItem("drawR", "Draw R").SetValue(new Circle(true, System.Drawing.Color.White)));


            Menu.AddToMainMenu();

            Q = new Spell(SpellSlot.Q, 850f);
            W = new Spell(SpellSlot.W, 725f);
            E = new Spell(SpellSlot.E, 800f);
            R = new Spell(SpellSlot.R, 2200f);

            Q.SetSkillshot(1.0f, 200f, Int32.MaxValue, false, SkillshotType.SkillshotCircle);
            R.SetSkillshot(0.5f, 325f, 1200f, false, SkillshotType.SkillshotLine);

            Game.OnGameUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Interrupter.OnPosibleToInterrupt += Interrupter_OnPosibleToInterrupt;

            Game.PrintChat("<font color=\"#FF6600\">>> Namik loaded.</font>");
            Orbwalking.BeforeAttack += Orbwalking_BeforeAttack;
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            switch(Orbwalker.ActiveMode.ToString())
            {
                case "Combo":
                    Perform_Combo();
                    break;

                case "Mixed":
                    Perform_Harass();
                    break;

                default:
                    Passive();
                    break;
            }
        }

        static void Drawing_OnDraw(EventArgs args)
        {
            var drawQ = Menu.Item("drawQ").GetValue<Circle>();
            var drawW = Menu.Item("drawW").GetValue<Circle>();
            var drawE = Menu.Item("drawE").GetValue<Circle>();
            var drawR = Menu.Item("drawR").GetValue<Circle>();

            if (drawQ.Active)
                Utility.DrawCircle(Player.Position, Q.Range, drawQ.Color);

            if (drawW.Active)
                Utility.DrawCircle(Player.Position, W.Range, drawW.Color);

            if (drawE.Active)
                Utility.DrawCircle(Player.Position, E.Range, drawE.Color);

            if (drawR.Active)
                Utility.DrawCircle(Player.Position, R.Range, drawR.Color);
            
        }

        static void Orbwalking_BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            bool AllyNear = ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsValid && hero.IsAlly && !hero.IsMe && hero.Distance(args.Unit) < Orbwalking.GetRealAutoAttackRange(hero)).Count() > 0;
            if (Orbwalker.ActiveMode.ToString() == "Mixed" && args.Target.IsMinion && AllyNear && Menu.Item("harassFarm").GetValue<bool>())
                args.Process = false;
        }

        private static void Perform_Combo()
        {
            Obj_AI_Hero Target = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Magical);
           
            var comboQ = Menu.Item("comboQ").GetValue<bool>();
            var comboW = Menu.Item("comboW").GetValue<StringList>();
            var comboE = Menu.Item("comboE").GetValue<StringList>();
            var comboR = Menu.Item("comboR").GetValue<Slider>();
            
            if (Q.IsReady() && comboQ)
            {
                Q.CastIfHitchanceEquals(Target, HitChance.High);
            }

            if (W.IsReady() && comboW.SelectedIndex > 0)
            {
                switch (comboW.SelectedIndex)
                {
                    case 1: // Both
                        Obj_AI_Hero BounceAlly = ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.Distance(Target) < W.Range && hero.Distance(Player) < W.Range && hero.IsValid && hero.IsEnemy).ToArray()[0];

                        if (BounceAlly != null && (BounceAlly.MaxHealth - BounceAlly.Health > HealAmmount() || Target.Distance(Player) > W.Range))
                            W.CastOnUnit(BounceAlly);
                        else if (Target.Distance(BounceAlly) < W.Range && Target.IsValidTarget(W.Range))
                            W.CastOnUnit(Target);

                        break;

                    case 2: // Heal
                        Obj_AI_Hero HealAlly = ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.Distance(Player) < W.Range && hero.IsValid && hero.IsEnemy && hero.MaxHealth - hero.Health > HealAmmount()).OrderBy(hero => hero.Health).ToArray()[0];

                        if (HealAlly != null)
                            W.CastOnUnit(HealAlly);

                        break;

                    case 3: // Damage
                        if (Target.Distance(Player) < W.Range)
                            W.CastOnUnit(Target);

                        break;
                }
            }

            if (E.IsReady() && comboE.SelectedIndex > 0)
            {
                switch (comboE.SelectedIndex)
                {
                    case 1: // Most AD
                        Obj_AI_Hero ADCarry = ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.Distance(Player) < E.Range && hero.IsAlly && hero.IsValid).OrderByDescending(hero => hero.FlatPhysicalDamageMod).ToArray()[0];
                        E.CastOnUnit(ADCarry);
                        break;
                    case 2: // Any Ally
                        Obj_AI_Hero AnyAlly = ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.Distance(Player) < E.Range && Utility.CountEnemysInRange((int)Orbwalking.GetRealAutoAttackRange(hero), hero) > 0 && hero.IsValid && hero.IsAlly).OrderByDescending(hero => hero.AttackRange).ToArray()[0];
                        E.CastOnUnit(AnyAlly);
                        break;
                    case 3: // Self
                        if (Target.Distance(Player) < Orbwalking.GetRealAutoAttackRange(Player))
                            E.CastOnUnit(Player);
                        break;
                }
            }
            
            if (R.IsReady() && comboR.Value > 0)
                R.CastIfWillHit(Target, comboR.Value);
        }

        private static void Perform_Harass()
        {
            Obj_AI_Hero Target = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Magical);

            var harassQ = Menu.Item("harassQ").GetValue<bool>();
            var harassW = Menu.Item("harassW").GetValue<StringList>();

            if(Q.IsReady() && harassQ)
            {
                Q.Cast(Target);
            }

            if(W.IsReady() && harassW.SelectedIndex > 0)
            {
                switch (harassW.SelectedIndex)
                {
                    case 1: // Both
                        Obj_AI_Hero BounceAlly = ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.Distance(Target) < W.Range && hero.Distance(Player) < W.Range && hero.IsValid && hero.IsEnemy).ToArray()[0];

                        if (BounceAlly != null && (BounceAlly.MaxHealth - BounceAlly.Health > HealAmmount() || Target.Distance(Player) > W.Range))
                            W.CastOnUnit(BounceAlly);
                        else if (Target.Distance(BounceAlly) < W.Range && Target.Distance(Player) < W.Range)
                            W.CastOnUnit(Target);

                        break;

                    case 2: // Heal
                        Obj_AI_Hero HealAlly = ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.Distance(Player) < W.Range && hero.IsValid && hero.IsEnemy && hero.Health <= HealAmmount()).OrderBy(hero => hero.Health).ToArray()[0];

                        if (HealAlly != null)
                            W.CastOnUnit(HealAlly);

                        break;

                    case 3: // Damage
                        if (Target.Distance(Player) < W.Range)
                            W.CastOnUnit(Target);

                        break;
                }
            }
        }

        private static void Passive()
        {
            if (Player.HasBuff("Recall")) return;

            var passiveHeal = Menu.Item("passiveHeal").GetValue<Slider>();

            if(passiveHeal.Value > 0 && W.IsReady())
            {
                Obj_AI_Hero Ally = ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.Health < (hero.MaxHealth * passiveHeal.Value / 100) && hero.IsValid && hero.IsAlly && hero.MaxHealth - hero.Health > HealAmmount()).OrderBy(hero => hero.Health).ToArray()[0];
                if (Ally != null)
                    W.CastOnUnit(Ally);
            }

            if (Q.IsReady() && Menu.Item("passiveAutoQ").GetValue<bool>())
            {
                foreach (Obj_AI_Hero Enemy in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsEnemy && hero.IsValidTarget(Q.Range)))
                {
                    PredictionOutput Predict = Prediction.GetPrediction(Enemy, Q.Delay, Q.Width, Q.Speed);
                    if (Predict.Hitchance == HitChance.Dashing || Predict.Hitchance == HitChance.Immobile)
                        Q.Cast(Predict.CastPosition.To2D());
                }
            }
        }

        /* Not sure if this is working properly */
        private static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (Q.IsReady() && gapcloser.Sender.IsValidTarget(Q.Range) && Menu.Item("antiGapcloser").GetValue<bool>())
                Q.Cast(gapcloser.Sender);
        }

        private static void Interrupter_OnPosibleToInterrupt(Obj_AI_Base unit, InterruptableSpell spell)
        {
            if (Q.IsReady() && unit.IsValidTarget(Q.Range) && Menu.Item("autoInterruptQ").GetValue<bool>())
                Q.Cast(unit);
            else if (R.IsReady() && unit.IsValidTarget(R.Range) && Menu.Item("autoInterruptR").GetValue<bool>())
                R.Cast(unit);
        }
        /*******/

        private static double HealAmmount()
        {
            int[] BaseHeal = {0, 65, 95, 125, 155, 185};

            return BaseHeal[Player.Spellbook.GetSpell(SpellSlot.W).Level] + Player.FlatMagicDamageMod * 0.3;
        }


    }
}
