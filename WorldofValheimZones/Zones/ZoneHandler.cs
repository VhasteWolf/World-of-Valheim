
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using System.Globalization;



namespace WorldofValheimZones
{
    public static class ZoneHandler
    {
        public class ZoneTypes
        {
            //What affect does the zone provide?
            public string Name = "Unknown";
            // PVP settings
            public bool PVP = false;
            public bool PVPEnforce = false;
            // Show position settings
            public bool ShowPosition = true;
            public bool PositionEnforce = false;
            // Message annoucement setting
            public bool MessageAnnoucement = true;
            // Administrators of the zone
            public string Admins = "null";
            // Configurations in the zone
            public string Configurations = "none";
        }
        public struct Zone
        {
            public int ID; // Use for id to maintain current zone
            public string Name;
            public string Type;
            public int Priority;
            public string Shape;//0 - circle, 1 - square, 2 - coords
            public Vector2 Position;
            public float Radius;
            public Zone(int _ID, string _Name, string _Type, int _Priority, string _Shape, Vector2 _Position,
                  float _Radius)
            {
                ID = _ID;
                Name = _Name;
                Type = _Type;
                Priority = _Priority;
                Shape = _Shape;
                Position = _Position;
                Radius = _Radius;
            }
        }
        public static int CurrentZoneID = -2; // -2 initial, -1 wilderness, 0 up are zones
        // List of all the zones
        public static List<Zone> Zones = new List<Zone>();
        public static List<ZoneTypes> ZoneT = new List<ZoneTypes>();
        // Generic debug output
        // Do not change name to debug. Will break "debug()" function in class.
#if DEBUG
        public static void _debug(Zone z)
        {
            Debug.Log($"   {z.ID} ({z.Name}, {z.Type}, {z.Priority}, {z.Position}, {z.Radius})");
        }

        public static void _debug(List<Zone> z)
        {
            Debug.Log("Loaded Zone Data: ");
            Debug.Log("  Zone Cnt: " + z.Count);

            using (List<Zone>.Enumerator enumerator = z.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    _debug(enumerator.Current);
                }
            }
        }
        public static void _debug(ZoneTypes zt)
        {
            Debug.Log($"  Type: {zt.Name} -> [ {zt.PVP}, {zt.PVPEnforce}, {zt.ShowPosition}, {zt.PositionEnforce}, {zt.MessageAnnoucement}, [{zt.Admins}], [{zt.Configurations}] ]");
        }


        public static void _debug(List<ZoneTypes> zt)
        {
            Debug.Log("Loaded Zone Type Data: ");
            Debug.Log("  Zone Type Cnt: " + zt.Count);

            using (List<ZoneTypes>.Enumerator enumerator = zt.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    _debug(enumerator.Current);
                }
            }
        }

        public static void _debug()
        {
            _debug(ZoneT);
            _debug(Zones);
        }
#endif
        //List all the zones were are currently occupy
        public static List<Zone> ListOccupiedZones(Vector3 point)
        {
            Vector2 a = new Vector2(point.x, point.z);
            List<Zone> occupiedZones = new List<Zone>();

            foreach (Zone checkZone in Zones)
            {
                switch (checkZone.Shape)
                {
                    case "square":
                        // Square check if you are in the boundaries
                        float boundary = checkZone.Radius;// / 2;
                        if (((checkZone.Position.x + boundary) > a.x) &&
                            ((checkZone.Position.x - boundary) < a.x) &&
                            ((checkZone.Position.y + boundary) > a.y) &&
                            ((checkZone.Position.y - boundary) < a.y))
                        {
                            occupiedZones.Add(checkZone);
                        }
                        break;
                    /*
                    case 2:
                        // Coords checks
                        break;
                    */
                    default:
                        //Default: We are a circle.
                        if (Vector2.Distance(a, checkZone.Position) <= checkZone.Radius)
                        {
                            occupiedZones.Add(checkZone);
                        }
                        break;
                }
            }
            return occupiedZones;
        }
        // Output the zone that we will use in the current area.
        // Will go through and find which one has the highest priority.
        public static Zone TopZone(List<Zone> z)
        {
            // Sort the Zone list and output the one on top.
            z.Sort((Zone a, Zone b) => a.Priority.CompareTo(b.Priority));
            return z[0];
        }
        public static ZoneTypes FindZoneType(string ztType)
        {
            //Debug.Log($"Searching for: {ztName}");
            bool contains = ZoneT.Any(b => b.Name == ztType);
            if (!contains)
                return new ZoneTypes();
            else
                return ZoneT.Find(a => a.Name == ztType) ?? new ZoneTypes();
        }
        public static bool Detect(Vector3 position, out bool changed, out Zone z, out ZoneTypes zt)
        {
            List<Zone> zlist = ListOccupiedZones(position);
            if (zlist.Count == 0)
            {
                // No Zones occupied (We are in the wilderness)
                z = new Zone();
                zt = new ZoneTypes();

                // Did we change to the wilderness?
                if (CurrentZoneID != -1)
                {
                    CurrentZoneID = -1;
                    zt = FindZoneType("wilderness");
                    changed = true;
                }
                else
                {
                    changed = false;
                }
                return false;

            }
            else
            {
                // We are in a zone
                z = TopZone(zlist);
                zt = new ZoneTypes();

                if (CurrentZoneID != z.ID)
                {
                    zt = FindZoneType(z.Type);//.ToLower());
                    CurrentZoneID = z.ID;
                    changed = true;
                }
                else
                {
                    changed = false;
                }
                return true;

            }
        }
        public static ZPackage Serialize(string SteamID)
        {
            ZPackage zip = new ZPackage();
            zip.Write(ZoneT.Count);
            foreach (ZoneTypes zt in ZoneT)
            {
                zip.Write(zt.Name);
                zip.Write(zt.PVP);
                zip.Write(zt.PVPEnforce);
                zip.Write(zt.ShowPosition);
                zip.Write(zt.PositionEnforce);
                zip.Write(zt.MessageAnnoucement);
                if (ValheimPermissions.ValheimDB.CheckUserAbsolutePermission(SteamID, "HackShardGaming.WoV-Zones.Override." + zt.Name))
                {
                    if (zt.Admins == "null")
                    {
                        zt.Admins = SteamID;
                    }
                    else
                    {
                        zt.Admins = SteamID + " " + zt.Admins;
                    }
                }
                zip.Write(zt.Admins);
                zip.Write(zt.Configurations);
            }
            zip.Write(Zones.Count);
            foreach (Zone z in Zones)
            {
                zip.Write(z.ID);
                zip.Write(z.Name);
                zip.Write(z.Type);
                zip.Write(z.Priority);
                zip.Write(z.Shape);
                zip.Write(z.Position.x);
                zip.Write(z.Position.y);
                zip.Write(z.Radius);
            }
            return zip;
        }
        public static void Deserialize(ZPackage package)
        {
            ZoneT.Clear();
            int tnum = package.ReadInt();
            for (int i = 0; i < tnum; i++)
            {
                ZoneT.Add(new ZoneTypes
                {
                    Name = package.ReadString(),
                    PVP = package.ReadBool(),
                    PVPEnforce = package.ReadBool(),
                    ShowPosition = package.ReadBool(),
                    PositionEnforce = package.ReadBool(),
                    MessageAnnoucement = package.ReadBool(),
                    Admins = package.ReadString(),
                    Configurations = package.ReadString()
                });
            }
            Zones.Clear();
            int num = package.ReadInt();
            for (int i = 0; i < num; i++)
            {
                Zones.Add(new Zone
                {
                    ID = package.ReadInt(),
                    Name = package.ReadString(),
                    Type = package.ReadString(),
                    Priority = package.ReadInt(),
                    Shape = package.ReadString(),
                    Position = new Vector2(package.ReadSingle(), package.ReadSingle()),
                    Radius = package.ReadSingle(),
                });
            }
        }
        // RPC function class. This is the class that you register to receive rpc data.
        public static void RPC(ZRpc rpc, ZPackage data)
        {
            Debug.Log("S2C Zone (RPC Call)");
            Debug.Assert(!ZNet.instance.IsServer());
#if DEBUG
            Debug.Log("Before");
            _debug();
#endif
            Deserialize(data);
#if DEBUG
            Debug.Log("After");
            _debug();
#endif

        }
        public static void RPC2(long rpc, ZPackage data)
        {
            Debug.Log("S2C Zone (RPC Call)");
            Debug.Assert(!ZNet.instance.IsServer());
#if DEBUG
            Debug.Log("Before");
            _debug();
#endif
            Deserialize(data);
#if DEBUG
            Debug.Log("After");
            _debug();
#endif
        }
         // WorldofValheimZones.ServerSafeZonePath.Value
        public static void LoadZoneData(string ZonePath)
        {
            Debug.Log($"Loading zone file: {ZonePath}");
            // Clean up the old zone data
            ZoneT.Clear();
            Zones.Clear();
            int pos = 0;
            foreach (string text2 in File.ReadAllLines(ZonePath))
            {
                if (!string.IsNullOrWhiteSpace(text2) && text2[0] != '#')
                {
                    string[] array2 = text2.Split(' ');
                    // Check if it is a type
                    if (array2[0].ToLower() == "type:")
                    {
                        Debug.Log($"Loading Type: {array2[1]}");
                        ZoneTypes zt = new ZoneTypes { Name = array2[1] };

                        // Go through each argument to override defaults.

                        if (array2.Length >= 3)
                            zt.PVP = bool.Parse(array2[2]);

                        if (array2.Length >= 4)
                            zt.PVPEnforce = bool.Parse(array2[3]);

                        if (array2.Length >= 5)
                            zt.ShowPosition = bool.Parse(array2[4]);

                        if (array2.Length >= 6)
                            zt.PositionEnforce = bool.Parse(array2[5]);

                        if (array2.Length >= 7)
                            zt.MessageAnnoucement = bool.Parse(array2[6]);

                        ZoneT.Add(zt);
                    }
                    else if (array2[0].ToLower() == "configuration:")
                    {
                        string texttosend = text2.Replace(": ", "|");
                        string[] array = texttosend.Replace(" | ", "|").Split('|');
                        ZoneHandler.ZoneTypes zt = ZoneHandler.FindZoneType(array[1]);
                        if (zt.Name.ToLower() != array[1].ToString().ToLower())
                        {
                            Debug.Log($"ERROR: While applying custom configuration for the Zone Type: {array[1]},");
                            Debug.Log($"Zone Type: {array[1]} Does not exist in the {WorldofValheimZones.ZonePath.Value} file!");
                            return;
                        }
                        else
                        {
                            Debug.Log($"Loading Custom Configuration for the Zone Type: {array[1]} with an Overridable Permission Node of: 'HackShardGaming.WoV-Zones.Override.{array[1]}'");
                            zt.Admins = array[2];
                            zt.Configurations = array[3];
                        }
                    }
                    else
                    {
                        if (array2.Length != 7)
                            Debug.Log($"Zone {text2} is not correctly formated!");
                        else
                        {
                            Debug.Log($"Loading Zone: {array2[0]}");
                            Zone z = new Zone();
                            z.Name = array2[0];
                            z.Type = array2[1];
                            z.Priority = int.Parse(array2[2]);
                            z.Shape = array2[3];
                            Vector2 posi = new Vector3();
                            CultureInfo info = new CultureInfo("en-US");
                            z.Position = new Vector2(Convert.ToSingle(array2[4], info), Convert.ToSingle(array2[5], info));
                            z.Radius = Convert.ToSingle(array2[6], info);
                            //z.pvp = bool.Parse(array2[7]);
                            z.ID = pos;
                            Zones.Add(z);
                            pos++;
                        }
                    }
                }
            }
#if DEBUG
            _debug();
#endif
        }
    }
}
