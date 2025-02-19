using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class PlanePrefabInstantiator : MonoBehaviour
{
    public GameObject planePrefab;
    private ARPlaneManager arPlaneManager;

    void Awake()
    {
        arPlaneManager = GetComponent<ARPlaneManager>();
    }

    void OnEnable()
    {
        arPlaneManager.planesChanged += OnPlanesChanged;
    }

    void OnDisable()
    {
        arPlaneManager.planesChanged -= OnPlanesChanged;
    }

    void OnPlanesChanged(ARPlanesChangedEventArgs eventArgs)
    {
        foreach (ARPlane plane in eventArgs.added)
        {
            Instantiate(planePrefab, plane.transform.position, plane.transform.rotation);
        }
    }
}