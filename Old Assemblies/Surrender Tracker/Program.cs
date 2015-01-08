using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;

namespace Surrender_Tracker
{
    class Program
    {

        private static Dictionary<string, string> ChatColors = new Dictionary<string, string>();

        static void Main(string[] args)
        {
            ChatColors.Add("default", "#FFAA00");
            ChatColors.Add("timestamp", "#D7D7D7");
            ChatColors.Add("T_ally", "#00FF00");
            ChatColors.Add("T_enemy", "#FA3232");

            Game.OnGameProcessPacket += Game_OnGameProcessPacket;
            CustomEvents.Game.OnGameLoad += delegate(EventArgs eventArgs)
            {
                Game.PrintChat(">> Surrender Tracker loaded!");
            };
        }

        static void Game_OnGameProcessPacket(GamePacketEventArgs args)
        {
            try
            {
                if (args.PacketData[0] == 201)
                {
                    var packet = new GamePacket(args.PacketData);
                    packet.Position = 6;
                    var networkid = packet.ReadInteger();
                    packet.Position = 11;
                    bool declined = packet.ReadByte() > 0;

                    var player = ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.NetworkId == networkid).ToArray()[0];
                    if (player != null)
                    {
                        TimeSpan timestamp = TimeSpan.FromSeconds(Game.Time);
                        Game.PrintChat(string.Format("<font color=\"{0}\">[{1:D2}:{2:D2}]</font> <font color=\"{3}\">{4} ({5})</font> <font color=\"{6}\">{7}</font> <font color=\"{8}\">the surrender.</font>",
                                                     ChatColors["timestamp"],
                                                     timestamp.Minutes,
                                                     timestamp.Seconds,
                                                     ChatColors["T_ally"],
                                                     player.Name,
                                                     player.ChampionName,
                                                     declined ? ChatColors["T_enemy"] : ChatColors["T_ally"],
                                                     declined ? "declined" : "accepted",
                                                     ChatColors["default"]));                            
                    }
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine("## Surrender Tracker exception found:");
                Console.WriteLine(exception.ToString());
            }
        }
    }
}
