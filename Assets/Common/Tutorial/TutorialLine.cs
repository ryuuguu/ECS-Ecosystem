using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
/// <summary>
/// draw lines on screen overlay canvas
/// all draw animated arrow on lines
/// used by TutorialManager
/// </summary>
public class TutorialLine : MonoBehaviour {

    public static Dictionary<string,TutorialLine> instDict;
    
    private static TutorialLine activeInstance;

    public string instanceName;
    
    public float scaleImages = 1;
	public float lineWidth = 5;
	public bool rotateAnimation = false;

	public RectTransform startRT;
	public RectTransform endRT;
	public bool drawArrows;
	public List<GameObject> highLightGOs;

	public Image[] lineImages;
	public TutorialImage[] animateImages;
	public TutorialImage[] endMarkerImages;
	
	//protected int customIndex = 0;
	
	public GameObject arrows;

	public List<LineInfo> lines;

	public bool debugDraw;

	public bool _isAnimating;
	public Vector3[] _animationStartPoints;
	public Vector3[] _animationEndPoints;
	public float[] _animationLengths; //in seconds 
	public float[] _animationTs;
	
    private void Awake() {
	    Setup();
    }

    public void Setup() {
	    if (instDict == null) {
		    instDict = new Dictionary<string, TutorialLine>();
	    }

	    instDict[instanceName] = this;
	    _animationStartPoints = new Vector3[animateImages.Length];
	    _animationEndPoints = new Vector3[animateImages.Length];
	    _animationLengths = new float[animateImages.Length];
	    _animationTs = new float[animateImages.Length];
	    Hide_All();

	    SetScale( 2);


    }
    
    public static void SetInstance(string iName) {
	    activeInstance = instDict?[iName];
	    //Debug.Log("SetInstance: "+ activeInstance);
    }
    
    void Update(){
		if (debugDraw) {
			Draw ();
			debugDraw = false;
		}

		if (_isAnimating ) {
			
			for (int i = 0; i < animateImages.Length; i++) {
				if(_animationLengths[i] < 0) continue;
				if (animateImages[i].useAnimation) {
					animateImages[i].baseAnimationGO.SetActive(true);
					animateImages[i].animator.enabled = true;
					animateImages[i].animator.speed = _animationLengths[i];
					animateImages[i].rectTrans.position = _animationStartPoints[i];
				} else {

					float t = _animationTs[i];
					if (_animationLengths[i] == 0) {
						t = 1;
					}
					else {
						_animationTs[i] += Time.deltaTime / _animationLengths[i];
						_animationTs[i] %= 1;
						t = _animationTs[i];
					}

					LocalAnimateDraw(_animationStartPoints[i], _animationEndPoints[i], t, animateImages[i],
						rotateAnimation);
				}
			}
		}
		
    }
    
    public void SetScale(float val) {
	    scaleImages = val;
	    foreach (var image in animateImages) {
		    var iRect = image.rectTrans;
		    var offset = (iRect.offsetMax - iRect.offsetMin) * scaleImages;
		    iRect.offsetMax = iRect.offsetMin + offset;
	    }
	    foreach (var image in endMarkerImages) {
		    var iRect = image.rectTrans;
		    var offset = (iRect.offsetMax - iRect.offsetMin) * scaleImages;
		    iRect.offsetMax = iRect.offsetMin + offset;
	    }
    }
    
	public void Draw(LineInfo line){
		startRT = line.startRT;
		endRT = line.endRT;
		drawArrows = line.drawArrows;
		highLightGOs = line.highlightGOs;
		Draw();
	}

	public void Draw(string lineName){
		var line = lines.Find (x => x.name == lineName);
		if (line != null) {
			Draw(line);
		} else {
			Debug.LogError ("TutorialLine not found: " + lineName + " :"+lineName+":");
		}
		
	}

    static public void Hide(){
        activeInstance.Hide_All();
    }
    
	public void Hide_All() {
		_isAnimating = false;
		foreach (var lineImage in lineImages) {
			lineImage.enabled = false;
		}
		
		foreach (var tutorialImage in animateImages) {
			tutorialImage.image.enabled = false;
			if (tutorialImage.baseAnimationGO) {
				tutorialImage.baseAnimationGO.SetActive(false);
				tutorialImage.animator.enabled = false;
			}
		}
		
		foreach (var tutorialImage in endMarkerImages) {
			tutorialImage.image.enabled = false;
			if (tutorialImage.baseAnimationGO) {
				tutorialImage.baseAnimationGO.SetActive(false);
				tutorialImage.animator.enabled = false;
			}
		}
		for(int i = 0; i< _animationLengths.Length;i++) {
			_animationLengths[i] = -1;
		}
		if (arrows != null) {
			arrows.SetActive(false);
		}

		foreach (var go in highLightGOs) {
			go.SetActive (false);
		}
	}
	
	public void Draw () {
		foreach (var go in highLightGOs) {
			go.SetActive (true);
		}
		if (startRT != null) {
            arrows.SetActive(drawArrows);
            Vector3[] wc = new Vector3[4];
            startRT.GetWorldCorners(wc);
            var startPoint = wc[0];
            endRT.GetWorldCorners(wc);
            var endPoint = wc[0];

            LocalDraw(startPoint, endPoint);
        }
	}

    static public void Draw(RectTransform rectTransform, Transform transform, int lineNumber = 0){
        Vector3[] wc = new Vector3[4];
        rectTransform.GetWorldCorners(wc);
        var startPoint = (wc[0]+wc[2])*0.5f;
        //Debug.Log(" Draw: " + startPoint);
        var rt = activeInstance.lineImages[lineNumber].canvas.GetComponent<RectTransform>();
        Vector3 scale = rt.sizeDelta;
        var pos = Camera.main.WorldToViewportPoint(transform.position);
        //Debug.Log(pos);
        //Debug.Log(scale);
        pos.Scale(scale);
        pos.Scale(rt.localScale);
        activeInstance.LocalDraw(startPoint, pos);
    }

    public static void Draw(Vector3 startPoint, Vector3 endPoint, bool useEndMarkers = false) {
	    activeInstance.LocalDraw(startPoint, endPoint);
	    if (useEndMarkers) {
		    var ti = activeInstance.endMarkerImages[0];
		    ti.image.enabled = true;
		    var imageRectTransform = ti.rectTrans;
		    imageRectTransform.position = startPoint;
		    ti = activeInstance.endMarkerImages[1];
		    ti.image.enabled = true;
		    imageRectTransform = ti.rectTrans;
		    imageRectTransform.position = endPoint;
		    
	    }
    }

    public static void AnimateDraw(Vector3 startPoint, Vector3 endPoint, float length, int animationNumber = 0 ) {
	    activeInstance._isAnimating = true;
	    activeInstance._animationLengths[animationNumber] = length;
	    activeInstance._animationStartPoints[animationNumber] = startPoint;
	    activeInstance._animationEndPoints[animationNumber] = endPoint;
    }
    
    public static void Animate(Vector3 position, float speed, int animationNumber = 0 ) { 
	    //Debug.Log("Animate: "+ activeInstance.name + " : " + position);
	    if (activeInstance.animateImages[animationNumber].useAnimation) {
		    activeInstance._isAnimating = true;
		    activeInstance._animationLengths[animationNumber] = speed;
		    activeInstance._animationStartPoints[animationNumber] = position;
	    }
    }
    
    public void LocalDraw(Vector3 startPoint, Vector3 endPoint) {
        lineImages[0].enabled = true;
        Vector3 differenceVector = endPoint - startPoint;
        var imageRectTransform = (RectTransform)lineImages[0].transform;
        imageRectTransform.pivot = new Vector2(0, 0.5f);
        imageRectTransform.position = startPoint;
        float angle = Mathf.Atan2(differenceVector.y, differenceVector.x) * Mathf.Rad2Deg;
        imageRectTransform.sizeDelta = new Vector2(differenceVector.magnitude, lineWidth*scaleImages) / imageRectTransform.lossyScale.x;
        imageRectTransform.rotation = Quaternion.Euler(0, 0, angle);
    }
    
    public void LocalAnimateDraw(Vector3 startPoint, Vector3 endPoint, float t, int animationNumber = 0, bool rotate=true) {
	    animateImages[animationNumber].image.enabled = true;
	    Vector3 differenceVector = endPoint - startPoint;
	    var imageRectTransform = animateImages[animationNumber].rectTrans;
	    imageRectTransform.position = Vector3.Lerp( startPoint,endPoint,t);
	    if (rotate) {
		    float angle = Mathf.Atan2(differenceVector.y, differenceVector.x) * Mathf.Rad2Deg;
		    imageRectTransform.rotation = Quaternion.Euler(0, 0, angle);
	    }
    }

    public void LocalAnimateDraw(Vector3 startPoint, Vector3 endPoint, float t, TutorialImage ti, bool rotate=true) {
	    ti.image.enabled = true;
	    Vector3 differenceVector = endPoint - startPoint;
	    var imageRectTransform = ti.rectTrans;
	    imageRectTransform.position = Vector3.Lerp( startPoint,endPoint,t);
	    if (rotate) {
		    float angle = Mathf.Atan2(differenceVector.y, differenceVector.x) * Mathf.Rad2Deg;
		    imageRectTransform.rotation = Quaternion.Euler(0, 0, angle);
	    }
    }
    
    [System.Serializable]
	public class LineInfo{
		public string name;
		public RectTransform startRT;
		public RectTransform endRT;
		public bool draw2Centers;
		public bool drawArrows;
		public List<GameObject> highlightGOs = new List<GameObject>();
	}


	static public Vector2 CanvasToCanvasPosition(RectTransform canvas, Camera aCamera, Vector3 position) {
		//Vector position (percentage from 0 to 1) considering camera size.
		//For example (0,0) is lower left, middle is (0.5,0.5)
		Vector2 temp = aCamera.WorldToViewportPoint(position);

		//Calculate position considering our percentage, using our canvas size
		//So if canvas size is (1100,500), and percentage is (0.5,0.5), current value will be (550,250)
		temp.x *= canvas.sizeDelta.x;
		temp.y *= canvas.sizeDelta.y;

		return temp;
	}

	static public Vector2 WorldToCanvasPosition(RectTransform canvas, Camera aCamera, Vector3 position) {
        //Vector position (percentage from 0 to 1) considering camera size.
        //For example (0,0) is lower left, middle is (0.5,0.5)
        Vector2 temp = aCamera.WorldToViewportPoint(position);

        //Calculate position considering our percentage, using our canvas size
        //So if canvas size is (1100,500), and percentage is (0.5,0.5), current value will be (550,250)
        temp.x *= canvas.sizeDelta.x;
        temp.y *= canvas.sizeDelta.y;
/*
		//This adjustment is wrong 
		//maybe because ??? don't know
         
        //The result is ready, but, this result is correct if canvas recttransform pivot is 0,0 - left lower corner.
        //But in reality its middle (0.5,0.5) by default, so we remove the amount considering cavnas rectransform pivot.
        //We could multiply with constant 0.5, but we will actually read the value, so if custom rect transform is passed(with custom pivot) , 
        //returned value will still be correct.

        temp.x -= canvas.sizeDelta.x * canvas.pivot.x;
        temp.y -= canvas.sizeDelta.y * canvas.pivot.y;
*/
        return temp;
    }

}
