using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Blocks;

namespace Snake
{
    class Field
    {
        //The position of the Field
        public static int FieldStart_x;
        public static int FieldStart_y;
        //The startposition of the snake
        public static int SnakeStart_x;
        public static int SnakeStart_y;

        public group_block[] cField;
        public group_block[] cMainMenu;

        public int iLength_x, iLength_y; //The maximums for the Random class in 'food'

        public string Path = "Saves\\";
        public string FileName = "standard.snake";
        public void NewMenu()
        {
            FileManage cLoad = new FileManage();
            cLoad.load_field(Path + "SnakeMenu.txt", out cMainMenu, 7, 2);
        }

        public void NewField(out group_block[] cField, out Teleporter[] cPortal)
        {
            int iTeleport = 0;
            int iObject = 0;

            cField = new group_block[0];
            group_block[] cTeleporter = new group_block[30];
            cPortal = new Teleporter[0];

            bool bSucces = File.Exists(Path + FileName);
            if (bSucces && FileName != "")
            {
                StreamReader cLoad = new StreamReader(Path + FileName);

                string sInput;

                while (!cLoad.EndOfStream)
                {
                    int size_x = 0;
                    int size_y = 0;
                    int pos_x = 0;
                    int pos_y = 0;
                    int iText_color = 16;
                    int iBack_color = 16;


                    sInput = cLoad.ReadLine();
                    string[] sResult = sInput.Split(' ');
                    string[] sSeperator = { "\"" };
                    if (sResult[0] == "Start_position_x:")
                    {
                        SnakeStart_x = Function.StringToInt(sResult[1]);
                    }
                    else if (sResult[0] == "Start_position_y:")
                    {
                        SnakeStart_y = Function.StringToInt(sResult[1]);
                    }
                    else if (sResult[0] == "iObject:") //The amount of objects
                    {
                        cField = new group_block[Function.StringToInt(sResult[1])];
                    }
                    else if (sResult[0] == "NumberOfTeleport")
                    {
                        cPortal = new Teleporter[Function.StringToInt(sResult[1]) / 2];
                    }
                    else if (sResult[0] == "Object" || sResult[0] == "Teleporter")
                    {
                        string[] Result;
                        do
                        {
                            sInput = cLoad.ReadLine();
                            //split de string
                            Result = sInput.Split(sSeperator, StringSplitOptions.RemoveEmptyEntries);

                            if (Result[0] == "pos_x =") //Positie
                            {
                                pos_x = Function.StringToInt(Result[1]);
                            }
                            else
                                if (Result[0] == "pos_y =")
                                {
                                    pos_y = Function.StringToInt(Result[1]);
                                }
                                else
                                    if (Result[0] == "size_x =") //grootte
                                    {
                                        size_x = Function.StringToInt(Result[1]);
                                    }
                                    else
                                        if (Result[0] == "size_y =")
                                        {
                                            size_y = Function.StringToInt(Result[1]);
                                        }
                                        else
                                            if (Result[0] == "Text_color =") //De kleur
                                            {
                                                iText_color = Function.StringToInt(Result[1]);
                                            }
                                            else
                                                if (Result[0] == "Background_color =")
                                                {
                                                    iBack_color = Function.StringToInt(Result[1]);
                                                }
                        } while (Result[0] != "//");
                        //Het object mag niet uit de bufferarea liggen.
                        if (pos_x > Console.BufferWidth || pos_y > Console.BufferHeight)
                        {
                            return;
                        }
                        if (sResult[0] == "Object")
                        {
                            cField[iObject] = new group_block(pos_x, pos_y, size_x, size_y);
                            for (int y = 0; y < size_y; y++) //De vorm
                            {
                                sInput = cLoad.ReadLine(); //Een regel van de vorm
                                for (int x = 0; x < size_x; x++)
                                {
                                    cField[iObject].SetShape(sInput[x], x, y);
                                }
                            }
                            cField[iObject].SetColor(iText_color, iBack_color);
                            iObject++; //Er is een nieuw object
                        }
                        else
                        {
                            cTeleporter[iTeleport] = new group_block(pos_x, pos_y);
                            cTeleporter[iTeleport].SetShape('@', 0, 0);
                            cTeleporter[iTeleport].SetColor(16, iBack_color);
                            iTeleport++;
                        }
                    }
                }
                //All objects have been loaded, now its time to put them into the right positions.

                //Number 0 is a horizontal border
                //Number 2 is an vertical border.
                iLength_x = cField[0].GetSize_x();
                iLength_y = cField[2].GetSize_y();

                //The beginpoint of the Field.
                FieldStart_x = (Console.LargestWindowWidth - iLength_x) / 2;
                FieldStart_y = (Console.LargestWindowHeight - iLength_y) / 2;

                for (int x = 0; x < iTeleport; x++)
                {
                    cTeleporter[x].pos_x += FieldStart_x;
                    cTeleporter[x].pos_y += FieldStart_y;
                }
                for (int x = 0; x < cField.Length; x++)
                {
                    cField[x].pos_x += FieldStart_x;
                    cField[x].pos_y += FieldStart_y;
                }
                SnakeStart_x += FieldStart_x;
                SnakeStart_y += FieldStart_y;

                cPortal = new Teleporter[iTeleport / 2];
                for (int x = 0; x < cPortal.Length; x++)
                {
                    cPortal[x] = new Teleporter();
                    cPortal[x].cPair[0] = cTeleporter[x * 2];
                    cPortal[x].cPair[1] = cTeleporter[x * 2 + 1];
                }
                cLoad.Close();
            }
            return;
        }
    }
}
