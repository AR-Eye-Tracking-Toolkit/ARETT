// Copyright (c) Sebastian Kapp.
// Licensed under the MIT License.

namespace ARETT.JSON
{
	/// <summary>
	/// Recording info with all information as string so we can parse it to JSON
	/// </summary>
	public class RecordingInfo
	{
		public string participantName;
		public string recordingName;
		public bool eyesApiAvailable;
		public bool gazeCalibrationValid;
		public string startTime;
		public string stopTime;
		public string recordingDuration;
		public string[] positionLoggedGameObjectNames;
		public string[] infoLogs;

		/// <summary>
		/// Create recording info in JSON compatible format from the original data format
		/// </summary>
		/// <param name="recordingInformation"></param>
		public RecordingInfo(ARETT.RecordingInfo recordingInformation)
		{
			participantName = recordingInformation.participantName;
			recordingName = recordingInformation.recordingName;
			eyesApiAvailable = recordingInformation.eyesApiAvailable;
			gazeCalibrationValid = recordingInformation.gazeCalibrationValid;
			startTime = recordingInformation.startTime.ToString();
			stopTime = recordingInformation.stopTime.ToString();
			recordingDuration = recordingInformation.recordingDuration.ToString();
			positionLoggedGameObjectNames = recordingInformation.positionLoggedGameObjectNames;

			// Lock the info log object while transferring logs
			lock (recordingInformation.infoLogs)
			{
				infoLogs = new string[recordingInformation.infoLogs.Count];
				for (int i = 0; i < recordingInformation.infoLogs.Count; i++)
				{
					infoLogs[i] = "[" + recordingInformation.infoLogs[i].timestamp.ToString("yyyy-MM-dd HH:mm:ss") + ", " + recordingInformation.infoLogs[i].timestamp.ToUnixTimeMilliseconds() + "] " + recordingInformation.infoLogs[i].info;
				}
			}
		}
	}
}
