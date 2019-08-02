using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;

namespace Jj2
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
       // jj2tcpClient tcpClient;
        jj2udpClient udpClient;
        private void button1_Click(object sender, EventArgs e)
        {
            TextBox.CheckForIllegalCrossThreadCalls = false;
           // udpClient = new jj2udpClient(IPAddress.Parse("127.0.0.1"),56979);
            IPAddress ip = IPAddress.Parse("127.0.0.1");
            int tcpport = 10052,udpport=56326;
            udpClient = new jj2udpClient(ip,tcpport,udpport,"L",richTextBox1,richTextBox2,richTextBox3);
           // tcpClient = new jj2tcpClient(ip,port,"L",richTextBox1,richTextBox2,richTextBox3);
           // tcpClient.Join();
            
          
        }//56979

     
    }
}
