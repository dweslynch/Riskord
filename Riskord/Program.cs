using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using Discord;
using Discord.WebSocket;

namespace Riskord
{
    class Program
    {
        const string TOKFILE = "token.k";
        public static void Main(string[] args)
        {

            var riskbot = new RiskBot();
            riskbot.MainAsync().GetAwaiter().GetResult(); // Just let it run in background

            // I really hate doing this because it'll block main() entirely while the bot boots up in the background
            // There's alternative ways but I don't want to have to go back and forth between event handlers, yk?
            // We can maybe implement that in a future update
            while (!riskbot.IsReady)
            {
                // Wait for bot to be ready
            }

            string cmd;
            do
            {
                cmd = Console.ReadLine();
                // Interpret cmd
            } while (cmd != "exit");
        }
    }

    public class RiskBot
    {
        const string TOKFILE = "token.k";
        Random randy = new Random();

        private string Token
        {
            get => File.ReadAllText(TOKFILE);
        }

        public DiscordSocketClient Client { get; set; } = new DiscordSocketClient();
        public GameMaster Game { get; set; } = null;
        public GameBuilder Builder { get; set; } = null;

        public bool IsReady { get; private set; } = false;

        public static Boolean TaggedIn(SocketMessage msg, String username)
        {
            // var _tagged = false;
            foreach(var usr in msg.MentionedUsers)
            {
                if (usr.Username == username)
                    return true;
            }
            return false;
        }

        public static Task Log(LogMessage msg)
        {
            Console.WriteLine("Logging : " + msg);
            return Task.CompletedTask;
        }

        public async Task MessageReceived(SocketMessage msg)
        {
            var author = msg.Author.Username;
            var text = msg.Content;
            var qadmin = (msg.Author is SocketGuildUser) ? (msg.Author as SocketGuildUser).GuildPermissions.Administrator : false;

            if (TaggedIn(msg, Client.CurrentUser.Username))
            {

                if (text.Contains(" say "))
                {
                    var response = text.TextAfter("say");
                    await msg.Channel.SendMessageAsync(response);
                }

                else if (text.Contains(" xtroops"))
                {
                    var xtroops = 0;
                    if (Game != null)
                    {
                        if (Game.Players.Exists(p => p.Name == author))
                        {
                            // Find the author
                            var player = Game.Players.Where(p => p.Name == author).ToList()[0];
                            xtroops = player.XTroops;
                            var response = String.Format("# of Troops for {0}:  {1}", author, xtroops);
                            await msg.Channel.SendMessageAsync(response);
                        }
                    }
                    else if (Builder != null)
                    {
                        if (Builder.Players.Exists(p => p.Name == author))
                        {
                            xtroops = Builder.XTroops(author);
                            var response = String.Format("# of Troops for {0}:  {1}", author, xtroops);
                            await msg.Channel.SendMessageAsync(response);
                        }
                    }
                    else
                    {
                        await msg.Channel.SendMessageAsync("No game in progress");
                    }
                }

                else if (text.Contains(" qturn"))
                {
                    if (Game != null)
                    {
                        var response = String.Format("Current Player:  {0}", Game.CurrentPlayer);
                        await msg.Channel.SendMessageAsync(response);
                    }
                    else if (Builder != null)
                    {
                        var turn = Builder.Players[Builder.Turn];
                        var response = String.Format("Current Player:  {0}", turn);
                        await msg.Channel.SendMessageAsync(response);
                    }
                    else
                    {
                        await msg.Channel.SendMessageAsync("No game in progress");
                    }
                }

                else if (text.Contains(" qphase"))
                {
                    if (Game != null)
                    {
                        var player = Game.Players[Game.Turn];
                        var phase = String.Empty;
                        if (player.CanPlace)
                            phase = "Place";
                        else if (player.CanAttack)
                            phase = "Attack";
                        else if (player.CanFortify)
                            phase = "Fortify";
                        else if (player.CanDraw)
                            phase = "Draw"; // Shouldn't happen yet
                        var response = String.Format("It's {0}'s turn to {1}", player.Name, phase);
                        await msg.Channel.SendMessageAsync(response);
                    }
                    else await msg.Channel.SendMessageAsync("No game in progress");
                }

                else if (text.Contains(" repr"))
                {
                    if (Game != null)
                    {
                        var acc = "Current Turn:  " + Game.CurrentPlayer + Environment.NewLine;
                        acc += "```fs" + Environment.NewLine;
                        acc += String.Format("{0,-20} : {1,-30} : {2,7}", "Territory", "Player", "Troops");
                        acc += Environment.NewLine;
                        foreach (KeyValuePair<string, ControlRecord> kvp in Game.Board.Territories)
                        {
                            acc += String.Format("{0,-20} : \"{1,-30}\" : {2,7}", kvp.Key, kvp.Value.PlayerName, kvp.Value.Troops);
                            acc += Environment.NewLine;
                        }
                        acc += "```";
                        await msg.Channel.SendMessageAsync(acc);
                    }
                    else if (Builder != null)
                    {
                        var acc = "Current Turn:  " + Builder.Players[Builder.Turn] + Environment.NewLine;
                        acc += "```fs" + Environment.NewLine;
                        acc += String.Format("{0,-20} : {1,-30} : {2,7}", "Territory", "Player", "Troops");
                        acc += Environment.NewLine;
                        foreach (KeyValuePair<string, ControlRecord> kvp in Builder.Territories)
                        {
                            acc += String.Format("{0,-20} : \"{1,-30}\" : {2,7}", kvp.Key, kvp.Value.PlayerName, kvp.Value.Troops);
                            acc += Environment.NewLine;
                        }
                        foreach (var s in Builder.Unclaimed)
                        {
                            acc += String.Format("{0,-20} : {1,-30} : {2,7}", s, "Unclaimed", "_");
                            acc += Environment.NewLine;
                        }
                        acc += "```";
                        await msg.Channel.SendMessageAsync(acc);
                    }
                    else
                    {
                        await msg.Channel.SendMessageAsync("No game in progress");
                    }
                }

                else if (text.Contains(" create map"))
                {
                    var filename = msg.Channel.Id.ToString() + ".map.pdo";
                    if ((!File.Exists(filename)) || qadmin) // Only let admins modify existing maps
                    {
                        var acc = text.AsLines().ToList();
                        acc.RemoveAt(0); // Ignore the `@riskord create map` line
                        var graph = Graph.FromAdjacencyLines(acc);
                        var jsongraph = graph.ToJson();
                        File.WriteAllText(filename, jsongraph);
                        await msg.Channel.SendMessageAsync("New map created!");
                    }
                    else await msg.Channel.SendMessageAsync("Only server administrators can overwrite existing maps");
                }

                else if (text.Contains(" start game "))
                {
                    // All users tagged in the message except ourselves
                    var usrs = msg.MentionedUsers.Select(u => u.Username).Where(x => x != Client.CurrentUser.Username).ToList();
                    if (usrs.Count > 2) // Fix later
                    {
                        var filename = msg.Channel.Id.ToString() + ".map.pdo";
                        if (File.Exists(filename))
                        {
                            var contents = File.ReadAllText(filename);
                            var graph = JsonConvert.DeserializeObject<Graph>(contents);
                            if (Builder == null && Game == null) // Might remove these checks later
                            {
                                Builder = new GameBuilder(usrs, graph);
                                string acc = "New game started with turn order @" + usrs[0];
                                for (int i = 1; i < usrs.Count; i++)
                                {
                                    acc += " -> @" + usrs[i];
                                }
                                await msg.Channel.SendMessageAsync(acc);
                                await msg.Channel.SendMessageAsync("Setup phase starts now");
                            }
                        }
                        else await msg.Channel.SendMessageAsync("No map file found for this server.  Create one with `@Riskord create map`");
                    }
                    else await msg.Channel.SendMessageAsync("You need at least 3 players to start a game");
                }

                else if (text.Contains(" claim "))
                {
                    var rest = text.TextAfter("claim");
                    if (Builder != null)
                    {
                        if (Builder.Unclaimed.Contains(rest))
                        {
                            if (Builder.Players[Builder.Turn].Name == author)
                            {
                                Builder.Claim(author, rest);
                                await msg.Channel.SendMessageAsync(author + " has claimed " + rest);
                                if (Builder.Unclaimed.Count == 0)
                                {
                                    await msg.Channel.SendMessageAsync("All territories have been claimed.  You can now place troops");
                                }
                            }
                            else await msg.Channel.SendMessageAsync("You can't claim territories outside of your turn");
                        }
                        else await msg.Channel.SendMessageAsync("Territory " + rest + " has already been claimed or does not exist");
                    }
                    else await msg.Channel.SendMessageAsync("No setup phase in progress");
                }

                else if (text.Contains(" place "))
                {
                    var rest = text.TextAfter("place");
                    if (Builder != null) // Setup Phase
                    {
                        if (Builder.Unclaimed.Count == 0) // All territories claimed
                        {
                            var parts = rest.Split(' ');
                            if (parts.Length == 2 && Int32.TryParse(parts[1], out int xtroops)) // Correct format
                            {
                                if (Builder.Territories.ContainsKey(parts[0])) // Territory exists
                                {
                                    if (Builder.Territories[parts[0]].PlayerName == author) // You own it
                                    {
                                        if (Builder.Players.Exists(p => p.Name == author)) // Does the player exist?  Shouldn't be necessary but whatever
                                        {
                                            // Get the first (only) element from a list of players whose names match author's name
                                            var player = Builder.Players.Where(p => p.Name == author).ToList()[0];
                                            if (player.XTroops >= xtroops) // Do they have enough troops?
                                            {
                                                player.XTroops -= xtroops;
                                                Builder.Territories[parts[0]].Troops += xtroops;
                                                var response = String.Format("{0} has placed {1} troops on {2}, with {3} remaining", author, xtroops, parts[0], player.XTroops);
                                                await msg.Channel.SendMessageAsync(response);
                                                var acc = false; // Does anyone have troops left?
                                                foreach (Player p in Builder.Players)
                                                    if (p.XTroops > 0)
                                                        acc = true;
                                                if (!acc) // If not, trash the GameBuilder and start the game!
                                                {
                                                    Game = Builder.Finalize();
                                                    Builder = null;
                                                    Game.Turn = 0;
                                                    Game.Players[0].XTroops = Game.XTroops(0);
                                                    Game.Players[0].CanPlace = true;
                                                    // Game.Turn = -1; Game.AdvanceTurn();
                                                    await msg.Channel.SendMessageAsync("Setup phase has ended - it's " + author + "'s turn");
                                                }
                                            }
                                        } // No else here, this should never happen
                                    }
                                    else await msg.Channel.SendMessageAsync("You don't own territory " + parts[0]);
                                }
                                else await msg.Channel.SendMessageAsync("Territory " + parts[0] + " doesn't exist");
                            }
                            else await msg.Channel.SendMessageAsync("Ya screwed up the formatting...try `@Riskord place [territory] [amount]`");
                        }
                    }
                    else if (Game != null) // We're in-game
                    {
                        var parts = rest.Split(' ');
                        if (parts.Length == 2 && Int32.TryParse(parts[1], out int xtroops))
                        {
                            if (Game.CurrentPlayer == author) // It's your turn
                            {
                                if (Game.Board.Territories.ContainsKey(parts[0])) // Territory exists
                                {
                                    if (Game.Board.Territories[parts[0]].PlayerName == author) // You own it
                                    {
                                        if (Game.Players.Exists(p => p.Name == author)) // Does the player exist?  Shouldn't be necessary
                                        {
                                            var player = Game.Players[Game.Turn];
                                            if (player.XTroops >= xtroops)
                                            {
                                                if (player.CanPlace)
                                                {
                                                    player.XTroops -= xtroops;
                                                    Game.Board.Territories[parts[0]].Troops += xtroops;
                                                    if (player.XTroops <= 0)
                                                    {
                                                        player.CanPlace = false;
                                                        player.CanAttack = true;
                                                        var response = String.Format("{0} has placed the rest of their troops on {1}, time to attack", author, parts[0]);
                                                        await msg.Channel.SendMessageAsync(response);
                                                    }
                                                    else
                                                    {
                                                        var response = String.Format("{0} has placed {1} troops on {2}, with {3} remaining", author, xtroops, parts[0], player.XTroops);
                                                        await msg.Channel.SendMessageAsync(response);
                                                    }
                                                }
                                                else await msg.Channel.SendMessageAsync("You can't place troops out of turn");
                                            }
                                            else
                                            {
                                                var response = String.Format("You are trying to place {0} troops but only have {1}", xtroops, player.XTroops);
                                                await msg.Channel.SendMessageAsync(response);
                                            }
                                        } // No else here, this should never happen
                                    }
                                    else await msg.Channel.SendMessageAsync("You don't own territory " + parts[0]);
                                }
                                else await msg.Channel.SendMessageAsync("Territory " + parts[0] + " doesn't exist");
                            }
                            else await msg.Channel.SendMessageAsync("You can't place troops out of turn");
                        }
                        else await msg.Channel.SendMessageAsync("Ya screwed up the formatting...try `@Riskord place [terrritory] [amount]`");
                    }
                    else await msg.Channel.SendMessageAsync("No game in progress");
                }

                else if (text.Contains(" noattack"))
                {
                    if (Game != null)
                    {
                        if (Game.CurrentPlayer == author) // Is it your turn?
                        {
                            var player = Game.Players[Game.Turn];
                            if (player.CanAttack) // Can you attack?
                            {
                                player.CanAttack = false;
                                player.CanFortify = true;
                                var response = String.Format("{0} has forfeited their attack", author);
                                await msg.Channel.SendMessageAsync(response);
                            }
                            else await msg.Channel.SendMessageAsync("It isn't your turn to attack");
                        }
                        else await msg.Channel.SendMessageAsync("You can't attack out of turn");
                    }
                    else await msg.Channel.SendMessageAsync("No game in progress");
                }

                else if (text.Contains(" nofort"))
                {
                    if (Game != null)
                    {
                        if (Game.CurrentPlayer == author) // Is it your turn?
                        {
                            var player = Game.Players[Game.Turn];
                            if (player.CanFortify)
                            {
                                player.CanFortify = false;
                                Game.AdvanceTurn();
                                var response = String.Format("{0} has forfeited their fortification{1}It's {2}'s turn", author, Environment.NewLine, Game.CurrentPlayer);
                                await msg.Channel.SendMessageAsync(response);
                            }
                            else await msg.Channel.SendMessageAsync("It isn't your turn to fortify");
                        }
                        else await msg.Channel.SendMessageAsync("You can't fortify out of turn");
                    }
                    else await msg.Channel.SendMessageAsync("No game in progress");
                }

                else if (text.Contains(" attack "))
                {
                    var rest = text.TextAfter("attack");
                    var parts = rest.Split(' ');
                    (string from, string target) = (parts[0], parts[1]);
                    if (Game != null) // A game exists
                    {
                        if (Game.CurrentPlayer == author) // It's your turn
                        {
                            if (Game.Players[Game.Turn].CanAttack) // Attack phase
                            {
                                if (Game.Board.Territories[from].PlayerName == author) // You own origin
                                {
                                    if (Game.Board.Territories[target].PlayerName != author) // You don't own target
                                    {
                                        if (Game.Board.Territories[from].Troops > 1) // You have enough troops
                                        {
                                            if (Game.Board.QAdjacent(from, target)) // The territories are adjacent
                                            {
                                                if (Game.Attack(from, target))
                                                {
                                                    // Need to fix this block
                                                    foreach (Player p in Game.Players)
                                                    {
                                                        var acc = false;
                                                        foreach (KeyValuePair<string, ControlRecord> kvp in Game.Board.Territories)
                                                        {
                                                            if (kvp.Value.PlayerName == p.Name)
                                                                acc = true;
                                                        }
                                                        if (!acc)
                                                        {
                                                            Game.Players.RemoveAll(_p => _p.Name == p.Name);
                                                            var response = String.Format("{0} has been eliminated!", p.Name);
                                                            await msg.Channel.SendMessageAsync(response);
                                                        }
                                                        if (Game.Players.Count < 2)
                                                        {
                                                            var response = String.Format("{0} has eliminated all opponents!", author);
                                                            Game = null;
                                                            await msg.Channel.SendMessageAsync(response);
                                                        }
                                                    }
                                                    // Nice!  Can continue attacking
                                                }
                                                else
                                                {
                                                    // Ouch, that sucks!  You're done for now
                                                    Game.Players[Game.Turn].CanAttack = false;
                                                    Game.Players[Game.Turn].CanFortify = true;
                                                    await msg.Channel.SendMessageAsync(author + " has been beaten!  They can no longer attack this turn");
                                                    await msg.Channel.SendMessageAsync("Time to fortify!");
                                                }
                                            }
                                            else await msg.Channel.SendMessageAsync("You can't attack a non-adjacent territory");
                                        }
                                        else await msg.Channel.SendMessageAsync("You don't have enough troops in " + from + " this territory to attack");
                                    }
                                    else await msg.Channel.SendMessageAsync("You own " + target + " and can't attack it");
                                }
                                else await msg.Channel.SendMessageAsync("You don't own " + from + " and can't attack from it");
                            }
                            else await msg.Channel.SendMessageAsync("It isn't your turn to attack");
                        }
                        else await msg.Channel.SendMessageAsync("You can't attack out of turn");
                    }
                    else await msg.Channel.SendMessageAsync("No game in progress");
                }

                else if (text.Contains(" fortify "))
                {
                    var rest = text.TextAfter("fortify");
                    var parts = rest.Split(' ');
                    if (Game != null) // A game exists
                    {
                        if (Game.CurrentPlayer == author) // It's your turn
                        {
                            if (Game.Players[Game.Turn].CanFortify) // Fortification Phase
                            {
                                if (Int32.TryParse(parts[0], out int xtroops)) // Valid number
                                {
                                    var path = new List<string>();
                                    for (int i = 1; i < parts.Length; i++)
                                    {
                                        if (Game.Board.Territories.ContainsKey(parts[i])) // Territory exists
                                        {
                                            path.Add(parts[i]);
                                        }
                                    }
                                    if (Game.Fortify(Game.Turn, path, xtroops))
                                    {
                                        Game.Players[Game.Turn].CanFortify = false;
                                        Game.AdvanceTurn();
                                        var response = String.Format("{0} has moved {1} troops from {2} to {3}", author, xtroops, parts[1], parts[parts.Length - 1]);
                                        response += Environment.NewLine + String.Format("It's {0}'s turn", Game.CurrentPlayer);
                                        await msg.Channel.SendMessageAsync(response);
                                    }
                                    else await msg.Channel.SendMessageAsync("Fortification failed - make sure your entire path is adjacent");
                                }
                                else await msg.Channel.SendMessageAsync("Ya screwed up the formatting...try `@Riskord fortify [amount] [territory1] [territory2] [territory3 etc.]`");
                            }
                            else await msg.Channel.SendMessageAsync("It's not your turn to fortify");
                        }
                        else await msg.Channel.SendMessageAsync("You can't fortify out of turn");
                    }
                    else await msg.Channel.SendMessageAsync("No game in progress");
                }

                else if (text.Contains(" help"))
                {
                    if (File.Exists("help.txt"))
                    {
                        var contents = File.ReadAllText("help.txt");
                        await msg.Channel.SendMessageAsync(contents);
                    }
                    else await msg.Channel.SendMessageAsync("help.txt does not exist...yet");
                }

                else if (text.Contains(" changes"))
                {
                    if (File.Exists("changes.txt"))
                    {
                        var contents = File.ReadAllText("changes.txt");
                        await msg.Channel.SendMessageAsync(contents);
                    }
                    else await msg.Channel.SendMessageAsync("changes.txt does not exist...yet");
                }

                else if (text.Contains(" map example"))
                {
                    if (File.Exists("sample.txt"))
                    {
                        var contents = File.ReadAllText("sample.txt");
                        await msg.Channel.SendMessageAsync(contents);
                    }
                    else await msg.Channel.SendMessageAsync("sample.txt does not exist...yet");
                }

                else if (text.Contains(" instructions"))
                {
                    if (File.Exists("instructions.txt"))
                    {
                        var contents = File.ReadAllText("instructions.txt");
                        await msg.Channel.SendMessageAsync(contents);
                    }
                    else await msg.Channel.SendMessageAsync("instructions.txt does not exist...yet");
                }
            }
        }

        public async Task Ready()
        {
            IsReady = true;
        }

        public async Task MainAsync()
        {
            Client.Log += Log;
            Client.MessageReceived += MessageReceived;
            Client.Ready += Ready;

            await Client.LoginAsync(TokenType.Bot, Token);
            await Client.StartAsync();
        }
    }

    // Don't call any of these rapidly
    public static class Extensions
    {
        public static T RandomElement<T>(this List<T> lst)
        {
            Random randy = new Random();
            return lst[randy.Next(0, lst.Count)];
        }
        public static T RandomElement<T>(this T[] lst)
        {
            Random randy = new Random();
            return lst[randy.Next(0, lst.Length)];
        }
        public static Boolean OneIn(int i)
        {
            Random randy = new Random();
            return (randy.Next(0, i) == 1);
        }

        public static IEnumerable<string> AsLines(this string s)
        {
            string line;
            using (var sr = new StringReader(s))
            {
                while ((line = sr.ReadLine()) != null)
                {
                    yield return line;
                }
            }
        }

        // Untested
        public static String TextAfter(this string target, string after)
        {
            if (target.Contains(after))
            {
                return target.Substring(target.IndexOf(after) + after.Length + 1);
            }
            else return target;
        }
    }
}