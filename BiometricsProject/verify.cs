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
                string Query = "SELECT id, first_name, last_name, left_index_fingerprint, right_index_fingerprint FROM swushsdb.tbl_users";

                using (MySqlConnection MyConn = new MySqlConnection(MyConnection))
                {
                    MySqlCommand MyCommand = new MySqlCommand(Query, MyConn);
                    MySqlDataAdapter MyAdapter = new MySqlDataAdapter(MyCommand);
                    DataTable dTable = new DataTable();
                    MyAdapter.Fill(dTable);

                    MyConn.Open();
                    MySqlDataReader myReader = MyCommand.ExecuteReader();

                    bool fingerprintMatched = false;
                    string verifiedUser = "";

                    foreach (DataRow row in dTable.Rows)
                    {
                        byte[] leftFingerprint = row["left_index_fingerprint"] as byte[];
                        byte[] rightFingerprint = row["right_index_fingerprint"] as byte[];

                        bool isVerified = false;

                        // Verify Left Fingerprint
                        if (leftFingerprint != null)
                        {
                            using (MemoryStream leftStream = new MemoryStream(leftFingerprint))
                            {
                                DPFP.Template leftTemplate = new DPFP.Template();
                                leftTemplate.DeSerialize(leftStream);
                                isVerified = VerifyFingerprint(Sample, leftTemplate);
                            }
                        }

                        // Verify Right Fingerprint
                        if (!isVerified && rightFingerprint != null)
                        {
                            using (MemoryStream rightStream = new MemoryStream(rightFingerprint))
                            {
                                DPFP.Template rightTemplate = new DPFP.Template();
                                rightTemplate.DeSerialize(rightStream);
                                isVerified = VerifyFingerprint(Sample, rightTemplate);
                            }
                        }

                        if (isVerified)
                        {
                            fingerprintMatched = true;
                            verifiedUser = "The fingerprint was verified as " + row["first_name"].ToString() + " " + row["last_name"].ToString();
                            MakeReport("The fingerprint was verified as " + verifiedUser);
                            Setfname(verifiedUser);
                            break;
                        }
                    }

                    if (!fingerprintMatched)
                    {
                        MakeReport("The fingerprint was not verified.");
                        Setfname("NO DATA");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Verification Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool VerifyFingerprint(DPFP.Sample sample, DPFP.Template template)
        {
            base.Process(sample);

            DPFP.FeatureSet features = ExtractFeatures(sample, DPFP.Processing.DataPurpose.Verification);

            if (features != null)
            {
                DPFP.Verification.Verification.Result result = new DPFP.Verification.Verification.Result();
                Verificator.Verify(features, template, ref result);
                UpdateStatus(result.FARAchieved);

                return result.Verified;
            }

            return false;
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
