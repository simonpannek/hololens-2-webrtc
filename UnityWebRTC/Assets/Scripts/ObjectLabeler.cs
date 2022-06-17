using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json.Linq;
using Microsoft.MixedReality.Toolkit.UI;

/// <summary>
/// Based on https://github.com/LocalJoost/ToyAircraftFinder/blob/master/Assets/App/Scripts/ObjectLabeler.cs
/// </summary>
public class ObjectLabeler
{
    private List<GameObject> _createdObjects = new List<GameObject>();

    private GameObject _labelObject;
    private GameObject _labelContainer;
    private GameObject _debugObject;

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
        var heightFactor = VideoHeight / VideoWidth;
        var topCorner = cameraTransform.position + cameraTransform.forward -
                        cameraTransform.right / 2f +
                        cameraTransform.up * heightFactor / 2f; 

        foreach (JObject prediction in predictions)
        {
            string name = prediction.GetValue("name").Value<string>();
            float xmin = prediction.GetValue("xmin").Value<float>();
            float ymin = prediction.GetValue("ymin").Value<float>();
            float xmax = prediction.GetValue("xmax").Value<float>();
            float ymax = prediction.GetValue("ymax").Value<float>();
            float confidence = prediction.GetValue("confidence").Value<float>();

            Debug.Log($"name: {name} x: {xmin} - {xmax}, y: {ymin} - {ymax}, conf: {confidence}");
            var centerX = xmax - xmin;
            var centerY = ymax - ymin;
            var recognizedPos = topCorner + cameraTransform.right * centerX -
                                cameraTransform.up * centerY * heightFactor;
            
            //#if UNITY_EDITOR
             _createdObjects.Add(CreateLabel(name, recognizedPos));
            //#endif
            /*var labelPos = DoRaycastOnSpatialMap(cameraTransform, recognizedPos);
            if (labelPos != null)
            {
                _createdObjects.Add(CreateLabel(_labelText, labelPos.Value));
            }*/
        }

        /*if (_debugObject != null)
        {
            _debugObject.SetActive(false);
        }

        UnityEngine.Object.Destroy(cameraTransform.gameObject);*/
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
