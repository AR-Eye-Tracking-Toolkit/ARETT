// Copyright (c) Sebastian Kapp.
// Licensed under the MIT License.

namespace ARETT.JSON
{
	/// <summary>
	/// Current eye tracking status of the device for JSON parsing
	/// </summary>
	public class Status
	{
		public string deviceName;
		public string participantName;
		public bool isGazeCalibrationValid;
		public bool eyesApiAvailable;
		public bool recording;
		public string recordingName;
		public string recordingStartTime;
		public string recordingStopTime;
		public string recordingDuration;
		public bool accuracyGridVisible;
		public int accuracyGridDistance;
		public bool checkVisible;

		public string[] infoLogs;
	}
}