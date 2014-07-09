using System;
using System.Collections.Generic;
using System.IO;

namespace DarkMultiPlayerServer
{
    public class GroupSystem
    {
        //groupName, group info
        private Dictionary<string, GroupObject> groupInfo = new Dictionary<string, GroupObject>();
        //playerName, groupName
        private Dictionary<string, string> playerGroup = new Dictionary<string, string>();
        private string groupDirectory;
        private string playerDirectory;
        private string playerTokenDirectory;

        public GroupSystem()
        {
            groupDirectory = Path.Combine(Server.universeDirectory, "Groups", "Groups");
            playerDirectory = Path.Combine(Server.universeDirectory, "Groups", "Players");
            playerTokenDirectory = Path.Combine(Server.universeDirectory, "Players");
            LoadGroups();
            LoadPlayers();
        }

        private void LoadGroups()
        {
            string[] groupFiles = Directory.GetFiles(groupDirectory, "*.txt", SearchOption.TopDirectoryOnly);
            foreach (string groupFile in groupFiles)
            {
                string groupName = Path.GetFileNameWithoutExtension(groupFile);
                try
                {
                    using (StreamReader sr = new StreamReader(groupFile))
                    {
                        string ownerName = sr.ReadLine();
                        GroupPrivacy groupPrivacy = (GroupPrivacy)Enum.Parse(typeof(GroupPrivacy), sr.ReadLine());
                        GroupObject go = new GroupObject(ownerName, groupPrivacy);
                        groupInfo[groupName] = go;
                        string groupPassword = sr.ReadLine();
                        if (groupPassword != null)
                        {
                            go.groupPassword = groupPassword;
                        }
                    }
                }
                catch (Exception e)
                {
                    DarkLog.Error("Error loading group " + groupName + ", Exception: " + e);
                }
            }
            DarkLog.Debug("Groups loaded");
        }

        private void LoadPlayers()
        {
            string[] playerFiles = Directory.GetFiles(playerDirectory, "*.txt", SearchOption.TopDirectoryOnly);
            foreach (string playerFile in playerFiles)
            {
                string playerName = Path.GetFileNameWithoutExtension(playerFile);
                try
                {
                    using (StreamReader sr = new StreamReader(playerFile))
                    {
                        string groupName = sr.ReadLine();
                        if (groupName != null && groupInfo.ContainsKey(groupName))
                        {
                            playerGroup[playerName] = groupName;
                        }
                    }
                }
                catch (Exception e)
                {
                    DarkLog.Error("Error loading player " + playerName + ", Exception: " + e);
                }
            }
            DarkLog.Debug("Groups members loaded");
        }

        private void SaveGroup(string groupName)
        {
            string groupFile = Path.Combine(groupDirectory, groupName + ".txt");
            if (groupInfo.ContainsKey(groupName))
            {
                using (StreamWriter sw = new StreamWriter(groupFile))
                {
                    sw.WriteLine(groupInfo[groupName].groupOwner);
                    sw.WriteLine(groupInfo[groupName].groupPrivacy.ToString());
                    if (groupInfo[groupName].groupPassword != null)
                    {
                        sw.WriteLine(groupInfo[groupName].groupPassword);
                    }
                }
                DarkLog.Debug("Group " + groupName + " saved");
            }
            else
            {
                if (File.Exists(groupFile))
                {
                    File.Delete(groupFile);
                    DarkLog.Debug("Group " + groupName + " deleted");
                }
            }
        }

        private void SavePlayer(string playerName)
        {
            string playerFile = Path.Combine(playerDirectory, playerName + ".txt");
            if (playerGroup.ContainsKey(playerName))
            {
                string groupName = playerGroup[playerName];
                using (StreamWriter sw = new StreamWriter(playerFile))
                {
                    sw.WriteLine(groupName);
                }
                DarkLog.Debug("Player " + playerName + " saved as member of " + groupName);
            }
            else
            {
                if (File.Exists(playerFile))
                {
                    File.Delete(playerFile);
                    DarkLog.Debug("Player " + playerName + " removed from group");
                }
            }
        }

        /// <summary>
        /// Creates the group. Returns true if successful
        /// </summary>
        public bool CreateGroup(string groupName, string ownerName, GroupPrivacy groupPrivacy)
        {
            if (groupInfo.ContainsKey(groupName))
            {
                DarkLog.Debug("Cannot create group " + groupName + ", Group already exists");
                return false;
            }
            if (playerGroup.ContainsKey(ownerName))
            {
                DarkLog.Debug("Cannot create group " + groupName + ", " + ownerName + " already belongs to a group");
                return false;
            }
            if (!PlayerExists(ownerName))
            {
                DarkLog.Debug("Cannot create group " + groupName + ", " + ownerName + " does not exist");
                return false;
            }
            GroupObject go = new GroupObject(ownerName, groupPrivacy);
            groupInfo[groupName] = go;
            playerGroup[ownerName] = groupName;
            SaveGroup(groupName);
            SavePlayer(ownerName);
            return true;
        }

        /// <summary>
        /// Make a player join the group. Returns true if the group was joined.
        /// </summary>
        public bool JoinGroup(string groupName, string playerName)
        {
            if (!groupInfo.ContainsKey(groupName))
            {
                DarkLog.Debug("Cannot join group " + groupName + ", Group does not exist");
                return false;
            }
            if (playerGroup.ContainsKey(playerName))
            {
                DarkLog.Debug("Cannot join group " + groupName + ", " + playerName + " already belongs to a group");
                return false;
            }
            if (!PlayerExists(playerName))
            {
                DarkLog.Debug("Cannot join group " + groupName + ", " + playerName + " doesn't exist");
                return false;
            }
            playerGroup[playerName] = groupName;
            SavePlayer(playerName);
            return true;
        }

        /// <summary>
        /// Make a player leave the group. Returns true if the group was left.
        /// </summary>
        public bool LeaveGroup(string playerName)
        {
            if (!PlayerExists(playerName))
            {
                DarkLog.Debug("Cannot leave group, " + playerName + " doesn't exist");
                return false;
            }
            if (!playerGroup.ContainsKey(playerName))
            {
                DarkLog.Debug("Cannot leave group, " + playerName + " does not belong to any a group");
                return false;
            }
            if (groupInfo.ContainsKey(playerGroup[playerName]))
            {
                if (groupInfo[playerGroup[playerName]].groupOwner == playerName)
                {
                    DarkLog.Debug("Cannot leave group, " + playerName + " is the owner");
                    return false;
                }
            }
            playerGroup.Remove(playerName);
            SavePlayer(playerName);
            return true;
        }

        /// <summary>
        /// Sets the group password. Set SHAPassword to null to remove the password. Returns true on success
        /// </summary>
        public bool SetGroupPassword(string groupName, string SHAPassword)
        {
            if (!groupInfo.ContainsKey(groupName))
            {
                DarkLog.Debug("Cannot set group password, " + groupName + " doesn't exist");
                return false;
            }
            groupInfo[groupName].groupPassword = SHAPassword;
            SaveGroup(groupName);
            return true;
        }

        /// <summary>
        /// Sets the group privacy. Set SHAPassword to null to remove the password. Returns true on success
        /// </summary>
        public bool SetGroupPrivacy(string groupName, GroupPrivacy groupPrivacy)
        {
            if (!groupInfo.ContainsKey(groupName))
            {
                DarkLog.Debug("Cannot set group privacy, " + groupName + " doesn't exist");
                return false;
            }
            if (groupPrivacy == GroupPrivacy.PUBLIC)
            {
                groupInfo[groupName].groupPassword = null;
            }
            groupInfo[groupName].groupPrivacy = groupPrivacy;
            SaveGroup(groupName);
            return true;
        }

        /// <summary>
        /// Returns a string array of group names
        /// </summary>
        public string[] GetGroups()
        {
            List<string> knownGroups = new List<string>();
            foreach (string groupName in groupInfo.Keys)
            {
                knownGroups.Add(groupName);
            }
            knownGroups.Sort();
            return knownGroups.ToArray();
        }

        /// <summary>
        /// Check if a group exists
        /// </summary>
        public bool GroupExists(string groupName)
        {
            return groupInfo.ContainsKey(groupName);
        }

        /// <summary>
        /// Check if a player is registered
        /// </summary>
        public bool PlayerExists(string playerName)
        {
            string playerFile = Path.Combine(playerTokenDirectory, playerName + ".txt");
            return File.Exists(playerFile);
        }

        /// <summary>
        /// Sets the group owner. If the group or player does not exist, returns false
        /// </summary>
        public bool SetGroupOwner(string groupName, string playerName)
        {
            if (!groupInfo.ContainsKey(groupName))
            {
                return false;
            }
            if (!PlayerExists(playerName))
            {
                return false;
            }
            groupInfo[groupName].groupOwner = playerName;
            SaveGroup(groupName);
            return true;
        }

        /// <summary>
        /// Returns the group that the player is in. If the player is not in the group, returns null.
        /// </summary>
        public string GetPlayerGroup(string playerName)
        {
            if (playerGroup.ContainsKey(playerName))
            {
                return playerGroup[playerName];
            }
            return null;
        }

        /// <summary>
        /// Returns the group owner. If the group does not exist, returns null
        /// </summary>
        public string GetGroupOwner(string groupName)
        {
            if (!groupInfo.ContainsKey(groupName))
            {
                return null;
            }
            return groupInfo[groupName].groupOwner;
        }

        /// <summary>
        /// Checks the group password for a match. Returns true on success. Always returns false if the group password is not set.
        /// </summary>
        public bool CheckGroupPassword(string groupName, string groupPassword)
        {
            if (!groupInfo.ContainsKey(groupName))
            {
                return false;
            }
            if (groupInfo[groupName] == null)
            {
                return false;
            }
            return (groupInfo[groupName].groupPassword == groupPassword);
        }
    }

    public class GroupObject
    {
        public GroupObject(string groupOwner, GroupPrivacy groupPrivacy)
        {
            this.groupOwner = groupOwner;
            this.groupPrivacy = groupPrivacy;
        }

        public string groupOwner;
        public GroupPrivacy groupPrivacy;
        public string groupPassword;
    }

    public enum GroupPrivacy
    {
        PUBLIC,
        PRIVATE,
    }
}

