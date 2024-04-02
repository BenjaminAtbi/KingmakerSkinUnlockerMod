using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityModManagerNet;

namespace SkinUnlocker
{
    public class Settings : UnityModManager.ModSettings
    {
        public bool UnlockSkin = false;
        public bool UnlockHair = false;
        public bool UnlockAllHair = false;
        public bool UnlockHorns = false;
        public bool UnlockTail = false;
        public bool UnlockFemaleDwarfBeards = false;
        public override void Save(UnityModManager.ModEntry modEntry)
        {
            Save(this, modEntry);
        }
    }
}
