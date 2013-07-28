using System;
using System.Collections.Generic;
using System.Threading;
using System.Text;
using Blocks;

namespace Snake
{
    class snake
    {
        protected char chBody = Constant.chBlock;

        public group_block[] cBody;
        public group_block cHead;

        public int iSize;
        protected int iMax_size;

        protected int pos_x, pos_y;
        public int iCourse; //the course the snake is following.

        public int Score;

        public snake()
        {
            Score = 0;
        }
        public void NewSnake()
        {
            pos_x = Field.SnakeStart_x;
            pos_y = Field.SnakeStart_y;

            iSize = 1;
            iMax_size = 10;

            cHead = new group_block(pos_x, pos_y);
            cHead.SetShape(chBody, 0, 0);
            cHead.SetColor(16, (int)ConsoleColor.Gray);
            cBody = new group_block[iMax_size];

            //The first body part of the snake needs to be placed manually
            //otherwise it would take the same position as the head.
            //When that happens it is game over.
            cBody[0] = new group_block(pos_x, pos_y);
            cBody[0].SetShape(chBody, 0, 0);
            cBody[0].SetColor(16, (int)ConsoleColor.Gray);

            PrintSnake();
        }
        public void ChangeCourse(int iDirection)
        {
            //The new course must no be the total opposite off the old one.
            switch (iDirection)
            {
                case Instruction.iMove_up:
                    if (iCourse != Instruction.iMove_down)
                    {
                        iCourse = iDirection;
                        MoveSnake();
                    }
                    break;
                case Instruction.iMove_down:
                    if (iCourse != Instruction.iMove_up)
                    {
                        iCourse = iDirection;
                        MoveSnake();
                    }
                    break;
                case Instruction.iMove_left:
                    if (iCourse != Instruction.iMove_right)
                    {
                        iCourse = iDirection;
                        MoveSnake();
                    }
                    break;
                case Instruction.iMove_right:
                    if (iCourse != Instruction.iMove_left)
                    {
                        iCourse = iDirection;
                        MoveSnake();
                    }
                    break;
            }
        }
        //When the tail goes over the teleporter it automatically clears the teleporter too
        //That has to be prevented.
        public void MoveSnake()
        {
            Program.Timer.Restart();
            Monitor.Enter(Global.Lock);
            {
                //First clear the tail.
                cBody[iSize - 1].Clear();


                //move the bodyparts of the snake
                for (int x = iSize - 1; x > 0; x--)
                {
                    cBody[x].pos_x = cBody[x - 1].pos_x;
                    cBody[x].pos_y = cBody[x - 1].pos_y;
                }

                //Move the cBody[0] to head before moving the head.
                cBody[0].pos_x = cHead.pos_x;
                cBody[0].pos_y = cHead.pos_y;

                //Then move the head in the right direction
                switch (iCourse)
                {
                    case Instruction.iMove_up:
                        cHead.pos_y--;
                        break;
                    case Instruction.iMove_down:
                        cHead.pos_y++;
                        break;
                    case Instruction.iMove_left:
                        cHead.pos_x--;
                        break;
                    case Instruction.iMove_right:
                        cHead.pos_x++;
                        break;
                }

                //And finally print the head.
                cHead.PrintShape();
            }
            Monitor.Exit(Global.Lock);
        }
        //The snake ate food. It grows.
        public void GrowSnake(int iGrowth)
        {
            //The body of the snake must be allowed to grow endlessly.
            if (iSize + iGrowth >= iMax_size)
            {
                iMax_size += 10;
                Array.Resize(ref cBody, iMax_size);
            }
            for (int x = iSize; x < iSize + iGrowth; x++)
            {
                cBody[x] = new group_block(cBody[x - 1].pos_x, cBody[x - 1].pos_y);
                cBody[x].SetShape(chBody, 0, 0);
                cBody[x].SetColor(16, (int)ConsoleColor.Gray);
            }
            //The size needs to be updated
            iSize += iGrowth;
        }
        //Make the snake smaller
        public void ShrinkSnake(int Size)
        {
            int OriginalSize = iSize;
            if (OriginalSize >= 1 + Size)
            {
                while (OriginalSize - iSize < Size)
                {
                    cBody[--iSize].Clear();
                    cBody[iSize] = null;
                }
            }
        }
        //Clears the columns at the spot where group_blocks of the snake are.
        public void ClearSnake()
        {
            cHead.Clear();
            for (int x = 0; x < iSize; x++)
            {
                cBody[x].Clear();
            }
        }
        //Processes the data and returns the proper action.
        public static int Process(int iKey)
        {
            int Result = 0;

            if (iKey == (int)ConsoleKey.W || iKey == (int)ConsoleKey.UpArrow)
            {
                Result = Instruction.iMove_up;
            }
            else if (iKey == (int)ConsoleKey.S || iKey == (int)ConsoleKey.DownArrow)
            {
                Result = Instruction.iMove_down;
            }
            else if (iKey == (int)ConsoleKey.A || iKey == (int)ConsoleKey.LeftArrow)
            {
                Result = Instruction.iMove_left;
            }
            else if (iKey == (int)ConsoleKey.D || iKey == (int)ConsoleKey.RightArrow)
            {
                Result = Instruction.iMove_right;
            }

            //return the processed instructions
            return Result;
        }
        public void PrintSnake()
        {
            cHead.PrintShape();

            for (int x = 0; x < iSize; x++)
            {
                cBody[x].PrintShape();
            }
        }
        public int GetSize()
        {
            return iSize;
        }
    }
}
