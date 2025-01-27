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

        public enroll(string userId)
        {
            InitializeComponent();
            this.userId = userId; // Assign the user ID
        }

        // Default constructor in case no arguments are passed
        public enroll()
        {
            InitializeComponent();
        }

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

                    switch (Enroller.TemplateStatus)
                    {
                        case DPFP.Processing.Enrollment.Status.Ready:
                            {
                                MemoryStream fingerprintData = new MemoryStream();
                                Enroller.Template.Serialize(fingerprintData);
                                fingerprintData.Position = 0;

                                if (isLeftIndex)
                                {
                                    leftFingerprintData = fingerprintData;
                                    MessageBox.Show("Left index fingerprint successfully registered!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                    isLeftIndex = false; // Switch to the right index fingerprint
                                    Enroller.Clear();
                                    Stop();
                                    Start();
                                }
                                else
                                {
                                    rightFingerprintData = fingerprintData;
                                    MessageBox.Show("Right index fingerprint successfully registered!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                    SaveToDatabase();
                                    Stop();
                                    Application.Exit(); // Close the application
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
                    while (myReader.Read())
                    {
                        count++;
                    }
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

                        MessageBox.Show("Fingerprints successfully updated for user ID: " + userId, "Register Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show("User ID not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("Both fingerprints must be registered before saving to the database.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }


        private void UpdateStatus()
        {
            string finger = isLeftIndex ? "Left Index Finger" : "Right Index Finger";
            SetStatus($"Enrolling {finger}. Fingerprint Samples Needed: {Enroller.FeaturesNeeded}");
        }
    }
}

