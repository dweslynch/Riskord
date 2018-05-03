using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System.IO;

namespace Riskord
{
    public class GameMaster
    {
        private Random randy = new Random();
        private Dictionary<string, int> continentvalues = JsonConvert.DeserializeObject<Dictionary<string, int>>(File.ReadAllText("default.values.pdo"));

        public Board Board { get; set; }
        public List<Player> Players { get; set; }
        public int Turn { get; set; }
        public List<string> TCards { get; set; }
        public int TCardValue { get; set; }

        public Player CurrentPlayer
        {
            get => Players[Turn];
        }

        public String CurrentUsername
        {
            get => Player.Lookup(CurrentPlayer.Id);
        }

        public GameMaster()
        {
            Board = new Board();
            Players = new List<Player>();
            Turn = 0;
            TCards = new List<string>();
            TCardValue = 4;
        }

        public GameMaster(Board b, List<Player> players)
        {
            this.Board = b;
            this.Players = players;
            this.Turn = 0;
            TCards = new List<string>();
            TCardValue = 4;
        }
        public GameMaster(Dictionary<string, List<string>> adj, List<Player> players, Dictionary<string, ControlRecord> territories, Dictionary<string, List<string>> cont)
        {
            this.Board = new Board(adj, territories, cont);
            this.Players = players;
            this.Turn = 0;
            TCards = new List<string>();
            TCardValue = 4;
        }

        private bool HasTerritory(ulong id, string territory) =>
            Board.Territories[territory].Id == id;

        private bool HasTerritory(int iplayer, string territory) =>
            Board.Territories[territory].Id == Players[iplayer].Id;

        public int PlayerIndexFromId(ulong id) =>
            Players.FindIndex(x => x.Id == id);

        private int XTerritories(int iplayer) =>
            Board.Territories.Values.Where(t => t.Id == Players[iplayer].Id).Count();

        private int Bonus(int xterr)
        {
            switch (xterr)
            {
                case 41:
                case 40:
                case 39:
                    return 13;
                case 38:
                case 37:
                case 36:
                    return 12;
                case 35:
                case 34:
                case 33:
                    return 11;
                case 32:
                case 31:
                case 30:
                    return 10;
                case 29:
                case 28:
                case 27:
                    return 9;
                case 26:
                case 25:
                case 24:
                    return 8;
                case 23:
                case 22:
                case 21:
                    return 7;
                case 20:
                case 19:
                case 18:
                    return 6;
                case 17:
                case 16:
                case 15:
                    return 5;
                case 14:
                case 13:
                case 12:
                    return 4;
                default:
                    return 3;
            }
        }

        private int ContinentBonus(int iplayer)
        {
            var acc = 0;
            foreach (KeyValuePair<string, List<string>> kvp in Board.Continents)
            {
                if (kvp.Value.TrueForAll(x => HasTerritory(iplayer, x)))
                {
                    acc += continentvalues[kvp.Key];
                }
            }
            return acc;
        }

        public void AdvanceTurn()
        {
            Turn++;
            if (Turn >= Players.Count)
            {
                Turn = 0;
            }
            Players[Turn].XTroops = XTroops(Turn);
            if (Players[Turn].Cards.Count > 4)
            {
                if (TradeIn())
                {
                    Players[Turn].XTroops += TCardValue;
                    AdvanceBonus();
                }
            }
            Players[Turn].CanAttack = false;
            Players[Turn].CanFortify = false;
            Players[Turn].CanPlace = true;
        }

        public int XTroops(int iplayer) =>
            Bonus(XTerritories(iplayer)) + ContinentBonus(iplayer);
        
        public bool TradeIn(int iplayer)
        {
            var x = Players[iplayer].Cards.Where(t => t == "X").ToList().Count;
            var y = Players[iplayer].Cards.Where(t => t == "Y").ToList().Count;
            var z = Players[iplayer].Cards.Where(t => t == "Z").ToList().Count;
            var unique = Players[iplayer].Cards.Distinct().ToList().Count;

            if (x > 2)
            {
                for (int i = 0; i < 3; i++)
                {
                    Players[iplayer].Cards.Remove("X");
                    TCards.Add("X"); // Put it back in the deck
                }
                return true;
            }
            else if (y > 2)
            {
                for (int i = 0; i < 3; i++)
                {
                    Players[iplayer].Cards.Remove("Y");
                    TCards.Add("Y");
                }
                return true;
            }
            else if (z > 2)
            {
                for (int i = 0; i < 3; i++)
                {
                    Players[iplayer].Cards.Remove("Z");
                    TCards.Add("Z");
                }
                return true;
            }
            else if (unique > 2)
            {
                Players[iplayer].Cards.Remove("X");
                TCards.Add("X");
                Players[iplayer].Cards.Remove("Y");
                TCards.Add("Y");
                Players[iplayer].Cards.Remove("Z");
                TCards.Add("Z");
                return true;
            }
            else return false;
        }

        public bool TradeIn()
        {
            return TradeIn(Turn);
        }

        public void AdvanceBonus()
        {
            switch(TCardValue)
            {
                case 0:
                    TCardValue = 4;
                    break;
                case 4:
                    TCardValue = 6;
                    break;
                case 6:
                    TCardValue = 10;
                    break;
                default:
                    TCardValue += 5;
                    break;
            }
        }

        // Have to check eligibility before calling these

        public int PlaceTroops(int iplayer, int xtroops, string terr)
        {
            if (xtroops > Players[iplayer].XTroops || !Board.Territories.ContainsKey(terr))
                return Players[iplayer].XTroops;
            else
            {
                if (Board.Territories[terr].Id == Players[iplayer].Id)
                {
                    Board.Territories[terr].Troops += xtroops;
                    Players[iplayer].XTroops -= xtroops;
                    return Players[iplayer].XTroops;
                }
                else return Players[iplayer].XTroops;
            }
        }

        public bool Fortify(int iplayer, List<string> path, int xtroops)
        {
            if (path.TrueForAll(terr => Board.Territories[terr].Id == Players[iplayer].Id))
            {
                var okay = true;
                for (int i = 1; i < path.Count; i++)
                {
                    if (!Board.QAdjacent(path[i], path[i - 1]))
                        okay = false;
                }

                if (okay)
                {
                    if (Board.Territories[path[0]].Troops > xtroops)
                    {
                        Board.Territories[path[0]].Troops -= xtroops;
                        Board.Territories[path.Last()].Troops += xtroops;
                        return true;
                    }
                    else return false;
                }
                else return false;
            }
            else return false;
        }

        // Check adjacency before calling.  Can't check here cause 'false' is interpreted as a loss.
        public bool Attack(string from, string target)
        {
            while (Board.Territories[from].Troops > 1 && Board.Territories[target].Troops > 0)
            {
                var xfrom = (Board.Territories[from].Troops > 3) ? 3 : Board.Territories[from].Troops - 1;
                var xtarget = (Board.Territories[target].Troops > 1) ? 2 : Board.Territories[target].Troops;
                (int aloss, int dloss) = Dice.Losses(xfrom, xtarget);
                Board.Territories[from].Troops -= aloss; Board.Territories[target].Troops -= dloss;
            }

            if (Board.Territories[target].Troops <= 0)
            {
                // Transfer ownership
                Board.Territories[target].Id = Board.Territories[from].Id;
                Players[Turn].LastFrom = from;
                return true;
            }
            else return false;
        }

        public string Draw(int iplayer)
        {
            var card = TCards.PopRandomElement(); // Removes element automatically
            Players[iplayer].Cards.Add(card);
            //TCards.Remove(card);
            return card;
        }
    }
}