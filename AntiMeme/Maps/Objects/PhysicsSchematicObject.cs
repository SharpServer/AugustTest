using System;
using System.Linq;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Features.Wrappers;
using UnityEngine;

namespace AntiMeme.Maps.Objects;

/// <summary>物理を有効にしたスキマティックを出す ObjectPrefab です。</summary>
public sealed class PhysicsSchematicObject : ObjectPrefab
{
    public string TargetSchematicName { get; set; } = string.Empty;
    public float Mass { get; set; } = 1f;
    public bool UseGravity { get; set; } = true;
    public bool IsKinematic { get; set; }
    public Vector3 InitialVelocity { get; set; } = Vector3.zero;
    protected override string SchematicName => TargetSchematicName;
    protected override void OnSetup()
    {
        foreach (Rigidbody body in Schematic?.GetComponentsInChildren<Rigidbody>(true) ?? Array.Empty<Rigidbody>())
        {
            body.mass = Mathf.Max(.001f, Mass);
            body.useGravity = UseGravity;
            body.isKinematic = IsKinematic;
            if (InitialVelocity.sqrMagnitude > 0f)
                body.AddForce(InitialVelocity, ForceMode.VelocityChange);
        }
    }
}
