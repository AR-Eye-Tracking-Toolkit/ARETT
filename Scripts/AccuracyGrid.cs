// Copyright (c) Sebastian Kapp.
// Licensed under the MIT License.

using UnityEngine;

namespace ARETT
{
	/// <summary>
	/// Class which controls the visibility of the accuracy grid
	/// </summary>
	public class AccuracyGrid : MonoBehaviour
	{
		/// <summary>
		/// GameObject which contains the grid for 0.5m distance
		/// </summary>
		[SerializeField]
		private GameObject grid05;

		/// <summary>
		/// GameObject which contains the grid for 1.0m distance
		/// </summary>
		[SerializeField]
		private GameObject grid10;

		/// <summary>
		/// GameObject which contains the grid for 2.0m distance
		/// </summary>
		[SerializeField]
		private GameObject grid20;

		/// <summary>
		/// GameObject which contains the grid for 4.0m distance
		/// </summary>
		[SerializeField]
		private GameObject grid40;

		/// <summary>
		/// Main camera of the scene (cached on Awake)
		/// </summary>
		private Camera mainCamera;

		/// <summary>
		/// Layer on which the accuracy grid "lives"
		/// </summary>
		[Tooltip("Unity Layer on which the accuracy grid \"lives\" and to which the camera is limited to while displaying it")]
		public string gridLayer = "EyeTrackingAccuracy";

		/// <summary>
		/// Layer Mask of the main camera before showing the grid
		/// </summary>
		private int oldLayerMask;

		/// <summary>
		/// Distance to which the grid is currently being set.
		/// Valid distances: 05 (0.5m), 10 (1.0m), 20 (2.0m), 40 (4.0m)
		/// </summary>
		public int currentGridDistance { get; private set; } = 10;

		/// <summary>
		/// Flag if the grid is currently visible
		/// </summary>
		public bool gridVisible = false;

		/// <summary>
		/// On Awake cache the main camera and attach the game object to it so we have fixed the position of the grid in front of the wearer
		/// </summary>
		private void Awake()
		{
			// Cache main Camera
			mainCamera = Camera.main;

			// Attach the grids to the camera
			transform.parent = mainCamera.transform;

			// Make sure the grids are hidden on startup
			grid05.SetActive(false);
			grid10.SetActive(false);
			grid20.SetActive(false);
			grid40.SetActive(false);
		}

		/// <summary>
		/// Change the distance in which the accuracy grid is displayed
		/// </summary>
		/// <param name="distance">Distance in which the grid should be displayed. Valid values: 05 (0.5m), 10 (1.0m), 20 (2.0m), 40 (4.0m)</param>
		public void ChangeGridDistance(int distance)
		{
			// Make sure we have a valid distance
			if (distance != 05 && distance != 10 && distance != 20 && distance != 40) throw new System.ArgumentException($"Distance {distance} not valid! Valid distances: 05, 10, 20, 40");

			// If the grid is already displayed in the specified distance do nothing
			if (gridVisible && currentGridDistance == distance)
			{
				Debug.Log("[EyeTracking Accuracy] Accuracy grid already visible at the requested distance.");
				return;
			}

			// If the grid is currently visible change the displayed distance
			if (gridVisible)
			{
				grid05.SetActive(distance == 05);
				grid10.SetActive(distance == 10);
				grid20.SetActive(distance == 20);
				grid40.SetActive(distance == 40);
			}

			// Update the distance
			currentGridDistance = distance;
		}

		/// <summary>
		/// Show the accuracy grid
		/// </summary>
		public void ShowGrid()
		{
			// If don't already visible update the camera culling mask to only show the grid
			if (!gridVisible)
			{
				// Save the old layer mask for the main camera
				oldLayerMask = mainCamera.cullingMask;

				// Set the camera culling mask to only show the accuracy grid
				mainCamera.cullingMask = LayerMask.GetMask(gridLayer);
			}

			// Enable the grid which was selected
			grid05.SetActive(currentGridDistance == 05);
			grid10.SetActive(currentGridDistance == 10);
			grid20.SetActive(currentGridDistance == 20);
			grid40.SetActive(currentGridDistance == 40);

			// Save visibility
			gridVisible = true;
		}

		/// <summary>
		/// Hide the accuracy grid
		/// </summary>
		public void HideGrid()
		{
			// Make sure we don't already show the grid
			if (!gridVisible)
			{
				Debug.LogError("[EyeTracking Accuracy] Accuracy grid wasn't visible, can't hide it!");
				return;
			}

			// Restore the old layer mask of the main camera
			mainCamera.cullingMask = oldLayerMask;

			// Disable all grids
			grid05.SetActive(false);
			grid10.SetActive(false);
			grid20.SetActive(false);
			grid40.SetActive(false);

			// Save visibility
			gridVisible = false;
		}
	}
}
