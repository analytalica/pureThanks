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
        //The List of admins as configured.
        private List<String> adminList = new List<String>();
        //The list of donators immediately detected online.
        private List<String> onlineList = new List<String>();
        //The list of admins immediately detected online.
        private List<String> adminsOnlineList = new List<String>();
        //The list of players that will be thanked.
        private List<String> onlineListPrint = new List<String>();
        private String thanksMessage = "Thanks to our active donors & volunteers online: [LIST]";
        private String adminsOnlineMessage = "The following admins are currently online: [LIST]";
        private String noAdminsOnlineMessage = "There are currently no admins online, if you need an admin please use !pageadmin to page an admin";
        private String debugLevelString = "1";
        private int debugLevel = 1;
        private String timeDelayString = "25";
        private String adminTimeDelayString = "60";
        private int timeDelay = 25;
        private int adminTimeDelay = 60 * 1000;

        private Timer thanksTimer = new Timer();
        public string thanksOutput = "";
        private Timer chatTimer = new Timer();
        private Timer listPlayersTimer = new Timer();
        private Timer listAdminsTimer = new Timer();
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
            return "1.5.7";
        }

        public string GetPluginAuthor()
        {
            return "Analytalica & CrashCourse001";
        }

        public string GetPluginWebsite()
        {
            return "purebattlefield.org";
        }

        public string GetPluginDescription()
        {
            return @"<p>pureThanks is a plugin that thanks select players who are online at the end of the round. When the round ends, the players in the configuration list who are currently playing are sent to a delayed chat output.
                    <br/><br/>This plugin was developed by Analytalica for PURE Battlefield/PURE Gaming.
                    <br/>The admin list portion was developed by CrashCourse001 for PURE Battlefield/PURE Gaming.
                    </p>
                    <h1>For the Donator/Volunteer list:</h1>
                    <p><b>To configure the chat output:</b>
                    <ul>
                    <li>Set the 'Thanks Message' to the message that is shown to all players at the end of the round + delay.</li>
                    <li>In the 'Thanks Message' text, add [LIST] to insert the player list. It is replaced with 'player1, player2, ..., and playerN.' with special cases 'player1' and 'player1 and player2.' when there are only one or two set found players online, respectively.
                    <li>The 'Message Time Delay' adjusts the amount of time, in seconds, after the round ends to send the message.</li>
                    <li>Type anything into 'Test output...' to simulate a round over event, causing the (delayed) output to run.</li>
                    </ul></p>
                    <p><b>Managing players:</b>
                    <ul>
                    <li>Type a Battlelog soldier name into 'Add a soldier name...' and that player will be added to the list.</li>
                    <li>Soldier names are automatically sorted alphabetically.</li>
                    <li>Soldier names are removed when their respective entry field is cleared.</li>
                    </ul></p>
                    <p>The default time delay is 25 seconds. If no chosen players are found online, there is no message output. By toggling the debug level to 3, 4, or 5, you can simulate having listed players online.</p>

                    <h1>For the Admin online list:</h1>
                    <p><b>To configure the chat output:</b>
                    <ul>
                    <li>Set the 'Admins Online Message' to the message that is shown to all players at the given interval (Admin Message Time Delay) if an admin is currently online.</li>
                    <li>Set the 'No Admins Online Message' to the message that is shown to all players at the given interval (Admin Message Time Delay) if no admins are currently online (this message can be disabled by using 'none' (without the quotes) as the message.</li>
                    <li>In the 'Admins Online Message' text, add [LIST] to insert the player list. It is replaced with 'admin1, admin2, ..., and adminN.' with special cases 'admin1' and 'admin1 and admin2.' when there are only one or two set found players online, respectively.
                    <li>The 'Admin Message Time Delay' adjusts how often, in seconds, to send the list of online admins to the entire server.</li>
                    <li>Type anything into 'Test output...' to display the online admins immediately.</li>
                    </ul></p>
                    <p><b>Managing admins:</b>
                    <ul>
                    <li>Type a Battlelog soldier name into 'Add an admin...' and that player will be added to the list.</li>
                    <li>Admin names are automatically sorted alphabetically.</li>
                    <li>Admin names are removed when their respective entry field is cleared.</li>
                    </ul></p>
                    <p>The default message interval is 60 seconds. If no admins are found online, the 'No Admins Online Message' is output. By toggling the debug level to 3, 4, or 5, you can simulate having listed admins online.</p>";
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
            if (this.pluginEnabled)
            {
                //this.toConsole(2, "chatTimer ticking...");
                if (chatQueue.Count > 0)
                {
					string nextOutput = chatQueue.Dequeue();
                    this.ExecuteCommand("procon.protected.send", "admin.say", nextOutput, "all");
                    this.toConsole(1, "Chat output: " + nextOutput);
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
            try
            {
                this.RegisterEvents(this.GetType().Name, "OnPluginLoaded", "OnRoundOver", "OnListPlayers");

                this.chatTimer = new Timer();
                this.chatTimer.Elapsed += new ElapsedEventHandler(this.chatOut);
                this.chatTimer.Interval = 800;
                this.chatTimer.Stop();
                this.toConsole(2, "chatTimer Initialized!");

                this.listPlayersTimer = new Timer();
                this.listPlayersTimer.Elapsed += new ElapsedEventHandler(this.callListPlayers);
                this.listPlayersTimer.Interval = 5000;
                this.listPlayersTimer.Stop();
                this.toConsole(2, "listPlayersTimer Initialized!");

                this.listAdminsTimer = new Timer();
                this.listAdminsTimer.Elapsed += new ElapsedEventHandler(this.printAdmins);
                this.listAdminsTimer.Interval = adminTimeDelay;
                this.listAdminsTimer.Stop();
                this.toConsole(2, "listAdminsTimer Initialized!");

                this.thanksTimer = new Timer();
                this.thanksTimer.Elapsed += new ElapsedEventHandler(this.thanksOut);
                this.thanksTimer.Interval = timeDelay * 1000;
                this.thanksTimer.Stop();
                this.toConsole(2, "thanksTimer Initialized!");
            }
            catch (Exception e)
            {
                this.toConsole(1, e.ToString());
            }
        }

        public void OnPluginEnable()
        {
            try
            {
                this.pluginEnabled = true;

                this.chatTimer = new Timer();
                this.chatTimer.Elapsed += new ElapsedEventHandler(this.chatOut);
                this.chatTimer.Interval = 800;
                this.chatTimer.Start();
                this.toConsole(2, "chatTimer Initialized!");

                this.listPlayersTimer = new Timer();
                this.listPlayersTimer.Elapsed += new ElapsedEventHandler(this.callListPlayers);
                this.listPlayersTimer.Interval = 5000;
                this.listPlayersTimer.Start();
                this.toConsole(2, "listPlayersTimer Initialized!");

                this.listAdminsTimer = new Timer();
                this.listAdminsTimer.Elapsed += new ElapsedEventHandler(this.printAdmins);
                this.listAdminsTimer.Interval = adminTimeDelay;
                this.listAdminsTimer.Start();
                this.toConsole(2, "listAdminsTimer Initialized!");

                this.thanksTimer = new Timer();
                this.thanksTimer.Elapsed += new ElapsedEventHandler(this.thanksOut);
                this.thanksTimer.Interval = timeDelay * 1000;
                this.thanksTimer.Start();
                this.toConsole(2, "thanksTimer Initialized!");
                //creditDonators();

                //The list of donators immediately detected online.
                this.onlineList = new List<String>();
                //The list of admins immediately detected online.
                this.adminsOnlineList = new List<String>();
                //The list of players that will be thanked.
                this.onlineListPrint = new List<String>();
                this.toConsole(2, "Lists cleared!");
                this.toConsole(1, "pureThanks Enabled!");
            }
            catch (Exception e)
            {
                this.toConsole(1, e.ToString());
            }
        }

        public void OnPluginDisable()
        {
            try
            {
            //onlineListPrint = onlineList;
            //creditDonators();
            this.chatTimer.Stop();
            this.thanksTimer.Stop();
			this.listPlayersTimer.Stop();
            this.listAdminsTimer.Stop();

            //The list of donators immediately detected online.
            this.onlineList = new List<String>();
            //The list of admins immediately detected online.
            this.adminsOnlineList = new List<String>();
            //The list of players that will be thanked.
            this.onlineListPrint = new List<String>();
            this.toConsole(2, "Lists cleared!");
            this.toConsole(1, "pureThanks Disabled!");
            this.pluginEnabled = false;
            }
            catch (Exception e)
            {
                this.toConsole(1, e.ToString());
            }
        }
		
		public void callListPlayers(object source, ElapsedEventArgs e){
			if(this.pluginEnabled){
				this.toConsole(2, "Calling list players...");
				this.ExecuteCommand("procon.protected.send", "admin.listPlayers", "all");
			}
		}

        public void printAdmins(object source, ElapsedEventArgs e)
        {
            if (this.pluginEnabled)
            {
                this.toConsole(2, "Printing Admins...");
                #region Debug outputs
                if (debugLevel > 2)
                {
                    toConsole(3, "Debug level greater than 2. Test output enabled.");
                    switch (debugLevel)
                    {
                        case 3:
                            adminsOnlineList = new List<String>() { "Analytalica" };
                            break;
                        case 4:
                            adminsOnlineList = new List<String>() { "Analytalica", "Draeger" };
                            break;
                        case 5:
                            adminsOnlineList = new List<String>() { "Analytalica", "Draeger", "Adama42", "Robozman", "Smileynulk", "VladmirPut1n", "Barack0bama", "G3orgeBvsh", "J1mmyC4rter", "abrahamL1nc0ln", "ge0rge_wash1ngton", "ANDREWJACKS0N", "RichardNixxxon" };
                            break;
                    }
                }
                #endregion

                int numOfAdminsOnline = adminsOnlineList.Count;
                if (numOfAdminsOnline == 0 && !noAdminsOnlineMessage.ToLower().Equals("none"))
                {
                    this.toChat(noAdminsOnlineMessage + "\n");
                }
                else
                {
                    String LIST = getAdminListString(adminsOnlineList);
                    String output = adminsOnlineMessage.Replace("[LIST]", "\n" + LIST + "\n");
                    this.toChat(output);
                }
            }
            else
            {
                this.toConsole(2, "Plugin is disabled (printAdmins was called).");
            }
        }
        private String getAdminListString(List<String> adminsOnlineList)
        {
            int numOfAdminsOnline = adminsOnlineList.Count;
            String outString = "";
            if (numOfAdminsOnline == 1)
            {
                outString = adminsOnlineList[0];
            } else if (numOfAdminsOnline == 2)
            {
                outString = adminsOnlineList[0] + " and " + adminsOnlineList[1] + ".";
            }
            else
            {
                StringBuilder outStringBuilder = new StringBuilder();
                for (int n = 0 ; n < numOfAdminsOnline - 1; n++)
                {
                    if (outStringBuilder.ToString().Length - outStringBuilder.ToString().LastIndexOf("\n") > 100)
                        outStringBuilder.Append("\n");
                    outStringBuilder.Append(adminsOnlineList[n]).Append(", ");
                }
                outStringBuilder.Append("and ").Append(adminsOnlineList[numOfAdminsOnline - 1]).Append(".");
                outString = outStringBuilder.ToString();
            }
            toConsole(2, "Admin [LIST] is " + outString);
            return outString;
        }
        public override void OnListPlayers(List<CPlayerInfo> players, CPlayerSubset subset)
        {
            if (this.pluginEnabled)
            {
				this.listPlayersTimer.Stop();
				this.listPlayersTimer = new Timer();
				this.listPlayersTimer.Elapsed += new ElapsedEventHandler(this.callListPlayers);
				this.listPlayersTimer.Interval = 5000;
				this.listPlayersTimer.Start();

                List<String> newOnlineList = new List<String>();
                List<String> tempAdminsOnlineList = new List<String>();
                toConsole(2, "Player list obtained.");
                foreach (CPlayerInfo player in players)
                {
                    if (donatorList.Contains(player.SoldierName.Trim().ToLower()))
                    {
                        newOnlineList.Add(player.SoldierName.Trim());
                    }
                    if (adminList.Contains(player.SoldierName.Trim().ToLower()))
                    {
                        tempAdminsOnlineList.Add(player.SoldierName.Trim());
                    }
                }
                this.onlineList = newOnlineList;
                this.adminsOnlineList = tempAdminsOnlineList;
                toConsole(2, "Donators/Volunteers found online: ");
                if (debugLevel > 1)
                {
                    foreach (string name in onlineList)
                    {
                        toConsole(2, name);
                    }
                }
                toConsole(2, "Admins found online: ");
                if (debugLevel > 1)
                {
                    foreach (string name in adminsOnlineList)
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
                this.toConsole(2, "Round ended...");
                this.onlineListPrint = this.onlineList;
                this.creditDonators();
                //this.ExecuteCommand("procon.protected.send", "admin.listPlayers", "all");
            }
        }
        
        public void creditDonators()
        {
            if (pluginEnabled)
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
                if (len > 0)
                {
                    if (len == 1)
                    {
                        theList = onlineListPrint[0];
                    }
                    else if (len == 2)
                    {
                        theList = onlineListPrint[0] + " and " + onlineListPrint[1] + ".";
                    }
                    else if (len > 2)
                    {

                        StringBuilder sb = new StringBuilder();

                        for (int n = 0; n < len - 1; n++)
                        {
                            if (sb.ToString().Length - sb.ToString().LastIndexOf("\n") > 100)
                                sb.Append("\n");
                            sb.Append(onlineListPrint[n]).Append(", ");
                        }

                        sb.Append("and ").Append(onlineListPrint[len - 1]).Append(".");
                        theList = sb.ToString();
                    }
                }
                toConsole(2, "[LIST] is " + theList);
                if (theList.Length > 0)
                {
                    this.thanksOutput = thanksMessage.Replace("[LIST]", "\n" + theList + "\n");
                    if (timeDelay < 2)
                    {
                        this.toChat(this.thanksOutput);
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
            else
            {
                this.toConsole(2, "Plugin is disabled (creditDonators was called).");
            }
        }

        public void thanksOut(object source, ElapsedEventArgs e)
        {
            if (pluginEnabled)
            {
                this.toConsole(2, "thanksOut Called.");
                this.toChat(this.thanksOutput);
                this.thanksTimer.Stop();
            }
            else
            {
                this.toConsole(2, "Plugin is disabled (thanksOut was called). It will be stopped.");
                this.thanksTimer.Stop();
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

            lstReturn.Add(new CPluginVariable("Donor List Settings|Thanks Message", typeof(string), thanksMessage));
            lstReturn.Add(new CPluginVariable("Donor List Settings|Message Time Delay (sec)", typeof(string), timeDelayString));
            lstReturn.Add(new CPluginVariable("Donor List Settings|Test output...", typeof(string), ""));


            lstReturn.Add(new CPluginVariable("Admins List|Add an admin... (ci)", typeof(string), ""));
            adminList.Sort();
            for (int i = 0; i < adminList.Count; i++)
            {
                String thisPlayer = adminList[i];
                if (String.IsNullOrEmpty(thisPlayer))
                {
                    adminList.Remove(thisPlayer);
                    i--;
                }
                else
                {
                    lstReturn.Add(new CPluginVariable("Admins List|" + i.ToString() + ". Admin name:", typeof(string), thisPlayer));
                }
            }
            lstReturn.Add(new CPluginVariable("Admin List Settings|Admins Online Message", typeof(string), adminsOnlineMessage));
            lstReturn.Add(new CPluginVariable("Admin List Settings|No Admins Online Message", typeof(string), noAdminsOnlineMessage));
            lstReturn.Add(new CPluginVariable("Admin List Settings|Admin Message Interval (sec)", typeof(string), adminTimeDelayString));
            lstReturn.Add(new CPluginVariable("Admin List Settings|Admin Test output...", typeof(string), ""));

            lstReturn.Add(new CPluginVariable("General Settings|Debug Level", typeof(string), debugLevelString));
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
            try
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
                else if (strVariable.Contains("Admin name:"))
                {
                    int n = getConfigIndex(strVariable);
                    try
                    {
                        adminList[n] = strValue.Trim().ToLower();
                    }
                    catch (ArgumentOutOfRangeException e)
                    {
                        adminList.Add(strValue.Trim().ToLower());
                    }
                }
                else if (strVariable.Contains("Add an admin"))
                {
                    adminList.Add(strValue.Trim().ToLower());
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
                else if (strVariable.Contains("Admin Message Interval"))
                {
                    adminTimeDelayString = strValue;
                    try
                    {
                        adminTimeDelay = Int32.Parse(adminTimeDelayString) * 1000;

                        this.listAdminsTimer.Interval = adminTimeDelay;
                    }
                    catch (Exception z)
                    {
                        toConsole(1, "Invalid admin time delay! Use integer values only.");
                        adminTimeDelay = 60 * 1000;
                        this.listAdminsTimer.Interval = adminTimeDelay;
                        adminTimeDelayString = "60";
                    }
                }
                else if (strVariable.Contains("Message Time Delay") && !strVariable.Contains("Admin"))
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
                    if (!String.IsNullOrEmpty(strValue.Trim()))
                    {
                        thanksMessage = strValue.Trim();
                    }
                    else
                    {
                        toConsole(1, "Resetting thanks message...");
                        thanksMessage = "Thanks to our active donors & volunteers online: [LIST]";
                    }
                }
                else if (strVariable.Contains("Admins Online Message") && !strVariable.Contains("No "))
                {
                    if (!String.IsNullOrEmpty(strValue.Trim()))
                    {
                        adminsOnlineMessage = strValue.Trim();
                    }
                    else
                    {
                        toConsole(1, "Resetting admins online message...");
                        adminsOnlineMessage = "The following admins are currently online: [LIST]";
                    }
                }
                else if (strVariable.Contains("No Admins Online Message"))
                {
                    if (!String.IsNullOrEmpty(strValue.Trim()))
                    {
                        noAdminsOnlineMessage = strValue.Trim();
                    }
                    else
                    {
                        toConsole(1, "Resetting no admins online message...");
                        noAdminsOnlineMessage = "There are currently no admins online, use !pageadmin to page an admin";
                    }
                }
                else if (strVariable.Contains("Admin Test output..."))
                {
                    if (!String.IsNullOrEmpty(strValue))
                    {
                        toConsole(1, "Displaying Test Output...");
                        printAdmins(null, null);
                    }
                }
                else if (strVariable.Contains("Test output...") && !strVariable.Contains("Admin"))
                {
                    if (!String.IsNullOrEmpty(strValue))
                    {
                        this.onlineListPrint = onlineList;
                        toConsole(1, "Displaying donator test output...");
                        toConsole(1, "Simulating a round over event...");
                        this.OnRoundOver(1);
                    }
                }
            }
            catch (Exception e)
            {
                this.toConsole(1, e.ToString());
            }
        }
    }
}