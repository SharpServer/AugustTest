using System;
using System.Collections.Generic;
using System.Linq;
using AntiMeme.Maps.Objects;
using LabApi.Features.Wrappers;
using MEC;
using UnityEngine;

namespace AntiMeme.Maps.Features.Warhead;

public static class WarheadDoorLockdown
{
    public static bool IsLocked { get; private set; }
    public static void LockAllDoorsClosed() => IsLocked = true;
    public static void UnlockAll() => IsLocked = false;
}
