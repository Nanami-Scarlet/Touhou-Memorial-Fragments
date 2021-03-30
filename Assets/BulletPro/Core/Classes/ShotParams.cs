using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// This script is part of the BulletPro package for Unity.
// Author : Simon Albou <albou.simon@gmail.com>

namespace BulletPro
{
	[System.Serializable]
	public class ShotParams : EmissionParams
	{
		// Main stats
		public DynamicBullet bulletParams;
		public DynamicInt simultaneousBulletsPerFrame;
		public BulletSpawn[] bulletSpawns;

		// Layouting
		public List<ShotModifier> modifiers;

		// Randomize bullet params // TODO : delete this
		public bool bulletParamsRandom;
		public BulletParams[] listOfPossibleBulletParams;
		public int possibleBulletParams;

		// Currently unused. Could be used in order to avoid recalculating values at runtime (stores min/max x/y spawn positions)
		public Vector3 highestValues, lowestValues;

		// Serialized vars that only help in inspector
		#if UNITY_EDITOR

		public bool hasBeenSerializedOnce;

		public bool foldout0, foldout1, foldout2, foldout3, foldout4, foldout5, foldout6, foldout7, foldout8;

		[Range(1, 100)]
		public int textureSliceRate;
		public Vector2 ingameSizeOfTexture;
		public Texture2D inputTexture;
		public BulletOutputTexture outputTexture;

		public override void FirstInitialization()
		{
			hasBeenSerializedOnce = true;

			SetUniqueIndex();
			
			simultaneousBulletsPerFrame = new DynamicInt(3);
			simultaneousBulletsPerFrame.SetButtons(new int[] { -5, -1, 1, 5 });
			bulletSpawns = new BulletSpawn[3];
			textureSliceRate = 20;
			ingameSizeOfTexture = Vector2.one;
			bulletParams = new DynamicBullet(null);
			modifiers = new List<ShotModifier>();

			ShotModifier sampleMod = new ShotModifier();
			sampleMod.modifierType = ShotModifierType.SpreadBullets;
			sampleMod.Initialize();
			sampleMod.degrees = new DynamicFloat(30f);
			sampleMod.enabled = true;
			modifiers.Add(sampleMod);

			// Uncomment for a second modifier in this default config
			/* *
			ShotModifier sampleMod2 = new ShotModifier();
			sampleMod2.modifierType = ShotModifierType.LocalTranslation;
			sampleMod2.Initialize();
			sampleMod2.localMovement = new DynamicVector2(Vector2.up*0.1f);
			sampleMod2.enabled = true;
			modifiers.Add(sampleMod2);
			/* */

			// init output texture
			outputTexture.tex = new Texture2D(256, 256);
			outputTexture.widthInWindow = 0.5f;
		}
		#endif

	}

	#region helper structs and enums

	// Having a struct avoids manipulating a shitload of arrays for each bullet in the shot
	[System.Serializable]
	public struct BulletSpawn
	{
		public int index;
		public Vector3 bulletOrigin;
		public bool useDifferentBullet;
		public BulletParams bulletParams;
	}

	// An enum that stores every layout modifier type for ShotParams
	[System.Serializable]
	public enum ShotModifierType
	{
		SpreadBullets,
		SpreadBulletsTotal,
		GlobalTranslation,
		LocalTranslation,
		Rotation,
		SetPivot,
		RotateAroundPivot,
		ScaleLayout,
		ResetCoordinates,
		HorizontalSpacing,
		HorizontalSpacingTotal,
		VerticalSpacing,
		VerticalSpacingTotal,
		FlipOrientation,
		LookAtPoint,
		LookAwayFromPoint,
		OnlySomeBullets,
		SetBulletParams
	}

	[System.Serializable]
	public enum BulletSortMode { X, Y, Z, TwoDimensional, Random }
	[System.Serializable]
	public enum BulletSortDirection { Ascending, Descending }

	// stacks of ShotModifiers will allow easier edition of ShotParams layout
	[System.Serializable]
	public struct ShotModifier
	{
		// What will this modifier do ?
		public ShotModifierType modifierType;

		// Corresponding values depending on modifier type
		public DynamicVector2 globalMovement, localMovement, pointToLookAt, pointToFleeFrom, pivot, scale;
		public DynamicBool resetX, resetY, resetZ, flipX, flipY;
		public DynamicFloat degrees, degreesTotal, rotationDegrees, horizontalSpacing, xSpacingTotal, verticalSpacing, ySpacingTotal, layoutDegrees;
		public DynamicBullet bulletParams;
		public int numberOfModifiersAffected; // for selection
		public Rect[] selectionRects;
		public int[] indexesOfBulletsAffected; // keep track of which bullets have been selected by selection rects
#if UNITY_EDITOR
		public Color selectionColor, pivotColor;
		public bool isEditingSelection;
		public bool selectionRectsVisible;
#endif
		// Store the output spawn position/rotation vectors of the whole shot
		public BulletSpawn[] postEffectBulletSpawns;

		// Offering to disable each modifier makes the whole system more flexible
		public bool enabled;

		public void Initialize()
		{
			degreesTotal = new DynamicFloat(0f);
			rotationDegrees = new DynamicFloat(0f);
			horizontalSpacing = new DynamicFloat(0f);
			verticalSpacing = new DynamicFloat(0f);
			xSpacingTotal = new DynamicFloat(0f);
			ySpacingTotal = new DynamicFloat(0f);
			resetX = new DynamicBool(false);
			resetY = new DynamicBool(false);
			resetZ = new DynamicBool(false);
			flipX = new DynamicBool(false);
			flipY = new DynamicBool(false);
			globalMovement = new DynamicVector2(Vector2.zero);
			localMovement = new DynamicVector2(Vector2.zero);
			pointToLookAt = new DynamicVector2(Vector2.zero);
			pointToFleeFrom = new DynamicVector2(Vector2.zero);
			pivot = new DynamicVector2(Vector2.zero);
			scale = new DynamicVector2(Vector2.one);
			bulletParams = new DynamicBullet(null);

#if UNITY_EDITOR
			pivotColor = Color.blue;
#endif
		}
	}

	[System.Serializable]
	public enum SeparationCalcType { PerBullet, Total }

#if UNITY_EDITOR
	// Used for displaying output Texture in ShotParams inspector
	[System.Serializable]
	public struct BulletOutputTexture
	{
		public Texture2D tex;
		public float widthInWindow;
		public float ingameDistance;
		public ShotTextureOrientation orientation;
		public bool justChangedOrientation;
		
		public bool hasCompactEditor;

		// Interactions stored here :
		
		public bool hasMouseDown; // returns whether mouse button has been pressed in rectangle
		public bool holdsMultiSelect; // returns whether "multiple selection rects" key is being pressed
		public bool isEditingSelection; // returns whether we're editing an existing selection (false means we create a new one)
		public bool isDoneWithSelection; // flip to true whenever user has finished drawing the selection
		public Rect[] selectionRects;
		public SelectionRectsFromModifier[] selectionRectsFromExistingMods; // stores selection rects made in the past, and saved in modifiers
		public Vector2 mouseStartPoint, mouseEndPoint; // used while making up a selection rect
		public Color currentColor; // current selection uses a different, randomized color to avoid confusion
		public int indexOfFirstUndo; // collapsing all substeps of a selection into a single undo
	}

	public enum ShotTextureOrientation { Up, Down, Left, Right }

	// Serializes selections that are saved in layout modifiers
	[System.Serializable]
	public struct SelectionRectsFromModifier
	{
		public Color color;
		public Rect[] selectionRects;
	} 
#endif

	#endregion

}