using UnityEngine;

namespace ModFarmer
{
    class PlayerExtended : Player
    {
        protected override void Start()
        {
            base.Start();
            new GameObject($"__{nameof(ModFarmer)}__").AddComponent<ModFarmer>();
        }
    }
}
