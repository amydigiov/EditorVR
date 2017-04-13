﻿#if UNITY_EDITOR
using System;
using System.Collections;
using System.Text;
using UnityEditor.Experimental.EditorVR.Extensions;
using UnityEditor.Experimental.EditorVR.Helpers;
using UnityEditor.Experimental.EditorVR.UI;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Menus
{
	public sealed class PinnedToolButton : MonoBehaviour, IPinnedToolButton,  ITooltip, ITooltipPlacement, ISetTooltipVisibility, ISetCustomTooltipColor, IConnectInterfaces, IUsesMenuOrigins
	{
		static Color s_FrameOpaqueColor;
		static Color s_SemiTransparentFrameColor;

		const int k_MenuButtonOrderPosition = 0; // A shared menu button position used in this particular ToolButton implementation
		const int k_ActiveToolOrderPosition = 1; // A active-tool button position used in this particular ToolButton implementation
		private const float k_alternateLocalScaleMultiplier = 0.85f; //0.64376f meets outer bounds of the radial menu
		const string k_MaterialColorProperty = "_Color";
		const string k_MaterialAlphaProperty = "_Alpha";
		const string k_SelectionToolTipText = "Selection Tool (cannot be closed)";
		const string k_MainMenuTipText = "Main Menu (cannot be closed)";
		readonly Vector3 k_ToolButtonActivePosition = new Vector3(0f, 0f, -0.035f);

		public Type toolType
		{
			get
			{
				return m_ToolType;
			}
			set
			{
				m_GradientButton.gameObject.SetActive(true);

				m_ToolType = value;
				if (m_ToolType != null)
				{
					transform.localScale = moveToAlternatePosition ? m_OriginalLocalScale : m_OriginalLocalScale * k_alternateLocalScaleMultiplier;
					transform.localPosition = moveToAlternatePosition ? m_AlternateLocalPosition : m_OriginalLocalPosition;
					if (isSelectionTool || isMainMenu)
					{
						//activeButtonCount = 1;
						order= isMainMenu ? menuButtonOrderPosition : activeToolOrderPosition;
						tooltipText = isSelectionTool ? k_SelectionToolTipText : k_MainMenuTipText;
						gradientPair = UnityBrandColorScheme.sessionGradient; // Select tool uses session gradientPair
					}
					else
					{
						tooltipText = toolType.Name;

						// Tools other than select fetch a random gradientPair; also used by the device when highlighted
						gradientPair = UnityBrandColorScheme.GetRandomGradient();
					}

					m_GradientButton.SetContent(GetTypeAbbreviation(m_ToolType));
					activeTool = true;
					m_GradientButton.visible = true;
				}
				else
				{
					m_GradientButton.visible = false;
					gradientPair = UnityBrandColorScheme.grayscaleSessionGradient;
				}
			}
		}

		public int order
		{
			get { return m_Order; }
			set
			{
				m_Order = value; // Position of this button in relation to other pinned tool buttons
				//m_InactivePosition = s_ActivePosition * ++value; // Additional offset for the button when it is visible and inactive
				activeTool = activeTool;
				const float kSmoothingMax = 50f;
				const int kSmoothingIncreaseFactor = 10;
				//var smoothingFactor = Mathf.Clamp(kSmoothingMax- m_Order * kSmoothingIncreaseFactor, 0f, kSmoothingMax);
				//m_SmoothMotion.SetPositionSmoothing(smoothingFactor);
				//m_SmoothMotion.SetRotationSmoothing(smoothingFactor);
				//this.RestartCoroutine(ref m_PositionCoroutine, AnimatePosition());
				m_LeftPinnedToolActionButton.visible = false;
				m_RightPinnedToolActionButton.visible = false;

				// We move in counter-clockwise direction
				// Account for the input & position phase offset, based on the number of actions, rotating the menu content to be bottom-centered
				const float kMaxPinnedToolButtonCount = 16; // TODO: add max count support in selectTool/setupPinnedToolButtonsForDevice
				const float kRotationSpacing = 360f / kMaxPinnedToolButtonCount; // dividend should be the count of pinned tool buttons showing at this time
				var phaseOffset = kRotationSpacing * 0.5f - (activeButtonCount * 0.5f) * kRotationSpacing;
				var newTargetRotation = Quaternion.AngleAxis(phaseOffset + kRotationSpacing * m_Order, Vector3.down);
				this.RestartCoroutine(ref m_PositionCoroutine, AnimatePosition(newTargetRotation));
				//transform.localRotation = newLocalRotation;
			}
		}

		/// <summary>
		/// GradientPair should be set with new random gradientPair each time a new Tool is associated with this Button
		/// This gradientPair is also used to highlight the input device when appropriate
		/// </summary>
		public GradientPair gradientPair
		{
			get { return m_GradientPair; }
			set
			{
				m_GradientPair = value;
				customToolTipHighlightColor = value;
			}
		}

		/// <summary>
		/// Type, that if not null, denotes that preview-mode is enabled
		/// This is enabled when highlighting a tool on the main menu
		/// </summary>
		public Type previewToolType
		{
			get { return m_previewToolType; }
			set
			{
				m_previewToolType = value;

				if (m_previewToolType != null) // Show the highlight if the preview type is valid; hide otherwise
				{
					// Show the grayscale highlight when previewing a tool on this button
					m_GradientButton.highlightGradientPair = UnityBrandColorScheme.grayscaleSessionGradient;
					m_GradientButton.SetContent(GetTypeAbbreviation(m_previewToolType));
					tooltipText = "Assign " + m_previewToolType.Name;
					customToolTipHighlightColor = UnityBrandColorScheme.grayscaleSessionGradient;
					this.ShowTooltip(this);
				}
				else
				{
					activeTool = activeTool;
					m_GradientButton.SetContent(GetTypeAbbreviation(m_ToolType));
					customToolTipHighlightColor = gradientPair;
					this.HideTooltip(this);
					tooltipText = (isSelectionTool || isMainMenu) ? (isSelectionTool ? k_SelectionToolTipText : k_MainMenuTipText) : toolType.Name;
				}

				m_GradientButton.highlighted = m_previewToolType != null;
			}
		}

		public Transform alternateMenuOrigin
		{
			get { return m_AlternateMenuOrigin; }
			set
			{
				if (m_AlternateMenuOrigin == value)
					return;

				m_AlternateMenuOrigin = value;
				transform.SetParent(m_AlternateMenuOrigin);
				transform.localPosition = Vector3.zero;
				transform.localRotation = Quaternion.identity;
			}
		}

		[SerializeField]
		GradientButton m_GradientButton;

		[SerializeField]
		SmoothMotion m_SmoothMotion;

		[SerializeField]
		PinnedToolActionButton m_LeftPinnedToolActionButton;

		[SerializeField]
		PinnedToolActionButton m_RightPinnedToolActionButton;

		[SerializeField]
		Transform m_ContentContainer;

		[SerializeField]
		Collider m_RootCollider;

		[SerializeField]
		MeshRenderer m_FrameRenderer;

		[SerializeField]
		MeshRenderer m_InsetMeshRenderer;

		[SerializeField]
		Transform m_TooltipTarget;

		[SerializeField]
		Transform m_TooltipSource;

		[SerializeField]
		Vector3 m_AlternateLocalPosition;

		Coroutine m_PositionCoroutine;
		Coroutine m_VisibilityCoroutine;
		Coroutine m_HighlightCoroutine;
		Coroutine m_ActivatorMoveCoroutine;

		string m_TooltipText;
		bool m_Highlighted;
		bool m_MoveToAlternatePosition;
		int m_Order;
		Type m_previewToolType;
		Type m_ToolType;
		GradientPair m_GradientPair;
		Transform m_AlternateMenuOrigin;
		Material m_FrameMaterial;
		Material m_InsetMaterial;
		//Vector3 m_InactivePosition; // Inactive button offset from the main menu activator
		Vector3 m_OriginalLocalPosition;
		Vector3 m_OriginalLocalScale;

		public string tooltipText { get { return tooltip != null ? tooltip.tooltipText : m_TooltipText; } set { m_TooltipText = value; } }
		public Transform tooltipTarget { get { return m_TooltipTarget; } }
		public Transform tooltipSource { get { return m_TooltipSource; } }
		public TextAlignment tooltipAlignment { get; private set; }
		public Transform rayOrigin { get; set; }
		public Node node { get; set; }
		public ITooltip tooltip { private get; set; } // Overrides text
		public Action<ITooltip> showTooltip { private get; set; }
		public Action<ITooltip> hideTooltip { private get; set; }
		public GradientPair customToolTipHighlightColor { get; set; }
		public bool isSelectionTool { get { return m_ToolType != null && m_ToolType == typeof(Tools.SelectionTool); } }
		public bool isMainMenu { get { return m_ToolType != null && m_ToolType == typeof(IMainMenu); } }
		public Action<Transform, PinnedToolButton> deletePinnedToolButton { get; set; }
		public int activeButtonCount { get; set; }
		public Transform menuOrigin { get; set; }
		public Action<Transform, bool> highlightAllToolButtons { get; set; }
		public Sprite icon { set { m_GradientButton.icon = value; } }
		public Action<Transform, Transform> OpenMenu { get; set; }
		public Action<Transform, Type> selectTool { get; set; }
		public int menuButtonOrderPosition { get { return k_MenuButtonOrderPosition; } }
		public int activeToolOrderPosition { get { return k_ActiveToolOrderPosition; } }
		public Vector3 toolButtonActivePosition { get { return k_ToolButtonActivePosition; } } // Shared active button offset from the alternate menu

		public event Action<Transform> hoverEnter;
		public event Action<Transform> hoverExit;
		public event Action<Transform> selected;

		private bool activeTool
		{
			get { return m_Order == activeToolOrderPosition; }
			set
			{
				m_GradientButton.normalGradientPair = value ? gradientPair : UnityBrandColorScheme.grayscaleSessionGradient;
				m_GradientButton.highlightGradientPair = value ? UnityBrandColorScheme.grayscaleSessionGradient : gradientPair;
				m_GradientButton.invertHighlightScale = value;
				m_GradientButton.highlighted = true;
				m_GradientButton.highlighted = false;
			}
		}

		public bool highlighted
		{
			set
			{
				//if (m_Highlighted == value || !gameObject.activeSelf)
					//return;

				this.RestartCoroutine(ref m_HighlightCoroutine, AnimateSemiTransparent(!value));
			}

			//get { return m_Highlighted; }
		}

		public bool moveToAlternatePosition
		{
			get { return m_MoveToAlternatePosition; }
			set
			{
				if (m_MoveToAlternatePosition == value)
					return;

				m_MoveToAlternatePosition = value;

				this.StopCoroutine(ref m_ActivatorMoveCoroutine);

				m_ActivatorMoveCoroutine = StartCoroutine(AnimateMoveActivatorButton(m_MoveToAlternatePosition));
			}
		}

		void Awake()
		{
			m_OriginalLocalPosition = transform.localPosition;
			m_OriginalLocalScale = transform.localScale;
		}

		void Start()
		{
			//m_GradientButton.onClick += ButtonClicked; // TODO remove after action button refactor

			Debug.LogWarning("Hide (L+R) pinned tool action buttons if button is the main menu button Hide select action button if button is in the first position (next to menu button)");

			transform.parent = alternateMenuOrigin;

			if (m_ToolType == null)
			{
				//transform.localPosition = m_InactivePosition;
				m_GradientButton.gameObject.SetActive(false);
			}
			else
			{
				//transform.localPosition = activePosition;
			}

			var tooltipSourcePosition = new Vector3(node == Node.LeftHand ? -0.01267f : 0.01267f, tooltipSource.localPosition.y, 0);
			var tooltipXOffset = node == Node.LeftHand ? -0.05f : 0.05f;
			tooltipSource.localPosition = tooltipSourcePosition;
			tooltipAlignment = node == Node.LeftHand ? TextAlignment.Right : TextAlignment.Left;
			m_TooltipTarget.localPosition = new Vector3(tooltipXOffset, tooltipSourcePosition.y, tooltipSourcePosition.z);
			this.ConnectInterfaces(m_SmoothMotion);

			m_FrameMaterial = MaterialUtils.GetMaterialClone(m_FrameRenderer);
			var frameMaterialColor = m_FrameMaterial.color;
			s_FrameOpaqueColor = new Color(frameMaterialColor.r, frameMaterialColor.g, frameMaterialColor.b, 1f);
			s_SemiTransparentFrameColor = new Color(s_FrameOpaqueColor.r, s_FrameOpaqueColor.g, s_FrameOpaqueColor.b, 0.5f);
			m_FrameMaterial.SetColor(k_MaterialColorProperty, s_SemiTransparentFrameColor);

			m_InsetMaterial = MaterialUtils.GetMaterialClone(m_InsetMeshRenderer);
			//m_InsetMaterial.SetFloat(k_MaterialAlphaProperty, 0f);

			m_GradientButton.hoverEnter += OnBackgroundHoverEnter; // Display the foreground button actions
			m_GradientButton.hoverExit += OnActionButtonHoverExit;
			m_GradientButton.click += OnBackgroundButtonClick;

			m_LeftPinnedToolActionButton.clicked = ActionButtonClicked;
			m_LeftPinnedToolActionButton.hoverEnter = HoverButton;
			m_LeftPinnedToolActionButton.hoverExit = OnActionButtonHoverExit;
			m_RightPinnedToolActionButton.clicked = ActionButtonClicked;
			m_RightPinnedToolActionButton.hoverEnter = HoverButton;
			m_RightPinnedToolActionButton.hoverExit = OnActionButtonHoverExit;

			// Assign the select action button to the side closest to the opposite hand, that allows the arrow to also point in the direction the
			var leftHand = node == Node.LeftHand;
			m_RightPinnedToolActionButton.buttonType = leftHand ? PinnedToolActionButton.ButtonType.SelectTool : PinnedToolActionButton.ButtonType.Close;
			m_LeftPinnedToolActionButton.buttonType = leftHand ? PinnedToolActionButton.ButtonType.Close : PinnedToolActionButton.ButtonType.SelectTool;

			m_RightPinnedToolActionButton.rotateIcon = leftHand ? false : true;
			m_LeftPinnedToolActionButton.rotateIcon = leftHand ? false : true;

			m_LeftPinnedToolActionButton.visible = false;
			m_RightPinnedToolActionButton.visible = false;

			m_LeftPinnedToolActionButton.mainButtonCollider = m_RootCollider;
			m_RightPinnedToolActionButton.mainButtonCollider = m_RootCollider;

			//m_ButtonCollider.enabled = true;
			//m_GradientButton.click += OnClick;
			//m_GradientButton.gameObject.SetActive(false);
		}

		// Create periodic table-style names for types
		string GetTypeAbbreviation(Type type)
		{
			var abbreviation = new StringBuilder();
			foreach (var ch in type.Name.ToCharArray())
			{
				if (char.IsUpper(ch))
					abbreviation.Append(abbreviation.Length > 0 ? char.ToLower(ch) : ch);

				if (abbreviation.Length >= 2)
					break;
			}

			return abbreviation.ToString();
		}

		IEnumerator AnimatePosition(Quaternion targetRotation)
		{
			var duration = 0f;
			//var currentPosition = transform.localPosition;
			//var targetPosition = activeTool ? activePosition : m_InactivePosition;
			var currentRotation = transform.localRotation;
			while (duration < 1)
			{
				duration += Time.unscaledDeltaTime * 6;
				var durationShaped = Mathf.Pow(MathUtilsExt.SmoothInOutLerpFloat(duration), 3);
				transform.localRotation = Quaternion.Lerp(currentRotation, targetRotation, durationShaped);
				CorrectIconRotation();
				//transform.localPosition = Vector3.Lerp(currentPosition, targetPosition, durationShaped);
				yield return null;
			}

			//transform.localPosition = targetPosition;
			transform.localRotation = targetRotation;
			CorrectIconRotation();
			m_PositionCoroutine = null;
		}

		bool IsSelectToolButton (PinnedToolActionButton.ButtonType buttonType)
		{
			return buttonType == PinnedToolActionButton.ButtonType.SelectTool;
		}

		void OnBackgroundHoverEnter ()
		{
			//if (!m_LeftPinnedToolActionButton.highlighted && !m_RightPinnedToolActionButton.highlighted)
			//{
				Debug.LogWarning("<color=green>Background button was hovered, now triggereing the foreground action button visuals</color>");
				//m_RootCollider.enabled = false;
				m_GradientButton.highlighted = true;
				//m_GradientButton.visible = false;

				//Debug.LogWarning("Handle for disabled buttons not being shown, ie the promotote(green) button on the first/selected tool");

			HoverButton();
			//m_ButtonCollider.enabled = false;
			//}
		}

		void HoverButton()
		{
			if (order < 2 && (isSelectionTool || isMainMenu)) // The main menu and the active tool occupy orders 0 and 1; don't show any action buttons for buttons in either position
			{
				m_RightPinnedToolActionButton.visible = false;
				m_LeftPinnedToolActionButton.visible = false;
				//m_RootCollider.enabled = true;
				StartCoroutine(DelayedCollderEnable());
			}
			else if (isSelectionTool)
			{
				/*
				if (activeTool)
				{
					m_RightPinnedToolActionButton.visible = false;
					m_LeftPinnedToolActionButton.visible = false;
					StartCoroutine(DelayedCollderEnable());
				}
				else
				*/
				{
					m_RightPinnedToolActionButton.visible = IsSelectToolButton(m_RightPinnedToolActionButton.buttonType) ? true : false;
					m_LeftPinnedToolActionButton.visible = IsSelectToolButton(m_LeftPinnedToolActionButton.buttonType) ? true : false;
				}
			} else
			{
				// Hide the select action button if this tool button is already the selected tool, else show the close button for inactive tools
				m_RightPinnedToolActionButton.visible = IsSelectToolButton(m_RightPinnedToolActionButton.buttonType) ? !activeTool : true;
				m_LeftPinnedToolActionButton.visible = IsSelectToolButton(m_LeftPinnedToolActionButton.buttonType) ? !activeTool : true;
			}

			highlightAllToolButtons(rayOrigin, true);
		}

		void ActionButtonClicked(PinnedToolActionButton button)
		{
			Debug.LogError("Action Button selectTool!");
			if (order > menuButtonOrderPosition)
			{
				// TODO: SELECT ACTION BUTTONS should be able to be interacted with due to their being hidden, so no need to handle for that case
				// Buttons in the activeToolOrderPosition cannot be selected when selectTool
				if (button.buttonType == PinnedToolActionButton.ButtonType.SelectTool && order > activeToolOrderPosition)
				{
					selectTool(rayOrigin, m_ToolType); // ButtonClicked will set button order to 0
					activeTool = activeTool;
					//SetButtonGradients(this.ButtonClicked(rayOrigin, m_ToolType));
				}
				else // Handle action buttons assigned Close-Tool functionality
				{
					//if (!isSelectionTool)
						this.RestartCoroutine(ref m_VisibilityCoroutine, AnimateClose());
					//else
						//Debug.LogError("<color=red>CANNOT DELETE THE SELECT TOOL!!!!!</color>");

					deletePinnedToolButton(rayOrigin, this);
				}

				OnActionButtonHoverExit();
				m_LeftPinnedToolActionButton.highlighted = false;
				m_RightPinnedToolActionButton.highlighted = false;
			}
		}

		void OnActionButtonHoverExit()
		{
			Debug.LogWarning("<color=orange>OnActionButtonHoverExit : </color>" + name + " : " + toolType);
			// in this case display the hover state for the gradient button, then enable visibility for each of the action buttons

			// Hide both action buttons if the user is no longer hovering over the button
			if (!m_LeftPinnedToolActionButton.highlighted && !m_RightPinnedToolActionButton.highlighted)
			{
				Debug.LogWarning("<color=green>!!!</color>");
				//m_ButtonCollider.enabled = true;
				m_LeftPinnedToolActionButton.visible = false;
				m_RightPinnedToolActionButton.visible = false;
				//m_GradientButton.visible = true;
				m_GradientButton.highlighted = false;
				highlightAllToolButtons(rayOrigin, false);
			}

			m_GradientButton.UpdateMaterialColors();
		}

		void OnBackgroundButtonClick()
		{
			Debug.LogWarning("<color=orange>OnBackgroundButtonClick : </color>" + name + " : " + toolType);
			// in this case display the hover state for the gradient button, then enable visibility for each of the action buttons

			// Hide both action buttons if the user is no longer hovering over the button
			if (!m_LeftPinnedToolActionButton.highlighted && !m_RightPinnedToolActionButton.highlighted)
			{
				selectTool(rayOrigin, m_ToolType); // Perform clik for a ToolButton that doesn't utilize ToolActionButtons
			}

			m_GradientButton.UpdateMaterialColors();
		}

		void CloseButton()
		{
			// TODO add full close functionality
			gameObject.SetActive(false);

			// perform a graceful hiding of visuals, then destroy this button gameobject
		}

		IEnumerator AnimateClose()
		{
			this.HideTooltip(this);
			m_RootCollider.enabled = false;
			var duration = 0f;
			var currentScale = transform.localScale;
			var targetScale = Vector3.zero;
			while (duration < 1)
			{
				duration += Time.unscaledDeltaTime * 3f;
				var durationShaped = Mathf.Pow(MathUtilsExt.SmoothInOutLerpFloat(duration), 4);
				transform.localScale = Vector3.Lerp(currentScale, targetScale, durationShaped);
				yield return null;
			}

			transform.localScale = targetScale;
			m_VisibilityCoroutine = null;
			ObjectUtils.Destroy(gameObject, 0.1f);
		}

		void CorrectIconRotation()
		{
			const float kIconLookForwardOffset = 0.5f;
			var iconLookDirection = m_ContentContainer.transform.position + transform.parent.forward * kIconLookForwardOffset; // set a position offset above the icon, regardless of the icon's rotation
			m_ContentContainer.LookAt(iconLookDirection);
			m_ContentContainer.localEulerAngles = new Vector3(0f, 0f, m_ContentContainer.localEulerAngles.z);
			var angle = m_ContentContainer.localEulerAngles.z;
			m_TooltipTarget.localEulerAngles = new Vector3(90f, 0f, angle);

			var yaw = transform.localRotation.eulerAngles.y;
			tooltipAlignment = yaw > 90 && yaw <= 270 ? TextAlignment.Right : TextAlignment.Left;
		}

		IEnumerator AnimateSemiTransparent(bool makeSemiTransparent)
		{
			Debug.LogWarning("<color=blue>AnimateSemiTransparent : </color>" + makeSemiTransparent);
			const float kFasterMotionMultiplier = 2f;
			var transitionAmount = Time.unscaledDeltaTime;
			var positionWait = (order + 1) * 0.25f; // pad the order index for a faster start to the transition
			//var semiTransparentTargetScale = new Vector3(0.9f, 0.15f, 0.9f);
			var currentFrameColor = m_FrameMaterial.color;
			var transparentFrameColor = new Color (s_FrameOpaqueColor.r, s_FrameOpaqueColor.g, s_FrameOpaqueColor.b, 0f);
			var targetFrameColor = makeSemiTransparent ? s_SemiTransparentFrameColor : s_FrameOpaqueColor;
			var currentInsetAlpha = m_InsetMaterial.GetFloat(k_MaterialAlphaProperty);
			var targetInsetAlpha = makeSemiTransparent ? 0.25f : 1f;
			//var currentIconColor = m_IconMaterial.GetColor(k_MaterialColorProperty);
			//var targetIconColor = makeSemiTransparent ? s_SemiTransparentFrameColor : Color.white;
			//var currentInsetScale = m_MenuInset.localScale;
			//var targetInsetScale = makeSemiTransparent ? m_HighlightedInsetLocalScale * 4 : m_VisibleInsetLocalScale;
			//var currentIconScale = m_IconContainer.localScale;
			//var semiTransparentTargetIconScale = Vector3.one * 1.5f;
			//var targetIconScale = makeSemiTransparent ? semiTransparentTargetIconScale : Vector3.one;
			while (transitionAmount < 1)
			{
				m_FrameMaterial.SetColor(k_MaterialColorProperty, Color.Lerp(currentFrameColor, transparentFrameColor, transitionAmount));
				//m_MenuInset.localScale = Vector3.Lerp(currentInsetScale, targetInsetScale, transitionAmount * 2f);
				//m_InsetMaterial.SetFloat(k_MaterialAlphaProperty, Mathf.Lerp(currentInsetAlpha, targetInsetAlpha, transitionAmount));
				//m_IconMaterial.SetColor(k_MaterialColorProperty, Color.Lerp(currentIconColor, targetIconColor, transitionAmount));
				//var shapedTransitionAmount = Mathf.Pow(transitionAmount, makeSemiTransparent ? 2 : 1) * kFasterMotionMultiplier;
				//m_IconContainer.localScale = Vector3.Lerp(currentIconScale, targetIconScale, shapedTransitionAmount);
				transitionAmount += Time.unscaledDeltaTime * 4f;
				CorrectIconRotation();
				yield return null;
			}

			m_FrameMaterial.SetColor(k_MaterialColorProperty, targetFrameColor);
			//m_InsetMaterial.SetFloat(k_MaterialAlphaProperty, targetInsetAlpha);
			//m_IconMaterial.SetColor(k_MaterialColorProperty, targetIconColor);
			//m_MenuInset.localScale = targetInsetScale;
			//m_IconContainer.localScale = targetIconScale;
		}

		IEnumerator AnimateMoveActivatorButton(bool moveToAlternatePosition = true)
		{
			var amount = 0f;
			var currentPosition = transform.localPosition;
			var targetPosition = moveToAlternatePosition ? m_AlternateLocalPosition : m_OriginalLocalPosition;
			var currentLocalScale = transform.localScale;
			var targetLocalScale = moveToAlternatePosition ? m_OriginalLocalScale : m_OriginalLocalScale * k_alternateLocalScaleMultiplier;
			var speed = moveToAlternatePosition ? 5 : 5; // perform faster is returning to original position

			while (amount < 1f)
			{
				amount += Time.unscaledDeltaTime * speed;
				var shapedAmount = MathUtilsExt.SmoothInOutLerpFloat(amount);
				transform.localPosition = Vector3.Lerp(currentPosition, targetPosition, shapedAmount);
				transform.localScale = Vector3.Lerp(currentLocalScale, targetLocalScale, shapedAmount);
				yield return null;
			}

			transform.localPosition = targetPosition;
			transform.localScale = targetLocalScale;
			m_ActivatorMoveCoroutine = null;
		}

		IEnumerator DelayedCollderEnable()
		{
			yield return null;
			m_RootCollider.enabled = true;
		}
	}
}
#endif
