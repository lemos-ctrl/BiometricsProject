using DPFP;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
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
        private MemoryStream leftFingerprintData;
        private MemoryStream rightFingerprintData;
        private bool isLeftIndex = true; // Tracks if we are enrolling the left or right fingerprint
        private string userId; // Store the user ID passed from the web
        private DPFP.Verification.Verification Verificator; // Fingerprint Verifier

        public enroll(string userId)
        {
            InitializeComponent();
            this.FormClosing += new FormClosingEventHandler(enroll_FormClosing);
            this.userId = userId; // Assign the user ID
        }

        public enroll()
        {
            InitializeComponent();
        }

        protected override void Init()
        {
            base.Init();
            base.Text = "Fingerprint Enrollment";
            Enroller = new DPFP.Processing.Enrollment();
            Verificator = new DPFP.Verification.Verification(); // Initialize Verificator
            UpdateStatus();
        }

        protected override void Process(DPFP.Sample Sample)
        {
            base.Process(Sample);
            DPFP.FeatureSet features = ExtractFeatures(Sample, DPFP.Processing.DataPurpose.Enrollment);

            if (features != null)
            {
                try
                {
                    MakeReport("The fingerprint feature set was created.");
                    Enroller.AddFeatures(features);
                }
                finally
                {
                    UpdateStatus();
                }

                if (Enroller.TemplateStatus == DPFP.Processing.Enrollment.Status.Ready)
                {
                    MemoryStream fingerprintData = new MemoryStream();
                    Enroller.Template.Serialize(fingerprintData);
                    fingerprintData.Position = 0;
                    byte[] fingerprintBytes = fingerprintData.ToArray();

                    if (FingerprintExists(Sample))  // Uses DPFP verification logic
                    {
                        MessageBox.Show(this, "This fingerprint is already registered in the system!", "Duplicate Detected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        Stop();
                        Application.Exit(); // **Automatically close the application**
                        return;
                    }

                    if (isLeftIndex)
                    {
                        leftFingerprintData = fingerprintData;
                        MessageBox.Show(this, "Left index fingerprint successfully registered!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        isLeftIndex = false; // Switch to right index fingerprint
                        Enroller.Clear();
                        Stop();
                        Start();
                    }
                    else
                    {
                        rightFingerprintData = fingerprintData;
                        MessageBox.Show(this, "Right index fingerprint successfully registered!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        SaveToDatabase();
                        Stop();
                        Application.Exit(); // **Close application after successful enrollment**
                    }
                }
                else if (Enroller.TemplateStatus == DPFP.Processing.Enrollment.Status.Failed)
                {
                    Console.WriteLine("FAILED");
                    Enroller.Clear();
                    Stop();
                    UpdateStatus();
                    OnTemplate(null);
                    Start();
                }
            }
        }

        private bool FingerprintExists(DPFP.Sample sample)
        {
            try
            {
                string MyConnection = "datasource=localhost;username=root;password=";
                string Query = "SELECT left_index_fingerprint, right_index_fingerprint FROM swushsdb.tbl_users";

                using (MySqlConnection MyConn = new MySqlConnection(MyConnection))
                {
                    MyConn.Open();
                    using (MySqlCommand MyCommand = new MySqlCommand(Query, MyConn))
                    {
                        using (MySqlDataReader myReader = MyCommand.ExecuteReader())
                        {
                            while (myReader.Read())
                            {
                                byte[] leftFingerprint = myReader["left_index_fingerprint"] as byte[];
                                byte[] rightFingerprint = myReader["right_index_fingerprint"] as byte[];

                                if (VerifyFingerprint(sample, leftFingerprint) || VerifyFingerprint(sample, rightFingerprint))
                                {
                                    return true; // **Duplicate fingerprint found**
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Error checking fingerprint: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return false; // **No duplicate found**
        }

        private bool VerifyFingerprint(DPFP.Sample sample, byte[] existingFingerprint)
        {
            if (existingFingerprint == null) return false;

            try
            {
                using (MemoryStream existingStream = new MemoryStream(existingFingerprint))
                {
                    DPFP.Template existingTemplate = new DPFP.Template();
                    existingTemplate.DeSerialize(existingStream);

                    DPFP.FeatureSet features = ExtractFeatures(sample, DPFP.Processing.DataPurpose.Verification);
                    if (features != null)
                    {
                        DPFP.Verification.Verification.Result result = new DPFP.Verification.Verification.Result();
                        Verificator.Verify(features, existingTemplate, ref result);
                        return result.Verified;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fingerprint verification error: " + ex.Message);
            }
            return false;
        }

        private void SaveToDatabase()
        {
            if (leftFingerprintData != null && rightFingerprintData != null)
            {
                try
                {
                    string MyConnection = "datasource=localhost;username=root;password=";
                    string Query = "SELECT * FROM swushsdb.tbl_users WHERE id = @userId";
                    MySqlConnection MyConn = new MySqlConnection(MyConnection);
                    MySqlCommand MyCommand = new MySqlCommand(Query, MyConn);
                    MyCommand.Parameters.AddWithValue("@userId", userId);
                    MyConn.Open();

                    int count = 0;
                    MySqlDataReader myReader = MyCommand.ExecuteReader();
                    while (myReader.Read()) { count++; }
                    MyConn.Close();

                    if (count > 0)
                    {
                        byte[] leftBytes = leftFingerprintData.ToArray();
                        byte[] rightBytes = rightFingerprintData.ToArray();

                        string UpdateQuery = "UPDATE swushsdb.tbl_users SET left_index_fingerprint = @leftFinger, right_index_fingerprint = @rightFinger WHERE id = @userId";
                        MySqlConnection MyConnUpdate = new MySqlConnection(MyConnection);
                        MySqlCommand MyCommandUpdate = new MySqlCommand(UpdateQuery, MyConnUpdate);

                        MyCommandUpdate.Parameters.AddWithValue("@userId", userId);
                        MyCommandUpdate.Parameters.AddWithValue("@leftFinger", leftBytes).DbType = DbType.Binary;
                        MyCommandUpdate.Parameters.AddWithValue("@rightFinger", rightBytes).DbType = DbType.Binary;

                        MyConnUpdate.Open();
                        MyCommandUpdate.ExecuteNonQuery();
                        MyConnUpdate.Close();

                        Console.WriteLine("SUCCESS");
                    }
                    else
                    {
                        Console.WriteLine("FAILED: User ID not found (" + userId + ")");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "Error saving fingerprint: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show(this, "FAILED: Both fingerprints must be registered before saving.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void enroll_FormClosing(object sender, FormClosingEventArgs e)
        {
            Console.WriteLine("CLOSED");
        }

        private void UpdateStatus()
        {
            string finger = isLeftIndex ? "Left Index Finger" : "Right Index Finger";
            SetStatus($"Enrolling {finger}. Fingerprint Samples Needed: {Enroller.FeaturesNeeded}");
        }
    }
}
