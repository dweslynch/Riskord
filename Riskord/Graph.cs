using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Riskord
{
    public class Graph
    {
        public List<string> Nodes { get; set; }
        public Dictionary<string, List<string>> AdjacencyLists { get; set; }

        public Graph()
        {
            Nodes = new List<string>();
            AdjacencyLists = new Dictionary<string, List<string>>();
        }

        public Graph(List<string> nodes)
        {
            Nodes = new List<string>(nodes);
            AdjacencyLists = new Dictionary<string, List<string>>();
            foreach (string n in Nodes)
            {
                AdjacencyLists.Add(n, new List<string>());
            }
        }

        public void AddNode(string x)
        {
            if (!Nodes.Contains(x))
            {
                Nodes.Add(x);
            }
            if (!AdjacencyLists.ContainsKey(x))
            {
                AdjacencyLists.Add(x, new List<string>());
            }
        }

        public void AddEdge(string x, string y)
        {
            if (AdjacencyLists.ContainsKey(x))
            {
                if (AdjacencyLists.ContainsKey(y))
                {
                    if (!AdjacencyLists[x].Contains(y)) // Don't duplicate
                    {
                        AdjacencyLists[x].Add(y);
                        AdjacencyLists[y].Add(x);
                    }
                }
                else
                {
                    Nodes.Add(y);
                    AdjacencyLists.Add(y, new List<string> { x });
                    AdjacencyLists[x].Add(y);
                }
            }
        }

        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        public static Graph FromAdjacencyLines(IEnumerable<string> lines)
        {
            var graph = new Graph();
            foreach (var line in lines)
            {
                var ts = line.Split(' ');
                var first = ts[0];
                graph.AddNode(first);
                for (int i = 1; i < ts.Length; i++)
                {
                    graph.AddEdge(first, ts[i]);
                }
            }

            return graph;
        }
    }
}
