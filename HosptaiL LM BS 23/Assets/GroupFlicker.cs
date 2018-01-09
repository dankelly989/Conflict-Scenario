using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GroupFlicker : Flicker {
    public List<MeshRenderer> lights;

    public new void turnOff()
    {
        foreach (MeshRenderer mesh in lights)
        {
            mesh.materials[0].SetColor("_Color", Color.black);
            mesh.materials[0].SetColor("_EmissionColor", Color.black);
        }

    }
}
