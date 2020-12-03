using System;
using System.Collections.Generic;
using System.Text;
using System.Numerics;

namespace FirstML
{
    public class Wall
    {
        public Vector2 startCor;            // Leftmost top corner of the wall
        public Vector2 endCor;              // Rightmost bottom corner of the wall
        public Vector2 direction;           //maybe not needed
        public int length;                  
        public float xSmall;
        public float xBig;
        public float ySmall;
        public float yBig;

        public Wall(Vector2 start, Vector2 dir, int l) //maybe remove?
        {
            startCor = start;
            direction = Vector2.Normalize(dir);
            length = l;
            endCor = Vector2.Add(start, Vector2.Multiply(l, direction));
        }

        public Wall(Vector2 start, Vector2 end)
        {
            startCor = start;
            endCor = end;
            direction = Vector2.Normalize(Vector2.Subtract(end, start));
            length = (int)Vector2.Distance(start, end);

            if (start.X <= end.X)
            {
                xSmall = start.X;
                xBig = end.X;
            }
            else
            {
                xSmall = end.X;
                xBig = start.X;
            }
            if (start.Y <= end.Y)
            {
                ySmall = start.Y;
                yBig = end.Y;
            }
            else
            {
                ySmall = end.Y;
                yBig = start.Y;
            }
        }
    }
}
