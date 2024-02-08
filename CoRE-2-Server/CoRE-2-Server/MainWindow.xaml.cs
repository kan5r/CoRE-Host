using System.Threading;
using System.Timers;
using System.Windows;


namespace CoRE_2_Server
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow() {
            InitializeComponent();
        }

        private void CoRE2RobotStatusControl_Loaded(object sender, RoutedEventArgs e) {
            
        }

        private void GameStartButton_Click(object sender, RoutedEventArgs e) {
            CoRE2ServerControl.Instance.GameStart();
        }

        private void GameResetButton_Click(object sender, RoutedEventArgs e) {
            CoRE2ServerControl.Instance.GameReset();
        }


        private void SettingStartButton_Click(object sender, RoutedEventArgs e) {
            CoRE2ServerControl.Instance.SettingTimeStart();
        }

        private void TimeoutButton_Click(object sender, RoutedEventArgs e) {
            CoRE2ServerControl.Instance.Timeout();
            TimeoutButton.IsEnabled = false;
        }

        private void TechnicalTimeOutButton_Click(object sender, RoutedEventArgs e) {
            CoRE2ServerControl.Instance.TechnicalTimeout();
        }

        private void SettingResumeButton_Click(object sender, RoutedEventArgs e) {
            CoRE2ServerControl.Instance.SettingTimeResume();
        }

        private void SettingResetButton_Click(object sender, RoutedEventArgs e) {
            CoRE2ServerControl.Instance.SettingTimeReset();
        }
        private void SettingSkipButton_Click(object sender, RoutedEventArgs e) {
            CoRE2ServerControl.Instance.SettingTimeSkip();
        }

        private void PreliminaryRadioButton_Checked(object sender, RoutedEventArgs e) {
            CoRE2ServerControl.Instance.Msgs.GameSystem = (int)CoRE2ServerControl.GameSystem.PRELIMINALY;
            RedScoreTextBox.IsEnabled = true;
            BlueScoreTextBox.IsEnabled = true;

            AllControlPanelDisabled();
            Red1.IsEnabled = true;
            Red2.IsEnabled = false;
            Red3.IsEnabled = false;
            Blue1.IsEnabled = true;
            Blue2.IsEnabled = true;
            Blue3.IsEnabled = true;

            PreliminaryRadioButton.IsEnabled = true;
            SettingStartButton.IsEnabled = true;
            SettingResetButton.IsEnabled = false;
            TimeoutButton.IsEnabled = false;
            TechnicalTimeOutButton.IsEnabled = false;
            SettingResumeButton.IsEnabled = false;
            SettingSkipButton.IsEnabled = false;
            GameStartButton.IsEnabled = true;
            GameResetButton.IsEnabled = false;

            CoRE2ServerControl.Instance.Msgs.GameStatus = (int)CoRE2ServerControl.GameStatus.NONE;
            int time = CoRE2ServerControl.Instance.PreSettingTimeMin;
            string timeText = $"{time:D2}:00";
            GameCountDown.Text = timeText;
            CoRE2ServerControl.Instance.Msgs.GameTime = timeText;
            CoRE2ServerControl.Instance.Msgs.SettingTime = timeText;
        }

        private void SemifinalsRadioButton_Checked(object sender, RoutedEventArgs e) {
            CoRE2ServerControl.Instance.Msgs.GameSystem = (int)CoRE2ServerControl.GameSystem.SEMIFINALS;
            
            RedScoreTextBox.IsEnabled = false;
            BlueScoreTextBox.IsEnabled = false;

            AllControlPanelDisabled();
            Red1.IsEnabled = true;
            Red2.IsEnabled = true;
            Red3.IsEnabled = false;
            Blue1.IsEnabled = true;
            Blue2.IsEnabled = true;
            Blue3.IsEnabled = false;

            PreliminaryRadioButton.IsEnabled = true;
            SettingStartButton.IsEnabled = true;
            SettingResetButton.IsEnabled = false;
            TimeoutButton.IsEnabled = false;
            TechnicalTimeOutButton.IsEnabled = false;
            SettingResumeButton.IsEnabled = false;
            SettingSkipButton.IsEnabled = false;
            GameStartButton.IsEnabled = true;
            GameResetButton.IsEnabled = false;

            CoRE2ServerControl.Instance.Msgs.GameStatus = (int)CoRE2ServerControl.GameStatus.NONE;
            int settingTime = CoRE2ServerControl.Instance.Added3min ? CoRE2ServerControl.Instance.AllianceMtgTimeMin + CoRE2ServerControl.Instance.SettingTimeMin : CoRE2ServerControl.Instance.SettingTimeMin;
            string timeText = $"{settingTime:D2}:00";
            GameCountDown.Text = timeText;
            CoRE2ServerControl.Instance.Msgs.GameTime = timeText;
            CoRE2ServerControl.Instance.Msgs.SettingTime = timeText;
        }

        private void FinalsRadioButton_Checked(object sender, RoutedEventArgs e) {
            CoRE2ServerControl.Instance.Msgs.GameSystem = (int)CoRE2ServerControl.GameSystem.FINALS;
            RedScoreTextBox.IsEnabled = false;
            BlueScoreTextBox.IsEnabled = false;

            AllControlPanelDisabled();
            Red1.IsEnabled = true;
            Red2.IsEnabled = true;
            Red3.IsEnabled = true;
            Blue1.IsEnabled = true;
            Blue2.IsEnabled = true;
            Blue3.IsEnabled = true;

            PreliminaryRadioButton.IsEnabled = true;
            SettingStartButton.IsEnabled = true;
            SettingResetButton.IsEnabled = false;
            TimeoutButton.IsEnabled = false;
            TechnicalTimeOutButton.IsEnabled = false;
            SettingResumeButton.IsEnabled = false;
            SettingSkipButton.IsEnabled = false;
            GameStartButton.IsEnabled = true;
            GameResetButton.IsEnabled = false;

            CoRE2ServerControl.Instance.Msgs.GameStatus = (int)CoRE2ServerControl.GameStatus.NONE;
            int settingTime = CoRE2ServerControl.Instance.Added3min ? CoRE2ServerControl.Instance.AllianceMtgTimeMin + CoRE2ServerControl.Instance.SettingTimeMin : CoRE2ServerControl.Instance.SettingTimeMin;
            string timeText = $"{settingTime:D2}:00";
            GameCountDown.Text = timeText;
            CoRE2ServerControl.Instance.Msgs.GameTime = timeText;
            CoRE2ServerControl.Instance.Msgs.SettingTime = timeText;
        }

        /// <summary>
        /// control changedを発生させるため
        /// </summary>
        private void AllControlPanelDisabled() {
            Red1.IsEnabled = false;
            Red2.IsEnabled = false;
            Red3.IsEnabled=false;
            Blue1.IsEnabled = false;
            Blue2.IsEnabled = false;
            Blue3.IsEnabled = false;
        }

        private void PlayerScreenToggleButton_CheckedChanged(object sender, RoutedEventArgs e) {
            if (PlayerScreenToggleButton.IsChecked == true) {
                CoRE2ServerControl.Instance._processCommTimer.Start();
            } else {
                CoRE2ServerControl.Instance._processCommTimer.Stop();
            }
        }

        private void AutoConnectToggleButton_CheckedChanged(object sender, RoutedEventArgs e) {
            /*if (!AutoConnectToggleButton.IsChecked == true) {
                CoRE2ServerControl.Instance.AutoConnect = true;
            } else {
                CoRE2ServerControl.Instance.AutoConnect = false;
            }*/
            ;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            PlayerScreenToggleButton.IsChecked = true;
            AutoConnectToggleButton.IsChecked = true;
        }

        private void AllClearButton_Click(object sender, RoutedEventArgs e) {
            CoRE2ServerControl.Instance.ClearData();
            TimerLabel.Text = "SETTING READY?";
            int time = CoRE2ServerControl.Instance.AllianceMtgTimeMin + CoRE2ServerControl.Instance.SettingTimeMin;
            string timeText = $"{time:D2}:00";
            GameCountDown.Text = timeText;
            CoRE2ServerControl.Instance.Msgs.GameTime = timeText;
            CoRE2ServerControl.Instance.Msgs.SettingTime = timeText;
        }
        private void Add3MinButton_Click(object sender, RoutedEventArgs e) {
            CoRE2ServerControl.Instance.Add3minSetting();
            int time = CoRE2ServerControl.Instance.AllianceMtgTimeMin + CoRE2ServerControl.Instance.SettingTimeMin;
            string timeText = $"{time:D2}:00";
            GameCountDown.Text = timeText;
            CoRE2ServerControl.Instance.Msgs.GameTime = timeText;
            CoRE2ServerControl.Instance.Msgs.SettingTime = timeText;
        }
    }
}
