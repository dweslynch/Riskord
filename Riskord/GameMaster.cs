﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Riskord
{
    public class GameMaster
    {
        private Random randy = new Random();

        public Board Board { get; set; }
        public List<Player> Players { get; set; }
        public int Turn { get; set; } = 0;

        public String CurrentPlayer
        {
            get => Players[Turn].Name;
        }

        public GameMaster() { }
        public GameMaster(Board b, List<Player> players)
        {
            this.Board = b;
            this.Players = players;
        }
        public GameMaster(Dictionary<string, List<string>> adj, List<Player> players, Dictionary<string, ControlRecord> territories)
        {
            this.Board = new Board(adj, territories);
            this.Players = players;
        }

        private int PlayerIndexFromName(string name) =>
            Players.FindIndex(prop => prop.Name == name);

        private int XTerritories(int iplayer) =>
            Board.Territories.Values.Where(t => t.PlayerName == Players[iplayer].Name).Count();

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

        public void AdvanceTurn()
        {
            Turn++;
            if (Turn >= Players.Count)
            {
                Turn = 0;
            }
            Players[Turn].XTroops = XTroops(Turn);
            Players[Turn].CanAttack = false;
            Players[Turn].CanFortify = false;
            Players[Turn].CanPlace = true;
        }

        public int XTroops(int iplayer) =>
            Bonus(XTerritories(iplayer));

        // Have to check eligibility before calling these

        public int PlaceTroops(int iplayer, int xtroops, string terr)
        {
            if (xtroops > Players[iplayer].XTroops || !Board.Territories.ContainsKey(terr))
                return Players[iplayer].XTroops;
            else
            {
                if (Board.Territories[terr].PlayerName == Players[iplayer].Name)
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
            if (path.TrueForAll(terr => Board.Territories[terr].PlayerName == Players[iplayer].Name))
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
                return true;
            else
                return false;

        }
    }
}