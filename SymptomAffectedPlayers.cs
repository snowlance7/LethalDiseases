using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Text;
using SnowyLib;

namespace LethalDiseases
{
    public static class SymptomAffectedPlayers
    {
        public static AutoDictionary<string, HashSet<PlayerControllerB>> symptomAffectedPlayers { get; private set; } = new AutoDictionary<string, HashSet<PlayerControllerB>>();
    }
}
