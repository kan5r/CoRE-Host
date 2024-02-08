using Server;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text.Json;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Threading;
using System.Security.Policy;
using System.Security.Cryptography;


namespace CoRE_2_Server
{
    public class CoRE2ServerControl
    {
        // シングルトン
        private static readonly Lazy<CoRE2ServerControl> _instance = new Lazy<CoRE2ServerControl>(() => new CoRE2ServerControl());
        public static CoRE2ServerControl Instance => _instance.Value;


        // 出場チーム
        public string[] TeamName = { "NONE",
                                     "[ID1] FRENTE-Selva",
                                     "[ID2] FRENTE-Cielo",
                                     "[ID3] FRENTE-Rosa",
                                     "[ID4] VERTEX-Zeta",
                                     "[ID5] VERTEX-Gamma",
                                     "[ID6] GIRASOLE-Volta",
                                     "[ID7] GIRASOLE-Gauss",
                                     "[ID8] KT-tokitama"
        };

        public Dictionary<string, int> TeamCh = new Dictionary<string, int> {
            {"NONE", -1},  // "NONE",
            {"[ID1] FRENTE-Selva", 15},  // "[ID1] FRENTE-Selva",
            {"[ID2] FRENTE-Cielo", 16},  // "[ID2] FRENTE-Cielo",
            {"[ID3] FRENTE-Rosa", 17},  // "[ID3] FRENTE-Rosa",
            {"[ID4] VERTEX-Zeta", 21},  // "[ID4] VERTEX-Zeta",
            {"[ID5] VERTEX-Gamma", 20},  // "[ID5] VERTEX-Gamma",
            {"[ID6] GIRASOLE-Volta", 18},  // "[ID6] GIRASOLE-Volta",
            {"[ID7] GIRASOLE-Gauss", 19},  // "[ID7] GIRASOLE-Gauss",
            {"[ID8] KT-tokitama", 13},  // "[ID8] KT-tokitama"
        };

        // 試合のルール
        public int SettingTimeMin { private set; get; } = 3;
        public int AllianceMtgTimeMin { private set; get; } = 3;
        public int PreSettingTimeMin { private set; get; } = 2;

        public int GameTimeMin { private set; get; } = 5;
        public int MaxHP { private set; get; } = 40;
        public int PreGameTimeMin { private set; get; } = 2;

        public int PreRedMaxHP { private set; get; } = 100;  // 予選の赤（攻撃サイド）のMaxHP

        public int PreBlueMaxHP { private set; get; } = 20;  // 予選の青（迎撃サイド）のMaxHP

        public int HitDamage { private set; get; } = 10;

        public int PunishDamage { private set; get; } = 10;
        public int RespawnTime { private set; get; } = 60;
        public int RespawnHP { private set; get; } = 30;
        public int InvincibleTime { private set; get; } = 5;


        // フラグ関係
        public bool AutoConnect { set; get; } = false;
        public bool GameEndFlag { set; get; } = false;
        public bool DuringGame { private set; get; } = false;

        public bool Added3min { private set; get; }  = false;

        // enum関係
        public enum TeamColor {
            NONE,
            RED,
            GREEN,
            BLUE,
            CYAN,
            MAGENTA,
            YELLOW,
            WHITE,
        };

        public enum DColor {
            NONE,
            
        }

        public enum GameSystem　{
            NONE,
            PRELIMINALY,
            SEMIFINALS,
            FINALS
        };

        public enum GameStatus {
            NONE,
            SETTING,
            PREGAME,
            GAME,
            POSTGAME,
        };

        private enum SettingStatus {
            NONE,
            RUNNING,
            TECH_TIMEOUT,
            RESUME,
            SKIP,
        };

        private enum Winner {
            NONE,
            RED,
            BLUE,
            DRAW,
        };

        public TimeSpan CurrentTime { private set; get; } = TimeSpan.Zero;
        public CoreClass Msgs = new CoreClass();

        public static MainWindow GetMainWindow() {
            return Application.Current.MainWindow as MainWindow;
        }

        // サーバー全体の更新
        // 200msの間隔でホスト基板と通信する
        private static System.Timers.Timer _updateTimer;
        public event Action UpdateEvent;

        // タイマー関係の変数
        private static System.Timers.Timer _countDownTimer;
        private static DateTime _startTime;
        private static TimeSpan _remainingTime;
        private static bool _isPaused = false;

        // プロセス間通信の変数
        public readonly System.Timers.Timer _processCommTimer;

        private CoRE2ServerControl() {
            _updateTimer = new System.Timers.Timer();
            _updateTimer.Interval = 300;
            _updateTimer.Elapsed += OnEventArrived;
            _updateTimer.Elapsed += CheckGameEnd;
            _updateTimer.Start();

            _countDownTimer = new System.Timers.Timer();
            _countDownTimer.Interval = 50;
            _countDownTimer.Elapsed += OnCountDownTimedEvent;

            _processCommTimer = new System.Timers.Timer();
            _processCommTimer.Interval = 100;
            _processCommTimer.Elapsed += OnProcessCommTimedEvent;
        }

        private void OnEventArrived(object sender, EventArgs e) {
            UpdateEvent?.Invoke();
        }

        private void CheckGameEnd(object sender, EventArgs e) {
            if (!Instance.DuringGame) return;

            if (Instance.Msgs.GameSystem == (int)GameSystem.PRELIMINALY) {
                if (Instance.Msgs.Robot[0].DeathFlag == 1) { // 攻撃サイドが撃破
                    Instance.GameEndFlag = true;
                    Instance.Msgs.Winner = (int)Winner.BLUE;
                    Application.Current.Dispatcher.Invoke(() => {
                        GetMainWindow().TimerLabel.Text = "BLUE TEAM WINS!!!";
                    });
                    Instance.DuringGame = false;
                    Instance.Msgs.GameStatus = (int)GameStatus.POSTGAME;
                } else if (Instance.Msgs.Robot[3].DeathFlag == 1 &&
                           Instance.Msgs.Robot[4].DeathFlag == 1 &&
                           Instance.Msgs.Robot[5].DeathFlag == 1) { // 迎撃サイドがすべて撃破
                    Instance.GameEndFlag = true;
                    Instance.Msgs.Winner = (int)Winner.RED;
                    Application.Current.Dispatcher.Invoke(() => {
                        GetMainWindow().TimerLabel.Text = "RED TEAM WINS!!!";
                    });
                    Instance.DuringGame = false;
                    Instance.Msgs.GameStatus = (int)GameStatus.POSTGAME;
                }
            } else if (Instance.Msgs.GameSystem == (int)GameSystem.SEMIFINALS) {
                if (Instance.Msgs.Robot[0].DeathFlag == 1 &&
                    Instance.Msgs.Robot[1].DeathFlag == 1) { // 赤同盟がすべて撃破
                    Instance.GameEndFlag = true;
                    Instance.Msgs.Winner = (int)Winner.BLUE;
                    Application.Current.Dispatcher.Invoke(() => {
                        GetMainWindow().TimerLabel.Text = "BLUE TEAM WINS!!!";
                    });
                    Instance.DuringGame = false;
                    Instance.Msgs.GameStatus = (int)GameStatus.POSTGAME;
                } else if (Instance.Msgs.Robot[3].DeathFlag == 1 &&
                           Instance.Msgs.Robot[4].DeathFlag == 1) { // 赤同盟がすべて撃破
                    Instance.GameEndFlag = true;
                    Instance.Msgs.Winner = (int)Winner.RED;
                    Application.Current.Dispatcher.Invoke(() => {
                        GetMainWindow().TimerLabel.Text = "RED TEAM WINS!!!";
                    });
                    Instance.DuringGame = false;
                    Instance.Msgs.GameStatus = (int)GameStatus.POSTGAME;
                }
            } else {
                if (Instance.Msgs.Robot[0].DeathFlag == 1 &&
                    Instance.Msgs.Robot[1].DeathFlag == 1 &&
                    Instance.Msgs.Robot[2].DeathFlag == 1) { // 赤同盟がすべて撃破
                    Instance.GameEndFlag = true;
                    Instance.Msgs.Winner = (int)Winner.BLUE;
                    Application.Current.Dispatcher.Invoke(() => {
                        GetMainWindow().TimerLabel.Text = "BLUE TEAM WINS!!!";
                    });
                    Instance.DuringGame = false;
                    Instance.Msgs.GameStatus = (int)GameStatus.POSTGAME;
                } else if (Instance.Msgs.Robot[3].DeathFlag == 1 &&
                           Instance.Msgs.Robot[4].DeathFlag == 1 &&
                           Instance.Msgs.Robot[5].DeathFlag == 1) { // 赤同盟がすべて撃破
                    Instance.GameEndFlag = true;
                    Instance.Msgs.Winner = (int)Winner.RED;
                    Application.Current.Dispatcher.Invoke(() => {
                        GetMainWindow().TimerLabel.Text = "RED TEAM WINS!!!";
                    });
                    Instance.DuringGame = false;
                    Instance.Msgs.GameStatus = (int)GameStatus.POSTGAME;
                }
            }
        }

        public event Action GameStartEvent;
        /// <summary>
        /// GAME STARTボタンが押されると更新タイマーが開始
        /// また，ゲームスタートイベントが発生
        /// </summary>
        public void GameStart() {
            GetMainWindow().TimerLabel.Text = "GAME TIME";
            Instance.Msgs.GameStatus = (int)GameStatus.GAME;
            Instance.DuringGame = true;
            //_updateTimer.Start();
            int gameTime = Instance.GameTimeMin;
            if (Instance.Msgs.GameSystem == (int)GameSystem.PRELIMINALY) gameTime = Instance.PreGameTimeMin;
            StartTimer(gameTime*60);
            GameStartEvent?.Invoke();
            AllocateButton();
        }

        public event Action GameResetEvent;
        /// <summary>
        /// GAME RESETボタンが押されると更新タイマーが停止
        /// また，ゲームリセットイベントが発生
        /// </summary>
        public void GameReset() {
            GetMainWindow().TimerLabel.Text = "GAME READY?";
            Instance.Msgs.GameStatus = (int)GameStatus.PREGAME;
            Instance.DuringGame = false;
            //_updateTimer.Stop();

            int gameTime = Instance.GameTimeMin;
            if (Instance.Msgs.GameSystem == (int)GameSystem.PRELIMINALY)
                gameTime = Instance.PreGameTimeMin;
            ResetTimer(gameTime*60);
            GameResetEvent?.Invoke();
            AllocateButton();
        }

        public void Add3minSetting() {
            Added3min = true;
        }

        public void SettingTimeStart() {
            GetMainWindow().TimerLabel.Text = "SETTING TIME";
            Instance.Msgs.GameStatus = (int)GameStatus.SETTING;
            _settingStatus = SettingStatus.RUNNING;
            int settingTime = Added3min ? Instance.AllianceMtgTimeMin + Instance.SettingTimeMin : Instance.SettingTimeMin;
            if (Instance.Msgs.GameSystem == (int)GameSystem.PRELIMINALY)
                settingTime = Instance.PreSettingTimeMin;
            StartTimer(settingTime*60);
            AllocateButton();
        }

        public void SettingTimeReset() {
            GetMainWindow().TimerLabel.Text = "SETTING READY?";
            Instance.Msgs.GameStatus = (int)GameStatus.NONE;
            _settingStatus = SettingStatus.NONE;
            int settingTime = Instance.AllianceMtgTimeMin + Instance.SettingTimeMin;
            if (Instance.Msgs.GameSystem == (int)GameSystem.PRELIMINALY)
                settingTime = Instance.PreSettingTimeMin;
            Added3min = false;
            ResetTimer(settingTime*60);
            AllocateButton();
        }

        public void SettingTimeResume() {
            GetMainWindow().TimerLabel.Text = "SETTING TIME";
            _settingStatus = SettingStatus.RUNNING;
            ResumeTimer();
            AllocateButton();
        }

        public void SettingTimeSkip() {
            GetMainWindow().TimerLabel.Text = "GAME READY?";
            Instance.Msgs.GameStatus = (int)GameStatus.PREGAME;
            _settingStatus = SettingStatus.SKIP;

            int gameTime = Instance.GameTimeMin;
            if (Instance.Msgs.GameSystem == (int)GameSystem.PRELIMINALY)
                gameTime = Instance.PreGameTimeMin;
            Added3min = false;
            ResetTimer(gameTime*60);
            AllocateButton();
        }

        public void Timeout() {
            if (!_addedTimeout) {
                _remainingTime += TimeSpan.FromSeconds(2 * 60);
                _addedTimeout = true;
            }
            AllocateButton();
        }
        public void TechnicalTimeout() {
            GetMainWindow().TimerLabel.Text = "TECH. TIMEOUT";
            _settingStatus = SettingStatus.TECH_TIMEOUT;
            PauseTimer();
            AllocateButton();
        }

        public event Action ClearDataEvent;
        public void ClearData() {
            Instance.DuringGame = false;
            Instance.GameEndFlag = false;
            Added3min = false;
            _addedTimeout = false;
            ClearDataEvent?.Invoke();
        }


        private static void StartTimer(double timeSec) {
            _remainingTime = TimeSpan.FromSeconds(timeSec);
            _startTime = DateTime.Now;
            _countDownTimer.Start();
        }

        private static void PauseTimer() {
            if (_isPaused) return;

            _countDownTimer.Stop();
            _remainingTime -= DateTime.Now - _startTime;
            _isPaused = true;
        }

        private static void ResumeTimer() {
            if (!_isPaused) return;
            _startTime = DateTime.Now;
            _countDownTimer.Start();
            _isPaused = false;
        }

        private static void ResetTimer(int timeSec=0) {
            _countDownTimer.Stop();

            string timeText = $"{timeSec/60:D2}:{timeSec%60:D2}";
            GetMainWindow().GameCountDown.Text = timeText;
            Instance.Msgs.GameTime = timeText;
            Instance.Msgs.SettingTime = timeText;
        }

        private static void OnCountDownTimedEvent(object source, ElapsedEventArgs e) {
            Instance.CurrentTime = DateTime.Now - _startTime;
            var currentRemainingTime = _remainingTime - Instance.CurrentTime;
            if (Instance.GameEndFlag || currentRemainingTime.TotalSeconds <= 0) {
                _countDownTimer.Stop();
                if (Instance.GameEndFlag) return;

                Instance.Msgs.GameStatus += 1;
                if (Instance.Msgs.GameStatus == (int)GameStatus.POSTGAME
                    && Instance.Msgs.GameSystem != (int)GameSystem.PRELIMINALY) {
                    // 決勝トーナメントにおけるラウンドの勝敗条件2以降
                    // 条件２
                    if (Instance.Msgs.RedDeathCnt != Instance.Msgs.BlueDeathCnt) {
                        if (Instance.Msgs.RedDeathCnt > Instance.Msgs.BlueDeathCnt) {
                            Instance.Msgs.Winner = (int)Winner.BLUE;
                            Application.Current.Dispatcher.Invoke(() => {
                                GetMainWindow().TimerLabel.Text = "BLUE TEAM WINS!!!";
                            });
                            Instance.DuringGame = false;
                        } else {
                            Instance.Msgs.Winner = (int)Winner.RED;
                            Application.Current.Dispatcher.Invoke(() => {
                                GetMainWindow().TimerLabel.Text = "RED TEAM WINS!!!";
                            });
                            Instance.DuringGame = false;
                        }
                    }
                    // 条件3は無し
                    // 条件4
                    else if (Instance.Msgs.RedReceivedDamage != Instance.Msgs.BlueReceivedDamage) {
                        if (Instance.Msgs.RedReceivedDamage > Instance.Msgs.BlueReceivedDamage) {
                            Instance.Msgs.Winner = (int)Winner.BLUE;
                            Application.Current.Dispatcher.Invoke(() => {
                                GetMainWindow().TimerLabel.Text = "BLUE TEAM WINS!!!";
                            });
                            Instance.DuringGame = false;
                        } else {
                            Instance.Msgs.Winner = (int)Winner.RED;
                            Application.Current.Dispatcher.Invoke(() => {
                                GetMainWindow().TimerLabel.Text = "RED TEAM WINS!!!";
                            });
                            Instance.DuringGame = false;
                        }
                    }
                    // 勝敗条件を満たさない
                    else {
                        Instance.Msgs.Winner = (int)Winner.DRAW;
                        Application.Current.Dispatcher.Invoke(() => {
                            GetMainWindow().TimerLabel.Text = "The Game was drawn";
                        });
                        Instance.DuringGame = false;
                    }
                }

                AllocateButton();
                Instance.DuringGame = false;
                return;
            }

            string timeText = $"{currentRemainingTime.Minutes:00}:{currentRemainingTime.Seconds:00}";
            Application.Current.Dispatcher.Invoke(() => {
                GetMainWindow().GameCountDown.Text = timeText;
            });
            Instance.Msgs.GameTime = timeText;
            Instance.Msgs.SettingTime = timeText;
        }

        private void OnProcessCommTimedEvent(object? sender, ElapsedEventArgs e) {
            SendMsgsToOperatorScreen();
        }


        private static bool _addedTimeout = false;
        private static SettingStatus _settingStatus = SettingStatus.NONE;
        private static void AllocateButton() {
            Application.Current.Dispatcher.Invoke(() => {
                if (Instance.Msgs.GameStatus == (int)GameStatus.NONE
                    || Instance.Msgs.GameStatus == (int)GameStatus.POSTGAME) {
                    _addedTimeout = false;
                    GetMainWindow().PreliminaryRadioButton.IsEnabled = true;
                    GetMainWindow().SettingStartButton.IsEnabled = true;
                    GetMainWindow().SettingResetButton.IsEnabled = false;
                    GetMainWindow().TimeoutButton.IsEnabled = false;
                    GetMainWindow().TechnicalTimeOutButton.IsEnabled = false;
                    GetMainWindow().SettingResumeButton.IsEnabled = false;
                    GetMainWindow().SettingSkipButton.IsEnabled = false;
                    GetMainWindow().GameStartButton.IsEnabled = true;
                    GetMainWindow().GameResetButton.IsEnabled = false;
                } else if (Instance.Msgs.GameStatus == (int)GameStatus.SETTING) {
                    if (_settingStatus == SettingStatus.RUNNING) {
                    GetMainWindow().PreliminaryRadioButton.IsEnabled = false;
                    GetMainWindow().SettingStartButton.IsEnabled = false;
                    GetMainWindow().SettingResetButton.IsEnabled = true;
                    GetMainWindow().TimeoutButton.IsEnabled = CoRE2ServerControl.Instance.Msgs.GameSystem == (int)CoRE2ServerControl.GameSystem.PRELIMINALY ? false : !_addedTimeout;
                    GetMainWindow().TechnicalTimeOutButton.IsEnabled = true;
                    GetMainWindow().SettingResumeButton.IsEnabled = false;
                    GetMainWindow().SettingSkipButton.IsEnabled = true;
                    GetMainWindow().GameStartButton.IsEnabled = false;
                    GetMainWindow().GameResetButton.IsEnabled = false;
                    } else if (_settingStatus == SettingStatus.TECH_TIMEOUT) {
                        GetMainWindow().PreliminaryRadioButton.IsEnabled = false;
                        GetMainWindow().SettingStartButton.IsEnabled = false;
                        GetMainWindow().SettingResetButton.IsEnabled = false;
                        GetMainWindow().TimeoutButton.IsEnabled = false;
                        GetMainWindow().TechnicalTimeOutButton.IsEnabled = false;
                        GetMainWindow().SettingResumeButton.IsEnabled = true;
                        GetMainWindow().SettingSkipButton.IsEnabled = false;
                        GetMainWindow().GameStartButton.IsEnabled = false;
                        GetMainWindow().GameResetButton.IsEnabled = false;
                    }
                } else if (Instance.Msgs.GameStatus == (int)GameStatus.PREGAME) {
                    GetMainWindow().PreliminaryRadioButton.IsEnabled = false;
                    GetMainWindow().SettingStartButton.IsEnabled = true;
                    GetMainWindow().SettingResetButton.IsEnabled = false;
                    GetMainWindow().TimeoutButton.IsEnabled = false;
                    GetMainWindow().TechnicalTimeOutButton.IsEnabled = false;
                    GetMainWindow().SettingResumeButton.IsEnabled = false;
                    GetMainWindow().SettingSkipButton.IsEnabled = false;
                    GetMainWindow().GameStartButton.IsEnabled = true;
                    GetMainWindow().GameResetButton.IsEnabled = false;
                } else if (Instance.Msgs.GameStatus == (int)GameStatus.GAME) {
                    GetMainWindow().PreliminaryRadioButton.IsEnabled = false;
                    GetMainWindow().SettingStartButton.IsEnabled = false;
                    GetMainWindow().SettingResetButton.IsEnabled = false;
                    GetMainWindow().TimeoutButton.IsEnabled = false;
                    GetMainWindow().TechnicalTimeOutButton.IsEnabled = false;
                    GetMainWindow().SettingResumeButton.IsEnabled = false;
                    GetMainWindow().SettingSkipButton.IsEnabled = false;
                    GetMainWindow().GameStartButton.IsEnabled = false;
                    GetMainWindow().GameResetButton.IsEnabled = true;
                }
            });
        }

        private int isProcessing = 0;
        private async void SendMsgsToOperatorScreen() {
            if (Interlocked.CompareExchange(ref isProcessing, 1, 0) != 0) return;

            try {
                using (var pipeClient = new NamedPipeClientStream("S->Wpipe")) {
                    try {
                        // サーバに接続(5秒タイムアウト)
                        await pipeClient.ConnectAsync(5000);

                        // メッセージを送信（書き込み）
                        using (var writer = new StreamWriter(pipeClient)) {
                            string fileName = "CoreClass.json";
                            string jsonString = JsonSerializer.Serialize(Instance.Msgs);
                            using FileStream createStream = File.Create(fileName);
                            await JsonSerializer.SerializeAsync(createStream, Instance.Msgs);
                            await createStream.DisposeAsync();
                            await writer.WriteLineAsync(jsonString);
                        }
                    } catch (TimeoutException ex) {
                        // タイムアウト時の処理
                        ;
                    } catch (Exception ex) {
                        // その他のエラー
                        ;
                    }
                }
            } finally {
                Interlocked.Exchange(ref isProcessing, 0);
            }
        }
    }
};