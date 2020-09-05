using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ModAPI.Interfaces
{
    public interface IModManager
    {
        /// <summary>
        /// Possible implementation in ModAPI: see online doc
        /// </summary>
        bool IsModActiveForMultiplayer { get; set; }

        /// <summary>
        /// Possible implementation in ModAPI:  see online doc
        /// => ReplTools.AmIMaster();
        /// </summary>
        bool IsModActiveForSingleplayer { get; set; }

    }
}
