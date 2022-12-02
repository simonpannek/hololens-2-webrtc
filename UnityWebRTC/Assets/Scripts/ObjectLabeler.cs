using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json.Linq;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.SpatialAwareness;
using Microsoft.MixedReality.Toolkit;

public class ObjectLabeler
{
    private List<GameObject> _createdObjects = new List<GameObject>();

    private GameObject _labelObject;
    private GameObject _labelContainer;
    private GameObject _debugObject;

    private static int _meshPhysicsLayer = 0;

    public ObjectLabeler(GameObject _labelObject, GameObject _labelContainer, GameObject _debugObject)
    {
        this._labelObject = _labelObject;
        this._labelContainer = _labelContainer;
        this._debugObject = _debugObject;
    }

    public virtual void LabelObjects(JArray predictions,
        Transform cameraTransform, uint VideoWidth, uint VideoHeight)
    {
        ClearLabels();
        // Head position of the user
        var headCenter = cameraTransform.forward - cameraTransform.up * 0.2f; 

        foreach (JObject prediction in predictions)
        {
            // Extract relevant fields from prediction
            string name = prediction.GetValue("name").Value<string>();
            float xmin = prediction.GetValue("xmin").Value<float>();
            float ymin = prediction.GetValue("ymin").Value<float>();
            float xmax = prediction.GetValue("xmax").Value<float>();
            float ymax = prediction.GetValue("ymax").Value<float>();
            float confidence = prediction.GetValue("confidence").Value<float>();
            
            // Calculate offset position for label
            var x = (xmin + xmax) / 2;
            var y = ymin + (ymax - ymin) / 4;

            // Calculate position of recognized object on a plane in front of the user
            var recognizedPos = headCenter + cameraTransform.right * (x - 0.5f) - cameraTransform.up * (y - 0.5f);

            if (Application.isEditor)
            {
                // For debugging in the Unity Editor
                _createdObjects.Add(CreateLabel(name, recognizedPos));
            } else
            {
                // Raycast on spatial map to get the position of the label
                var labelPos = DoRaycastOnSpatialMap(cameraTransform, recognizedPos);

                if (labelPos != null)
                {
                    // If raycast hit the spatial map, create label at the hit position
                    _createdObjects.Add(CreateLabel(name, labelPos.Value));
                } else
                {
                    // If raycast did not hit the spatial map, create label at the position on the plane
                    _createdObjects.Add(CreateLabel(name, cameraTransform.position + recognizedPos));
                }
            }
        }
    }

    private Vector3? DoRaycastOnSpatialMap(Transform cameraTransform, Vector3 recognitionCenterPos)
    {
        // Define ray from camera to the recognition center
        RaycastHit hit;
        var ray = new Ray(cameraTransform.position, recognitionCenterPos);

        // Raycast on spatial map
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, GetSpatialMeshMask()))
        {
            return hit.point;
        }

        return null;
    }

    private static int GetSpatialMeshMask()
    {
        // Get the physics layer of the spatial mesh
        if (_meshPhysicsLayer == 0)
        {
            var spatialMappingConfig =
              CoreServices.SpatialAwarenessSystem.ConfigurationProfile as
                MixedRealitySpatialAwarenessSystemProfile;
            if (spatialMappingConfig != null)
            {
                foreach (var config in spatialMappingConfig.ObserverConfigurations)
                {
                    var observerProfile = config.ObserverProfile
                        as MixedRealitySpatialAwarenessMeshObserverProfile;
                    if (observerProfile != null)
                    {
                       _meshPhysicsLayer |= (1 << observerProfile.MeshPhysicsLayer);
                    }
                }
            }
        }

        return _meshPhysicsLayer;
    }

    private void ClearLabels()
    {
        foreach (var label in _createdObjects)
        {
            UnityEngine.Object.Destroy(label);
        }
        _createdObjects.Clear();
    }

    private GameObject CreateLabel(string text, Vector3 location)
    {
        var labelObject = UnityEngine.Object.Instantiate(_labelObject);

        var toolTip = labelObject.GetComponent<ToolTip>();
        toolTip.ShowHighlight = false;
        toolTip.ShowBackground = true;
        toolTip.ToolTipText = text;
        toolTip.transform.position = location + Vector3.up * 0.2f;
        toolTip.transform.parent = _labelContainer.transform;
        toolTip.AttachPointPosition = location;
        toolTip.ContentParentTransform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

        var connector = toolTip.GetComponent<ToolTipConnector>();
        connector.PivotDirectionOrient = ConnectorOrientType.OrientToCamera;
        connector.Target = labelObject;

        return labelObject;
    }
}
