using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using Blocks;

namespace Snake
{
    class Program
    {
        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        static void Main(string[] args)
        {
            Console.Title = "Snake";
            Console.CursorVisible = false;
            Console.SetBufferSize(Console.LargestWindowWidth, Console.BufferHeight);
            //Maximize the window.
            ShowWindow(System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle, 3);

            Program cControl = new Program();
        }
        //Indicater wether or not the threads need to abort.
        protected bool bAbort = false;
        protected bool bStartWaiting = true; //Syncronizing the Threads Automove and Action

        bool bGotoMenu;

        static public System.Diagnostics.Stopwatch Timer;
        protected Menu cMenu;
        protected Field cField;
        protected snake cSnake;
        protected Food cFood;
        protected Food cSpecial;
        protected pop_up Box;
        protected Teleporter[] cTeleport;
        protected group_block[] OptionsLayout;
        
        //The object used for locking the Array "
        protected object ThisMethod = new object();
        protected object Queue = new object();

        protected int iMaxInstruction = 15;
        protected int iArraySize = 0;
        protected int[] InstructionQueue;
        protected int iMode;

        public Program()
        {
            InstructionQueue = new int[iMaxInstruction];

            cField = new Field();

            //The specifics for the pop_up.
            Box = new pop_up(0, 0);
            Box.SetColorSign((int)ConsoleColor.Yellow);
            Box.SetColorText((int)ConsoleColor.Black, (int)ConsoleColor.Gray);
            Box.SetColorWall((int)ConsoleColor.DarkYellow, 16);

            

            //Initialise the main threads for the game.
            ThreadStart StartAction = new ThreadStart(Action);
            ThreadStart StartInput = new ThreadStart(KeyboardInput);

            Thread cInput = new Thread(StartInput);
            Thread cAction = new Thread(StartAction);

            cAction.Name = "Instruction";
            cInput.Name = "Input";

            cAction.Start();
            cInput.Start();

            Timer = new System.Diagnostics.Stopwatch();

            cMenu = new Menu();

            cMenu.CreateItem("", 30, 16);
            cMenu.CreateItem("", 30, 17);
            cMenu.CreateItem("", 30, 18);
            NewMenu();
        }
        protected void UnlockKeyboard() //Unlocks the key. Does nothing if not locked.
        {
            Monitor.Enter(Global.LockKeyboard);
            {
                Monitor.Pulse(Global.LockKeyboard);
            }
            Monitor.Exit(Global.LockKeyboard);
        }

        protected void ChooseMap()
        {
            if (Directory.Exists(cField.Path))
            {
                //Make sure Filenames can contain the same amount of filenames as there are ffiles in the directory.
                string[] FileNames = new string[Directory.GetFiles(cField.Path).Length];
                int NumberOfFiles = 0;
                string[] Files = Directory.GetFiles(cField.Path);
                //Find any Files that End with .txt
                foreach (string x in Files)
                {
                    string[] FileEnd = x.Split('.');
                    if (FileEnd[FileEnd.Length - 1] == "snake")
                    {
                        string[] FileName = x.Split('\\');
                        FileNames[NumberOfFiles] = FileName[FileName.Length - 1];
                        NumberOfFiles++;
                    }
                }
                //If any files had been found.
                if (NumberOfFiles > 0)
                {
                    //The choises can be shown.
                    Array.Resize(ref FileNames, NumberOfFiles);

                    Global.bLockKeyboard = true;
                    int Choise = Box.Choise("What map do you wish to use?", FileNames);
                    UnlockKeyboard();

                    //Now change the map that will be shown to the player during the game.
                    cField.FileName = FileNames[Choise];
                }
            }
        }
        //Create the options
        protected void NewOptions()
        {
            cMenu.ChangeItem("Difficulty", 0);
            cMenu.ChangeItem("Change Map", 1);
            cMenu.ChangeItem("Return", 2);

            iMode = Instruction.iMode_options;
        }
        //Create a new Menu for the game.
        protected void NewMenu()
        {
            cField.NewMenu();
            //Let the program know the Menu is up.
            iMode = Instruction.iMode_menu;

            cMenu.ChangeItem("New Game", 0);
            cMenu.ChangeItem("Options", 1);
            cMenu.ChangeItem("Exit", 2);
            cMenu.SetCursor("<^>");
        }
        //Create a new game for snake
        protected void NewGame()
        {
            //Let the program know the game has started.
            iMode = Instruction.iMode_snake;
            bGotoMenu = false;

            cField.NewField(out cField.cField, out cTeleport);
            PrintField();
            
            cFood = new Food(cField.iLength_x, cField.iLength_y);
            cSnake = new snake();

            cSnake.NewSnake();
            cFood.NewFood();

            bool bSpot;
            do
            {
                cFood.MoveFood();
                bSpot = true;
                for (int x = 0; x < cSnake.GetSize(); x++)
                {
                    if (group_block.bCollision(cSnake.cBody[x],cFood.cFood))
                    {
                        bSpot = false;
                    }
                }
                for (int x = 0; x < cField.cField.Length; x++)
                {
                    if (group_block.bCollision(cField.cField[x],cFood.cFood))
                    {
                        bSpot = false;
                    }
                }
                for (int x = 0; x < cTeleport.Length; x++)
                {
                    if (group_block.bCollision(cTeleport[x].cPair[0], cFood.cFood)
                        ||
                        group_block.bCollision(cTeleport[x].cPair[1], cFood.cFood))
                    {
                        bSpot = false;
                    }
                }
                if (group_block.bCollision(cSnake.cHead,cFood.cFood))
                {
                    bSpot = false;
                }
            } while (!bSpot);

            cFood.PrintFood();

            ThreadStart StartAutoMove = new ThreadStart(AutoMoveSnake);
            
            Thread cAutoMove = new Thread(StartAutoMove);

            cAutoMove.Name = "AutoMove";

            IncreaseScore(0);

            cAutoMove.Start();
            
        }
        
        //Add an instruction to the queue
        public void AddInstruction(int iInstruction)
        {
            Monitor.TryEnter(Queue, 5);
            {
                if (iArraySize < iMaxInstruction)
                {
                    InstructionQueue[iArraySize] = iInstruction;
                    iArraySize++;
                }
            }
            Monitor.Exit(Queue);
        }
        //Lets AutoMoveThread wait
        protected void Wait()
        {
            Monitor.Enter(ThisMethod);
            {
                bStartWaiting = false;
                Monitor.Wait(ThisMethod);
            }
            Monitor.Exit(ThisMethod);
        }
        //Move the snake every x millisecondst h
        //Make sure the snake did not hit something
        public void AutoMoveSnake()
        {
            lock (this)
            {
                Monitor.Wait(this);
            }
            //Create the timer for the SpecialFood.
            System.Diagnostics.Stopwatch SpecialTimer = new System.Diagnostics.Stopwatch();

            bool bCreatedSpecial = false;

            int iSpeed = 150 - 14 * Global.Difficulty;
            int iIniterSpecial = 40000 - 2000 * Global.Difficulty;
            int iSpecialDuration = 20000 - 1000 * Global.Difficulty;

            SpecialTimer.Start();
            while (!bGotoMenu)
            {
                if (Timer.ElapsedMilliseconds >= iSpeed)
                {
                    AddInstruction(Instruction.iMove_course);
                    Wait();
                }
                //Did the snake hit food?
                if (SnakeEat())
                {
                    AddInstruction(Instruction.iSnake_has_eaten);
                    Wait();
                }
                //First check if its time for the SpecialFood.
                if (SpecialTimer.ElapsedMilliseconds >= iIniterSpecial && !bCreatedSpecial)
                {
                    AddInstruction(Instruction.iCreate_special);
                    bCreatedSpecial = true;
                    Wait();
                }
                if (SpecialTimer.ElapsedMilliseconds >= iIniterSpecial + iSpecialDuration && bCreatedSpecial == true)
                {
                    bCreatedSpecial = false;
                    AddInstruction(Instruction.iRemove_special);
                    Wait();
                    SpecialTimer.Restart();
                }
                if (cSpecial != null && bCreatedSpecial)
                {
                    //If the snake eats the SpecialFood. The snake needs to grow and the SpecialFood needs to be removed.
                    if (SnakeEatSpecial())
                    {
                        bCreatedSpecial = false;
                        AddInstruction(Instruction.iSnake_eat_special);
                        Wait();
                        SpecialTimer.Restart();
                    }
                }
                if (SnakeHitTeleport())
                {
                    AddInstruction(Instruction.iSnake_hit_teleport);
                    Wait();
                }
                //Game Over?
                if (SnakeCollide())
                {
                    AddInstruction(Instruction.iGame_over);
                    Wait();
                }
            }
        }
        //This method sends the input to the appropriate objects for processing
        //Then the processed data from the keyboard will be send to the method 'Action'
        public void KeyboardInput()
        {
            while (true)
            {
                if(Global.bLockKeyboard)
                {
                    Monitor.Enter(Global.LockKeyboard);
                    {
                        Global.bLockKeyboard = false;
                        Monitor.Wait(Global.LockKeyboard);
                    }
                    Monitor.Exit(Global.LockKeyboard);
                }
                if (Console.KeyAvailable)
                {
                    int iInstruction = 0;
                    ConsoleKeyInfo Input = Console.ReadKey(true);

                    switch (iMode)
                    {
                        case Instruction.iMode_menu:
                        case Instruction.iMode_options:
                            iInstruction = SnakeMenu.Process((int)Input.Key);
                            break;
                        case Instruction.iMode_snake:
                            iInstruction = snake.Process((int)Input.Key);
                            break;
                        case Instruction.iMode_difficulty:
                            iInstruction = SetDifficulty.Process((int)Input.Key);
                            break;
                    }
                    AddInstruction(iInstruction);
                }
                if (bAbort)
                {
                    Thread.CurrentThread.Abort();
                }
            }
        }
        //This method gets the instruction for the InstructionQueue.
        //And sends it to the right object.
        //After that it removes the Instruction
        protected void Action()
        {
            while (true)
            {
                if (iArraySize > 0)
                {
                    switch (InstructionQueue[0])
                    {
                        case Instruction.iMenu_enter:
                            if (iMode == Instruction.iMode_menu)
                            {
                                switch (cMenu.pos_cursor)
                                {
                                    case 0: //Start
                                        Console.Clear();
                                        NewGame();
                                        break;
                                    case 1: //Options
                                        NewOptions();
                                        break;
                                    case 2: //Exit
                                        bAbort = true;
                                        break;
                                }
                                break;
                            }
                            if (iMode == Instruction.iMode_options)
                            {
                                switch (cMenu.pos_cursor)
                                {
                                    case 0: //Difficulty
                                        int pos_x = cMenu.cMenu[0].pos_x + 20;
                                        int pos_y = cMenu.cMenu[0].pos_y;
                                        OptionsLayout = new group_block[3];
                                        //The Cursor for the difficulty.
                                        OptionsLayout[0] = new group_block(pos_x + Global.Difficulty, pos_y);
                                        //The borders
                                        OptionsLayout[1] = new group_block(pos_x, pos_y);
                                        OptionsLayout[2] = new group_block(pos_x + Constant.MaxDifficulty, pos_y);
                                        
                                        //Setting the shapes
                                        OptionsLayout[0].SetShape(Constant.chBlock, 0, 0);
                                        OptionsLayout[1].SetShape(Constant.chBlock, 0, 0);
                                        OptionsLayout[2].SetShape(Constant.chBlock, 0, 0);

                                        Console.SetCursorPosition(OptionsLayout[1].pos_x, OptionsLayout[1].pos_y);
                                        for (int x = OptionsLayout[1].pos_x + 1; x <= OptionsLayout[2].pos_x; x++)
                                        {
                                            Console.Write("-");
                                        }

                                        foreach (group_block x in OptionsLayout)
                                        {
                                            x.PrintShape();
                                        }

                                        iMode = Instruction.iMode_difficulty;
                                        break;
                                    case 1: //Change map
                                        ChooseMap();
                                        PrintMenu();
                                        break;
                                    case 2: //Return to main Menu
                                        iMode = Instruction.iMode_options;
                                        NewMenu();
                                        break;
                                }
                                break;
                            }
                            break;
                        case Instruction.iMenu_up:
                            cMenu.MoveCursor(-1);
                            break;
                        case Instruction.iMenu_down:
                            cMenu.MoveCursor(1);
                            break;
                        case Instruction.iMove_course:
                            while (bStartWaiting) { } //Wait until the Automove is waiting.

                            bStartWaiting = true;
                            cSnake.MoveSnake();

                            Monitor.Enter(ThisMethod);
                            {
                                Monitor.Pulse(ThisMethod);
                            }
                            Monitor.Exit(ThisMethod);
                            break;
                        case Instruction.iMove_right:
                        case Instruction.iMove_left:
                        case Instruction.iMove_up:
                        case Instruction.iMove_down:
                            cSnake.ChangeCourse(InstructionQueue[0]);
                            lock (this)
                            {
                                Monitor.Pulse(this);
                            }
                            break;
                        case Instruction.iSnake_hit_teleport:
                            while (bStartWaiting) { } //Wait until the Automove is waiting.

                            TeleportSnake();

                            bStartWaiting = true;
                            Monitor.Enter(ThisMethod);
                            {
                                Monitor.Pulse(ThisMethod);
                            }
                            Monitor.Exit(ThisMethod);
                            break;
                        case Instruction.iSnake_has_eaten:
                            while (bStartWaiting) { } //Wait until the Automove is waiting.

                            IncreaseScore(100);
                            //The food cannot fall on a part of the snake or an obstacle.
                            bool bSpot = true;
                            do
                            {
                                cFood.MoveFood();
                                bSpot = true;
                                for (int x = 0; x < cSnake.GetSize(); x++)
                                {
                                    if (group_block.bCollision(cSnake.cBody[x],cFood.cFood))
                                    {
                                        bSpot = false;
                                    }
                                }
                                for (int x = 0; x < cField.cField.Length; x++)
                                {
                                    if (group_block.bCollision(cField.cField[x],cFood.cFood))
                                    {
                                        bSpot = false;
                                    }
                                }
                                for (int x = 0; x < cTeleport.Length; x++)
                                {
                                    if (group_block.bCollision(cTeleport[x].cPair[0],cFood.cFood)
                                        ||
                                        group_block.bCollision(cTeleport[x].cPair[1],cFood.cFood))
                                    {
                                        bSpot = false;
                                    }
                                }
                                if (group_block.bCollision(cSnake.cHead,cFood.cFood))
                                {
                                    bSpot = false;
                                }
                            } while (!bSpot);
                            cFood.PrintFood();
                            cSnake.GrowSnake(1);

                            bStartWaiting = true;
                            Monitor.Enter(ThisMethod);
                            {
                                Monitor.Pulse(ThisMethod);
                            }
                            Monitor.Exit(ThisMethod);
                            break;
                        case Instruction.iGame_over:
                            while (bStartWaiting) ;
                            //You hit something.
                            //First empty the instructionqueue
                            //Then clear the entire Field and remove the instance of snake and Field.
                            //Then go to the Menu.
                            Global.bLockKeyboard = true;
                            Box.Message("Game Over");
                            UnlockKeyboard();

                            bGotoMenu = true;

                            Console.Clear();
                            cSnake = null;
                            NewMenu();

                            Monitor.Enter(ThisMethod);
                            {
                                InstructionQueue = new int[iMaxInstruction];
                                Monitor.Pulse(ThisMethod);
                            }
                            Monitor.Exit(ThisMethod);
                            break;
                        case Instruction.iCreate_special:
                            while (bStartWaiting) { } //Wait until the Automove is waiting.

                            cSpecial = new SpecialFood(cField.iLength_x - 1, cField.iLength_y - 1);
                            cSpecial.NewFood();
                            cSpecial.MoveFood();
                            //The food cannot appear on a part of the snake or an obstacle.
                            do
                            {
                                cSpecial.MoveFood();
                                bSpot = true;
                                for (int x = 0; x < cSnake.GetSize(); x++)
                                {
                                    if (group_block.bCollision(cSnake.cBody[x],cSpecial.cFood))
                                    {
                                        bSpot = false;
                                    }
                                }
                                for (int x = 0; x < cField.cField.Length; x++)
                                {
                                    if(group_block.bCollision(cField.cField[x],cSpecial.cFood))
                                    {
                                        bSpot = false;
                                    }
                                }
                                for (int x = 0; x < cTeleport.Length; x++)
                                {
                                    if (group_block.bCollision(cTeleport[x].cPair[0],cSpecial.cFood)
                                        ||
                                        group_block.bCollision(cTeleport[x].cPair[1],cSpecial.cFood))
                                    {
                                        bSpot = false;
                                    }
                                }
                                if (group_block.bCollision(cSnake.cHead,cSpecial.cFood))
                                {
                                    bSpot = false;
                                }
                            } while (!bSpot);
                            cSpecial.PrintFood();
                            
                            
                            bStartWaiting = true;
                            Monitor.Enter(ThisMethod);
                            {
                                Monitor.Pulse(ThisMethod);
                            }
                            Monitor.Exit(ThisMethod);
                            break;
                        case Instruction.iRemove_special:
                            while (bStartWaiting) { } //Wait until the Automove is waiting.

                            cSpecial.ClearFood();
                            cSpecial = null;

                            bStartWaiting = true;
                            Monitor.Enter(ThisMethod);
                            {
                                Monitor.Pulse(ThisMethod);
                            }
                            Monitor.Exit(ThisMethod);
                            break;
                        case Instruction.iSnake_eat_special:
                            while (bStartWaiting) { } //Wait until the Automove is waiting.

                            IncreaseScore(150);

                            cSnake.ShrinkSnake(2);
                            cSpecial = null;

                            bStartWaiting = true;
                            Monitor.Enter(ThisMethod);
                            {
                                Monitor.Pulse(ThisMethod);
                            }
                            Monitor.Exit(ThisMethod);
                            break;
                        case Instruction.iDifficulty_down:
                            //First place an minus at the position of the cursor
                            //Then change the position of the cursor and print it.
                            if (OptionsLayout[0].pos_x > OptionsLayout[1].pos_x + 1)
                            {
                                Console.SetCursorPosition(OptionsLayout[0].pos_x, OptionsLayout[0].pos_y);
                                Console.Write("-");
                                OptionsLayout[0].pos_x--;
                                OptionsLayout[0].PrintShape();
                            }
                            break;
                        case Instruction.iDifficulty_up:
                            //First place an minus at the position of the cursor
                            //Then change the position of the cursor and print it.
                            if (OptionsLayout[0].pos_x < OptionsLayout[2].pos_x - 1)
                            {
                                Console.SetCursorPosition(OptionsLayout[0].pos_x, OptionsLayout[0].pos_y);
                                Console.Write("-");
                                OptionsLayout[0].pos_x++;
                                OptionsLayout[0].PrintShape();
                            }
                            break;
                        case Instruction.iDifficulty_enter:
                            Global.Difficulty = OptionsLayout[0].pos_x - OptionsLayout[1].pos_x;

                            //Clear the bar for the difficulty.
                            Console.SetCursorPosition(OptionsLayout[1].pos_x, OptionsLayout[1].pos_y);
                            for (int x = OptionsLayout[1].pos_x; x <= OptionsLayout[2].pos_x; x++ )
                            {
                                Console.Write(" ");
                            }

                            iMode = Instruction.iMode_options;
                            break;
                    }
                    if (bAbort)
                    {
                        Thread.CurrentThread.Abort();
                    }
                    //Remove the instruction
                    Monitor.Enter(Queue);
                    {
                        for (int x = 0; x < iArraySize - 1; x++)
                        {
                            InstructionQueue[x] = InstructionQueue[x + 1];
                        }
                        InstructionQueue[iArraySize - 1] = 0;
                        iArraySize--;
                    }
                    Monitor.Exit(Queue);
                }
            }
        }
        public void IncreaseScore(int Score)
        {
            cSnake.Score += Score;
            Console.SetCursorPosition(0, 0);
            Console.WriteLine("Score: " + cSnake.Score);
        }
        //Print the looks of the Menu.
        void PrintMenu()
        {
            for (int x = 0; x < cField.cMainMenu.Length; x++)
            {
                cField.cMainMenu[x].PrintShape();
            }
        }
        //Print the entire Field.
        public void PrintField()
        {
            for(int x = 0; x < cTeleport.Length; x++)
            {
                cTeleport[x].cPair[0].PrintShape();
                cTeleport[x].cPair[1].PrintShape();
            }
            for (int x = 0; x < cField.cField.Length; x++)
            {
                cField.cField[x].PrintShape();
            }
        }

        //Does the snake activate a teleporter?
        protected bool SnakeHitTeleport()
        {
            bool bResult = false;
            foreach (Teleporter x in cTeleport)
            {
                if (cSnake.cHead.pos_x == x.cPair[1].pos_x && cSnake.cHead.pos_y == x.cPair[1].pos_y)
                {
                    bResult = true;
                }
                else if (cSnake.cHead.pos_x == x.cPair[0].pos_x && cSnake.cHead.pos_y == x.cPair[0].pos_y)
                {
                    bResult = true;
                }
            }
            return bResult;
        }
        //Teleports the head of the snake to the other teleporter of the pair.
        //If the tail goes over the teleporter it needs to move without erasing the teleporter.
        protected void TeleportSnake()
        {
            foreach (Teleporter x in cTeleport)
            {
                bool bSucces = false;
                if (cSnake.cHead.pos_x == x.cPair[1].pos_x && cSnake.cHead.pos_y == x.cPair[1].pos_y)
                {
                    cSnake.cHead.pos_x = x.cPair[0].pos_x;
                    cSnake.cHead.pos_y = x.cPair[0].pos_y;
                    x.cPair[1].PrintShape();
                    bSucces = true;
                }
                else if (cSnake.cHead.pos_x == x.cPair[0].pos_x && cSnake.cHead.pos_y == x.cPair[0].pos_y)
                {
                    x.cPair[0].PrintShape();
                    cSnake.cHead.pos_x = x.cPair[1].pos_x;
                    cSnake.cHead.pos_y = x.cPair[1].pos_y;
                    bSucces = true;
                }
                if (bSucces)
                {
                    switch (cSnake.iCourse)
                    {
                        case Instruction.iMove_up:
                            cSnake.cHead.pos_y--;
                            break;
                        case Instruction.iMove_down:
                            cSnake.cHead.pos_y++;
                            break;
                        case Instruction.iMove_left:
                            cSnake.cHead.pos_x--;
                            break;
                        case Instruction.iMove_right:
                            cSnake.cHead.pos_x++;
                            break;
                    }
                    cSnake.PrintSnake();
                }
            }
        }
        //Did the snake hid hit a wall or a part of his body?
        protected bool SnakeCollide()
        {
            lock (Global.Lock)
            {
                foreach (group_block x in cField.cField)
                {
                    if (group_block.bCollision(x, cSnake.cHead))
                    {
                        return true;
                    }
                }
                for (int x = 0; x < cSnake.GetSize(); x++)
                {
                    if (group_block.bCollision(cSnake.cHead, cSnake.cBody[x]))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        //Did the snale eat something?
        protected bool SnakeEat()
        {
            if (group_block.bCollision(cSnake.cHead,cFood.cFood))
            {
                return true;
            }
            return false;
        }
        protected bool SnakeEatSpecial()
        {
            if (group_block.bCollision(cSnake.cHead,cSpecial.cFood))
            {
                return true;
            }
            return false;
        }
    }
    class SetDifficulty
    {
        public static int Process(int iInput)
        {
            switch (iInput)
            {
                case (int)ConsoleKey.A:
                case (int)ConsoleKey.LeftArrow:
                    return Instruction.iDifficulty_down;

                case (int)ConsoleKey.D:
                case (int)ConsoleKey.RightArrow:
                    return Instruction.iDifficulty_up;

                case (int)ConsoleKey.Enter:
                    return Instruction.iDifficulty_enter;
            }
            return 0;
        }
    }
    class SnakeMenu
    {
        public static int Process(int iInput)
        {
            switch (iInput)
            {
                case (int)ConsoleKey.W:
                case (int)ConsoleKey.UpArrow:
                    return Instruction.iMenu_up;

                case (int)ConsoleKey.S:
                case (int)ConsoleKey.DownArrow:
                    return Instruction.iMenu_down;

                case (int)ConsoleKey.Enter:
                    return Instruction.iMenu_enter;

                default:
                    return 0;
            }
        }
    }
}
