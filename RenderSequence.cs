using UnityEngine;
using System.Collections;
using System;

public class RenderSequence : MonoBehaviour {

	/* VARIABLES FOR RENDERING SCENE */

	// settings for rendering image files
	public int imageWidth=1280;		// image resolution (used by render but not screencap)
	public int imageHeight=720;		// image resolution (used by render but not screencap)
	//[Range(0,25)]public int renderDepth=16; // acceptable depths are 0, 16, 24 (24 has stencil)
	public int startFrame=1;		// the first frame to render
	public int endFrame=500;		// the final frame to render
	private bool captureThisFrame;	// uses start and end frames to determine if a frame should be rendered

	// file extension selected in inspector
	public enum ImageFileTypes {PNG, JPG};
	public ImageFileTypes fileExtensions;

	// reference to render cameras
	private Camera sceneCamera;				// "main" camera that renders to game views and takes screenshots
	private GameObject renderCameraObject;	// gameobject to hold new created camera
	private Camera renderCamera;			// camera for render textures (cannot also be enabled during play)

	// changing time to decouple game from realtime and pause while rendering
	public int frameRate = 25;
	private float timeScale;

	/*	VARIABLES FOR CAPTURING SCREEN INSTEAD
	 *	   /!\	VERY sensitive to visible player sizing in the editor
	 *			regardless of the "screen size" you have set
	 */
	// set the frames per second of captured footage (not renderer)
	public bool captureScreenInstead=true;

	// create directory path to save files
	private string destinationFolder;


	void Start() {

		// assign scene cameras, create render camera and parent render camera to main camera
		sceneCamera = Camera.main;
		renderCameraObject = new GameObject ("Render Camera");
		renderCamera = renderCameraObject.AddComponent<Camera> ();
		renderCameraObject.transform.parent = sceneCamera.transform;

		// change framerate to step through frames instead of realtime
		Time.captureFramerate = frameRate;
		timeScale = Time.timeScale;

		// SCREENCAP SETUP
		if (captureScreenInstead) {
			// verify camera is not disabled
			renderCamera.gameObject.SetActive (true);
			// setup file path for saving screenshots
			destinationFolder = "Screenshots/";
			if (!System.IO.Directory.Exists (destinationFolder)) {
				System.IO.Directory.CreateDirectory(destinationFolder);
			}

		// RENDER SETUP
		} else {
			// initial setup for render
			renderCamera.gameObject.SetActive (false);		// disable camera in order to render
		}
	}
	

	void Update () {

		// check that current frame is among the frames to be rendered
		if (Time.frameCount <= endFrame && Time.frameCount >= startFrame) {
			captureThisFrame = true;
		} else {
			captureThisFrame = false;
		}

		// render and save a certain number of files
		if (!captureScreenInstead && captureThisFrame) {
			StartCoroutine ("RenderAndSave");
		}
		// ignore render and take screenshots instead
		else if (captureScreenInstead && captureThisFrame) {
			StartCoroutine (TakeScreenShot ());
		}
		// notify console that rendering is not currently happening
		else if (Time.frameCount > endFrame) {
			Debug.Log ("Rendering is FINISHED!");
		} else {
			Debug.Log ("Waiting to render...");
		}
	}


	/**
	 * 	Construct a unique file name out of given id and extension
	 */
	public string GenerateFileName (string identifier, string extension) {
		string fileName = string.Format ("{0}.{1}", identifier, extension);
		return fileName;
	}


	/**
	 *	Pause, take a screenshot of the current frame, unpause
	 */
	public IEnumerator TakeScreenShot () {

		// stop time and wait for everything in this frame to update
		Time.timeScale = 0;
		yield return new WaitForEndOfFrame ();

		// name and grab the screenshot
		string screenshotName = string.Format ("{0}.png", Time.frameCount);
		Application.CaptureScreenshot (destinationFolder + screenshotName, 1);
		Debug.Log (string.Format("Screen saved: {0}", screenshotName));

		// start time again to advance to the next frame
		Time.timeScale = timeScale;
		yield return null;

	}


	/**
	 *	Render the current frame and save it to disk as a single image
	 *	 /!\ Requires camera value tweaks to near clipping planes (higher), maybe rendering path and other options
	 *	 /!\ When tested repeatedly in Update(), failed to reproduce fx like DoF
	 */
	public IEnumerator RenderAndSave () {

		// pause time while rendering frame
		Time.timeScale = 0;

		yield return new WaitForEndOfFrame (); 	// wait until all events finish (starts at 2)

		// create a new render texture
		RenderTexture renderTex = new RenderTexture (imageWidth, imageHeight, 24, RenderTextureFormat.RGB565);
		renderTex.antiAliasing = 4;
		renderCamera.targetTexture = renderTex;

		// create image of same dimensions to save render as texture
		Texture2D img = new Texture2D (imageWidth, imageHeight);

		// render the frame
		renderCamera.Render ();
		RenderTexture.active = renderTex;

		// read pixels from render to the new image
		img.ReadPixels (new Rect (0, 0, imageWidth, imageHeight), 0, 0);
		img.Apply ();

		// precaution reset for camera and render textures
		renderCamera.targetTexture = null;
		RenderTexture.active = null;
		Destroy (renderTex);

		// turn image into binary based on file type selected in inspector
		byte[] binary;
		string fileName;
		if (fileExtensions == ImageFileTypes.PNG) {
			binary = img.EncodeToPNG ();
			// build name based on current frame, subtracting in compensation for WaitForEndOfFrame
			fileName = GenerateFileName ((Time.frameCount - 1).ToString (), "png");
		} else {
			binary = img.EncodeToJPG ();
			fileName = GenerateFileName ((Time.frameCount - 1).ToString (), "jpg");
		}
			
		// output the image 
		string filePath = Application.dataPath+"/"+fileName;
		System.IO.File.WriteAllBytes (filePath, binary);

		// unpause to move forward a frame (since calling this coroutine again will pause)
		Time.timeScale = timeScale;

		yield return null;
	}

}
