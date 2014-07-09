using DarkMultiPlayerCommon;
using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;

namespace DarkMultiPlayerServer
{    
    public class ClientObject
    {
        public bool authenticated;
        public string playerName = "Unknown";
        public bool isBanned;
        public IPAddress ipAddress;
        public Guid GUID = Guid.Empty;
        //subspace tracking
        public int subspace = -1;
        public float subspaceRate = 1f;
        //vessel tracking
        public string activeVessel = "";
        //connection
        public string endpoint;
        public TcpClient connection;
        //Send buffer
        public long lastSendTime;
        public bool isSendingToClient;
        public Queue<ServerMessage> sendMessageQueueHigh = new Queue<ServerMessage>();
        public Queue<ServerMessage> sendMessageQueueSplit = new Queue<ServerMessage>();
        public Queue<ServerMessage> sendMessageQueueLow = new Queue<ServerMessage>();
        public long lastReceiveTime;
        public bool disconnectClient;
        //Receive buffer
        public bool isReceivingMessage;
        public int receiveMessageBytesLeft;
        public ClientMessage receiveMessage;
        //Receive split buffer
        public bool isReceivingSplitMessage;
        public int receiveSplitMessageBytesLeft;
        public ClientMessage receiveSplitMessage;
        //State tracking
        public ConnectionStatus connectionStatus;
        public PlayerStatus playerStatus;
        public float[] playerColor;
        //Send lock
        public object sendLock = new object();
        public object queueLock = new object();
        public object disconnectLock = new object();
    }
}

