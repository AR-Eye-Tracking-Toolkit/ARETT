// Copyright (c) Sebastian Kapp.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Timers;
using UnityEngine;

namespace ARETT
{
	/// <summary>
	/// The data access layer for the Unity Editor which periodically generates new eye tracking data and passes it to the data provider for processing
	/// </summary>
	public class DataAccessEditor : IDataAccess
	{
		/// <summary>
		/// Time in milliseconds at what interval we check for new data
		/// </summary>
		private float fetchDataSleepMs;

		/// <summary>
		/// Flag if we actually want to simulate an eye position in the editor using the main camera
		/// </summary>
		private bool simulateEyePosition;

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


		/// <summary>
		/// Transform of the main Unity camera from which the Editor takes the simulated eye position
		/// </summary>
		private Transform mainCameraTransform;
		/// <summary>
		/// Cache for the position of the main camera when simulating eye movements
		/// </summary>
		private Vector3 cameraPosition = Vector3.zero;
		/// <summary>
		/// Cache for the direction of the main camera when simulating eye movements
		/// </summary>
		private Vector3 cameraDirection = Vector3.zero;


		/// <summary>
		/// Queue of gaze data in which we are supposed to place new gaze data
		/// </summary>
		private ConcurrentQueue<GazeAPIData> dataQueue;

		/// <summary>
		/// When creating the Editor data generation initialize the configuration
		/// </summary>
		/// <param name="fetchDataSleepMs">Time in milliseconds at what interval we check for new data</param>
		/// <param name="dataQueue">Queue of gaze data in which we are supposed to place new gaze data</param>
		/// <param name="simulateEyePosition">Flag if we actually want to simulate an eye position in the editor using the main camera</param>
		/// <param name="mainCameraTransform">Transform of the main Unity camera from which the Editor takes the simulated eye position</param>
		public DataAccessEditor(float fetchDataSleepMs, ConcurrentQueue<GazeAPIData> dataQueue, bool simulateEyePosition, Transform mainCameraTransform)
		{
			// Keep the configuration
			this.fetchDataSleepMs = fetchDataSleepMs;
			this.dataQueue = dataQueue;
			this.simulateEyePosition = simulateEyePosition;
			this.mainCameraTransform = mainCameraTransform;

			// If we initialized the layer the "eyes api" is available
			// However if the "calibration" is valid depends on whether we want to simulate the eye position
			EyesApiAvailable = true;
			IsGazeCalibrationValid = simulateEyePosition;
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
			if (fetchDataTimer!= null && fetchDataTimer.Enabled)
			{
				// Disable timer
				fetchDataTimer.Stop();
				// Dispose of timer
				fetchDataTimer.Dispose();
			}
		}
		
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
				// If we are in the editor either use the current camera position as dummy or send empty data
				if (simulateEyePosition)
				{
					dataQueue.Enqueue(new GazeAPIData()
					{
						EyeDataTimestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
						EyeDataRelativeTimestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
						IsCalibrationValid = true,
						GazeHasValue = true,
						GazeOrigin = cameraPosition,
						GazeDirection = cameraDirection
					});
				}
				else
				{
					dataQueue.Enqueue(new GazeAPIData()
					{
						EyeDataTimestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
						EyeDataRelativeTimestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
						IsCalibrationValid = false,
						GazeHasValue = false,
						GazeOrigin = Vector3.zero,
						GazeDirection = Vector3.zero
					});
				}
			}
			finally
			{
				fetchDataTimerIsBusy = 0;
			}
		}

		/// <summary>
		/// On every Unity Update cache the current camera position so we can use it in the data retrieval thread
		/// </summary>
		public void UnityUpdate()
		{
			if (simulateEyePosition)
			{
				cameraPosition = mainCameraTransform.position;
				cameraDirection = mainCameraTransform.forward;
			}
		}
	}
}
