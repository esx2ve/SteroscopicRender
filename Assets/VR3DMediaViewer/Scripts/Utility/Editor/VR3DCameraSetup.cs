/*
 * VR3DCameraSetup
 * 
 * This script was made by Jason Peterson (DarkAkuma) of http://darkakumadev.z-net.us/
*/

using UnityEngine;
using UnityEditor;
using System.Collections;

namespace VR3D
{
    /// <summary>
    /// This adds context menus to Camera components, whoes purpose is to turn a stock VR camera into 2 in which each are deticated to each eye, and thus can have different settings.
    /// </summary>
    public class VR3DCameraSetup : MonoBehaviour
    {
        private static string[] ignoreList = { "UnityEngine.Camera", "UnityEngine.GUILayer", "UnityEngine.FlareLayer" };

        /// <summary>
        /// Takes a single camera and splits it into 2 cameras, each deticated to a single eye. Both cameras are automatically set up to work with VR3DMediaViewer. Any components that are non-standard for a camera are placed on the left eyes camera.
        /// </summary>
        /// <param name="command"></param>
        [MenuItem("CONTEXT/Camera/VR3DMediaViewer Camera Setup")]
        static void SetupCamera(MenuCommand command)
        {
            SetupCameras(command, false);

            Debug.LogWarning("VR3DMediaViewer: Any non-standard camera components like scripts, that were on the original camera are now on the \"-Left\" camera. You may need to check over each component to make sure it's where it makes the most sence.");
        }

        /// <summary>
        /// Takes a single camera and splits it into 2 cameras, each deticated to a single eye. Both cameras are automatically set up to work with VR3DMediaViewer. Any components that are non-standard for a camera are placed on both eyes cameras.
        /// </summary>
        /// <param name="command"></param>
        [MenuItem("CONTEXT/Camera/VR3DMediaViewer Camera Setup - Perserve Components for Both Eyes")]
        static void SetupCamera2(MenuCommand command)
        {
            SetupCameras(command, true);

            Debug.LogWarning("VR3DMediaViewer: Any non-standard camera components like scripts, that were on the original camera are now on the \"-Left\" & \"-Right\" cameras. You may need to check over each component to make sure it's where it makes the most sence.");
        }

        /// <summary>
        /// The guts of the above functions.
        /// </summary>
        /// <param name="command"></param>
        /// <param name="copyScripts">Copy scripts to both cameras or just one?</param>
        static private void SetupCameras(MenuCommand command, bool copyScripts)
        {
            Camera leftCamera = (Camera)command.context;
            string cameraSourceName = leftCamera.name; 
            GameObject leftCameraObject = leftCamera.gameObject;
            
            Undo.RecordObject(leftCamera.gameObject, leftCamera.name + " Changed");

            // We make a copy of the camera, and this new copy will be for the right eye.
            GameObject rightCameraObject = GameObject.Instantiate(leftCamera.gameObject);
            rightCameraObject.transform.parent = rightCameraObject.transform.parent;

            Camera rightCamera = rightCameraObject.GetComponent<Camera>();

            Undo.RegisterCreatedObjectUndo(rightCameraObject, "Create " + rightCameraObject);

            // Name these cameras for their purposes.
            leftCamera.name = cameraSourceName + "-Left";
            rightCamera.name = cameraSourceName + "-Right";

            // Set the camera to only render for their designated eyes.
            leftCamera.stereoTargetEye = StereoTargetEyeMask.Left;
            rightCamera.stereoTargetEye = StereoTargetEyeMask.Right;

            // Set these cameras to exclude seeing the other eyes images.
            leftCamera.cullingMask &= ~(1 << LayerManager.RightLayerIndex); // Everything except the right layer. 
            rightCamera.cullingMask &= ~(1 << LayerManager.LeftLayerIndex); // Everything except the left layer.

            if (!copyScripts) ClearBehaviors(rightCameraObject);

            // Dont need more then one audio listener.
            if (rightCameraObject.GetComponent<AudioListener>())
                DestroyImmediate(rightCameraObject.GetComponent<AudioListener>());

            // If the source game object had any children, we remove their copys from the right camera.
            ClearChildren(rightCameraObject);
        }

        /// <summary>
        /// Checks if a given behaviour is in a ignore list.
        /// </summary>
        /// <param name="behaviour">A behaviour you want to see if is suposed to be ignored.</param>
        /// <returns>True if the behaviour is ignored.</returns>
        private static bool BehaviourIgnore(Behaviour behaviour)
        {
            foreach (string ignoredBehavior in ignoreList)
                if (behaviour.GetType().ToString() == ignoredBehavior) return true;

            return false;
        }

        /// <summary>
        /// Removes all non-ignored behaviors from the given GameObject.
        /// </summary>
        /// <param name="targetGameObject">The GameObject to scan.</param>
        private static void ClearBehaviors(GameObject targetGameObject)
        {
            // We don't need it to have any scripts.            
            Behaviour[] behaviours = targetGameObject.GetComponents<Behaviour>();

            foreach (Behaviour behaviour in behaviours)
                if (!BehaviourIgnore(behaviour)) DestroyImmediate(behaviour);
        }

        /// <summary>
        /// Removes all children from the given GameObject.
        /// </summary>
        /// <param name="targetGameObject">The GameObject to scan.</param>
        private static void ClearChildren(GameObject targetGameObject)
        {
            foreach (Transform child in targetGameObject.transform)
                DestroyImmediate(child.gameObject);
        }
    }
}