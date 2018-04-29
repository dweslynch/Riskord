using System;
using System.Collections.Generic;
using System.Linq;

namespace Riskord
{
    public class GameBuilder
    {
        public List<Player> Players { get; set; }
        public Dictionary<string, ControlRecord> Territories { get; set; }
        public List<string> Unclaimed { get; set; }
        public int Turn { get; set; }
        public Dictionary<string, List<string>> Adjacency { get; set; }
        public Dictionary<string, List<string>> Continents { get; set; }

        public GameBuilder()
        {
            Players = new List<Player>();
            Territories = new Dictionary<string, ControlRecord>();
            Unclaimed = new List<string>();
            Turn = 0;
            Adjacency = new Dictionary<string, List<string>>();
            Continents = new Dictionary<string, List<string>>();
        }

        // Check for number of players before calling
        public GameBuilder(List<ulong> _players, Graph _graph)
        {
            Players = new List<Player>();
            Territories = new Dictionary<string, ControlRecord>();
            Unclaimed = new List<string>(_graph.AdjacencyLists.Keys);
            Turn = 0;
            Adjacency = _graph.AdjacencyLists;
            Continents = new Dictionary<string, List<string>>();

            var xtroops = XTroopsFromXPlayers(_players.Count);
            foreach (ulong uid in _players)
            {
                var player = new Player(uid, xtroops);
                Players.Add(player);
            }
        }

        public GameBuilder(List<ulong> _players, Graph _graph, Dictionary<string, List<string>> _cont) : this(_players, _graph)
        {
            Continents = _cont;
        }

        // Check that player exists and territory is unclaimed before calling
        // This function does test for this, but doesn't give feedback
        public void Claim(ulong id, string territory)
        {
            if (Players.Exists(x => x.Id == id))
            {
                if (Players.FindIndex(x => x.Id == id) == Turn)
                {
                    if (Unclaimed.Contains(territory))
                    {
                        var cr = new ControlRecord();
                        cr.Id = id;
                        cr.Troops = 1;
                        Players[Turn].XTroops -= 1;
                        Unclaimed.Remove(territory);
                        Territories.Add(territory, cr);
                        AdvanceTurn();
                    }
                }
            }
        }

        public void AddTroops(ulong id, string territory, int xtroops)
        {
            if (Unclaimed.Count == 0)
            {
                if (Players.Exists(x => x.Id == id))
                {
                    if (Territories.ContainsKey(territory) && Territories[territory].Id == id)
                    {
                        var _player = Players.Where(p => p.Id == id).ToList()[0];
                        if (_player.XTroops >= xtroops)
                        {
                            Territories[territory].Troops += xtroops;
                            _player.XTroops -= xtroops;
                        }
                    }
                }
            }
        }

        public GameMaster Finalize() => new GameMaster(Adjacency, Players, Territories, Continents);

        public int XTroops(ulong id)
        {
            if (Players.Exists(x => x.Id == id))
            {
                var _player = Players.Where(p => p.Id == id).ToList()[0];
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
