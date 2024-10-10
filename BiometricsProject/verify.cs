using DPFP;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using System.IO;

namespace BiometricsProject
{
    public partial class verify : capture
    {

        private DPFP.Template Template;
        private DPFP.Verification.Verification Verificator;
        public void Verify(DPFP.Template template)
        {
            Template = template;
            ShowDialog();
            
        }

        protected override void Process(Sample Sample)
        {
            try
            {
                string MyConnection = "datasource=localhost;username=root;password=";
                string Query = "SELECT * FROM testdb.members";
                MySqlConnection MyConn = new MySqlConnection(MyConnection);
                MySqlCommand MyCommand = new MySqlCommand(Query, MyConn);

                MySqlDataAdapter MyAdapter = new MySqlDataAdapter();
                MyAdapter.SelectCommand = MyCommand;
                DataTable dTable = new DataTable();

                MyAdapter.Fill(dTable);

                MySqlDataReader myReader;
                MyConn.Open();
                myReader = MyCommand.ExecuteReader();


                foreach (DataRow row in dTable.Rows)
                {
                    byte[] _img_ = (byte[])row["finger_print"];
                    MemoryStream ms = new MemoryStream(_img_);  // Load the fingerprint data into the MemoryStream
                    DPFP.Template Template = new DPFP.Template();
                    Template.DeSerialize(ms);  // Deserialize from the MemoryStream


                    base.Process(Sample);

                    DPFP.FeatureSet features = ExtractFeatures(Sample, DPFP.Processing.DataPurpose.Verification);

                    if (features != null)
                    {
                        DPFP.Verification.Verification.Result result = new DPFP.Verification.Verification.Result();
                        Verificator.Verify(features, Template, ref result);
                        UpdateStatus(result.FARAchieved);
                        if (result.Verified)
                        {
                            MakeReport("The Fingerprint was verified as " + row["fname"].ToString());

                            Setfname(row["fname"].ToString());

                            MyConn.Close();
                            break;
                        }

                        else
                        {
                            MakeReport("The fingerprint was not verified.");
                            Setfname("NO DATA");

                            MyConn.Close();
                        }
                    }
                }


            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        protected override void Init()
        {
            base.Init();
            base.Text = "Fingerprint Verification";
            Verificator = new DPFP.Verification.Verification();
            UpdateStatus(0);

        }

        private void UpdateStatus(int FAR)
        {
            SetStatus(String.Format("False Accept Rate (FAR) = {0}", FAR));
        }
    }
}
