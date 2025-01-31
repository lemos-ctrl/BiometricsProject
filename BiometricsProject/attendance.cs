using DPFP;
using DPFP.Capture;
using System;
using System.Data;
using System.IO;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using System.Threading.Tasks;

namespace BiometricsProject
{
    public partial class attendance : Form, DPFP.Capture.EventHandler
    {
        private DPFP.Template Template;
        private DPFP.Capture.Capture Capturer;
        private DPFP.Verification.Verification Verificator;
        private bool IsVerified = false;
        private string VerifiedUserName;
        private int VerifiedUserId;

        public attendance()
        {
            InitializeComponent();
            Init();
            StartCapture();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
        }

        private void Init()
        {
            try
            {
                Capturer = new DPFP.Capture.Capture();
                if (Capturer != null)
                {
                    Capturer.EventHandler = this;
                    Verificator = new DPFP.Verification.Verification();
                }
                else
                {
                    MakeReport("Can't initiate fingerprint capture.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void StartCapture()
        {
            if (Capturer != null)
            {
                try
                {
                    Capturer.StartCapture();
                    MakeReport("Using the fingerprint reader...");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void StopCapture()
        {
            if (Capturer != null)
            {
                try
                {
                    Capturer.StopCapture();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        public void OnComplete(object Capture, string ReaderSerialNumber, DPFP.Sample Sample)
        {
            MakeReport("The fingerprint sample was captured.");
            Process(Sample);
        }

        public async void OnFingerGone(object Capture, string ReaderSerialNumber)
        {
            // Wait for 3 seconds before showing the message
            await Task.Delay(3000);
            MakeReport("The finger was removed from the reader.");
        }

        public void OnFingerTouch(object Capture, string ReaderSerialNumber)
        {
            MakeReport("The finger was placed on the reader.");
        }

        public void OnReaderConnect(object Capture, string ReaderSerialNumber)
        {
            MakeReport("The fingerprint reader was connected.");
        }

        public void OnReaderDisconnect(object Capture, string ReaderSerialNumber)
        {
            MakeReport("The fingerprint reader was disconnected.");
        }

        public void OnSampleQuality(object Capture, string ReaderSerialNumber, DPFP.Capture.CaptureFeedback CaptureFeedback)
        {
            if (CaptureFeedback == DPFP.Capture.CaptureFeedback.Good)
                MakeReport("The quality of the fingerprint sample is good.");
            else
                MakeReport("The quality of the fingerprint sample is poor.");
        }

        protected void Process(DPFP.Sample Sample)
        {
            // Extract features from the sample
            DPFP.FeatureSet features = ExtractFeatures(Sample, DPFP.Processing.DataPurpose.Verification);
            if (features != null)
            {
                // Load fingerprints from the database and verify
                VerifyFingerprint(features);
            }
        }

        private void VerifyFingerprint(DPFP.FeatureSet features)
        {
            try
            {
                string MyConnection = "datasource=localhost;username=root;password=;database=swushsdb";
                string Query = "SELECT * FROM tbl_users";
                using (MySqlConnection MyConn = new MySqlConnection(MyConnection))
                {
                    MyConn.Open();
                    using (MySqlCommand MyCommand = new MySqlCommand(Query, MyConn))
                    {
                        MySqlDataAdapter MyAdapter = new MySqlDataAdapter();
                        MyAdapter.SelectCommand = MyCommand;
                        DataTable dTable = new DataTable();

                        MyAdapter.Fill(dTable);

                        foreach (DataRow row in dTable.Rows)
                        {
                            // Check for null fingerprints
                            byte[] leftFingerprintData = row["left_index_fingerprint"] != DBNull.Value
                                ? (byte[])row["left_index_fingerprint"]
                                : null;

                            byte[] rightFingerprintData = row["right_index_fingerprint"] != DBNull.Value
                                ? (byte[])row["right_index_fingerprint"]
                                : null;

                            if (leftFingerprintData == null && rightFingerprintData == null)
                                continue;

                            DPFP.Template leftTemplate = null, rightTemplate = null;

                            if (leftFingerprintData != null)
                            {
                                using (MemoryStream leftMs = new MemoryStream(leftFingerprintData))
                                {
                                    leftTemplate = new DPFP.Template();
                                    leftTemplate.DeSerialize(leftMs);
                                }
                            }

                            if (rightFingerprintData != null)
                            {
                                using (MemoryStream rightMs = new MemoryStream(rightFingerprintData))
                                {
                                    rightTemplate = new DPFP.Template();
                                    rightTemplate.DeSerialize(rightMs);
                                }
                            }

                            DPFP.Verification.Verification.Result resultLeft = new DPFP.Verification.Verification.Result();
                            DPFP.Verification.Verification.Result resultRight = new DPFP.Verification.Verification.Result();

                            if (leftTemplate != null)
                                Verificator.Verify(features, leftTemplate, ref resultLeft);

                            if (rightTemplate != null)
                                Verificator.Verify(features, rightTemplate, ref resultRight);

                            if (resultLeft.Verified || resultRight.Verified)
                            {
                                VerifiedUserName = row["first_name"].ToString();
                                VerifiedUserId = Convert.ToInt32(row["id"]);
                                IsVerified = true;

                                // Process attendance for the verified user
                                ProcessAttendance(MyConn);
                                break;
                            }
                        }

                        if (!IsVerified)
                        {
                            MakeReport("Fingerprint not verified.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void ProcessAttendance(MySqlConnection conn)
        {
            try
            {
                // Check if the user is on leave today
                string leaveQuery = @"
    SELECT * 
    FROM leave_records 
    WHERE user_id = @UserId 
      AND leave_status = 'Approved' 
      AND leave_date = CURDATE()"; // Match only today's leave date

                using (MySqlCommand leaveCmd = new MySqlCommand(leaveQuery, conn))
                {
                    leaveCmd.Parameters.AddWithValue("@UserId", VerifiedUserId);

                    using (MySqlDataReader leaveReader = leaveCmd.ExecuteReader())
                    {
                        if (leaveReader.HasRows) // If there are records for leave today
                        {
                            MakeReport($"{VerifiedUserName} is on leave today. Attendance action is not allowed.");
                            return; // Stop further actions
                        }
                    }
                }

                // Check if attendance is already completed for the day
                string completionQuery = "SELECT * FROM attendance WHERE user_id = @UserId AND attendance_date = CURDATE() AND attendance_complete = 1";
                using (MySqlCommand completionCmd = new MySqlCommand(completionQuery, conn))
                {
                    completionCmd.Parameters.AddWithValue("@UserId", VerifiedUserId);
                    using (MySqlDataReader completionReader = completionCmd.ExecuteReader())
                    {
                        if (completionReader.HasRows)
                        {
                            MakeReport($"{VerifiedUserName} has already completed attendance for today.");
                            return;
                        }
                    }
                }

                // Check if the user is currently checked in
                string checkStatusQuery = "SELECT * FROM attendance WHERE user_id = @UserId AND attendance_date = CURDATE() AND is_checked_in = 1";
                using (MySqlCommand checkStatusCmd = new MySqlCommand(checkStatusQuery, conn))
                {
                    checkStatusCmd.Parameters.AddWithValue("@UserId", VerifiedUserId);
                    using (MySqlDataReader statusReader = checkStatusCmd.ExecuteReader())
                    {
                        if (statusReader.HasRows)
                        {
                            // User is checked in, log them out
                            statusReader.Close();
                            string updateQuery = "UPDATE attendance SET check_out_time = @CheckOutTime, is_checked_in = 0, attendance_complete = 1 WHERE user_id = @UserId AND attendance_date = CURDATE() AND is_checked_in = 1";
                            using (MySqlCommand updateCmd = new MySqlCommand(updateQuery, conn))
                            {
                                updateCmd.Parameters.AddWithValue("@UserId", VerifiedUserId);
                                updateCmd.Parameters.AddWithValue("@CheckOutTime", DateTime.Now);
                                updateCmd.ExecuteNonQuery();
                                MakeReport($"{VerifiedUserName} has been logged out at {DateTime.Now:hh:mm:ss tt}");
                            }
                        }
                        else
                        {
                            // User is not checked in, log them in
                            statusReader.Close();
                            string insertQuery = "INSERT INTO attendance (user_id, check_in_time, attendance_date, status, is_checked_in, is_late, attendance_complete) VALUES (@UserId, @CheckInTime, @AttendanceDate, 'Present', 1, @IsLate, 0)";
                            using (MySqlCommand insertCmd = new MySqlCommand(insertQuery, conn))
                            {
                                insertCmd.Parameters.AddWithValue("@UserId", VerifiedUserId);
                                insertCmd.Parameters.AddWithValue("@CheckInTime", DateTime.Now);
                                insertCmd.Parameters.AddWithValue("@AttendanceDate", DateTime.Now.Date);

                                // Late detection logic
                                bool isLate = DateTime.Now.TimeOfDay > new TimeSpan(9, 0, 0); // Late cutoff: 9:00 AM
                                insertCmd.Parameters.AddWithValue("@IsLate", isLate ? 1 : 0);

                                insertCmd.ExecuteNonQuery();
                                MakeReport($"{VerifiedUserName} has been logged in at {DateTime.Now:hh:mm:ss tt}");

                                // Add late record to user_lates if the user is late
                                if (isLate)
                                {
                                    string lateInsertQuery = "INSERT INTO user_lates (user_id, late_date, notified) VALUES (@UserId, @LateDate, 0)";
                                    using (MySqlCommand lateCmd = new MySqlCommand(lateInsertQuery, conn))
                                    {
                                        lateCmd.Parameters.AddWithValue("@UserId", VerifiedUserId);
                                        lateCmd.Parameters.AddWithValue("@LateDate", DateTime.Now.Date);
                                        lateCmd.ExecuteNonQuery();
                                        MakeReport($"{VerifiedUserName} has been marked as late for today.");
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }







        private void MarkAttendance(int userId, DateTime checkInTime)
        {
            try
            {
                string MyConnection = "datasource=localhost;username=root;password=;database=swushsdb";
                string Query = "INSERT INTO attendance (user_id, check_in_time, attendance_date, status) VALUES (@UserId, @CheckInTime, @AttendanceDate, 'Present')";
                MySqlConnection MyConn = new MySqlConnection(MyConnection);
                MySqlCommand MyCommand = new MySqlCommand(Query, MyConn);

                MyCommand.Parameters.AddWithValue("@UserId", userId);
                MyCommand.Parameters.AddWithValue("@CheckInTime", checkInTime);
                MyCommand.Parameters.AddWithValue("@AttendanceDate", checkInTime.Date);

                MyConn.Open();
                MyCommand.ExecuteNonQuery();
                MyConn.Close();

                // Ensure the UI update is marshaled to the main thread
                if (lblStatus.InvokeRequired)
                {
                    lblStatus.BeginInvoke(new Action(() =>
                        lblStatus.Text = $"Status: {VerifiedUserName} logged in at {checkInTime.ToString("hh:mm:ss tt")}."
                    ));
                }
                else
                {
                    lblStatus.Text = $"Status: {VerifiedUserName} logged in at {checkInTime.ToString("hh:mm:ss tt")}.";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private DPFP.FeatureSet ExtractFeatures(DPFP.Sample Sample, DPFP.Processing.DataPurpose Purpose)
        {
            DPFP.Processing.FeatureExtraction extractor = new DPFP.Processing.FeatureExtraction();
            DPFP.Capture.CaptureFeedback feedback = DPFP.Capture.CaptureFeedback.None;
            DPFP.FeatureSet features = new DPFP.FeatureSet();

            extractor.CreateFeatureSet(Sample, Purpose, ref feedback, ref features);
            if (feedback == DPFP.Capture.CaptureFeedback.Good)
            {
                return features;
            }
            else
            {
                return null;
            }
        }

        private void MakeReport(string message)
        {
            if (lblStatus.InvokeRequired)
            {
                // Using BeginInvoke to avoid blocking UI
                lblStatus.BeginInvoke(new Action(() => lblStatus.Text = message));
            }
            else
            {
                lblStatus.Text = message;
            }
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            StopCapture();
            this.Close();
        }
    }
}
