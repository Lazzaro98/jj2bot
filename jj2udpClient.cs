using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;
using System.Threading;
using System.Timers;
namespace Jj2
{
    class jj2udpClient
    {
        //---------------------------------UDP PACKETS-----------------------------
        byte[] playerStatus = {0xD8,0x2A,0x01,0x52,0x01,0xb9,0x00,0x00,0x10,0x4f,0x89,0x97,0x04,0x3D,0x03,0x03,0x02 };

        byte[] ping = { 0xA7,0xBB,0x03,0x00,0xDD,0x17,0x04,0x00,0x32,0x34,0x20,0x20};

        byte[] query = { 0x06,0x0d,0x05,0x00};
        //---------------------------------UDP PACKETS-----------------------------


        IPAddress ip;
        int tcpport,udpport;
        EndPoint serverDest,listenPort;
        Socket jj2udpSocket = new Socket(AddressFamily.InterNetwork,SocketType.Dgram,ProtocolType.Udp);
        jj2tcpClient tcpClient;
        byte udpc=0x00;
        RichTextBox sender, chatLog, playerList;
        //constructor
        public jj2udpClient(IPAddress ip,int tcpport,int udpport,string name,RichTextBox chatLog,RichTextBox playerList,RichTextBox sender){
            this.ip = ip;
            this.tcpport = tcpport;
            this.udpport=udpport;
            serverDest = new IPEndPoint(ip, udpport);
            listenPort = new IPEndPoint(IPAddress.Any,udpport);
            this.sender = sender;
            this.chatLog = chatLog;
            this.playerList = playerList;
            jj2udpSocket.Bind(listenPort);
            sendQuery();
            tcpClient = new jj2tcpClient(ip,tcpport,name,chatLog,playerList,sender);
            startListening();
            
        }
        public void startListening() {
            byte[] paket = new byte[500];
            jj2udpSocket.BeginReceiveFrom(paket,0,paket.Length,0,ref listenPort,new AsyncCallback(onReceive),paket);
        }
        public void posalji(byte[] paket) {
            IPEndPoint ipe = new IPEndPoint(ip, tcpport);
            jj2udpSocket.SendTo(paket,paket.Length,SocketFlags.None,ipe);
        }
        public void sendPlayerStatus() {
            playerStatus[3] = udpc=Convert.ToByte(((udpc+1)%255));
            posalji(playerStatus);
        }//sendPlayerStatus()
        public void sendPing() {
            posalji(ping);
        }
        public void sendQuery() {
            posalji(query);
        }
        public void sendKeepAlive() {
            
            byte[] keepAlive1 = { 0x76,0x81,0x09,udpc};
            udpc = Convert.ToByte(((udpc + 1) % 255));
            byte[] keepAlive2 = { 0x00,0x00,0x09,udpc,0x00,0x00,0x00,0x00};
            udpc = Convert.ToByte(((udpc + 1) % 255));
            for (int i = 0; i < 4; i++)
                keepAlive2[4 + i] = tcpClient.magicNumbers[i];
            UDPchecksum(ref keepAlive2);
            posalji(keepAlive1);
            posalji(keepAlive2);
        }
        public void saljiLokaciju() {
            System.Timers.Timer aTimer = new System.Timers.Timer();
            aTimer.Elapsed += new ElapsedEventHandler(timer_Tick);
            aTimer.Interval = 2000;
            aTimer.Enabled = true;

        }
        void timer_Tick(object sender,EventArgs e) {
            sendPlayerStatus();
            
        }
        public bool connected() {
            return tcpClient.isConnected();
        }
        public void onReceive(IAsyncResult r) {
            int size = jj2udpSocket.EndReceiveFrom(r,ref listenPort);
            byte[] receivedData = new byte[size];
            receivedData = (byte[])r.AsyncState;
            checkOnGivenPacket(receivedData);
            if (connected()) startListening();
        }
        public void checkOnGivenPacket(byte[] packet) {
            byte packetID = packet[2];
            switch (packetID) { 
                case 0x02://nothing for now
                    break;
                case 0x04://pong, nothing for now
                    tcpClient.addMsg("ponged");
                    saljiLokaciju();
                    break;
                case 0x06://query reply, 17.serverNameLength
                    int serverNameSize = (int)packet[16];
                    string qserverName="";
                    for (int i = 0; i < serverNameSize; i++) qserverName += Convert.ToChar(packet[17+i]);
                    tcpClient.serv.serverName = qserverName;
                    tcpClient.addMsg("Server name:"+qserverName);
                    sendKeepAlive();
                    saljiLokaciju();
                    sendPing();
                    break;
                case 0x09:
                    byte[] packet1 = new byte[9];
                    for (int i = 0; i < 9; i++) packet1[i] = packet[i];
                    posalji(packet1);//resent only
                    break;

            }
        }
        //-----------------UDP checkSum----------------------------//
        public static void UDPchecksum(ref byte[] buffer)//3.UDP checksum
        {
            int x = 1, y = 1;
            for (int i = 2; i < buffer.Length; ++i)
            {
                x += buffer[i];
                y += x;
            }
            buffer[0] = (byte)(x % 251);
            buffer[1] = (byte)(y % 251);
        }
    }
}
