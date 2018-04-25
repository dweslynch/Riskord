using System;
using System.Collections.Generic;

namespace Riskord
{
    public class Board
    {
        private Dictionary<string, List<string>> adjacencies;

        public Dictionary<string, ControlRecord> Territories { get; set; }

        public Board(Dictionary<string, List<string>> adj, Dictionary<string, ControlRecord> territories)
        {
            adjacencies = adj;
            Territories = territories;
        }

        public bool QAdjacent(string x, string y) => adjacencies[x].Contains(y);
    }

    public class ControlRecord
    {
        public string PlayerName { get; set; }
        public int Troops { get; set; }
    }
}
