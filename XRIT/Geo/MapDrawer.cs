﻿using System;
using DotSpatial.Data;
using System.Drawing;

namespace OpenSatelliteProject.Geo {
    /// <summary>
    /// Map Drawer Class
    /// You can get ShapeFiles from: http://www.naturalearthdata.com
    /// Just load the .shp file
    /// </summary>
    public class MapDrawer {
        private readonly Shapefile shapeFile;
        
        public MapDrawer(string shapeFile) {
            this.shapeFile = Shapefile.OpenFile(shapeFile);
        }

        ~MapDrawer() {
            shapeFile.Close();
        }

        /// <summary>
        /// Draws the Map using the loaded Shapefile on bitmap.
        /// </summary>
        /// <param name="bmp">Bitmap to be draw</param>
        /// <param name="gc">Initialized GeoConverter</param>
        /// <param name="color">Color of the lines</param>
        /// <param name="lineWidth">Thickness of the Lines</param>
        public void DrawMap(ref Bitmap bmp, GeoConverter gc, Color color, int lineWidth = 5, bool fixCrop = false) {
            Pen pen = new Pen(color, lineWidth);
            float lastX = -1f;
            float lastY = -1f;

            using (var graphics = Graphics.FromImage(bmp)) {
                graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                lock(shapeFile) {   // TODO: This is BAD, SO BAD, PLEASE FIX ME
                                    // Thats because for some reason running this in multiple
                                    // threads is causing Features to be modified (wtf?)
                foreach (var f in shapeFile.Features.ToList()) {
                        for (int i = 0; i < f.NumGeometries; i++) {
                            var geom = f.GetBasicGeometryN (i);
                            var k = geom.Coordinates;

                            lastX = -1f;
                            lastY = -1f;

                            foreach (var z in k) {
                                float lon = (float)z.X;
                                float lat = (float)z.Y;
                                if (lat < gc.MaxLatitude && lat > gc.MinLatitude && lon < gc.MaxLongitude && lon > gc.MinLongitude) {
                                    var xy = gc.latlon2xy (lat, lon);
                                    float cx = (float)xy.Item1;
                                    float cy = (float)xy.Item2;

                                    if (fixCrop) {
                                        cx -= gc.CropLeft;
                                    }
                                    if (
                                        (lastX != -1f && lastY != -1f) &&
                                        (cx > 0 && cy > 0) &&
                                        (cx < bmp.Width && cy < bmp.Height) &&
                                        (lastX > 0 && lastY > 0) &&
                                        (lastX < bmp.Width && lastY < bmp.Height)) {
                                        graphics.DrawLine (pen, lastX, lastY, cx, cy);
                                    }
                                    lastX = cx;
                                    lastY = cy;
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}

