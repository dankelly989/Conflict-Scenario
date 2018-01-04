using System;
using System.Collections.Generic;
using UnityEngine;

namespace MH.Constants
{
    public partial class Keys
    {
        public const string Cancel = "Cancel";
    }

    public partial class Level
    {
        public const string StartLevel = "Lv1";
    }

    public partial class Save
    {
        public const int PROFILE_FMT = 1;
        public const string SLOT = "slot";
    }

    public partial class LvName
    {
        public const string Loading = "Loading";
    }

    public partial class Tags
    {
        public const string GlobalScript = "GlobalScript";
        public const string Player = "Player";        
        public const string SceneObjs = "SceneObjs";
        public const string PrefabPools = "PrefabPools";
        public const string Cam2D = "Cam2D";
        public const string Cam3D = "Cam3D";

        public const string Grids = "Grids";
        public const string OverlayUI3D = "OverlayUI3D";

        public const string NotifiIconCont = "NotifiIconCont";
    }

    public partial class Layers
    {
        public const string Default = "Default";
        public const string Player = "Player";
        public const string Friend = "Friend";
        public const string TransparentFX = "TransparentFX";
        public const string Water = "Water";
        public const string IgnoreRaycast = "Ignore Raycast";
        public const string Tile = "Tile";
        public const string SelectableMarker = "SelectableMarker";
        public const string OverlayUI3D = "OverlayUI3D";

        private static int m_layer_Player = -1;
        public static int layer_Player{ get {
            if (m_layer_Player == -1)
                m_layer_Player = LayerMask.NameToLayer(Player);
            return m_layer_Player;
        } }

        private static int m_layer_Tile = -1;
        public static int layer_Tile
        {
            get {
                if (m_layer_Tile == -1)
                    m_layer_Tile = LayerMask.NameToLayer(Tile);
                return m_layer_Tile;
            }
        }

        private static int m_layer_SelectableMarker = -1;
        public static int layer_SelectableMarker
        {
            get {
                if (m_layer_SelectableMarker == -1)
                    m_layer_SelectableMarker = LayerMask.NameToLayer(SelectableMarker);
                return m_layer_SelectableMarker;
            }
        }

        private static int m_layer_OverlayUI3D = -1;
        public static int layer_OverlayUI3D
        {
            get {
                if (m_layer_OverlayUI3D == -1)
                    m_layer_OverlayUI3D = LayerMask.NameToLayer(OverlayUI3D);
                return m_layer_OverlayUI3D;
            }
        }
    }

}
