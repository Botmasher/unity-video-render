using UnityEngine;
using System.Collections;
using System;

public class RenderSequence : MonoBehaviour {

	/* FOR RENDERING SCENE */

	// settings for rendering image files
	public int imageWidth=1280;
	public int imageHeight=720;
	[Range(0,25)]public int renderDepth=5;

	// file extension selected in inspector
	public enum imageFileTypes {PNG, JPG};
	public imageFileTypes fileExtensions;

	// counter to individuate file names
	public static int renderCounter;

	// reference to render camera
	public Camera renderCamera;

	/*	FOR CAPTURING SCREEN INSTEAD
	 *	   /!\	VERY sensitive to visible player sizing in the editor
	 *			regardless of the "screen size" you have set
	 */
	// set the frames per second of captured footage (not renderer)
	public bool captureScreenInstead=true;
	public int frameRate = 25;
	// create directory path to save files
	private string destinationFolder;


	void Start() {

		// SCREENCAP SETUP
		if (captureScreenInstead) {
			// change framerate to step through frames instead of realtime
			Time.captureFramerate = frameRate;
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
			renderCounter = 0;
		}
	}
	

	void Update () {

		// render and save a certain number of files
		if (!captureScreenInstead) {
			StartCoroutine ("RenderAndSave");
			renderCounter++;
		}
		// ignore render and take screenshots instead
		else {
			Application.CaptureScreenshot (destinationFolder + string.Format ("{0}.png", Time.frameCount), 1);
			Debug.Log (string.Format("Screen saved: {0}.png", Time.frameCount));
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
	 *	Render the current frame and save it to disk as a single image
	 *	 /!\ Requires camera value tweaks to near clipping planes (higher), maybe rendering path and other options
	 *	 /!\ When tested repeatedly in Update(), failed to reproduce fx like DoF
	 */
	public IEnumerator RenderAndSave () {

		yield return new WaitForEndOfFrame ();

		// create a new render texture
		RenderTexture renderTex = new RenderTexture (imageWidth, imageHeight, renderDepth, RenderTextureFormat.ARGB32);
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
		if (fileExtensions == 0) {
			binary = img.EncodeToPNG ();
			fileName = GenerateFileName (renderCounter.ToString(),"png");
		} else {
			binary = img.EncodeToJPG ();
			fileName = GenerateFileName (renderCounter.ToString(),"jpg");
		}
			
		// output the image 
		string filePath = Application.dataPath+"/"+fileName;
		System.IO.File.WriteAllBytes (filePath, binary);

		yield return null;
	}

}
