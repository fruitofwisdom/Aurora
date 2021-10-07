﻿using System;
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

            List<List<object>> roomsTableValues = Database.Instance.ReadTable("players", "name", Name);
            if (roomsTableValues.Count > 0)
            {
                string actualPassword = (string)roomsTableValues[0][2];
                string saltAsString = (string)roomsTableValues[0][3];
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
            List<List<object>> roomsTableValues = Database.Instance.ReadTable("players", "name", Name);
            if (roomsTableValues.Count > 0)
            {
                Password = (string)roomsTableValues[0][2];
                Salt = (string)roomsTableValues[0][3];
                IsAdmin = (long)roomsTableValues[0][4] != 0;
                CurrentRoomId = (long)roomsTableValues[0][5];
            }
        }

        public void Save()
        {
            List<string> columns = new List<string>() { "name", "password", "salt", "is_admin", "current_room_id" };
            List<object> values = new List<object>() { Name, Password, Salt, IsAdmin, CurrentRoomId };
            Database.Instance.WriteTable("players", columns, values);
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

            input = input.ToLower();
            input = LookupShorthand(input);

            switch (input)
            {
                case "exit":
                case "quit":
                    LocalConnection.SendMessage("Good-bye!\r\n");
                    LocalConnection.Quit(true);
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

        private bool TryExit(string input)
        {
            bool didExit = false;

            long? newRoomId = Game.RoomContainsExit(CurrentRoomId, input);
            if (newRoomId != null)
            {
                if (Game.RoomExists((long)newRoomId))
                {
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
