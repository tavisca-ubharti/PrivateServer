using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PrivateChatServer
{
    public static class Server
    {
        private static Dictionary<Socket, string> _connectionList = new Dictionary<Socket, string>();

        public static void StartServer()
        {

            IPHostEntry ipHost = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ipAddress = ipHost.AddressList[1];
            Console.WriteLine("Server Ip Address : " + ipAddress);
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 8080);
            Socket connectionListener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                connectionListener.Bind(localEndPoint);
                connectionListener.Listen(100);
                Console.WriteLine("Waiting connection ... ");
                while (true)
                {
                    var clientHandler = connectionListener.Accept();
                    var userName = string.Empty;
                    while (userName.Trim().Equals(string.Empty))
                    {
                        if (_connectionList.ContainsKey(clientHandler))
                            break;
                        clientHandler.Send(Encoding.ASCII.GetBytes("Enter your name"));
                        var message = new byte[1024];
                        var numByte = clientHandler.Receive(message);
                        userName = Encoding.ASCII.GetString(message, 0, numByte);
                        if (!userName.Trim().Equals(string.Empty))
                        {
                            _connectionList[clientHandler] = userName;
                            clientHandler.Send(Encoding.ASCII.GetBytes("You have logged in..."));
                        }

                    }
                    Task.Factory.StartNew(() => RecieveMessage(clientHandler));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public static void RecieveMessage(Socket clientSocket)
        {
            if (clientSocket == null)
                return;
            var messageRecieved = new Byte[1024];
            var messageToBeSend = string.Empty;
            while (true)
            {
                int numByte = clientSocket.Receive(messageRecieved);
                messageToBeSend = Encoding.ASCII.GetString(messageRecieved,0,numByte);
                GiveResponse(clientSocket, messageToBeSend);
                
                
            }
        }

        private static void GiveResponse(Socket clientSocket, string messageToBeSend)
        {
            switch (messageToBeSend)
            {
                case "ACTIVE":
                    GetActiveUsers(clientSocket);
                    break;
                case "LOGOUT":
                    LogOutUser(clientSocket);
                    break;
                default:
                    SendMessage(clientSocket, messageToBeSend);
                    break;
            }
        }

        private static void SendMessage(Socket clientSocket, string messageToBeSend)
        {
            var message = "Please send in proper format";
            var userName = string.Empty;
            if (IsValidMessage(messageToBeSend))
            {
                message = messageToBeSend.Substring(messageToBeSend.IndexOf(']') + 1);
                userName = messageToBeSend.Substring(1, messageToBeSend.IndexOf(']') - 1);
                SendMessageToUser(userName, message);
            }
            else
                clientSocket.Send(Encoding.ASCII.GetBytes(message));
        }

        private static void SendMessageToUser(string userName, string message)
        {
            Socket removeSocket = null;
            foreach (var client in _connectionList.Keys)
            {
                if (_connectionList[client].Equals(userName))
                {
                    try
                    {
                        client.Send(Encoding.ASCII.GetBytes(message));
                    }
                    catch (Exception)
                    {
                        removeSocket = client;
                    }
                }
            }
            if (removeSocket != null)
            {
                _connectionList.Remove(removeSocket);
                removeSocket = null;
            }
        }

        private static bool IsValidMessage(string messageToBeSend)
        {
            return Regex.IsMatch(messageToBeSend, @"^\[\w+\]\w+$");
        }

        private static void LogOutUser(Socket clientSocket)
        {
            if (clientSocket != null)
            {
                var message = "you have been logged out...";
                clientSocket.Send(Encoding.ASCII.GetBytes(message));
                clientSocket.Send(Encoding.ASCII.GetBytes("bye"));
                _connectionList.Remove(clientSocket);
            }
        }

        private static void GetActiveUsers(Socket clientSocket)
        {
            foreach (var client in _connectionList.Keys)
            {
                var userName = _connectionList[client];
                clientSocket.Send(Encoding.ASCII.GetBytes(userName));
            }
        }
    }
}