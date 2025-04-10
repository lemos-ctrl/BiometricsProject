﻿using DPFP;
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
            DPFP.FeatureSet features = ExtractFeatures(Sample, DPFP.Processing.DataPurpose.Verification);
            if (features != null)
            {
                VerifyFingerprint(features);
            }
        }



        private void VerifyFingerprint(DPFP.FeatureSet features)
        {
            try
            {
                // Check if enough time has passed since last verification (5 minutes)
                if ((DateTime.Now - lastVerificationTime).TotalSeconds < 5 && IsVerified)
                {
                    MakeReport("Please wait before scanning again. Minimum interval: 5 seconds.");
                    return;
                }

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
                                lastVerificationTime = DateTime.Now;
                                VerifiedUserName = row["first_name"].ToString();
                                VerifiedUserId = Convert.ToInt32(row["id"]);
                                IsVerified = true;

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
                DateTime currentTime = DateTime.Now;

                // Check if user is on leave
                string leaveQuery = @"
            SELECT * 
            FROM leave_records 
            WHERE user_id = @UserId 
            AND leave_status = 'Approved' 
            AND leave_date = CURDATE()";
                using (MySqlCommand leaveCmd = new MySqlCommand(leaveQuery, conn))
                {
                    leaveCmd.Parameters.AddWithValue("@UserId", VerifiedUserId);
                    using (MySqlDataReader leaveReader = leaveCmd.ExecuteReader())
                    {
                        if (leaveReader.HasRows)
                        {
                            MakeReport($"{VerifiedUserName} is on leave today. Attendance action is not allowed.");
                            return;
                        }
                    }
                }

                // Get teacher's schedule
                string currentDay = currentTime.ToString("dddd");
                string scheduleQuery = @"
    SELECT start_time, end_time 
    FROM teacher_daily_schedules 
    WHERE user_id = @UserId AND day = @Day 
    LIMIT 1";

                bool hasSchedule = false;
                TimeSpan scheduledStartTime = TimeSpan.Zero;
                TimeSpan scheduledEndTime = TimeSpan.Zero;

                using (MySqlCommand scheduleCmd = new MySqlCommand(scheduleQuery, conn))
                {
                    scheduleCmd.Parameters.AddWithValue("@UserId", VerifiedUserId);
                    scheduleCmd.Parameters.AddWithValue("@Day", currentDay);
                    using (MySqlDataReader scheduleReader = scheduleCmd.ExecuteReader())
                    {
                        if (scheduleReader.Read())
                        {
                            // Safely parse the time values
                            if (TimeSpan.TryParse(scheduleReader["start_time"].ToString(), out scheduledStartTime) &&
                                TimeSpan.TryParse(scheduleReader["end_time"].ToString(), out scheduledEndTime))
                            {
                                hasSchedule = true;
                            }
                        }
                    }
                }

                if (!hasSchedule)
                {
                    MakeReport($"{VerifiedUserName} does not have a scheduled class today. Attendance action is not allowed.");
                    return;
                }

                // Check if attendance already completed
                string completionQuery = @"
            SELECT * 
            FROM attendance 
            WHERE user_id = @UserId 
            AND attendance_date = CURDATE() 
            AND attendance_complete = 1";
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

                // Check if user is checked in (for checkout)
                string checkStatusQuery = @"
            SELECT * 
            FROM attendance 
            WHERE user_id = @UserId 
            AND attendance_date = CURDATE() 
            AND is_checked_in = 1";
                using (MySqlCommand checkStatusCmd = new MySqlCommand(checkStatusQuery, conn))
                {
                    checkStatusCmd.Parameters.AddWithValue("@UserId", VerifiedUserId);
                    using (MySqlDataReader statusReader = checkStatusCmd.ExecuteReader())
                    {
                        if (statusReader.HasRows)
                        {
                            statusReader.Close();

                            // Check for early checkout
                            bool isEarlyCheckOut = currentTime.TimeOfDay < scheduledEndTime;
                            TimeSpan earlyCheckoutBy = scheduledEndTime - currentTime.TimeOfDay;

                            if (isEarlyCheckOut)
                            {
                                using (var earlyCheckOutForm = new EarlyCheckOutConfirmationForm(VerifiedUserName, earlyCheckoutBy))
                                {
                                    if (earlyCheckOutForm.ShowDialog(this) != DialogResult.Yes)
                                    {
                                        MakeReport("Early check-out cancelled.");
                                        return;
                                    }
                                }
                            }
                            else
                            {
                                using (var confirmForm = new ConfirmationForm(VerifiedUserName))
                                {
                                    if (confirmForm.ShowDialog(this) != DialogResult.Yes)
                                    {
                                        MakeReport("Check out cancelled.");
                                        return;
                                    }
                                }
                            }

                            // Process checkout
                            string updateQuery = @"
                        UPDATE attendance 
                        SET check_out_time = @CheckOutTime, 
                            is_checked_in = 0, 
                            attendance_complete = 1,
                            is_early_checkout = @IsEarlyCheckout,
                            early_checkout_minutes = @EarlyCheckoutMinutes
                        WHERE user_id = @UserId 
                        AND attendance_date = CURDATE() 
                        AND is_checked_in = 1";
                            using (MySqlCommand updateCmd = new MySqlCommand(updateQuery, conn))
                            {
                                updateCmd.Parameters.AddWithValue("@UserId", VerifiedUserId);
                                updateCmd.Parameters.AddWithValue("@CheckOutTime", currentTime);
                                updateCmd.Parameters.AddWithValue("@IsEarlyCheckout", isEarlyCheckOut ? 1 : 0);
                                updateCmd.Parameters.AddWithValue("@EarlyCheckoutMinutes", isEarlyCheckOut ? (int)earlyCheckoutBy.TotalMinutes : 0);

                                int rowsAffected = updateCmd.ExecuteNonQuery();
                                if (rowsAffected > 0)
                                {
                                    MakeReport($"{VerifiedUserName} has been logged out at {currentTime:hh:mm:ss tt}");
                                    if (isEarlyCheckOut)
                                    {
                                        MakeReport($"{VerifiedUserName} checked out {earlyCheckoutBy.TotalMinutes} minutes early.");
                                    }
                                }
                                else
                                {
                                    MakeReport("Failed to update attendance record.");
                                }
                            }
                            return;
                        }
                    }
                }

                // Process check-in (either normal or early)
                bool isEarlyCheckIn = currentTime.TimeOfDay < scheduledStartTime;
                TimeSpan earlyCheckinBy = scheduledStartTime - currentTime.TimeOfDay;

                if (isEarlyCheckIn)
                {
                    using (var earlyConfirmForm = new EarlyCheckInConfirmationForm(VerifiedUserName, earlyCheckinBy))
                    {
                        if (earlyConfirmForm.ShowDialog(this) != DialogResult.Yes)
                        {
                            MakeReport("Early Clock-in cancelled.");
                            return;
                        }
                    }
                }

                // Insert check-in record
                string insertQuery = @"
            INSERT INTO attendance (
                user_id, check_in_time, attendance_date, status, 
                is_checked_in, is_late, minutes_late, 
                is_early_checkin, early_checkin_minutes, attendance_complete
            ) VALUES (
                @UserId, @CheckInTime, @AttendanceDate, 'Present', 
                1, @IsLate, @MinutesLate, 
                @IsEarlyCheckin, @EarlyCheckinMinutes, 0
            )";
                using (MySqlCommand insertCmd = new MySqlCommand(insertQuery, conn))
                {
                    insertCmd.Parameters.AddWithValue("@UserId", VerifiedUserId);
                    insertCmd.Parameters.AddWithValue("@CheckInTime", currentTime);
                    insertCmd.Parameters.AddWithValue("@AttendanceDate", currentTime.Date);

                    bool isLate = currentTime.TimeOfDay > scheduledStartTime;
                    int minutesLate = isLate ? (int)(currentTime.TimeOfDay - scheduledStartTime).TotalMinutes : 0;
                    insertCmd.Parameters.AddWithValue("@IsLate", isLate ? 1 : 0);
                    insertCmd.Parameters.AddWithValue("@MinutesLate", minutesLate);
                    insertCmd.Parameters.AddWithValue("@IsEarlyCheckin", isEarlyCheckIn ? 1 : 0);
                    insertCmd.Parameters.AddWithValue("@EarlyCheckinMinutes", isEarlyCheckIn ? (int)earlyCheckinBy.TotalMinutes : 0);

                    int rowsAffected = insertCmd.ExecuteNonQuery();
                    if (rowsAffected > 0)
                    {
                        MakeReport($"{VerifiedUserName} has been logged in at {currentTime:hh:mm:ss tt}");

                        if (isLate)
                        {
                            string lateInsertQuery = @"
    INSERT INTO user_lates (user_id, late_date, late_minutes, notified)
    VALUES (@UserId, @LateDate, @LateMinutes, 0)";
                            using (MySqlCommand lateCmd = new MySqlCommand(lateInsertQuery, conn))
                            {
                                lateCmd.Parameters.AddWithValue("@UserId", VerifiedUserId);
                                lateCmd.Parameters.AddWithValue("@LateDate", currentTime.Date);
                                lateCmd.Parameters.AddWithValue("@LateMinutes", minutesLate);
                                lateCmd.ExecuteNonQuery();
                                MakeReport($"{VerifiedUserName} has been marked as late for today by {minutesLate} minutes.");
                            }
                        }
                        else if (isEarlyCheckIn)
                        {
                            MakeReport($"{VerifiedUserName} checked in {earlyCheckinBy.TotalMinutes} minutes early.");
                        }
                    }
                    else
                    {
                        MakeReport("Failed to record attendance. Please try again.");
                    }
                }
            }
            catch (Exception ex)
            {
                MakeReport($"Error processing attendance: {ex.Message}");
                MessageBox.Show($"Error: {ex.Message}", "Attendance Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // Ensure connection is closed if we're done with it
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
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