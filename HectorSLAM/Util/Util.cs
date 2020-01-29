﻿using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace HectorSLAM.Util
{
    public static class Util
    {
        public static float NormalizeAnglePos(float angle)
        {
            float pi2 = MathF.PI * 2.0f;

            return ((angle % pi2) + pi2) % pi2;
        }

        public static float NormalizeAngle(float angle)
        {
            float a = NormalizeAnglePos(angle);

            if (a > MathF.PI)
            {
                a -= 2.0f * MathF.PI;
            }

            return a;
        }

        public static float Sqr(float val)
        {
            return val * val;
        }

        public static float ToDeg(float radVal)
        {
            return radVal * (180.0f / MathF.PI);
        }

        public static float ToRad(float degVal)
        {
            return degVal * (MathF.PI / 180.0f);
        }

        public static bool PoseDifferenceLargerThan(Vector3 pose1, Vector3 pose2, float distanceDiffThresh, float angleDiffThresh)
        {
            // Check distance
            if (Vector2.Distance(new Vector2(pose1.X, pose1.Y), new Vector2(pose2.X, pose2.Y)) > distanceDiffThresh)
            {
                return true;
            }

            float angleDiff = pose1.Z - pose2.Z;

            if (angleDiff > MathF.PI)
            {
                angleDiff -= MathF.PI * 2.0f;
            }
            else if (angleDiff < -MathF.PI)
            {
                angleDiff += MathF.PI * 2.0f;
            }

            if (MathF.Abs(angleDiff) > angleDiffThresh)
            {
                return true;
            }

            return false;
        }
    }
}
