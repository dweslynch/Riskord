using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Newtonsoft.Json;

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
        public ulong Id { get; set; }
        public int Troops { get; set; }

        public ControlRecord()
        {
            Id = 0L;
            Troops = 0;
        }
    }

    public static class MapExtensions
    {
        public static void ToGraphicalRepresentation(this Board board, string namenoex)
        {
            var jsoncoords = File.ReadAllText("coords.pdo");
            var coords = JsonConvert.DeserializeObject<Dictionary<string, (float, float)>>(jsoncoords);

            var img = Image.FromFile("mapblank.png");
            var graphics = Graphics.FromImage(img);
            var font = new Font("Arial", 80, FontStyle.Bold);

            var brushes = new List<SolidBrush>
            {
                new SolidBrush(Color.Black),
                new SolidBrush(Color.Blue),
                new SolidBrush(Color.Orange),
                new SolidBrush(Color.Green),
                new SolidBrush(Color.Red),
                new SolidBrush(Color.Teal)
            };

            var players = new List<ulong>();

            // Pass one
            foreach (KeyValuePair<string, ControlRecord> kvp in board.Territories)
            {
                if (!players.Contains(kvp.Value.Id))
                    players.Add(kvp.Value.Id);
            }

            // Pass two
            foreach (KeyValuePair<string, ControlRecord> kvp in board.Territories)
            {
                var iplayer = players.IndexOf(kvp.Value.Id);
                var brush = brushes[iplayer];
                var txt = kvp.Value.Troops.ToString();
                (float x, float y) = coords[kvp.Key];
                var point = new PointF(x, y);

                graphics.DrawString(txt, font, brush, point);
            }

            var basename = "{0}.graph.png";
            var filename = String.Format(basename, namenoex);

            img.Save(filename);
        }
    }
}
