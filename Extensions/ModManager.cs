using ModAPI.Interfaces;
using Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ModFarmer
{
    public class MyModManager : MonoBehaviour, IModManager
    {
        private bool _isActiveForMultiplayer;
        public bool IsModActiveForMultiplayer
        {
            get => _isActiveForMultiplayer;
            set => _isActiveForMultiplayer = FindObjectOfType(typeof(ModManager.ModManager)) != null && ModManager.ModManager.AllowModsForMultiplayer;
        }

        private bool _isActiveForSingleplayer;
        public bool IsModActiveForSingleplayer
        {
            get => _isActiveForSingleplayer;
            set => _isActiveForSingleplayer = ReplTools.AmIMaster();
        }
    }
}
