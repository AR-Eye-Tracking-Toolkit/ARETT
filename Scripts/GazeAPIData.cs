// Copyright (c) Sebastian Kapp.
// Licensed under the MIT License.

using UnityEngine;

namespace ARETT {
	/// <summary>
	/// Data of the eye gaze received from the device API
	/// </summary>
	public class GazeAPIData
	{
		/// <summary>
		/// Unix Timestamp in ms from which the data stems (accuracy 1ms)
		/// </summary>
		public long EyeDataTimestamp;

		/// <summary>
		/// Relative Timestamp in ms from which the data stems (accuracy up to 100ns, DateTimeOffset of data)
		/// </summary>
		public double EyeDataRelativeTimestamp;

		/// <summary>
		/// Flag if the gaze calibration was valid
		/// </summary>
		public bool IsCalibrationValid;

		/// <summary>
		/// Flag if there is a value for the gaze
		/// </summary>
		public bool GazeHasValue;

		/// <summary>
		/// Origin of the gaze in Unity coordinates
		/// </summary>
		public Vector3 GazeOrigin;

		/// <summary>
		/// Direction of the gaze in Unity coordinates
		/// </summary>
		public Vector3 GazeDirection;
	}
}