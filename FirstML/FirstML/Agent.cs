using System;
using System.Collections.Generic;
using System.Text;
using System.Numerics;

namespace FirstML
{
    public class Agent
    {
        public bool pursuer;                                            // When true, this agent is the pursuer, if false it's the target
        public int speed;                                               // Currently set at 1 and not further implemented
        public int sight;                                               // Currently not implemented, would limit the range of sight and thus affect the inSight() function
        public Vector2 location;                                        // The agetns current location
        public Vector2 lastSeen;                                        // Last known location of the other player 

        public Agent(bool p, int spd, int sght, Vector2 startLoc)
        {
            pursuer = p;
            speed = spd;
            sight = sght;
            location = startLoc;
        }

        public bool Action(int dir, Field field, Agent other)                         // Dir is either north(0), east(1), south(2) or west(3) direction.
        {
            int newCor;
            Vector2 temp = location;

            if (dir == 0)                                               // Try to take a step north (y-1), making sure it's not of the map
            {
                newCor = (int) location.Y - 1;
                if (0 <= newCor)
                    location.Y = newCor;
            }
            else if (dir == 1)                                          // Try to take a step east (x+1), making sure it's not of the map
            {
                newCor = (int)location.X + 1;
                if (newCor < field.field.GetLength(0))
                    location.X = newCor;
            }
            else if (dir == 2)                                          //Try to take a step south (y+1), making sure it's not of the map
            {
                newCor = (int)location.Y + 1;
                if (newCor < field.field.GetLength(1))
                    location.Y = newCor;
            }
            else if (dir == 3)                                          //Try to take a step west (x-1), making sure it's not of the map
            {
                newCor = (int)location.X - 1;
                if (0 <= newCor)
                    location.X = newCor;
            }
            else
            {
                //incorect input
            }

            if (field.field[(int)location.X, (int)location.Y] == 1)
                location = temp;
            else if (location == other.location)
                return true;

            if (pursuer && location != temp)                                // Updates the map if the agents has moved
            {
                field.field[(int)location.X, (int)location.Y] = 0;
                field.field[(int)location.X, (int)location.Y] = 2;
            }
            else if (!pursuer && location != temp)
            {
                field.field[(int)location.X, (int)location.Y] = 0;
                field.field[(int)location.X, (int)location.Y] = 3;
            }

            return false;
        }
    }
}
