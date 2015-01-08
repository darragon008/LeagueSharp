#region

using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using LX_Orbwalker;
using SharpDX;
using Color = System.Drawing.Color;

#endregion

namespace Nidalee
{
    internal class Program
    {
        private static Spell Q1, Q2, W1, W2, E1, E2, R;
        private static Items.Item Bork, Cutlass, DFG;
        private static SpellSlot Ignite;
        private static Menu Config;
        private static Obj_AI_Hero Player;

        private static Orbwalking.OrbwalkingMode ActiveMode
        {
            get
            {
                if (Config.Item("KeysCombo").GetValue<KeyBind>().Active)
                    return Orbwalking.OrbwalkingMode.Combo;

                if (Config.Item("KeysLaneClear").GetValue<KeyBind>().Active)
                    return Orbwalking.OrbwalkingMode.LaneClear;

                if (Config.Item("KeysMixed").GetValue<KeyBind>().Active)
                    return Orbwalking.OrbwalkingMode.Mixed;

                return Orbwalking.OrbwalkingMode.None;
            }
        }

        private static bool PacketCasting
        {
            get { return Config.Item("packetCasting").GetValue<bool>(); }
        }

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            Player = ObjectManager.Player;
            if (Player.ChampionName != "Nidalee") return;

            Game.PrintChat(
                "<font color=\"#0066FF\">[<font color=\"#FFFFFF\">madk</font>]</font><font color=\"#FFFFFF\"> Nidalee assembly loaded! :^)</font>");

            #region Spells

            /* Human Spells */
            Q1 = new Spell(SpellSlot.Q, 1500f);
            W1 = new Spell(SpellSlot.W, 900f);
            E1 = new Spell(SpellSlot.E, 600f);
            /* Cougar Spells */
            Q2 = new Spell(SpellSlot.Q, 125f + 50f);
            W2 = new Spell(SpellSlot.W, 750f);
            E2 = new Spell(SpellSlot.E, 300f);
            /* Form Switcher */
            R = new Spell(SpellSlot.R);

            Q1.SetSkillshot(0.125f, 70f, 1300, true, SkillshotType.SkillshotLine);
            W1.SetSkillshot(1.5f, 80f, float.MaxValue, false, SkillshotType.SkillshotCircle);

            #endregion

            #region Items

            var map = Utility.Map.GetMap()._MapType;
            var DFGId = (map == Utility.Map.MapType.TwistedTreeline || map == Utility.Map.MapType.CrystalScar)
                ? 3128
                : 3188;

            Bork = new Items.Item(3153, 450f);
            Cutlass = new Items.Item(3144, 450f);
            DFG = new Items.Item(DFGId, 750f);

            #endregion

            /* Summoner Spells */
            Ignite = Player.GetSpellSlot("SummonerDot");

            #region Create Menu

            Config = new Menu("Nidaleek", "Nidaleek", true);

            // Simple Target Selector
            var Menu_TargetSelector = new Menu("Target Selector", "Target Selector");
            SimpleTs.AddToMenu(Menu_TargetSelector);

            // Orbwalker
            var Menu_Orbwalker = new Menu("Orbwalker", "Orbwalker");
            LXOrbwalker.AddToMenu(Menu_Orbwalker);

            // Key Bindings
            var Menu_KeyBindings = new Menu("Key Bindings", "KB");
            Menu_KeyBindings.AddItem(
                new MenuItem("KeysCombo", "Combo").SetValue(
                    new KeyBind(Menu_Orbwalker.Item("Combo_Key").GetValue<KeyBind>().Key, KeyBindType.Press, false)));
            Menu_KeyBindings.AddItem(
                new MenuItem("KeysMixed", "Harass").SetValue(
                    new KeyBind(Menu_Orbwalker.Item("Harass_Key").GetValue<KeyBind>().Key, KeyBindType.Press, false)));
            Menu_KeyBindings.AddItem(
                new MenuItem("KeysLaneClear", "Lane/Jungle Clear").SetValue(
                    new KeyBind(Menu_Orbwalker.Item("LaneClear_Key").GetValue<KeyBind>().Key, KeyBindType.Press,
                        false)));

            // Combo
            var Menu_Combo = new Menu("Combo", "combo");
            Menu_Combo.AddItem(new MenuItem("combo_info1", "Human Form:"));
            Menu_Combo.AddItem(new MenuItem("combo_Q1", "Javelin Toss").SetValue(true));
            Menu_Combo.AddItem(new MenuItem("combo_W1", "Bushwhack").SetValue(true));
            Menu_Combo.AddItem(new MenuItem("combo_E1", "Primal Surge").SetValue(true));
            Menu_Combo.AddItem(new MenuItem("combo_info2", "Cougar Form:"));
            Menu_Combo.AddItem(new MenuItem("combo_Q2", "Takedown").SetValue(true));
            Menu_Combo.AddItem(new MenuItem("combo_W2", "Pounce").SetValue(true));
            Menu_Combo.AddItem(new MenuItem("combo_E2", "Swipe").SetValue(true));
            Menu_Combo.AddItem(new MenuItem("combo_info3", "Extra Functions:"));
            Menu_Combo.AddItem(new MenuItem("combo_R", "Auto Switch Forms").SetValue(true));
            Menu_Combo.AddItem(new MenuItem("combo_Items", "Use Items").SetValue(true));
            Menu_Combo.AddItem(new MenuItem("combo_UT", "Jump to turret range").SetValue(true));

            // Harass
            var Menu_Harass = new Menu("Harass", "harass");
            Menu_Harass.AddItem(new MenuItem("harass_Q1", "Javelin Toss").SetValue(true));
            Menu_Harass.AddItem(new MenuItem("harass_W1", "Bushwhack").SetValue(true));

            // Lane Clear
            var Menu_Farm = new Menu("Lane Clear", "farm");
            Menu_Farm.AddItem(new MenuItem("farm_info1", "Human Form:"));
            Menu_Farm.AddItem(new MenuItem("farm_E1", "Primal Surge").SetValue(true));
            Menu_Farm.AddItem(new MenuItem("farm_info2", "Cougar Form:"));
            Menu_Farm.AddItem(new MenuItem("farm_Q2", "Takedown").SetValue(true));
            Menu_Farm.AddItem(new MenuItem("farm_W2", "Pounce").SetValue(true));
            Menu_Farm.AddItem(new MenuItem("farm_E2", "Swipe").SetValue(true));
            Menu_Farm.AddItem(new MenuItem("farm_R", "Auto Swtich Forms").SetValue(false));

            // Kill Steal
            var Menu_KillSteal = new Menu("Kill Steal", "killsteal");
            Menu_KillSteal.AddItem(new MenuItem("ks_enabled", "State").SetValue(true));
            Menu_KillSteal.AddItem(new MenuItem("ks_Q1", "Javelin Toss").SetValue(true));
            Menu_KillSteal.AddItem(new MenuItem("ks_dot", "Ignite").SetValue(true));

            // Misc
            var Menu_Misc = new Menu("Misc", "Misc");
            Menu_Misc.AddItem(new MenuItem("autoHealMode", "Auto Heal Mode").SetValue(new StringList(new[] {"OFF", "Self", "Allies"}, 1)));
            Menu_Misc.AddItem(new MenuItem("autoHealPct", "Auto Heal %").SetValue(new Slider(50)));
            Menu_Misc.AddItem(new MenuItem("packetCasting", "Packet Casting").SetValue(true));

            // Drawings
            var Menu_Drawings = new Menu("Drawings", "drawings");
            Menu_Drawings.AddItem(new MenuItem("draw_info1", "Human Form:"));
            Menu_Drawings.AddItem(new MenuItem("draw_Q1", "Javelin Toss").SetValue(new Circle(true, Color.White)));
            Menu_Drawings.AddItem(
                new MenuItem("draw_Q1MaxDmg", "Javelin Toss: Max DMG").SetValue(new Circle(true, Color.White)));
            Menu_Drawings.AddItem(new MenuItem("draw_W1", "Bushwhack").SetValue(new Circle(true, Color.White)));
            Menu_Drawings.AddItem(new MenuItem("draw_E1", "Primal Surge").SetValue(new Circle(true, Color.White)));
            Menu_Drawings.AddItem(new MenuItem("draw_info2", "Cougar Form:"));
            Menu_Drawings.AddItem(new MenuItem("draw_W2", "Pounce").SetValue(new Circle(true, Color.White)));
            Menu_Drawings.AddItem(new MenuItem("draw_E2", "Swipe").SetValue(new Circle(true, Color.White)));
            Menu_Drawings.AddItem(new MenuItem("draw_CF", "Current Form Only").SetValue(false));

            Config.AddSubMenu(Menu_TargetSelector);
            Config.AddSubMenu(Menu_Orbwalker);
            Config.AddSubMenu(Menu_KeyBindings);
            Config.AddSubMenu(Menu_Combo);
            Config.AddSubMenu(Menu_Harass);
            Config.AddSubMenu(Menu_KillSteal);
            Config.AddSubMenu(Menu_Farm);
            Config.AddSubMenu(Menu_Misc);
            Config.AddSubMenu(Menu_Drawings);

            Config.AddToMainMenu();

            #endregion

            Game.OnGameUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            LXOrbwalker.AfterAttack += AfterAttack;
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            if (Config.Item("ks_enabled").GetValue<bool>())
                KillSteal();

            if (Config.Item("autoHealMode").GetValue<StringList>().SelectedIndex > 0)
                AutoHeal(Config.Item("autoHealMode").GetValue<StringList>().SelectedIndex == 2);
            
            switch (ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.Combo:
                    doCombo();
                    break;
                case Orbwalking.OrbwalkingMode.Mixed:
                    doHarass();
                    break;
                case Orbwalking.OrbwalkingMode.LaneClear:
                    doFarm();
                    break;
            }
        }

        private static void AfterAttack(Obj_AI_Base unit, Obj_AI_Base target)
        {
            if (unit.IsMe && ActiveMode == Orbwalking.OrbwalkingMode.Combo && Q2.IsReady() && IsCougar &&
                Config.Item("combo_Q2").GetValue<bool>())
                Q2.CastOnUnit(Player, PacketCasting);
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            var drawQ1MD = Config.Item("draw_Q1MaxDmg").GetValue<Circle>();
            var drawQ1 = Config.Item("draw_Q1").GetValue<Circle>();
            var drawW1 = Config.Item("draw_W1").GetValue<Circle>();
            var drawE1 = Config.Item("draw_E1").GetValue<Circle>();
            var drawW2 = Config.Item("draw_W2").GetValue<Circle>();
            var drawE2 = Config.Item("draw_E2").GetValue<Circle>();
            var drawCF = Config.Item("draw_CF").GetValue<bool>();

            if (drawQ1.Active && (drawCF && !IsCougar || !drawCF))
                Utility.DrawCircle(Player.Position, Q1.Range, drawQ1.Color);

            if (drawQ1MD.Active && (drawCF && !IsCougar || !drawCF))
                Utility.DrawCircle(Player.Position, 1300f, drawQ1MD.Color);

            if (drawW1.Active && (drawCF && !IsCougar || !drawCF))
                Utility.DrawCircle(Player.Position, W1.Range, drawW1.Color);

            if (drawE1.Active && (drawCF && !IsCougar || !drawCF))
                Utility.DrawCircle(Player.Position, E1.Range, drawE1.Color);

            if (drawW2.Active && (drawCF && IsCougar || !drawCF))
                Utility.DrawCircle(Player.Position, W2.Range, drawW2.Color);

            if (drawE2.Active && (drawCF && IsCougar || !drawCF))
                Utility.DrawCircle(Player.Position, E2.Range, drawE2.Color);
        }

        private static void doCombo()
        {
            var Target = SimpleTs.GetTarget(Q1.Range, SimpleTs.DamageType.Magical);
            var Marked = Target.HasBuff("nidaleepassivehunted", true);
            var Hunting = Player.HasBuff("nidaleepassivehunting", true);
            var Distance = Player.Distance(Target);
            var useItems = Config.Item("combo_Items").GetValue<bool>();

            if (useItems)
            {
                if (Items.CanUseItem(Bork.Id)) Bork.Cast(Target);
                if (Items.CanUseItem(Cutlass.Id)) Cutlass.Cast(Target);
            }

            var comboUT = Config.Item("combo_UT").GetValue<bool>();

            /* Human Form */
            if (!IsCougar)
            {
                if (Marked && R.IsReady() && Config.Item("combo_R").GetValue<bool>() && Distance < 750f ||
                    (!Q1.IsReady() && !Q1.IsReady(2500) && Target.Distance(Player) < 300f) &&
                    (!Utility.UnderTurret(Target, true) || comboUT))
                    R.CastOnUnit(Player, PacketCasting);

                else if (Q1.IsReady() && Config.Item("combo_Q1").GetValue<bool>())
                    Q1.Cast(Target, PacketCasting);

                else if (W1.IsReady() && Config.Item("combo_W1").GetValue<bool>())
                    W1.Cast(Target, PacketCasting);

                else if (E1.IsReady() && Config.Item("combo_E1").GetValue<bool>() &&
                         (!R.IsReady() || !Marked && Distance < W2.Range + 75f))
                    E1.CastOnUnit(Player, PacketCasting);
            }
            
            /* Cougar Form */
            else
            {
                if (!Marked && R.IsReady() && Config.Item("combo_R").GetValue<bool>() && Distance < W2.Range + 75f)
                {
                    R.CastOnUnit(Player, PacketCasting);
                    return;
                }

                // Deathfire grasp / Blackfire Torch
                var dmg = Q1.GetDamage(Target, 1) + W1.GetDamage(Target, 1) + E1.GetDamage(Target, 1);
                if (Target.IsValidTarget(DFG.Range) && Q1.IsReady() && W1.IsReady() && E1.IsReady() && dmg < Target.Health && (dmg * 1.2f) + (Target.MaxHealth * (DFG.Id == 3188 ? 0.20f : 0.15f)) > Target.Health && useItems)
                    DFG.Cast(Target);
                
                if (Marked && Hunting && W2.IsReady() && Config.Item("combo_W2").GetValue<bool>() && Distance < 750f &&
                         Distance > 200f && (!Utility
                             .UnderTurret(Target, true) || comboUT))
                    Player.Spellbook.CastSpell(SpellSlot.W, Target);
                else if (E2.IsReady() && Distance < 300f)
                {
                    var Pred = Prediction.GetPrediction(Target, 0.5f);
                    E2.Cast(Pred.CastPosition, PacketCasting);
                }
            }
        }

        private static void doHarass()
        {
            var Target = SimpleTs.GetTarget(Q1.Range, SimpleTs.DamageType.Magical);
            var OrbTarget = LXOrbwalker.GetPossibleTarget();
            if (!IsCougar && (OrbTarget == null || !OrbTarget.IsMinion))
            {
                if (Q1.IsReady() && Config.Item("harass_Q1").GetValue<bool>())
                    Q1.Cast(Target, PacketCasting);

                if (W1.IsReady() && Config.Item("harass_W1").GetValue<bool>())
                    W1.Cast(Target, PacketCasting);
            }
        }

        private static void doFarm()
        {
            foreach (
                var Minion in
                    ObjectManager.Get<Obj_AI_Minion>()
                        .Where(
                            minion =>
                                minion.Team != Player.Team && !minion.IsDead &&
                                Vector2.Distance(minion.ServerPosition.To2D(), Player.ServerPosition.To2D()) < 600f)
                        .OrderBy(minion => Vector2.Distance(minion.Position.To2D(), Player.Position.To2D())))
            {
                if (IsCougar)
                {
                    if (Q2.IsReady() && Config.Item("farm_Q2").GetValue<bool>())
                        Q2.CastOnUnit(Player, PacketCasting);
                    else if (W2.IsReady() && Config.Item("farm_W2").GetValue<bool>() && Player.Distance(Minion) > 200f)
                        W2.Cast(Minion.Position, PacketCasting);
                    else if (E2.IsReady() && Config.Item("farm_E2").GetValue<bool>())
                        E2.Cast(Minion.Position, PacketCasting);
                }
                else if (R.IsReady() && Config.Item("farm_R").GetValue<bool>())
                    R.CastOnUnit(Player, PacketCasting);
                else if (E1.IsReady() && Config.Item("farm_E1").GetValue<bool>())
                    E1.CastOnUnit(Player, PacketCasting);
                return;
            }
        }

        private static void KillSteal()
        {
            var ks_Q1 = Config.Item("ks_Q1").GetValue<bool>();
            var ks_dot = Config.Item("ks_dot").GetValue<bool>();


            if (ks_Q1 && !IsCougar)
            {
                var Q1Enemy =
                    ObjectManager.Get<Obj_AI_Hero>()
                        .FirstOrDefault(hero => hero.IsValidTarget(Q1.Range) && hero.Health < Q1.GetDamage(hero));

                if (Q1Enemy != null && Q1.IsReady() && Q1Enemy.IsValid)
                    Q1.Cast(Q1Enemy, PacketCasting);
            }

            if (ks_dot && Ignite != SpellSlot.Unknown)
            {
                var dotEnemy =
                    ObjectManager.Get<Obj_AI_Hero>()
                        .FirstOrDefault(
                            hero =>
                                hero.IsValidTarget(600f) &&
                                hero.Health < Player.GetSummonerSpellDamage(hero, Damage.SummonerSpell.Ignite) &&
                                !hero.HasBuff("SummonerDot", true));
                if (dotEnemy != null && Player.Spellbook.GetSpell(Ignite).State == SpellState.Ready && dotEnemy.IsValid)
                {
                    Player.Spellbook.CastSpell(Ignite, dotEnemy);
                }
            }
        }

        private static void AutoHeal(bool healAllies)
        {
            if (Player.HasBuff("Recall"))
                return;

            Obj_AI_Hero Target = healAllies
                ? ObjectManager.Get<Obj_AI_Hero>()
                    .OrderBy(hero => hero.Health)
                    .First(hero => hero.IsAlly && hero.IsValidTarget(E1.Range, false))
                : Target = Player;

            if (E1.IsReady() && Target.Health / Target.MaxHealth < Config.Item("autoHealPct").GetValue<Slider>().Value / 100f)
                E1.CastOnUnit(Target, PacketCasting);

        }

        private static bool IsCougar
        {
            get { return Player.BaseSkinName != "Nidalee"; }
        }
    }
}