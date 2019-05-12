using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MouseInteraction : MonoBehaviour {

	[Tooltip("Color change of the interacted object.")]
		public Color interactionColor = new Color(0.38f, 0.97f, 0.44f) ;
	[Tooltip("Fade speed of the color change (slow -> quick)")]
		public float interactionSpeed = 4f ;
	[Tooltip("Emission intensity (doesn't work with material which has no emissive intensity)")]
		[Range(0f, 1f)]public float emissionIntensity = 0f ;
	[Tooltip("Use the center of the screen instead mouse position to detect interaction.")]
		public bool useCenterScreen = false ;
	[Tooltip("Use the touch/click instead mouse position to detect interaction.")]
		public bool useTouchClick = false ;
	[Tooltip("Cursor sprite when interaction with the object (texture must be a Cursor in import settings)")]
		public Texture2D mouseCursor ;
	[Tooltip("Interaction with all objects of the same parent.")]
		public bool groupedInteraction = false ;
	[Tooltip("Number of ascent to define the parent for the Grouped Interaction setting.")]
		public int numberOfAscent = 1 ;
	[Tooltip("Max distance of the interaction (1000 = far away, 6 = melee range)")]
		public int interactionDistance = 1000 ;
	[Tooltip("Animation played when interacted.")]
		public AnimationClip interactionAnim ;
	[Tooltip("Loop the interacted animation.")]
		public bool animationLoop = true ;
	[Tooltip("[Loop animation only] Reset the animation loop when the interaction exit.")]
		public bool animationReset = false ;
	[Tooltip("Show a text over the interacted object.")]
		public bool showTooltip = true ;
	[Tooltip("Show a predefined UI Panel over the interacted object.")]
		public GameObject tooltipUIPanel ;
	/*[Tooltip("[Mouse interaction only] Check if the UI Panel is outside the screen, and auto change its anchor.")]
		public bool checkOutOfScreen = true ;*/
	[Tooltip("Show the tooltip over the object instead of over the mouse.")]
		public bool fixedToTheObject = false ;
	[Tooltip("Position of the tooltip showed over the interacted object.")]	
		public Vector2 tooltipPosition = new Vector2 (-50f, 30f) ;
	[Tooltip("Text to show over the interacted object.")]
		public string tooltipText = "" ;
	[Tooltip("Color of the text showed over the interacted object.")]
		public Color tooltipColor = new Color(0.9f, 0.9f, 0.9f) ;
	[Tooltip("Size of the text showed over the interacted object.")]
		public int tooltipSize = 20 ;
	[Tooltip("Resize the text, relative to the distance between the object and the camera.")]
		public bool textResized = false ;
	[Tooltip("Font of the text showed over the interacted object.")]
		public Font tooltipFont ;
	public enum TooltipAlignment {Center, Left, Right}
	[Tooltip("Alignment of the text showed over the interacted object.")]
		public TooltipAlignment tooltipAlignment ;
	[Tooltip("Color of the text shadow showed over the interacted object.")]
	public Color tooltipShadowColor = new Color(0.1f, 0.1f, 0.1f) ;
	[Tooltip("Position of the text shadow showed over the interacted object.")]
		public Vector2 tooltipShadowPosition = new Vector2 (-2f, -2f) ;

	private Renderer render ;
	private Renderer[] render_child ;
	private MouseInteraction[] scripts_child ;
	private Material[] allMaterials ;
	private Color[] baseColor ;
	private float t = 0f ;
	private bool over = false ;
	private bool otherGroupedObj = false ;
	private bool clickDelayed = false ;
	private Vector2 UIanchor ;
	private string currentText = "" ;
	private GUIStyle tooltipStyle = new GUIStyle() ;
	private GUIStyle tooltipStyleShadow = new GUIStyle() ;
	private Vector3 positionToScreen ;
	private float cameraDistance ;
	private bool lookedByCam = false ;
	private Animation animationComponent ;

	// ===== HINTS =====
	// Don't forget to attach a collider component to the object which must be interacted.
	// For the center of screen interaction, don't forget to attach the CameraRaycast script to your main active camera.

	// Initialization
	void Start () {

		// Get all materials and all colors for supporting multi-materials object
		render = GetComponent<Renderer>() ;
		allMaterials = render.materials;
		baseColor = new Color[allMaterials.Length] ;
		int temp_length = baseColor.Length ;
		for(int i=0; i<temp_length; i++) {
			baseColor[i] = allMaterials[i].color ;
		}

		// Get all parent and children script according to the Number Of Ascent
		if(groupedInteraction) {
			Transform current_transform = transform ;
			for(int i = 1 ; i <= numberOfAscent ; i++) {
				current_transform = current_transform.parent ;
			}
			scripts_child = current_transform.GetComponentsInChildren<MouseInteraction>() ;
		}

		// Get the animation component, if exist
		if(interactionAnim != null)
			animationComponent = GetComponent<Animation>() ;

		// Start settings of the tooltip
		if (showTooltip) { // Tooltip text style customization
			if(tooltipUIPanel != null) { // Initialization of the UI Panel
				tooltipUIPanel.SetActive(false) ;
			}
			tooltipStyle.normal.textColor = tooltipColor ; // Color of the tooltip text
			tooltipStyleShadow.normal.textColor = tooltipShadowColor ; // Color of the tooltip shadow
			tooltipStyle.fontSize = tooltipStyleShadow.fontSize = tooltipSize ; // Size of the tooltip font
			tooltipStyle.fontStyle = tooltipStyleShadow.fontStyle = FontStyle.Bold ; // Style of the tooltip font
			tooltipStyle.font = tooltipStyleShadow.font = tooltipFont ;
			switch(tooltipAlignment) { // Alignment of the tooltip text
				case TooltipAlignment.Center :
					tooltipStyle.alignment = tooltipStyleShadow.alignment = TextAnchor.UpperCenter ;
					break ;
				case TooltipAlignment.Left :
					tooltipStyle.alignment = tooltipStyleShadow.alignment = TextAnchor.UpperLeft ;
					break ;
				case TooltipAlignment.Right :
					tooltipStyle.alignment = tooltipStyleShadow.alignment = TextAnchor.UpperRight ;
					break ;
				default :
					break ;
			}
		}

	}
	
	// Update once per frame
	void Update () {
		
		if (over && t < 1) { // Fade of the interaction enter color
			foreach(Material material in allMaterials) {
				material.color = Color.Lerp(material.color, interactionColor, t) ;
				if(emissionIntensity > 0f) {
					Color baseEColor = material.GetColor("_EmissionColor") ;
					Color newEColor = new Color (emissionIntensity, emissionIntensity, emissionIntensity) ;
					material.SetColor("_EmissionColor", Color.Lerp(baseEColor, newEColor, t)) ;
				}
			}
			t += interactionSpeed * Time.deltaTime ;
		} else if (!over && t < 1f) { // Fade of the interaction exit color
			foreach(Material material in allMaterials) {
				material.color = Color.Lerp(material.color, baseColor[System.Array.IndexOf(allMaterials, material)], t) ;
				if(emissionIntensity > 0f) {
					Color baseEColor = material.GetColor("_EmissionColor") ;
					Color newEColor = new Color (0f, 0f, 0f) ;
					material.SetColor("_EmissionColor", Color.Lerp(baseEColor, newEColor, t)) ;
				}
			}
			t += interactionSpeed * Time.deltaTime ;
		}
		// Check the interaction exit in Touch/Click mode
		if (useTouchClick && Input.GetMouseButtonDown(0) && over && clickDelayed) {
			if(groupedInteraction) {
				foreach(MouseInteraction script in scripts_child) {
					script.otherGroupedObj = false ;
					script.interaction_exit() ;
					script.clickDelayed = false ;
				}
			} else {
				interaction_exit() ;
				clickDelayed = false ;
			}
		}
		
	}

	// Called when mouse over this object
	void OnMouseEnter () {

		if (!useTouchClick && !useCenterScreen && (Vector3.Distance(Camera.main.transform.position, this.transform.position) < interactionDistance)) {
			if(groupedInteraction) {
				foreach(MouseInteraction script in scripts_child) {
					if(script.transform != this.transform)
						script.otherGroupedObj = true ;
					script.interaction_enter() ;
				}
			} else {
				interaction_enter() ;
			}
		}

	}

	// Called when mouse exit this object
	void OnMouseExit () {

		if (!useTouchClick && !useCenterScreen) {
			if(groupedInteraction) {
				foreach(MouseInteraction script in scripts_child) {
					script.otherGroupedObj = false ;
					script.interaction_exit() ;
				}
			} else {
				interaction_exit() ;
			}
		}
		
	}

	// Called when mouse click on this object
	void OnMouseDown () {
		
		if (useTouchClick && !useCenterScreen && !over && (Vector3.Distance(Camera.main.transform.position, this.transform.position) < interactionDistance)) {
			if(groupedInteraction) {
				foreach(MouseInteraction script in scripts_child) {
					if(script.transform != this.transform)
						script.otherGroupedObj = true ;
					script.interaction_enter() ;
				}
			} else {
				interaction_enter() ;
			}
			Invoke("clickDelay", 0.1f) ;
		}
		
	}

	// Tooltip creation
	void OnGUI () {

		if(!otherGroupedObj) {
			// Display of text/tooltip for the Mouse interaction mode
			if (showTooltip && !fixedToTheObject && !useCenterScreen && over) {
				if(textResized) {
					cameraDistance = Vector3.Distance(Camera.main.transform.position, this.transform.position) ;
					tooltipStyle.fontSize = tooltipStyleShadow.fontSize = Mathf.RoundToInt(tooltipSize - (cameraDistance/3)) ;
				}
				GUI.Label(new Rect (Event.current.mousePosition.x + tooltipPosition.x - tooltipShadowPosition.x, Event.current.mousePosition.y + tooltipPosition.y - tooltipShadowPosition.y, 100f, 20f), currentText, tooltipStyleShadow) ;
				GUI.Label(new Rect (Event.current.mousePosition.x + tooltipPosition.x, Event.current.mousePosition.y + tooltipPosition.y, 100f, 20f), currentText, tooltipStyle) ;
				if(tooltipUIPanel != null) {
					tooltipUIPanel.transform.localPosition = new Vector3(Event.current.mousePosition.x + tooltipPosition.x - Screen.width/2f, -Event.current.mousePosition.y + tooltipPosition.y + Screen.height/2f, 0) ;
				}
			// Display of text/tooltip for the Center of the Screen interaction mode
			} else if (showTooltip && !fixedToTheObject && useCenterScreen && lookedByCam && over) {
				if(textResized) {
					cameraDistance = Vector3.Distance(Camera.main.transform.position, this.transform.position) ;
					tooltipStyle.fontSize = tooltipStyleShadow.fontSize = Mathf.RoundToInt(tooltipSize - (cameraDistance/3)) ;
				}
				GUI.Label(new Rect (Screen.width/2f + tooltipPosition.x - tooltipShadowPosition.x, Screen.height/2f + tooltipPosition.y - tooltipShadowPosition.y, 100f, 20f), currentText, tooltipStyleShadow) ;
				GUI.Label(new Rect (Screen.width/2f + tooltipPosition.x, Screen.height/2f + tooltipPosition.y, 100f, 20f), currentText, tooltipStyle) ;
				if(tooltipUIPanel != null) {
					tooltipUIPanel.transform.localPosition = new Vector3(tooltipPosition.x, tooltipPosition.y, 0) ;
				}
			// Display of text/tooltip for the Touch/Click interaction mode
			} else if (showTooltip && fixedToTheObject && over) {
				positionToScreen = Camera.main.WorldToScreenPoint(transform.position) ;
				cameraDistance = Vector3.Distance(Camera.main.transform.position, this.transform.position) ;
				if(textResized)
					tooltipStyle.fontSize = tooltipStyleShadow.fontSize = Mathf.RoundToInt(tooltipSize - (cameraDistance/3)) ;
				GUI.Label(new Rect (positionToScreen.x + tooltipPosition.x - tooltipShadowPosition.x, -positionToScreen.y + Screen.height + tooltipPosition.y / (cameraDistance/10) - tooltipShadowPosition.y, 100f, 20f), currentText, tooltipStyleShadow) ;
				GUI.Label(new Rect (positionToScreen.x + tooltipPosition.x, -positionToScreen.y + Screen.height + tooltipPosition.y / (cameraDistance/10), 100f, 20f), currentText, tooltipStyle) ;
				if(tooltipUIPanel != null) {
					tooltipUIPanel.transform.localPosition = new Vector3(positionToScreen.x + tooltipPosition.x - Screen.width/2f, positionToScreen.y - Screen.height/2f + tooltipPosition.y / (cameraDistance/10), positionToScreen.z) ;
				}
			}
		}
		
	}

	// Called when camera over this object
	void lookedByCam_enter () {

		if (useCenterScreen && !lookedByCam && (Vector3.Distance(Camera.main.transform.position, this.transform.position) < interactionDistance)) {
			if(groupedInteraction) {
				foreach(MouseInteraction script in scripts_child) {
					script.lookedByCam = true ;
					if(script.transform != this.transform)
						script.otherGroupedObj = true ;
					script.interaction_enter() ;
				}
			} else {
				lookedByCam = true ;
				interaction_enter() ;
			}
		}

	}

	// Called when camera exit this object
	void lookedByCam_exit () {

		if (useCenterScreen && lookedByCam) {
			if(groupedInteraction) {
				foreach(MouseInteraction script in scripts_child) {
					script.lookedByCam = false ;
					script.otherGroupedObj = false ;
					script.interaction_exit() ;
				}
			} else {
				lookedByCam = false ;
				interaction_exit() ;
			}
		}
		
	}

	// Begin the interaction system (show tooltip and focus color of this object)
	void interaction_enter () {

		t = 0f ;
		over = true ;
		currentText = tooltipText ;
		if (mouseCursor != null && !useCenterScreen && !useTouchClick) {
			Cursor.SetCursor (mouseCursor, Vector2.zero, CursorMode.Auto);
		}
		if(tooltipUIPanel != null) {
			Invoke("tooltipDelayOn", 0.05f) ;
		}
		if (interactionAnim != null) {
			if(animationReset) {
				animationComponent[interactionAnim.name].time = 0.0f ;
				animationComponent[interactionAnim.name].speed = 1.0f ;
			}
			if(animationLoop) {
				animationComponent[interactionAnim.name].wrapMode = WrapMode.Loop ;
			} else {
				animationComponent[interactionAnim.name].wrapMode = WrapMode.Once ;
			}
			animationComponent.Play(interactionAnim.name) ;
		}

	}

	// End the interaction system (hide tooltip and focus color of this object)
	void interaction_exit () {

		t = 0f ;
		over = false ;
		currentText = "" ;
		if (mouseCursor != null && !useCenterScreen && !useTouchClick) {
			Cursor.SetCursor (null, Vector2.zero, CursorMode.Auto);
		}
		if(tooltipUIPanel != null) {
			Invoke("tooltipDelayOff", 0.05f) ;
		}
		if (interactionAnim != null) {
			if(animationLoop) {
				animationComponent.Stop () ;
				if(animationReset) {
					animationComponent[interactionAnim.name].time = 0.0f ;
					animationComponent[interactionAnim.name].speed = 0.0f ;
					animationComponent.Play(interactionAnim.name) ;
				}
			}
		}

	}
	
	// Delay for the click interaction
	void clickDelay () {

		clickDelayed = true ;

	}

	// Delay for the tooltip display (on)
	void tooltipDelayOn () {
		
		tooltipUIPanel.SetActive(true) ;
		
	}

	// Delay for the tooltip display (off)
	void tooltipDelayOff () {
		
		tooltipUIPanel.SetActive(false) ;
		
	}

}
