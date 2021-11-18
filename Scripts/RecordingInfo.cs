// Copyright (c) Sebastian Kapp.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace ARETT
{
	/// <summary>
	/// Class containing the information about the current recording
	/// </summary>
	public class RecordingInfo
	{
		public string participantName;
		public string recordingName;
		public bool eyesApiAvailable;
		public bool gazeCalibrationValid;
		public DateTime startTime;
		public DateTime stopTime;
		public TimeSpan recordingDuration;
		public string[] positionLoggedGameObjectNames;

		public List<(DateTimeOffset timestamp, string info)> infoLogs = new List<(DateTimeOffset, string)>();
	}
}
