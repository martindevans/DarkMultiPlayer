using System;
using System.Collections.Generic;
using System.IO;
using DarkMultiPlayerCommon;
using MessageStream;

namespace DarkMultiPlayerServer
{
    public class GroupSystem
    {
        //A list of all the groups a player belongs to
        private Dictionary<string, List<string>> playerGroups = new Dictionary<string, List<string>>();
        //Group owners
        private Dictionary<string, string> groupOwners = new Dictionary<string, string>();
        //Group passwords (SHA256Sums)
        private Dictionary<string, string> groupPasswords = new Dictionary<string, string>();
        //Directories
        private string groupDirectory = Path.Combine(Path.Combine(Server.universeDirectory, "Groups"), "Groups");
        private string playerDirectory = Path.Combine(Path.Combine(Server.universeDirectory, "Groups"), "Players");

        public GroupSystem()
        {
            LoadGroups();
            LoadPlayers();
        }

        private void LoadGroups()
        {
            string[] groupFiles = Directory.GetFiles(groupDirectory, "*", SearchOption.TopDirectoryOnly);
            foreach (string groupFile in groupFiles)
            {
                string groupName = Path.GetFileNameWithoutExtension(groupFile);
                using (StreamReader sr = new StreamReader(groupFile))
                {
                    try
                    {
                        //First line is the owner
                        string groupOwner = sr.ReadLine();
                        if (groupOwner == null)
                        {
                            DarkLog.Error("Error loading group " + groupName + ", Group does not have an owner specified");
                        }
                        else
                        {
                            groupOwners.Add(groupName, groupOwner);
                            //Second line (if it exists) is the SHA256Sum of the password
                            string groupPassword = sr.ReadLine();
                            if (groupPassword != null)
                            {
                                groupPasswords[groupName] = groupPassword;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        DarkLog.Error("Error loading group " + groupName + ", Exception: " + e);
                    }
                }
            }
        }

        private void SaveGroup(string groupName)
        {
            if (!groupOwners.ContainsKey(groupName))
            {
                DarkLog.Debug("Cannot save group '" + groupName + "', group does not have an owner");
                return;
            }
            string groupFile = Path.Combine(groupDirectory, groupName + ".txt");
            using (StreamWriter sw = new StreamWriter(groupFile, false))
            {
                sw.WriteLine(groupOwners[groupName]);
                if (groupPasswords.ContainsKey(groupName))
                {
                    sw.WriteLine(groupPasswords[groupName]);
                }
            }
        }

        private void LoadPlayers()
        {
            string[] playerFiles = Directory.GetFiles(playerDirectory, "*", SearchOption.TopDirectoryOnly);
            foreach (string playerFile in playerFiles)
            {
                //I don't believe we can do the search above - Linux/mac are case sensitive.
                //Skip non .txt files
                if (Path.GetExtension(playerFile).ToLower() != ".txt")
                {
                    continue;
                }
                string playerName = Path.GetFileNameWithoutExtension(playerFile);
                playerGroups.Add(playerName, new List<string>());
                try
                {
                    bool nonExistentGroupDetected = false;
                    using (StreamReader sr = new StreamReader(playerFile))
                    {
                        string groupName;
                        while ((groupName = sr.ReadLine()) != null)
                        {
                            if (groupOwners.ContainsKey(groupName))
                            {
                                playerGroups[playerName].Add(groupName);
                            }
                            else {
                                nonExistentGroupDetected = true;
                            }
                        }
                    }
                    //Save the player file if a missing group was detected
                    if (nonExistentGroupDetected)
                    {
                        SavePlayer(playerName);
                    }
                }
                catch (Exception e)
                {
                    DarkLog.Error("Error reading group file for '" + playerName + "', Exception: " + e);
                }
            }
        }

        private void SavePlayer(string playerName)
        {
            string playerGroupsFile = Path.Combine(playerDirectory, playerName + ".txt");
            //Remove player entry if the player doesn't belong to any groups
            if (playerGroups.ContainsKey(playerName))
            {
                if (playerGroups[playerName].Count == 0)
                {
                    playerGroups.Remove(playerName);
                }
            }
            //Delete the player group file if the player does not belong to any groups
            if (!playerGroups.ContainsKey(playerName))
            {
                if (File.Exists(playerGroupsFile))
                {
                    DarkLog.Debug("Removing group file for '" + playerName + "', no longer belongs to any groups");
                    File.Delete(playerGroupsFile);
                }
                return;
            }
            string groupFile = Path.Combine(playerDirectory, playerName + ".txt");
            //Overwrite the old file
            using (StreamWriter sw = new StreamWriter(groupFile, false))
            {
                foreach (string groupName in playerGroups[playerName])
                {
                    sw.WriteLine(groupName);
                }
            }
            DarkLog.Debug("Saved groups for '" + playerName + "'");
        }

        public void AddGroup(string groupName, string groupOwner)
        {
            if (!groupOwners.ContainsKey(groupName))
            {
                groupOwners[groupName] = groupOwner;
                SaveGroup(groupName);
            }
            else
            {
                DarkLog.Debug("Group '" + groupName + "' already exists");
            }
        }

        public void RemoveGroup(string groupName)
        {
            if (!groupOwners.ContainsKey(groupName))
            {
                DarkLog.Debug("Unable to remove group '" + groupName + "', group does not exist");
                return;
            }
            //Remove players from group
            List<string> modifyList = new List<string>();
            foreach (KeyValuePair<string, List<string>> playerGroup in playerGroups)
            {
                if (playerGroup.Value.Contains(groupName))
                {
                    DarkLog.Debug("Removed '" + playerGroup.Key + "' from the '" + groupName + "' group");
                    playerGroup.Value.Remove(groupName);
                    modifyList.Add(playerGroup.Key);
                }
            }
            foreach (string modifyPlayer in modifyList)
            {
                SavePlayer(modifyPlayer);
            }
            //Remove group owner
            if (groupOwners.ContainsKey(groupName))
            {
                groupOwners.Remove(groupName);
            }
            //Remove group password
            if (groupOwners.ContainsKey(groupName))
            {
                groupOwners.Remove(groupName);
            }
            string groupFile = Path.Combine(groupDirectory, groupName + ".txt");
            if (File.Exists(groupFile))
            {
                File.Delete(groupFile);
            }
            DarkLog.Debug("Group '" + groupName + "' deleted");
        }

        public void SetGroupPassword(string groupName, string groupPasswordSHA256Sum)
        {
            if (!groupOwners.ContainsKey(groupName))
            {
                DarkLog.Debug("Unable to set password for '" + groupName + "', group does not exist");
                return;
            }
            if (groupPasswordSHA256Sum != "")
            {
                groupPasswords[groupName] = groupPasswordSHA256Sum;
                DarkLog.Debug("Set password for '" + groupName + "'");
            }
            else
            {
                groupPasswords.Remove(groupName);
                DarkLog.Debug("Unset password for '" + groupName + "'");
            }
            SaveGroup(groupName);
        }

        public void AddPlayerToGroup(string playerName, string groupName)
        {
            if (!groupOwners.ContainsKey(groupName))
            {
                DarkLog.Debug("Unable to add '" + playerName + "' to '" + groupName + "', group does not exist"); 
                return;
            }
            if (!playerGroups.ContainsKey(playerName))
            {
                playerGroups.Add(playerName, new List<string>());
            }
            if (playerGroups[playerName].Contains(groupName))
            {
                DarkLog.Debug("Player '" + playerName + "' is already a member of the '" + groupName + "' group");
                return;
            }
            playerGroups[playerName].Add(groupName);
            DarkLog.Debug("Added '" + playerName + "' to '" + groupName + "'");
            SavePlayer(playerName);
        }

        public void RemovePlayerFromGroup(string playerName, string groupName)
        {
            if (!groupOwners.ContainsKey(groupName))
            {
                DarkLog.Debug("Unable to remove '" + playerName + "' from '" + groupName + "', group does not exist");
                return;
            }
            if (playerGroups.ContainsKey(playerName))
            {
                DarkLog.Debug("Player '" + playerName + "' is not a member of the '" + groupName + "' group");
                return;
            }
            if (!playerGroups[playerName].Contains(groupName))
            {
                DarkLog.Debug("Player '" + playerName + "' is not a member of the '" + groupName + "' group");
                return;
            }
            playerGroups[playerName].Remove(groupName);
            DarkLog.Debug("Removed '" + playerName + "' from the '" + groupName + "' group");
            SavePlayer(playerName);
        }

        public void HandleGroupMessage(ClientObject client, byte[] messageData)
        {
            using (MessageReader mr = new MessageReader(messageData, false))
            {
                GroupMessageType messageType = (GroupMessageType)mr.Read<int>();
                switch (messageType)
                {
                    case GroupMessageType.JOIN:
                        {
                            string groupName = mr.Read<string>();
                            if (groupOwners.ContainsKey(groupName))
                            {
                                if (groupPasswords.ContainsKey(groupName))
                                {
                                    string groupPassword = mr.Read<string>();
                                }
                            }
                        }
                        break;
                    case GroupMessageType.LEAVE:
                        break;
                    case GroupMessageType.ADD_PLAYER:
                        break;
                    case GroupMessageType.REMOVE_PLAYER:
                        break;
                }
            }
        }
    }
}

