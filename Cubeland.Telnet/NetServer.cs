using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Cubeland.Telnet {
    public class NetServer {

        private int ListenPort = 4444;
        Socket Listener;
        AutoResetEvent Runner = new AutoResetEvent(false);
        ArgsPool Pool = new ArgsPool();
        Func<Int64> GetClientID;
        Action<NetConnection> AddClient;
        Action<NetConnection> RemoveClient;
        private Dictionary<Int64, SocketAsyncEventArgs> Senders;
        private Dictionary<Int64, SocketAsyncEventArgs> Receivers;
        private Action<Object> RoutePayloadsFromTelnet;

        // TODO: This should be a standard object pool, once I figure out how to deal
        // with the fact that returning an asynceventarg that still has pending I/O
        // blows up the app. The last version used two queues, one to generate and one
        // to return, and only popped off the return queue if the SAEA was in a closed
        // or errored state. For now, we'll just stub in a skeleton class.
        private class ArgsPool {

            public SocketAsyncEventArgs Get() {
                return(new SocketAsyncEventArgs());
            }
            public void Return(SocketAsyncEventArgs args) {
                args.AcceptSocket = null;
                args.UserToken = null;
                args = null;
            }
        }


        public NetServer(int sock, Func<Int64> GetClientIDAct, Action<NetConnection> AddClientAct, Action<NetConnection> RemoveClientAct, Action<Object> RouteDataAct) {
            Logger.Log(" Initializing net.");
            ListenPort = sock;
            AddClient = AddClientAct;
            RemoveClient = RemoveClientAct;
            RoutePayloadsFromTelnet = RouteDataAct;
            GetClientID = GetClientIDAct;
            Senders = new Dictionary<long, SocketAsyncEventArgs>();
            Receivers = new Dictionary<long, SocketAsyncEventArgs>();

            StartListener(sock);
        }

        private void StartListener(int port) {
            Logger.Log(" Starting listener.");

            IPEndPoint iep = new IPEndPoint(0, port);
            Listener = new Socket(iep.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            if (iep.AddressFamily == AddressFamily.InterNetworkV6) {
                Listener.SetSocketOption(SocketOptionLevel.IPv6, (SocketOptionName)27, false);
                Listener.Bind(new IPEndPoint(IPAddress.IPv6Any, port));
            } else {
                Listener.Bind(iep);
            }
            Listener.Listen(100);
            DoAccept(null);

            Logger.Log(" Server online.");

            Runner.WaitOne();
        }

        private void DoAccept(SocketAsyncEventArgs args) {
            // create a new SAEA if null
            if (args == null) {
                // we're creating our first SAEA
                args = Pool.Get();
                args.Completed += new EventHandler<SocketAsyncEventArgs>(CompleteAccept);
                Logger.Log(" Listening for events on port " + ListenPort);
            } else {
                // otherwise, wipe out the AcceptSocket on the assumption we're using a 
                // reclaimed object from the object pool
                args.AcceptSocket = null;
            }
            // if an action completes synchronously, IT WON'T RAISE AN EVENT! Thus,
            // handle it explicitly by calling the HandleAccept() method.
                bool WillRaiseEvent = Listener.AcceptAsync(args);
                if (!WillRaiseEvent) {
                    HandleAccept(args);
                }                       
        }

        private void CompleteAccept(object sender, SocketAsyncEventArgs args) {
            HandleAccept(args);
        }

        private void HandleAccept(SocketAsyncEventArgs args) {
            // we need to create two SAEAs: one to receive data and one to send.
            // We create a NetConnection and set their .UserTokens to it, then
            // connect their Completed events to NetConnection.Send() and
            // .Receive(). Finally we stick them in the Sender and Receiver 
            // dictionaries.

            Int64 ClientID = GetClientID();
            SocketAsyncEventArgs sender = Pool.Get();
            SocketAsyncEventArgs receiver = Pool.Get();
            NetConnection usernc = new NetConnection(ClientID, args.AcceptSocket, sender, receiver, EndSession, RoutePayloadsFromTelnet);
            sender.UserToken = usernc;
            receiver.UserToken = usernc;

            receiver.SetBuffer(usernc.ReceptorBuffer, 0, usernc.ReceptorBuffer.Length);
            sender.SetBuffer(usernc.SendBuffer, 0, 0);

            sender.Completed += usernc.Send;
            receiver.Completed += usernc.Receive;
            Senders.Add(ClientID, sender);
            Receivers.Add(ClientID, receiver);
            AddClient(usernc);

            Logger.Log("Accepted client " + ClientID);

            // Set receptor into receive mode
            if (args.AcceptSocket.Connected) {
                bool WillRaiseEvent = args.AcceptSocket.ReceiveAsync(receiver);
                if (!WillRaiseEvent) {
                    ((NetConnection)receiver.UserToken).Receive(new object(), receiver);
                }
                WillRaiseEvent = args.AcceptSocket.SendAsync(sender);
                if (!WillRaiseEvent) {
                    ((NetConnection)sender.UserToken).Send(new object(), sender);
                }
            }

            usernc.Negotiate();

            // go back to DoAccept and wait for the next connection to arrive
            DoAccept(args);
        }

        private void HandleClose(SocketAsyncEventArgs args) {
        }

        public void EndSession(Int64 client) {
            Logger.Log("Killing session " + client + " by NetConnection request.");
            SocketAsyncEventArgs sender = Senders[client];
            SocketAsyncEventArgs receiver = Receivers[client];
            NetConnection nc = (NetConnection)(receiver.UserToken);
            if (receiver != null) {
                Socket s = ((NetConnection)receiver.UserToken).AcceptSocket;
                try {
                    s.Shutdown(SocketShutdown.Both);
                } catch (Exception e) {
                    Logger.Log("Tried to shutdown a dead socket with client ID " + client);
                }
                s.Close();
                receiver.Completed += null;
                receiver.UserToken = null;
                receiver.AcceptSocket = null;
                Receivers.Remove(client);
                
            }
            if (sender != null) {
                sender.Completed += null;
                sender.UserToken = null;
                sender.AcceptSocket = null;
                Senders.Remove(client);
            }
            RemoveClient(nc);


        }

    }
}
