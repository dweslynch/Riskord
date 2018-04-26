using System;

namespace Riskord
{

    public class Player
    {
        public string Name { get; set; }
        public int XTroops { get; set; }
        public bool CanPlace { get; set; }
        public bool CanAttack { get; set; }
        public bool CanFortify { get; set; }
        public bool CanDraw { get; set; }
        public string LastFrom { get; set; }

        public Player()
        {
            Name = String.Empty;
            XTroops = 0;
            CanPlace = false;
            CanAttack = false;
            CanFortify = false;
            CanDraw = false;
            LastFrom = String.Empty;
        }

        public Player(string _name, int _xtroops) : this()
        {
            Name = _name;
            XTroops = _xtroops;
        }
    }
}
