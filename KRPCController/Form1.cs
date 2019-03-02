using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace KRPCController
{
    public partial class Form1 : Form
    {
        public TextBox LogBox { get { return logBox; } }
        public TextBox InfoBox { get { return infoBox; } }

        public Form1()
        {
            InitializeComponent();
            ConnectionInitializer.form = this;
            //ConnectionInitializer.Init();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            Time.Update();
        }

        private void timerFast_Tick(object sender, EventArgs e)
        {
            if (ConnectionInitializer.socketServer != null)
            {
                ConnectionInitializer.socketServer.Update();
            }
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            Input.OnKeyDown(e);
        }

        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            Input.OnKeyUp(e);
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            Info.paused ^= true;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            ConnectionInitializer.Init();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            ConnectionInitializer.InitSocket();
        }
    }
}
