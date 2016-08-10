﻿using UnityEngine;
using UnityEngine.VR.Utilities;

public class ChessboardWorkspace : Workspace
{
	private const float kGridScale = 10f; //Scale grid cells because workspace is <1m wide by default
	//HACK: dummy input for bounds change
	[SerializeField]
	private Renderer m_DummyBounds;

	//NOTE: since pretty much all workspaces will want a prefab, should this go in the base class?
	[SerializeField]
	private GameObject m_ContentPrefab;

	private MiniWorld m_MiniWorld;
	private ChessboardUI m_ChessboardUI;
	private Material m_GridMaterial;

	public override void Setup()
	{
		base.Setup();
		GameObject content = U.Object.ClonePrefab(m_ContentPrefab, handle.sceneContainer);
		content.transform.localPosition = Vector3.zero;
		content.transform.localRotation = Quaternion.identity;
		content.transform.localScale = Vector3.one;
		m_MiniWorld = GetComponentInChildren<MiniWorld>();
		m_ChessboardUI = GetComponentInChildren<ChessboardUI>();
		m_GridMaterial = m_ChessboardUI.grid.sharedMaterial;
		OnBoundsChanged();
	}

	void Update()
	{
		if (m_DummyBounds)
			contentBounds = m_DummyBounds.bounds;

		if (m_MiniWorld.clipBox.transform.hasChanged)
		{
			float clipHeight = m_MiniWorld.clipBox.transform.position.y / m_MiniWorld.clipBox.transform.localScale.y;
			if (Mathf.Abs(clipHeight) < contentBounds.extents.y)
			{
				m_ChessboardUI.grid.gameObject.SetActive(true);
				m_ChessboardUI.grid.transform.localPosition = Vector3.down * clipHeight;
			}
			else
			{
				m_ChessboardUI.grid.gameObject.SetActive(false);
			}

			//Update grid material if ClipBox has moved
			m_GridMaterial.mainTextureScale = new Vector2(
				contentBounds.size.x * m_MiniWorld.clipBox.transform.localScale.x,
				contentBounds.size.z * m_MiniWorld.clipBox.transform.localScale.x) * kGridScale;
			m_GridMaterial.mainTextureOffset =
				Vector2.one * 0.5f //Center grid
				+ new Vector2(m_GridMaterial.mainTextureScale.x % 2, m_GridMaterial.mainTextureScale.y % 2) * -0.5f //Scaling offset
				+ new Vector2(m_MiniWorld.clipBox.transform.position.x, m_MiniWorld.clipBox.transform.position.z) * kGridScale; //Translation offset
		}
		m_MiniWorld.clipBox.transform.hasChanged = false;
	}

	protected override void OnBoundsChanged()
	{
		m_MiniWorld.transform.localPosition = Vector3.up * contentBounds.extents.y;
		m_MiniWorld.SetBounds(contentBounds);
		m_ChessboardUI.grid.transform.localScale = contentBounds.size;
	}
}
