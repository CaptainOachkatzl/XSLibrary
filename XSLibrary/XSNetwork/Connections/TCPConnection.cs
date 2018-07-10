﻿using System.Net;
using System.Net.Sockets;

namespace XSLibrary.Network.Connections
{
    public class TCPConnection : ConnectionInterface
    {
        public TCPConnection(Socket socket) 
            : base(socket)
        {
            Local = socket.LocalEndPoint as IPEndPoint;
            Remote = socket.RemoteEndPoint as IPEndPoint;
        }

        protected override void SendSpecialized(byte[] data)
        {
            ConnectionSocket.Send(data);
        }

        protected override void PreReceiveSettings()
        {
            return;
        }

        protected override bool ReceiveSpecialized(out byte[] data, out IPEndPoint source)
        {
            data = new byte[MaxReceiveSize];
            source = Remote;

            int size = ConnectionSocket.Receive(data, MaxReceiveSize, SocketFlags.None);

            if (size <= 0)
            {
                ReceiveThread = null;
                return false;
            }

            data = TrimData(data, size);
            return true;
        }
    }
}