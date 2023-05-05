// Copyright (c) Sebastian Kapp.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Timers;
using UnityEngine;

// Microsoft Mixed Reality Toolkit
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.Windows.Utilities;
using Microsoft.MixedReality.Toolkit.WindowsMixedReality;
using Microsoft.MixedReality.OpenXR;

// Windows Eye Tracking APIs
#if WINDOWS_UWP
using Windows.Perception;
using Windows.Perception.People;
using Windows.Perception.Spatial;
using Windows.UI.Input.Spatial;
#elif UNITY_WSA && DOTNETWINRT_PRESENT
using Microsoft.Windows.Perception;
using Microsoft.Windows.Perception.People;
using Microsoft.Windows.Perception.Spatial;
using Microsoft.Windows.UI.Input.Spatial;
#endif

namespace ARETT
{
	/// <summary>
	/// The data access layer periodically checks for new eye tracking data and enqueues new data in the data queue of the data provider
	/// </summary>
	public class DataAccessUWP : IDataAccess
	{
		/// <summary>
		/// Time in milliseconds at what interval we check for new data
		/// </summary>
		private int fetchDataSleepMs;

		/// <summary>
		/// Timer which invokes fetching the data
		/// </summary>
		private Timer fetchDataTimer;
		/// <summary>
		/// Is the previous timer to fetch data still busy?
		/// </summary>
		private static int fetchDataTimerIsBusy = 0;


		/// <summary>
		/// Is the current gaze calibration valid?
		/// </summary>
		public bool IsGazeCalibrationValid = false;
		bool IDataAccess.IsGazeCalibrationValid => IsGazeCalibrationValid;

		/// <summary>
		/// Is the eyes API available?
		/// </summary>
		public bool EyesApiAvailable = false;
		bool IDataAccess.EyesApiAvailable => EyesApiAvailable;

#if (UNITY_WSA && DOTNETWINRT_PRESENT) || WINDOWS_UWP
		/// <summary>
		/// Timestamp of the last eye data we received
		/// </summary>
		private long lastEyeDataTimestamp = 0;
#endif

		/// <summary>
		/// Queue of gaze data in which we are supposed to place new gaze data
		/// </summary>
		private ConcurrentQueue<GazeAPIData> dataQueue;

		/// <summary>
		/// When creating the UWP data access initialize the configuration
		/// </summary>
		/// <param name="fetchDataSleepMs">Time in milliseconds at what interval we check for new data</param>
		/// <param name="dataQueue">Queue of gaze data in which we are supposed to place new gaze data</param>
		public DataAccessUWP(int fetchDataSleepMs, ConcurrentQueue<GazeAPIData> dataQueue)
		{
			// Keep the configuration
			this.fetchDataSleepMs = fetchDataSleepMs;
			this.dataQueue = dataQueue;

			// Check for the Eyes API
			CheckIfEyesApiAvailable();

			// Check for Permission
			CheckEyePermission();
		}
		
		/// <summary>
		/// Start fetching eye tracking data
		/// </summary>
		public void StartFetching()
		{
			// Configure the timer
			fetchDataTimer = new Timer(fetchDataSleepMs);
			fetchDataTimer.Elapsed += CheckForEyeData;
			fetchDataTimer.AutoReset = true;

			// Start it
			fetchDataTimer.Start();
		}

		/// <summary>
		/// Start fetching eye tracking data
		/// </summary>
		public void StopFetching()
		{
			if (fetchDataTimer != null && fetchDataTimer.Enabled)
			{
				// Disable timer
				fetchDataTimer.Stop();
				// Dispose of timer
				fetchDataTimer.Dispose();
			}
		}


		/// <summary>
		/// Check whether the Windows Eyes API is available
		/// </summary>
		private void CheckIfEyesApiAvailable()
		{
#if (UNITY_WSA && DOTNETWINRT_PRESENT) || WINDOWS_UWP
			// Make sure EyeTracking is available on the device
			EyesApiAvailable = WindowsApiChecker.IsPropertyAvailable(
					"Windows.UI.Input.Spatial",
					"SpatialPointerPose",
					"Eyes");

			// If yes, ask for permission to use it
			if (EyesApiAvailable)
			{
				EyesApiAvailable &= EyesPose.IsSupported();
			}
#endif
		}

		/// <summary>
		/// Check whether the eye tracking permission is set up in the application and was granted
		/// </summary>
		private void CheckEyePermission()
		{
#if UNITY_EDITOR && UNITY_WSA && UNITY_2019_3_OR_NEWER
			Microsoft.MixedReality.Toolkit.Utilities.Editor.UWPCapabilityUtility.RequireCapability(
					UnityEditor.PlayerSettings.WSACapability.GazeInput,
					this.GetType());
#endif

			if (Application.isPlaying && EyesApiAvailable)
			{
#if (UNITY_WSA && DOTNETWINRT_PRESENT) || WINDOWS_UWP
				AskForETPermission();
#endif
			}
		}

#if (UNITY_WSA && DOTNETWINRT_PRESENT) || WINDOWS_UWP
		/// <summary>
		/// Flag to make sure that we only request eye tracking access once
		/// </summary>
		private static bool askedForETAccessAlready = false;

		/// <summary>
		/// Triggers a prompt to let the user decide whether to permit using eye tracking 
		/// </summary>
		private async void AskForETPermission()
		{
			if (!askedForETAccessAlready) // Making sure this is only triggered once
			{
				askedForETAccessAlready = true;
				await EyesPose.RequestAccessAsync();
			}
		}
#endif
		
		/// <summary>
		/// Function which checks for new eye tracking data and is called periodically by the timer
		/// </summary>
		/// <param name="source"></param>
		/// <param name="e"></param>
		private void CheckForEyeData(object source, ElapsedEventArgs e)
		{
			// Make sure the previous event isn't still running
			if (System.Threading.Interlocked.CompareExchange(ref fetchDataTimerIsBusy, 1, 0) == 1)
			{
				//Debug.LogError("Previous event still running!");
				return;
			}

			try {
#if (UNITY_WSA && DOTNETWINRT_PRESENT) || WINDOWS_UWP
				// Make sure we have the spatial coordinate system (which is cached every update) and the eyes API is available
				if (currentSpatialCoordinateSystem == null || !EyesApiAvailable)
				{
					//Debug.Log("[UWPDataAccess] No currentSpatialCoordinateSystem or Eyes API not available!");
					return;
				}

				// Try to get the new pointer data (which includes eye tracking)
				SpatialPointerPose pointerPose = SpatialPointerPose.TryGetAtTimestamp(currentSpatialCoordinateSystem, PerceptionTimestampHelper.FromHistoricalTargetTime(DateTimeOffset.Now));
				if (pointerPose != null)
				{
					// Check if we actually got any eye tracking data
					var eyes = pointerPose.Eyes;
					if (eyes != null)
					{
						// Unix time stamp from when the eye tracking data we got was acquired
						long targetTimeUnix = eyes.UpdateTimestamp.TargetTime.ToUnixTimeMilliseconds();
					
						// Check if we have new data
						if (lastEyeDataTimestamp != targetTimeUnix) {
							// Save new time stamp
							lastEyeDataTimestamp = targetTimeUnix;

							// Save the information whether the calibration is valid
							IsGazeCalibrationValid = eyes.IsCalibrationValid;

							// If we have eye tracking data announce it in the event, otherwise simply announce Vector3.zero as origin and direction
							if (eyes.Gaze.HasValue) {
								dataQueue.Enqueue(new GazeAPIData() {
									EyeDataTimestamp = targetTimeUnix,
									EyeDataRelativeTimestamp = eyes.UpdateTimestamp.SystemRelativeTargetTime.TotalMilliseconds,
									IsCalibrationValid = eyes.IsCalibrationValid,
									GazeHasValue = eyes.Gaze.HasValue,
									GazeOrigin = eyes.Gaze.Value.Origin.ToUnityVector3(),
									GazeDirection = eyes.Gaze.Value.Direction.ToUnityVector3()
								});
							}	
							else {
								dataQueue.Enqueue(new GazeAPIData() {
									EyeDataTimestamp = targetTimeUnix,
									EyeDataRelativeTimestamp = eyes.UpdateTimestamp.SystemRelativeTargetTime.TotalMilliseconds,
									IsCalibrationValid = eyes.IsCalibrationValid,
									GazeHasValue = eyes.Gaze.HasValue,
									GazeOrigin = Vector3.zero,
									GazeDirection = Vector3.zero
								});
							}
						}
					}
				}
#else
				// On all platforms which are not UWP print error
				Debug.Log("[UWPDataAccess] Not on correct platform! Doing nothing!");
#endif
			}
			finally
			{
				fetchDataTimerIsBusy = 0;
			}
		}

#if (UNITY_WSA && DOTNETWINRT_PRESENT) || WINDOWS_UWP
		/// <summary>
		/// Current spatial coordinate system, as provided by the Mixed Reality Toolkit and needed for requesting eye tracking data using the Windows API
		/// </summary>
		SpatialCoordinateSystem currentSpatialCoordinateSystem = null;
#endif

		/// <summary>
		/// On every Unity Update cache the current spatial coordinate system so we can use it in the data retrieval thread
		/// </summary>
		public void UnityUpdate()
		{
#if (UNITY_WSA && DOTNETWINRT_PRESENT) || WINDOWS_UWP
            // Always get the current coordinate system and cache it so we can use it when requesting eye tracking data
            // Note: Caching is needed as getting the current coordinate system uses Unity functions which need to be executed in the main Unity thread
            //       while we request the EyeTracking data outside the main thread
            //Original way to get the CSCS
            //currentSpatialCoordinateSystem = WindowsMixedRealityUtilities.SpatialCoordinateSystem;

            //Alternative way to get the CSCS
            //currentSpatialCoordinateSystem = SpatialLocator.GetDefault().CreateStationaryFrameOfReferenceAtCurrentLocation().CoordinateSystem;

            // See: https://github.com/microsoft/MixedRealityToolkit-Unity/issues/10082
            currentSpatialCoordinateSystem = PerceptionInterop.GetSceneCoordinateSystem(Pose.identity) as SpatialCoordinateSystem;
#endif
        }
	}
}
