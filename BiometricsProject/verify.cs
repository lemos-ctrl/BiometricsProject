using DPFP;
using System;
using System.Data;
using System.IO;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace BiometricsProject
{
    public partial class verify : capture
    {
        private DPFP.Verification.Verification Verificator;


        public verify()
        {
            InitializeComponent();
            // Attach the FormClosing event
            this.Shown += verify_Shown;
            this.FormClosing += new FormClosingEventHandler(verify_FormClosing);
        }
        private void verify_Shown(object sender, EventArgs e)
        {
            this.Activate();
            this.Focus();
        }


        protected override void Init()
        {
            base.Init();
            base.Text = "Fingerprint Verification";
            Verificator = new DPFP.Verification.Verification();
            UpdateStatus(0);
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

                    MyConn.Open();  // Open connection (though we’re just reading)
                    //myReader not strictly needed here if we’re using dTable
                    //MySqlDataReader myReader = MyCommand.ExecuteReader(); // optional if you prefer

                    bool fingerprintMatched = false;
                    string userName = "";

                    // Compare the incoming Sample with each stored fingerprint
                    foreach (DataRow row in dTable.Rows)
                    {
                        byte[] leftFingerprint = row["left_index_fingerprint"] as byte[];
                        byte[] rightFingerprint = row["right_index_fingerprint"] as byte[];

                        bool isVerified = false;

                        // 1) Try the left fingerprint
                        if (leftFingerprint != null)
                        {
                            using (MemoryStream leftStream = new MemoryStream(leftFingerprint))
                            {
                                DPFP.Template leftTemplate = new DPFP.Template();
                                leftTemplate.DeSerialize(leftStream);
                                isVerified = VerifyFingerprint(Sample, leftTemplate);
                            }
                        }

                        // 2) If not verified yet, try the right fingerprint
                        if (!isVerified && rightFingerprint != null)
                        {
                            using (MemoryStream rightStream = new MemoryStream(rightFingerprint))
                            {
                                DPFP.Template rightTemplate = new DPFP.Template();
                                rightTemplate.DeSerialize(rightStream);
                                isVerified = VerifyFingerprint(Sample, rightTemplate);
                            }
                        }

                        // If matched, record user’s name and stop searching
                        if (isVerified)
                        {
                            fingerprintMatched = true;
                            userName = row["first_name"].ToString() + " " + row["last_name"].ToString();
                            MakeReport($"The fingerprint was verified as {userName}");
                            Setfname(userName);
                            break;
                        }
                    }

                    if (fingerprintMatched)
                    {
                        // PRINT ALERT for success
                        Console.WriteLine($"ALERT! SUCCESS: The fingerprint was verified as {userName}");
                    }
                    else
                    {
                        MakeReport("The fingerprint was not verified.");
                        Setfname("NO DATA");
                        // PRINT ALERT for failed match
                        Console.WriteLine("ALERT! FAILED: The fingerprint was not verified.");
                    }
                }
            }
            catch (Exception ex)
            {
                // PRINT ALERT for error
                Console.WriteLine($"ALERT:ERROR: {ex.Message}");
                MessageBox.Show(
                    "Error: " + ex.Message,
                    "Verification Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        private bool VerifyFingerprint(DPFP.Sample sample, DPFP.Template template)
        {
            // We still call the parent’s base.Process(sample) to generate images, logs, etc.
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

        private void UpdateStatus(int FAR)
        {
            SetStatus($"False Accept Rate (FAR) = {FAR}");
        }

        // Called when the user closes the form
        private void verify_FormClosing(object sender, FormClosingEventArgs e)
        {
            // PRINT ALERT for closed
            Console.WriteLine("ALERT:CLOSED: The verification form was closed by the user.");
        }
    }
}
