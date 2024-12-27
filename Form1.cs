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

            // ���f��l��
            this.Text = "�[���P�ܤ�����";
            this.Size = new Size(330, 180);
            this.StartPosition = FormStartPosition.CenterScreen;

            // �[������
            Label lblSit = new Label { Text = "�[������ (����):", Location = new Point(20, 20), AutoSize = true };
            nudSitReminder = new NumericUpDown { Location = new Point(150, 20), Width = 100, Minimum = 1, Maximum = 120, Value = 30 };

            chkEnableSit = new CheckBox
            {
                Name = "chkEnableSit",
                Text = "�ҥ�",
                Location = new Point(260, 20),
                AutoSize = true
            };
            this.Controls.Add(chkEnableSit);

            // �ܤ�����
            Label lblWater = new Label { Text = "�ܤ����� (����):", Location = new Point(20, 60), AutoSize = true };
            nudWaterReminder = new NumericUpDown { Location = new Point(150, 60), Width = 100, Minimum = 1, Maximum = 120, Value = 10 };

            chkEnableDrink = new CheckBox
            {
                Name = "chkEnableDrink",
                Text = "�ҥ�",
                Location = new Point(260, 60),
                AutoSize = true
            };
            this.Controls.Add(chkEnableDrink);
            chkEnableDrink.Checked = true;

            // ��s�������s
            btnUpdateTimers = new Button
            {
                Text = "��s�����ɶ�",
                Location = new Point(80, 100),
                Size = new Size(120, 30)
            };
            btnUpdateTimers.Click += BtnUpdateTimers_Click;

            // �K�[����쵡��
            this.Controls.Add(lblSit);
            this.Controls.Add(nudSitReminder);
            this.Controls.Add(lblWater);
            this.Controls.Add(nudWaterReminder);
            this.Controls.Add(btnUpdateTimers);

            // NotifyIcon �]�m
            notifyIcon = new NotifyIcon
            {
                Icon = SystemIcons.Information,
                Visible = true,
                Text = "�[���P�ܤ�����"
            };
            notifyIcon.DoubleClick += NotifyIcon_DoubleClick;

            // �k����
            ContextMenuStrip contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("��ܥD�e��", null, ShowMainForm);
            contextMenu.Items.Add("�����", null, StopReminders);
            contextMenu.Items.Add("�h�X", null, ExitApplication);
            notifyIcon.ContextMenuStrip = contextMenu;

            // Timer ��l��
            sitTimer = new Timer();
            sitTimer.Tick += SitTimer_Tick;

            waterTimer = new Timer();
            waterTimer.Tick += WaterTimer_Tick;

            // ���f�[���ɱҰʴ���
            this.Load += Form1_Load;
        }

        private void BtnStart_Click(object sender, EventArgs e)
        {
            sitTimer.Interval = (int)nudSitReminder.Value * 60 * 1000;
            waterTimer.Interval = (int)nudWaterReminder.Value * 60 * 1000;

            sitTimer.Start();
            waterTimer.Start();

            MessageBox.Show("�����w�ҰʡA�{�Ǥw�̤p�Ʀܨt�Φ��L", "����", MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.Hide();
        }

        private void BtnStop_Click(object sender, EventArgs e)
        {
            sitTimer.Stop();
            waterTimer.Stop();
            MessageBox.Show("�����w����", "����", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ShowReminderWindow(string title, string message, Point initialPosition, Action<Point> savePositionCallback)
        {
            // �p�G�w�g�����������A������
            if (currentReminderForm != null)
            {
                currentReminderForm.Close();
                currentReminderForm = null;
            }

            // ��l�Ʒs����������
            Form reminderForm = new Form
            {
                Text = title,
                Size = new Size(300, 180),
                StartPosition = FormStartPosition.Manual,
                FormBorderStyle = FormBorderStyle.FixedToolWindow
            };

            // �]�w������l��m
            if (initialPosition == Point.Empty)
            {
                var screenBounds = Screen.PrimaryScreen.WorkingArea;
                initialPosition = new Point(
                    screenBounds.Left + (screenBounds.Width - reminderForm.Width) / 2,
                    screenBounds.Top + (screenBounds.Height - reminderForm.Height) / 2
                );
            }
            reminderForm.Location = initialPosition;

            // �K�[��������
            Label lblMessage = new Label
            {
                Text = message,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Arial", 12)
            };
            reminderForm.Controls.Add(lblMessage);

            // �ƹ��ƥ�B�z�A�Ω��ʵ���
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

            // ���������ɫO�s��m
            reminderForm.FormClosed += (s, e) =>
            {
                savePositionCallback(reminderForm.Location);
                currentReminderForm = null; // ����������M���ޥ�
            };

            // ��ܷs����������
            currentReminderForm = reminderForm;
            reminderForm.TopMost = true;
            reminderForm.ShowDialog();
        }


        // �����[������
        private void ToggleSitReminder(object sender, EventArgs e)
        {
            if (chkEnableSit.Checked)
            {
                sitTimer.Start();  // �Ұʩw�ɾ�
            }
            else
            {
                sitTimer.Stop();  // ����w�ɾ�
            }
        }

        // �����ܤ�����
        private void ToggleWaterReminder(object sender, EventArgs e)
        {
            if (chkEnableDrink.Checked)
            {
                waterTimer.Start();  // �Ұʩw�ɾ�
            }
            else
            {
                waterTimer.Stop();  // ����w�ɾ�
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

            MessageBox.Show("�����ɶ��w��s�I", "���\", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void SitTimer_Tick(object sender, EventArgs e)
        {
            if (!chkEnableSit.Checked)
            {
                return;
            }
            ShowReminderWindow("�[������", "�[���Я��_�ʤ@�ʡI", sitReminderPosition, newPosition => sitReminderPosition = newPosition);
        }

        private void WaterTimer_Tick(object sender, EventArgs e)
        {
            if (!chkEnableDrink.Checked)
            {
                return;
            }
            ShowReminderWindow("�ܤ�����", "�ɶ���A�Цh�ܤ��I", waterReminderPosition, newPosition => waterReminderPosition = newPosition);
        }

        private void NotifyIcon_DoubleClick(object sender, EventArgs e)
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            StartReminders();
            this.Hide(); // ���åD���f
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
            notifyIcon.ShowBalloonTip(1000, "��������", "�w����Ҧ�����", ToolTipIcon.Info);
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
