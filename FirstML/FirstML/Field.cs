using System;
using System.Collections.Generic;
using System.Text;
using System.Numerics;

namespace FirstML
{
    public class Field
    {
        public int[,] field;                                    //Size of the test area; 0=empty, 1=wall, 2=pursuer, 3=target
        public Wall[] walls;

        public Field(int width, int hight,  Wall[] muur)
        {
            field = new int[width, hight];
            walls = muur;

            int x;
            int y;
            Vector2 temp;

            foreach (Wall w in walls)                                                           // Adds the wall segments to the field
            {
                for (int i = 0; i < w.length; i++)
                {
                    temp = Vector2.Add(w.startCor, Vector2.Multiply(i, w.direction));
                    x = (int)temp.X;
                    y = (int)temp.Y;
                    field[x, y] = 1;
                }
            }
        }
    }
}
