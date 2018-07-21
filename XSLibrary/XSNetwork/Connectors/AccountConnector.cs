﻿using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using XSLibrary.Cryptography.ConnectionCryptos;
using XSLibrary.Network.Connections;

namespace XSLibrary.Network.Connectors
{
    public class AccountConnector : Connector<TCPPacketConnection>
    {
        public const string HANDSHAKE_FAILED = "Handshake failed.";
        public const string AUTHENTICATION_FAILED = "Authentication failed.";

        public CryptoType Crypto { get; set; } = CryptoType.NoCrypto;
        public int TimeoutCryptoHandshake { get; set; } = 5000;
        public int TimeoutAuthentication { get; set; } = 5000;
        public string Login { get; set; } = "";
        public string SuccessResponse { get; set; } = "+";

        protected override TCPPacketConnection InitializeConnection(Socket connectedSocket)
        {
            TCPPacketConnection connection = new TCPPacketConnection(connectedSocket);

            if (!connection.InitializeCrypto(CryptoFactory.CreateCrypto(Crypto, true), TimeoutCryptoHandshake))
                throw new Exception(HANDSHAKE_FAILED);

            connection.Send(Encoding.ASCII.GetBytes(Login) , TimeoutAuthentication);
            if (!connection.Receive(out byte[] data, out EndPoint source, TimeoutAuthentication) || Encoding.ASCII.GetString(data) != SuccessResponse)
                throw new Exception(AUTHENTICATION_FAILED);

            return connection;
        }
    }
}