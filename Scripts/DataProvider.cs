// Copyright (c) Sebastian Kapp.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using UnityEngine;

namespace ARETT
{
	/// <summary>
	/// The data provider starts the device specific data access which periodically checks for new eye tracking data and invokes an event whenever new data is available
	/// </summary>
	public class DataProvider : MonoBehaviour
	{
		#region Configuration
		[Header("Configuration")]
		/// <summary>
		/// Time in milliseconds at what interval we should check for new data
		/// Note: According to the HL2 documentation we get data in 30Hz which means every 33.33 milliseconds.
		///       If we check every 10 ms we should get data about every third time and we also shouldn't miss any data
		/// </summary>
		[Tooltip("Time in milliseconds at what interval we should check for new data")]
		public int fetchDataSleepMs = 10;

		/// <summary>
		/// Collision layers which are evaluated as AOIs for EyeTracking
		/// </summary>
		[Tooltip("Collision layers which are evaluated as AOIs for EyeTracking")]
		public string[] eyeTrackingAOILayers = new string[] { "EyeTracking" };

		/// <summary>
		/// Collision layers which are used for visualization of eye tracking data and should be excluded when searching for the gaze point
		/// </summary>
		[Tooltip("Collision layers which are used for visualization of eye tracking data and should be excluded when searching for the gaze point")]
		public string[] eyeTrackingVisLayers = new string[] { "EyeTrackingVis" };

		/// <summary>
		/// Visualization layers which show a visual indication to check the position of the AOIs
		/// </summary>
		[Tooltip("Visualization layers which show a visual indication to check the position of the AOIs")]
		public string[] eyeTrackingCheckLayers = new string[] { "EyeTrackingCheck" };

		/// <summary>
		/// Are the AOI check layers currently visible?
		/// </summary>
		public bool eyeTrackingCheckLayersVisible { get; private set; } = false;

		/// <summary>
		/// Layer Mask which filters the AOI detection to the relevant Layers
		/// </summary>
		private LayerMask eyeTrackingAOILayerMask;

		/// <summary>
		/// Layer Mask which contains every layer, except for the layer(s) specified for eye tracking use (AOIs and visualizations)
		/// </summary>
		private LayerMask notEyeTrackingLayerMask;

		/// <summary>
		/// GameObject which visualizes the current gaze point in the mixed reality capture
		/// </summary>
		[Tooltip("GameObject which visualizes the current gaze point in the mixed reality capture")]
		[SerializeField]
		private GameObject GazePointVis;

		/// <summary>
		/// Unity Camera which mimics the real web cam on the device and is used to identify the gaze coordinates on the camera image
		/// </summary>
		[Tooltip("Unity Camera which mimics the real web cam on the device and is used to identify the gaze coordinates on the camera image")]
		[SerializeField]
		private Camera webcamCamera;

		#endregion Configuration


		#region Position logged game objects

		/// <summary>
		/// Array of GameObjects which can be set in the Unity Editor and contains GameObjects which should always be logged
		/// Note: This array will be transferred to the list of logged GameObjects on awake but not during runtime.
		/// </summary>
		[Header("GameObjects to Log")]
		[SerializeField]
		[Tooltip("List of GameObjects whose position should be always logged during recording (only set in Editor)")]
		private GameObject[] AlwaysPositionLoggedGameObjects = new GameObject[0];

		/// <summary>
		/// List of GameObjects whose position should be logged during recording
		/// Note: This list is initialized on awake with the AlwaysPositionLoggedGameObjects set in the editor but can be changed during runtime
		/// </summary>
		public ObservableCollection<GameObject> PositionLoggedGameObjects = new ObservableCollection<GameObject>();

		/// <summary>
		/// Array which contains the names of all game objects of which we are currently logging the position
		/// Note: This array is updated whenever the list of logged objects changes
		/// </summary>
		public string[] PositionLoggedGameObjectNames { get; private set; } = new string[0];

		/// <summary>
		/// Flag whether the list of game object names will be updated in the next update
		/// Note: This is a "safety" so we only update once even when we add multiple game objects to the list
		/// </summary>
		private bool PositionLoggedGameObjectNamesWillBeUpdated = false;

		#endregion Position logged game objects



		#region Editor options

		[Header("Editor Options")]
		/// <summary>
		/// Debug flag to start fetching simulated gaze data when we are in the Unity editor
		/// </summary>
		[Tooltip("Debug flag to start fetching simulated gaze data when we are in the Unity editor")]
		public bool SimulateAvailablility = true;

		/// <summary>
		/// Flag to simulate the eye position in the editor by using the current camera position
		/// </summary>
		[Tooltip("Flag to simulate the eye position in the editor by using the current camera position")]
		public bool simulateEyePosition = true;

		/// <summary>
		/// Multiplier by which the sleep between fetching (simulating) data is increased when running in the editor
		/// Note: This might be needed as we always get "gaze data" if we are simulating it and don't want to get too much data
		/// </summary>
		[Tooltip("Multiplier by which the sleep between fetching (simulating) data is increased when running in the editor")]
		public float sleepMultiplier = 3.33f;

		#endregion Editor options


		/// <summary>
		/// Data access layer which is being used to get eye tracking data
		/// </summary>
		private IDataAccess dataAccessLayer;

		/// <summary>
		/// Cache of the main Unity camera (initialized on awake)
		/// </summary>
		private Camera mainCamera;

		/// <summary>
		/// Flag if the current gaze calibration is valid
		/// </summary>
		[HideInInspector]
		public bool IsGazeCalibrationValid
		{
			get
			{
				if (dataAccessLayer == null) return false;
				return dataAccessLayer.IsGazeCalibrationValid;
			}
		}

		/// <summary>
		/// Flag if the eyes API is available
		/// </summary>
		[HideInInspector]
		public bool EyesApiAvailable
		{
			get
			{
				if (dataAccessLayer == null) return false;
				return dataAccessLayer.EyesApiAvailable;
			}
		}

		/// <summary>
		/// Queue of gaze data from the data access layer which waits to be processed
		/// </summary>
		private ConcurrentQueue<GazeAPIData> dataQueue = new ConcurrentQueue<GazeAPIData>();

		/// <summary>
		/// Delegate for the new gaze data event
		/// </summary>
		/// <param name="gazeData">New GazeData</param>
		public delegate void NewDataEventHandler(GazeData gazeData);

		/// <summary>
		/// Event which is invoked every time new gaze data was processed
		/// Note: This is invoked in the main Unity thread during the update!
		/// </summary>
		public event NewDataEventHandler NewDataEvent;


		/// <summary>
		/// Queue of actions which need to be executed on the main thread
		/// </summary>
		private ConcurrentQueue<Action> actionQueue = new ConcurrentQueue<Action>();


		#region Unity awake, enable and disable events

		/// <summary>
		/// On awake initialize the main camera, list ob objects to log, layer mask and the device specific data access layer
		/// </summary>
		private void Awake()
		{
			// Cache the main camera
			mainCamera = Camera.main;

			// Transfer the array of game objects to the list of game objects which position should be logged
			foreach (GameObject gameObject in AlwaysPositionLoggedGameObjects)
			{
				PositionLoggedGameObjects.Add(gameObject);
			}

			// Manually update the list of names once
			UpdatePositionLoggedGameObjectNames();

			// Subscribe to future changes in the list of game objects
			PositionLoggedGameObjects.CollectionChanged += PositionLoggedGameObjectsChanged;

			// Get AOI LayerMask
			eyeTrackingAOILayerMask = LayerMask.GetMask(eyeTrackingAOILayers);


			// Calculate Layer Mask without AOIs
			// Note: As Layers are represented by bits, this calculation is also happening on bit level

			// We start with the default ray cast layers
			notEyeTrackingLayerMask = Physics.DefaultRaycastLayers;
			// Ignore all AOI layers
			foreach (string aoiLayer in eyeTrackingAOILayers)
			{
				notEyeTrackingLayerMask ^= (1 << LayerMask.NameToLayer(aoiLayer));
			}
			// Also ignore the layers which visualize the eye tracking data
			foreach (string visLayer in eyeTrackingVisLayers)
			{
				notEyeTrackingLayerMask ^= (1 << LayerMask.NameToLayer(visLayer));
			}
			// And ignore the AOI check layers
			foreach (string checkLayer in eyeTrackingCheckLayers)
			{
				notEyeTrackingLayerMask ^= (1 << LayerMask.NameToLayer(checkLayer));
			}


			// Initialize the data access layer depending on the platform currently in use
#if (UNITY_WSA && DOTNETWINRT_PRESENT) || WINDOWS_UWP
			dataAccessLayer = new DataAccessUWP(fetchDataSleepMs, dataQueue);
#elif UNITY_EDITOR
			// In the editor only initialize the layer if we want to simulate availability
			if (SimulateAvailablility)
			{
				dataAccessLayer = new DataAccessEditor(fetchDataSleepMs * sleepMultiplier, dataQueue, simulateEyePosition, mainCamera.transform);
			}
#endif
		}

		/// <summary>
		/// On enable start fetching data
		/// </summary>
		private void OnEnable()
		{
			// Check if the check layers are currently visible
			eyeTrackingCheckLayersVisible = true;
			foreach (string checkLayer in eyeTrackingCheckLayers)
			{
				eyeTrackingCheckLayersVisible &= (mainCamera.cullingMask & (1 << LayerMask.NameToLayer(checkLayer))) != 0;
			}

			// If we initialized an data access layer start fetching data
			dataAccessLayer?.StartFetching();
		}

		/// <summary>
		/// On disable stop fetching data
		/// </summary>
		private void OnDisable()
		{
			// If we initialized an data access layer stop fetching data
			dataAccessLayer?.StopFetching();
		}

		#endregion Unity awake, enable and disable events


		#region Position logged game objects actions

		/// <summary>
		/// On every change of the list of game objects of which we want to log the position, also update the name list
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void PositionLoggedGameObjectsChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			// Only add the update command if we don't already plan on updating the list on the next unity update
			if (!PositionLoggedGameObjectNamesWillBeUpdated)
			{
				// Add the update command to the queue of commands to be executed in the main thread
				//  as the name of game objects can only be accessed in the main thread and we can't be sure
				//  that we currently are in the main thread
				actionQueue.Enqueue(UpdatePositionLoggedGameObjectNames);

				// Note that we will be updating the list in the next update
				PositionLoggedGameObjectNamesWillBeUpdated = true;
			}
		}

		/// <summary>
		/// Update the list of names of the game objects which position we want to log
		/// Note: This function must be called in the main Unity thread!
		/// </summary>
		private void UpdatePositionLoggedGameObjectNames()
		{
			// Create a new string array with the needed length
			PositionLoggedGameObjectNames = new string[PositionLoggedGameObjects.Count];

			// Go through every game object and get its name
			for (int i = 0; i < PositionLoggedGameObjects.Count; i++)
			{
				PositionLoggedGameObjectNames[i] = PositionLoggedGameObjects[i].name;
			}
		}

		#endregion Position logged game objects actions


		#region AOI check visibility control

		/// <summary>
		/// Set the culling mask of the main camera so that the AOI check layer is (not) visible, based on the argument
		/// Note: Can be called at any time and queues the command itself to be executed on the next update
		/// </summary>
		/// <param name="visible">True if the layer should be visible, false if it should be hidden</param>
		public void SetAOICheckVisibleAsync(bool visible)
		{
			// Queue the command to be executed on the next update
			actionQueue.Enqueue(() => SetAOICheckVisible(visible));
		}

		/// <summary>
		/// Set the culling mask of the main camera so that the AOI check layer is (not) visible, based on the argument
		/// Note: Has to be called from the main Unity thread!
		/// </summary>
		/// <param name="visible">True if the layer should be visible, false if it should be hidden</param>
		public void SetAOICheckVisible(bool visible)
		{
			// If we want to show the mask but it is already visible log an error
			if (visible && eyeTrackingCheckLayersVisible)
			{
				Debug.Log("[EyeTracking] Can't show AOI check layer as it is already visible!");
			}
			// If we want to hide it and it isn't currently visible also log an error
			else if (!visible && !eyeTrackingCheckLayersVisible)
			{
				Debug.Log("[EyeTracking] Can't hide AOI check layer as it isn't currently visible!");
			}

			// If we want to show it and it currently isn't visible, make it visible
			else if (visible && !eyeTrackingCheckLayersVisible)
			{
				foreach (string checkLayer in eyeTrackingCheckLayers)
				{
					mainCamera.cullingMask |= (1 << LayerMask.NameToLayer(checkLayer));
				}

				eyeTrackingCheckLayersVisible = true;

				//Debug.Log("Showed check layers, new status: " + eyeTrackingCheckLayersVisible);
			}
			// If we want to hide it and it currently is visible, hide it
			else if (!visible && eyeTrackingCheckLayersVisible)
			{
				foreach (string checkLayer in eyeTrackingCheckLayers)
				{
					mainCamera.cullingMask &= ~(1 << LayerMask.NameToLayer(checkLayer));
				}

				eyeTrackingCheckLayersVisible = false;

				//Debug.Log("Hidden check layers, new status: " + eyeTrackingCheckLayersVisible);
			}
		}

		#endregion AOI check visibility control


		#region Main processing in Unity update

		/// <summary>
		/// On Update check if we need to invoke an action, process waiting data and if existing update the data access layer
		/// </summary>
		private void Update()
		{
			// Call the Unity update for the data access layer if it exists
			dataAccessLayer?.UnityUpdate();

			// Process all actions which are waiting to be processed
			// Note: This isn't 100% thread save as we could end in a loop when there are new actions coming in faster than we are processing them.
			//       However, actions are added that rarely that we shouldn't run into issues.
			if (!actionQueue.IsEmpty)
			{
				while (actionQueue.TryDequeue(out Action action))
				{
					// Invoke the action from the queue
					action.Invoke();
				}
			}

			// Process all data which is waiting to be processed
			// Note: This isn't 100% thread save as we could end in a loop when there is still new data coming in faster than we are processing it.
			//       However, data is added slowly enough that we shouldn't run into issues.
			if (!dataQueue.IsEmpty)
			{
				while (dataQueue.TryDequeue(out GazeAPIData gazeAPIData))
				{
					// Initialize the resulting data object with the API data
					GazeData gazeData = new GazeData(gazeAPIData);

					// Add the current frame time
					gazeData.FrameTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

					// If we have valid gaze data process it
					if (gazeAPIData.GazeHasValue)
					{
						// Crate a gaze ray based on the gaze data
						Ray gazeRay = new Ray(gazeAPIData.GazeOrigin, gazeAPIData.GazeDirection);

						////
						// The 3D gaze point is the actual position the wearer is looking at.
						// As everything apart from the eye tracking layers is visible, we have to collide the gaze with every layer except the eye tracking layers

						// Check if the gaze hits anything that isn't an AOI
						gazeData.GazePointHit = Physics.Raycast(gazeRay, out RaycastHit hitInfo, Mathf.Infinity, notEyeTrackingLayerMask);

						// If we hit something, write the hit info to the data
						if (gazeData.GazePointHit)
						{
							// Write all info from the hit to the data object
							gazeData.GazePoint = hitInfo.point;
							gazeData.GazePointName = hitInfo.collider.name;

							// Cache the transform of the game object which was hit
							Transform hitTransform = hitInfo.collider.transform;

							// Get the position of the hit in the local coordinates of the game object which was hit
							gazeData.GazePointOnHit = hitTransform.InverseTransformPoint(hitInfo.point);

							// Get the info about the object which was hit
							gazeData.GazePointHitPosition = hitTransform.position;
							gazeData.GazePointHitRotation = hitTransform.rotation.eulerAngles;
							gazeData.GazePointHitScale = hitTransform.lossyScale;

							// Update the position of the GazePoint visualization (only visible in the MRC view)
							GazePointVis.transform.position = hitInfo.point;

							// Get the position of the gaze point in the right and left eye if we have stereo rendering
							if (mainCamera.stereoActiveEye != Camera.MonoOrStereoscopicEye.Mono)
							{
								gazeData.GazePointLeftDisplay = mainCamera.WorldToScreenPoint(hitInfo.point, Camera.MonoOrStereoscopicEye.Left);
								gazeData.GazePointRightDisplay = mainCamera.WorldToScreenPoint(hitInfo.point, Camera.MonoOrStereoscopicEye.Right);
							}
							else
							{
								gazeData.GazePointLeftDisplay = null;
								gazeData.GazePointRightDisplay = null;
							}

							// Also get the mono position (and always do this)
							gazeData.GazePointMonoDisplay = mainCamera.WorldToScreenPoint(hitInfo.point, Camera.MonoOrStereoscopicEye.Mono);

							// Get the position of the gaze point on the webcam image
							gazeData.GazePointWebcam = webcamCamera.WorldToScreenPoint(hitInfo.point, Camera.MonoOrStereoscopicEye.Mono);
						}
						else
						{
							// Update the position of the GazePoint visualization (only visible in the MRC view)
							GazePointVis.transform.position = Vector3.zero;
						}

						////
						// To check for AOIs we do a separate ray cast on the AOI layer

						// Check if the gaze hits a AOI
						gazeData.GazePointAOIHit = Physics.Raycast(gazeRay, out hitInfo, Mathf.Infinity, eyeTrackingAOILayerMask);

						// If we hit an AOI, write the hit info to data, otherwise simply leave it empty
						if (gazeData.GazePointAOIHit)
						{
							// Write all info from the hit to the data object
							gazeData.GazePointAOI = hitInfo.point;
							gazeData.GazePointAOIName = hitInfo.collider.name;

							// Cache the transform of the game object which was hit
							Transform hitTransform = hitInfo.collider.transform;

							// Get the position of the hit in the local coordinates of the game object which was hit
							gazeData.GazePointAOIOnHit = hitTransform.InverseTransformPoint(hitInfo.point);

							// Get the info about the object which was hit
							gazeData.GazePointAOIHitPosition = hitTransform.position;
							gazeData.GazePointAOIHitRotation = hitTransform.rotation.eulerAngles;
							gazeData.GazePointAOIHitScale = hitTransform.lossyScale;

							// Get the position of the gaze point on the web cam image
							gazeData.GazePointAOIWebcam = webcamCamera.WorldToScreenPoint(hitInfo.point, Camera.MonoOrStereoscopicEye.Mono);
						}
					}

					// Get the position of the game objects we want to log

					// Create new data array
					gazeData.positionInfos = new PositionInfo[PositionLoggedGameObjects.Count];
					// Go through every game object and log its position
					for (int i = 0; i < PositionLoggedGameObjects.Count; i++)
					{
						// Check if the game object still exists
						gazeData.positionInfos[i].positionValid = PositionLoggedGameObjects[i] != null;

						// If it still exists log its position
						if (gazeData.positionInfos[i].positionValid)
						{
							// Name
							gazeData.positionInfos[i].gameObjectName = PositionLoggedGameObjects[i].name;

							// Position
							Vector3 position = PositionLoggedGameObjects[i].transform.position;
							gazeData.positionInfos[i].xPosition = position.x;
							gazeData.positionInfos[i].yPosition = position.y;
							gazeData.positionInfos[i].zPosition = position.z;

							// Rotation
							Vector3 rotation = PositionLoggedGameObjects[i].transform.rotation.eulerAngles;
							gazeData.positionInfos[i].xRotation = rotation.x;
							gazeData.positionInfos[i].yRotation = rotation.y;
							gazeData.positionInfos[i].zRotation = rotation.z;

							// Scale
							Vector3 scale = PositionLoggedGameObjects[i].transform.lossyScale;
							gazeData.positionInfos[i].xScale = scale.x;
							gazeData.positionInfos[i].yScale = scale.y;
							gazeData.positionInfos[i].zScale = scale.z;
						}
					}

					// Invoke new data event
					NewDataEvent?.Invoke(gazeData);
				}
			}
		}

		#endregion Main processing in Unity update

	}
}
