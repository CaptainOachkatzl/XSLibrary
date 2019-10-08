using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using XSLibrary.Utility;

namespace XSLibrary.Network.Connections
{
    public partial class TCPPacketConnection : TCPConnection
    {
        // this includes any cryptographic overhead as well so consider this while deciding its value
        public int MaxPacketReceiveSize { get; set; } = 2048;

        const byte Header_ID_Packet = 0x00;
        const byte Header_ID_KeepAlive = 0x01;
        const int Header_Size_ID = 1;
        const int Header_Size_PacketLength = 4;
        const int Header_Size_Total = Header_Size_ID + Header_Size_PacketLength;

        public override Logger Logger
        {
            get { return Parser.Logger; }
            set { Parser.Logger = value; }
        }

        PackageParser Parser = new PackageParser();

        public TCPPacketConnection(Socket socket)
            : base(socket)
        {
        }

        protected override void SendSpecialized(byte[] data)
        {
            ConnectionSocket.Send(CreateHeader(data.Length));
            ConnectionSocket.Send(data);
        }

        private byte[] CreateHeader(int length)
        {
            byte[] header = new byte[Header_Size_ID + Header_Size_PacketLength];
            byte[] lengthHeader = BitConverter.GetBytes(length);

            header[0] = Header_ID_Packet;
            Array.Copy(lengthHeader, 0, header, Header_Size_ID, Header_Size_PacketLength);
            return header;
        }

        public void SendKeepAlive()
        {
            if(SafeSend(() => ConnectionSocket.Send(new byte[] { Header_ID_KeepAlive, 0, 0, 0, 0 })))
                Logger.Log(LogLevel.Detail, "Sent keep alive.");
        }

        public void StartKeepAliveLoop(int loopInterval, int checkInterval)
        {
            DebugTools.ThreadpoolStarter("Keep alive loop", () =>
            {
                int currentWaitTime = 0;
                while (Connected)
                {
                    Thread.Sleep(checkInterval);

                    currentWaitTime += checkInterval;
                    if (currentWaitTime >= loopInterval)
                    {
                        currentWaitTime = 0;
                        SendKeepAlive();
                    }
                }
            });
        }

        protected override bool ReceiveSpecialized(out byte[] data, out EndPoint source)
        {
            data = null;
            source = Remote;

            OneShotTimer timeoutTimer = null;
            int receiveTimeout = ReceiveTimeout;   // copy to avoid race condition
            if (receiveTimeout > 0)
                timeoutTimer = new OneShotTimer(receiveTimeout * 1000);

            Parser.MaxPackageSize = MaxPacketReceiveSize;

            Func<byte[]> receive = () =>
            {
                byte[] receivedChunk;
                if (!TimedReceive(out receivedChunk, timeoutTimer))
                    throw new ConnectionException(String.Format("Receive from {0} failed!", Remote));

                return receivedChunk;
            };

            byte[] header;
            header = Parser.GetPacket(Header_Size_Total, receive);
            while (IsKeepAlive(header))   // consume keep alives
            {
                Logger.Log(LogLevel.Detail, "Received keep alive.");
                header = Parser.GetPacket(Header_Size_Total, receive);
            }

            data = Parser.GetPacket(GetPacketSize(header), receive);
            return true;
        }

        private bool IsKeepAlive(byte[] header)
        {
            return IsHeader(header) && header[0] == Header_ID_KeepAlive; 
        }

        private int GetPacketSize(byte[] header)
        {
            if (!IsHeader(header))
                throw new ConnectionException("Failed to parse header data!");

            if (header[0] != Header_ID_Packet)
                throw new ConnectionException("Packet flag in header is invalid!");

            return ReadSize(header, 1);
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

        private bool TimedReceive(out byte[] data, OneShotTimer timeoutTimer)
        {
            data = null;
            if (timeoutTimer == null) // no timeout
            {
                if (!AsyncReceive(out data, 0))
                    return false;
            }
            else                // with timeout
            {
                int timeLeft = (int)timeoutTimer.TimeLeft.TotalMilliseconds;
                if (timeLeft <= 0 || !AsyncReceive(out data, timeLeft))
                    return false;
            }

            return true;
        }

        private bool AsyncReceive(out byte[] data, int timeout)
        {
            data = new byte[ReceiveBufferSize];

            IAsyncResult receiveResult = ConnectionSocket.BeginReceive(data, 0, ReceiveBufferSize, SocketFlags.None, null, null);

            bool success = false;
            if (timeout > 0)
                success = receiveResult.AsyncWaitHandle.WaitOne(timeout, true);
            else if (timeout == 0)
                success = receiveResult.AsyncWaitHandle.WaitOne();

            if (!success)
                ConnectionSocket.Dispose();

            int size = ConnectionSocket.EndReceive(receiveResult);
            if (size <= 0)
                throw new DisconnectedGracefullyException();

            if (success)
                TrimData(ref data, size);
            else
                data = null;

            return success;
        }
    }
}
