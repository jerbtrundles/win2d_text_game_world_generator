﻿using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Input;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace win2d_text_game_world_generator
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        Map map;

        public MainPage()
        {
            this.InitializeComponent();
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.FullScreen;

            Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;
            Window.Current.CoreWindow.KeyUp += CoreWindow_KeyUp;
        }

        private void CoreWindow_KeyUp(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.KeyEventArgs args)
        {

        }

        private void CoreWindow_KeyDown(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.KeyEventArgs args)
        {
            switch (args.VirtualKey)
            {
                case Windows.System.VirtualKey.H:
                    Statics.DebugMapDrawType = MapDrawType.HEIGHTMAP;
                    Statics.HeightMapOpacity = 255;
                    break;
                case Windows.System.VirtualKey.R:
                    Statics.DebugMapDrawType = MapDrawType.REGIONS;
                    Statics.HeightMapOpacity = 75;
                    break;
                case Windows.System.VirtualKey.P:
                    Statics.DebugDrawPaths = !Statics.DebugDrawPaths;
                    break;
                case Windows.System.VirtualKey.S:
                    Statics.DebugDrawSubregions = !Statics.DebugDrawSubregions;
                    break;
                case Windows.System.VirtualKey.D:
                    Statics.DebugDrawDebug = !Statics.DebugDrawDebug;
                    break;
                case Windows.System.VirtualKey.G:
                    Statics.DebugDrawGrid = !Statics.DebugDrawGrid;
                    break;
            }
        }

        #region Mouse
        private void gridMain_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            PointerPointProperties p = e.GetCurrentPoint(gridMain).Properties;
            if (p.IsLeftButtonPressed)
            {
                Statics.DebugDrawSubregions = !Statics.DebugDrawSubregions;
            }
            else if (p.IsRightButtonPressed)
            {
                // Reset();

                Statics.RollingReset = !Statics.RollingReset;
                if (Statics.RollingReset)
                {
                    RollingReset();
                }
            }
        }
        private void gridMain_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
        }
        private void gridMain_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            Point p = e.GetCurrentPoint(gridMain).Position;
            Statics.MouseX = p.X;
            Statics.MouseY = p.Y;
            int x = (int)(p.X - Statics.Padding) / Statics.PixelScale;
            int y = (int)(p.Y - Statics.Padding) / Statics.PixelScale;
            Statics.CurrentMouseRegion = map.GetRegion(x, y);
            if (Statics.CurrentMouseRegion != null)
            {
                Statics.CurrentMouseSubregion = map.GetSubregion(Statics.CurrentMouseRegion, x, y);

                if (Statics.CurrentMouseSubregion != null)
                {
                    Room room = map.GetRoom(Statics.CurrentMouseSubregion, x, y);
                    if (room != null)
                    {
                        Statics.DebugHeightString = "Elevation: " + room.Elevation.ToString();
                    }
                }
            }
            else
            {
                Statics.CurrentMouseSubregion = null;
            }
        }
        #endregion

        #region Draw
        private void canvasMain_Draw(ICanvasAnimatedControl sender, CanvasAnimatedDrawEventArgs args)
        {
            switch (Statics.DebugMapDrawType)
            {
                case MapDrawType.REGIONS:
                    map.DrawRegions(args);
                    break;
                case MapDrawType.HEIGHTMAP:
                    map.DrawHeightMap(args);
                    break;
            }

            if (Statics.DebugDrawDebug)
            {
                DrawDebug(args);
            }
        }

        private void DrawDebug(CanvasAnimatedDrawEventArgs args)
        {
            lock (Statics.lockDebugLists)
            {
                DrawDebugRadar(args);

                List<string> DebugStrings = new List<string>();

                // populate list
                if (Statics.MapCreationTimes != null && Statics.MapCreationTimes.Count > 0)
                {
                    DebugStrings.Add("Average map creation time: " + Statics.MapCreationTimes.Average().ToString() + "ms");
                    DebugStrings.Add("Min map creation time: " + Statics.MapCreationTimes.Min().ToString() + "ms");
                    DebugStrings.Add("Max map creation time: " + Statics.MapCreationTimes.Max().ToString() + "ms");
                }
                if(map != null)
                {
                    DebugStrings.Add("Last map creation time: " + map.DebugCreationTime.TotalMilliseconds.ToString() + "ms");
                }
                if (Statics.MapAbortCounts != null && Statics.MapAbortCounts.Count > 0)
                {
                    DebugStrings.Add("Average map abort count: " + Statics.MapAbortCounts.Average().ToString());
                    DebugStrings.Add("Min map abort count: " + Statics.MapAbortCounts.Min().ToString());
                    DebugStrings.Add("Max map abort count: " + Statics.MapAbortCounts.Max().ToString());
                }
                if(map!= null)
                {
                    DebugStrings.Add("Last map abort count: " + map.DebugAbortedCount.ToString());
                }

                DebugStrings.Add(Statics.DebugMapTotalTileCountString);
                DebugStrings.Add("Map width (tiles): " + map.WidthInTiles.ToString());
                DebugStrings.Add("Map height (tiles): " + map.HeightInTiles.ToString());
                DebugStrings.Add("Map count: " + Statics.MapCount.ToString());
                DebugStrings.Add(Statics.DebugHeightString);

                // draw
                Rect DebugRect = new Rect(1500, 10, 400, 560);
                args.DrawingSession.FillRectangle(DebugRect, Colors.CornflowerBlue);

                float fCurrentY = 20;
                foreach (string strDebugString in DebugStrings)
                {
                    args.DrawingSession.DrawText(strDebugString, new Vector2(1510, fCurrentY), Colors.White);
                    fCurrentY += 20;
                }

                //if (Statics.FixLoopCounts != null && Statics.FixLoopCounts.Count > 0)
                //{
                //    args.DrawingSession.DrawText("Average fix loop count: " + Statics.FixLoopCounts.Average().ToString(), new Vector2(1510, 80), Colors.White);
                //    args.DrawingSession.DrawText("Min fix loop count: " + Statics.FixLoopCounts.Min().ToString(), new Vector2(1510, 100), Colors.White);
                //    args.DrawingSession.DrawText("Max fix loop count: " + Statics.FixLoopCounts.Max().ToString(), new Vector2(1510, 120), Colors.White);
                //}
                //args.DrawingSession.DrawText(Statics.DebugMapTotalRegionCountString, new Vector2(1510, 140), Colors.White);

                //args.DrawingSession.DrawText("Mouse: " + ((int)Statics.MouseX).ToString() + ", " + ((int)Statics.MouseY).ToString(), new Vector2(1510, 180), Colors.White);
                //if (Statics.CurrentMouseRegion != null)
                //{
                //    args.DrawingSession.DrawText("Region ID: " + Statics.CurrentMouseRegion.ID.ToString(), new Vector2(1510, 200), Colors.White);
                //    args.DrawingSession.DrawText("Region name: " + Statics.CurrentMouseRegion.Name, new Vector2(1510, 220), Colors.White);
                //    args.DrawingSession.DrawText("Region room count: " + Statics.CurrentMouseRegion.RoomCount.ToString(), new Vector2(1510, 240), Colors.White);
                //    args.DrawingSession.DrawText("Region subregion count: " + Statics.CurrentMouseRegion.Subregions.Count.ToString(), new Vector2(1510, 260), Colors.White);
                //}

                //if (Statics.CurrentMouseSubregion != null)
                //{
                //    args.DrawingSession.DrawText("Subregion: " + Statics.CurrentMouseSubregion.ID.ToString(), new Vector2(1510, 280), Colors.White);
                //    args.DrawingSession.DrawText("Subregion room count: " + Statics.CurrentMouseSubregion.Rooms.Count.ToString(), new Vector2(1510, 300), Colors.White);
                //}

                //args.DrawingSession.DrawText("Frequency: " + Statics.fFrequency.ToString(), new Vector2(1510, 340), Colors.White);
                //args.DrawingSession.DrawText("Amplitude: " + Statics.fAmplitude.ToString(), new Vector2(1510, 360), Colors.White);
                //args.DrawingSession.DrawText("Persistence: " + Statics.fPersistence.ToString(), new Vector2(1510, 380), Colors.White);
                //args.DrawingSession.DrawText("Octaves: " + Statics.nOctaves.ToString(), new Vector2(1510, 400), Colors.White);

                //args.DrawingSession.DrawText("NW: " + Statics.DebugNWConnectionCount.ToString(), new Vector2(1510, 340), Colors.White);
                //args.DrawingSession.DrawText("N: " + Statics.DebugNConnectionCount.ToString(), new Vector2(1510, 360), Colors.White);
                //args.DrawingSession.DrawText("NE: " + Statics.DebugNEConnectionCount.ToString(), new Vector2(1510, 380), Colors.White);
                //args.DrawingSession.DrawText("W: " + Statics.DebugWConnectionCount.ToString(), new Vector2(1510, 400), Colors.White);
                //args.DrawingSession.DrawText("E: " + Statics.DebugEConnectionCount.ToString(), new Vector2(1510, 420), Colors.White);
                //args.DrawingSession.DrawText("SW: " + Statics.DebugSWConnectionCount.ToString(), new Vector2(1510, 440), Colors.White);
                //args.DrawingSession.DrawText("S: " + Statics.DebugSConnectionCount.ToString(), new Vector2(1510, 460), Colors.White);
                //args.DrawingSession.DrawText("SE: " + Statics.DebugSEConnectionCount.ToString(), new Vector2(1510, 480), Colors.White);


            }
        }
        private void DrawDebugRadar(CanvasAnimatedDrawEventArgs args)
        {
            CanvasTextLayout LayoutHundred = new CanvasTextLayout(args.DrawingSession, (100 / Statics.PixelScale).ToString(), Statics.FontSmall, 0, 0);
            args.DrawingSession.DrawTextLayout(LayoutHundred, new Vector2((float)Statics.MouseX - (float)LayoutHundred.LayoutBounds.Width / 2, (float)Statics.MouseY - 70), Colors.White);

            CanvasTextLayout LayoutTwoHundred = new CanvasTextLayout(args.DrawingSession, (200 / Statics.PixelScale).ToString(), Statics.FontSmall, 0, 0);
            args.DrawingSession.DrawTextLayout(LayoutTwoHundred, new Vector2((float)Statics.MouseX - (float)LayoutHundred.LayoutBounds.Width / 2, (float)Statics.MouseY - 120), Colors.White);

            CanvasTextLayout LayoutThreeHundred = new CanvasTextLayout(args.DrawingSession, (300 / Statics.PixelScale).ToString(), Statics.FontSmall, 0, 0);
            args.DrawingSession.DrawTextLayout(LayoutThreeHundred, new Vector2((float)Statics.MouseX - (float)LayoutHundred.LayoutBounds.Width / 2, (float)Statics.MouseY - 170), Colors.White);

            args.DrawingSession.DrawRectangle(new Rect(Statics.MouseX - 50, Statics.MouseY - 50, 100, 100), Colors.White);
            args.DrawingSession.DrawRectangle(new Rect(Statics.MouseX - 100, Statics.MouseY - 100, 200, 200), Colors.White);
            args.DrawingSession.DrawRectangle(new Rect(Statics.MouseX - 150, Statics.MouseY - 150, 300, 300), Colors.White);
        }
        #endregion

        #region Update
        private void canvasMain_Update(ICanvasAnimatedControl sender, CanvasAnimatedUpdateEventArgs args)
        {
            Statics.FrameCount++;
            // map.Update(args);
        }
        #endregion

        #region Initialization
        private void canvasMain_CreateResources(CanvasAnimatedControl sender, Microsoft.Graphics.Canvas.UI.CanvasCreateResourcesEventArgs args)
        {
            args.TrackAsyncAction(CreateResourcesAsync(sender).AsAsyncAction());
        }

        private async Task CreateResourcesAsync(CanvasAnimatedControl sender)
        {
            // c.TargetElapsedTime = new TimeSpan(0, 0, 0, 0, 200);
            // c.Paused = true;

            Statics.CanvasWidth = (int)sender.Size.Width;
            Statics.CanvasHeight = (int)sender.Size.Height;
            Reset();
        }

        private void Reset()
        {
            Statics.DebugNWConnectionCount = 0;
            Statics.DebugNConnectionCount = 0;
            Statics.DebugNEConnectionCount = 0;
            Statics.DebugWConnectionCount = 0;
            Statics.DebugEConnectionCount = 0;
            Statics.DebugSWConnectionCount = 0;
            Statics.DebugSConnectionCount = 0;
            Statics.DebugSEConnectionCount = 0;

            Statics.FrameCount = 0;
            Statics.MapCount++;

            map = Map.Create(Statics.MapWidthInPixels, Statics.MapHeightInPixels);

            lock (Statics.lockDebugLists)
            {
                Statics.MapCreationTimes.Add(map.DebugCreationTime.TotalMilliseconds);
                Statics.MapAbortCounts.Add(map.DebugAbortedCount);
            }

            Statics.DebugMapCreationTimeString = "Map creation time: " + map.DebugCreationTime.TotalMilliseconds.ToString() + "ms";
            Statics.DebugMapTotalRegionCountString = "Total regions: " + map.Regions.Count.ToString();
            Statics.DebugMapTotalTileCountString = "Total tiles: " + (map.WidthInTiles * map.HeightInTiles).ToString();

            if (map.DebugCreationTime.TotalMilliseconds > 20000) { Statics.RollingReset = false; }
        }

        private void RollingReset()
        {
            while (Statics.RollingReset)
            {
                Reset();
            }
        }
        #endregion
    }
}
