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
using System.Threading;

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
            this.FormClosing += attendance_FormClosing;
            Init();
            StartCapture();
        }

        private void attendance_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Stop and dispose the countdown timer
            if (countdownTimer != null)
            {
                countdownTimer.Stop();
                countdownTimer.Dispose();
            }

            // Also stop the fingerprint capture if needed
            StopCapture();
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

        private CancellationTokenSource fingerGoneCancellation;
        public async void OnFingerGone(object Capture, string ReaderSerialNumber)
        {
            if (!isCountdownActive)
            {
                // Cancel any previous delay
                fingerGoneCancellation?.Cancel();
                fingerGoneCancellation = new CancellationTokenSource();

                try
                {
                    await Task.Delay(3000, fingerGoneCancellation.Token);
                    if (!isCountdownActive) // Check again after delay
                    {
                        ShowMessage("The finger was removed from the reader.", MessageType.Info);
                    }
                }
                catch (TaskCanceledException)
                {
                    // This is expected when cancelled
                }
            }
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

        private bool isCountdownActive = false;

        private void StartCountdown(int seconds)
        {
            // Cancel any pending "finger gone" message
            fingerGoneCancellation?.Cancel();

            // Rest of your countdown code...
            UpdateCountdownDisplay(seconds);
            lblCountdown.Invoke((MethodInvoker)(() =>
            {
                lblCountdown.Visible = true;
            }));

            countdownTimer.Interval = 1000;
            countdownTimer.Tick -= CountdownTimer_Tick;
            countdownTimer.Tick += CountdownTimer_Tick;
            countdownTimer.Start();
        }

        private void CountdownTimer_Tick(object sender, EventArgs e)
        {
            // Get current remaining time
            int remainingSeconds = 11 - (int)(DateTime.Now - lastVerificationTime).TotalSeconds;

            if (remainingSeconds <= 0)
            {
                // Countdown finished
                countdownTimer.Stop();
                isCountdownActive = false;

                lblCountdown.Invoke((MethodInvoker)(() =>
                {
                    lblCountdown.Visible = false;
                }));

                ShowMessage("Ready to scan again.", MessageType.Info);
            }
            else
            {
                // Update display
                UpdateCountdownDisplay(remainingSeconds);
            }
        }

        private void UpdateCountdownDisplay(int seconds)
        {
            if (lblCountdown.InvokeRequired)
            {
                lblCountdown.Invoke((MethodInvoker)(() =>
                {
                    lblCountdown.Text = $"Please wait for: {seconds} seconds before scanning again.";
                    lblCountdown.ForeColor = seconds <= 3 ? Color.Red : Color.Orange;
                }));
            }
            else
            {
                lblCountdown.Text = $"Please wait for: {seconds} seconds before scanning again.";
                lblCountdown.ForeColor = seconds <= 3 ? Color.Red : Color.Orange;
            }
        }

        private async void VerifyFingerprint(DPFP.FeatureSet features)
        {
            try
            {
                if ((DateTime.Now - lastVerificationTime).TotalSeconds < 10 && IsVerified)
                {
                    int remainingSeconds = 11 - (int)(DateTime.Now - lastVerificationTime).TotalSeconds;
                    isCountdownActive = true;

                    // Start the visual countdown
                    StartCountdown(remainingSeconds);

                    //ShowMessage($"Please wait {remainingSeconds} seconds before scanning again.", MessageType.Warning);
                    ShowMessage($" ", MessageType.Warning);
                    return;
                }

                // Reset the countdown flag when a new verification starts
                isCountdownActive = false;

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

                // 2. Get existing attendance record
                var existingAttendance = GetExistingAttendance(conn, VerifiedUserId);

                // 3. Get teacher's schedule
                var schedule = GetTeacherSchedule(conn, VerifiedUserId, currentDate, currentDay);
                if (!schedule.HasSchedule)
                {
                    ShowMessage($"{VerifiedUserName} has no scheduled class today.", MessageType.Warning);
                    return;
                }

                // 4. Check attendance state and process accordingly
                if (existingAttendance != null)
                {
                    // If manual attendance exists
                    if (existingAttendance.IsManual)
                    {
                        // For manual clock-in records
                        if (existingAttendance.CheckInTime != null && existingAttendance.CheckOutTime == null)
                        {
                            // Show confirmation modal for clock-out attempt
                            if (ShowConfirmationModal(
                                $"{VerifiedUserName} has been manually clocked-in at {existingAttendance.CheckInTime.Value.ToString("hh:mm tt")}.",
                                "Are you sure you want to clock out via system?",
                                "Clock Out Confirmation"))
                            {
                                ProcessCheckOut(conn, VerifiedUserId, VerifiedUserName, currentTime, schedule.EndTime);
                            }
                            return;
                        }

                        // For complete manual records
                        ShowMessage($"{VerifiedUserName} has been manually marked. Please contact admin if this is incorrect.", MessageType.Warning);
                        return;
                    }

                    // Normal biometric flow
                    if (existingAttendance.CheckInTime != null && existingAttendance.CheckOutTime == null)
                    {
                        ProcessCheckOut(conn, VerifiedUserId, VerifiedUserName, currentTime, schedule.EndTime);
                        return;
                    }

                    if (existingAttendance.CheckOutTime != null)
                    {
                        ShowMessage($"{VerifiedUserName} has already completed attendance today.", MessageType.Info);
                        return;
                    }
                }

                // 5. If no existing record or not checked in yet, process check-in
                ProcessCheckIn(conn, VerifiedUserId, VerifiedUserName, currentTime, schedule.StartTime, schedule.EndTime);
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

        // Add this helper method for modal confirmation
        private bool ShowConfirmationModal(string message, string question, string title)
        {
            // Implementation depends on your UI framework
            // This would typically show a modal dialog with Yes/No buttons
            // Return true if user confirms, false if cancels
            // Example implementation:
            var result = MessageBox.Show($"{message}\n\n{question}", title,
                                       MessageBoxButtons.YesNo,
                                       MessageBoxIcon.Question);
            return result == DialogResult.Yes;
        }


        private AttendanceRecord GetExistingAttendance(MySqlConnection conn, int userId)
        {
            string query = @"SELECT 
            check_in_time, 
            check_out_time, 
            attendance_complete, 
            CASE WHEN check_in_time IS NOT NULL AND check_out_time IS NULL THEN 1 ELSE 0 END as is_checked_in,
            CASE WHEN check_in_manual = 1 OR check_out_manual = 1 THEN 1 ELSE 0 END as is_manual
            FROM attendance 
            WHERE user_id = @UserId 
            AND attendance_date = CURDATE()";

            using (var cmd = new MySqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@UserId", userId);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new AttendanceRecord
                        {
                            CheckInTime = reader["check_in_time"] != DBNull.Value ? (DateTime?)reader["check_in_time"] : null,
                            CheckOutTime = reader["check_out_time"] != DBNull.Value ? (DateTime?)reader["check_out_time"] : null,
                            AttendanceComplete = reader["attendance_complete"] != DBNull.Value && Convert.ToBoolean(reader["attendance_complete"]),
                            IsCheckedIn = reader["is_checked_in"] != DBNull.Value && Convert.ToBoolean(reader["is_checked_in"]),
                            IsManual = reader["is_manual"] != DBNull.Value && Convert.ToBoolean(reader["is_manual"])
                        };
                    }
                }
            }
            return null;
        }

        private class AttendanceRecord
        {
            public DateTime? CheckInTime { get; set; }
            public DateTime? CheckOutTime { get; set; }
            public bool AttendanceComplete { get; set; }
            public bool IsCheckedIn { get; set; }
            public bool IsManual { get; set; }
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
                    $"{userName} is clocking out {earlyCheckoutBy.TotalMinutes} minutes early. Continue?",
                    "Early Clock-Out",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (dialogResult != DialogResult.Yes)
                {
                    ShowMessage("Early clock-out cancelled.", MessageType.Info);
                    return;
                }
            }

            // Modified query to be more flexible with conditions
            string query = @"UPDATE attendance 
                   SET check_out_time = @CheckOutTime, 
                       is_checked_in = 0, 
                       attendance_complete = 1,
                       is_early_checkout = @IsEarlyCheckout,
                       early_checkout_minutes = @EarlyCheckoutMinutes,
                       check_out_manual = 0
                   WHERE user_id = @UserId 
                   AND attendance_date = CURDATE() 
                   AND (check_out_time IS NULL OR check_out_time = '')";

            using (var cmd = new MySqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@CheckOutTime", currentTime);
                cmd.Parameters.AddWithValue("@IsEarlyCheckout", isEarlyCheckOut ? 1 : 0);
                cmd.Parameters.AddWithValue("@EarlyCheckoutMinutes", isEarlyCheckOut ? (int)earlyCheckoutBy.TotalMinutes : 0);

                int rowsAffected = cmd.ExecuteNonQuery();

                if (rowsAffected > 0)
                {
                    string message = $"{userName} clocked out at {currentTime:hh:mm tt}";
                    if (isEarlyCheckOut)
                    {
                        message += $"\n(Early by {earlyCheckoutBy.TotalMinutes} minutes)";
                    }
                    ShowMessage(message, MessageType.Success);
                }
                else
                {
                    // More detailed error message
                    string errorQuery = @"SELECT check_out_time FROM attendance 
                                WHERE user_id = @UserId 
                                AND attendance_date = CURDATE()";
                    using (var errorCmd = new MySqlCommand(errorQuery, conn))
                    {
                        errorCmd.Parameters.AddWithValue("@UserId", userId);
                        object result = errorCmd.ExecuteScalar();

                        if (result != null && result != DBNull.Value)
                        {
                            ShowMessage($"{userName} has already clocked out at {Convert.ToDateTime(result):hh:mm tt}", MessageType.Warning);
                        }
                        else
                        {
                            ShowMessage("No matching attendance record found to update. Please clock in first.", MessageType.Error);
                        }
                    }
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
                    $"{userName} is clocking in {earlyCheckinBy.TotalMinutes} minutes early. Continue?",
                    "Early Clock-In",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (dialogResult != DialogResult.Yes)
                {
                    ShowMessage("Early clock-in cancelled.", MessageType.Info);
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
                    ShowMessage("Clock-in cancelled.", MessageType.Info);
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