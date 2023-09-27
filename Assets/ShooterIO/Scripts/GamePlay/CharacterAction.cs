﻿using Photon.Pun;
using UnityEngine;

[DisallowMultipleComponent]
public class CharacterAction : MonoBehaviourPun, IPunObservable
{
    public bool IsBlocking { get; set; } = false;
    public int AttackingActionId { get; set; } = -1;
    public Vector3 AimPosition { get; set; } = Vector3.zero;

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsReading)
        {
            IsBlocking = (bool)stream.ReceiveNext();
            if (!IsBlocking)
                AttackingActionId = (int)stream.ReceiveNext();
            AimPosition = (Vector3)stream.ReceiveNext();
        }
        else
        {
            stream.SendNext(IsBlocking);
            if (!IsBlocking)
                stream.SendNext(AttackingActionId);
            stream.SendNext(AimPosition);
            // Reset states
            IsBlocking = false;
            AttackingActionId = -1;
        }
    }
}
