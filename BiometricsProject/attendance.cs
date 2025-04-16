using DPFP;
using DPFP.Capture;
using System;
using System.Data;
using System.IO;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using System.Threading.Tasks;
using System.Drawing;
using System.Diagnostics;

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
        private DateTime lastVerificationTime = DateTime.MinValue;

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
                    ShowMessage("Can't initiate fingerprint capture.", MessageType.Error);
                }
            }
            catch (Exception ex)
            {
                ShowMessage(ex.Message, MessageType.Error);
            }
        }

        private void StartCapture()
        {
            if (Capturer != null)
            {
                try
                {
                    Capturer.StartCapture();
                    ShowMessage("Using the fingerprint reader...", MessageType.Info);
                }
                catch (Exception ex)
                {
                    ShowMessage(ex.Message, MessageType.Error);
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
                    ShowMessage(ex.Message, MessageType.Error);
                }
            }
        }

        public void OnComplete(object Capture, string ReaderSerialNumber, DPFP.Sample Sample)
        {
            ShowMessage("The fingerprint sample was captured.", MessageType.Info);
            Process(Sample);
        }

        public async void OnFingerGone(object Capture, string ReaderSerialNumber)
        {
            await Task.Delay(3000);
            ShowMessage("The finger was removed from the reader.", MessageType.Info);
        }

        public void OnFingerTouch(object Capture, string ReaderSerialNumber)
        {
            ShowMessage("The finger was placed on the reader.", MessageType.Info);
        }

        public void OnReaderConnect(object Capture, string ReaderSerialNumber)
        {
            ShowMessage("The fingerprint reader was connected.", MessageType.Info);
        }

        public void OnReaderDisconnect(object Capture, string ReaderSerialNumber)
        {
            ShowMessage("The fingerprint reader was disconnected.", MessageType.Info);
        }

        public void OnSampleQuality(object Capture, string ReaderSerialNumber, DPFP.Capture.CaptureFeedback CaptureFeedback)
        {
            if (CaptureFeedback == DPFP.Capture.CaptureFeedback.Good)
                ShowMessage("The quality of the fingerprint sample is good.", MessageType.Info);
            else
                ShowMessage("The quality of the fingerprint sample is poor.", MessageType.Warning);
        }

        protected void Process(DPFP.Sample Sample)
        {
            Task.Run(() =>
            {
                DPFP.FeatureSet features = ExtractFeatures(Sample, DPFP.Processing.DataPurpose.Verification);
                if (features != null)
                {
                    this.Invoke((MethodInvoker)delegate
                    {
                        VerifyFingerprint(features);
                    });
                }
            });
        }

        private async void VerifyFingerprint(DPFP.FeatureSet features)
        {
            try
            {
                if ((DateTime.Now - lastVerificationTime).TotalSeconds < 10 && IsVerified)
                {
                    ShowMessage("Please wait before scanning again. Minimum interval: 10 seconds.", MessageType.Warning);
                    return;
                }

                string MyConnection = "datasource=localhost;username=root;password=;database=swushsdb";
                using (MySqlConnection MyConn = new MySqlConnection(MyConnection))
                {
                    MyConn.Open();
                    DataTable dTable = GetUserFingerprints(MyConn);

                    foreach (DataRow row in dTable.Rows)
                    {
                        if (VerifyUserFingerprint(row, features))
                        {
                            lastVerificationTime = DateTime.Now;
                            VerifiedUserName = row["first_name"].ToString();
                            VerifiedUserId = Convert.ToInt32(row["id"]);
                            IsVerified = true;

                            ProcessAttendance(MyConn);
                            return;
                        }
                    }

                    ShowMessage("Fingerprint not verified.", MessageType.Error);
                }
            }
            catch (Exception ex)
            {
                ShowMessage($"Error: {ex.Message}", MessageType.Error);
                Debug.WriteLine($"Verification error: {ex.Message}");
                ShowMessage($"Error: {ex.Message}", MessageType.Error);
            }
        }

        private DataTable GetUserFingerprints(MySqlConnection connection)
        {
            string Query = "SELECT * FROM tbl_users";
            using (MySqlCommand MyCommand = new MySqlCommand(Query, connection))
            {
                MySqlDataAdapter MyAdapter = new MySqlDataAdapter();
                MyAdapter.SelectCommand = MyCommand;
                DataTable dTable = new DataTable();
                MyAdapter.Fill(dTable);
                return dTable;
            }
        }

        private bool VerifyUserFingerprint(DataRow row, DPFP.FeatureSet features)
        {
            byte[] leftFingerprintData = row["left_index_fingerprint"] != DBNull.Value
                ? (byte[])row["left_index_fingerprint"]
                : null;

            byte[] rightFingerprintData = row["right_index_fingerprint"] != DBNull.Value
                ? (byte[])row["right_index_fingerprint"]
                : null;

            if (leftFingerprintData == null && rightFingerprintData == null)
                return false;

            return VerifySingleFingerprint(features, leftFingerprintData) ||
                   VerifySingleFingerprint(features, rightFingerprintData);
        }

        private bool VerifySingleFingerprint(DPFP.FeatureSet features, byte[] fingerprintData)
        {
            if (fingerprintData == null) return false;

            using (MemoryStream ms = new MemoryStream(fingerprintData))
            {
                var template = new DPFP.Template();
                template.DeSerialize(ms);

                var result = new DPFP.Verification.Verification.Result();
                Verificator.Verify(features, template, ref result);
                return result.Verified;
            }
        }

        private void ProcessAttendance(MySqlConnection conn)
        {
            try
            {

                if (conn.State != ConnectionState.Open)
                {
                    conn.Open();
                }

                DateTime currentTime = DateTime.Now;
                DateTime currentDate = currentTime.Date;
                string currentDay = currentTime.ToString("dddd");

                // 1. Check if user is on leave
                if (IsUserOnLeave(conn, VerifiedUserId))
                {
                    ShowMessage($"{VerifiedUserName} is on leave today. Attendance not allowed.", MessageType.Warning);
                    return;
                }

                // 2. Get teacher's schedule (custom first, then regular)
                var schedule = GetTeacherSchedule(conn, VerifiedUserId, currentDate, currentDay);
                if (!schedule.HasSchedule)
                {
                    ShowMessage($"{VerifiedUserName} has no scheduled class today.", MessageType.Warning);
                    return;
                }

                // 3. Check if attendance already completed
                if (IsAttendanceCompleted(conn, VerifiedUserId))
                {
                    ShowMessage($"{VerifiedUserName} already completed attendance today.", MessageType.Info);
                    return;
                }

                // 4. Process check-in or check-out
                if (IsCheckedIn(conn, VerifiedUserId))
                {
                    ProcessCheckOut(conn, VerifiedUserId, VerifiedUserName, currentTime, schedule.EndTime);
                }
                else
                {
                    ProcessCheckIn(conn, VerifiedUserId, VerifiedUserName, currentTime, schedule.StartTime, schedule.EndTime);
                }
            }
            catch (Exception ex)
            {
                ShowMessage($"Error processing attendance: {ex.Message}", MessageType.Error);
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
            }
        }

        // Helper Methods

        private bool IsUserOnLeave(MySqlConnection conn, int userId)
        {
            string query = @"SELECT 1 FROM leave_records 
                          WHERE user_id = @UserId 
                          AND leave_status = 'Approved' 
                          AND leave_date = CURDATE()";
            using (var cmd = new MySqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@UserId", userId);
                using (var reader = cmd.ExecuteReader())
                {
                    return reader.HasRows;
                }
            }
        }

        private (bool HasSchedule, TimeSpan StartTime, TimeSpan EndTime, bool IsCustom)
            GetTeacherSchedule(MySqlConnection conn, int userId, DateTime currentDate, string currentDay)
        {
            // Try custom schedule first
            string customQuery = @"SELECT start_time, end_time 
                                 FROM teacher_custom_schedules 
                                 WHERE user_id = @UserId 
                                 AND @CurrentDate BETWEEN start_date AND end_date
                                 AND is_active = 1
                                 LIMIT 1";

            using (var cmd = new MySqlCommand(customQuery, conn))
            {
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@CurrentDate", currentDate);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        if (TimeSpan.TryParse(reader["start_time"].ToString(), out var startTime) &&
                            TimeSpan.TryParse(reader["end_time"].ToString(), out var endTime))
                        {
                            return (true, startTime, endTime, true);
                        }
                    }
                }
            }

            // Fall back to regular schedule
            string regularQuery = @"SELECT start_time, end_time 
                                  FROM teacher_daily_schedules 
                                  WHERE user_id = @UserId AND day = @Day 
                                  LIMIT 1";

            using (var cmd = new MySqlCommand(regularQuery, conn))
            {
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@Day", currentDay);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        if (TimeSpan.TryParse(reader["start_time"].ToString(), out var startTime) &&
                            TimeSpan.TryParse(reader["end_time"].ToString(), out var endTime))
                        {
                            return (true, startTime, endTime, false);
                        }
                    }
                }
            }

            return (false, TimeSpan.Zero, TimeSpan.Zero, false);
        }

        private bool IsAttendanceCompleted(MySqlConnection conn, int userId)
        {
            string query = @"SELECT 1 FROM attendance 
                            WHERE user_id = @UserId 
                            AND attendance_date = CURDATE() 
                            AND attendance_complete = 1";
            using (var cmd = new MySqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@UserId", userId);
                using (var reader = cmd.ExecuteReader())
                {
                    return reader.HasRows;
                }
            }
        }

        private bool IsCheckedIn(MySqlConnection conn, int userId)
        {
            string query = @"SELECT 1 FROM attendance 
                           WHERE user_id = @UserId 
                           AND attendance_date = CURDATE() 
                           AND is_checked_in = 1";
            using (var cmd = new MySqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@UserId", userId);
                using (var reader = cmd.ExecuteReader())
                {
                    return reader.HasRows;
                }
            }
        }

        private void ProcessCheckOut(MySqlConnection conn, int userId, string userName, DateTime currentTime, TimeSpan scheduledEndTime)
        {
            bool isEarlyCheckOut = currentTime.TimeOfDay < scheduledEndTime;
            TimeSpan earlyCheckoutBy = scheduledEndTime - currentTime.TimeOfDay;

            if (isEarlyCheckOut)
            {
                var dialogResult = MessageBox.Show(
                    $"{userName} is checking out {earlyCheckoutBy.TotalMinutes} minutes early. Continue?",
                    "Early Check-Out",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (dialogResult != DialogResult.Yes)
                {
                    ShowMessage("Early check-out cancelled.", MessageType.Info);
                    return;
                }
            }

            string query = @"UPDATE attendance 
                           SET check_out_time = @CheckOutTime, 
                               is_checked_in = 0, 
                               attendance_complete = 1,
                               is_early_checkout = @IsEarlyCheckout,
                               early_checkout_minutes = @EarlyCheckoutMinutes
                           WHERE user_id = @UserId 
                           AND attendance_date = CURDATE() 
                           AND is_checked_in = 1";

            using (var cmd = new MySqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@CheckOutTime", currentTime);
                cmd.Parameters.AddWithValue("@IsEarlyCheckout", isEarlyCheckOut ? 1 : 0);
                cmd.Parameters.AddWithValue("@EarlyCheckoutMinutes", isEarlyCheckOut ? (int)earlyCheckoutBy.TotalMinutes : 0);

                if (cmd.ExecuteNonQuery() > 0)
                {
                    string message = $"{userName} checked out at {currentTime:hh:mm tt}";
                    if (isEarlyCheckOut)
                    {
                        message += $"\n(Early by {earlyCheckoutBy.TotalMinutes} minutes)";
                    }
                    ShowMessage(message, MessageType.Success);
                }
                else
                {
                    ShowMessage("Failed to update attendance record.", MessageType.Error);
                }
            }
        }

        private void ProcessCheckIn(MySqlConnection conn, int userId, string userName, DateTime currentTime, TimeSpan scheduledStartTime, TimeSpan scheduledEndTime)
        {
            bool isEarlyCheckIn = currentTime.TimeOfDay < scheduledStartTime;
            bool isLate = currentTime.TimeOfDay > scheduledStartTime;
            TimeSpan earlyCheckinBy = scheduledStartTime - currentTime.TimeOfDay;
            int minutesLate = isLate ? (int)(currentTime.TimeOfDay - scheduledStartTime).TotalMinutes : 0;

            if (isEarlyCheckIn)
            {
                var dialogResult = MessageBox.Show(
                    $"{userName} is checking in {earlyCheckinBy.TotalMinutes} minutes early. Continue?",
                    "Early Check-In",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (dialogResult != DialogResult.Yes)
                {
                    ShowMessage("Early check-in cancelled.", MessageType.Info);
                    return;
                }
            }
            else if (isLate)
            {
                var dialogResult = MessageBox.Show(
                    $"{userName} is {minutesLate} minutes late. Record attendance?",
                    "Late Arrival",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (dialogResult != DialogResult.Yes)
                {
                    ShowMessage("Check-in cancelled.", MessageType.Info);
                    return;
                }
            }

            string query = @"INSERT INTO attendance (
                            user_id, check_in_time, attendance_date, status, 
                            is_checked_in, is_late, minutes_late, 
                            is_early_checkin, early_checkin_minutes, attendance_complete
                        ) VALUES (
                            @UserId, @CheckInTime, @AttendanceDate, 'Present', 
                            1, @IsLate, @MinutesLate, 
                            @IsEarlyCheckin, @EarlyCheckinMinutes, 0
                        )";

            using (var cmd = new MySqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@CheckInTime", currentTime);
                cmd.Parameters.AddWithValue("@AttendanceDate", currentTime.Date);
                cmd.Parameters.AddWithValue("@IsLate", isLate ? 1 : 0);
                cmd.Parameters.AddWithValue("@MinutesLate", minutesLate);
                cmd.Parameters.AddWithValue("@IsEarlyCheckin", isEarlyCheckIn ? 1 : 0);
                cmd.Parameters.AddWithValue("@EarlyCheckinMinutes", isEarlyCheckIn ? (int)earlyCheckinBy.TotalMinutes : 0);

                if (cmd.ExecuteNonQuery() > 0)
                {
                    string message = $"{userName} checked in at {currentTime:hh:mm tt}";
                    if (isLate)
                    {
                        RecordLateArrival(conn, userId, minutesLate);
                        message += $"\n(Late by {minutesLate} minutes)";
                    }
                    else if (isEarlyCheckIn)
                    {
                        message += $"\n(Early by {earlyCheckinBy.TotalMinutes} minutes)";
                    }
                    ShowMessage(message, MessageType.Success);
                }
                else
                {
                    ShowMessage("Failed to record attendance.", MessageType.Error);
                }
            }
        }

        private void RecordLateArrival(MySqlConnection conn, int userId, int minutesLate)
        {
            string query = @"INSERT INTO user_lates 
                           (user_id, late_date, late_minutes, notified)
                           VALUES (@UserId, CURDATE(), @LateMinutes, 0)";

            using (var cmd = new MySqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@LateMinutes", minutesLate);
                cmd.ExecuteNonQuery();
            }
        }

        private DPFP.FeatureSet ExtractFeatures(DPFP.Sample Sample, DPFP.Processing.DataPurpose Purpose)
        {
            DPFP.Processing.FeatureExtraction extractor = new DPFP.Processing.FeatureExtraction();
            DPFP.Capture.CaptureFeedback feedback = DPFP.Capture.CaptureFeedback.None;
            DPFP.FeatureSet features = new DPFP.FeatureSet();

            extractor.CreateFeatureSet(Sample, Purpose, ref feedback, ref features);
            return feedback == DPFP.Capture.CaptureFeedback.Good ? features : null;
        }

        private void ShowMessage(string message, MessageType type)
        {
            if (lblStatus.InvokeRequired)
            {
                lblStatus.BeginInvoke(new Action(() =>
                {
                    lblStatus.Text = message;
                    UpdateStatusColor(type);
                }));
            }
            else
            {
                lblStatus.Text = message;
                UpdateStatusColor(type);
            }
        }

        private void UpdateStatusColor(MessageType type)
        {
            switch (type)
            {
                case MessageType.Error:
                    lblStatus.ForeColor = Color.Red;
                    break;
                case MessageType.Warning:
                    lblStatus.ForeColor = Color.Orange;
                    break;
                case MessageType.Success:
                    lblStatus.ForeColor = Color.Green;
                    break;
                default:
                    lblStatus.ForeColor = Color.Black;
                    break;
            }
        }

        private enum MessageType
        {
            Info,
            Success,
            Warning,
            Error
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            StopCapture();
            this.Close();
        }
    }
}