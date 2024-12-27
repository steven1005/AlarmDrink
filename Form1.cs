using System;
using System.Drawing;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace WinFormsApp1
{
    public partial class Form1 : Form
    {
        private NumericUpDown nudSitReminder;
        private NumericUpDown nudWaterReminder;
        private Button btnUpdateTimers;
        private NotifyIcon notifyIcon;
        private Timer sitTimer;
        private Timer waterTimer;
        private Form currentReminderForm = null;

        private CheckBox chkEnableSit;
        private CheckBox chkEnableDrink;

        private Point sitReminderPosition = Point.Empty;
        private Point waterReminderPosition = Point.Empty;

        private const string ConfigFile = "reminder_positions.txt";

        public Form1()
        {
            InitializeComponent();

            // 窗口初始化
            this.Text = "久坐與喝水提醒";
            this.Size = new Size(330, 180);
            this.StartPosition = FormStartPosition.CenterScreen;

            // 久坐提醒
            Label lblSit = new Label { Text = "久坐提醒 (分鐘):", Location = new Point(20, 20), AutoSize = true };
            nudSitReminder = new NumericUpDown { Location = new Point(150, 20), Width = 100, Minimum = 1, Maximum = 120, Value = 30 };

            chkEnableSit = new CheckBox
            {
                Name = "chkEnableSit",
                Text = "啟用",
                Location = new Point(260, 20),
                AutoSize = true
            };
            this.Controls.Add(chkEnableSit);

            // 喝水提醒
            Label lblWater = new Label { Text = "喝水提醒 (分鐘):", Location = new Point(20, 60), AutoSize = true };
            nudWaterReminder = new NumericUpDown { Location = new Point(150, 60), Width = 100, Minimum = 1, Maximum = 120, Value = 10 };

            chkEnableDrink = new CheckBox
            {
                Name = "chkEnableDrink",
                Text = "啟用",
                Location = new Point(260, 60),
                AutoSize = true
            };
            this.Controls.Add(chkEnableDrink);
            chkEnableDrink.Checked = true;

            // 更新提醒按鈕
            btnUpdateTimers = new Button
            {
                Text = "更新提醒時間",
                Location = new Point(80, 100),
                Size = new Size(120, 30)
            };
            btnUpdateTimers.Click += BtnUpdateTimers_Click;

            // 添加控件到窗體
            this.Controls.Add(lblSit);
            this.Controls.Add(nudSitReminder);
            this.Controls.Add(lblWater);
            this.Controls.Add(nudWaterReminder);
            this.Controls.Add(btnUpdateTimers);

            // NotifyIcon 設置
            notifyIcon = new NotifyIcon
            {
                Icon = SystemIcons.Information,
                Visible = true,
                Text = "久坐與喝水提醒"
            };
            notifyIcon.DoubleClick += NotifyIcon_DoubleClick;

            // 右鍵菜單
            ContextMenuStrip contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("顯示主畫面", null, ShowMainForm);
            contextMenu.Items.Add("停止提醒", null, StopReminders);
            contextMenu.Items.Add("退出", null, ExitApplication);
            notifyIcon.ContextMenuStrip = contextMenu;

            // Timer 初始化
            sitTimer = new Timer();
            sitTimer.Tick += SitTimer_Tick;

            waterTimer = new Timer();
            waterTimer.Tick += WaterTimer_Tick;

            // 窗口加載時啟動提醒
            this.Load += Form1_Load;
        }

        private void BtnStart_Click(object sender, EventArgs e)
        {
            sitTimer.Interval = (int)nudSitReminder.Value * 60 * 1000;
            waterTimer.Interval = (int)nudWaterReminder.Value * 60 * 1000;

            sitTimer.Start();
            waterTimer.Start();

            MessageBox.Show("提醒已啟動，程序已最小化至系統托盤", "提醒", MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.Hide();
        }

        private void BtnStop_Click(object sender, EventArgs e)
        {
            sitTimer.Stop();
            waterTimer.Stop();
            MessageBox.Show("提醒已停止", "提醒", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ShowReminderWindow(string title, string message, Point initialPosition, Action<Point> savePositionCallback)
        {
            // 如果已經有提醒視窗，先關閉
            if (currentReminderForm != null)
            {
                currentReminderForm.Close();
                currentReminderForm = null;
            }

            // 初始化新的提醒視窗
            Form reminderForm = new Form
            {
                Text = title,
                Size = new Size(300, 180),
                StartPosition = FormStartPosition.Manual,
                FormBorderStyle = FormBorderStyle.FixedToolWindow
            };

            // 設定視窗初始位置
            if (initialPosition == Point.Empty)
            {
                var screenBounds = Screen.PrimaryScreen.WorkingArea;
                initialPosition = new Point(
                    screenBounds.Left + (screenBounds.Width - reminderForm.Width) / 2,
                    screenBounds.Top + (screenBounds.Height - reminderForm.Height) / 2
                );
            }
            reminderForm.Location = initialPosition;

            // 添加消息標籤
            Label lblMessage = new Label
            {
                Text = message,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Arial", 12)
            };
            reminderForm.Controls.Add(lblMessage);

            // 滑鼠事件處理，用於拖動視窗
            Point? dragOffset = null;

            reminderForm.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    dragOffset = new Point(e.X, e.Y);
                }
            };

            reminderForm.MouseMove += (s, e) =>
            {
                if (e.Button == MouseButtons.Left && dragOffset.HasValue)
                {
                    var offset = dragOffset.Value;
                    var newLocation = new Point(
                        reminderForm.Left + e.X - offset.X,
                        reminderForm.Top + e.Y - offset.Y
                    );
                    reminderForm.Location = newLocation;
                }
            };

            reminderForm.MouseUp += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    dragOffset = null;
                }
            };

            // 關閉視窗時保存位置
            reminderForm.FormClosed += (s, e) =>
            {
                savePositionCallback(reminderForm.Location);
                currentReminderForm = null; // 視窗關閉後清除引用
            };

            // 顯示新的提醒視窗
            currentReminderForm = reminderForm;
            reminderForm.TopMost = true;
            reminderForm.ShowDialog();
        }


        // 切換久坐提醒
        private void ToggleSitReminder(object sender, EventArgs e)
        {
            if (chkEnableSit.Checked)
            {
                sitTimer.Start();  // 啟動定時器
            }
            else
            {
                sitTimer.Stop();  // 停止定時器
            }
        }

        // 切換喝水提醒
        private void ToggleWaterReminder(object sender, EventArgs e)
        {
            if (chkEnableDrink.Checked)
            {
                waterTimer.Start();  // 啟動定時器
            }
            else
            {
                waterTimer.Stop();  // 停止定時器
            }
        }

        private void LoadReminderPositions()
        {
            if (File.Exists(ConfigFile))
            {
                string[] lines = File.ReadAllLines(ConfigFile);
                if (lines.Length >= 2)
                {
                    string[] sitPos = lines[0].Split(',');
                    string[] waterPos = lines[1].Split(',');
                    if (sitPos.Length == 2 && waterPos.Length == 2)
                    {
                        sitReminderPosition = new Point(int.Parse(sitPos[0]), int.Parse(sitPos[1]));
                        waterReminderPosition = new Point(int.Parse(waterPos[0]), int.Parse(waterPos[1]));
                    }
                }
            }
        }

        private void SaveReminderPositions()
        {
            using (StreamWriter writer = new StreamWriter(ConfigFile))
            {
                writer.WriteLine($"{sitReminderPosition.X},{sitReminderPosition.Y}");
                writer.WriteLine($"{waterReminderPosition.X},{waterReminderPosition.Y}");
            }
        }

        private void BtnUpdateTimers_Click(object sender, EventArgs e)
        {
            sitTimer.Interval = (int)nudSitReminder.Value * 60 * 1000;
            waterTimer.Interval = (int)nudWaterReminder.Value * 60 * 1000;

            MessageBox.Show("提醒時間已更新！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void SitTimer_Tick(object sender, EventArgs e)
        {
            if (!chkEnableSit.Checked)
            {
                return;
            }
            ShowReminderWindow("久坐提醒", "久坐請站起動一動！", sitReminderPosition, newPosition => sitReminderPosition = newPosition);
        }

        private void WaterTimer_Tick(object sender, EventArgs e)
        {
            if (!chkEnableDrink.Checked)
            {
                return;
            }
            ShowReminderWindow("喝水提醒", "時間到，請多喝水！", waterReminderPosition, newPosition => waterReminderPosition = newPosition);
        }

        private void NotifyIcon_DoubleClick(object sender, EventArgs e)
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            StartReminders();
            this.Hide(); // 隱藏主窗口
        }

        private void StartReminders()
        {
            sitTimer.Interval = (int)nudSitReminder.Value * 60 * 1000;
            waterTimer.Interval = (int)nudWaterReminder.Value * 60 * 1000;

            sitTimer.Start();
            waterTimer.Start();
        }

        private void StopReminders(object sender, EventArgs e)
        {
            sitTimer.Stop();
            waterTimer.Stop();
            notifyIcon.ShowBalloonTip(1000, "提醒停止", "已停止所有提醒", ToolTipIcon.Info);
        }

        private void ExitApplication(object sender, EventArgs e)
        {
            sitTimer.Stop();
            waterTimer.Stop();
            notifyIcon.Visible = false;
            Application.Exit();
        }

        private void ShowMainForm(object sender, EventArgs e)
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
        }

    }
}
