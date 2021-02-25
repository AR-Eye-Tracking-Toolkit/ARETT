// Copyright (c) Sebastian Kapp.
// Licensed under the MIT License.

namespace ARETT
{
	/// <summary>
	/// Interface for the device specific data access layers
	/// </summary>
	public interface IDataAccess
	{
		/// <summary>
		/// Check if the gaze calibration is valid
		/// </summary>
		/// <returns></returns>
		bool IsGazeCalibrationValid { get; }

		/// <summary>
		/// Check if the eyes API is available
		/// </summary>
		/// <returns></returns>
		bool EyesApiAvailable { get; }

		/// <summary>
		/// Start fetching eye tracking data
		/// </summary>
		void StartFetching();

		/// <summary>
		/// Stop fetching eye tracking data
		/// </summary>
		void StopFetching();

		/// <summary>
		/// Unity update which is forwarded to the data access layer.
		/// To be used if the data access layer needs to do something every frame in the main Unity thread.
		/// </summary>
		void UnityUpdate();
	}
}
