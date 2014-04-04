using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cubeland.Telnet;
using System.Threading;
using System.Text.RegularExpressions;

namespace TelnetTest {
    class Server {
        String ConfigFile;
        private Int64 TotalClients;
        private Int64 NextClientID;
        int PortNum = 9999;
        bool IsRunning = false;
        Thread NetThread;
        Dictionary<Int64, NetConnection> Conns = new Dictionary<long, NetConnection>();
        Dictionary<String, System.Timers.Timer> Timers = new Dictionary<string, System.Timers.Timer>();
        Dictionary<String, String> ConfigFiles = new Dictionary<string, string>();   

        public Int64 GetNextClientID() {
            Interlocked.Increment(ref NextClientID);
            TotalClients++;
            return (NextClientID);
        }

        public void AddClient(NetConnection nc) {
            Conns.Add(nc.GetID(), nc);
        }

        public void RemoveClient(NetConnection nc) {
            try {
                Conns.Remove(nc.GetID());
                Logger.Log("SrvMgr: Removed " + nc.GetID());
            } catch (Exception e) {
                Logger.Log("SrvMgr: Failed to remove " + nc.GetID() + " (Already removed from collection?)");
            }
        }

        public void RouteCommand(Object payload) {
            NetPayload[] Payload = payload as NetPayload[];
            if (Payload != null) {
                String rets = "";
                String cmd = "";
                foreach (NetPayload np in Payload) {
                    if (np.Type == PayloadType.BYTE_ARRAY) {
                        cmd = ASCIIEncoding.UTF8.GetString(np.ByteString);
                        cmd = Regex.Replace(cmd, @"^\s+", "");
                        cmd = Regex.Replace(cmd, @"\s+$", "");
                        cmd = Regex.Replace(cmd, @"\s+", " ");
                    }
                    if (cmd != null && cmd != "" && cmd != " ") {
                        // Process the command
                        rets = "";
                        if (rets == "") {
                            rets = System.Environment.NewLine + "<@red@><@bright@>You sent the command: <@off@>\'" + cmd + "\'";
                        } else {
                            rets = System.Environment.NewLine + rets;
                        }
                    }
                    Conns[np.NetClientID].SendMsg(0, TelnetDecorator.Decorate(TelnetDecorator.Wrap(rets, Conns[np.NetClientID].ScreenX)) + System.Environment.NewLine + ">");
                }
            } else {
                Console.WriteLine("Payload came in as type " + payload.GetType().Name.ToString() + "!");
            }
        }

       public void Start() {
            int PortNum = 9999;
            

            NetServer server = new NetServer(PortNum, GetNextClientID, AddClient, RemoveClient, RouteCommand); 
        }
    }
}
