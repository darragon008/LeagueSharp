using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using LX_Orbwalker;
using SharpDX;
using Color = System.Drawing.Color;

namespace Rengar
{
    internal class Program
    {
        private static Obj_AI_Hero Player;
        private static Items.Item YGB, TMT, HYD, BCL, BRK, DFG;
        private static Spell Q, W, E, R;
        private static Menu Config;

        private static float LastETick;

        private static bool UsePackets
        {
            get { return Config.Item("usePackets").GetValue<bool>(); }
        }

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += delegate(EventArgs eventArgs)
            {
                try
                {
                    Player = ObjectManager.Player;
                    if (Player.ChampionName != "Rengar") return;

                    #region Menu 

                    Config = new Menu("Rengar", "Rengark", true);

                    var Menu_Orbwalker = new Menu("Orbwalker", "Orbwalker");
                    LXOrbwalker.AddToMenu(Menu_Orbwalker);

                    var Menu_STS = new Menu("Target Selector", "Target Selector");
                    SimpleTs.AddToMenu(Menu_STS);

                    // Keys
                    var KeyBindings = new Menu("Key Bindings", "KB");
                    KeyBindings.AddItem(
                        new MenuItem("KeysCombo", "Combo").SetValue(
                            new KeyBind(Menu_Orbwalker.Item("Combo_Key").GetValue<KeyBind>().Key, KeyBindType.Press, false)));
                    KeyBindings.AddItem(
                        new MenuItem("KeysMixed", "Harass").SetValue(
                            new KeyBind(Menu_Orbwalker.Item("Harass_Key").GetValue<KeyBind>().Key, KeyBindType.Press, false)));
                    KeyBindings.AddItem(
                        new MenuItem("KeysLaneClear", "Lane/Jungle Clear").SetValue(
                            new KeyBind(Menu_Orbwalker.Item("LaneClear_Key").GetValue<KeyBind>().Key, KeyBindType.Press,
                                false)));
                    KeyBindings.AddItem(
                        new MenuItem("KeysLastHit", "Last Hit").SetValue(
                            new KeyBind(Menu_Orbwalker.Item("LaneFreeze_Key").GetValue<KeyBind>().Key, KeyBindType.Press, false)));
                    KeyBindings.AddItem(
                        new MenuItem("KeysE", "Cast E").SetValue(new KeyBind("T".ToCharArray()[0], KeyBindType.Press)));
                    var FeroSwitcher =
                        KeyBindings.AddItem(
                            new MenuItem("KeysFS", "Switch Empowered spell").SetValue(new KeyBind("N".ToCharArray()[0],
                                KeyBindType.Press)));

                    // Combo
                    var Combo = new Menu("Combo", "Combo");
                    Combo.AddItem(
                        new MenuItem("FeroSpellC", "Empowered Spell").SetValue(new StringList(new[] { "Q", "W", "E" }, 2)));
                    Combo.AddItem(new MenuItem("ForceWC", "Force W %HP").SetValue(new Slider(30)));
                    Combo.AddItem(new MenuItem("ForceEC", "Force E").SetValue(false));

                    // Harass
                    var Harass = new Menu("Harass", "Harass");
                    Harass.AddItem(new MenuItem("HarassW", "W").SetValue(true));
                    Harass.AddItem(new MenuItem("HarassE", "E").SetValue(true));
                    Harass.AddItem(
                        new MenuItem("FeroSpellH", "Empowered Spell").SetValue(new StringList(new[] { "OFF", "W", "E" })));

                    // Lane Clear
                    var LaneClear = new Menu("Lane/Jungle Clear", "LJC");
                    LaneClear.AddItem(new MenuItem("FeroSaveRRdy", "Save 5 Ferocity").SetValue(true));
                    LaneClear.AddItem(
                        new MenuItem("FeroSpellF", "Ferocity").SetValue(new StringList(new[] {"Q", "W", "E"}, 1)));
                    LaneClear.AddItem(new MenuItem("ForceWF", "Force W %HP").SetValue(new Slider(70)));

                    // LastHit
                    var LastHit = new Menu("Last Hit", "LH");
                    LastHit.AddItem(new MenuItem("LastHitW", "W").SetValue(true));
                    LastHit.AddItem(new MenuItem("LastHitE", "E").SetValue(true));
                    LastHit.AddItem(
                        new MenuItem("FeroSpellLH", "Empowered Spell").SetValue(new StringList(new[] {"OFF", "W", "E"})));

                    // Drawings
                    var Drawings = new Menu("Drawings", "Drawings");
                    Drawings.AddItem(new MenuItem("DrawW", "W Range").SetValue(true));
                    Drawings.AddItem(new MenuItem("DrawE", "E Range").SetValue(true));
                    Drawings.AddItem(new MenuItem("DrawES", "E: Search").SetValue(true));
                    Drawings.AddItem(
                        new MenuItem("DrawUR", "R").SetValue(new StringList(new[] {"Off", "Normal", "Minimap", "Both"},
                            2)));
                    Drawings.AddItem(new MenuItem("DrawFS", "Empowered Spell").SetValue(true));

                    Config.AddSubMenu(Menu_Orbwalker);
                    Config.AddSubMenu(Menu_STS);
                    Config.AddSubMenu(KeyBindings);
                    Config.AddSubMenu(Combo);
                    Config.AddSubMenu(Harass);
                    Config.AddSubMenu(LaneClear);
                    Config.AddSubMenu(LastHit);
                    Config.AddSubMenu(Drawings);

                    Config.AddItem(new MenuItem("usePackets", "Use Packets").SetValue(false));

                    Config.AddToMainMenu();

                    #endregion

                    #region Items 

                    var map = Utility.Map.GetMap()._MapType;
                    var DFGId = (map == Utility.Map.MapType.TwistedTreeline || map == Utility.Map.MapType.CrystalScar)
                        ? 3128
                        : 3188;

                    YGB = new Items.Item(3142, float.MaxValue); // Ghostblade
                    TMT = new Items.Item(3077, 400f); // Tiamat
                    HYD = new Items.Item(3074, 400f); // Hydra
                    BCL = new Items.Item(3144, 450f); // Cutlass
                    BRK = new Items.Item(3153, 450f); // Blade of the Ruined King
                    DFG = new Items.Item(DFGId, 750f); // Deathfire Grasp

                    #endregion

                    #region Spells 

                    Q = new Spell(SpellSlot.Q);
                    W = new Spell(SpellSlot.W, 500f);
                    E = new Spell(SpellSlot.E, 1000f);
                    R = new Spell(SpellSlot.R);

                    E.SetSkillshot(.5f, 70f, 1500f, true, SkillshotType.SkillshotLine);

                    #endregion

                    Game.PrintChat(
                        "<font color=\"#0066FF\">[<font color=\"#FFFFFF\">madk</font>]</font><font color=\"#FFFFFF\"> Rengar assembly loaded! :^)</font>");

                    Game.OnGameUpdate += OnGameUpdate;
                    Obj_AI_Hero.OnProcessSpellCast += OnProcessSpell;
                    LXOrbwalker.BeforeAttack += BeforeAttack;
                    LXOrbwalker.AfterAttack += AfterAttack;
                    Drawing.OnDraw += OnDraw;
                    Drawing.OnEndScene += OnDraw_EndScene;


                    FeroSwitcher.ValueChanged += delegate(object sender, OnValueChangeEventArgs vcArgs)
                    {
                        if (vcArgs.GetOldValue<KeyBind>().Active) return;
                         
                        var FeroSpell = Config.Item("FeroSpellC");
                        var OldValues = FeroSpell.GetValue<StringList>();
                        var NewValue = OldValues.SelectedIndex + 1 >= OldValues.SList.Count()
                            ? 0
                            : OldValues.SelectedIndex + 1;
                        FeroSpell.SetValue(new StringList(OldValues.SList, NewValue));
                    };
                }
                catch (Exception ex)
                {
                    Game.PrintChat(
                        "<font color=\"#0066FF\">[<font color=\"#FFFFFF\">madk</font>]</font> <font color=\"#FFFFFF\">An error ocurred loading Rengar assembly.</font>");
                    
                    Console.WriteLine("~~~ Rengar Exeption found: ~~~");
                    Console.WriteLine(ex);
                }
            };
        }

        private static void OnGameUpdate(EventArgs args)
        {

            if (!Player.HasBuff("RengarR", true))
                LXOrbwalker.SetAttack(true);

            #region Cast E to mouse
            var useE = Config.Item("KeysE").GetValue<KeyBind>();
            if (useE.Active && E.IsReady())
            {
                var ForceE = Config.Item("ForceEC").GetValue<bool>();
                Vector3 SearchPosition;

                if (Player.Distance(Game.CursorPos) < E.Range - 200f)
                    SearchPosition = Game.CursorPos;
                else
                    SearchPosition = Player.Position +
                                     Vector3.Normalize(Game.CursorPos - Player.Position)*(E.Range - 200f);

                var Target =
                    ObjectManager.Get<Obj_AI_Hero>()
                        .Where(hero => hero.IsValidTarget(E.Range) && hero.Distance(SearchPosition) < 200f)
                        .OrderByDescending(hero => SimpleTs.GetPriority(hero))
                        .First();
                if (Target.IsValid &&
                    (!Target.HasBuff("RengarEFinalMAX", true) && !Target.HasBuff("rengareslow") &&
                     LastETick + 1500 < Environment.TickCount || ForceE))
                    E.Cast(Target, UsePackets);
            }
            #endregion

            // Current Mode
            switch (ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.Combo:
                    doCombo();
                    break;
                case Orbwalking.OrbwalkingMode.LaneClear:
                    doFarm();
                    break;
                case Orbwalking.OrbwalkingMode.Mixed:
                    doHarass();
                    break;
                case Orbwalking.OrbwalkingMode.LastHit:
                    doLastHit();
                    break;
            }
        }

        private static void OnProcessSpell(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            // Register tick of last E
            if (sender.IsMe && args.SData.Name == "RengarE")
                LastETick = Environment.TickCount;
        }

        private static void BeforeAttack(LXOrbwalker.BeforeAttackEventArgs eventArgs)
        {
            if (eventArgs.Unit.IsMe && CanCastQ())
                Q.CastOnUnit(Player, UsePackets);
        }

        private static void AfterAttack(Obj_AI_Base unit, Obj_AI_Base target)
        {
            if (unit.IsMe && CanCastQ())
                Q.CastOnUnit(Player, UsePackets);
        }

        #region Drawings

        private static void OnDraw(EventArgs args)
        {
            if (Player.IsDead) 
                return;

            var drawW = Config.Item("DrawW").GetValue<bool>();
            var drawE = Config.Item("DrawE").GetValue<bool>();
            var drawES = Config.Item("DrawES").GetValue<bool>();
            var drawFS = Config.Item("DrawFS").GetValue<bool>();
            var drawUR = Config.Item("DrawUR").GetValue<StringList>();

            // W Range
            if (drawW)
                Utility.DrawCircle(Player.Position, W.Range, W.IsReady() ? Color.Green : Color.DarkRed, 1);

            // E Range
            if (drawE)
                Utility.DrawCircle(Player.Position, E.Range, E.IsReady() ? Color.Green : Color.DarkRed, 1);

            // E Search Position
            if (drawES && Config.Item("KeysE").GetValue<KeyBind>().Active)
            {
                Vector3 SearchPosition;

                if (Player.Distance(Game.CursorPos) < E.Range - 200f)
                    SearchPosition = Game.CursorPos;
                else
                    SearchPosition = Player.Position +
                                     Vector3.Normalize(Game.CursorPos - Player.Position)*(E.Range - 200f);

                Utility.DrawCircle(SearchPosition, 200f, E.IsReady() ? Color.Green : Color.DarkRed, 1);
            }

            // Ultimate Range
            if (R.Level > 0 && (drawUR.SelectedIndex == 1 || drawUR.SelectedIndex == 3))
                Utility.DrawCircle(Player.Position, 1000f + 1000f*R.Level, Color.Green, 10);

            // Ferocity Spell
            if (drawFS)
            {
                var FeroSpell = Config.Item("FeroSpellC").GetValue<StringList>();

                var posX = Drawing.WorldToMinimap(new Vector3()).X < Drawing.Width/2 ? Drawing.Width - 210 : 10;

                Drawing.DrawText(posX, (Drawing.Height*0.85f), Color.YellowGreen, "Empowered Spell: {0}",
                    FeroSpell.SList[FeroSpell.SelectedIndex]);
            }
        }

        private static void OnDraw_EndScene(EventArgs args)
        {
            if (Player.IsDead) 
                return;

            // Draw Ultimate Range on Minimap
            var drawUR = Config.Item("DrawUR").GetValue<StringList>();

            if (drawUR.SelectedIndex > 1 && R.Level > 0)
                Utility.DrawCircle(Player.Position, 1000f + 1000f*R.Level, Color.Green, 1, 30, true);
        }

        #endregion

        #region Combos

        private static void doCombo()
        {
            try
            {

                // Menu Config
                var FeroSpell = Config.Item("FeroSpellC").GetValue<StringList>();
                var ForceW = Config.Item("ForceWC").GetValue<Slider>();
                var ForceE = Config.Item("ForceEC").GetValue<bool>();

                var Target = SimpleTs.GetSelectedTarget() ?? SimpleTs.GetTarget(1600f, SimpleTs.DamageType.Physical);

                // Force Leap to target
                if (Player.HasBuff("RengarR", true))
                {
                    LXOrbwalker.ForcedTarget = Target;
                    LXOrbwalker.SetAttack(LXOrbwalker.GetPossibleTarget() == Target);
                }

                // Use Tiamat / Hydra
                if (Target.IsValidTarget(TMT.Range))
                    if (TMT.IsReady()) TMT.Cast();
                    else if (HYD.IsReady()) HYD.Cast();

                // Use Yommus Ghostblade
                if (YGB.IsReady() && Target.IsValidTarget(Orbwalking.GetRealAutoAttackRange(Player) + (Player.HasBuff("RengarR", true) ? Player.MoveSpeed / 2 : 0)))
                    YGB.Cast();

                // Cutlass
                if (BCL.IsReady() && Target.IsValidTarget(BCL.Range))
                    BCL.Cast(Target);

                // BORK
                if (BRK.IsReady() && Target.IsValidTarget(BRK.Range))
                    BRK.Cast(Target);

                // DFG
                if (W.IsReady() && DFG.IsReady() && Target.IsValidTarget(DFG.Range))
                    DFG.Cast(Target);

                // Ferocity Spell
                if (Player.Mana == 5)
                {
                    if (Player.Health/Player.MaxHealth < ForceW.Value/100f && Target.IsValidTarget(W.Range))
                    {
                        W.CastOnUnit(Player, UsePackets);
                        return;
                    }

                    switch (FeroSpell.SelectedIndex)
                    {
                        case 1:
                            if (!Target.IsValidTarget(W.Range))
                                return;
                            W.CastOnUnit(Player, UsePackets);
                            break;
                        case 2:
                            if (!Target.IsValidTarget(E.Range) || Player.HasBuff("RengarR", true))
                                return;
                            E.Cast(Target, UsePackets);
                            break;
                    }
                    return;
                }

                // Don't cast W or E while ultimate is active (force leap)
                if (Player.HasBuff("RengarR", true))
                    return;

                if (E.IsReady() && Target.IsValidTarget(E.Range) &&
                    (!Target.HasBuff("RengarEFinalMAX", true) && !Target.HasBuff("rengareslow") &&
                     LastETick + 1500 < Environment.TickCount || ForceE))
                    E.Cast(Target, UsePackets);

                if (W.IsReady() && Target.IsValidTarget(W.Range))
                    W.CastOnUnit(Player, UsePackets);
            }
            catch (Exception e)
            {
                Console.WriteLine("Combo Exception: {0}", e);
            }
        }

        private static void doFarm()
        {
            var SaveFero = Config.Item("FeroSaveRRdy").GetValue<bool>();
            var FeroSpell = Config.Item("FeroSpellF").GetValue<StringList>();
            var ForceW = Config.Item("ForceWF").GetValue<Slider>();
            var Target = LXOrbwalker.GetPossibleTarget();

            // Save Ferocity
            if (SaveFero && R.IsReady() && Player.Mana == 5) return;

            // Ferocity Spells
            if (Player.Mana == 5)
            {
                if (Target.IsValidTarget(W.Range) &&
                    (Player.Health/Player.MaxHealth <= ForceW.Value/100f || FeroSpell.SelectedIndex == 1))
                    W.CastOnUnit(Player, UsePackets);

                if (Target.IsValidTarget(E.Range) && FeroSpell.SelectedIndex == 2)
                    E.Cast(Target, UsePackets);

                return;
            }

            // Normal Spells
            if (Q.IsReady() && Target.IsValidTarget(Orbwalking.GetRealAutoAttackRange(Player))) Q.CastOnUnit(Player, UsePackets);
            if (W.IsReady() && Target.IsValidTarget(W.Range)) W.CastOnUnit(Player, UsePackets);
            if (E.IsReady() && Target.IsValidTarget(E.Range)) E.Cast(Target, UsePackets);
        }

        private static void doHarass()
        {
            var Target = SimpleTs.GetTarget(E.Range, SimpleTs.DamageType.Physical);
            var useW = Config.Item("HarassW").GetValue<bool>();
            var useE = Config.Item("HarassE").GetValue<bool>();
            var FeroSpell = Config.Item("FeroSpellH").GetValue<StringList>();

            if (Player.Mana == 5)
            {
                if (FeroSpell.SelectedIndex == 1 && Target.IsValidTarget(W.Range))
                    W.CastOnUnit(Player, UsePackets);
                if (FeroSpell.SelectedIndex == 2 && Target.IsValidTarget(E.Range))
                    E.Cast(Target, UsePackets);

                return;
            }

            if (useW && W.IsReady() && Target.IsValidTarget(W.Range))
                W.CastOnUnit(Player, UsePackets);

            if (useE && E.IsReady() && Target.IsValidTarget(E.Range))
                E.Cast(Target, UsePackets);
        }

        private static void doLastHit()
        {
            var useW = Config.Item("LastHitW").GetValue<bool>();
            var useE = Config.Item("LastHitE").GetValue<bool>();
            var FeroSpell = Config.Item("FeroSpellLH").GetValue<StringList>();

            if (Player.Mana == 5 && FeroSpell.SelectedIndex == 0) return;

            foreach (
                var minion in
                    MinionManager.GetMinions(Player.Position, E.Range, MinionTypes.All, MinionTeam.NotAlly))
            {
                if (minion.IsValidTarget(Orbwalking.GetRealAutoAttackRange(Player)) && LXOrbwalker.GetPossibleTarget() == minion)
                    return;

                if (useW && W.IsReady() && minion.IsValidTarget(W.Range) && minion.Health < W.GetDamage(minion) &&
                    (Player.Mana == 5 ? FeroSpell.SelectedIndex == 1 : true))
                {
                    W.CastOnUnit(Player, UsePackets);
                    return;
                }

                if (useE && E.IsReady() && minion.IsValidTarget(E.Range) &&
                    HealthPrediction.GetHealthPrediction(minion, (int) (Player.Distance(minion)*E.Speed)) <
                    E.GetDamage(minion) && (Player.Mana == 5 ? FeroSpell.SelectedIndex == 1 : true))
                {
                    E.Cast(minion, UsePackets);
                    return;
                }
            }
        }

        private static bool CanCastQ()
        {
            if (!Q.IsReady() || !(ActiveMode == Orbwalking.OrbwalkingMode.Combo || ActiveMode == Orbwalking.OrbwalkingMode.LaneClear))
                return false;

            var EmpoweredQ =
                Config.Item("FeroSpell" + (ActiveMode == Orbwalking.OrbwalkingMode.Combo ? "C" : "F"))
                    .GetValue<StringList>()
                    .SelectedIndex == 0;

            var SaveFerocity = ActiveMode == Orbwalking.OrbwalkingMode.LaneClear &&
                                Config.Item("FeroSaveRRdy").GetValue<bool>();

            if (Player.Mana == 5 && (!EmpoweredQ || (SaveFerocity && R.IsReady())))
                return false;

            return true;
        }

        #endregion

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

                if (Config.Item("KeysLastHit").GetValue<KeyBind>().Active)
                    return Orbwalking.OrbwalkingMode.LastHit;

                return Orbwalking.OrbwalkingMode.None;
            }
        }
    }
}