

VR3DMediaViewer ReadMe


Sections:

	Brief "How to" for known devices/SDKs
	Demo
	FAQ
	Additional Notes
	Stereoscopic 3D formats and standards
	Scripting
	Change Log


---------------------------------------

Brief "How to" for known devices/SDKs:

	Setting up to use this asset can be broken down into 3 areas:

		Camera Setup
		Image/Video Setup
		Canvas Setup

	General Preperation:

		Some SDKs may require it, but not automatically enable the following, so you may need to do it yourself.
			    Edit > Project Settings > Player > Other Settings > Virtual Reality Supported
		
	Camera Setup:

		1. Make sure you have the SDK for your target VR Device imported, and add its Camera Prefab to your scene.
		2. Locate the Camera component in the prefab/MainCamera object in your scene, Right click on it and select "VR3DMediaViewer Camera Setup".
		
			OculusVR SDK Camera:
				"OVRCameraRig/TrackingSpace/CenterEyeAnchor"
		
			SteamVR SDK Camera:
				"[CameraRig]/Camera (head)/Camera (eye)""		
		
			Daydream/Cardboard Camera (Post Unity v5.6):			
				Add a normal camera to the scene.
		
			Daydream/Cardboard Camera (Pre Unity v5.6/Legacy):
				Add a normal camera to the scene.
				Extra steps:
					Create a new GameObject, drag the "****-Left" and "****-Right" camera objects under it and zero their positions/rotations.
					Add a "StereoController.cs" from the GoogleVR SDK folders to it.
					Add an "GvrEye.cs" from the GoogleVR SDK folders to each of the "****-Left" and "****-Right" camera objects, and on each GvrEye component select its corresponding eye and opposite eye Layer.

	Image/Video Setup:

		1. Import a 3D image or video. Once imported this will be refered to as a Texture or Video.
		2. Locate the Texture(s) or Video you want to be displayed in 3D in the Project panel, Right Click on it and select "Create > Stereoscopic 3D Image".
		3. Chose the correct Image Format for the Texture/Video. Example: If the content of the Texture has 2 slightly different images Side-by-Side like [][], its of the "Side-by-Side" format.

	Canvas Setup:

		1. In the Hierarchy panel of the current scene create or select a GameObject with a MeshRenderer and a single Material. If you're uncertain, just Create a "Quad" and select that. This is our Canvas.
		2. With the Canvas GameObject selected, in the Inspector panel add a component called "VR3DMediaViewer".
		3. In the "Default S3D Image" field of the "VR3DMediaViewer" component, select the Stereoscopic 3D Image asset you created in #2-#3 of the "Image/Video Setup".			

    Other Devices/SDKs:
       
        This asset can't predict the existence of every possible VR device or SDK. But it should work with most that I haven't been able to list in this ReadMe.


---------------------------------------

Demo:
 
    Several Demos have been provided.
    All demos have been created with a purpose of conveying this script on a 2D monitor. Different VR HMDs use their own SDK's, which of course I can't include. So all I can do is provide my own generic camera rigs to help illustrate the idea.
    If you want to try using the demo scenes with your VR HMD/SDK, refer to the "How to" above.
	     
    (!BONUS!)
    Stereoscopic 3D Screenshot Demo:
   
        This demo scene is just to show you how the bonus Stereoscopic 3D screenshot script works.
   
        Select the MainCamera object in this scene, and examine the "Stereoscopic 3D Screenshot" component to gather an idea of how to use it.

        The "1"/"!" button in that scene is the key to press to take a screenshot at run time.

        You can call "Stereoscopic3DScreenshot.TakeScreenshot();" from your own scripts too as needed instead.

		Generally you should only take screenshots using the Side-by-Side, Top/Bottom or Two Images formats. Avoid the Interlaced/Checkerboard formats for typical use.

		You can control the full/squashed size of the output image, but be reminded. You can always squash a full image later in a paint program, but cant un-squash a image without quality loss.

		Generally you should leave the projection setting on OffCenter. The other 2 have little to no benefits but do have drawbacks.

		When changing the focal distance you can see a plane in the Scene View. This represents the images frame and point in the resulting image were your focus about matches the border/frame of the image. Objects in between the plane and the camera will appear to pop out of the image.


---------------------------------------

FAQ:
	
	Issue:
		My image looks more pixelated or blurry then I would expect since its of a High Resolution.
	
	Answer:
		This is likely because Stereoscopic 3D images tend to be in monitor resolutions (I.E. 1920x1080), and as such commonly exceed the default size that Unity imports them as. When that happens Unity lowers the resolution to more common texture resolutions.
		So after importing a image, select it and ensure it's the size you want. If not, try turning up the max size past the larger of the base images Width/Height.
		Also Unity by default rounds a textures resolution to the nearest "Power of 2". So you may want to set the textures "Advanced > Non Power of 2" property to "None".


	Issue:
		I create my texture/video at runtime (perhaps like by downloading it from a webhost), but the setup seems geared only towards pre-exiting textures/video. Can I still use this?

	Answer:
		Yes! Though naturally you would need to use scripting. Refer to the Scripting section of the ReadMe for more. Of note though is that dynamicly created textures/video wont come attached with 3D formating data like "Is it Side-by-Side, Top-Bottom, etc?". You will need to figure out or supply this info on your own.


	Issue:
		I can tell my 3D Image/Video is displaying differently then a 2D image. But its an underwhelming or difficult to look at effect. Something seems off about it.

	Answer:
		A common cause of this is the Stereoscopic 3D Image asset needing its SwapLeftRight property changed. Some images may be incorrectly labeled from what you expect, or were meant for whats known as "Cross-eyed" viewing.
		For example, the Demo Scenes included with this asset are meant for cross-eyed viewing as the demos need to convey the purpose of the asset to people using a 2D monitor, and the Cross-eye technique can be done with a 2D monitor by anyone.
		This is just something you will have to test and set for each image yourself.


	Issue:
		I have to strain to look at my image. It's as if what im trying to look at in the image is REALLY close to my eyes and I just cant converge my eyes together enuough to focus on it.

	Answer:
		Sometimes 3D images were taken incorrectly, and the center of each eyes image is to far apart. It's up to the artists flavor, but generally the bulk of a 3D images content should appear as if its behind the borders of the image, with maybe 1 or so forground objects in the center jumping out as an exception. Images that cause a stain such as this dont appear behind the border, but instead in front.
		I've added a option to the Stereoscopic 3D Image assets called "Convergence". Using this you can still use such incorrectly taken 3D images by sacraficing some pixels from the width of the image. How many pixels you need to sacrafice with "Max Convergence" and what "Convergence" value you need is something you will have to play with and figure out yourself per image.

	
	Issue:
		Interlaced/Checkerboard media looks like it isnt working. Or maybe made worse by being more blocky/pixelated.

	Answer:
		While I cant dismiss the possibility of a bug, this is likely not the case. What is probably the reason is that the media you are trying to display has experienced some data loss due to compression. JPG for example is not a loss-less format, and is terrible for interlaced/checkerboard 3D.
		Likewise, videos uploaded to common websites like YouTube get recompressed and as such the alternating interlacing patterns get disrupted in the final video.
		However in the case of images, Unitys own default compression and settings can distort the image enough to disrupt the interlacing pattern.
		Change the following texture import settings to:
			Non Power of 2  = None
			Gerate Mip Maps = false
			Filter Mode     = Point Filter
			Compression     = None

	Issue:
		Videos dont play when using the Unity 5.6 VideoPlayer. No texture is ever set to the canvases Mesh Renderer material.

	Answer:
		Try selecting the video clip file and selecting H264 for a Codec. This has fixed such an issue several times for me. This may get better over time as the VideoPlayer component is improved.

		

---------------------------------------

Additional Notes:
   
    Reflection Probes will only reflect the image from one eye. Thankfully most 3D images aren't very different per eye, so viewers wont notice the difference in the reflection as much, if at all.    
    The Stereoscopic 3D image can use transparency.
    You can use any shader you want. Standard, Unlit, Custom, etc.



---------------------------------------

Stereoscopic 3D formats and standards:

    Through my research of the various stereoscopic 3D formats I've tried to support with this asset, I've come to the conclusion that, there just aren't any "Standards".
    What I mean by this is, for example, to some Top/Bottom images have the image meant for the left eye on top, for others the Right eye is on top.
    Even the terms used for the formats don't have a single standard. Top/Bottom can also be called Over/Under, Above/Below, etc.
    Ideally I would support the "Standards" by default, and require swapping the left and right eye images only for cross-eyed SBS images and fringe cases. But that's just not how things are.
    So I'm going to do my best to support the common implementations of the standards as I can see based off of images/videos I find online.
    Because of that, all I can do is call how this asset supports stereoscopic 3D as "My Standards", or "VR3DMediaViewer Standards".

    Here are the standards this asset acknowledges, for reference:

        Format										ASCII Representation	Notes

        Side-by-Side:
            A.K.A. SBS, L/R							[L][R]					This supports Full* or Squashed*.
            Variations:														This supports Cross-eyed by use of the Swap Left/Right option.
                Squashed/Half Size*
                Full*
                Cross-eyed

        Top-Bottom:
            A.K.A. Over-Under, Above-Below			[ L ]					This supports Full* or Squashed*.
            Variations:								[ R ]					The image for the left eye is expected to be on the top.
                Squashed/Half Size*											Use the Swap Left/Right option if your image has the right on top.
                Full*

        Two Images:
            A.K.A.									N/A						This is simply using a different image file/texture for each eye.

        Horizontal Interlaced:
            A.K.A. Interlaced, Row Interlaced		[R][R][R][R]			This supports Full* or Squashed*, though generally such images are squashed.
            Variations:								[L][L][L][L]			All odd rows are for the right image, even rows are for the left.
                Squashed/Half Size*					[R][R][R][R]			Internally, this converts to squashed Top-Bottom as described above.
                Full*								[L][L][L][L]			Use the Swap Left/Right option if your image has the left as odd/right as even.

        Vertical Interlaced:
            A.K.A. Column Interlaced				[R][L][R][L]			This supports Full* or Squashed*, though generally such images are squashed.
            Variations:								[R][L][R][L]			All odd columns are for the right image, even columns are for the left.
                Squashed/Half Size*					[R][L][R][L]			Internally, this converts to squashed Side-by-Side as described above.
                Full*								[R][L][R][L]			Use the Swap Left/Right option if your image has the left as odd/right as even.

        Checkerboard:
            A.K.A. Tiled							[L][R][L][R]			This supports Full* or Squashed*, though generally such images are squashed.
            Variations:								[R][L][R][L]			All odd pixels on odd rows are for the left image, even pixels are for the right. All even pixels on odd rows are for the left, odd pixels are for the right.
                Squashed/Half Size*					[L][R][L][R]			Internally, this converts to squashed Side-by-Side as described above.
                Full*								[R][L][R][L]			Use the Swap Left/Right option if your image has the right as odd/left as even.

        Anaglyph:
            A.K.A. Red/Cyan, Red/Blue, R/B, etc		N/A						Both images exist in the same space
            Variations:														This only support Red/Cyan right now.
                Red/Cyan													This displays a very different image to each eye with a similar effect to wearing colored glasses, but only having the image tinted.
                Magenta/Green												Internally, this converts to squashed Side-by-Side as described above.
                Yellow/Blue													There is a option for trying to show each image as monochrome, but it has to be enabled by editing the script.

         Frame Sequential:							N/A						This is just listed for completeness. It's UNSUPPORTED!
            A.K.A. Alternating Frames										This is a video only format.
                Variations:													Proper, full video support would be nice, but it is just to hard to actively support for an asset like this.
                    N/A														If you want to use 3D video, it's recommended to use SBS formatted 3D video.

        * Full or Squashed/Half-sized means, if both images are full resolution, or cut in half so both fit into a full resolution ratio. Example 3840x1080 = Full, 1920x1080 = Squashed/Half sized.
		       


  ---------------------------------------

  Scripting:

		The main scripts in this asser use the "VR3D" namespace.

		VR3DMediaViewer.cs:
			
			SetNewImage();
				You can call this at any time after the scripts Start() method has finished to display a new image in the canvas.
				Generally you should use the "Stereoscopic3DImage" overload so you can supply how the image should be formated, but overloads for just a Texture/VideoClip/URL exist too. Using those uses the current 3D settings with the new image.

			SetImageFormat();
				You can call this to change the current images format to something new.
			
			The following are public assesors you can set directly and see a immediate change.
				SwapLeftRight;
				Convergence;
				MaxConvergence;
				VerticalFlip; (Only available via scripting.)
				LeftEyeColor;
				RightEyeColor;

			Events:
				NewImageLoaded(Stereoscopic3DImage newImage);
					Subscribe to this to know when a new image is loaded. The "defaultImage" being loaded will trigger this too.
				FormatChanged(ImageFormat imageFormat);
					Subsribe to this to know when the format for the currentImage has changed. This will also fire when a image is loaded.

		Stereoscopic3DScreenshot.cs:

			TakeScreenshot();
				Call this to take a screenshot.
				Screenshots are saved in the applications data directory using the applications name with some date/time info appended.

			Get3DTextures(Texture2D texture, Texture2D texture2 = null);
				If you want a little more control over the use of the screenshot, you can call this supplying a texture (or 2 if using TwoImages). 
				The result will be the screenshot as a texture you can do whatever with. Perhaps display immediately in game? Save it yourself using your own naming convention?

		Notes:
			You can view an example of setting images via script in the demo script named "ImageCycler".
  
  
  ---------------------------------------

  Change Log:

  2.0:
		Complete rewrite.
		Full support for all formats when using animated textures like video.
		New "Stereoscopic 3D Image" asset system to better pre-configure media for display.
		Easier streamlined setup.
		Support for Unity 5.6 VideoPlayer.
		Improved Stereoscopic 3D Screenshot script.
		Reorganized file hierarchy.
		Replaced the old "Example Image" with 7 real Stereoscopic 3D images.