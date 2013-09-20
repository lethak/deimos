using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Collections.Generic;

using System.Data;
using System.Text.RegularExpressions;

using PRoCon.Core;
using PRoCon.Core.Plugin;
using PRoCon.Core.Plugin.Commands;
using PRoCon.Core.Players;
using PRoCon.Core.Players.Items;
using PRoCon.Core.Battlemap;
using PRoCon.Core.Maps;

using System.Net;

namespace PRoConEvents
{

    public class lethakAlertAdmin : PRoConPluginAPI, IPRoConPluginInterface
    {
        private bool plugin_enabled;
		private string serverName;
		
		public lethakAlertAdmin()
        {
		
		}
		
        public void TS3_Invoke_proconchat(string querystring)
        {
			WebClient webClient = new WebClient();
			webClient.Headers.Add("Content-Type", "text/xml");
			byte[] responseBytes = webClient.DownloadData("http://nfr.imperialbase.net/teamspeak/index/proconchat/?"+querystring);
        }

        public void OnPlayerKilled(Kill killInfo)
        {       
        }

        private void TextAction(string strSpeaker, string strMessage)
        {
            //this.ConsoleWrite("TextAction [" + strSpeaker + "] " + strMessage);

            /**/
            bool cmdAdmin = Regex.Match(strMessage, @"\s*[!@/]\s*admin", RegexOptions.IgnoreCase).Success;
            bool wordAdmin = Regex.Match(strMessage, @"admin", RegexOptions.IgnoreCase).Success;
            bool cmdLethak = Regex.Match(strMessage, @"\s*[!@/]\s*lethak", RegexOptions.IgnoreCase).Success;
            /**/

            if ((cmdAdmin || wordAdmin) && strSpeaker!="Server")
            {
                //this.ConsoleWrite("admin quote [" + strSpeaker + "] " + strMessage);
                //this.ExecuteCommand("procon.protected.send", "admin.say", strMessage, "all");
				TS3_Invoke_proconchat("gserv="+this.serverName+"&pname="+strSpeaker+"&pmsg="+strMessage+"");
            }

            if (cmdLethak)
            {
                //this.ExecuteCommand("procon.protected.send", "admin.say", "lethak is admin", "all");
            }
            
        }

        public void OnGlobalChat(string strSpeaker, string strMessage)
        {
            this.TextAction(strSpeaker, strMessage);
        }
        public void OnTeamChat(string strSpeaker, string strMessage, int iTeamID)
        {
            this.TextAction(strSpeaker, strMessage);
        }
        public void OnSquadChat(string strSpeaker, string strMessage, int iTeamID, int iSquadID)
        {
            this.TextAction(strSpeaker, strMessage);
        }

        public void OnPlayerLeft(string strSoldierName)
        {
        }
        public void OnRoundOver(int iWinningTeamID)
        {
        }

        private void ConsoleWrite(string msg)
        {
            string prefix = "[^b" + GetPluginName() + "^n] ";
            this.ExecuteCommand("procon.protected.pluginconsole.write", prefix + msg);
        }
        private void KillPlayer(string SoldierName, string strMessage)
        {
            this.ExecuteCommand("procon.protected.send", "admin.say", strMessage, "all");
            this.ExecuteCommand("procon.protected.send", "admin.killPlayer", SoldierName);
        }
		

		
		public void OnServerInfo(CServerInfo csiServerInfo)
        {
            this.serverName = csiServerInfo.ServerName;
			//TS3_Invoke_proconchat("gserv="+this.serverName+"&pname=Procon Frostbite&pmsg=OnServerInfo");
        }

        public void OnPluginEnable()
        {
            this.plugin_enabled = true;
            ConsoleWrite("Enabled");
            this.ExecuteCommand("procon.protected.send", "admin.say", "Plugin info: Alert !admin enabled", "all");
			
			TS3_Invoke_proconchat("gserv="+this.serverName+"&pname=Procon Frostbite&pmsg=OnPluginEnable");

        }
		

        public void OnPluginDisable()
        {
            this.plugin_enabled = false;
            ConsoleWrite("Disabled");
            this.ExecuteCommand("procon.protected.send", "admin.say", "Plugin info: Alert !admin disabled", "all");
			TS3_Invoke_proconchat("gserv="+this.serverName+"&pname=Procon Frostbite&pmsg=OnPluginDisable");
        }

        public string GetPluginName()
        {
            return "lethakAlertAdmin";
        }
        public string GetPluginVersion()
        {
            return "0.0.0.1";
        }
        public string GetPluginAuthor()
        {
            return "lethak";
        }
        public string GetPluginWebsite()
        {
            return "lethak@imperialbase.net";
        }
        public string GetPluginDescription()
        {
            return "alert server admins via in game global chat";
        }

        public List<CPluginVariable> GetDisplayPluginVariables()
        {
            List<CPluginVariable> lstReturn = new List<CPluginVariable>();
            return lstReturn;
        }
        public List<CPluginVariable> GetPluginVariables()
        {
            List<CPluginVariable> lstReturn = new List<CPluginVariable>();
            return lstReturn;
        }
        public void SetPluginVariable(string strVariable, string strValue)
        {
        }
        public void OnPluginLoaded(string strHostName, string strPort, string strPRoConVersion)
        {
        }

    }

}
