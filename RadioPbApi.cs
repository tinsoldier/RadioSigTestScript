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
    partial class Program
    {
        public class RadioPbApi
        {
            Func<IMyTerminalBlock, ICollection<MyDetectedEntityInfo>, int> _getAllBroadcasters;
            Func<IMyTerminalBlock, ICollection<MyTuple<int, string, string, Vector3D, bool, Color>>> _getGpsList;
            Func<IMyTerminalBlock, string, string, Vector3D, Color, bool, bool, MyTuple<int, string, string, Vector3D, bool, Color>> _createGps;
            Func<IMyTerminalBlock, int, string, string, Vector3D, Color, bool, bool, MyTuple<int, string, string, Vector3D, bool, Color>> _updateGps;
            Func<IMyTerminalBlock, int, bool> _deleteGps;

            public bool Activate(IMyTerminalBlock pbBlock)
            {
                var dict = pbBlock.GetProperty("RadioPbAPI")?.As<IReadOnlyDictionary<string, Delegate>>().GetValue(pbBlock);
                return ApiAssign(dict);
            }

            public bool ApiAssign(IReadOnlyDictionary<string, Delegate> delegates)
            {
                if (delegates == null) return false;

                AssignMethod(delegates, "GetAllBroadcasters", ref _getAllBroadcasters);
                AssignMethod(delegates, "GetGpsList", ref _getGpsList);
                AssignMethod(delegates, "CreateGps", ref _createGps);
                AssignMethod(delegates, "UpdateGps", ref _updateGps);
                AssignMethod(delegates, "DeleteGps", ref _deleteGps);

                return true;
            }

            private void AssignMethod<T>(IReadOnlyDictionary<string, Delegate> delegates, string name, ref T field) where T : class
            {
                if (delegates == null)
                {
                    field = null;
                    return;
                }
                Delegate del;
                if (!delegates.TryGetValue(name, out del))
                    throw new Exception($"{GetType().Name} :: Couldn't find {name} delegate of type {typeof(T)}");
                field = del as T;
                if (field == null)
                    throw new Exception($"{GetType().Name} :: Delegate {name} is not type {typeof(T)}, instead it's: {del.GetType()}");
            }

            public int GetAllBroadcasters(IMyTerminalBlock antennaBlock, ICollection<MyDetectedEntityInfo> output) => _getAllBroadcasters?.Invoke(antennaBlock, output) ?? 0;

            public ICollection<MyTuple<int, string, string, Vector3D, bool, Color>> GetGpsList(IMyTerminalBlock owner) => _getGpsList.Invoke(owner);
            public MyTuple<int, string, string, Vector3D, bool, Color> CreateGps(IMyTerminalBlock owner, string name, string description, Vector3D coords, Color color, bool showOnHud = true, bool temporary = false) => _createGps.Invoke(owner, name, description, coords, color, showOnHud, temporary);
            public MyTuple<int, string, string, Vector3D, bool, Color> UpdateGps(IMyTerminalBlock owner, int hash, string name, string description, Vector3D coords, Color color, bool showOnHud = true, bool temporary = false) => _updateGps.Invoke(owner, hash, name, description, coords, color, showOnHud, temporary);
            public bool DeleteGps(IMyTerminalBlock owner, int hash) => _deleteGps.Invoke(owner, hash);
        }
    }
}
