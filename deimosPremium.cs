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
	public class deimosPremium : PRoConPluginAPI, IPRoConPluginInterface
	{
		public deimosPremium()
		{
			PremiumBackerController.levelSystem_enabled = true;
			PremiumBackerController.levelSystem_DefaultBackerLevel = 30;
			
			PremiumBackerController.commandChat_enabled = true;
		}

		public string GetPluginName()
		{
			return "Deimos Premium System";
		}
		public string GetPluginVersion()
		{
			return "4.0001";
		}
		public string GetPluginAuthor()
		{
			return "LethaK";
		}
		public string GetPluginWebsite()
		{
			return "http://deim.fr/bf4/";
		}
		public string GetPluginDescription()
		{
			return "";
		}

	#region Gameplay Events

		public string serverHostName;
		public string serverPort;
		public void OnPluginLoaded(string strHostName, string strPort, string strPRoConVersion)
		{
			this.serverHostName = strHostName;
			this.serverPort = strPort;
			Action.consoleWrite(String.Format("OnPluginLoaded[{0}] on {1}:{2}", this.GetPluginName(), this.serverHostName, this.serverPort));
		}

		public void OnPluginEnable()
		{
			Action.consoleWrite(String.Format("OnPluginEnable[{0}] on {1}:{2}", this.GetPluginName(), this.serverHostName, this.serverPort));

			this._PremiumBackerController = new PremiumBackerController();
		}
		public void OnPluginDisable()
		{
			Action.consoleWrite(String.Format("OnPluginDisable[{0}] on {1}:{2}", this.GetPluginName(), this.serverHostName, this.serverPort));
		}

		public void OnServerInfo(CServerInfo csiServerInfo)
		{
			Info.Gameserver = csiServerInfo;
		}

		public void OnGlobalChat(string strSpeaker, string strMessage)
		{
			this._PremiumBackerController.enforceCommandChat(strSpeaker, strMessage);
		}
		public void OnTeamChat(string strSpeaker, string strMessage, int iTeamID)
		{
			this._PremiumBackerController.enforceCommandChat(strSpeaker, strMessage);
		}
		public void OnSquadChat(string strSpeaker, string strMessage, int iTeamID, int iSquadID)
		{
			this._PremiumBackerController.enforceCommandChat(strSpeaker, strMessage);
		}
		public void OnPlayerLeft(string strSoldierName)
		{
		}
		public void OnRoundOver(int iWinningTeamID)
		{
		}
		
		public void OnPlayerKilled(Kill killInfo)
		{
			PlayerKillOccurrence killOccurrence = new PlayerKillOccurrence(killInfo.Killer, killInfo.Victim);
			this._PremiumBackerController.PunishSystem_onKill(killOccurrence);
			//this._PremiumBackerController.CheatBuster_onKill(killOccurrence);
		}


	#endregion

	#region Tool classes

		internal static class Info
		{
			public static CServerInfo Gameserver;
		}

		internal static class Action
		{
			public static void consoleWrite(string message)
			{
				this.ExecuteCommand("procon.protected.pluginconsole.write", message);
			}

			public static void killPlayer(CPlayerInfo Player)
			{
				this.ExecuteCommand("procon.protected.send", "admin.killPlayer", Player.SoldierName);
			}
			public static void killPlayer(string SoldierName)
			{
				this.ExecuteCommand("procon.protected.send", "admin.killPlayer", SoldierName);
			}
			
			public static void say(string message)
			{
				this.ExecuteCommand("procon.protected.send", "admin.say", message, "all");
			}

			public static void say(string message, string destination)
			{
				this.ExecuteCommand("procon.protected.send", "admin.say", message, destination);
			}

			public static void teamspeakText(string speaker, string message)
			{
				Action.requestJSON("http://deim.fr/proconEndPoint/teamspeak/text?gserv="+Info.Gameserver.ServerName+"&pname="+speaker+"&pmsg="+querystring);
			}

			public static void teamspeakTextAction(string action, string speaker, string message, int destinationNodeId)
			{
				Action.requestJSON("http://deim.fr/proconEndPoint/teamspeak/"+action+"?dest="+destinationNodeId+"&gserv="+Info.Gameserver.ServerName+"&pname="+speaker+"&pmsg="+querystring);
			}


			public static Hashtable requestJSON(string queryURL)
			{
				WebClient client = new WebClient();
				client.Headers.Add("Content-Type", "text/xml");
				//byte[] responseBytes = webClient.DownloadData(queryURL);
				string response = client.DownloadString(address);
				Hashtable data = (Hashtable)JSON.JsonDecode(response);
				return data;
			}


		}

		internal class PlayerKillOccurrence
		{
			public CPlayerInfo Victim;
			public CPlayerInfo Killer;
			public DateTime Time;

			public PlayerOccurrence(CPlayerInfo victimPlayer, CPlayerInfo killerPlayer)
			{
				this.Time = DateTime.Now;
				this.Victim = victimPlayer;
				this.Killer = killerPlayer;
			}

			public bool isTeamkill()
			{
				if((this.Killer.TeamID == this.Victim.TeamID) && (this.Killer.SoldierName != this.Victim.SoldierName))
				{
					return true;
				}

				return false;
			}

			public bool isSuicide()
			{
				if((this.Killer.TeamID == this.Victim.TeamID) && (this.Killer.SoldierName == this.Victim.SoldierName))
				{
					return true;
				}

				return false;
			}

			public void punish()
			{
				if(this.isTeamkill())
				{
					Action.killPlayer(this.Killer);
					Action.say(String.Format("{0} punished for teamkilling", this.Killer.SoldierName ));
				}
			}
		}

	#endregion



	#region Premium Backer System

	public PremiumBackerController _PremiumBackerController;
	internal class PremiumBackerController
	{
		public static bool levelSystem_enabled;
		public static bool levelSystem_DefaultBackerLevel;

		public Hashtable BackerLevelList;
		private Hashtable commandChatRx;

		public bool isBackerListImported;

		public PremiumBackerController()
		{

			this.isBackerListImported = false;
			this.importBackerList();
			
			// Command Chat Init
			this.commandChatRx = new Hashtable();
			this.commandChatRx.Add("!admin", @"\s*[!@/]\s*admin");
			this.commandChatRx.Add("!cheater", @"\s*[!@/]\s*admin");
			this.commandChatRx.Add("!punish", @"\s*[!@/]\s*punish");
			this.commandChatRx.Add("!punishkick", @"\s*[!@/]\s*punish");
			this.commandChatRx.Add("!punishban", @"\s*[!@/]\s*punish");
			this.commandChatRx.Add("!forgive", @"\s*[!@/]\s*forgive");
			this.commandChatRx.Add("!strike", @"\s*[!@/]\s*strike");
			this.commandChatRx.Add("!kick", @"\s*[!@/]\s*kick");
			this.commandChatRx.Add("!kill", @"\s*[!@/]\s*kill");
			this.commandChatRx.Add("!ban", @"\s*[!@/]\s*ban");

			this.commandChatRx.Add("admin", @"admin");
			this.commandChatRx.Add("cheater", @"cheater");
		}

		public void importBackerList()
		{
			if(this.levelSystem_enabled)
			{
				try
				{
					this.BackerList = Action.requestJSON('http://deim.fr/proconEndPoint/backer/import/?');

					// TODO sorting, testing;
					this.isBackerListImported = true;

				}
				catch(Exception e)
				{
					Action.consoleWrite(String.Format('^1Error[PremiumBackerController::import]^0 ^b{0}^n', e.Message));
				}
			}
			else
			{
				this.BackerList = new Hashtable();
			}
		}

		public bool isBacker(string PlayerName)
		{
			return this.BackerList.Contains(PlayerName);
		}

		public int getBackerLevel(string PlayerName)
		{
			int level = this.levelSystem_DefaultBackerLevel;
			if(this.levelSystem_enabled)
			{
				level = 0;
				if(this.isBacker(PlayerName))
				{
					level = (int)this.BackerList[PlayerName];
				}
			}
			return level;
		}

		// Enforcing things

		public void enforceCommandChat(string speakerPlayerName, string message)
		{
			if(this.commandChat_enabled)
			{
				this.chatCmd_admin(speakerPlayerName, message);
				this.chatCmd_kick(speakerPlayerName, message);
				this.chatCmd_ban(speakerPlayerName, message);
				this.chatCmd_kill(speakerPlayerName, message);
				this.chatCmd_punish(speakerPlayerName, message);
				this.chatCmd_forgive(speakerPlayerName, message);
				this.chatCmd_strike(speakerPlayerName, message);
				this.chatCmd_punishkick(speakerPlayerName, message);
				this.chatCmd_punishban(speakerPlayerName, message);
				this.chatCmd_votekick(speakerPlayerName, message);
				this.chatCmd_voteban(speakerPlayerName, message);
			}
		}

		// Chat Command List

		private void chatCmd_admin(string speakerPlayerName, string message)
		{
			bool doAction = false;
			if(Regex.Match(message, this.commandChatRx["!admin"], RegexOptions.IgnoreCase).Success) {doAction=true;}
			if(Regex.Match(message, this.commandChatRx["admin"], RegexOptions.IgnoreCase).Success) {doAction=true;}
			if(Regex.Match(message, this.commandChatRx["cheater"], RegexOptions.IgnoreCase).Success) {doAction=true;}
			if(Regex.Match(message, this.commandChatRx["!cheater"], RegexOptions.IgnoreCase).Success) {doAction=true;}
			if(doAction)
			{
				// todo stuff ...
				Action
			}
		}


		private void chatCmd_kick(string speakerPlayerName, string message)
		{
			bool doAction = false;
			if(Regex.Match(message, this.commandChatRx["!kick"], RegexOptions.IgnoreCase).Success) {doAction=true;}
			if(doAction)
			{
				// todo stuff ...
			}
		}

		private void chatCmd_ban(string speakerPlayerName, string message)
		{
			bool doAction = false;
			if(Regex.Match(message, this.commandChatRx["!ban"], RegexOptions.IgnoreCase).Success) {doAction=true;}
			if(doAction)
			{
				// todo stuff ...
			}
		}

		private void chatCmd_kill(string speakerPlayerName, string message)
		{
			bool doAction = false;
			if(Regex.Match(message, this.commandChatRx["!kill"], RegexOptions.IgnoreCase).Success) {doAction=true;}
			if(doAction)
			{
				// todo stuff ...
			}
		}

		private void chatCmd_punish(string speakerPlayerName, string message)
		{
			bool doAction = false;
			if(Regex.Match(message, this.commandChatRx["!punish"], RegexOptions.IgnoreCase).Success) {doAction=true;}
			if(doAction)
			{
				// todo stuff ...
			}
		}

		private void chatCmd_forgive(string speakerPlayerName, string message)
		{
			bool doAction = false;
			if(Regex.Match(message, this.commandChatRx["!forgive"], RegexOptions.IgnoreCase).Success) {doAction=true;}
			if(doAction)
			{
				// todo stuff ...
			}
		}

		private void chatCmd_strike(string speakerPlayerName, string message)
		{
			bool doAction = false;
			if(Regex.Match(message, this.commandChatRx["!strike"], RegexOptions.IgnoreCase).Success) {doAction=true;}
			if(doAction)
			{
				// todo stuff ...
			}
		}

		private void chatCmd_punishkick(string speakerPlayerName, string message)
		{
			bool doAction = false;
			if(Regex.Match(message, this.commandChatRx["!punishkick"], RegexOptions.IgnoreCase).Success) {doAction=true;}
			if(doAction)
			{
				// todo stuff ...
			}
		}

		private void chatCmd_punishban(string speakerPlayerName, string message)
		{
			bool doAction = false;
			if(Regex.Match(message, this.commandChatRx["!punishban"], RegexOptions.IgnoreCase).Success) {doAction=true;}
			if(doAction)
			{
				// todo stuff ...
			}
		}

		private void chatCmd_votekick(string speakerPlayerName, string message)
		{
			bool doAction = false;
			if(Regex.Match(message, this.commandChatRx["!votekick"], RegexOptions.IgnoreCase).Success) {doAction=true;}
			if(doAction)
			{
				// todo stuff ...
			}
		}

		private void chatCmd_votebank(string speakerPlayerName, string message)
		{
			bool doAction = false;
			if(Regex.Match(message, this.commandChatRx["!voteban"], RegexOptions.IgnoreCase).Success) {doAction=true;}
			if(doAction)
			{
				// todo stuff ...
			}
		}

	}

	#endregion


}
