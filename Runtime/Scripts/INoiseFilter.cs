using UnityEngine;

namespace ProceduralPlanet
{
    public interface INoiseFilter
    {
        float Evaluate(Vector3 point);
    }
}
