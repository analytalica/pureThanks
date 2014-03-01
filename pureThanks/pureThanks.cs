//Import various C# things.
using System;
using System.IO;
using System.Text;
using System.Timers;
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
        //The List of donators as configured.
        private List<String> donatorList = new List<String>();
        //The list of donators immediately detected online.
        private List<String> onlineList = new List<String>();
        //The list of players that will be thanked.
        private List<String> onlineListPrint = new List<String>();
        private String thanksMessage = "Thanks to our active donors & volunteers online: [LIST]";
        private String debugLevelString = "1";
        private int debugLevel = 1;
        private String timeDelayString = "25";
        private int timeDelay = 25;

        private Timer thanksTimer = new Timer();
        public string thanksOutput = "";
        private Timer chatTimer = new Timer();
        private Queue<String> chatQueue = new Queue<String>();

        public pureThanks()
        {

        }

        public string GetPluginName()
        {
            return "pureThanks";
        }

        public string GetPluginVersion()
        {
            return "1.0.7";
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
            return @"<p>pureThanks is a plugin that thanks select players who are online at the end of the round. When the round ends, the players in the configuration list who are currently playing are sent to a delayed chat output.<br/><br/>This plugin was developed by Analytalica for PURE Battlefield/PURE Gaming.</p>
<p><b>To configure the chat output:</b>
<ul>
<li>Set the 'Thanks Message' to the message that is shown to all players at the end of the round + delay.</li>
<li>In the 'Thanks Message' text, add [LIST] to insert the player list. It is replaced with 'player1, player2, ..., and playerN.' with special cases 'player1' and 'player1 and player2.' when there are only one or two set found players online, respectively.
<li>The 'Message Time Delay' adjusts the amount of time, in seconds, after the round ends to send the message.</li>
<li>Type anything into 'Test output...' to simulate a round over event, causing the (delayed) output to run.</li>
</ul></p>
<p><b>Managing players:</b>
<ul>
<li>Type a Battlelog soldier name into 'Add a soldier name...' and that player will be added to the liset.</li>
<li>Soldier names are automatically sorted alphabetically.</li>
<li>Soldier names are removed when their respective entry field is cleared.</li>
</ul></p>
<p>The default time delay is 25 seconds. If no chosen players are found online, there is no message output. By toggling the debug level to 3, 4, or 5, you can simulate having listed players online.</p>";
        }

        public void toChat(String message)
        {
            if (!message.Contains("\n") && !String.IsNullOrEmpty(message))
            {
                toConsole(2, "Sent to chat queue: \"" + message + "\"");
                chatQueue.Enqueue(message);
                this.ExecuteCommand("procon.protected.send", "admin.say", message, "all");
            }
            else if(message != "\n")
            {
                string[] multiMsg = message.Split(new string[] { "\n" }, StringSplitOptions.None);
                foreach (string send in multiMsg)
                {
                    if (!String.IsNullOrEmpty(send))
                    {
                        toConsole(2, "Sent to chat queue: \"" + send + "\"");
                        chatQueue.Enqueue(send);
                    }
                }
            }
        }

        public void chatOut(object source, ElapsedEventArgs e)
        {
            if (chatQueue.Count > 0)
            {
                this.ExecuteCommand("procon.protected.send", "admin.say", chatQueue.Dequeue(), "all");
                toConsole(3, "Chat output...");
            }
        }

        #region player chat function
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
                    System.Threading.Thread.Sleep(400);
                    toChat(send, playerName);
                }
            }
        }
        #endregion
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
            this.chatTimer = new Timer();
            this.chatTimer.Elapsed += new ElapsedEventHandler(this.chatOut);
            this.chatTimer.Interval = 400;
            this.chatTimer.Start();
            this.toConsole(2, "chatTimer Enabled!");
            //creditDonators();
        }

        public void OnPluginDisable()
        {
            //onlineListPrint = onlineList;
            //creditDonators();
            this.chatTimer.Stop();
            this.thanksTimer.Stop();
            this.toConsole(1, "pureThanks Disabled!");
            this.pluginEnabled = false;
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
                if (debugLevel > 1)
                {
                    foreach (string name in onlineList)
                    {
                        toConsole(2, name);
                    }
                }
            }
        }

        public override void OnRoundOver(int winningTeamId) 
        {
            if (this.pluginEnabled)
            {
                toConsole(2, "Round ended...");
                onlineListPrint = onlineList;
                creditDonators();
                //this.ExecuteCommand("procon.protected.send", "admin.listPlayers", "all");
            }
        }
        
        public void creditDonators()
        {
            toConsole(1, "Crediting donators and volunteers...");
            String theList = "";
            #region Debug outputs
            if (debugLevel > 2)
            {
                toConsole(3, "Debug level greater than 2. Test output enabled.");
                switch (debugLevel)
                {
                    case 3:
                        onlineListPrint = new List<String>() { "Analytalica" };
                        break;
                    case 4: 
                        onlineListPrint = new List<String>() { "Analytalica", "Draeger" };
                        break;
                    case 5:
                        onlineListPrint = new List<String>() { "Analytalica", "Draeger", "Adama42", "Robozman", "Smileynulk", "VladmirPut1n", "Barack0bama", "G3orgeBvsh", "J1mmyC4rter", "abrahamL1nc0ln", "ge0rge_wash1ngton", "ANDREWJACKS0N", "RichardNixxxon" };
                        break;
                }
            }
            #endregion
            int len = onlineListPrint.Count;
            if(len > 0){
                if(len == 1){
                    theList = onlineListPrint[0];
                }else if(len == 2){
                    theList = onlineListPrint[0] + " and " + onlineListPrint[1] + ".";
                }else if(len > 2){

                    StringBuilder sb = new StringBuilder();

                    for(int n = 0; n < len - 1; n++){
                        if (sb.ToString().Length - sb.ToString().LastIndexOf("\n") > 120)
                            sb.Append("\n");
                        sb.Append(onlineListPrint[n]).Append(", ");
                    }

                    sb.Append("and ").Append(onlineListPrint[len - 1]).Append(".");
                    theList = sb.ToString();
                }
            }
            toConsole(2, "[LIST] is " + theList);
            if(theList.Length > 0){
                this.thanksOutput = thanksMessage.Replace("[LIST]", "\n" + theList + "\n");
                if (timeDelay < 2) {
                    toChat(thanksOutput);
                    this.thanksTimer.Stop();
                }
                else
                {
                    this.thanksTimer = new Timer();
                    this.thanksTimer.Elapsed += new ElapsedEventHandler(this.thanksOut);
                    this.thanksTimer.Interval = timeDelay * 1000;
                    this.thanksTimer.Start();
                }
            }
        }

        public void thanksOut(object source, ElapsedEventArgs e)
        {
            toChat(thanksOutput);
            this.thanksTimer.Stop();
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
            lstReturn.Add(new CPluginVariable("Settings|Message Time Delay (sec)", typeof(string), timeDelayString));
            lstReturn.Add(new CPluginVariable("Settings|Debug Level", typeof(string), debugLevelString));
            lstReturn.Add(new CPluginVariable("Settings|Test output...", typeof(string), ""));
            
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
                    timeDelay = 25;
                    timeDelayString = "25";
                }
            }
            else if (strVariable.Contains("Thanks Message"))
            {
                thanksMessage = strValue.Trim();
            }
            else if (strVariable.Contains("Test output..."))
            {
                if(!String.IsNullOrEmpty(strValue)){
                    onlineListPrint = onlineList;
                    creditDonators();
                }
            }
        }
    }
}