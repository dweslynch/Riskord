using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Riskord
{
    public class GameBuilder
    {
        public List<Player> Players { get; set; }
        public Dictionary<string, ControlRecord> Territories { get; set; }
        public List<string> Unclaimed { get; set; }
        public int Turn { get; set; } = 0;
        public Dictionary<string, List<string>> Adjacency { get; set; }

        public GameBuilder() { } // For serialization

        // Check for number of players before calling
        public GameBuilder(List<string> _players, Graph _graph)
        {
            Players = new List<Player>();
            Territories = new Dictionary<string, ControlRecord>();
            Unclaimed = new List<string>(_graph.AdjacencyLists.Keys);
            Turn = 0;
            Adjacency = _graph.AdjacencyLists;

            var xtroops = XTroopsFromXPlayers(_players.Count);
            foreach (string s in _players)
            {
                var player = new Player(s, xtroops);
                Players.Add(player);
            }
        }

        // Check that player exists and territory is unclaimed before calling
        // This function does test for this, but doesn't give feedback
        public void Claim(string playername, string territory)
        {
            if (Players.Exists(x => x.Name == playername))
            {
                if (Players.FindIndex(x => x.Name == playername) == Turn)
                {
                    if (Unclaimed.Contains(territory))
                    {
                        var cr = new ControlRecord();
                        cr.PlayerName = playername;
                        cr.Troops = 1;
                        // var _player = Players.Where(p => p.Name == playername).ToList()[0];
                        Players[Turn].XTroops -= 1;
                        Unclaimed.Remove(territory);
                        Territories.Add(territory, cr);
                        AdvanceTurn();
                    }
                }
            }
        }

        public void AddTroops(string playername, string territory, int xtroops)
        {
            if (Unclaimed.Count == 0)
            {
                if (Players.Exists(x => x.Name == playername))
                {
                    if (Territories.ContainsKey(territory) && Territories[territory].PlayerName == playername)
                    {
                        var _player = Players.Where(p => p.Name == playername).ToList()[0];
                        if (_player.XTroops >= xtroops)
                        {
                            Territories[territory].Troops += xtroops;
                            _player.XTroops -= xtroops;
                        }
                    }
                }
            }
        }

        public GameMaster Finalize() => new GameMaster(Adjacency, Players, Territories);

        public int XTroops(string playername)
        {
            if (Players.Exists(x => x.Name == playername))
            {
                var _player = Players.Where(p => p.Name == playername).ToList()[0];
                return _player.XTroops;
            }
            else return 0;
        }

        private void AdvanceTurn()
        {
            Turn++;
            if (Turn >= Players.Count)
            {
                Turn = 0;
            }
        }

        public static int XTroopsFromXPlayers(int xplayers)
        {
            switch(xplayers)
            {
                case 2: return 50;
                case 3: return 35;
                case 4: return 30;
                case 5: return 25;
                default: return 20;
            }
        }
    }
}
