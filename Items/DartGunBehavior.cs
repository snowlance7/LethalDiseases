using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

// give darts rigidbody, if it hits against a wall richochet and land on the ground and destroy self in a minute, if hits player, stick into player for a few seconds and then fall out. magazines are main ammo you have to buy seperately.

namespace LethalDiseases.Items
{
    internal class DartGunBehavior : PhysicsProp
    {
        public AudioSource audioSource = null!;
        public AudioClip fireSFX = null!;
        public AudioClip reloadSFX = null!;
    }

    internal class DartGunAmmo : GunAmmo
    {
        public MeshRenderer fluidRenderer = null!;
    }
}
