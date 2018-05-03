using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Riskord
{

    public class Player
    {
        public ulong Id { get; set; }
        public int XTroops { get; set; }
        public bool CanPlace { get; set; }
        public bool CanAttack { get; set; }
        public bool CanFortify { get; set; }
        public bool CanDraw { get; set; }
        public string LastFrom { get; set; }
        public List<string> Cards { get; set; }

        public Player()
        {
            Id = 0L;
            XTroops = 0;
            CanPlace = false;
            CanAttack = false;
            CanFortify = false;
            CanDraw = false;
            LastFrom = String.Empty;
            Cards = new List<string>();
        }

        public Player(ulong _id, int _xtroops) : this()
        {
            Id = _id;
            XTroops = _xtroops;
        }

        public static string Lookup(ulong id)
        {
            var contents = File.ReadAllText("ids.pdo");
            var players = JsonConvert.DeserializeObject<Dictionary<ulong, string>>(contents);
            return players[id];
        }
    }
}
