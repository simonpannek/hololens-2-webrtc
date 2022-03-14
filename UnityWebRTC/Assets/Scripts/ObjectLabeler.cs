using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json.Linq;
using Microsoft.MixedReality.Toolkit.UI;

/// <summary>
/// Based on https://github.com/LocalJoost/ToyAircraftFinder/blob/master/Assets/App/Scripts/ObjectLabeler.cs
/// </summary>
public class ObjectLabeler : MonoBehaviour
{
    private List<GameObject> _createdObjects = new List<GameObject>();

    [SerializeField]
    private GameObject _labelObject;

    [SerializeField]
    private GameObject _labelContainer;

    [SerializeField]
    private GameObject _debugObject;

    public virtual void LabelObjects(JArray predictions,
        Transform cameraTransform, uint VideoWidth, uint VideoHeight)
    {
        ClearLabels();
        var heightFactor = VideoHeight / VideoWidth;
        /*var topCorner = cameraTransform.position + cameraTransform.forward -
                        cameraTransform.right / 2f +
                        cameraTransform.up * heightFactor / 2f;*/
        foreach (JObject prediction in predictions)
        {
            string text = prediction.GetValue("name").ToString();

            Debug.Log($"test: {text}");
            /*var center = prediction.GetCenter();
            var recognizedPos = topCorner + cameraTransform.right * center.x -
                                cameraTransform.up * center.y * heightFactor;

#if UNITY_EDITOR
             _createdObjects.Add(CreateLabel(_labelText, recognizedPos));
#endif
            var labelPos = DoRaycastOnSpatialMap(cameraTransform, recognizedPos);
            if (labelPos != null)
            {
                _createdObjects.Add(CreateLabel(_labelText, labelPos.Value));
            }*/
        }

        /*if (_debugObject != null)
        {
            _debugObject.SetActive(false);
        }

        Destroy(cameraTransform.gameObject);*/
    }

    private Vector3? DoRaycastOnSpatialMap(Transform cameraTransform, Vector3 recognitionCenterPos)
    {
        /*RaycastHit hitInfo;

        if (SpatialMappingManager.Instance != null &&
            Physics.Raycast(cameraTransform.position, (recognitionCenterPos - cameraTransform.position),
                out hitInfo, 10, SpatialMappingManager.Instance.LayerMask))
        {
            return hitInfo.point;
        }*/
        return null;
    }

    private void ClearLabels()
    {
        foreach (var label in _createdObjects)
        {
            Destroy(label);
        }
        _createdObjects.Clear();
    }

    private GameObject CreateLabel(string text, Vector3 location)
    {
        var labelObject = Instantiate(_labelObject);
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
