using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace BiometricsProject
{
    public partial class enroll : capture
    {

        public delegate void OnTemplateEventHandler(DPFP.Template template);

        public event OnTemplateEventHandler OnTemplate;

        private DPFP.Processing.Enrollment Enroller;

        protected override void Init()
        {
            base.Init();
            base.Text = "Fingerprint Enrollment";
            Enroller = new DPFP.Processing.Enrollment();
            UpdateStatus();
        }

        protected override void Process(DPFP.Sample Sample)
        {
            base.Process(Sample);
            DPFP.FeatureSet features = ExtractFeatures(Sample, DPFP.Processing.DataPurpose.Enrollment);

            if (features != null)
                try
                {
                    MakeReport("The fingerprint feature set was created.");
                    Enroller.AddFeatures(features);
                }
                finally
                {
                    UpdateStatus();

                    switch(Enroller.TemplateStatus)
                    {
                        case DPFP.Processing.Enrollment.Status.Ready:
                            {
                                int count = 0;

                                OnTemplate(Enroller.Template);

                                MemoryStream fingerprintData = new MemoryStream();
                                Enroller.Template.Serialize(fingerprintData);
                                fingerprintData.Position = 0;
                                BinaryReader br = new BinaryReader(fingerprintData);


                                byte[] bytes = br.ReadBytes((Int32)fingerprintData.Length);

                                try
                                {
                                    string MyConnection = "datasource=localhost;username=root;password=";
                                    string Query = "SELECT * FROM testdb.members WHERE UPPER(fname) = '" + FirstName.ToUpper() + "' ";
                                    MySqlConnection MyConn = new MySqlConnection(MyConnection);
                                    MySqlCommand MyCommand = new MySqlCommand(Query, MyConn);

                                    MyConn.Open();


                                    MySqlDataReader myReader = MyCommand.ExecuteReader();

                                    while (myReader.Read())
                                    {
                                        count = count + 1;
                                    }

                                    MakeReport("Total Member Found: " + count);

                                    if (count > 0)
                                    {
                                        MessageBox.Show("The Person you want to register is already exists.");
                                        Stop();
                                        Start();

                                    }

                                    else
                                    {
                                        try
                                        {
                                            string MyConnection1 = "datasource=localhost;username=root;password=";
                                            string Query1 = "INSERT INTO testdb.members (fname,finger_print) VALUES ('" + FirstName + "', @finger) ";
                                            MySqlConnection MyConn1 = new MySqlConnection(MyConnection1);
                                            MySqlCommand MyCommand1 = new MySqlCommand(Query1, MyConn1);


                                            MyCommand1.Parameters.AddWithValue("@finger", bytes).DbType = DbType.Binary;
                                            MySqlDataReader MyReader1;
                                            MyConn1.Open();
                                            MyReader1 = MyCommand1.ExecuteReader();
                                            MessageBox.Show(FirstName + " was successfully registered!", "Register Success", MessageBoxButtons.OK, MessageBoxIcon.Information);


                                            MyConn1.Close();
                                            Stop();
                                        }
                                        catch (Exception ex)
                                        {
                                            MessageBox.Show(ex.Message);
                                        }
                                    }

                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show("Error :"+ ex.Message);
                                }


                                break;
                            }

                        case DPFP.Processing.Enrollment.Status.Failed:
                            {
                                Enroller.Clear();
                                Stop();
                                UpdateStatus();
                                OnTemplate(null);
                                Start();
                                break;
                            }
                    }
                }
        }








        private void UpdateStatus()
        {
            SetStatus(String.Format("Fingerprint Sample Needed: {0}", Enroller.FeaturesNeeded));
        }

        
    }
}
