using GameNetcodeStuff;
using SnowyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace LethalDiseases.Items
{
    internal class HealingMushroom : PhysicsProp
    {
        public int bodyPartIndex = 0;
        public Vector3 positionOffset;
        public Vector3 rotationOffset;

        new Transform? parentObject => playerAttachedTo?.bodyParts[bodyPartIndex];

        public PlayerControllerB? playerAttachedTo;

        public override void Start()
        {
            base.Start();
            playerAttachedTo = StartOfRound.Instance.allPlayerScripts.Where(x => x.actualClientId == 1).FirstOrDefault();

        }

        public override void LateUpdate()
        {
            if (parentObject != null)
            {
                base.transform.rotation = parentObject.rotation;
                base.transform.Rotate(rotationOffset);
                base.transform.position = parentObject.position;
                Vector3 _positionOffset = positionOffset;
                _positionOffset = parentObject.rotation * _positionOffset;
                base.transform.position += _positionOffset;
            }
            if (rotateObject)
            {
                base.transform.Rotate(new Vector3(0f, Time.deltaTime * 60f, 0f), Space.World);
            }
            if (radarIcon != null)
            {
                radarIcon.position = base.transform.position;
            }
        }
    }

    public class MushroomAttachmentPoint(int bodyPartIndex, Vector3 positionOffset, Vector3 rotationOffset)
    {
        public int bodyPartIndex = bodyPartIndex;

        public Vector3 positionOffset = positionOffset;
        public Vector3 rotationOffset = rotationOffset;

        public bool occupied;
    }

    public static class MushroomManager
    {
        public static List<MushroomAttachmentPoint> MushroomAttachmentPoints = new List<MushroomAttachmentPoint>();

        [StaticInit]
        public static void Init()
        {
            MushroomAttachmentPoints.Add(new MushroomAttachmentPoint(0, new Vector3(0f, 0f, 0f), new Vector3(0, 0, 0)));
        }
    }
}