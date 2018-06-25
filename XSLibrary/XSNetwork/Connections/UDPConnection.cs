﻿using System.Net;
using System.Net.Sockets;

namespace XSLibrary.Network.Connections
{
    public class UDPConnection: ConnectionInterface
    {
        public delegate void DataReceivedHandler(object sender, byte[] data, IPEndPoint endPoint);
        public event DataReceivedHandler DataReceivedEvent;

        public UDPConnection(IPEndPoint local) : base(new Socket(local.AddressFamily, SocketType.Dgram, ProtocolType.Udp), local)
        {
        }

        public virtual void Send(byte[] data, IPEndPoint remoteEndPoint)
        {
            m_lock.Execute(() => UnsafeSend(data, remoteEndPoint));
        }

        private void UnsafeSend(byte[] data, IPEndPoint remoteEndPoint)
        {
            if (!Disconnecting)
                ConnectionSocket.SendTo(data, remoteEndPoint);
        }

        public void HolePunching(IPEndPoint remoteEndPoint)
        {
            Send(new byte[0], remoteEndPoint);
        }

        protected override void PreReceiveSettings()
        {
            ConnectionSocket.Bind(ConnectionEndpoint);
        }

        protected override void ReceiveFromSocket()
        {
            byte[] data = new byte[MaxPacketSize];
            EndPoint source = new IPEndPoint(ConnectionEndpoint.Address, ConnectionEndpoint.Port);

            int size = ConnectionSocket.ReceiveFrom(data, ref source);

            if(IsHolePunching(size))
                return;

            RaiseReceivedEvent(TrimData(data, size), source as IPEndPoint);    
        }

        private bool IsHolePunching(int size)
        {
            return size == 0;
        }

        private void RaiseReceivedEvent(byte[] data, IPEndPoint source)
        {
            DataReceivedHandler threadCopy = DataReceivedEvent;
            if (threadCopy != null)
                threadCopy.Invoke(this, data, source);
        }
    }
}
