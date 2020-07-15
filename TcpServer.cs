using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System;

[CLSCompliant(false)]
public class TcpServer : MonoBehaviour {
    Socket serverSocket;

    public class SocketPack {
        public Socket currentSocket;
        public byte[] dataBuffer = new byte[4096];
    }

    private int port = 5566;
    private const int MAX_CLIENTS = 20;
    private Socket[] socketChannel = new Socket[MAX_CLIENTS];

    //init
    void InitSocket() {
        //listen any IP
        IPEndPoint ipEnd = new IPEndPoint(IPAddress.Any, port);
        //create socket instance
        serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        serverSocket.Bind(ipEnd);

        //開始偵聽 "同時要求連線最大值" 3
        serverSocket.Listen(3);
        serverSocket.BeginAccept(new AsyncCallback(OnClientConnect), null);
    }

    public class NoSocketAvailableException : Exception {
        new public string Message = "NoSocketAvailableException";
    }

    // find avaliable channel
    private int FindEmptyChannel() {
        for (int i = 0; i < MAX_CLIENTS; i++) {
            if (socketChannel[i] == null || !socketChannel[i].Connected)
                return i;
        }
        return -1;
    }

    private void OnClientConnect(IAsyncResult async) {
        try {
            int emptyChannelIndex = -1;
            if (serverSocket == null)
                return;

            Socket tmpSocket = serverSocket.EndAccept(async);
            EndPoint remoteEndPoint = tmpSocket.RemoteEndPoint;
            //get avaliable channel
            emptyChannelIndex = FindEmptyChannel();
            if (emptyChannelIndex == -1)
                throw new NoSocketAvailableException();
            //handle tmp socket
            socketChannel[emptyChannelIndex] = tmpSocket;
            tmpSocket = null;

            waitData(socketChannel[emptyChannelIndex]);
        }
        catch (Exception e) {
            Debug.Log(e.ToString());
        }
        finally {
            //release lock
            serverSocket.BeginAccept(new AsyncCallback(OnClientConnect), null);
        }
    }

    private static AsyncCallback socketCallBack;
    private static void waitData(Socket socket) {
        try {
            if (socketCallBack == null)
                socketCallBack = new AsyncCallback(onDataReceive);

            SocketPack socketPack = new SocketPack();
            socketPack.currentSocket = socket;
            socket.BeginReceive(socketPack.dataBuffer, 0, socketPack.dataBuffer.Length, SocketFlags.None, socketCallBack, socketPack);
        }
        catch (Exception e) {
            Debug.Log(e.ToString());
        }
    }

    private static void onDataReceive(IAsyncResult async) {
        try {
            SocketPack socketData = (SocketPack)async.AsyncState;
            socketData.currentSocket.EndReceive(async);
            dataHandle(socketData.dataBuffer);
            //complete get data, wait next
            waitData(socketData.currentSocket);
        }
        catch (Exception e) {
            Debug.Log(e.ToString());
        }
    }

    //////////   custom area /////////////////

    //all data get from client would be handled here
    private static void dataHandle(byte[] data) {
        //message from client
        NetMgr.OnMsgRcv(data, false);
    }

    //send data to all client
    public void SocketSend(byte[] sendMsg) {
        for (int i = 0; i < MAX_CLIENTS; i++) {
            if (socketChannel[i] != null && socketChannel[i].Connected) {
                socketChannel[i].Send(sendMsg, sendMsg.Length, SocketFlags.None);
            }
        }
    }

    //////////   custom area /////////////////
    
    //連線關閉
    private void SocketQuit() {
        //close client
        for (int i = 0; i < MAX_CLIENTS; i++) {
            if (socketChannel[i] != null)
                socketChannel[i].Close();
        }
        //close server
        if (serverSocket != null)
            serverSocket.Close();
        print("diconnect server");
    }

    void Awake() {
        //set persist node
        DontDestroyOnLoad(this);
    }

    void Start() {
        //init server
        InitSocket();
    }

    void OnApplicationQuit() {
        SocketQuit();
    }
}
