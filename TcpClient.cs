using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

[CLSCompliant(false)]
public class TcpClient : MonoBehaviour {

    Socket clientSocket;
    IPEndPoint ipEnd;
    byte[] dataBuffer = new byte[4096];
    Thread connectThread;

    private int port = 5566;

    //init
    public void InitSocket(string ipAddr) {
        IPAddress ip = IPAddress.Parse(ipAddr);
        ipEnd = new IPEndPoint(ip, port);
        //create socket instance
        clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        clientSocket.BeginConnect(ipEnd, new AsyncCallback(connectCallback), null);
    }

    private void connectCallback(IAsyncResult async) {
        try {
            clientSocket.EndConnect(async);
            waitData(clientSocket);
        }
        catch (Exception e) {
            Debug.Log(e.ToString());
        }
    }

    private AsyncCallback socketCallBack;
    private void waitData(Socket socket) {
        try {
            if (socketCallBack == null)
                socketCallBack = new AsyncCallback(onDataReceive);

            socket.BeginReceive(dataBuffer, 0, dataBuffer.Length, SocketFlags.None, socketCallBack, socket);
        }
        catch (Exception e) {
            Debug.Log(e.ToString());
        }
    }

    private void onDataReceive(IAsyncResult async) {
        try {
            Socket client = (Socket)async.AsyncState;
            client.EndReceive(async);
            dataHandle(dataBuffer);
            //complete get data, wait next data
            waitData(clientSocket);
        }
        catch (Exception e) {
            Debug.Log(e.ToString());
        }
    }

    //////////   custom area /////////////////

    //send data to server
    public void SocketSend(byte[] sendMsg) {
        clientSocket.BeginSend(sendMsg, 0, sendMsg.Length, 0, new AsyncCallback(sendCallback), null);
    }

    private void sendCallback(IAsyncResult async) {
        try {
            int bytesSent = clientSocket.EndSend(async);
            Debug.Log("Sent" + bytesSent.ToString() +  "bytes to server.");
        }
        catch(Exception e) {
            Debug.Log(e.ToString());
        }
    }

    //all data get from server would be handled here
    private static void dataHandle(byte[] data) {
        //message from server
        NetMgr.OnMsgRcv(data, true);
    }

    //handle exception
    void socketErrHandle(SocketException e) {
        Debug.Log("error" + e);
        MainMgr.inst.panelWaitingList.Enqueue("NetErrorPanel");
        //UIMgr.inst.generatePanel("NetErrorPanel");
    }


    //public UnityEngine.UI.Text txt = null;
    //public UnityEngine.UI.InputField input = null;
    //string str = "";

    //public static byte[] Trans2byte<T>(T data) {
    //    byte[] dataBytes;
    //    using (MemoryStream ms = new MemoryStream()) {
    //        BinaryFormatter bf1 = new BinaryFormatter();
    //        bf1.Serialize(ms, data);
    //        dataBytes = ms.ToArray();
    //    }
    //    return dataBytes;
    //}

    //void Update() {
    //    txt.text = str;
    //}

    //void Start() {
    //    InitSocket("ip");

    //}
    //trigger by button
    //public void TESTSEND() {
    //    Debug.Log("send" + input.text);
    //    SocketSend(Trans2byte(input.text));
    //}

    //////////   custom area /////////////////

    private void SocketQuit() {
        if (clientSocket != null)
            clientSocket.Close();
        print("diconnect client");
    }

    void Awake() {
        //set persist node
        DontDestroyOnLoad(this);
    }

    //程式退出則關閉連線
    void OnApplicationQuit() {
        SocketQuit();
    }
}