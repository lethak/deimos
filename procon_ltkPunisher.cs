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

namespace PRoConEvents
{

    public class lethakPunisher : PRoConPluginAPI, IPRoConPluginInterface
    {
        private bool plugin_enabled;
        private List<PlayerOccurrence> occ;

        public lethakPunisher(){
            this.occ = new List<PlayerOccurrence>();
        }

        public string listOcc()
        {
            string saida = "";
            foreach (PlayerOccurrence po in occ)
            {
                saida += po.kPlayer + "-" + po.vPlayer + "|";
            }
            return saida;
        }

        public void OnPlayerKilled(Kill killInfo)
        {
            this.cleanOccurrence();

            CPlayerInfo killer = killInfo.Killer;
            CPlayerInfo victim = killInfo.Victim;

            if ((killer.TeamID == victim.TeamID) && (killer.SoldierName != victim.SoldierName))
            {
                this.occ.Add(new PlayerOccurrence(victim.SoldierName, killer.SoldierName));
                //this.ExecuteCommand("procon.protected.send", "admin.say", String.Format("{0} has TK you, you can choose !forgive or !punish", killer.SoldierName), victim.SoldierName);
                //this.ExecuteCommand("procon.protected.send", "admin.say", String.Format("{0} has TK {1}, {1} can !forgive or !punish", killer.SoldierName, victim.SoldierName), "all");
                //this.ConsoleWrite(killer.SoldierName + " > TK > " + victim.SoldierName);
                //this.ConsoleWrite(listOcc());
            }          
        }

        private void TextAction(string strSpeaker, string strMessage)
        {
            if (this.isPunish(strMessage))
            {
                List<string> killers = this.hasOccurrence(strSpeaker);
                foreach (string killer in killers)
                {
                    //this.ExecuteCommand("procon.protected.events.write", "Plugins", "PluginAction", strSpeaker + " has punished " + killer, "lethakPunisher");
                    this.KillPlayer(killer, strSpeaker + " has punished " + killer);
                    this.cleanOccurrence(killer, strSpeaker);
                }
            }

            if (this.isForgive(strMessage))
            {
                List<string> killers = this.hasOccurrence(strSpeaker);
                foreach (string killer in killers)
                {
                    //this.ExecuteCommand("procon.protected.send", "admin.say", strSpeaker + " has forgiven " + killer, "all");
                    //this.ExecuteCommand("procon.protected.events.write", "Plugins", "PluginAction", strSpeaker + " has forgiven " + killer, "lethakPunisher");
                    this.cleanOccurrence(killer, strSpeaker);
                }
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
            this.cleanOccurrence(strSoldierName);
        }
        public void OnRoundOver(int iWinningTeamID)
        {
            this.occ.Clear();
        }

        private bool isPunish(string msg)
        {
            if (Regex.Match(msg, @"\s*[!@/]\s*punish", RegexOptions.IgnoreCase).Success)
                return true;

            return false;                
        }
        private bool isForgive(string msg)
        {
            if (Regex.Match(msg, @"\s*[!@/]\s*forgive", RegexOptions.IgnoreCase).Success)
                return true;

            return false;
        }

        private void ConsoleWrite(string msg)
        {
            string prefix = "[^b" + GetPluginName() + "^n] ";
            this.ExecuteCommand("procon.protected.pluginconsole.write", prefix + msg);
        }
        private void KillPlayer(string SoldierName, string strMessage)
        {
            //this.ExecuteCommand("procon.protected.send", "admin.say", strMessage, "all");
            this.ExecuteCommand("procon.protected.send", "admin.killPlayer", SoldierName);
        }

        private List<string> hasOccurrence(string vPlayer)
        {
            List<string> saida = new List<string>();
            foreach (PlayerOccurrence po in this.occ)
            {
                if (po.vPlayer == vPlayer)
                {
                    saida.Add(po.kPlayer);
                }
            }
            return saida;
        }
        private void cleanOccurrence(string SoldierName)
        {
            foreach (PlayerOccurrence po in this.occ)
            {
                if (po.kPlayer == SoldierName || po.vPlayer == SoldierName)
                {
                    this.occ.Remove(po);
                }
            }
        }
        private void cleanOccurrence(string kPlayer, string vPlayer)
        {
            foreach (PlayerOccurrence po in this.occ)
            {
                if (po.kPlayer == kPlayer && po.vPlayer == vPlayer)
                {
                    this.occ.Remove(po);
                }
            }
        }
        private void cleanOccurrence()
        {
            foreach (PlayerOccurrence po in this.occ)
            {
                if (po.Time.AddMinutes(2) < DateTime.Now)
                {
                    this.occ.Remove(po);
                }
            }
        }

        public void OnPluginEnable()
        {
            this.plugin_enabled = true;
			ConsoleWrite("Enabled");
            this.ExecuteCommand("procon.protected.send", "admin.say", "Plugin info: Alert !punish enabled", "all");
        }
        public void OnPluginDisable()
        {
            this.plugin_enabled = false;
			ConsoleWrite("Disabled");
            //this.ExecuteCommand("procon.protected.send", "admin.say", "Plugin info: Alert !punish Disabled", "all");
        }

        public string GetPluginName()
        {
            return "lethakPunisher";
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
            return "http://bf.deim.fr";
        }
        public string GetPluginDescription()
        {
            return "";
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

        internal class PlayerOccurrence
        {
            public PlayerOccurrence(string vPlayer, string kPlayer)
            {
                this.vPlayer = vPlayer;
                this.kPlayer = kPlayer;
                this.Time = DateTime.Now;
            }

            private string _vPlayer;
            public string vPlayer
            {
                get
                {
                    return this._vPlayer;
                }
                set
                {
                    this._vPlayer = value;
                }
            }

            private string _kPlayer;
            public string kPlayer
            {
                get
                {
                    return this._kPlayer;
                }
                set
                {
                    this._kPlayer = value;
                }
            }

            private DateTime _Time;
            public DateTime Time
            {
                get
                {
                    return this._Time;
                }
                set
                {
                    this._Time = value;
                }
            }
        }

    }

}
