using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class CoreClass {
        public String GameTime { get; set; } = "00:00";
        public String SettingTime { get; set; } = "00:00";
        public int GameSystem { get; set; } = 0;
        public int GameStatus { get; set; } = 0;
        public int RedDeathCnt { get; set; } = 0;
        public int BlueDeathCnt { get; set; } = 0;
        public int RedReceivedDamage { get; set; } = 0;
        public int BlueReceivedDamage { get; set; } = 0;

        public int Winner { get; set; } = 0; // 1赤、2青 
        public RobotClass[] Robot { get; set; } = { new RobotClass(),
                                                    new RobotClass(),
                                                    new RobotClass(),
                                                    new RobotClass(),
                                                    new RobotClass(),
                                                    new RobotClass(), };
    }

    public class RobotClass {

        public int TeamID { get; set; } = 0;
        public String TeamColor { get; set; } = "";
        public int HP { get; set; } = 0;
        public int MaxHP { get; set; } = 0;
        public int DeathFlag { get; set; } = 0;
        public String RespawnTime { get; set; } = "00:00";
    }
}
