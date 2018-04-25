using System;

namespace Riskord
{

    public class Player
    {
        public string Name { get; set; }
        public int XTroops { get; set; }
        public bool CanPlace { get; set; } = false;
        public bool CanAttack { get; set; } = false;
        public bool CanFortify { get; set; } = false;
        public bool CanDraw { get; set; } = false;

        public Player()
        {
            Name = String.Empty;
            XTroops = 0;
        }

        public Player(string _name, int _xtroops)
        {
            Name = _name;
            XTroops = _xtroops;
        }

        public Player(string _name, int _xtroops, bool _place, bool _attack, bool _fortify, bool _draw)
        {
            Name = _name;
            XTroops = _xtroops;
            CanPlace = _place;
            CanAttack = _attack;
            CanFortify = _fortify;
            CanDraw = _draw;
        }
    }
}
