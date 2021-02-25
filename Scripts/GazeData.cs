// Copyright (c) Sebastian Kapp.
// Licensed under the MIT License.

using UnityEngine;

namespace ARETT {
	/// <summary>
	/// Data of the eye gaze after processing
	/// </summary>
	public class GazeData
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
		/// Unity Timestamp in ms of the Unity frame the data was processed
		/// </summary>
		public long FrameTimestamp;

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

		/// <summary>
		/// Flag if the gaze ray hit a Unity object and therefore the GazePoint data is valid
		/// </summary>
		public bool GazePointHit;

		/// <summary>
		/// Location of the gaze point in Unity coordinates
		/// </summary>
		public Vector3 GazePoint;

		/// <summary>
		/// Name of the game object which was hit
		/// </summary>
		public string GazePointName;

		/// <summary>
		/// Position of the gaze point in the local coordinates of the game object which was hit
		/// </summary>
		public Vector3 GazePointOnHit;

		/// <summary>
		/// Position of the game object which the gaze hit
		/// </summary>
		public Vector3 GazePointHitPosition;

		/// <summary>
		/// Rotation of the game object which the gaze hit
		/// </summary>
		public Vector3 GazePointHitRotation;

		/// <summary>
		/// Scale of the game object which the gaze hit
		/// Note: Scale in (lossy) world scale, not the localScale in relation to the parent game object!
		/// </summary>
		public Vector3 GazePointHitScale;

		/// <summary>
		/// Position of the GazePoint on the left display
		/// </summary>
		public Vector3? GazePointLeftDisplay;

		/// <summary>
		/// Position of the GazePoint on the right display
		/// </summary>
		public Vector3? GazePointRightDisplay;

		/// <summary>
		/// Position of the GazePoint in a mono view
		/// </summary>
		public Vector3 GazePointMonoDisplay;

		/// <summary>
		/// Position of the GazePoint in the view of the webcam
		/// </summary>
		public Vector3 GazePointWebcam;

		/// <summary>
		/// Flag if the AOI gaze ray it a Unity object and therefore the AOI GazePoint data is valid
		/// </summary>
		public bool GazePointAOIHit;

		/// <summary>
		/// Location of the gaze point on the AOI layer in Unity coordinates
		/// </summary>
		public Vector3 GazePointAOI;

		/// <summary>
		/// Name of the AOI game object which was hit
		/// </summary>
		public string GazePointAOIName;

		/// <summary>
		/// Position of the AOI gaze point in the local coordinates of the game object which was hit
		/// </summary>
		public Vector3 GazePointAOIOnHit;

		/// <summary>
		/// Position of the game object which the AOI gaze hit
		/// </summary>
		public Vector3 GazePointAOIHitPosition;

		/// <summary>
		/// Rotation of the game object which the AOI gaze hit
		/// </summary>
		public Vector3 GazePointAOIHitRotation;

		/// <summary>
		/// Scale of the game object which the AOI gaze hit
		/// Note: Scale in (lossy) world scale, not the localScale in relation to the parent game object
		/// </summary>
		public Vector3 GazePointAOIHitScale;

		/// <summary>
		/// Position of the GazePoint on the AOI layer in the view of the web cam
		/// </summary>
		public Vector3 GazePointAOIWebcam;

		/// <summary>
		/// Information about the position and rotation of the game objects specified for logging
		/// </summary>
		public PositionInfo[] positionInfos;

		/// <summary>
		/// Empty constructor for initialization without data
		/// </summary>
		public GazeData() {	}

		/// <summary>
		/// Initialize the GazeData with the data from an API call
		/// </summary>
		/// <param name="gazeAPIData">Data received from the API call</param>
		public GazeData(GazeAPIData gazeAPIData)
		{
			EyeDataTimestamp = gazeAPIData.EyeDataTimestamp;
			EyeDataRelativeTimestamp = gazeAPIData.EyeDataRelativeTimestamp;
			IsCalibrationValid = gazeAPIData.IsCalibrationValid;
			GazeHasValue = gazeAPIData.GazeHasValue;
			GazeOrigin = gazeAPIData.GazeOrigin;
			GazeDirection = gazeAPIData.GazeDirection;
		}
	}
}
