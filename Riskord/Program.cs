using System;
using System.Collections.Generic;
using System.Linq;
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
        const string IDFILE = "ids.pdo";

        Random randy = new Random();

        private string Token
        {
            get => File.ReadAllText(TOKFILE);
        }

        public DiscordSocketClient Client { get; set; } = new DiscordSocketClient();

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

        // Gets snapshot of ids
        public static Dictionary<ulong, string> GrabIds()
        {
            var contents = File.ReadAllText(IDFILE);
            return JsonConvert.DeserializeObject<Dictionary<ulong, string>>(contents);
        }

        public static Task Log(LogMessage msg)
        {
            Console.WriteLine("Logging : " + msg);
            return Task.CompletedTask;
        }

        public async Task MessageReceived(SocketMessage msg)
        {
            var author = msg.Author.Username;
            var uid = msg.Author.Id;
            var text = msg.Content;
            var qadmin = (msg.Author is SocketGuildUser) ? (msg.Author as SocketGuildUser).GuildPermissions.Administrator : false;
            var channelid = msg.Channel.Id.ToString();
            var buildfile = String.Format("{0}.builder.pdo", channelid);
            var gamefile = String.Format("{0}.game.pdo", channelid);
            var mapfile = String.Format("{0}.map.pdo", channelid);

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
                    if (File.Exists(gamefile))
                    {
                        var gamecontents = File.ReadAllText(gamefile);
                        var game = JsonConvert.DeserializeObject<GameMaster>(gamecontents);
                        if (game.Players.Exists(p => p.Id == uid))
                        {
                            // Find the author
                            var player = game.Players.Where(p => p.Id == uid).ToList()[0];
                            xtroops = player.XTroops;
                            var response = String.Format("# of Troops for {0}:  {1}", author, xtroops);
                            await msg.Channel.SendMessageAsync(response);
                        }
                        else await msg.Channel.SendMessageAsync("Player " + author + " is not listed for this game");
                    }
                    else if (File.Exists(buildfile))
                    {
                        var buildcontents = File.ReadAllText(buildfile);
                        var builder = JsonConvert.DeserializeObject<GameBuilder>(buildcontents);
                        if (builder.Players.Exists(p => p.Id == uid))
                        {
                            xtroops = builder.XTroops(uid);
                            var response = String.Format("# of Troops for {0}:  {1}", author, xtroops);
                            await msg.Channel.SendMessageAsync(response);
                        }
                        else await msg.Channel.SendMessageAsync(author + " is not listed for this game");
                    }
                    else
                    {
                        await msg.Channel.SendMessageAsync("No game in progress");
                    }
                }

                else if (text.Contains(" qturn"))
                {
                    if (File.Exists(gamefile))
                    {
                        var gamecontents = File.ReadAllText(gamefile);
                        var game = JsonConvert.DeserializeObject<GameMaster>(gamecontents);
                        var response = String.Format("Current Player:  {0}", game.CurrentPlayer);
                        await msg.Channel.SendMessageAsync(response);
                    }
                    else if (File.Exists(buildfile))
                    {
                        var buildcontents = File.ReadAllText(buildfile);
                        var builder = JsonConvert.DeserializeObject<GameBuilder>(buildcontents);
                        var turn = builder.Players[builder.Turn].Id;
                        var name = Player.Lookup(turn);
                        var response = String.Format("Current Player:  {0}", name);
                        await msg.Channel.SendMessageAsync(response);
                    }
                    else
                    {
                        await msg.Channel.SendMessageAsync("No game in progress");
                    }
                }

                else if (text.Contains(" qphase"))
                {
                    if (File.Exists(gamefile))
                    {
                        var gamecontents = File.ReadAllText(gamefile);
                        var game = JsonConvert.DeserializeObject<GameMaster>(gamecontents);
                        var player = game.Players[game.Turn];
                        var phase = String.Empty;
                        if (player.CanPlace)
                            phase = "Place";
                        else if (player.CanAttack)
                            phase = "Attack";
                        else if (player.CanFortify)
                            phase = "Fortify";
                        else if (player.CanDraw)
                            phase = "Draw"; // Shouldn't happen yet
                        var response = String.Format("It's {0}'s turn to {1}", Player.Lookup(player.Id), phase);
                        await msg.Channel.SendMessageAsync(response);
                    }
                    else await msg.Channel.SendMessageAsync("No game in progress");
                }

                else if (text.Contains(" repr"))
                {
                    if (File.Exists(gamefile))
                    {
                        var gamecontents = File.ReadAllText(gamefile);
                        var game = JsonConvert.DeserializeObject<GameMaster>(gamecontents);
                        var acc = "Current Turn:  " + game.CurrentPlayer + Environment.NewLine;
                        var filename = channelid + ".graph.png";

                        game.Board.ToGraphicalRepresentation(channelid);

                        await msg.Channel.SendFileAsync(filename);
                        /*
                        acc += "```fs" + Environment.NewLine;
                        acc += String.Format("{0,-15}  {1,7}  {2}", "Territory", "Troops", "Player");
                        acc += Environment.NewLine;
                        foreach (KeyValuePair<string, ControlRecord> kvp in game.Board.Territories)
                        {
                            acc += String.Format("{0,-15}  {1,7}  {2}", kvp.Key, kvp.Value.Troops, "\"" + kvp.Value.PlayerName + "\"");
                            acc += Environment.NewLine;
                        }
                        acc += "```";
                        await msg.Channel.SendMessageAsync(acc);
                        */
                    }
                    else if (File.Exists(buildfile))
                    {
                        var buildcontents = File.ReadAllText(buildfile);
                        var builder = JsonConvert.DeserializeObject<GameBuilder>(buildcontents);
                        var currentplayerid = builder.Players[builder.Turn].Id;
                        var acc = "Current Turn:  " + Player.Lookup(currentplayerid) + Environment.NewLine;
                        acc += "```fs" + Environment.NewLine;
                        acc += String.Format("{0,-15}  {1,7}  {2}", "Territory", "Troops", "Player");
                        acc += Environment.NewLine;
                        foreach (KeyValuePair<string, ControlRecord> kvp in builder.Territories)
                        {
                            acc += String.Format("{0,-15}  {1,7}  {2}", kvp.Key, kvp.Value.Troops, "\"" + Player.Lookup(kvp.Value.Id) + "\"");
                            acc += Environment.NewLine;
                        }
                        foreach (var s in builder.Unclaimed)
                        {
                            acc += String.Format("{0,-15}  {1,7}  {2}", s, "_", "Unclaimed");
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
                    if (!File.Exists(filename))
                    {
                        var acc = text.AsLines().ToList();
                        acc.RemoveAt(0); // Ignore the `@riskord create map` line
                        var graph = Graph.FromAdjacencyLines(acc);
                        var jsongraph = graph.ToJson();
                        File.WriteAllText(filename, jsongraph);
                        await msg.Channel.SendMessageAsync("New map created!");
                    }
                    else await msg.Channel.SendMessageAsync("A map already exists for this channel...If you'd like to replace it, delete the old one first with `@Riskord delmap`");
                }

                else if (text.Contains(" delmap"))
                {
                    if (File.Exists(mapfile))
                        File.Delete(mapfile);
                    await msg.Channel.SendMessageAsync("Map reset");
                }

                else if (text.Contains(" delgame"))
                {
                    if (File.Exists(buildfile))
                        File.Delete(buildfile);
                    if (File.Exists(gamefile))
                        File.Delete(gamefile);
                    await msg.Channel.SendMessageAsync("Current game deleted");
                }

                else if (text.Contains(" resetall"))
                {
                    if (File.Exists(buildfile))
                        File.Delete(buildfile);
                    if (File.Exists(gamefile))
                        File.Delete(gamefile);
                    if (File.Exists(mapfile))
                        File.Delete(mapfile);
                    await msg.Channel.SendMessageAsync("Channel files reset");
                    // Add more as the game starts using more files
                }

                else if (text.Contains(" start game "))
                {
                    var snapshot = GrabIds();

                    // All users tagged in the message except ourselves
                    var socketusers = msg.MentionedUsers.Where(u => u.Id != Client.CurrentUser.Id).ToList();
                    var usrids = new List<ulong>();
                    foreach (var usr in socketusers)
                    {
                        if (snapshot.ContainsKey(usr.Id))
                            snapshot[usr.Id] = usr.Username; // Update username
                        else snapshot.Add(usr.Id, usr.Username); // Add to dictionary
                    }
                    var jsonsnap = JsonConvert.SerializeObject(snapshot, Formatting.Indented);
                    File.WriteAllText(IDFILE, jsonsnap);

                    if (usrids.Count > 2)
                    {
                        var filename = (File.Exists(mapfile)) ? mapfile : "default.map.pdo";
                        var contents = File.ReadAllText(filename);
                        var graph = JsonConvert.DeserializeObject<Graph>(contents);
                        if ((!File.Exists(buildfile)) && (!File.Exists(gamefile)))
                        {
                            Dictionary<string, List<string>> continents;
                            if (filename == "default.map.pdo") // If we're using the default map, use continents
                            {
                                var _contents = File.ReadAllText("default.continents.pdo");
                                continents = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(_contents);
                            }
                            else continents = new Dictionary<string, List<string>>();
                            var builder = new GameBuilder(usrids, graph, continents);
                            var jsonbuilder = JsonConvert.SerializeObject(builder, Formatting.Indented);
                            File.WriteAllText(buildfile, jsonbuilder);

                            string acc = "New game started with turn order @" + snapshot[usrids[0]];
                            for (int i = 1; i < usrids.Count; i++)
                            {
                                acc += " -> @" + snapshot[usrids[i]];
                            }
                            await msg.Channel.SendMessageAsync(acc);
                            await msg.Channel.SendMessageAsync("Setup phase starts now");
                        }
                        else await msg.Channel.SendMessageAsync("There's already a game in progress - end it with `@Riskord end game` before starting a new one");
                    }
                    else await msg.Channel.SendMessageAsync("You need at least 3 players to start a game");
                }

                else if (text.Contains(" claim "))
                {
                    var rest = text.TextAfter("claim");
                    if (File.Exists(buildfile)) // Setup phase
                    {
                        var buildcontents = File.ReadAllText(buildfile);
                        var builder = JsonConvert.DeserializeObject<GameBuilder>(buildcontents);
                        if (builder.Unclaimed.Contains(rest)) // Territory is unclaimed
                        {
                            if (builder.Players[builder.Turn].Id == uid) // It's your turn
                            {
                                builder.Claim(uid, rest);
                                await msg.Channel.SendMessageAsync(author + " has claimed " + rest);
                                if (builder.Unclaimed.Count == 0)
                                {
                                    await msg.Channel.SendMessageAsync("All territories have been claimed.  You can now place troops");
                                }
                                var jsonbuilder = JsonConvert.SerializeObject(builder, Formatting.Indented);
                                File.WriteAllText(buildfile, jsonbuilder);
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
                    if (File.Exists(buildfile)) // Setup Phase
                    {
                        var buildcontents = File.ReadAllText(buildfile);
                        var builder = JsonConvert.DeserializeObject<GameBuilder>(buildcontents);
                        if (builder.Unclaimed.Count == 0) // All territories claimed
                        {
                            var parts = rest.Split(' ');
                            if (parts.Length == 2 && Int32.TryParse(parts[1], out int xtroops)) // Correct format
                            {
                                if (builder.Territories.ContainsKey(parts[0])) // Territory exists
                                {
                                    if (builder.Territories[parts[0]].Id == uid) // You own it
                                    {
                                        if (builder.Players.Exists(p => p.Id == uid)) // Does the player exist?  Shouldn't be necessary but whatever
                                        {
                                            // Get the first (only) element from a list of players whose ids match author's id
                                            var player = builder.Players.Where(p => p.Id == uid).ToList()[0];
                                            if (player.XTroops >= xtroops) // Do they have enough troops?
                                            {
                                                player.XTroops -= xtroops;
                                                builder.Territories[parts[0]].Troops += xtroops;
                                                var response = String.Format("{0} has placed {1} troops on {2}, with {3} remaining", author, xtroops, parts[0], player.XTroops);
                                                await msg.Channel.SendMessageAsync(response);

                                                var jsonbuilder = JsonConvert.SerializeObject(builder, Formatting.Indented);
                                                File.WriteAllText(buildfile, jsonbuilder);

                                                var acc = false; // Does anyone have troops left?
                                                foreach (Player p in builder.Players)
                                                    if (p.XTroops > 0)
                                                        acc = true;
                                                if (!acc) // If not, trash the GameBuilder and start the game!
                                                {
                                                    var game = builder.Finalize();
                                                    builder = null;
                                                    File.Delete(buildfile);
                                                    game.Turn = 0;
                                                    game.Players[0].XTroops = game.XTroops(0);
                                                    game.Players[0].CanPlace = true;
                                                    var jsongame = JsonConvert.SerializeObject(game, Formatting.Indented);
                                                    File.WriteAllText(gamefile, jsongame);
                                                    await msg.Channel.SendMessageAsync("Setup phase has ended - it's " + game.CurrentPlayer + "'s turn");
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
                    else if (File.Exists(gamefile))
                    {
                        var gamecontents = File.ReadAllText(gamefile);
                        var game = JsonConvert.DeserializeObject<GameMaster>(gamecontents);
                        var parts = rest.Split(' ');
                        if (parts.Length == 2 && Int32.TryParse(parts[1], out int xtroops))
                        {
                            if (game.Players[game.Turn].Id == uid) // It's your turn
                            {
                                if (game.Board.Territories.ContainsKey(parts[0])) // Territory exists
                                {
                                    if (game.Board.Territories[parts[0]].Id == uid) // You own it
                                    {
                                        if (game.Players.Exists(p => p.Id == uid)) // Does the player exist?  Shouldn't be necessary
                                        {
                                            var player = game.Players[game.Turn];
                                            if (player.XTroops >= xtroops) // Do they have enough troops?
                                            {
                                                if (player.CanPlace) // Can they place them?
                                                {
                                                    player.XTroops -= xtroops;
                                                    game.Board.Territories[parts[0]].Troops += xtroops;
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
                                                    var jsongame = JsonConvert.SerializeObject(game, Formatting.Indented);
                                                    File.WriteAllText(gamefile, jsongame);
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
                    if (File.Exists(gamefile))
                    {
                        var gamecontents = File.ReadAllText(gamefile);
                        var game = JsonConvert.DeserializeObject<GameMaster>(gamecontents);
                        if (game.Players[game.Turn].Id == uid) // Is it your turn?
                        {
                            var player = game.Players[game.Turn];
                            if (player.CanAttack) // Can you attack?
                            {
                                player.CanAttack = false;
                                player.CanFortify = true;
                                var jsongame = JsonConvert.SerializeObject(game, Formatting.Indented);
                                File.WriteAllText(gamefile, jsongame);
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
                    if (File.Exists(gamefile))
                    {
                        var gamecontents = File.ReadAllText(gamefile);
                        var game = JsonConvert.DeserializeObject<GameMaster>(gamecontents);
                        if (game.Players[game.Turn].Id == uid) // Is it your turn?
                        {
                            var player = game.Players[game.Turn];
                            if (player.CanFortify) // Can you fortify?
                            {
                                player.CanFortify = false;
                                game.AdvanceTurn();
                                var jsongame = JsonConvert.SerializeObject(game, Formatting.Indented);
                                File.WriteAllText(gamefile, jsongame);
                                var response = String.Format("{0} has forfeited their fortification{1}It's {2}'s turn", author, Environment.NewLine, game.CurrentPlayer);
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
                    if (File.Exists(gamefile))
                    {
                        var gamecontents = File.ReadAllText(gamefile);
                        var game = JsonConvert.DeserializeObject<GameMaster>(gamecontents);
                        if (game.Players[game.Turn].Id == uid) // It's your turn
                        {
                            if (game.Players[game.Turn].CanAttack) // Attack phase
                            {
                                if (game.Board.Territories[from].Id == uid) // You own origin
                                {
                                    if (game.Board.Territories[target].Id != uid) // You don't own target
                                    {
                                        if (game.Board.Territories[from].Troops > 1) // You have enough troops
                                        {
                                            if (game.Board.QAdjacent(from, target)) // The territories are adjacent
                                            {
                                                if (game.Attack(from, target))
                                                {
                                                    // Need to fix this block to be more efficient
                                                    foreach (Player p in game.Players)
                                                    {
                                                        var acc = false; // Do they own any territories?
                                                        foreach (KeyValuePair<string, ControlRecord> kvp in game.Board.Territories)
                                                        {
                                                            if (kvp.Value.Id == p.Id)
                                                                acc = true;
                                                        }
                                                        if (!acc)
                                                        {
                                                            game.Players.RemoveAll(_p => _p.Id == p.Id);
                                                            var response = String.Format("{0} has been eliminated!", Player.Lookup(p.Id));
                                                            await msg.Channel.SendMessageAsync(response);
                                                        }
                                                        if (game.Players.Count < 2)
                                                        {
                                                            var response = String.Format("{0} has eliminated all opponents!", author);
                                                            game = null;
                                                            File.Delete(gamefile);
                                                            await msg.Channel.SendMessageAsync(response);
                                                        }
                                                    }
                                                    // Nice!  Can continue attacking
                                                }
                                                else
                                                {
                                                    // Ouch, that sucks!  You're done for now
                                                    game.Players[game.Turn].CanAttack = false;
                                                    game.Players[game.Turn].CanFortify = true;
                                                    await msg.Channel.SendMessageAsync(author + " has been beaten!  They can no longer attack this turn");
                                                    await msg.Channel.SendMessageAsync("Time to fortify!");
                                                }
                                                var jsongame = JsonConvert.SerializeObject(game, Formatting.Indented);
                                                File.WriteAllText(gamefile, jsongame);
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
                    if (File.Exists(gamefile))
                    {
                        var gamecontents = File.ReadAllText(gamefile);
                        var game = JsonConvert.DeserializeObject<GameMaster>(gamecontents);
                        if (game.Players[game.Turn].Id == uid) // It's your turn
                        {
                            if (game.Players[game.Turn].CanFortify) // Fortification Phase
                            {
                                if (Int32.TryParse(parts[0], out int xtroops)) // Valid number
                                {
                                    var path = new List<string>();
                                    for (int i = 1; i < parts.Length; i++)
                                    {
                                        if (game.Board.Territories.ContainsKey(parts[i])) // Territory exists
                                        {
                                            path.Add(parts[i]);
                                        }
                                    }
                                    if (game.Fortify(game.Turn, path, xtroops))
                                    {
                                        game.Players[game.Turn].CanFortify = false;
                                        game.AdvanceTurn();
                                        var response = String.Format("{0} has moved {1} troops from {2} to {3}", author, xtroops, parts[1], parts[parts.Length - 1]);
                                        response += Environment.NewLine + String.Format("It's {0}'s turn", game.CurrentPlayer);
                                        await msg.Channel.SendMessageAsync(response);

                                        var jsongame = JsonConvert.SerializeObject(game, Formatting.Indented);
                                        File.WriteAllText(gamefile, jsongame);
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

                else if (text.Contains(" mv "))
                {
                    var rest = text.TextAfter("mv");
                    if (File.Exists(gamefile))
                    {
                        var gamecontents = File.ReadAllText(gamefile);
                        var game = JsonConvert.DeserializeObject<GameMaster>(gamecontents);
                        if (game.Players[game.Turn].Id == uid)
                        {
                            var targets = game.Board.Territories.Where(kvp => kvp.Value.Troops == 0).ToList();
                            if (targets.Count > 0)
                            {
                                if (Int32.TryParse(rest, out int xtroops))
                                {
                                    game.Board.Territories[game.Players[game.Turn].LastFrom].Troops -= xtroops;
                                    game.Board.Territories[targets[0].Key].Troops += xtroops;
                                    var response = String.Format("Moved {0} troops from {1} to {2}", xtroops, game.Players[game.Turn].LastFrom, targets[0].Key);
                                    await msg.Channel.SendMessageAsync(response);
                                    var jsongame = JsonConvert.SerializeObject(game, Formatting.Indented);
                                    File.WriteAllText(gamefile, jsongame);
                                }
                                else await msg.Channel.SendMessageAsync("Ya screwed up the formatting...try `@Riskord mv [amount]`");
                            }
                            else await msg.Channel.SendMessageAsync("You have to capture a territory before moving troops there");
                        }
                        else await msg.Channel.SendMessageAsync("You can only do this on your turn");
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