using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Diagnostics;
using System.Threading;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace WindowsFormsApplication1
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            string CurrDir = Path.GetDirectoryName(Assembly.GetAssembly(typeof(Form1)).CodeBase);
                XmlDocument xdoc = new XmlDocument();
                xdoc.Load(CurrDir + "\\ConnectionString.xml");
                XmlNode list = xdoc.SelectSingleNode("/root/ConnectionString");
                
                this.comInitTableAdapter.Connection.ConnectionString = list.InnerText;
                this.operationsTableAdapter.Connection.ConnectionString = list.InnerText;
                this.sALESTableAdapter.Connection.ConnectionString = list.InnerText;
                //this.paymentTableAdapter.Connection.ConnectionString = list.InnerText;
                //this.cashIOTableAdapter.Connection.ConnectionString = list.InnerText;
                //this.cashiersTableAdapter.Connection.ConnectionString = list.InnerText;
                this.operationsTableAdapter1.Connection.ConnectionString = list.InnerText;
                this.operationsTableAdapter3.Connection.ConnectionString = list.InnerText;
                this.operationsTableAdapter2.Connection.ConnectionString = list.InnerText;
                this.infoTableAdapter.Connection.ConnectionString = list.InnerText;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // TODO: This line of code loads data into the 'fPWorkDataSet6.Info' table. You can move, or remove it, as needed.
            this.infoTableAdapter.Fill(this.fPWorkDataSet6.Info);
            // TODO: This line of code loads data into the 'fPWorkDataSet5.Operations' table. You can move, or remove it, as needed.
            this.operationsTableAdapter3.Fill(this.fPWorkDataSet5.Operations);
            // TODO: This line of code loads data into the 'fPWorkDataSet4.Operations' table. You can move, or remove it, as needed.
            this.operationsTableAdapter2.Fill(this.fPWorkDataSet4.Operations);
            // TODO: This line of code loads data into the 'fPWorkDataSet3.SALES' table. You can move, or remove it, as needed.
            this.sALESTableAdapter.Fill(this.fPWorkDataSet3.SALES);
            // TODO: This line of code loads data into the 'fPWorkDataSet3.Payment' table. You can move, or remove it, as needed.
            this.paymentTableAdapter.Fill(this.fPWorkDataSet3.Payment);
            // TODO: This line of code loads data into the 'fPWorkDataSet3.CashIO' table. You can move, or remove it, as needed.
            this.cashIOTableAdapter.Fill(this.fPWorkDataSet3.CashIO);
            // TODO: This line of code loads data into the 'fPWorkDataSet3.Cashiers' table. You can move, or remove it, as needed.
            this.cashiersTableAdapter.Fill(this.fPWorkDataSet3.Cashiers);
            // TODO: This line of code loads data into the 'fPWorkDataSet2.Operations' table. You can move, or remove it, as needed.
            this.operationsTableAdapter1.Fill(this.fPWorkDataSet2.Operations);
            // TODO: This line of code loads data into the 'fPWorkDataSet1.Operations' table. You can move, or remove it, as needed.
            this.operationsTableAdapter.Fill(this.fPWorkDataSet1.Operations);
            // TODO: This line of code loads data into the 'fPWorkDataSet.ComInit' table. You can move, or remove it, as needed.
            
            this.comInitTableAdapter.Fill(this.fPWorkDataSet.ComInit);

        }

        private void button1_Click(object sender, EventArgs e)
        {
            update();
            
        }

        private void fillByToolStripButton_Click(object sender, EventArgs e)
        {
            try
            {
               // this.operationsTableAdapter.FillBy(this.fPWorkDataSet1.Operations);
            }
            catch (System.Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.Message);
            }

        }

        private void fillBy1ToolStripButton_Click(object sender, EventArgs e)
        {
            try
            {
                //this.operationsTableAdapter.FillBy1(this.fPWorkDataSet1.Operations);
            }
            catch (System.Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.Message);
            }

        }

        private void splitContainer1_Panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void dataGridView4_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void dataGridView2_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            Int32 selectedCellCount =
        dataGridView2.GetCellCount(DataGridViewElementStates.Selected);
            if (selectedCellCount > 0)
            {
                int rowindex = dataGridView2.SelectedCells[0].RowIndex;
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            update();
        }

        void update()
        {
            this.comInitTableAdapter.Fill(this.fPWorkDataSet.ComInit);
            this.operationsTableAdapter.Fill(this.fPWorkDataSet1.Operations);
            this.operationsTableAdapter1.Fill(this.fPWorkDataSet2.Operations);
            this.operationsTableAdapter3.Fill(this.fPWorkDataSet5.Operations);
            this.operationsTableAdapter2.Fill(this.fPWorkDataSet4.Operations);
            this.infoTableAdapter.Fill(this.fPWorkDataSet6.Info);

        }

        private void operationsBindingSource1_CurrentChanged(object sender, EventArgs e)
        {

        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            timer1.Enabled = checkBox1.Checked;
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }
    }
}
