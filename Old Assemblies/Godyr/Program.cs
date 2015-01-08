#region

using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using LX_Orbwalker;

#endregion

namespace Godyr
{
    internal class Program
    {
        private static readonly Obj_AI_Hero Player = ObjectManager.Player;
        private static Menu Config;
        private static Spell Q, W, E, R;

        private static int RFlameCount;

        private static readonly string[] EpicMonsters =
        {
            "Worm", "Dragon", "TT_Spiderboss"
        };

        private static Stances CurrentStance
        {
            get
            {
                switch (GetStanceBuff().DisplayName)
                {
                    case "UdyrTigerStance":
                        return Stances.Tiger;
                    case "UdyrTurtleStance":
                        return Stances.Turtle;
                    case "UdyrBearStance":
                        return Stances.Bear;
                    case "UdyrPhoenixStance":
                        return Stances.Phoneix;
                    default:
                        return Stances.NoStance;
                }
            }
        }

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += delegate
            {
                if (Player.ChampionName != "Udyr") return;

                #region Menu   

                Config = new Menu("Godyr", "Godyr", true);

                var Menu_Orbwalker = new Menu("Orbwalker", "Orbwalker");
                LXOrbwalker.AddToMenu(Menu_Orbwalker);

                var Menu_STS = new Menu("Target Selector", "Target Selector");
                SimpleTs.AddToMenu(Menu_STS);

                var Menu_Combo = new Menu("Combo", "Combo");
                Menu_Combo.AddItem(
                    new MenuItem("comboW", "W").SetValue(new StringList(new[] {"OFF", "Always", "Under Tower"}, 2)));

                var Menu_Shield = new Menu("Shield", "Shield");
                Menu_Shield.AddItem(
                    new MenuItem("comboW", "Combo").SetValue(new StringList(new[] {"OFF", "Always", "Under Tower"}, 2)));
                Menu_Shield.AddItem(
                    new MenuItem("farmW", "Farm").SetValue(new StringList(new[] {"OFF", "Always", "Epic"}, 2)));

                var Menu_Phoenix = new Menu("Phoenix", "Phoneix");
                Menu_Phoenix.AddItem(new MenuItem("stackBlock", "Max Stacks").SetValue(new Slider(2, 0, 3)));
                Menu_Phoenix.AddItem(new MenuItem("flameBlock", "Min Flames").SetValue(new Slider(2, 0, 5)));

                Config.AddSubMenu(Menu_Orbwalker);
                Config.AddSubMenu(Menu_STS);
                Config.AddSubMenu(Menu_Shield);
                Config.AddSubMenu(Menu_Phoenix);

                Config.AddToMainMenu();

                #endregion

                #region Spells

                Q = new Spell(SpellSlot.Q);
                W = new Spell(SpellSlot.W);
                E = new Spell(SpellSlot.E);
                R = new Spell(SpellSlot.R);

                #endregion

                Game.PrintChat(
                    "<font color=\"#0066FF\">[<font color=\"#FFFFFF\">madk</font>]</font><font color=\"#FFFFFF\"> Udyr assembly loaded! :^)</font>");

                Game.OnGameUpdate += Game_OnGameUpdate;
                Obj_AI_Base.OnProcessSpellCast += Obj_AI_Hero_OnProcessSpellCast;
            };
        }

        private static void Obj_AI_Hero_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsMe)
                return;

            if (CurrentStance == Stances.Phoneix && args.SData.Name.ToLower().Contains("spirit"))
                RFlameCount++;
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            if (CurrentStance != Stances.Phoneix && RFlameCount > 0)
                RFlameCount = 0;

            switch (LXOrbwalker.CurrentMode)
            {
                case LXOrbwalker.Mode.Combo:
                    doCombo();
                    break;
                case LXOrbwalker.Mode.LaneClear:
                    doFarm();
                    break;
                case LXOrbwalker.Mode.Flee:
                    if (E.IsReady())
                        E.Cast();
                    break;
            }
        }

        private static void doCombo()
        {
            var StanceBuff = GetStanceBuff();

            var Target = LXOrbwalker.GetPossibleTarget();

            var comboW = Config.Item("comboW").GetValue<StringList>();
            var stackBlock = Config.Item("stackBlock").GetValue<Slider>();
            var flameBlock = Config.Item("flameBlock").GetValue<Slider>();

            var useW = (comboW.SelectedIndex == 1 || comboW.SelectedIndex == 2 && Utility.UnderTurret(Player, true));

            if (Target == null) return;

            switch (CurrentStance)
            {
                case Stances.Tiger:
                    if (R.IsReady())
                        R.Cast();
                    break;

                case Stances.Turtle:
                    if (Q.IsReady())
                        Q.Cast();
                    else if (R.IsReady() && Q.Level == 0)
                        R.Cast();
                    break;

                case Stances.Bear:
                    if (!Target.HasBuff("udyrbearstuncheck", true))
                        return;

                    if (Q.IsReady())
                        Q.Cast();
                    else if (R.IsReady() && Q.Level == 0)
                        R.Cast();
                    break;
                case Stances.Phoneix:
                    if (StanceBuff.Count > stackBlock.Value || RFlameCount < flameBlock.Value)
                        return;

                    if (!Target.HasBuff("udyrbearstuncheck", true) && E.IsReady())
                        E.Cast();
                    else if (W.IsReady() && useW)
                        W.Cast();
                    else if (Q.IsReady() && (W.Level == 0 || !useW))
                        Q.Cast();
                    break;
                default:
                    if (R.IsReady())
                        R.Cast();
                    else if (Q.IsReady())
                        Q.Cast();
                    break;
            }
        }

        private static void doFarm()
        {
            var Target = LXOrbwalker.GetPossibleTarget();
            if (Target == null) return;

            var StanceBuff = GetStanceBuff();

            var farmW = Config.Item("farmW").GetValue<StringList>();
            var stackBlock = Config.Item("stackBlock").GetValue<Slider>();
            var flameBlock = Config.Item("flameBlock").GetValue<Slider>();

            var useW = (farmW.SelectedIndex == 1 ||
                        farmW.SelectedIndex == 2 && EpicMonsters.Any(monster => Target.Name.StartsWith(monster)));

            switch (CurrentStance)
            {
                case Stances.Tiger:
                    if (R.IsReady())
                        R.Cast();
                    break;

                case Stances.Turtle:
                    if (Q.IsReady())
                        Q.Cast();
                    else if (R.IsReady() && Q.Level == 0)
                        R.Cast();
                    break;

                case Stances.Bear:
                    if (Q.IsReady())
                        Q.Cast();
                    else if (R.IsReady() && Q.Level == 0)
                        R.Cast();
                    break;

                case Stances.Phoneix:
                    if (StanceBuff.Count > stackBlock.Value || RFlameCount < flameBlock.Value)
                        return;

                    if (W.IsReady() && useW)
                        W.Cast();
                    else if (Q.IsReady() && (W.Level == 0 || !useW))
                        Q.Cast();
                    break;

                default:
                    if (R.IsReady())
                        R.Cast();
                    else if (Q.IsReady())
                        Q.Cast();
                    break;
            }
        }

        private static BuffInstance GetStanceBuff()
        {
            return Player.Buffs.First(buff => buff.DisplayName.Contains("Stance"));
        }

        private enum Stances
        {
            NoStance,
            Tiger,
            Turtle,
            Bear,
            Phoneix
        }
    }
}