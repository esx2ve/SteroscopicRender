/*
 * VR3DMediaViewer
 * 
 * This script was made by Jason Peterson (DarkAkuma) of http://darkakumadev.z-net.us/
*/

#if !UNITY_4 && !UNITY_5_0 && !UNITY_5_1 && !UNITY_5_2 && !UNITY_5_2 && !UNITY_5_4 && !UNITY_5_5
#define UNITY_5_6_PLUS
#endif

// Comment this out if you need/want to see the right image object. We just hide it to keep things looking clean and simple.
#define HIDE_RIGHT

// Comment this out if you need/want to see the VideoPlayer component this script adds when it doesnt find one. We just hide it to keep things looking clean and simple.
#define HIDE_VIDEOPLAYER

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_5_6_PLUS
using UnityEngine.Video;
#endif

namespace VR3D
{
    /// <summary>
    /// Image Formats available for this asset to display textures in. 3D, 2D and Monoscopic.
    /// <para>2D = Standard image. No 3D formating is applied.</para>
    /// <para>Side-by-Side = 3D format. 2 images from slightly different perspectives placed side-by-side.</para>
    /// <para>Top/Bottom = 3D format. 2 images from slightly different perspectives placed one on top of the other.</para>
    /// <para>Two Images = 3D format. 2 images from slightly different perspectives in different files.</para>
    /// <para>Horizontal Interlaced = 3D format. 2 images from slightly different perspectives interwoven with horizontal lines of pixels.</para>
    /// <para>Vertical Interlaced = 3D format. 2 images from slightly different perspectives interwoven with vertical lines of pixels.</para>
    /// <para>Checkerboard = 3D format. 2 images from slightly different perspectives interwoven with a checkerboard pattern of pixels.</para>
    /// <para>Anaglyph = 3D format. 2 images from slightly different perspectives with opposing RGB color channels removed, overlayed on top of each other.</para>
    /// <para>Mono = A version of the named 3D format, but using the image for a single eye for both eyes so no 3D effect can be seen.</para>    
    /// </summary>
    public enum ImageFormat : int { _2D, Side_By_Side, Top_Bottom, TwoImages, HorizontalInterlaced, VerticalInterlaced, Checkerboard, Anaglyph, Mono_Side_By_Side, Mono_Top_Bottom, Mono_TwoImages, Mono_HorizontalInterlaced, Mono_VerticalInterlaced, Mono_Checkerboard, Mono_Anaglyph };

    /// <summary>
    /// Show Stereoscopic 3D Images on a GameObjects mesh.
    /// </summary>
    public class VR3DMediaViewer : MonoBehaviour 
    {
        [Header("Canvas")]
        [Tooltip("This stereoscopic 3D image will be displayed immediately on start.")]
        public Stereoscopic3DImage defaultImage;

        [Tooltip("A list of texture shader properties in which we assign our stereoscopic 3D images.")]
        public string[] targetTextureMaps = { "_MainTex" };

        /// <summary>
        /// The material belonging to the the renderer of the GameObject thats visible to the left eye.
        /// </summary>
        private Material m_leftImageMaterial;

        /// <summary>
        /// The material belonging to the the renderer of the GameObject thats visible to the right eye.
        /// </summary>
        private Material m_rightImageMaterial;
         
        /// <summary>
        /// We change the materials main texture map Tiling value at runtime, so we save what it was at default as a reference for some calculations.
        /// </summary>
        private Vector2 m_defaultMainTextureScale;

        /// <summary>
        /// We change the materials main texture map Offset value at runtime, so we save what it was at default as a reference for some calculations.
        /// </summary>
        private Vector2 m_defaultMainTextureOffset;        

        /// <summary>
        /// When we load a new image into this canvas we save it so we can reference its settings. FYI, changing the settings in editor at runtime applies those settings to the S3DImage in the project heirarcy. Conveinent!
        /// </summary>
        [HideInInspector]
        [SerializeField]
        private Stereoscopic3DImage currentImage;
                
        /// <summary>
        /// The Image Format of the S3DImage currently being displayed int he canvas.
        /// </summary>
        public ImageFormat ImageFormat
        {
            get 
            {
                if (currentImage != null)
                    return currentImage.imageFormat;
                else
                    return ImageFormat._2D; // If theres no S3D image, we display the image as a normal texture.
            }
            set { currentImage.imageFormat = value; }
        }

        /// <summary>
        /// The Swap Left & Right value of the S3DImage currently being displayed int he canvas.
        /// </summary>
        public bool SwapLeftRight
        {   
            get 
            {
                if (currentImage != null)
                    return currentImage.swapLeftRight;
                else
                    return false; // If theres no S3D image, we display don't swap anything.
            }
            set
            {
                currentImage.swapLeftRight = value;

                if (Application.isPlaying)
                {
                    // If not using 2 images we just swap eyes by swapping the material offsets.
                    if (currentImage.imageFormat != ImageFormat.TwoImages &&
                        currentImage.imageFormat != ImageFormat.Mono_TwoImages)
                        SetMaterialOffset();
                    else
                    {
                        // When using two images, we have to swap the images in the materials entirely.
                        foreach (string targetTextureMap in targetTextureMaps)
                        {
                            m_leftImageMaterial.SetTexture(targetTextureMap, (currentImage.swapLeftRight ? currentImage.sourceTexture2 : currentImage.sourceTexture));
                            m_rightImageMaterial.SetTexture(targetTextureMap, (currentImage.swapLeftRight ? currentImage.sourceTexture : currentImage.sourceTexture2));
                        }
                    }
                }
            }
        }
            
        /// <summary>
        /// Convergence is a value ranging between -MaxConvergence and +MaxConvergence that allows you to try and fix the focal point of some hard to focus, generally incorrectly made 3D images, at the cost of shaving some pixels off the sides of the image.
        /// Each unit of convergence equals 1 vertical row of pixels offset for each eye. So 2 vertical rows total.
        /// </summary>
        public int Convergence
        {   
            get 
            {
                if (currentImage != null)
                    return currentImage.convergence;
                else
                    return 0; // If theres no S3D image, we dont add any convergence.
            }
            set
            {   
                if (value == currentImage.convergence)                
                    return;

                int m = MaximumAllowedConvergence;

                // We want raising convergence to also raise maxConvergence during non-runtime, but during runtime we just set convergence back down to maxConvergence if it exceeds it.
                if ((Application.isPlaying && Mathf.Abs(value) > MaxConvergence) || (m != -1 && Mathf.Abs(value) > m))
                    currentImage.convergence = (value >= 0 ? MaxConvergence : -MaxConvergence);
                else
                    currentImage.convergence = value;

                // Raise the max convergence if it isnt runtie and value is higher than the current max.
                if (!Application.isPlaying && Mathf.Abs(value) > MaxConvergence)
                    MaxConvergence = Mathf.Abs(value);

                if (Application.isPlaying)
                    SetMaterialOffset();
            }
        }        

        /// <summary>
        /// See Convergence. This sets the max -/+ range of Convergence.
        /// The maximum this can be set to is 1/4 the amount of pixels of the displayed image an eye sees.
        /// </summary>
        public int MaxConvergence
        {
            get 
            {
                if (currentImage != null)
                    return currentImage.maxConvergence;
                else
                    return 0; // If theres no S3D image, we dont add any convergence.
            }
            set
            {
                // Discard any negetive values.
                if (value == currentImage.maxConvergence || value < 0)
                    return;

                int m = MaximumAllowedConvergence;

                if (m != -1 && value > m)
                {
                    // Discard values if they would reduce the displayed image to less than half of the split image size.
                    return;
                }

                // Ensure convergence is always lower than or equal to maxConvergence.
                if (value < Mathf.Abs(currentImage.convergence))
                    currentImage.convergence = value;

                currentImage.maxConvergence = value;

                if (Application.isPlaying)
                {
                    SetMaterialTiling();
                    SetMaterialOffset();
                }
            }
        }
            
        /// <summary>
        /// Some textures are mirrored vertically (not be be confused with up-side-down). This allows you to fix that.
        /// This is just available via scripting, not in the editor. I didn't want to add it, and wanted as little clutter as possible.
        /// </summary>
        public bool VerticalFlip
        {
            get { return currentImage.verticalFlip; }
            set { currentImage.verticalFlip = value; }
        }

        /// <summary>
        /// The left eye color filter on the active Anaglyph image.
        /// </summary>
        public Color LeftEyeColor
        {   
            get { return currentImage.leftEyeColor; }
        }

        /// <summary>
        /// The right eye color filter on the active Anaglyph image.
        /// </summary>
        public Color RightEyeColor
        {
            get { return currentImage.rightEyeColor; }
        }
        
        /// <summary>
        /// A shortcut to get the width of the currently displayed image, whatever type it may be of.
        /// </summary>
        private int CurrentImageWidth
        {
            get 
            {
                if (currentImage.sourceTexture != null)
                    return currentImage.sourceTexture.width;
#if UNITY_5_6_PLUS
                else if (currentImage.videoClip != null)
                    return (int)currentImage.videoClip.width;
                else if (currentImage.videoURL != null)
                    return GetComponent<VideoPlayer>().texture.width;
#endif
                return 0;
            }
        }

        /// <summary>
        /// A shortcut to get the height of the currently displayed image, whatever type it may be of.
        /// </summary>
        private int CurrentImageHeight
        {
            get 
            {
                if (currentImage.sourceTexture != null)
                    return currentImage.sourceTexture.height;
#if UNITY_5_6_PLUS
                else if (currentImage.videoClip != null)
                    return (int)currentImage.videoClip.height;
                else if (currentImage.videoURL != null)
                    return GetComponent<VideoPlayer>().texture.height;
#endif
                return 0;
            }
        }
                
        /// <summary>
        /// A Texel is a unit of TextureScale thats equal to 1 pixel of the current displayed texture.
        /// </summary>
        private Vector2 CurrentImageTexelSize
        {
            get
            {
                if (currentImage.sourceTexture != null)
                    return currentImage.sourceTexture.texelSize;
#if UNITY_5_6_PLUS
                else if (currentImage.videoClip != null ||
                    currentImage.videoURL != null)
                    return GetComponent<VideoPlayer>().texture.texelSize;
#endif
                return Vector2.zero;
            }
        }

        /// <summary>
        /// Calculates the maximum that MaxConvergence can be set to, based on the current texture.
        /// The max is 1/4 of each eyes pixels as a limit has to be set, and really... anymore is going to distort the image to much.
        /// </summary>        
        private int MaximumAllowedConvergence
        {
            get
            {
                int splitTextureWidth = CurrentImageWidth;

                if (splitTextureWidth != 0)
                {
                    switch (ImageFormat)
                    {
                        case ImageFormat.Side_By_Side:
                        case ImageFormat.VerticalInterlaced:
                        case ImageFormat.Checkerboard:
                        case ImageFormat.Anaglyph:
                        case ImageFormat.Mono_Side_By_Side:
                        case ImageFormat.Mono_VerticalInterlaced:
                        case ImageFormat.Mono_Checkerboard:
                        case ImageFormat.Mono_Anaglyph:
                            splitTextureWidth /= 8;
                            break;
                        default:
                            //splitTextureWidth /= 4; // ?? Why do we do a different value for TB based formats?
                            splitTextureWidth /= 4;
                            break;
                    }

                    // Quartered instead of halved because convergences deal in double.                    
                    return splitTextureWidth;
                }

                // Return -1 if theres no texture.
                return -1;
            }
        }

        /// <summary>
        /// The source texture thats being filtered because its of a Special format.
        /// </summary>
        private Texture m_renderTextureSource;
        
        /// <summary>
        /// The internal texture we use for filtering Special formats into SBS/TB.
        /// </summary>
        private RenderTexture m_renderTexture;

        /// <summary>
        /// The internal material we use for filtering Special formats into SBS/TB.
        /// </summary>
        private Material m_renderMaterial;

        /// <summary>
        /// We use this shader to do our conversions in the render material.
        /// </summary>
        [HideInInspector]
        [SerializeField]
        private Shader m_renderShader;
        
        /// <summary>
        /// The index of the pass we use in the renderShader.
        /// </summary>
        private int m_shaderPass = -1;

        // Just so we have some context for the shaderPass values when we set them.
        private const int SHADER_PASS_HORIZONTAL_INTERLACED = 0;
        private const int SHADER_PASS_VERTICAL_INTERLACED = 1;
        private const int SHADER_PASS_CHECKERBOARD = 2;
        private const int SHADER_PASS_ANAGLYPH = 3;
                
        // Some events you can subscribe to.
        public delegate void NewImageLoaded(Stereoscopic3DImage newImage);
        public static event NewImageLoaded OnNewImageLoadedEvent = (Stereoscopic3DImage newImage) => { };
        public delegate void FormatChanged(ImageFormat imageFormat);
        public static event FormatChanged OnFormatChangedEvent = (ImageFormat imageFormat) => { };

	    // Use this for initialization
	    void Start () 
        {
            // We use the settings in the material as our default.
            m_defaultMainTextureScale = gameObject.GetComponent<Renderer>().sharedMaterial.mainTextureScale;
            m_defaultMainTextureOffset = gameObject.GetComponent<Renderer>().sharedMaterial.mainTextureOffset;

            // This needs to be done before we start trying to display anything.
            SetupCanvas();
            
            // After everything is setup, we can check and see if theres a default texture selected, as well as default 3D formatting settings to use.            
            if (defaultImage != null) DisplayImageInCanvas(defaultImage);
	    }
	
	    // Update is called once per frame
	    void Update () 
        {
            /*
            // Test code to cycle through all the Image Formats possible.
		    if (Input.GetKeyDown(KeyCode.F))
            {
                int index = (int)ImageFormat;
                if (index < System.Enum.GetValues(typeof(ImageFormat)).Length - 1)
                    SetImageFormat((ImageFormat)(index + 1));
                else
                    SetImageFormat(ImageFormat._2D);                
            }
            */

            if (m_renderTexture != null &&
                m_renderTextureSource != null &&
                m_renderMaterial != null &&
                m_renderShader != null &&
                m_shaderPass != -1)
            {
                switch (currentImage.imageFormat)
                {
                    case ImageFormat.HorizontalInterlaced:                    
                    case ImageFormat.VerticalInterlaced:
                    case ImageFormat.Checkerboard:
                    case ImageFormat.Anaglyph:
                    case ImageFormat.Mono_HorizontalInterlaced:
                    case ImageFormat.Mono_VerticalInterlaced:
                    case ImageFormat.Mono_Checkerboard:
                    case ImageFormat.Mono_Anaglyph:
                        Graphics.Blit(m_renderTextureSource, m_renderTexture, m_renderMaterial, m_shaderPass);
                        break;
                    default:
                        break;
                }
            }
	    }

#if UNITY_EDITOR
        void Reset()
        {
            // Our 3D format conversion shader needs to be included in the build, but we dont reference it until runtime. So we store it here.
            if (m_renderShader == null)
            {
                m_renderShader = Shader.Find("Hidden/VR3D/3DFormatConversion");

                if (m_renderShader == null)
                    Debug.LogWarning("[VR3DMediaViewer] Unable to load the \"3DFormatConversion\" shader. Did you delete it?");
            }
        }
#endif

        #region Core Methods

        /// <summary>
        /// Our Canvas is just the GameObjects we use to display the S3DImage. But to do that we need to create a second GameObject and use 1 for the Left eye and one for the Right eye.
        /// </summary>
        private void SetupCanvas()
        {   
            // Duplicate this entire gameobject and immediately remove any behaviours from the dupe.
            GameObject rightImageCanvas = Instantiate(gameObject, transform.position, transform.rotation) as GameObject;

            // We don't need it to have any scripts.
            Behaviour[] behaviours = rightImageCanvas.GetComponents<Behaviour>();

            foreach (Behaviour behaviour in behaviours)
                DestroyImmediate(behaviour);

            // Parent it to this GameObject so if its transform properties change the copy is changed a well.
            rightImageCanvas.transform.parent = transform;

            // Ensure its scale is the same as the parent/left image.
            rightImageCanvas.transform.localScale = new Vector3(1, 1, 1);

#if HIDE_RIGHT
            // It doesn't need to be seen.
            rightImageCanvas.hideFlags = HideFlags.HideInHierarchy;
#endif

            m_leftImageMaterial = gameObject.GetComponent<Renderer>().material;
            m_rightImageMaterial = rightImageCanvas.GetComponent<Renderer>().material;

            // Set the layer of each canvas.
            gameObject.layer = LayerManager.LeftLayerIndex;
            rightImageCanvas.layer = LayerManager.RightLayerIndex;
        }

        /// <summary>
        /// This processes a given S3DImage and displays it in the scene.
        /// </summary>
        /// <param name="s3DImage"></param>
        private void DisplayImageInCanvas(Stereoscopic3DImage s3DImage)
        {
            // Save the 3D settings from the currently selected 3D image to our canvas.            
            currentImage = s3DImage;
            
            //
            if (currentImage.sourceTexture != null)
            {
                    switch (currentImage.imageFormat)
                    {
                        case ImageFormat.HorizontalInterlaced:
                        case ImageFormat.Mono_HorizontalInterlaced:
                        case ImageFormat.VerticalInterlaced:
                        case ImageFormat.Mono_VerticalInterlaced:
                        case ImageFormat.Checkerboard:
                        case ImageFormat.Mono_Checkerboard:
                        case ImageFormat.Anaglyph:
                        case ImageFormat.Mono_Anaglyph:
                            SpecialFormatSetup(currentImage.sourceTexture);
                            break;                        
                        default:
                            // Select the given texture into the texture maps of both materials.
                            foreach (string targetTextureMap in targetTextureMaps)
                            {
                                if (currentImage.imageFormat == ImageFormat.TwoImages ||
                                    currentImage.imageFormat == ImageFormat.Mono_TwoImages)
                                {   
                                    m_leftImageMaterial.SetTexture(targetTextureMap, (currentImage.swapLeftRight ? (currentImage.sourceTexture2 != null ? currentImage.sourceTexture2 : currentImage.sourceTexture) : currentImage.sourceTexture));
                                    m_rightImageMaterial.SetTexture(targetTextureMap, (currentImage.swapLeftRight ? currentImage.sourceTexture : (currentImage.sourceTexture2 != null ? currentImage.sourceTexture2 : currentImage.sourceTexture)));
                                }
                                else
                                {
                                    m_leftImageMaterial.SetTexture(targetTextureMap, currentImage.sourceTexture);
                                    m_rightImageMaterial.SetTexture(targetTextureMap, currentImage.sourceTexture);
                                }
                            }

                            // We null these so the Graphics.Blit doesnt happen in the Update every frame when a RenderTexture isnt even being used.
                            m_renderTexture = null;
                            m_renderTextureSource = null;
                            m_renderMaterial = null;
                            m_shaderPass = -1;
                            break;
                    }

                Update3DFomatSettings();
            }
#if UNITY_5_6_PLUS
            else if (currentImage.videoClip != null || currentImage.videoURL != null)
            {
                // Video player setup.
                VideoPlayer videoPlayer = GetComponent<VideoPlayer>();
                
                if (videoPlayer == null)
                {
                    videoPlayer = gameObject.AddComponent<VideoPlayer>();
#if HIDE_VIDEOPLAYER
                    videoPlayer.hideFlags = HideFlags.HideInInspector;
#endif
                    videoPlayer.renderMode = VideoRenderMode.APIOnly;
                }
                else
                {
                    /*
                     * (04/16/17)
                     * We only support VideoRenderMode.RenderTexture and VideoRenderMode.APIOnly.
                     * VideoRenderMode.MaterialOverride is not compatible. (It locks the "mainTextureScale"/"mainTextureOffset" of the materials.)
                     * The 2 Camera plane modes dont seems to make sense to support.
                    */
                    // Default to APIOnly if a incompatable mode is selected.
                    if (videoPlayer.renderMode != VideoRenderMode.RenderTexture || videoPlayer.renderMode != VideoRenderMode.APIOnly)
                        videoPlayer.renderMode = VideoRenderMode.APIOnly;
                }

                videoPlayer.playOnAwake = true;
                videoPlayer.waitForFirstFrame = true;
                videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
                videoPlayer.controlledAudioTrackCount = 1;

                // Audio setup.
                AudioSource audioSource = GetComponent<AudioSource>();

                if (audioSource == null)
                    audioSource = gameObject.AddComponent<AudioSource>();
                
                audioSource.spatialBlend = 1;
                
                videoPlayer.SetTargetAudioSource(0, audioSource);
                
                // Load our video.
                if (currentImage.videoClip != null)
                {
                    videoPlayer.source = VideoSource.VideoClip;
                    videoPlayer.clip = currentImage.videoClip;
                }
                else if (currentImage.videoURL != null)
                {
                    videoPlayer.source = VideoSource.Url;
                    videoPlayer.url = currentImage.videoURL;
                }

                // We finish setting up in "OnPrepareComplete" once the video is loaded.
                videoPlayer.prepareCompleted += OnPrepareComplete;
                videoPlayer.Prepare();
            }            
#endif
            else
                Debug.LogError("[VR3DMediaViewer] No Stereoscopic 3D image/video found in " + s3DImage.name + ". Unable to display anything.");

            OnNewImageLoadedEvent(currentImage);
        }

        /// <summary>
        /// This applies the settings of the currently selected S3DImage to the canvas.
        /// </summary>
        private void Update3DFomatSettings()
        {   
            // Apply the new format settings to our canvas.
            SetMaterialTiling();
            SetMaterialOffset();

            OnFormatChangedEvent(ImageFormat);
        }
                
        /// <summary>
        /// This changes the main texture Tiling in the shader properties of the left/right materials, so each shows a different half of the texture.
        /// </summary>
        private void SetMaterialTiling()
        {   
            // The full source image, each eyes half of the source image, and the actual image each eye sees are different dimensions.
            // We need the dimensions of each image after the full source image has been split in 2 then cropped to allow a range of convergence adjustment.
            // We quad the maxConvergence values as both eyes lose double.
            int modifiedTextureWidth = CurrentImageWidth - (Mathf.Abs(MaxConvergence) * 2); // Convergence adjustments only need to be calculated on the horizontal axis as this is based on eyes being side-by-side, not placement of the images on the texture.            
                        
            Vector2 newTiling = new Vector2((modifiedTextureWidth * CurrentImageTexelSize.x), m_defaultMainTextureScale.y);            
            
            switch (ImageFormat)
            {
                // Get tiling for Side-by-Side based formats.
                case ImageFormat.Side_By_Side:
                case ImageFormat.VerticalInterlaced:
                case ImageFormat.Checkerboard:
                case ImageFormat.Anaglyph:
                case ImageFormat.Mono_Side_By_Side:
                case ImageFormat.Mono_VerticalInterlaced:
                case ImageFormat.Mono_Checkerboard:
                case ImageFormat.Mono_Anaglyph:
                    newTiling.x /= 2;
                    break;
                // Get tiling for Top/Bottom based formats.
                case ImageFormat.Top_Bottom:
                case ImageFormat.HorizontalInterlaced:
                case ImageFormat.Mono_Top_Bottom:
                case ImageFormat.Mono_HorizontalInterlaced:                    
                    newTiling.y /= 2;
                    break;
                default:
                    break;
            }
                        
            // I hate this, but some textures are like this...
            newTiling.y = (VerticalFlip ? (newTiling.y * -1) : newTiling.y);
                                    
            // Now that we have our values, we update the 2 materials.
            m_leftImageMaterial.mainTextureScale = newTiling;

            switch (ImageFormat)
            {
                case ImageFormat._2D:
                case ImageFormat.Side_By_Side:
                case ImageFormat.Top_Bottom:
                case ImageFormat.TwoImages:
                case ImageFormat.HorizontalInterlaced:
                case ImageFormat.VerticalInterlaced:
                case ImageFormat.Checkerboard:
                case ImageFormat.Anaglyph:
                    m_rightImageMaterial.mainTextureScale = newTiling;
                    break;             
                default:
                    m_rightImageMaterial.mainTextureScale = m_leftImageMaterial.mainTextureScale;
                    break;
            }
        }

        /// <summary>    
        /// This changes the main texture Offset in the shader properties of the left/right materials, so each shows a different half of the texture.
        /// </summary>
        private void SetMaterialOffset()
        {
            // We need the convergence translated into a TextureScale value.
            float currentTextureScaleOffset = (Convergence * CurrentImageTexelSize.x);

            // When cropping out pixels our texture alignment wont be centered anymore unless we compensate for the amount of scale that we lost.
            // This is a quarter of the texture scale amount that has been lost to cropping, because EACH EYE is losing X pixels on each side. So X*2 for each eye = X*4.                        
            float maxTextureScaleOffset = (MaxConvergence * CurrentImageTexelSize.x);

            // We use the default values as a fallback.
            Vector2 newLeftEyeOffset = m_defaultMainTextureOffset;
            Vector2 newRightEyeOffset = m_defaultMainTextureOffset;

            switch (ImageFormat)
            {
                // Get offset for Side-by-Side based formats.
                case ImageFormat.Side_By_Side:
                case ImageFormat.VerticalInterlaced:
                case ImageFormat.Checkerboard:
                case ImageFormat.Anaglyph:
                case ImageFormat.Mono_Side_By_Side:
                case ImageFormat.Mono_VerticalInterlaced:
                case ImageFormat.Mono_Checkerboard:
                case ImageFormat.Mono_Anaglyph:
                    // Texture coordinates use a format where "0,0" is the "Left,Bottom".
                    // So we place the Left image on the Left when not swapping due to our interpriation of the standards.
                    if (currentImage.swapLeftRight)
                        newLeftEyeOffset.x = (m_defaultMainTextureScale.x / 2);
                    else
                        newRightEyeOffset.x = (m_defaultMainTextureScale.x / 2);

                    // Since the image is halved we need to half this too.
                    currentTextureScaleOffset /= 2;
                    maxTextureScaleOffset /= 2;
                    break;
                // Get offset for Top/Bottom based formats.
                case ImageFormat.Top_Bottom:
                case ImageFormat.HorizontalInterlaced:
                case ImageFormat.Mono_Top_Bottom:
                case ImageFormat.Mono_HorizontalInterlaced:
                    // Texture coordinates use a format where "0,0" is the "Left,Bottom".
                    // So we place the Left image on the top when not swapping due to our interpriation of the standards.
                    if (currentImage.swapLeftRight)
                        newRightEyeOffset.y = (m_defaultMainTextureScale.y / 2);
                    else
                        newLeftEyeOffset.y = (m_defaultMainTextureScale.y / 2);
                    break;
                default:
                    break;
            }
                        
            // Here we set the horizontal position of the virtual textures as they are cropped from the source image.
            m_leftImageMaterial.mainTextureOffset = new Vector2(
                (newLeftEyeOffset.x + currentTextureScaleOffset) + maxTextureScaleOffset,
                newLeftEyeOffset.y);
            
            switch (ImageFormat)
            {
                // Non-Mono formats.
                case ImageFormat._2D:
                case ImageFormat.Side_By_Side:
                case ImageFormat.Top_Bottom:
                case ImageFormat.TwoImages:
                case ImageFormat.HorizontalInterlaced:
                case ImageFormat.VerticalInterlaced:
                case ImageFormat.Checkerboard:
                case ImageFormat.Anaglyph:                    
                    m_rightImageMaterial.mainTextureOffset = new Vector2(
                    (newRightEyeOffset.x - currentTextureScaleOffset) + maxTextureScaleOffset,
                    newRightEyeOffset.y);
                    break;
                // Mono formats.
                default:
                    m_rightImageMaterial.mainTextureOffset = m_leftImageMaterial.mainTextureOffset;
                    break;
            }
        }

        /// <summary>
        /// Some formats require "decrypting" the image data in order to be displayed, like Anaglyph, Checkerboard and the Interlaced formats. 
        /// So for those we use a RenderTexture and special shaders, as the alternative would be to slow for animated textures like video.
        /// </summary>
        private void SpecialFormatSetup(Texture sourceTexture)
        {   
            // Setup the appropriate material/shader.
            switch (currentImage.imageFormat)
            {
                case ImageFormat.HorizontalInterlaced:
                case ImageFormat.Mono_HorizontalInterlaced:
                    m_renderMaterial = new Material(m_renderShader);
                    m_shaderPass = SHADER_PASS_HORIZONTAL_INTERLACED;
                    break;
                case ImageFormat.VerticalInterlaced:
                case ImageFormat.Mono_VerticalInterlaced:
                    m_renderMaterial = new Material(m_renderShader);
                    m_shaderPass = SHADER_PASS_VERTICAL_INTERLACED;
                    break;
                case ImageFormat.Checkerboard:
                case ImageFormat.Mono_Checkerboard:
                    m_renderMaterial = new Material(m_renderShader);
                    m_shaderPass = SHADER_PASS_CHECKERBOARD;
                    break;
                case ImageFormat.Anaglyph:
                case ImageFormat.Mono_Anaglyph:
                    m_renderMaterial = new Material(m_renderShader);
                    m_shaderPass = SHADER_PASS_ANAGLYPH;
                    m_renderMaterial.SetColor("_LeftColor", currentImage.leftEyeColor);
                    m_renderMaterial.SetColor("_RightColor", currentImage.rightEyeColor);
                    break;
                default:
                    break;
            }            

            // Create our RenderTexture.
            if (m_renderTexture)
            {
                RenderTexture.active = null;
                m_renderTexture.Release();
            }

            int width = (currentImage.imageFormat == ImageFormat.Anaglyph ||
                         currentImage.imageFormat == ImageFormat.Mono_Anaglyph ?
                         (sourceTexture.width * 2) :
                         sourceTexture.width);
            m_renderTexture = new RenderTexture(width, sourceTexture.height, 24, RenderTextureFormat.ARGB32);
            m_renderTexture.filterMode = FilterMode.Point;

            m_renderTextureSource = sourceTexture;

            // Render the initial image.
            Graphics.Blit(m_renderTextureSource, m_renderTexture, m_renderMaterial, m_shaderPass);

            // Select the given texture into the texture maps of both materials.
            foreach (string targetTextureMap in targetTextureMaps)
            {
                m_leftImageMaterial.SetTexture(targetTextureMap, m_renderTexture);
                m_rightImageMaterial.SetTexture(targetTextureMap, m_renderTexture);
            }
        }
        
#if UNITY_5_6_PLUS
        /// <summary>
        /// We catch when a video is ready after the VideoPlayer being previosuly set because until now we didnt have a texture file to work with.
        /// </summary>
        /// <param name="videoPlayer"></param>
        private void OnPrepareComplete(VideoPlayer videoPlayer)
        {   
            videoPlayer.prepareCompleted -= OnPrepareComplete;

            if (videoPlayer.clip != null || videoPlayer.url != null)
            {
                if (videoPlayer.renderMode == VideoRenderMode.RenderTexture)
                {
                    // We have set a RenderTexture in the inspector.
                    if (videoPlayer.targetTexture != null)
                    {
                        // If its not the same dimensions as the clip, discard it.
                        if (videoPlayer.targetTexture.width != videoPlayer.clip.width || videoPlayer.targetTexture.height != videoPlayer.clip.height)
                        {
                            videoPlayer.targetTexture.Release();
                            videoPlayer.targetTexture = null;
                        }
                    }

                    // If we dont have a RenderTexture set already, make one.
                    if (videoPlayer.targetTexture == null)
                    {
                        // Sets our RenderTexures size to that of the video.
                        videoPlayer.targetTexture = new RenderTexture((int)videoPlayer.texture.width, (int)videoPlayer.texture.height, 24);
                    }

                    // Select the given texture into the texture maps of both materials.
                    foreach (string targetTextureMap in targetTextureMaps)
                    {
                        m_leftImageMaterial.SetTexture(targetTextureMap, videoPlayer.targetTexture);
                        m_rightImageMaterial.SetTexture(targetTextureMap, videoPlayer.targetTexture);
                    }
                }
                else if (videoPlayer.renderMode == VideoRenderMode.APIOnly)
                {
                    switch (currentImage.imageFormat)
                    {
                        case ImageFormat.HorizontalInterlaced:
                        case ImageFormat.Mono_HorizontalInterlaced:
                        case ImageFormat.VerticalInterlaced:
                        case ImageFormat.Mono_VerticalInterlaced:
                        case ImageFormat.Checkerboard:
                        case ImageFormat.Mono_Checkerboard:
                        case ImageFormat.Anaglyph:
                        case ImageFormat.Mono_Anaglyph:
                            SpecialFormatSetup(videoPlayer.texture);
                            break;
                        default:
                            // Select the given texture into the texture maps of both materials.
                            foreach (string targetTextureMap in targetTextureMaps)
                            {
                                m_leftImageMaterial.SetTexture(targetTextureMap, videoPlayer.texture);
                                m_rightImageMaterial.SetTexture(targetTextureMap, videoPlayer.texture);
                            }
                            break;
                    }
                }
            }

            Update3DFomatSettings();
        }
#endif

        #endregion

        #region Public Functions for Runtime Use

        /// <summary>
        /// Set a new Stereoscopic 3D Image to be displayed with this VR3DMediaViewer instance.
        /// </summary>
        /// <param name="newS3DImage"></param>
        public void SetNewImage(Stereoscopic3DImage newS3DImage)
        {
            DisplayImageInCanvas(newS3DImage);
        }

        /// <summary>
        /// Set a new Stereoscopic 3D Image to be displayed with this VR3DMediaViewer instance.
        /// When not supplying the formatting data like with the Stereoscopic3DImage overload, its just assumed you want to use the current 3D settings.
        /// </summary>
        /// <param name="texture">A single texture containing images for both eyes for a Stereoscopic 3D Image. Can be Texture, Texture2D, MovieTexture etc.</param>
        public void SetNewImage(Texture texture)
        {
            Stereoscopic3DImage newS3DImage = Stereoscopic3DImage.Create(texture, ImageFormat, SwapLeftRight, Convergence, MaxConvergence, VerticalFlip, LeftEyeColor, RightEyeColor);

            SetNewImage(newS3DImage);
        }

        /// <summary>
        /// Set a new Stereoscopic 3D Image to be displayed with this VR3DMediaViewer instance.
        /// When not supplying the formatting data like with the Stereoscopic3DImage overload, its just assumed you want to use the current 3D settings.
        /// </summary>
        /// <param name="texture">A texture containing the image for the Left eye of a Stereoscopic 3D Image.</param>
        /// <param name="texture2">A texture containing the image for the Right eye of a Stereoscopic 3D Image.</param>
        public void SetNewImage(Texture texture, Texture texture2)
        {
            Stereoscopic3DImage newS3DImage = Stereoscopic3DImage.Create(texture, texture2, ImageFormat, SwapLeftRight, Convergence, MaxConvergence, VerticalFlip, LeftEyeColor, RightEyeColor);

            SetNewImage(newS3DImage);
        }

#if UNITY_5_6_PLUS
        /// <summary>
        /// Set a new Stereoscopic 3D Image to be displayed with this VR3DMediaViewer instance.
        /// When not supplying the formatting data like with the Stereoscopic3DImage overload, its just assumed you want to use the current 3D settings.
        /// </summary>
        /// <param name="videoClip">A videoClip of a Stereoscopic 3D format video. Requires Unity 5.6 and a UnityEngine.Video.VideoPlayer.</param>
        public void SetNewImage(VideoClip videoClip)
        {
            Stereoscopic3DImage newS3DImage = Stereoscopic3DImage.Create(videoClip, ImageFormat, SwapLeftRight, Convergence, MaxConvergence, VerticalFlip, LeftEyeColor, RightEyeColor);
            
            SetNewImage(newS3DImage);
        }

        /// <summary>
        /// Set a new Stereoscopic 3D Image to be displayed with this VR3DMediaViewer instance.
        /// When not supplying the formatting data like with the Stereoscopic3DImage overload, its just assumed you want to use the current 3D settings.
        /// </summary>
        /// <param name="videoURL">A URL to a video file of a Stereoscopic 3D format video. Requires Unity 5.6 and a UnityEngine.Video.VideoPlayer.</param>
        public void SetNewImage(string videoURL)
        {
            Stereoscopic3DImage newS3DImage = Stereoscopic3DImage.Create(videoURL, ImageFormat, SwapLeftRight, Convergence, MaxConvergence, VerticalFlip, LeftEyeColor, RightEyeColor);

            SetNewImage(newS3DImage);
        }
#endif

        /// <summary>
        /// Call to change the display format of the currently displayed S3DImage.
        /// Can also be use to set the canavas to display the source image with no 3D formating.
        /// Can also be used to display the canvas in "Mono" mode, which displays the same half of a S3DImage to both eyes.
        /// </summary>
        /// <param name="newFormat">Side-by-Side, Top/Bottom. Two Images, etc.</param>
        public void SetImageFormat(ImageFormat newFormat, bool verticalFlip = false)
        {
            ImageFormat = newFormat;
            VerticalFlip = verticalFlip;
            
            // Calling this refreshes the image with the new formatting.
            SetNewImage(currentImage);
        }

        #endregion
    }
}
