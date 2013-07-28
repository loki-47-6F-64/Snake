using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Snake
{
    class Global
    {
        public static int Difficulty = 4;
        public static object Lock = new object();
        public static object LockKeyboard = new object();
        public static bool bLockKeyboard = false;
    }
    static class Constant
    {
        public const char chBlock = (char)9608;
        public const int MaxDifficulty = 10;
        //public const int MinDifficulty = 1;
    }
    static class Instruction
    {
        public const int iMode_snake = 001;
        public const int iMode_menu = 002;
        public const int iMode_options = 003;
        public const int iMode_difficulty = 004;

        public const int iMove_course = 100;
        public const int iMove_up = 101;
        public const int iMove_down = 102;
        public const int iMove_left = 103;
        public const int iMove_right = 104;

        public const int iSnake_has_eaten = 201;
        public const int iSnake_eat_special = 202;
        public const int iSnake_hit_teleport = 203;

        public const int iGame_over = 301;

        public const int iMenu_up = 401;
        public const int iMenu_down = 402;
        public const int iMenu_enter = 403;

        public const int iCreate_special = 501;
        public const int iRemove_special = 502;

        public const int iDifficulty_up = 601;
        public const int iDifficulty_down = 602;
        public const int iDifficulty_enter = 603;
    }
}
