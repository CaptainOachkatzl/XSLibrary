using System;
using System.Net;
using System.Net.Sockets;
using XSLibrary.Utility;

namespace XSLibrary.Network.Connections
{
    public class TCPPacketConnection : TCPConnection
    {
        // this includes any cryptographic overhead as well so consider this while deciding its value
        public int MaxPacketReceiveSize { get; set; } = 2048;

        const int Header_Size_PacketLength = 4;
        const int Header_Size_Total = Header_Size_PacketLength;

        public override Logger Logger
        {
            get { return Parser.Logger; }
            set { Parser.Logger = value; }
        }

        PacketParser Parser = new PacketParser();
        OneShotTimer TimeoutTimer = null;

        public TCPPacketConnection(Socket socket)
            : base(socket)
        {
        }

        protected override void SendSpecialized(byte[] data)
        {
            ConnectionSocket.Send(CreateHeader(data.Length));
            ConnectionSocket.Send(data);
        }

        private byte[] CreateHeader(int contentSize)
        {
            byte[] header = new byte[Header_Size_Total];
            byte[] contentSizeBytes = BitConverter.GetBytes(contentSize);

            Array.Copy(contentSizeBytes, 0, header, 0, Header_Size_PacketLength);
            return header;
        }

        protected override bool ReceiveSpecialized(out byte[] data, out EndPoint source)
        {
            data = null;
            source = Remote;

            StartTimeoutTimer();

            try
            {
                byte[] header = GetPacketHeader();

                int packetSize = GetPacketSize(header);
                if (packetSize > MaxPacketReceiveSize)
                    throw new ConnectionException(String.Format("Packet size exceeded from {0}!", Remote));

                data = Parser.GetPacket(packetSize, TimedReceive);
            }
            catch (PacketParser.PacketException ex) { throw new ConnectionException(ex.Message); }

            return true;
        }

        private void StartTimeoutTimer()
        {
            TimeoutTimer = null;
            int receiveTimeout = ReceiveTimeout;   // copy to avoid race condition
            if (receiveTimeout > 0)
                TimeoutTimer = new OneShotTimer(receiveTimeout * 1000);
        }

        private byte[] GetPacketHeader()
        {
            return Parser.GetPacket(Header_Size_Total, TimedReceive);
        }

        private int GetPacketSize(byte[] header)
        {
            if (!IsHeader(header))
                throw new ConnectionException("Failed to parse header data!");

            return ReadSize(header, 0);
        }

        private bool IsHeader(byte[] data)
        {
            return data.Length == Header_Size_Total;
        }

        private int ReadSize(byte[] header, int offset)
        {
            return header[offset]
                + (header[offset + 1] << 8)
                + (header[offset + 2] << 16)
                + (header[offset + 3] << 24);
        }

        private byte[] TimedReceive()
        {
            int size;
            byte[] data = null;
            if (TimeoutTimer != null)   // with timeout
            {
                int timeLeft = (int)TimeoutTimer.TimeLeft.TotalMilliseconds;
                if (timeLeft <= 0)
                    throw new ConnectionException(String.Format("Timeout while receiving from {0}!", Remote));

                size = AsyncReceive(out data, timeLeft);
            }
            else                        // no timeout
                size = AsyncReceive(out data, 0);

            if(size == 0)
                throw new DisconnectedGracefullyException();
            else if(size < 0)
                throw new ConnectionException(String.Format("Receive from {0} failed!", Remote));

            return data;
        }

        private int AsyncReceive(out byte[] data, int timeout)
        {
            data = new byte[ReceiveBufferSize];

            IAsyncResult receiveResult = ConnectionSocket.BeginReceive(data, 0, ReceiveBufferSize, SocketFlags.None, null, null);

            bool success = false;
            if (timeout > 0)
                success = receiveResult.AsyncWaitHandle.WaitOne(timeout, true);
            else if (timeout == 0)
                success = receiveResult.AsyncWaitHandle.WaitOne();

            if (!success)
            {
                data = null;
                ConnectionSocket.Dispose();
                return -1;
            }

            int size = ConnectionSocket.EndReceive(receiveResult);

            if (size > 0)
                TrimData(ref data, size);
            else
                data = null;

            return size;
        }
    }
}
