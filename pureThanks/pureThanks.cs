//Import various C# things.
using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;

//Import Procon things.
using PRoCon.Core;
using PRoCon.Core.Plugin;
using PRoCon.Core.Players;

namespace PRoConEvents
{
    public class pureThanks : PRoConPluginAPI, IPRoConPluginInterface
    {

        private bool pluginEnabled = false;

        private List<String> donatorList = new List<String>();
        //private List<String> onlineListFull = new List<String>();
        private List<String> onlineList = new List<String>();
        private List<String> onlineListPrint = new List<String>();
        private String thanksMessage = "Thanks to our active donors & volunteers online: [LIST]";
        private String debugLevelString = "1";
        private int debugLevel = 1;
        private String timeDelayString = "15";
        private int timeDelay = 15;

        public pureThanks()
        {

        }

        public string GetPluginName()
        {
            return "pureThanks";
        }

        public string GetPluginVersion()
        {
            return "0.3.6";
        }

        public string GetPluginAuthor()
        {
            return "Analytalica";
        }

        public string GetPluginWebsite()
        {
            return "purebattlefield.org";
        }

        public string GetPluginDescription()
        {
            return @"<p>pureThanks is a plugin that thanks players who are online at the end of the round.</p>";
        }

        public void toChat(String message)
        {
            if (!message.Contains("\n") && !String.IsNullOrEmpty(message))
            {
                toConsole(2, "Sent to chat: \"" + message + "\"");
                this.ExecuteCommand("procon.protected.send", "admin.say", message, "all");
            }
            else if(message != "\n")
            {
                string[] multiMsg = message.Split(new string[] { "\n" }, StringSplitOptions.None);
                foreach (string send in multiMsg)
                {
                    toChat(send);
                }
            }
        }

        public void toChat(String message, String playerName)
        {
            if (!message.Contains("\n") && !String.IsNullOrEmpty(message))
            {
                toConsole(2, "Sent to chat: \"" + message + "\" to player: " + playerName);
                this.ExecuteCommand("procon.protected.send", "admin.say", message, "player", playerName);
            }
            else if (message != "\n")
            {
                string[] multiMsg = message.Split(new string[] { "\n" }, StringSplitOptions.None);
                foreach (string send in multiMsg)
                {
                    toChat(send, playerName);
                }
            }
        }

        public void toConsole(int msgLevel, String message)
        {
            if (debugLevel >= msgLevel)
            {
                this.ExecuteCommand("procon.protected.pluginconsole.write", "pureThanks: " + message);
            }
        }

        //--------------------------------------
        //These methods run when Procon does what's on the label.
        //--------------------------------------

        //Runs when the plugin is compiled.

        public void OnPluginLoaded(string strHostName, string strPort, string strPRoConVersion)
        {
            this.RegisterEvents(this.GetType().Name, "OnPluginLoaded", "OnRoundOver", "OnListPlayers");
        }

        public void OnPluginEnable()
        {
            this.pluginEnabled = true;
            this.toConsole(1, "pureThanks Enabled!");
            //creditDonators();
        }

        public void OnPluginDisable()
        {
            onlineListPrint = onlineList;
            creditDonators();
            this.pluginEnabled = false;
            this.toConsole(1, "pureThanks Disabled!");
        }

        public override void OnListPlayers(List<CPlayerInfo> players, CPlayerSubset subset)
        {
            if (this.pluginEnabled)
            {
                List<String> newOnlineList = new List<String>();
                toConsole(2, "Player list obtained.");
                foreach (CPlayerInfo player in players)
                {
                    if (donatorList.Contains(player.SoldierName.Trim().ToLower()))
                    {
                        newOnlineList.Add(player.SoldierName.Trim());
                    }
                }
                onlineList = newOnlineList;
                toConsole(2, "Donators/Volunteers found online: ");
                foreach (string name in onlineList)
                {
                    toConsole(2, name);
                }
            }
        }

        public override void OnRoundOver(int winningTeamId) 
        {
            if (this.pluginEnabled)
            {
                toConsole(2, "Round ended...");
                onlineListPrint = onlineList;
                //this.ExecuteCommand("procon.protected.send", "admin.listPlayers", "all");
            }
        }
        
        public void creditDonators()
        {
            toConsole(2, "Crediting donators and volunteers...");
            String theList = "";
            int len = onlineListPrint.Count;
            if(len > 0){
                if(len == 1){
                    theList = onlineListPrint[0];
                }else if(len == 2){
                    theList = onlineListPrint[0] + " and " + onlineListPrint[1] + ".";
                }else if(len > 2){

                    StringBuilder sb = new StringBuilder();

                    for(int n = 0; n < len - 1; n++){
                        if (sb.ToString().Length - sb.ToString().LastIndexOf("/n") > 120)
                            sb.Append("/n");
                        sb.Append(onlineListPrint[n]).Append(", ");
                    }

                    sb.Append("and ").Append(onlineListPrint[len - 1]).Append(".");
                    theList = sb.ToString();
                }
            }
            toConsole(2, "[LIST] is " + theList);
            if(theList.Length > 0){
                toChat(thanksMessage.Replace("[LIST]", "/n" + theList + "/n"));
            }
        }

        //List plugin variables.
        public List<CPluginVariable> GetDisplayPluginVariables()
        {
            List<CPluginVariable> lstReturn = new List<CPluginVariable>();
            lstReturn.Add(new CPluginVariable("Donator/Volunteer List|Add a soldier name... (ci)", typeof(string), ""));
            donatorList.Sort();
            for(int i = 0; i < donatorList.Count; i++)
            {
                String thisPlayer = donatorList[i];
                if (String.IsNullOrEmpty(thisPlayer))
                {
                    donatorList.Remove(thisPlayer);
                    i--;
                }
                else
                {
                    lstReturn.Add(new CPluginVariable("Donator/Volunteer List|" + i.ToString() + ". Soldier name:", typeof(string), thisPlayer));
                }
            }
            lstReturn.Add(new CPluginVariable("Settings|Thanks Message", typeof(string), thanksMessage));
            lstReturn.Add(new CPluginVariable("Settings|Message Time Delay", typeof(string), timeDelayString));
            lstReturn.Add(new CPluginVariable("Settings|Debug Level", typeof(string), debugLevelString));
            
            return lstReturn;
        }

        public List<CPluginVariable> GetPluginVariables()
        {
            return GetDisplayPluginVariables();
        }

        public int getConfigIndex(string configString)
        {
            int lineLocation = configString.IndexOf('|');
            return Int32.Parse(configString.Substring(lineLocation + 1, configString.IndexOf('.') - lineLocation - 1));
        }

        //Set variables.
        public void SetPluginVariable(String strVariable, String strValue)
        {
            if (strVariable.Contains("Soldier name:"))
            {
                int n = getConfigIndex(strVariable);
                try
                {
                    donatorList[n] = strValue.Trim().ToLower();
                }
                catch (ArgumentOutOfRangeException e)
                {
                    donatorList.Add(strValue.Trim().ToLower());
                }
            }
            else if (strVariable.Contains("Add a soldier name"))
            {
                donatorList.Add(strValue.Trim().ToLower());
            }
            else if (strVariable.Contains("Debug Level"))
            {
                debugLevelString = strValue;
                try
                {
                    debugLevel = Int32.Parse(debugLevelString);
                }
                catch (Exception z)
                {
                    toConsole(1, "Invalid debug level! Choose 0, 1, or 2 only.");
                    debugLevel = 1;
                    debugLevelString = "1";
                }
            }
            else if (strVariable.Contains("Message Time Delay"))
            {
                timeDelayString = strValue;
                try
                {
                    timeDelay = Int32.Parse(timeDelayString);
                }
                catch (Exception z)
                {
                    toConsole(1, "Invalid time delay! Use integer values only.");
                    timeDelay = 15;
                    timeDelayString = "15";
                }
            }
            else if (strVariable.Contains("Thanks Message"))
            {
                thanksMessage = strValue.Trim();
            }
        }
    }
}