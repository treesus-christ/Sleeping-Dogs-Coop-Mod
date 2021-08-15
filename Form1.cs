using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Memory;

namespace Sleeping_Dogs_Mods
{
    public partial class Form1 : Form
    {
        Mem connection = new Mem();

        //"SDHDShip.exe"+02409CE0 + 3C4
        string unlimited_money_address = "SDHDShip.exe+0x02409CE0,0x3C4";

        //
        string unlimited_health_address = "SDHDShip.exe+0x02087B78,0x14";

        string x_position_address = "SDHDShip.exe+0x021738A8,0x220";
        string y_position_address = "SDHDShip.exe+0x021738A8,0x228";
        string z_position_address = "SDHDShip.exe+0x021738A8,0x224";

        //string nine_mm_address = "SDHDShip.exe+0x02087BF8,0x94";
        string nine_mm_address = "SDHDShip.exe+0x02087BF8,0x94";

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            int PID = connection.GetProcIdFromName("sdhdship");
            if (PID > 0)
            {
                connection.OpenProcess(PID);
                textBox1.KeyPress += new System.Windows.Forms.KeyPressEventHandler(CheckEnterKeyPress1);
                textBox2.KeyPress += new System.Windows.Forms.KeyPressEventHandler(CheckEnterKeyPress2);
                textBox3.KeyPress += new System.Windows.Forms.KeyPressEventHandler(CheckEnterKeyPress3);
                timer1.Start();
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            {
                connection.FreezeValue(unlimited_money_address, "int", "10000000");
            }

            if (checkBox2.Checked)
            {
                connection.FreezeValue(unlimited_health_address, "float", "200");
            }

            //X
            if (checkBox3.Checked)
            {
                textBox1.ReadOnly = false;
            }
            else
            {
                textBox1.Text = connection.ReadFloat(x_position_address).ToString();
                textBox1.ReadOnly = true;
            }

            //Y
            if (checkBox4.Checked)
            {
                textBox2.ReadOnly = false;
            }
            else
            {
                textBox2.Text = connection.ReadFloat(y_position_address).ToString();
                textBox2.ReadOnly = true;
            }

            //Z
            if (checkBox5.Checked)
            {
                textBox3.ReadOnly = false;
            }
            else
            {
                textBox3.Text = connection.ReadFloat(z_position_address).ToString();
                textBox3.ReadOnly = true;
            }

            if (checkBox6.Checked)
            {
                connection.FreezeValue(nine_mm_address, "int", "999");
            }
            connection.UnfreezeValue(unlimited_money_address);
            connection.UnfreezeValue(unlimited_health_address);
            connection.UnfreezeValue(x_position_address);
            connection.UnfreezeValue(y_position_address);
            connection.UnfreezeValue(z_position_address);
            connection.UnfreezeValue(z_position_address);
            connection.UnfreezeValue(nine_mm_address);

            //if (checkBox2.Checked)
            //{
            //    unlimited_money.FreezeValue(unlimited_health_address, "int", "100");
            //}
            //else
            //{
            //    unlimited_money.UnfreezeValue(unlimited_health_address);
            //}
        }

        private void CheckEnterKeyPress1(object sender, System.Windows.Forms.KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Return)
            {
                connection.FreezeValue(x_position_address, "float",textBox1.Text);
            }
        }

        private void CheckEnterKeyPress2(object sender, System.Windows.Forms.KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Return)
            {
                connection.FreezeValue(y_position_address, "float", textBox2.Text);
            }
        }

        private void CheckEnterKeyPress3(object sender, System.Windows.Forms.KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Return)
            {
                connection.FreezeValue(z_position_address, "float", textBox3.Text);
            }
        }
    }
}
