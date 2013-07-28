using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Blocks;

namespace Snake
{
    //This class creates two group_blocks representing the teleporters.
    class Teleporter
    {
        public group_block[] cPair;

        public Teleporter()
        {
            cPair = new group_block[2];
            cPair[0] = new group_block(0, 0);
            cPair[1] = new group_block(0, 0);
        }
        public int[] GetPosition(int ID) //Gets the position the class program needs to send the snake.
        {
            int[] Position = new int[2];
            Position[0] = cPair[ID].pos_x;
            Position[1] = cPair[ID].pos_y;
            return Position;
        }
    }
}
