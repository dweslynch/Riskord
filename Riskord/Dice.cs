using System;
using System.Collections.Generic;
using System.Linq;

namespace Riskord
{
    public static class Dice
    {
        private static Random randy = new Random();

        private static int RollDie() => randy.Next(0, 5) + 1;

        public static (int, int) Losses(int a, int d)
        {
            var ares = new List<int>();
            var dres = new List<int>();
            var aloss = 0;
            var dloss = 0;

            for (int i = 0; i < a; i++)
            {
                ares.Add(RollDie());
            }

            for (int i = 0; i < d; i++)
            {
                dres.Add(RollDie());
            }

            while (ares.Count > 0 && dres.Count > 0)
            {
                if (ares.Max() > dres.Max())
                    dloss++;
                else
                    aloss++;

                ares.Remove(ares.Max());
                dres.Remove(dres.Max());
            }

            return (aloss, dloss);
        }
    }
}
