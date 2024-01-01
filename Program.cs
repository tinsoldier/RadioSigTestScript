using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        const double BIT_SPACING = 255.0 / 7.0;

        RadioPbApi _radioPbApi;
        bool _initialized = false;
        private ICollection<MyTuple<int, string, string, Vector3D, bool, Color>> _gpsList;

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;

            _radioPbApi = new RadioPbApi();
            _radioPbApi.Activate(Me);

            var surface = Me.GetSurface(0);
            surface.ContentType = ContentType.TEXT_AND_IMAGE;
            surface.BackgroundColor = new Color(0, 0, 0); 
            surface.FontColor = new Color(255, 255, 255); 
            surface.FontSize = 0.95f;
            surface.Font = "Monospace";
            surface.Alignment = VRage.Game.GUI.TextPanel.TextAlignment.LEFT;

            Echo("Setup Success");
        }

        public void Main(string argument, UpdateType updateSource)
        {
            if(updateSource == UpdateType.Update100)
            {
                UpdateGpsListDisplay();
                CheckGpsProximity();
            }

            if (argument.ToLower() == "test")
            {
                //test the antenna/broadcaster api
                RunAntennaTest();
            }

            if (argument.ToLower() == "create")
            {
                CreateGps();
            }
        }

        private void CreateGps()
        {
            // Find a camera on the grid
            var cameras = GetBlocksWithName<IMyCameraBlock>("Camera");
            if (cameras.Count == 0)
            {
                Echo("No camera found");
                return;
            }

            var camera = cameras[0] as IMyCameraBlock;
            if (!camera.IsFunctional)
            {
                Echo("Camera is not functional");
                return;
            }

            // Enable raycast
            camera.EnableRaycast = true;

            var fixedRange = 40000;

            if (camera.AvailableScanRange < fixedRange)
            {
                Echo("Camera range is too short");
                return;
            }

            if (!camera.CanScan(fixedRange))
            {
                Echo("Camera cannot scan");
                return;
            }

            // Perform a raycast
            var raycastInfo = camera.Raycast(fixedRange);

            if (!raycastInfo.IsEmpty())
            {
                // Raycast hit something, create a GPS
                var hitPosition = raycastInfo.HitPosition.Value;

                var numAsteroidGps = _gpsList.Count(g => g.Item2.Contains("Asteroid"));

                var gpsName = $"{raycastInfo.Type} #{numAsteroidGps + 1}";

                if ((hitPosition - Me.GetPosition()).Length() > 1000f)
                {
                    gpsName += " (Unvisited)";
                }
                else
                {
                    gpsName += " (Visited)";
                }

                //TODO: Use entityId to track distinct asteroids so that multiple hits on the same asteroid don't create multiple GPS points or they share the same #
                var description = $"EntityId:{raycastInfo.EntityId}";

                var color = GetRandomColor();
                _radioPbApi.CreateGps(Me, gpsName, description, hitPosition, color);

                Echo($"Created GPS: {gpsName}");
            }
            else
            {
                Echo("Raycast did not hit anything");
            }
        }

        private void RunAntennaTest()
        {
            List<IMyTerminalBlock> antennaBlocks = GetBlocksWithName<IMyRadioAntenna>("Antenna");
            if (antennaBlocks.Count == 0)
            {
                Echo("no antennas");
                return;
            }

            List<MyDetectedEntityInfo> broadcasters = new List<MyDetectedEntityInfo>();
            var cnt = _radioPbApi.GetAllBroadcasters(antennaBlocks[0], broadcasters);

            if (cnt <= 0)
            {
                Echo("no broadcasters: " + cnt.ToString());
                return;
            }

            Echo(string.Join("\n", broadcasters.Select(b => b.Name)));
        }

        private void CheckGpsProximity()
        {
            //If distance between this grid and any GPS' is less than 1000m, update the GPS to be gray and have the text (Visited) added to the name.

            //Foreach gps in _gpsList, filter by distance < 1000m
            _gpsList
                .Where(g => g.Item4 != null && (Me.GetPosition() - g.Item4).Length() < 1000)
                .Where(g => !g.Item2.Contains("Visited"))
                .ToList().ForEach(g =>
            {
                //Update the gps to be gray and have the text (Visited) added to the name.
                var newName = g.Item2.Replace("Unvisited", "Visited");
                var updatedGps = _radioPbApi.UpdateGps(Me, g.Item1, newName, g.Item3, g.Item4, new Color(128, 128, 128));
            });
        }

        private void UpdateGpsListDisplay()
        {
            _gpsList = _radioPbApi.GetGpsList(Me);

            if (_gpsList == null || _gpsList.Count == 0)
            {
                WriteToLCD("No GPS results", Me.GetSurface(0));
                return;
            }

            StringBuilder sb = new StringBuilder();

            var cameras = GetBlocksWithName<IMyCameraBlock>("Camera");
            if (cameras.Count == 0)
            {
                Echo("No camera found");
                return;
            }

            var camera = cameras[0] as IMyCameraBlock;
            if (!camera.IsFunctional)
            {
                Echo("Camera is not functional");
                return;
            }

            // Enable raycast
            camera.EnableRaycast = true;

            // Perform a raycast
            sb.AppendLine($"Raycast range: {camera.AvailableScanRange}");

            foreach (var gps in _gpsList)
            {
                Color color = gps.Item6;
                char colorChar = ColorToChar(color.R, color.G, color.B);
                double distance = (Me.GetPosition() - gps.Item4).Length(); // Calculate distance from PB to GPS

                sb.AppendLine($"{colorChar} {gps.Item2} - {distance:F2}m");
            }

            WriteToLCD(sb.ToString(), Me.GetSurface(0));
        }

        private Color GetRandomColor()
        {
            // Pick a random color
            Random rnd = new Random();
            float red = (float)rnd.NextDouble();
            float grn = (float)rnd.NextDouble();
            float blu = (float)rnd.NextDouble();

            // Ensure at least one component is greater than 0.8
            switch (rnd.Next(3)) // Pick one of the three components randomly
            {
                case 0:
                    red = 0.8f + 0.2f * red; // Ensure red is bright
                    break;
                case 1:
                    grn = 0.8f + 0.2f * grn; // Ensure green is bright
                    break;
                case 2:
                    blu = 0.8f + 0.2f * blu; // Ensure blue is bright
                    break;
            }

            // Avoid all components being too low
            grn = grn > 0.3f ? grn : 0.3f + 0.7f * grn;
            blu = blu > 0.3f ? blu : 0.3f + 0.7f * blu;

            var color = new Color(red, grn, blu);
            return color;
        }

        private void WriteToLCD(string text, IMyTextSurface surface)
        {
            surface.WriteText(text, false);
        }

        public static char ColorToChar(byte r, byte g, byte b)
        {
            const double BIT_SPACING = 255.0 / 7.0;
            return (char)(0xe100 + ((int)Math.Round(r / BIT_SPACING) << 6) + ((int)Math.Round(g / BIT_SPACING) << 3) + (int)Math.Round(b / BIT_SPACING));
        }


        List<IMyTerminalBlock> GetBlocksWithName<T>(string name) where T : class, IMyTerminalBlock
        {
            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.SearchBlocksOfName(name, blocks);

            List<IMyTerminalBlock> filteredBlocks = new List<IMyTerminalBlock>();
            for (int i = 0; i < blocks.Count; i++)
            {
                IMyTerminalBlock block = blocks[i] as T;
                if (block != null)
                {
                    filteredBlocks.Add(block);
                }
            }

            return filteredBlocks;
        }
    }
}
