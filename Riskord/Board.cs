using System;
using System.Collections.Generic;

namespace Riskord
{
    public class Board
    {
        public Dictionary<string, List<string>> Adjacencies { get; set; }
        public Dictionary<string, ControlRecord> Territories { get; set; }
        public Dictionary<string, List<string>> Continents { get; set; }

        public Board()
        {
            Adjacencies = new Dictionary<string, List<string>>();
            Territories = new Dictionary<string, ControlRecord>();
            Continents = new Dictionary<string, List<string>>();
        }

        public Board(Dictionary<string, List<string>> adj, Dictionary<string, ControlRecord> territories)
        {
            Adjacencies = adj;
            Territories = territories;
            Continents = new Dictionary<string, List<string>>();
        }

        public Board(Dictionary<string, List<string>> adj, Dictionary<string, ControlRecord> territories, Dictionary<string, List<string>> cont)
        {
            Adjacencies = adj;
            Territories = territories;
            Continents = cont;
        }

        public bool QAdjacent(string x, string y) => Adjacencies[x].Contains(y);
    }

    public class ControlRecord
    {
        public string PlayerName { get; set; }
        public int Troops { get; set; }

        public ControlRecord()
        {
            PlayerName = String.Empty;
            Troops = 0;
        }
    }
}
