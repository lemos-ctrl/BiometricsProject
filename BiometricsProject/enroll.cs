using DPFP;
using System;
using System.IO;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using System.Data;

namespace BiometricsProject
{
    public partial class enroll : capture
    {
        public delegate void OnTemplateEventHandler(DPFP.Template template);
        public event OnTemplateEventHandler OnTemplate;

        private DPFP.Processing.Enrollment Enroller;
        private MemoryStream leftFingerprintData;
        private MemoryStream rightFingerprintData;
        private bool isLeftIndex = true; // Tracks which finger we are enrolling
        private string userId; // User ID passed from the web
        private DPFP.Verification.Verification Verificator; // Fingerprint verifier

        public enroll(string userId)
        {
            InitializeComponent();
            this.FormClosing += new FormClosingEventHandler(enroll_FormClosing);
            this.userId = userId;
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

                // If the enrollment template is ready, we have a complete fingerprint
                if (Enroller.TemplateStatus == DPFP.Processing.Enrollment.Status.Ready)
                {
                    MemoryStream fingerprintData = new MemoryStream();
                    Enroller.Template.Serialize(fingerprintData);
                    fingerprintData.Position = 0;

                    if (FingerprintExists(Sample))
                    {
                        // MATCHES "ALERT:DUPLICATE:<message>"
                        Console.WriteLine("ALERT:DUPLICATE: This fingerprint is already registered in the system!");
                        MessageBox.Show(this,
                            "This fingerprint is already registered in the system!",
                            "Duplicate Detected",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);

                        Stop();
                        Application.Exit(); // Close the application
                        return;
                    }

                    if (isLeftIndex)
                    {
                        leftFingerprintData = fingerprintData;
                        // MATCHES "ALERT:SUCCESS:<message>"
                        //Console.WriteLine("ALERT:SUCCESS: Left index fingerprint successfully registered!");

                        MessageBox.Show(this,
                            "Left index fingerprint successfully registered!",
                            "Success",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);

                        // Prepare for right index
                        isLeftIndex = false;
                        Enroller.Clear();
                        Stop();
                        Start();
                    }
                    else
                    {
                        rightFingerprintData = fingerprintData;
                        // MATCHES "ALERT:SUCCESS:<message>"
                        //Console.WriteLine("ALERT:SUCCESS: Right index fingerprint successfully registered!");

                        MessageBox.Show(this,
                            "Right index fingerprint successfully registered!",
                            "Success",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);

                        SaveToDatabase();
                        Stop();

                        // Done enrolling
                        Application.Exit();
                        Console.WriteLine("ALERT:SUCCESS: Fingerprint successfully registered!");
                    }
                }
                else if (Enroller.TemplateStatus == DPFP.Processing.Enrollment.Status.Failed)
                {
                    // MATCHES "ALERT:FAILED:<message>"
                    Console.WriteLine("ALERT:FAILED: Enrollment process failed. Please try again.");

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

                                if (VerifyFingerprint(sample, leftFingerprint) ||
                                    VerifyFingerprint(sample, rightFingerprint))
                                {
                                    return true; // Found a duplicate
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // MATCHES "ALERT:ERROR:<message>"
                Console.WriteLine("ALERT:ERROR: Checking fingerprint failed. " + ex.Message);

                MessageBox.Show(this,
                    "Error checking fingerprint: " + ex.Message,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            return false;
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

                    using (MySqlConnection MyConn = new MySqlConnection(MyConnection))
                    using (MySqlCommand MyCommand = new MySqlCommand(Query, MyConn))
                    {
                        MyCommand.Parameters.AddWithValue("@userId", userId);
                        MyConn.Open();

                        int count = 0;
                        using (MySqlDataReader myReader = MyCommand.ExecuteReader())
                        {
                            while (myReader.Read())
                            {
                                count++;
                            }
                        }

                        if (count > 0)
                        {
                            byte[] leftBytes = leftFingerprintData.ToArray();
                            byte[] rightBytes = rightFingerprintData.ToArray();

                            string UpdateQuery = @"
                                UPDATE swushsdb.tbl_users 
                                SET left_index_fingerprint = @leftFinger, 
                                    right_index_fingerprint = @rightFinger 
                                WHERE id = @userId";

                            using (MySqlConnection MyConnUpdate = new MySqlConnection(MyConnection))
                            using (MySqlCommand MyCommandUpdate = new MySqlCommand(UpdateQuery, MyConnUpdate))
                            {
                                MyCommandUpdate.Parameters.AddWithValue("@userId", userId);
                                MyCommandUpdate.Parameters.AddWithValue("@leftFinger", leftBytes).DbType = DbType.Binary;
                                MyCommandUpdate.Parameters.AddWithValue("@rightFinger", rightBytes).DbType = DbType.Binary;

                                MyConnUpdate.Open();
                                MyCommandUpdate.ExecuteNonQuery();
                            }

                            // MATCHES "ALERT:SUCCESS:<message>" in case it’s relevant
                            Console.WriteLine("ALERT:SUCCESS: Fingerprint data saved in the database!");
                        }
                        else
                        {
                            // MATCHES "ALERT:FAILED:<message>"
                            Console.WriteLine($"ALERT:FAILED: User ID not found ({userId}).");
                        }
                    }
                }
                catch (Exception ex)
                {
                    // MATCHES "ALERT:ERROR:<message>"
                    Console.WriteLine("ALERT:ERROR: Unable to save fingerprint. " + ex.Message);

                    MessageBox.Show(this,
                        "Error saving fingerprint: " + ex.Message,
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
            else
            {
                // MATCHES "ALERT:FAILED:<message>"
                Console.WriteLine("ALERT:FAILED: Both fingerprints must be registered before saving.");

                MessageBox.Show(this,
                    "FAILED: Both fingerprints must be registered before saving.",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private void enroll_FormClosing(object sender, FormClosingEventArgs e)
        {
            // MATCHES "ALERT:CLOSED:<message>"
            Console.WriteLine("ALERT:CLOSED: The enrollment form was closed by the user.");
        }

        private void UpdateStatus()
        {
            string finger = isLeftIndex ? "Left Index Finger" : "Right Index Finger";
            SetStatus($"Enrolling {finger}. Fingerprint Samples Needed: {Enroller.FeaturesNeeded}");
        }
    }
}
