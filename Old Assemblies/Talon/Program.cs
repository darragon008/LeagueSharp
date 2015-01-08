using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using LX_Orbwalker;
using SharpDX;

namespace Talon
{
    class Program
    {
        private static Obj_AI_Hero Player;
        private static Menu Config;
        private static Spell Q, W, E, R;
        private static SpellSlot Ignite;
        private static Items.Item GB, TMT, HYD, SOTD;

        private static Obj_AI_Hero LockedTarget;
        

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            Player = ObjectManager.Player;
            if (Player.ChampionName != "Talon") return;

            Game.PrintChat("<font color=\"#0066FF\">[<font color=\"#FFFFFF\">madk</font>]</font><font color=\"#FFFFFF\"> Talon assembly loaded! :^)</font>");


            Config = new Menu("Talon", "Talon", true);

            var Menu_TargetSelector = new Menu("Target Selector", "Target Selector");
            SimpleTs.AddToMenu(Menu_TargetSelector);

            var Menu_Orbwalker = new Menu("Orbwalker", "Orbwalker");
            LXOrbwalker.AddToMenu(Menu_Orbwalker);

            var Menu_Combo = new Menu("Combo", "combo");
            Menu_Combo.AddItem(new MenuItem("combo_Q", "Q").SetValue(true));
            Menu_Combo.AddItem(new MenuItem("combo_W", "W").SetValue(true));
            Menu_Combo.AddItem(new MenuItem("combo_E", "E").SetValue(true));
            Menu_Combo.AddItem(new MenuItem("combo_R", "R").SetValue(true));
            Menu_Combo.AddItem(new MenuItem("combo_ITM", "Items").SetValue(true));
            Menu_Combo.AddItem(new MenuItem("combo_IGN", "Ignite").SetValue(true));
            Menu_Combo.AddItem(new MenuItem("combo_RUSH", "Ultimate Rush").SetValue(true));

            var Menu_Harass = new Menu("Harass", "harass");
            Menu_Harass.AddItem(new MenuItem("harass_W", "W").SetValue(true));
            Menu_Harass.AddItem(new MenuItem("harass_mn", "Required MN.").SetValue(new Slider(40, 0, 100)));

            var Menu_Farm = new Menu("Farm", "farm");
            Menu_Farm.AddItem(new MenuItem("farm_Q", "Q").SetValue(true));
            Menu_Farm.AddItem(new MenuItem("farm_W", "W").SetValue(true));

            var Menu_Items = new Menu("Items", "items");
            Menu_Items.AddItem(new MenuItem("item_GB", "Ghostblade").SetValue(true));
            Menu_Items.AddItem(new MenuItem("item_TMT", "Tiamat").SetValue(true));
            Menu_Items.AddItem(new MenuItem("item_HYD", "Hydra").SetValue(true));
            Menu_Items.AddItem(new MenuItem("item_SOTD", "SOTD").SetValue(true));

            var Menu_Drawings = new Menu("Drawings", "drawings");
            Menu_Drawings.AddItem(new MenuItem("draw_W", "W & E").SetValue(new Circle(true, System.Drawing.Color.White)));
            Menu_Drawings.AddItem(new MenuItem("draw_R", "R").SetValue(new Circle(true, System.Drawing.Color.White)));
            
            // From Esk0r's Syndra
            var dmgAfterCombo = Menu_Drawings.AddItem(new MenuItem("draw_Dmg", "Draw HP after Combo").SetValue(true));

            Utility.HpBarDamageIndicator.DamageToUnit = GetComboDamage;
            Utility.HpBarDamageIndicator.Enabled = dmgAfterCombo.GetValue<bool>();
            dmgAfterCombo.ValueChanged += delegate(object sender, OnValueChangeEventArgs eventArgs)
            {
                Utility.HpBarDamageIndicator.Enabled = eventArgs.GetNewValue<bool>();
            };


            Config.AddSubMenu(Menu_TargetSelector);
            Config.AddSubMenu(Menu_Orbwalker);
            Config.AddSubMenu(Menu_Combo);
            Config.AddSubMenu(Menu_Harass);
            Config.AddSubMenu(Menu_Farm);
            Config.AddSubMenu(Menu_Items);
            Config.AddSubMenu(Menu_Drawings);

            Config.AddToMainMenu();

            // Spells
            Q = new Spell(SpellSlot.Q, 0f);
            W = new Spell(SpellSlot.W, 700f);
            E = new Spell(SpellSlot.E, 700f);
            R = new Spell(SpellSlot.R, 500f);

            Ignite = Player.GetSpellSlot("summonerdot", true);

            // Items
            GB = new Items.Item(3142, 0f);
            TMT = new Items.Item(3077, 400f);
            HYD = new Items.Item(3074, 400f);
            SOTD = new Items.Item(3131, 0f);

            Game.OnGameUpdate += OnGameUpdate;
            Drawing.OnDraw += OnDraw;
            LXOrbwalker.AfterAttack += AfterAttack;
        }

        private static void OnGameUpdate(EventArgs args)
        {
            // Reset Locked Target for Ultimate Rush
            if (LockedTarget != null && (LockedTarget.IsDead || Player.IsDead || GetComboDamage(LockedTarget) < LockedTarget.Health))
                LockedTarget = null;

            switch(LXOrbwalker.CurrentMode)
            {
                case LXOrbwalker.Mode.Combo:
                    doCombo();
                    break;
                case LXOrbwalker.Mode.Harass:
                    doHarass();
                    break;
                case LXOrbwalker.Mode.LaneClear:
                    doFarm();
                    break;
            }
        }

        private static void AfterAttack(Obj_AI_Base unit, Obj_AI_Base target)
        {
            var useQC = Config.Item("combo_Q").GetValue<bool>();
            var useQF = Config.Item("farm_Q").GetValue<bool>();

            if (!unit.IsMe) return;

            if ((LXOrbwalker.CurrentMode == LXOrbwalker.Mode.Combo && useQC) || (LXOrbwalker.CurrentMode == LXOrbwalker.Mode.LaneClear && useQF))
                Q.Cast(Player.Position, true);
        }

        private static void OnDraw(EventArgs args)
        {
            var drawWE = Config.Item("draw_W").GetValue<Circle>();
            var drawR = Config.Item("draw_R").GetValue<Circle>();

            if (drawWE.Active)
                Utility.DrawCircle(Player.Position, W.Range, drawWE.Color);

            if (drawR.Active)
                Utility.DrawCircle(Player.Position, R.Range, drawR.Color);
        }

        private static void doCombo()
        {
            var useW = Config.Item("combo_W").GetValue<bool>();
            var useE = Config.Item("combo_E").GetValue<bool>();
            var useR = Config.Item("combo_R").GetValue<bool>();
            var useI = Config.Item("combo_IGN").GetValue<bool>();

            var useGB = Config.Item("item_GB").GetValue<bool>();
            var useTMT = Config.Item("item_TMT").GetValue<bool>();
            var useHYD = Config.Item("item_HYD").GetValue<bool>();
            var useSOTD = Config.Item("item_SOTD").GetValue<bool>();

            var useRush = Config.Item("combo_RUSH").GetValue<bool>();

            var Target = LockedTarget ?? SimpleTs.GetTarget(1500f, SimpleTs.DamageType.Physical);

            // Ultimate Rush
            if(UltimateRush(Target) && useRush)
            {
                LockedTarget = Target;
                R.Cast();
            }

            // Items
            if (TMT.IsReady() && Target.IsValidTarget(TMT.Range) && useTMT)
                TMT.Cast();

            if (HYD.IsReady() && Target.IsValidTarget(HYD.Range) && useHYD)
                HYD.Cast();

            if (GB.IsReady() && Target.IsValidTarget(LXOrbwalker.GetAutoAttackRange(Player) + (Player.MoveSpeed / 2)) && useGB)
                GB.Cast();

            if(SOTD.IsReady() && Target.IsValidTarget(LXOrbwalker.GetAutoAttackRange(Player)) && useSOTD)
                SOTD.Cast();

            // Spells
            if (E.IsReady() && Target.IsValidTarget(E.Range) && useE)
                E.CastOnUnit(Target);
            else if (W.IsReady() && Target.IsValidTarget(W.Range) && useW)
                W.Cast(Target.Position);
            else if (R.IsReady() && Target.IsValidTarget(R.Range) && useR && R.GetDamage(Target) > Target.Health)
                R.Cast();
            
            // Auto Ignite
            if (!useI || Ignite == SpellSlot.Unknown || Player.SummonerSpellbook.CanUseSpell(Ignite) != SpellState.Ready)
                return;

            foreach(var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsEnemy && hero.IsValidTarget(600f) && !hero.IsDead && hero.Health < Player.GetSummonerSpellDamage(hero, Damage.SummonerSpell.Ignite)).OrderByDescending(SimpleTs.GetPriority))
            {
                Player.SummonerSpellbook.CastSpell(Ignite, enemy);
                return;
            }
        }

        private static void doHarass()
        {
            var Target = SimpleTs.GetTarget(W.Range, SimpleTs.DamageType.Physical);

            var useW = Config.Item("harass_W").GetValue<bool>();
            var reqMN = Config.Item("harass_mn").GetValue<Slider>();

            if (useW && W.IsReady() && Player.Mana > (Player.MaxMana * reqMN.Value / 100))
                W.Cast(Target.Position);
        }

        private static void doFarm()
        {
            if (!Config.Item("farm_W").GetValue<bool>()) return;

            // Logic from HellSing's ViktorSharp
            var Minions = MinionManager.GetMinions(Player.Position, W.Range, MinionTypes.All, MinionTeam.NotAlly);
            var hitCount = 0;
            Obj_AI_Base target = null;
            foreach(var Minion in Minions)
            {
                var hits = MinionManager.GetBestLineFarmLocation((from mnion in MinionManager.GetMinions(Minion.Position, W.Range - Player.Distance(Minion.Position), MinionTypes.All, MinionTeam.NotAlly) select mnion.Position.To2D()).ToList<Vector2>(), 300f, W.Range).MinionsHit;

                if (hitCount >= hits) continue;

                hitCount = hits;
                target = Minion;
            }

            if (target != null)
                W.Cast(target.Position);   
        }

        private static bool UltimateRush(Obj_AI_Hero target)
        {
            return !(Vector3.Distance(Player.Position, target.Position) - E.Range > (Player.MoveSpeed*1.4)*2.5) &&
                   Q.IsReady() && W.IsReady() && E.IsReady() && R.IsReady() &&
                   Player.Spellbook.GetSpell(SpellSlot.R).Name != "talonshadowassaulttoggle" &&
                   !(GetComboDamage(target) < target.Health);
        }

        private static float GetComboDamage(Obj_AI_Base target)
        {
            double DamageDealt = 0;

            var useQ = Config.Item("combo_Q").GetValue<bool>();
            var useW = Config.Item("combo_W").GetValue<bool>();
            var useE = Config.Item("combo_E").GetValue<bool>();
            var useR = Config.Item("combo_R").GetValue<bool>();
            var useRUSH = Config.Item("combo_RUSH").GetValue<bool>();
            var useTMT = Config.Item("item_TMT").GetValue<bool>();
            var useHYD = Config.Item("item_HYD").GetValue<bool>();
            var useSOTD = Config.Item("item_SOTD").GetValue<bool>();

            // Q
            if(Q.IsReady() && useQ)
                DamageDealt += DamageDealt += Q.GetDamage(target);
            

            // W
            if(W.IsReady() && useW)
                DamageDealt += W.GetDamage(target);

            // R
            if(R.IsReady() && (useR || useRUSH))
                DamageDealt += R.GetDamage(target);

            // Double AA + SOTD
            int SOTDbonus = SOTD.IsReady() && useSOTD ? 2 : 1;
            DamageDealt += ((Player.GetAutoAttackDamage(target) * 1.1 * (Q.IsReady() ? 2 : 1)) * SOTDbonus);


            //  Tiamat
            if (TMT.IsReady() && useTMT)
                DamageDealt += Player.GetItemDamage(target, Damage.DamageItems.Tiamat);


            // Hydra
            if (HYD.IsReady() && useHYD)
                DamageDealt += Player.GetItemDamage(target, Damage.DamageItems.Hydra);

            // E damage amplification
            double[] Amp = { 0, 1.03, 1.06, 1.09, 1.12, 1.15 };

            if(E.IsReady() && useE)
                DamageDealt += DamageDealt * Amp[E.Level];

            return (float)DamageDealt;
        }

    }
}
