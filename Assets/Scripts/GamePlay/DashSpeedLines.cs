using UnityEngine;

public class DashSpeedLines : MonoBehaviour
{
    [SerializeField] private ParticleSystem speedLines;

    public void OnDashStart()
    {
        Debug.Log("DashEffect");
        if (!speedLines) return;
        speedLines.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        speedLines.Play(true);
    }
}
