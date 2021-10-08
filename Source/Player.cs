using System;
using System.Collections.Generic;

namespace Aurora
{
    internal class Player
    {
        private readonly Connection LocalConnection;
        public string Name = "Unknown Player";
        private string Password;
        private string Salt;
        public bool IsAdmin = false;
        public long CurrentRoomId = 0;

        public Player(Connection connection)
        {
            LocalConnection = connection;
        }

        public bool PasswordMatches(string password)
        {
            bool passwordMatches = false;

            List<List<object>> playersTableValues = Database.Instance.ReadTable("players", "name", Name);
            if (playersTableValues.Count > 0)
            {
                string actualPassword = (string)playersTableValues[0][2];
                string saltAsString = (string)playersTableValues[0][3];
                byte[] salt = Convert.FromBase64String(saltAsString);
                string hashedPassword = Connection.HashPassword(password, salt);
                passwordMatches = actualPassword == hashedPassword;
            }

            return passwordMatches;
        }

        public void Initialize(string password, string salt, long startingRoomId)
        {
            Password = password;
            Salt = salt;
            CurrentRoomId = startingRoomId;
            Save();
        }

        public void Load()
        {
            List<List<object>> playersTableValues = Database.Instance.ReadTable("players", "name", Name);
            if (playersTableValues.Count > 0)
            {
                Password = (string)playersTableValues[0][2];
                Salt = (string)playersTableValues[0][3];
                IsAdmin = (long)playersTableValues[0][4] != 0;
                CurrentRoomId = (long)playersTableValues[0][5];
            }
        }

        public void Save()
        {
            List<string> columns = new List<string>() { "name", "password", "salt", "is_admin", "current_room_id" };
            List<object> values = new List<object>() { Name, Password, Salt, IsAdmin, CurrentRoomId };
            Database.Instance.WriteTable("players", columns, values);
        }

        public void Message(string message)
        {
            LocalConnection.SendMessage(message);
        }

        private string LookupShorthand(string input)
        {
            string toReturn = input;

            Dictionary<string, string> shorthand = new Dictionary<string, string>()
            {
                { "n", "north" },
                { "ne", "northeast" },
                { "e", "east" },
                { "se", "southeast" },
                { "s", "south" },
                { "sw", "southwest" },
                { "w", "west" },
                { "nw", "northwest" }
            };
            if (shorthand.ContainsKey(input))
            {
                toReturn = shorthand[input];
            }

            return toReturn;
        }

        public void HandleInput(string input, out bool descriptionNeeded)
        {
            descriptionNeeded = false;

            string command = input.Split()[0].ToLower();
            command = LookupShorthand(command);
            switch (command)
            {
                case "exit":
                case "quit":
                    LocalConnection.SendMessage("Good-bye!\r\n");
                    LocalConnection.Disconnect(true);
                    break;
                case "help":
                case "?":
                    PrintHelp();
                    break;
                case "look":
                    descriptionNeeded = true;
                    break;
                case "who":
                    PrintWho();
                    break;
                case "say":
                    Say(input);
                    break;
                case "emote":
                    Emote(input);
                    break;
                default:
                    descriptionNeeded = TryExit(input);
                    break;
            }
        }

        private void PrintHelp()
        {
            LocalConnection.SendMessage("Type \"exit\" or \"quit\" to finish playing.\r\n");
            LocalConnection.SendMessage("     \"help\" or \"?\" to see these instructions.\r\n");
            LocalConnection.SendMessage("     \"look\" to look around at your surroundings.\r\n");
            LocalConnection.SendMessage("     \"north\", \"n\", \"south\", etc to move around the environment.\r\n");
            LocalConnection.SendMessage("     \"who\" to see who else is playing.\r\n");
            LocalConnection.SendMessage("     \"say\" to say something to everyone nearby.\r\n");
            LocalConnection.SendMessage("     \"emote\" to express yourself.\r\n");
        }

        private void PrintWho()
        {
            if (Game.Instance.Players.Count == 1)
            {
                LocalConnection.SendMessage("There is 1 player currently:\r\n");
            }
            else
            {
                LocalConnection.SendMessage("There are " + Game.Instance.Players.Count + " players currently:\r\n");
            }
            foreach (Player player in Game.Instance.Players)
            {
                LocalConnection.SendMessage("     " + player.Name + "\r\n");
            }
        }

        private void Say(string input)
        {
            string[] words = input.Split(' ');
            string speech = "";
            for (int i = 1; i < words.Length; ++i)
            {
                speech += words[i];
                if (i != words.Length - 1)
                {
                    speech += " ";
                }
            }
            speech = speech.Trim();
            LocalConnection.SendMessage("You say, \"" + speech + "\".\r\n");
            Game.Instance.ReportPlayerSaid(this, speech);
        }
        private void Emote(string input)
        {
            string[] words = input.Split(' ');
            string action = "";
            for (int i = 1; i < words.Length; ++i)
            {
                action += words[i];
                if (i != words.Length - 1)
                {
                    action += " ";
                }
            }
            action = action.Trim();
            LocalConnection.SendMessage("You " + action + ".\r\n");
            Game.Instance.ReportPlayerEmoted(this, action);
        }

        private bool TryExit(string input)
        {
            bool didExit = false;

            long? newRoomId = Game.RoomContainsExit(CurrentRoomId, input);
            if (newRoomId != null)
            {
                if (Game.RoomExists((long)newRoomId))
                {
                    Game.Instance.ReportPlayerMoved(this, CurrentRoomId, (long)newRoomId);
                    CurrentRoomId = (long)newRoomId;
                    Save();
                    didExit = true;
                }
                else
                {
                    ServerInfo.Instance.Report("[Player \"" + Name + "\"] Room with room_id " + newRoomId + " wasn't found!\n");
                }
            }

            if (!didExit)
            {
                LocalConnection.SendMessage("You can't \"" + input + "\" here!\r\n");
            }

            return didExit;
        }
    }
}
