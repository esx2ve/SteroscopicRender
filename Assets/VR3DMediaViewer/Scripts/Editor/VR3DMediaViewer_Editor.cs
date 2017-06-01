/*
 * VR3DMediaViewer_Editor
 * 
 * This script was made by Jason Peterson (DarkAkuma) of http://darkakumadev.z-net.us/
*/

#if !UNITY_4 && !UNITY_5_0 && !UNITY_5_1 && !UNITY_5_2 && !UNITY_5_2 && !UNITY_5_4 && !UNITY_5_5
#define UNITY_5_6_PLUS
#endif

using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Reflection;
using VR3D;
#if UNITY_5_6_PLUS
using UnityEngine.Video;
#endif

[CustomEditor(typeof(VR3DMediaViewer))]
public class VR3DMediaViewer_Editor : Editor 
{
    private VR3DMediaViewer theScript;
    private SerializedProperty currentS3DImageProperty;
    
    void OnEnable()
    {
        theScript = (VR3DMediaViewer)target;

        currentS3DImageProperty = serializedObject.FindProperty("currentImage");
    }

	public override void OnInspectorGUI()
	{
        serializedObject.Update();

        CheckCameras();
        
        DrawDefaultInspector();                

        // We want some controls disabled at runtime. Mostly because they are not meant to work then, so changes would do nothing.
        if (Application.isPlaying)
        {
            EditorGUI.BeginChangeCheck();

            #region Image Properties

            EditorGUILayout.Separator();

            GUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField("Current Image Settings", EditorStyles.boldLabel);
            
            GUI.enabled = false;
            EditorGUILayout.PropertyField(currentS3DImageProperty);
            GUI.enabled = true;

            ImageFormat imageFormat_ = (ImageFormat)EditorGUILayout.EnumPopup(new GUIContent("Image Format", GetTooltipForProperty("imageFormat")), theScript.ImageFormat);

            bool swapLeftRight_ = EditorGUILayout.Toggle(new GUIContent("Swap Left/Right", GetTooltipForProperty("swapLeftRight")), theScript.SwapLeftRight);
            int convergence_ = EditorGUILayout.IntField(new GUIContent("Convergence", GetTooltipForProperty("convergence")), theScript.Convergence);
            int maxConvergence_ = EditorGUILayout.IntField(new GUIContent("Max Convergence", GetTooltipForProperty("maxConvergence")), theScript.MaxConvergence);

            EditorGUILayout.Separator();

            // We show some helpful reolution info for adjusting convergence.
            EditorGUILayout.LabelField("Current Image Info", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(ImageInfo(), GUILayout.Height(50));

            GUILayout.EndVertical();

            #endregion

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(target, "VR3DMediaViewer Changed");

                if (imageFormat_ != theScript.ImageFormat) theScript.SetImageFormat(imageFormat_);
                theScript.SwapLeftRight = swapLeftRight_;
                theScript.MaxConvergence = maxConvergence_; // Do this before convergence incase convergence is higher and needs to raise the max.
                theScript.Convergence = convergence_;
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    /// <summary>
    /// Sets the ImageSize string for being displayed in editor.
    /// </summary>
    private string ImageInfo()
    {
        int width = 0;
        int height = 0;

        Material mat = theScript.GetComponent<MeshRenderer>().sharedMaterial;

        if (((Application.isPlaying) && mat) && mat.mainTexture)
        {
            width = mat.mainTexture.width;
            height = mat.mainTexture.height;
        }
        else if (theScript.defaultImage)
        {
            if (theScript.defaultImage.sourceTexture != null)
            {
                width = theScript.defaultImage.sourceTexture.width;
                height = theScript.defaultImage.sourceTexture.height;
            }
#if UNITY_5_6_PLUS
            else if (theScript.defaultImage.videoClip != null)
            {
                width = (int)theScript.defaultImage.videoClip.width;
                height = (int)theScript.defaultImage.videoClip.height;
            }
            else if (theScript.defaultImage.videoClip != null)
            {
                VideoPlayer videoPlayer = theScript.gameObject.GetComponent<VideoPlayer>();
                width = (int)videoPlayer.texture.width;
                height = (int)videoPlayer.texture.height;
            }
#endif
            else
                return "No image loaded...";
        }
            else
                return "No image loaded...";

        int virtualTextureWidth = width; // Convergence adjustments only need to be calculated on the horizontal axis as this is based on eyes being side-by-side, not placement of the images on the texture.         
        int virtualTextureHeight = height;        

        int splitTextureWidth = width;
        int splitTextureHeight = height;

        if (theScript.ImageFormat == ImageFormat.Side_By_Side || theScript.ImageFormat == ImageFormat.Mono_Side_By_Side)
        {
            splitTextureWidth /= 2;
            virtualTextureWidth /= 2;
        }
        else if (theScript.ImageFormat == ImageFormat.Top_Bottom || theScript.ImageFormat == ImageFormat.Mono_Top_Bottom)
        {   
            splitTextureHeight /= 2;
            virtualTextureHeight /= 2;
        }
        else if (theScript.ImageFormat == ImageFormat.Anaglyph || theScript.ImageFormat == ImageFormat.Mono_Anaglyph)
        {
            width /= 2;
            splitTextureWidth /= 2;
            virtualTextureWidth /= 2;
        }

        virtualTextureWidth -= (Mathf.Abs(theScript.MaxConvergence) * 2);

        // For conveinence we want to display the image sizes in editor.       
        return "Source Image:\t" + width + "x" + height +                                   // Source Image     = The acctual size of the source texture.
               "\nSplit Image:\t" + splitTextureWidth + "x" + splitTextureHeight +          // Split Image      = The size of one of the halves of the 3D Image.
               "\nDisplayed Image:\t" + virtualTextureWidth + "x" + virtualTextureHeight;   // Displayed Image  = The size of the image the user sees after any convergence.
    }

    /// <summary>
    /// Custom function to work around a bug in at least Unity 5.0-5.3.
    /// </summary>
    /// <param name="propertyName"></param>
    /// <returns>The string value for the tooltip belonging to the given property.</returns>    
    private string GetTooltipForProperty(string propertyName)
    {
        FieldInfo field = typeof(VR3DMediaViewer).GetField(propertyName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        
        if (field == null)
            return null;

        TooltipAttribute[] attributes = field.GetCustomAttributes(typeof(TooltipAttribute), true) as TooltipAttribute[];

        if (attributes.Length >= 1)
            return attributes[0].tooltip;

        return null;
    }

    /// <summary>
    /// 
    /// </summary>
    void CheckCameras()
    {
        Camera[] cameras = FindObjectsOfType<Camera>();

        bool leftCameraFound = false;
        bool rightCameraFound = false;

        foreach (Camera camera in cameras)
        {
            if (camera.stereoTargetEye == StereoTargetEyeMask.Left && camera.cullingMask == ~(1 << LayerManager.RightLayerIndex))
            {
                leftCameraFound = true;
            }
            if (camera.stereoTargetEye == StereoTargetEyeMask.Right && camera.cullingMask == ~(1 << LayerManager.LeftLayerIndex))
            {
                rightCameraFound = true;
            }
        }

        if (!leftCameraFound || !rightCameraFound)
            EditorGUILayout.HelpBox(
                "WARNING: No properly configured camera setup found in the current scene.\n\nRight click on your main camera and select the \"VR3DMediaViewer Camera Setup\" as needed.\n\nIf you instantiate a properly configured camera setup at runtime, please ignore this.", MessageType.Warning);
    }
}