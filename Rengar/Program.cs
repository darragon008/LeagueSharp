using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

namespace Rengar
{
    class Rengar
    {
        private static Menu _config;
        private static Orbwalking.Orbwalker _orbwalker;
        private static Spell _q, _w, _e, _r;
        private static Items.Item _ghostblade, _tiamat, _hydra, _cutlass, _bork, _dfg;

        private static Obj_AI_Hero Player
        {
            get { return ObjectManager.Player; }
        }

        private static bool InUltimate
        {
            get { return Player.HasBuff("RengarRBuff"); }   
        }

        private static bool FullFerocity
        {
            get { return Player.Mana > 4; }
        }

        private static float MyRange
        {
            get { return Orbwalking.GetRealAutoAttackRange(Player);  }
        }

        private static bool UsePackets
        {
            get { return _config.Item("packetCasting").GetValue<bool>(); }
        }

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += OnGameLoad;
        }

        private static void OnGameLoad(EventArgs args)
        {
            if (Player.ChampionName != "Rengar") return;

            // Create Menu
            _config = new Menu("madk's Rengar", "madk_rengar", true);


            // Load Target Selector
            var targetSelector = new Menu("Target Selector", "ts");
            SimpleTs.AddToMenu(targetSelector);
            _config.AddSubMenu(targetSelector);

            // Load Orbwalker
            var orbwalker = new Menu("Orbwalker", "Orbwalker");
            _orbwalker = new Orbwalking.Orbwalker(orbwalker);
            _config.AddSubMenu(orbwalker);


            // Combo
            var comboItems = new List<MenuItem>
            {
                new MenuItem("EmpoweredSpellC", "Empowered Spell").SetValue(new StringList(new[] {"Q", "W", "E"})),
                new MenuItem("ForceWC", "Force W at %HP").SetValue(new Slider(40)),
                // new MenuItem("ForceE", "Force E (Ignore slow/root)").SetValue(true) // todo
            };


            // Harass
            var harassItems = new List<MenuItem>
            {
                new MenuItem("EmpoweredSpellH", "Empowered Spell").SetValue(new StringList(new [] {"OFF", "W", "E"})),
                new MenuItem("ForceWF", "Force W at %HP").SetValue(new Slider(40)),
                new MenuItem("Wh", "W").SetValue(true),
                new MenuItem("Eh", "E").SetValue(true)
            };


            // Farm
            var farmItems = new List<MenuItem>
            {
                new MenuItem("EmpoweredSpellF", "Empowered Spell").SetValue(new StringList(new [] {"OFF", "Q", "W"})),
                new MenuItem("Qf", "Q").SetValue(true),
                new MenuItem("Wf", "W").SetValue(true),
                new MenuItem("Ef", "E").SetValue(true),
                new MenuItem("SaveFerocity", "Save Ferocity (Ult Ready)").SetValue(true)
            };


            // Drawings
            var drawItems = new List<MenuItem>
            {
                new MenuItem("Wd", "W").SetValue(new Circle(true, Color.DarkGreen)),
                new MenuItem("Ed", "E").SetValue(new Circle(true, Color.DarkGreen)),
                new MenuItem("Rd", "R").SetValue(new Circle(true, Color.DarkGreen)),
                new MenuItem("Rdm", "R (mode)").SetValue(new StringList(new [] {"Normal", "Minimap", "Both"})),
                new MenuItem("EmpSpell", "Empowered Spell").SetValue(new Circle(true, Color.DarkOrange)),
                new MenuItem("DSdynamic", "Dynamic Colors").SetValue(true),
                new MenuItem("DSthick", "Thickness").SetValue(new Slider(3, 1, 10))
            };


            // Misc
            var miscItems = new List<MenuItem>
            {
                new MenuItem("packetCasting", "Use Packets").SetValue(true),
                new MenuItem("ChangeESC", "Change Empowered Spell").SetValue(new KeyBind("N".ToCharArray()[0], KeyBindType.Press))
            };


            // Link submenus
            var subMenus = new List<KeyValuePair<Menu,List<MenuItem>>>
            {
                new KeyValuePair<Menu, List<MenuItem>>(new Menu("Combo", "combo"), comboItems),
                new KeyValuePair<Menu, List<MenuItem>>(new Menu("Harass", "harass"), harassItems),
                new KeyValuePair<Menu, List<MenuItem>>(new Menu("Farm", "farm"), farmItems),
                new KeyValuePair<Menu, List<MenuItem>>(new Menu("Drawings", "draw"), drawItems),
                new KeyValuePair<Menu, List<MenuItem>>(new Menu("Misc", "misc"), miscItems)
            };


            // Add items
            foreach (var sm in subMenus)
            {
                sm.Value.ForEach(i => sm.Key.AddItem(i));
                _config.AddSubMenu(sm.Key);
            }
            
            
            // Add menu to the Main Menu
            _config.AddToMainMenu();


            // Empowered Spell Changer
            _config.Item("ChangeESC").ValueChanged += (sender, eventArgs) =>
            {
                if (eventArgs.GetOldValue<KeyBind>().Active) return;

                var eSpell = _config.Item("EmpoweredSpellC");
                var oldValue = eSpell.GetValue<StringList>();
                var newValue = oldValue.SelectedIndex + 1 >= oldValue.SList.Count() ? 0 : oldValue.SelectedIndex + 1;
                eSpell.SetValue(new StringList(oldValue.SList, newValue));
            };
            

            // Spells
            _q = new Spell(SpellSlot.Q);
            _w = new Spell(SpellSlot.W, 500f);
            _e = new Spell(SpellSlot.E, 1000f);
            _r = new Spell(SpellSlot.R);

            _e.SetSkillshot(0.5f, 70f, 1500f, true, SkillshotType.SkillshotLine);


            // Items
            _ghostblade = new Items.Item(3142, float.MaxValue);
            _tiamat = new Items.Item(3077, 400f);
            _hydra = new Items.Item(3074, 400f);
            _cutlass = new Items.Item(3144, 450f);
            _bork = new Items.Item(3153, 450f);
            _dfg = new Items.Item(3188, 750f);


            // Game Events
            Game.OnGameUpdate += OnGameUpdate;
            Drawing.OnDraw += OnDraw;
            Drawing.OnEndScene += OnEndScene;


            Game.PrintChat("<font color=\"#0066FF\">[<font color=\"#FFFFFF\">madk's Rengar</font>]</font> <font color=\"#FFFFFF\">Assembly loaded sucessfully!</font>");
            Game.PrintChat("<font color=\"#0066FF\">[<font color=\"#FFFFFF\">madk's Rengar</font>]</font> <font color=\"#FFFFFF\">Hi, i rewrote this assembly, it's still not finished yet,");
            Game.PrintChat("<font color=\"#FFFFFF\">there should be some bugs that i still need to fix. I do not recommend you using this is rankeds. Have fun :^)");
        }

        private static void OnGameUpdate(EventArgs args)
        {
            switch (_orbwalker.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.Combo:
                    Combo();
                    break;

                case Orbwalking.OrbwalkingMode.Mixed:
                    Harass();
                    break;

                case Orbwalking.OrbwalkingMode.LaneClear:
                    LaneClear();
                    break;

                case Orbwalking.OrbwalkingMode.LastHit:
                    LastHit();
                    break;
            }
        }

        private static void OnDraw(EventArgs args)
        {
            var w = _config.Item("Wd").GetValue<Circle>();
            var e = _config.Item("Ed").GetValue<Circle>();
            var r = _config.Item("Rd").GetValue<Circle>();
            var es = _config.Item("EmpSpell").GetValue<Circle>();
            var rMode = _config.Item("Rdm").GetValue<StringList>().SelectedIndex;
            var dsDynamic = _config.Item("DSdynamic").GetValue<bool>();
            var dsThick = _config.Item("DSthick").GetValue<Slider>().Value;


            if (w.Active && _w.Level > 0)
            {
                Utility.DrawCircle(Player.Position, _w.Range, dsDynamic ? _w.IsReady() ? Color.Green : Color.Red : w.Color, dsThick);
            }

            if (e.Active && _e.Level > 0)
            {
                Utility.DrawCircle(Player.Position, _e.Range, dsDynamic ? _e.IsReady() ? Color.Green : Color.Red : w.Color, dsThick);
            }

            if (r.Active && rMode != 1 && _r.Level > 0)
            {
                Utility.DrawCircle(Player.Position, 1000f + 1000f * _r.Level, dsDynamic ? _r.IsReady() ? Color.Green : Color.Red : r.Color, dsThick);
            }

            if (es.Active)
            {
                var eSpell = _config.Item("EmpoweredSpellC").GetValue<StringList>();

                var posX = Drawing.WorldToMinimap(new Vector3()).X > Drawing.Width/2f ? Drawing.Width - 160 : 10;

                Drawing.DrawText(posX, (Drawing.Height*0.68f), es.Color, "Empowered Spell: {0}", eSpell.SList[eSpell.SelectedIndex]);
            }
        }

        private static void OnEndScene(EventArgs args)
        {
            var r = _config.Item("Rd").GetValue<Circle>();
            var rMode = _config.Item("Rdm").GetValue<StringList>().SelectedIndex;

            var dsDynamic = _config.Item("DSdynamic").GetValue<bool>();

            if (rMode == 0 || !r.Active || _r.Level == 0)
                return;

            Utility.DrawCircle(Player.Position, 1000f + 1000f * _r.Level, dsDynamic ? _r.IsReady() ? Color.Green : Color.Red : r.Color, 1, 30, true); 
        }


        #region Combos
        private static void Combo()
        {
            var empoweredSpell = _config.Item("EmpoweredSpellC").GetValue<StringList>().SelectedIndex;
            var forceW = _config.Item("ForceWC").GetValue<Slider>().Value;

            var target = SimpleTs.GetSelectedTarget() ?? SimpleTs.GetTarget(1000f, SimpleTs.DamageType.Physical);

            if (_ghostblade.IsReady() && target.IsValidTarget(MyRange + (InUltimate ? Player.MoveSpeed / 2 : 0)))
            {
                _ghostblade.Cast();   
            }

            if (InUltimate)
            {
                if (target.IsValidTarget(MyRange))
                {
                    if ((!FullFerocity || empoweredSpell == 0) && _q.IsReady())
                        _q.Cast();

                    AttackUnit(target, UsePackets);
                }
                else
                {
                    Player.IssueOrder(GameObjectOrder.MoveTo, target.Position);
                }
                return;
            }

            if (target.IsValidTarget(_tiamat.Range))
            {
                if (_tiamat.IsReady())
                {
                    _tiamat.Cast();
                }
                else if (_hydra.IsReady())
                {
                    _hydra.Cast();
                }
            }

            if (target.IsValidTarget(_cutlass.Range))
            {
                if (_bork.IsReady())
                {
                    _bork.Cast(target);
                }
                else if (_cutlass.IsReady())
                {
                    _cutlass.Cast(target);
                }
            }

            if (target.IsValidTarget() && _dfg.IsReady())
            {
                // todo: add W damage calculation
                _dfg.Cast(target);
            }

            if (FullFerocity)
            {
                // Force W
                if (Player.Health / Player.MaxHealth <= forceW / 100f && target.IsValidTarget(_w.Range))
                {
                    _w.Cast();
                    return;
                }

                switch (empoweredSpell)
                {
                    case 0: // Q
                        if (target.IsValidTarget(MyRange))
                        {
                            _q.Cast();
                            AttackUnit(target, UsePackets);
                        }
                        break;

                    case 1: // W
                        if (target.IsValidTarget(_w.Range))
                        {
                            _w.Cast();
                        }
                        break;

                    case 2: // E
                        if (target.IsValidTarget(_e.Range))
                        {
                            _e.Cast();
                        }
                        break;
                }

                return;
            }


            if (_q.IsReady() && target.IsValidTarget(MyRange))
            {
                _q.Cast();
                AttackUnit(target, UsePackets);
            }

            if (_w.IsReady() && target.IsValidTarget(_w.Range))
            {
                _w.Cast();
            }

            if (_e.IsReady() && target.IsValidTarget(_e.Range))
            {
                _e.Cast();
            }
        }

        private static void Harass()
        {
            var target = SimpleTs.GetTarget(1000f, SimpleTs.DamageType.Physical);

            var useW = _config.Item("harassW").GetValue<bool>();
            var useE = _config.Item("harassE").GetValue<bool>();

            if (useW && _w.IsReady() && target.IsValidTarget(_w.Range))
            {
                _w.Cast();
            }

            if (useE && _e.IsReady() && target.IsValidTarget(_e.Range))
            {
                _e.Cast(target);
            }
        }

        private static void LaneClear()
        {
            var target = _orbwalker.GetTarget();

            var eSpell = _config.Item("EmpoweredSpellF").GetValue<StringList>().SelectedIndex;

            var useQ = _config.Item("Qf").GetValue<bool>();
            var useW = _config.Item("Wf").GetValue<bool>();
            var useE = _config.Item("Ef").GetValue<bool>();

            var forceW = _config.Item("ForceWF").GetValue<Slider>().Value;

            if (FullFerocity)
            {
                if (_config.Item("SaveFerocity").GetValue<bool>() && _r.IsReady())
                    return;

                if (Player.Health/Player.MaxHealth <= forceW/100f && target.IsValidTarget(_w.Range))
                {
                    _w.Cast();
                    return;
                }

                switch (eSpell)
                {
                    case 1:
                        if (target.IsValidTarget(MyRange))
                        {
                            _q.Cast();
                            AttackUnit(target, UsePackets);
                        }
                        break;

                    case 2:
                        if (target.IsValidTarget(_w.Range))
                        {
                            _w.Cast();
                        }
                        break;
                }

                return;
            }

            if (useQ && _q.IsReady() && target.IsValidTarget(MyRange))
            {
                _q.Cast();
                AttackUnit(target, UsePackets);
            }

            if (useW && _w.IsReady() && target.IsValidTarget(_w.Range))
            {
                _w.Cast();
            }

            if (useE && _w.IsReady() && target.IsValidTarget(_e.Range))
            {
                _e.Cast(target);
            }
        }

        private static void LastHit()
        {
            foreach (var minion in MinionManager.GetMinions(Player.Position, _e.Range))
            {
                if (minion.Health <= _w.GetDamage(minion) && _w.IsReady() && minion.IsValidTarget(_w.Range))
                {
                    _w.Cast();
                }
                else if (minion.Health <= _e.GetDamage(minion) && _e.IsReady() && minion.IsValidTarget(_e.Range))
                {
                    if (_e.GetPrediction(minion).CollisionObjects.Count > 0)
                        continue;

                    _e.Cast(minion);
                }
            }
        }
        #endregion


        private static void AttackUnit(Obj_AI_Base unit, bool usePackets)
        {
            if (!usePackets)
            {
                Player.IssueOrder(GameObjectOrder.AttackUnit, unit);
                return;
            }

            var pStruct = new Packet.C2S.Move.Struct
            {
                MoveType = 3,
                SourceNetworkId = Player.NetworkId,
                TargetNetworkId = unit.NetworkId
            };

            var packet = Packet.C2S.Move.Encoded(pStruct);

            packet.Send();
        }
    }
}
