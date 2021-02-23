﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Numerics;
using System.Drawing;
using BaseSLAM;
using HectorSLAM.Util;
using System.Runtime.CompilerServices;

namespace HectorSLAM.Map
{
    public class GridMapBase<T> where T : ICell
    {
        protected readonly T[] mapArray;          // Map representation used with plain pointer array.
        private readonly Matrix3x2 worldTmap;     // Homogenous transform from map to world coordinates.
        private readonly Matrix3x2 mapTworld;     // Homogenous transform from world to map coordinates.
        private int lastUpdateIndex = -1;

        /// <summary>
        /// Map properties
        /// </summary>
        public MapProperties Properties { get; }

        /// <summary>
        /// Shortcut to map dimensions
        /// </summary>
        public Point Dimensions => Properties.Dimensions;

        /// <summary>
        /// Constructor, creates grid representation and transformations.
        /// </summary>
        /// <param name="mapResolution">Map resolution in meters per pixel</param>
        /// <param name="size">Map size in pixels</param>
        /// <param name="offset">Offset if meters</param>
        public GridMapBase(float mapResolution, Point size, Vector2 offset)
        {
            Properties = new MapProperties(mapResolution, size, offset);

            // Construct map rray
            int length = Dimensions.X * Dimensions.Y;
            mapArray = new T[length];

            for (int i = 0; i < length; ++i)
            {
                mapArray[i] = (T)Activator.CreateInstance(typeof(T));
                mapArray[i].Reset();
            }

            // Construct transformation matrix
            mapTworld = Matrix3x2.CreateScale(Properties.ScaleToMap) * Matrix3x2.CreateTranslation(Properties.Offset.X, Properties.Offset.Y);
            if (!Matrix3x2.Invert(mapTworld, out worldTmap))
            {
                throw new Exception("Map to world matrix is not invertible");
            }
        }

        /// <summary>
        /// Reset map
        /// </summary>
        public void Reset()
        {
            Clear();
        }

        /// <summary>
        /// Resets the grid cell values by using the resetGridCell() function.
        /// </summary>
        public void Clear()
        {
            mapArray.ForEach(i => i.Reset());
        }

        /// <summary>
        /// Get cell by coordinates
        /// </summary>
        /// <param name="x">X</param>
        /// <param name="y">Y</param>
        /// <returns>Cell</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetCell(int x, int y)
        {
            return mapArray[y * Dimensions.X + x];
        }

        /// <summary>
        /// Get cell by index
        /// </summary>
        /// <param name="index">Arry index</param>
        /// <returns>Cell</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetCell(int index)
        {
            return mapArray[index];
        }

        /**
         * Returns the world coordinates for the given map coords.
         */
        public Vector2 GetWorldCoords(Vector2 mapCoords)
        {
            return Vector2.Transform(mapCoords, worldTmap);
        }

        /**
         * Returns the map coordinates for the given world coords.
         */
        public Vector2 GetMapCoords(Vector2 worldCoords)
        {
            return Vector2.Transform(worldCoords, mapTworld);
        }

        /**
         * Returns the world pose for the given map pose.
         */
        public Vector3 GetWorldCoordsPose(Vector3 mapPose)
        {
            Vector2 worldCoords = Vector2.Transform(mapPose.ToVector2(), worldTmap);
            return new Vector3(worldCoords.X, worldCoords.Y, mapPose.Z);
        }

        /// <summary>
        /// Returns the map pose for the given world pose.
        /// </summary>
        /// <param name="worldPose">World pose (X and Y in meters, Z in radians)</param>
        /// <returns>Map pose (X and Y in pixels, Z in radians)</returns>
        public Vector3 GetMapCoordsPose(Vector3 worldPose)
        {
            Vector2 mapCoords = Vector2.Transform(worldPose.ToVector2(), mapTworld);
            return new Vector3(mapCoords.X, mapCoords.Y, worldPose.Z);
        }

        protected void SetUpdated()
        {
            lastUpdateIndex++;
        }

        /// <summary>
        /// Returns the rectangle ([xMin,yMin],[xMax,xMax]) containing non-default cell values
        /// </summary>
        /// <param name="xMax"></param>
        /// <param name="yMax"></param>
        /// <param name="xMin"></param>
        /// <param name="yMin"></param>
        /// <returns></returns>
        public bool GetMapExtends(out int xMax, out int yMax, out int xMin, out int yMin)
        {
            int lowerStart = -1;
            int upperStart = 10000;

            int xMaxTemp = lowerStart;
            int yMaxTemp = lowerStart;
            int xMinTemp = upperStart;
            int yMinTemp = upperStart;

            for (int x = 0; x < Dimensions.X; ++x)
            {
                for (int y = 0; y < Dimensions.Y; ++y)
                {
                    if (mapArray[y * Dimensions.X + x].Value != 0.0f)
                    {
                        if (x > xMaxTemp)
                        {
                            xMaxTemp = x;
                        }

                        if (x < xMinTemp)
                        {
                            xMinTemp = x;
                        }

                        if (y > yMaxTemp)
                        {
                            yMaxTemp = y;
                        }

                        if (y < yMinTemp)
                        {
                            yMinTemp = y;
                        }
                    }
                }
            }

            if ((xMaxTemp != lowerStart) &&
                (yMaxTemp != lowerStart) &&
                (xMinTemp != upperStart) &&
                (yMinTemp != upperStart)) {

                xMax = xMaxTemp;
                yMax = yMaxTemp;
                xMin = xMinTemp;
                yMin = yMinTemp;

                return true;
            }
            else
            {
                xMax = 0;
                yMax = 0;
                xMin = 0;
                yMin = 0;

                return false;
            }
        }
    }
}
