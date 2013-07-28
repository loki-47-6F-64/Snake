using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Blocks;

namespace Snake
{
    class Food
    {
        //protected int iSize_x, iSize_y;

        protected int iMax_x, iMax_y; //The maximums for the Random class

        public group_block cFood;
        protected Random cRandom;

        public Food(int Max_x, int Max_y)
        {
            iMax_x = Max_x - 1;
            iMax_y = Max_y - 1;
        }
        public virtual void NewFood()
        {
            cRandom = new Random();
            cFood = new group_block(0, 0); //The positions are put in later.
            cFood.SetShape('#', 0, 0);
        }
        public void MoveFood()
        {
            cFood.pos_x = cRandom.Next(Field.FieldStart_x + 1, Field.FieldStart_x + iMax_x);
            cFood.pos_y = cRandom.Next(Field.FieldStart_y + 1, Field.FieldStart_y + iMax_y);
        }
        public void PrintFood()
        {
            cFood.PrintShape();
        }
        //Clears the column at the spot where the food is.
        public void ClearFood()
        {
            cFood.Clear();
        }
    }
    class SpecialFood : Food
    {
        public SpecialFood(int Max_x, int Max_y)
            : base(Max_x, Max_y)
        {
            iMax_x = Max_x - 1;
            iMax_y = Max_y - 1;
        }
        public override void NewFood()
        {
            cRandom = new Random();
            cFood = new group_block(0, 0); //The positions are put in later.
            cFood.SetShape('$', 0, 0);
        }
    }
}
