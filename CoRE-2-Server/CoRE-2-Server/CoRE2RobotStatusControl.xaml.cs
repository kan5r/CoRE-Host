using System;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Timers;
using System.Windows.Threading;
using System.Diagnostics;
using System.Collections.Generic;

namespace CoRE_2_Server
{
    /// <summary>
    /// CoRE2RobotStatusControl.xaml の相互作用ロジック
    /// </summary>
    public partial class CoRE2RobotStatusControl : UserControl
    {
        #region RobotStatusの定義
        public class RobotStatus
        {
            private readonly CoRE2RobotStatusControl _core2RobotStatusControl;
            public int team2idx;
            private string _teamName;
            private int _teamNo;
            private string _teamColor;
            private int _hp = 0;
            private int _maxHp = 0;
            private bool _destroyedFlag = false;
            private bool _powerOnFlag = false;
            private bool _invincivilityFlag = false;
            private TimeSpan _respawnTime;
            private List<string> _log = new List<string>();
            private int _damage = 0;
            private int _destroyedNum = 0;

            public RobotStatus(CoRE2RobotStatusControl instance) {
                _core2RobotStatusControl = instance;
            }

            public string TeamName {
                get { return _teamName; }
                set { _teamName = value; }
            }

            public int TeamNo {
                get { return _teamNo; }
                set {
                    _teamNo = value;
                    CoRE2ServerControl.Instance.Msgs.Robot[team2idx].TeamID = _teamNo;
                }
            }

            public string TeamColor {
                get { return _teamColor; }
                set {
                    _teamColor = value;
                    CoRE2ServerControl.Instance.Msgs.Robot[team2idx].TeamColor = _teamColor;
                }
            }

            public int HP {
                get { return _hp; }
                set {
                    _hp = value;
                    CoRE2ServerControl.Instance.Msgs.Robot[team2idx].HP = _hp;
                    Application.Current.Dispatcher.Invoke(() => {
                        _core2RobotStatusControl.HPBar.Value = _hp;
                        _core2RobotStatusControl.HPTextBox.Text = $"{_hp}/{_maxHp}";
                    });
                }
            }

            public int MaxHP {
                get { return _maxHp; }
                set {
                    _maxHp = value;
                    if (!Application.Current.Dispatcher.CheckAccess()) {
                        Application.Current.Dispatcher.Invoke(() => {
                            CoRE2ServerControl.Instance.Msgs.Robot[team2idx].MaxHP = _maxHp;
                            _core2RobotStatusControl.HPBar.Maximum = _maxHp;
                        });
                    } else {
                        CoRE2ServerControl.Instance.Msgs.Robot[team2idx].MaxHP = _maxHp;
                        _core2RobotStatusControl.HPBar.Maximum = _maxHp;
                    }
                }
            }

            public bool DestroyedFlag {
                get { return _destroyedFlag; }
                set {
                    _destroyedFlag = value;
                    CoRE2ServerControl.Instance.Msgs.Robot[team2idx].DeathFlag = Convert.ToInt32(_destroyedFlag);
                }
            }

            public bool PowerOnFlag {
                get { return _powerOnFlag; }
                set {
                    _powerOnFlag = value;
                    CoRE2ServerControl.Instance.Msgs.Robot[team2idx].DeathFlag = Convert.ToInt32(_destroyedFlag);
                }
            }

            private bool _invincibilityFlagPrev = false;
            public bool InvincibilityFlag {
                get { return _invincivilityFlag; }
                set {
                    _invincivilityFlag = value;
                    if (!_invincibilityFlagPrev && _invincivilityFlag)
                        AddRobotLog("Respawn & Invincible Time Start...");
                    else if (_invincibilityFlagPrev && !InvincibilityFlag)
                        AddRobotLog("Invincile Time End.");
                    _invincibilityFlagPrev = _invincivilityFlag;
                }
            }

            public TimeSpan RespawnTime {
                get { return _respawnTime; }
                set {
                    _respawnTime = value;

                    if (_respawnTime.TotalSeconds <= 0) {
                        Application.Current.Dispatcher.Invoke(() => {
                            _core2RobotStatusControl.RespawnTimeTextBlock.Text = "";
                            _core2RobotStatusControl.RespawnTimeTextBlock.IsEnabled = false;
                        });
                        CoRE2ServerControl.Instance.Msgs.Robot[team2idx].RespawnTime = "00:00";
                        return;
                    }

                    Application.Current.Dispatcher.Invoke(() => {
                        _core2RobotStatusControl.RespawnTimeTextBlock.Text = $"{_respawnTime.Seconds:00} sec.";
                    });
                    CoRE2ServerControl.Instance.Msgs.Robot[team2idx].RespawnTime = $"{_respawnTime.Minutes:00}:{_respawnTime.Seconds:00}";
                }
            }

            // ログ
            public List<string> Log {
                get { return _log; }
                set { _log = value; }
            }

            public void AddRobotLog(string text) {
                _log.Add($"[{CoRE2ServerControl.Instance.CurrentTime.Minutes:00}:{CoRE2ServerControl.Instance.CurrentTime.Seconds:00}:{CoRE2ServerControl.Instance.CurrentTime.Milliseconds / 100}]" + text + "\r\n");
                Application.Current.Dispatcher.Invoke(() => {
                    _core2RobotStatusControl.RobotLogTextBox.AppendText(_log[_log.Count - 1]);
                    _core2RobotStatusControl.RobotLogTextBox.ScrollToEnd();
                });
            }

            public int Damage {
                get { return _damage; }
                set {
                    _damage = value;
                    if (_core2RobotStatusControl._redBlue == (int)CoRE2ServerControl.TeamColor.RED) {
                        if (value == 0) CoRE2ServerControl.Instance.Msgs.RedReceivedDamage = _damage;
                        else CoRE2ServerControl.Instance.Msgs.RedReceivedDamage += 10; 
                        // CとDの計算
                        if (CoRE2ServerControl.Instance.Msgs.GameSystem == (int)CoRE2ServerControl.GameSystem.PRELIMINALY) {
                            Application.Current.Dispatcher.Invoke(() => {
                                CoRE2ServerControl.GetMainWindow().BlueScoreTextBox.Text = $"[Blue score] C: {3 * CoRE2ServerControl.Instance.Msgs.RedDeathCnt} pt.," +
                                                                                          $"D: {Clip(CoRE2ServerControl.Instance.Msgs.RedReceivedDamage / 10, 0, 10)} pt.";
                            });
                        }
                    } else {
                        if (value == 0) CoRE2ServerControl.Instance.Msgs.BlueReceivedDamage = _damage;
                        else CoRE2ServerControl.Instance.Msgs.BlueReceivedDamage += 10;
                        // AとBの計算
                        if (CoRE2ServerControl.Instance.Msgs.GameSystem == (int)CoRE2ServerControl.GameSystem.PRELIMINALY) {
                            Application.Current.Dispatcher.Invoke(() => {
                                CoRE2ServerControl.GetMainWindow().RedScoreTextBox.Text = $"[Red score] A: {5 * CoRE2ServerControl.Instance.Msgs.BlueDeathCnt} pt., " +
                                                                                          $"B: {Clip(CoRE2ServerControl.Instance.Msgs.BlueReceivedDamage / 10, 0, 6)} pt.";
                            });
                        }
                    }
                    Application.Current.Dispatcher.Invoke(() => {
                        _core2RobotStatusControl.DamageTakenTextBlock.Text = _damage.ToString();
                    });
                }
            }

            public int DestroyedNum {
                get { return _destroyedNum; }
                set {
                    _destroyedNum = value;
                    if (_core2RobotStatusControl._redBlue == (int)CoRE2ServerControl.TeamColor.RED) {
                        if (value == 0) CoRE2ServerControl.Instance.Msgs.RedDeathCnt = _destroyedNum;
                        else CoRE2ServerControl.Instance.Msgs.RedDeathCnt++;
                        // CとDの計算
                        if (CoRE2ServerControl.Instance.Msgs.GameSystem == (int)CoRE2ServerControl.GameSystem.PRELIMINALY) {
                            Application.Current.Dispatcher.Invoke(() => {
                                CoRE2ServerControl.GetMainWindow().BlueScoreTextBox.Text = $"[Blue score] C: {3 * CoRE2ServerControl.Instance.Msgs.RedDeathCnt} pt.," +
                                                                                          $"D: {Clip(CoRE2ServerControl.Instance.Msgs.RedReceivedDamage / 10, 0, 10)} pt.";
                            });
                        }
                    } else {
                        if (value == 0) CoRE2ServerControl.Instance.Msgs.BlueDeathCnt = _destroyedNum;
                        else CoRE2ServerControl.Instance.Msgs.BlueDeathCnt++;
                        // AとBの計算
                        if (CoRE2ServerControl.Instance.Msgs.GameSystem == (int)CoRE2ServerControl.GameSystem.PRELIMINALY) {
                            Application.Current.Dispatcher.Invoke(() => {
                                CoRE2ServerControl.GetMainWindow().RedScoreTextBox.Text = $"[Red score] A: {5 * CoRE2ServerControl.Instance.Msgs.BlueDeathCnt} pt., " +
                                                                                          $"B: {Clip(CoRE2ServerControl.Instance.Msgs.BlueReceivedDamage / 10, 0, 6)} pt.";
                            });
                        }
                    }

                    Application.Current.Dispatcher.Invoke(() => {
                        _core2RobotStatusControl.NumDestroysTextBlock.Text = _destroyedNum.ToString();
                    });
                }
            }

            private int Clip(int val, int min, int max) {
                return val < min ? min : val > max ? max : val;
            }
        };

        #endregion

        private enum CommunicationStartSeq {
            NONE,
            OPEN,
            WAIT_POWER_ON,
            SET_CH,
            WAIT_SETTING_CH,
            SET_RUN,
            WAIT_SETTING_RUN,
            SEND,
            WAIT_CLIENT_DATA,
            CLIENT_OK,
        }

        public RobotStatus _robot;
        private readonly SerialPort _sp;
        private  CommunicationStartSeq _startSeq = CommunicationStartSeq.NONE;
        private string _waitData = "";
        private int _isCommunicating = 0;
        private List<string> _sendData;
        private string _receivedDataPrev = "";

        private DateTime _startTime;
        private TimeSpan _remainingTime;

        // 復活タイマー
        private System.Timers.Timer _respawnTimer;

        // 無敵タイマー
        private System.Timers.Timer _invincibilityTimer;


        public CoRE2RobotStatusControl() {
            InitializeComponent();

            #region シリアルポート関係の設定
            _sp = new SerialPort {
                BaudRate = 115200,
                DataBits = 8,
                Parity = Parity.None,
                StopBits = StopBits.One,
                Handshake = Handshake.None,
                Encoding = System.Text.Encoding.ASCII,
                NewLine = "\r\n",
                ReadTimeout = 300,
            };
            _sp.DataReceived += SerialPort_DataReceived;

            COMPortWatcher.Instance.PortsUpdated += UpdateCOMPortsList;
            UpdateCOMPortsList();
            #endregion

            TeamNameComboBox.Items.Clear();
            foreach (string tn in CoRE2ServerControl.Instance.TeamName)
                TeamNameComboBox.Items.Add(tn);

            _robot = CreateRobotStatusClass();
            _sendData = new List<string>();

            // イベント
            CoRE2ServerControl.Instance.UpdateEvent += UpdateRobotStatus;
            CoRE2ServerControl.Instance.GameStartEvent += DataReceivedEventStop;
            //CoRE2ServerControl.Instance.GameResetEvent += 
            CoRE2ServerControl.Instance.ClearDataEvent += ClearData;


            _respawnTimer = new System.Timers.Timer();
            _respawnTimer.Interval = 50;
            _respawnTimer.Elapsed += OnRespawnTimedEvent;

            _invincibilityTimer = new System.Timers.Timer();
            _invincibilityTimer.Interval = 50;
            _invincibilityTimer.Elapsed += OnInvincibilityTimedEvent;
        }

        public RobotStatus CreateRobotStatusClass() {
            return new RobotStatus(this);
        }

        private void Send(string text) {
            byte[] sendDataByte = System.Text.Encoding.ASCII.GetBytes(text + "\r\n");
            foreach (byte b in sendDataByte) {
                try {
                    _sp.Write(new byte[] { b }, 0, 1);
                    // 早すぎると基板側の受信が失敗するため一文字ごとに待機
                    Thread.Sleep(2);
                } catch (Exception e) {
                    ; 
                }
            }
        }

        private void UpdateRobotStatus() {
            // 接続してなければスキップ
            if (!_sp.IsOpen) return;

            // クライアント基板との通信に成功していなければスキップ
            // すなわち、ゲーム前でも接続がOKになれば通信を始める
            if (_startSeq != CommunicationStartSeq.CLIENT_OK) return;

            //　別スレッドで実行中ならばスキップ
            if (Interlocked.CompareExchange(ref _isCommunicating, 1, 0) != 0) return;

            try {
                // 送信データの作成
                _sendData.Clear();
                _sendData.Add("02");                                              // 機能ID
                _sendData.Add(BitShift(_robot.DestroyedFlag, 1).ToString("X2"));  // [b1:撃破フラグ]
                _sendData.Add((BitShift(SelectColor(), 4)|BitShift(_redBlue, 0)).ToString("X2")); // [b4...7: チームカラー]
                _sendData.Add((100 * _robot.HP / _robot.MaxHP).ToString("X2"));   // ヒットポイントのパーセンテージ (0x00~0x64)
                _sendData.Add("00");                                              // 以下未使用
                _sendData.Add("00");
                _sendData.Add("00");
                _sendData.Add("00");

                //Debug.WriteLine("send data: "+string.Join(",", _sendData));

                // データを送信
                _sp.DiscardInBuffer();
                Send(string.Join("", _sendData));
                byte[] sendDataByte = System.Text.Encoding.ASCII.GetBytes(string.Join("", _sendData) + "\r\n");
                foreach (byte b in sendDataByte) {
                    _sp.Write(new byte[] { b }, 0, 1);
                    // 早すぎると基板側の受信が失敗するため一文字ごとに待機
                    Thread.Sleep(1);
                }
                //_sp.WriteLine(string.Join("", _sendData));

                try {
                    // データを受信
                    string receivedDataString = _sp.ReadLine();

                    /*string receivedDataString = "";
                    while (true) {
                        string d = _sp.ReadExisting();
                        receivedDataString += d;
                        Dispatcher.Invoke(() => {
                            ReceivedDataTextBox.AppendText(d);
                            ReceivedDataTextBox.ScrollToEnd();
                        });
                        Thread.Sleep(1);
                        if (receivedDataString.Contains("\r\n")) break;
                    }*/

                    Debug.WriteLine("received: " + receivedDataString);
                    if (receivedDataString.Length < 8) return; // たまにOKやNGが返ってくるので除去
                    if (receivedDataString.Contains("00,00,00,00,00,00,00")) return;

                    // 受信データの復号
                    Dispatcher.Invoke(() => {
                        ReceivedDataTextBox.AppendText(receivedDataString + "\r\n");
                        ReceivedDataTextBox.ScrollToEnd();
                    });

                    receivedDataString = receivedDataString.TrimEnd('\r', '\n');  // ReadLineはすでに省かれている？念のため
                    receivedDataString = receivedDataString.TrimStart('>');
                    try {
                        int[] receivedDataInt = receivedDataString.Split(':')[1].Split(',').Select(part => Convert.ToInt32(part, 16)).ToArray();
                        // ダメージパネルの判定
                        if (!_robot.InvincibilityFlag) {
                            if (receivedDataString == _receivedDataPrev) return; // 完全に一致した場合は，同クロックのデータである可能性が高いためスキップ
                            _receivedDataPrev = receivedDataString;
                            for (int i = 0; i < 4; i++) {
                                if (BitHigh(receivedDataInt[3], i)) {
                                    _robot.AddRobotLog("Hit");
                                    _robot.HP -= CoRE2ServerControl.Instance.HitDamage;
                                    _robot.Damage += CoRE2ServerControl.Instance.HitDamage;
                                }
                            }
                        }

                        if (_robot.HP <= 0) {
                            _robot.HP = 0;
                            if (!_robot.DestroyedFlag) {
                                _robot.AddRobotLog("Destroyed");
                                _robot.DestroyedFlag = true;
                                _robot.PowerOnFlag = false;
                                _robot.DestroyedNum++;
                                if (CoRE2ServerControl.Instance.Msgs.GameSystem != (int)CoRE2ServerControl.GameSystem.PRELIMINALY
                                    && !CoRE2ServerControl.Instance.GameEndFlag)
                                    StartRespawnTimer();
                            }
                        }
                    } catch (Exception e) {
                        ;
                    }
                } catch (TimeoutException e) {
                    Debug.WriteLine("Read timeout");
                    Dispatcher.Invoke(() => {
                        ReceivedDataTextBox.AppendText("Read timeout \r\n");
                        ReceivedDataTextBox.ScrollToEnd();
                    });
                }          
            } finally {
                Interlocked.Exchange(ref _isCommunicating, 0);
            }
        }
         
        #region 依存プロパティの設定
        public static readonly DependencyProperty ControlPanelColorProperty = DependencyProperty.Register("ControlPanelColor", typeof(string), typeof(CoRE2RobotStatusControl), new PropertyMetadata("Red"));


        public static readonly DependencyProperty ControlPanelLabelProperty = DependencyProperty.Register("ControlPanelLabel", typeof(string), typeof(CoRE2RobotStatusControl), new PropertyMetadata("Blue/Red #"));
        public string ControlPanelLabel {
            get { return (string)GetValue(ControlPanelLabelProperty); }
            set { SetValue(ControlPanelLabelProperty, value); }
        }

        public string ControlPanelColor {
            get { return (string)GetValue(ControlPanelColorProperty); }
            set { SetValue(ControlPanelColorProperty, value); }
        }

        #endregion

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e) {
            string data = _sp.ReadExisting();
            Dispatcher.Invoke(() => {
                ReceivedDataTextBox.AppendText(data);
                ReceivedDataTextBox.ScrollToEnd();
            });

            // 手動接続のときrunコマンドを送信したらOKにする
            /*if (!CoRE2ServerControl.Instance.AutoConnect) { 
                Dispatcher.Invoke(() =>　{
                    if (ReceivedDataTextBox.Text.Contains("run")) {
                        _startSeq = CommunicationStartSeq.CLIENT_OK;
                        RespawnButton.IsEnabled = true;
                        DestroyButton.IsEnabled = true;
                        PunishButton.IsEnabled = true;
                    }
                });
                _sp.DataReceived -= SerialPort_DataReceived;
                return;
            }*/

            /*_waitData += data;
            if (_startSeq == CommunicationStartSeq.WAIT_POWER_ON) {
                if (_waitData.Contains(">")) {
                    Dispatcher.Invoke(() => {
                        HostStatusTextBox.Text = "Setting CH...";
                    });
                    _waitData = "";
                    _sp.DiscardInBuffer();
                    Send($"setCh {CoRE2ServerControl.Instance.TeamCh[_robot.TeamName]}");
                    _startSeq = CommunicationStartSeq.WAIT_SETTING_CH;
                }
            } else if (_startSeq  == CommunicationStartSeq.WAIT_SETTING_CH) {
                if (_waitData.Contains("OK")) {
                    Dispatcher.Invoke(() => {
                        HostStatusTextBox.Text = "Setting run...";
                    });
                    _waitData = "";
                    _sp.DiscardInBuffer();
                    Send("run");
                    _startSeq = CommunicationStartSeq.WAIT_SETTING_RUN;
                }
            } else if (_startSeq == CommunicationStartSeq.WAIT_SETTING_RUN) {
                if (_waitData.Contains("run")) {
                    Dispatcher.Invoke(() => {
                        HostStatusTextBox.Text = "Clinet OK";
                        RespawnButton.IsEnabled = true;
                        DestroyButton.IsEnabled = true;
                        PunishButton.IsEnabled = true;
                    });
                    _waitData = "";
                    _sp.DiscardInBuffer();
                    _sp.DataReceived -= SerialPort_DataReceived;
                }
            } */
        }
        private void UpdateCOMPortsList() {
            Dispatcher.Invoke(() => {
                ComPortSelectionComboBox.Items.Clear();
                foreach (string port in COMPortWatcher.Instance.GetAvailablePorts())
                    ComPortSelectionComboBox.Items.Add(port);
            });
        }

        private void DataReceivedEventStop() {
            _sp.DataReceived -= SerialPort_DataReceived;
        }

        private void ClearData() {
            _robot.HP = _robot.MaxHP;
            _robot.DestroyedFlag = false;
            _robot.PowerOnFlag = true;
            _robot.InvincibilityFlag = false;
            _robot.RespawnTime = TimeSpan.Zero;
            _robot.Damage = 0;
            _robot.DestroyedNum = 0;
            _robot.Log.Clear();
            ReceivedDataTextBox.Clear();
            _respawnTimer.Stop();
            _invincibilityTimer.Stop();
            _sp.Close();
            _sp.DataReceived += SerialPort_DataReceived;
            ConnectButton.Content = "Connect";
            _startSeq = CommunicationStartSeq.NONE;
    }

        private void ConnectButton_Click(object sender, RoutedEventArgs e) {
            if (!_sp.IsOpen) {
                if (ComPortSelectionComboBox.SelectedItem == null) {
                    MessageBox.Show("COMポートを選択してください", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _sp.PortName = ComPortSelectionComboBox.SelectedItem.ToString();
                try {
                    // ホスト基板と接続
                    _sp.Open();

                    /*if (CoRE2ServerControl.Instance.AutoConnect) {
                        _startSeq = CommunicationStartSeq.WAIT_POWER_ON;
                        _waitData = "";
                    } else {
                        SendButton.IsEnabled = true;
                    }*/

                    HostStatusTextBox.Text = "Connected Host";
                    ConnectButton.Content = "Disconn.";
                    SetChButton.IsEnabled = true;
                } catch (Exception ex) {
                    MessageBox.Show($"{ControlPanelLabel}: ホスト基板との接続に失敗しました", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    ConnectButton.Content = "Connect";
                }
            } else {
                _sp.Close();
                ConnectButton.Content = "Connect";
                SetChButton.IsEnabled = false;
                RunButton.IsEnabled = false;
                RespawnButton.IsEnabled = false;
                DestroyButton.IsEnabled = false;
                PunishButton.IsEnabled = false;
                SendButton.IsEnabled= false;
                _startSeq = CommunicationStartSeq.NONE;
            }
        }

        private void SendButton_Click(object sender, RoutedEventArgs e) {
            if (_sp.IsOpen) {
                byte[] data = System.Text.Encoding.ASCII.GetBytes(SendDataTextBox.Text.ToString() + "\r\n");
                foreach (byte b in data) {
                    _sp.Write(new byte[] { b }, 0, 1);
                    Thread.Sleep(2);
                }

                if (SendDataTextBox.Text == "run") {
                    _startSeq = CommunicationStartSeq.CLIENT_OK;
                    RespawnButton.IsEnabled = true;
                    DestroyButton.IsEnabled = true;
                    PunishButton.IsEnabled = true;
                    _sp.DataReceived -= SerialPort_DataReceived;
                }

                SendDataTextBox.Clear();
            } else {
                MessageBox.Show("先にホスト基板と接続してください", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void RespawnButton_Click(object sender, RoutedEventArgs e) {
            _robot.HP = CoRE2ServerControl.Instance.RespawnHP;
            _robot.DestroyedFlag = false;
            _robot.PowerOnFlag = true;
            _robot.RespawnTime = TimeSpan.Zero;
            _robot.InvincibilityFlag = true;
            _respawnTimer.Stop();

            _remainingTime = TimeSpan.FromSeconds(CoRE2ServerControl.Instance.InvincibleTime);
            _startTime = DateTime.Now;
            _invincibilityTimer.Start();
        }

        private void DestroyButton_Click(object sender, RoutedEventArgs e) {
            _robot.HP = 0;
            _robot.DestroyedFlag = true;
            _robot.PowerOnFlag = false;
            _robot.DestroyedNum++;
            _robot.AddRobotLog("Destroyed by Server");
            if (CoRE2ServerControl.Instance.Msgs.GameSystem != (int)CoRE2ServerControl.GameSystem.PRELIMINALY)
                StartRespawnTimer();
        }

        private void PunishButton_Click(object sender, RoutedEventArgs e) {
            _robot.HP -= CoRE2ServerControl.Instance.PunishDamage;
            _robot.Damage += CoRE2ServerControl.Instance.PunishDamage;

            if (_robot.HP <= 0) {
                _robot.HP = 0;
                _robot.DestroyedFlag = true;
                _robot.PowerOnFlag = false;
                _robot.DestroyedNum++;
                _robot.AddRobotLog("Destroyed");

                if (CoRE2ServerControl.Instance.Msgs.GameSystem != (int)CoRE2ServerControl.GameSystem.PRELIMINALY)
                    StartRespawnTimer();
            }
        }

        private void TeamNameComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            _robot.TeamName = TeamNameComboBox.SelectedItem.ToString();
            _robot.TeamNo = TeamNameComboBox.SelectedIndex;
            _robot.TeamColor = ControlPanelLabel.Replace(" ", "");
            _startSeq = CommunicationStartSeq.NONE;

            if (_robot.TeamName != "NONE") {
                ConnectButton.IsEnabled = true;
            } else {
                ConnectButton.IsEnabled= false;
            }
        }

        private bool BitHigh(int data, int i) {
            return ((data & (0b1 << i)) >> i != 0) ? true : false;
        }

        private int BitShift(bool data, int shift) {
            int b = data ? 1 : 0;
            return b << shift;
        }

        private int BitShift(int data, int shift) {
            return data << shift;
        }

        private int _redBlue = 0;
        private int RedBlue() {
            return ControlPanelLabel[0] == 'R' ? (int)CoRE2ServerControl.TeamColor.RED : (int)CoRE2ServerControl.TeamColor.BLUE;
        }

        private int SelectColor() {
            if (_robot.DestroyedFlag) return (int)CoRE2ServerControl.TeamColor.YELLOW;
            else if (_robot.InvincibilityFlag) return (int)CoRE2ServerControl.TeamColor.WHITE;
            else return _redBlue;
        }

        private void StartRespawnTimer() {
            _remainingTime = TimeSpan.FromSeconds(CoRE2ServerControl.Instance.RespawnTime);
            _startTime = DateTime.Now;
            _respawnTimer.Start();
            Dispatcher.Invoke(() => {
                RespawnTimeTextBlock.IsEnabled = true;
            });
        }

        private void OnRespawnTimedEvent(object source, ElapsedEventArgs e) {
            var timePassed = DateTime.Now - _startTime;
            _robot.RespawnTime = _remainingTime - timePassed;

            if (_robot.RespawnTime.TotalSeconds <= 0) {
                _respawnTimer.Stop();

                _robot.HP = CoRE2ServerControl.Instance.RespawnHP;
                _robot.DestroyedFlag = false;
                _robot.PowerOnFlag = true;
                _robot.InvincibilityFlag = true;

                _remainingTime = TimeSpan.FromSeconds(CoRE2ServerControl.Instance.InvincibleTime);
                _startTime = DateTime.Now;
                _invincibilityTimer.Start();

                return;
            }
        }

        private void OnInvincibilityTimedEvent(object? sender, ElapsedEventArgs e) {
            var timePassed = DateTime.Now - _startTime;
            var currentRemainingTime = _remainingTime - timePassed;

            if (currentRemainingTime.TotalSeconds <= 0) {
                _invincibilityTimer.Stop();
                _robot.InvincibilityFlag = false;
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e) {
            TeamNameComboBox.SelectedIndex = 0;
            ComPortSelectionComboBox.SelectedIndex = -1;
            ConnectButton.IsEnabled = false;
            SetChButton.IsEnabled = false;
            RunButton.IsEnabled = false;
            //RespawnButton.IsEnabled = false;
            //DestroyButton.IsEnabled = false;
            //PunishButton.IsEnabled = false;
            SendButton.IsEnabled = false;
            _robot.team2idx = Team2Idx();
            _redBlue = RedBlue();
        }

        private int Team2Idx() {
            string[] tn = { "Red 1", "Red 2", "Red 3", "Blue 1", "Blue 2", "Blue 3" };
            for (int i = 0; i < 6; i++) {
                if (ControlPanelLabel == tn[i])
                    return i;
            }
            return -1;
        }

        private void UserControl_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e) {
            int maxHp = 0;
            if (this.IsEnabled) {
                if (CoRE2ServerControl.Instance.Msgs.GameSystem == (int)CoRE2ServerControl.GameSystem.PRELIMINALY)
                    maxHp = _redBlue == (int)CoRE2ServerControl.TeamColor.RED ? CoRE2ServerControl.Instance.PreRedMaxHP : _robot.MaxHP = CoRE2ServerControl.Instance.PreBlueMaxHP;
                else
                    maxHp = CoRE2ServerControl.Instance.MaxHP;
            }
            _robot.MaxHP = maxHp;
            _robot.HP = maxHp;
        }

        private void SetChButton_Click(object sender, RoutedEventArgs e) {
            Send($"setCh {CoRE2ServerControl.Instance.TeamCh[_robot.TeamName]}");
            _startSeq = CommunicationStartSeq.SET_CH;
            RunButton.IsEnabled = true;
        }

        private void RunButton_Click(object sender, RoutedEventArgs e) {
            Send("run");

            _startSeq = CommunicationStartSeq.CLIENT_OK;
            RespawnButton.IsEnabled = true;
            DestroyButton.IsEnabled = true;
            PunishButton.IsEnabled = true;
            _sp.DataReceived -= SerialPort_DataReceived;
        }
    }
}