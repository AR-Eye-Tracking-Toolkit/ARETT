// Copyright (c) Sebastian Kapp.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace ARETT {
	/// <summary>
	/// The data logger handles the recording of eye tracking data by taking it from the data provider, processing it and then transferring it to the file handler to write it to the disk.
	/// </summary>
	public class DataLogger : MonoBehaviour
	{
		#region Recording Info
		/// <summary>
		/// Information about the current recording
		/// </summary>
		public RecordingInfo CurrentRecording { get; private set; } = new RecordingInfo
		{
			participantName = "None",
			recordingName = "None"
		};
		#endregion Recording Info

		#region Framework Components
		[Header("Framework Components")]
		/// <summary>
		/// Data Provider for the eye tracking data
		/// </summary>
		[SerializeField]
		private DataProvider dataProvider;

		/// <summary>
		/// File Handler which handles writing to the data log as well as the info file
		/// Note: This isn't a MonoBehaviour therefore we initialize it here and don't need it as component of a GameObject
		/// </summary>
		public IFileHandler FileHandler { get; private set; }
		#endregion Framework Components


		#region Configuration
		[Header("Configuration")]
		/// <summary>
		/// Should the data be written to the documents folder in a UWP Build (HoloLens 2)?
		/// If false it is written to the persistent application data (like on any other platform).
		/// </summary>
		[Tooltip("Should the data be written to the documents folder in a UWP Build (HoloLens 2)?")]
		public bool useDocumentsFolder = true;
		#endregion Configuration

		/// <summary>
		/// Culture Info which is used when generating the data log so the output format is independent from the language set on the device
		/// </summary>
		private static readonly CultureInfo ci = new CultureInfo("en-US");

		/// <summary>
		/// Name of the current participant
		/// </summary>
		public string ParticipantName
		{
			get
			{
				lock (CurrentRecording)
				{
					return CurrentRecording.participantName;
				}
			}
			set
			{
				lock (CurrentRecording)
				{
					CurrentRecording.participantName = value;
				}
			}
		}

		/// <summary>
		/// Name of the current recording (to be set before starting a recording)
		/// </summary>
		public string RecordingName
		{
			get
			{
				lock (CurrentRecording)
				{
					return CurrentRecording.recordingName;
				}
			}
			set
			{
				lock (CurrentRecording)
				{
					CurrentRecording.recordingName = value;
				}
			}
		}

		/// <summary>
		/// Queue of strings which are supposed to be logged together with the next gaze data
		/// </summary>
		private ConcurrentQueue<string> infoToLog = new ConcurrentQueue<string>();


		/// <summary>
		/// On Awake initialize the FileHandler
		/// </summary>
		private void Awake()
		{
#if (UNITY_WSA && DOTNETWINRT_PRESENT) || WINDOWS_UWP
			// Check if we want to write to the documents folder
			if (useDocumentsFolder)
			{
				// If yes, use the UWP file handler
				FileHandler = new FileHandlerUWP(Application.productName);
			}
			else
			{
				// Otherwise use the "regular" file handler for the persistent app data
				FileHandler = new FileHandler(Application.persistentDataPath);
			}
#else
			// Otherwise use the persistent app data
			FileHandler = new FileHandler(Application.persistentDataPath);
#endif
		}

		/// <summary>
		/// Make sure we stop recording when the logger is disabled (e.g. when exiting the app)
		/// </summary>
		private void OnDisable()
		{
			if (FileHandler.writingData)
			{
				StopRecording();
			}
		}

		/// <summary>
		/// Start recording gaze data
		/// </summary>
		public void StartRecording()
		{
			if (FileHandler.writingData)
			{
				Debug.LogError("[EyeTracking DataLogger] Can't start recording as we already are recording!");
			}

			// Start the coroutine to actually start recording
			StartCoroutine(StartRecordingCoroutine());
		}

		/// <summary>
		/// Asynchronous Coroutine which starts the recording
		/// </summary>
		/// <returns></returns>
		private IEnumerator StartRecordingCoroutine()
		{
			// Start the data log and thereby initialize the file handler for this recording
			FileHandler.StartDataLog(ParticipantName, RecordingName);

			// Wait for the file to be opened
			yield return new WaitUntil(() => FileHandler.writingData);

			// Create recording info
			// Note: At this point we transfer the game objects of which we want to log the position into the recording info.
			//       This means the list stays static during recording which is important for the data file header
			lock (CurrentRecording)
			{
				CurrentRecording = new RecordingInfo
				{
					participantName = ParticipantName,
					recordingName = RecordingName,
					eyesApiAvailable = dataProvider.EyesApiAvailable,
					gazeCalibrationValid = dataProvider.IsGazeCalibrationValid,
					startTime = DateTime.Now,
					positionLoggedGameObjectNames = dataProvider.PositionLoggedGameObjectNames
				};

				// Write information file
				JSON.RecordingInfo infoJson = new JSON.RecordingInfo(CurrentRecording);
				string infoString = JsonUtility.ToJson(infoJson);
				FileHandler.WriteInformation(infoString, true);
			}

			// Create the header for the data file
			StringBuilder dataFileHeader = new StringBuilder();

			// Append the general information
			dataFileHeader.Append("eyeDataTimestamp,eyeDataRelativeTimestamp,frameTimestamp,isCalibrationValid,");
			dataFileHeader.Append("gazeHasValue,gazeOrigin_x,gazeOrigin_y,gazeOrigin_z,gazeDirection_x,gazeDirection_y,gazeDirection_z,");
			dataFileHeader.Append("gazePointHit,gazePoint_x,gazePoint_y,gazePoint_z,gazePoint_target_name,gazePoint_target_x,gazePoint_target_y,gazePoint_target_z,");
			dataFileHeader.Append("gazePoint_target_pos_x,gazePoint_target_pos_y,gazePoint_target_pos_z,gazePoint_target_rot_x,gazePoint_target_rot_y,gazePoint_target_rot_z,gazePoint_target_scale_x,gazePoint_target_scale_y,gazePoint_target_scale_z,");
			dataFileHeader.Append("gazePointLeftScreen_x,gazePointLeftScreen_y,gazePointLeftScreen_z,gazePointRightScreen_x,gazePointRightScreen_y,gazePointRightScreen_z,gazePointMonoScreen_x,gazePointMonoScreen_y,gazePointMonoScreen_z,");
			dataFileHeader.Append("GazePointWebcam_x,GazePointWebcam_y,GazePointWebcam_z,");
			dataFileHeader.Append("gazePointAOIHit,gazePointAOI_x,gazePointAOI_y,gazePointAOI_z,gazePointAOI_name,gazePointAOI_target_x,gazePointAOI_target_y,gazePointAOI_target_z,");
			dataFileHeader.Append("gazePointAOI_target_pos_x,gazePointAOI_target_pos_y,gazePointAOI_target_pos_z,gazePointAOI_target_rot_x,gazePointAOI_target_rot_y,gazePointAOI_target_rot_z,gazePointAOI_target_scale_x,gazePointAOI_target_scale_y,gazePointAOI_target_scale_z,");
			dataFileHeader.Append("GazePointAOIWebcam_x,GazePointAOIWebcam_y,GazePointAOIWebcam_z");

			lock (CurrentRecording)
			{
				// Append the game object information if we want to log game objects
				for (int i = 0; i < CurrentRecording.positionLoggedGameObjectNames.Length; i++)
				{
					// We start with NA as game object name and replace it with the actual name if the game object (still) exists
					string gameObjectName = "NA";
					if (CurrentRecording.positionLoggedGameObjectNames[i] != null)
					{
						gameObjectName = FileHandler.RemoveAllNonAlphanumeric(CurrentRecording.positionLoggedGameObjectNames[i]);
					}

					dataFileHeader.Append(",GameObject_");
					dataFileHeader.Append(gameObjectName);
					dataFileHeader.Append("_xPos,GameObject_");
					dataFileHeader.Append(gameObjectName);
					dataFileHeader.Append("_yPos,GameObject_");
					dataFileHeader.Append(gameObjectName);
					dataFileHeader.Append("_zPos,GameObject_");
					dataFileHeader.Append(gameObjectName);
					dataFileHeader.Append("_xRot,GameObject_");
					dataFileHeader.Append(gameObjectName);
					dataFileHeader.Append("_yRot,GameObject_");
					dataFileHeader.Append(gameObjectName);
					dataFileHeader.Append("_zRot,GameObject_");
					dataFileHeader.Append(gameObjectName);
					dataFileHeader.Append("_xScale,GameObject_");
					dataFileHeader.Append(gameObjectName);
					dataFileHeader.Append("_yScale,GameObject_");
					dataFileHeader.Append(gameObjectName);
					dataFileHeader.Append("_zScale");
				}
			}
			// Header for the info column
			dataFileHeader.Append(",info");

			// Write the header for the data file
			FileHandler.WriteData(dataFileHeader.ToString());

			// Subscribe to new data event
			dataProvider.NewDataEvent += NewDataHandler;

			Debug.Log("[EyeTracking DataLogger] Started recording");
		}


		/// <summary>
		/// Stop recording gaze data
		/// </summary>
		public void StopRecording()
		{
			if (!FileHandler.writingData)
			{
				Debug.LogError("[EyeTracking DataLogger] Can't stop recording as we currently aren't recording!");
			}

			// Unsubscribe from new data event
			dataProvider.NewDataEvent -= NewDataHandler;

			lock (CurrentRecording)
			{
				// Update the recording information
				CurrentRecording.stopTime = DateTime.Now;
				CurrentRecording.recordingDuration = CurrentRecording.stopTime - CurrentRecording.startTime;

				// Write the new information file
				FileHandler.WriteInformation(JsonUtility.ToJson(new JSON.RecordingInfo(CurrentRecording), true), true);
			}

			// Stop the current log
			FileHandler.StopDataLog();

			Debug.Log("[EyeTracking DataLogger] Stopped recording");
		}

		/// <summary>
		/// Add the string argument to the next line of gaze data in the log as well as the info file
		/// </summary>
		/// <param name="info"></param>
		public void LogInfo(string info)
		{
			if (!FileHandler.writingData)
			{
				Debug.LogWarning("[EyeTracking DataLogger] Can't add info as we aren't logging!");
				throw new Exception("Can't add info as we aren't logging!");
			}

			// Lock the info log object while adding info
			lock (CurrentRecording.infoLogs)
			{
				// Add the info to the current recording info
				CurrentRecording.infoLogs.Add((DateTimeOffset.Now, info));
			}

			// Add the info to the queue of infos which are supposed to be added to the log
			infoToLog.Enqueue(info);

			Debug.Log("[EyeTracking DataLogger] Logged info: " + info);
		}

		/// <summary>
		/// Handle new eye tracking data by logging it
		/// Note: We should be in the main Unity thread when receiving the event from the data provider, however logging should also work outside the main thread
		/// </summary>
		/// <param name="gazeData"></param>
		private void NewDataHandler(GazeData gazeData)
		{
			// Start the resulting data string
			StringBuilder logStringBuilder = new StringBuilder();
			logStringBuilder.Append(gazeData.EyeDataTimestamp.ToString(ci));
			logStringBuilder.Append(",");
			// Note: Highest accuracy for the EyeDataRelativeTimestamp is 100ns so we don't loose information by outputting a fixed number of decimal places
			logStringBuilder.Append(gazeData.EyeDataRelativeTimestamp.ToString("F4", ci));
			logStringBuilder.Append(",");
			logStringBuilder.Append(gazeData.FrameTimestamp.ToString(ci));
			logStringBuilder.Append(",");
			logStringBuilder.Append(gazeData.IsCalibrationValid.ToString(ci));
			logStringBuilder.Append(",");
			logStringBuilder.Append(gazeData.GazeHasValue.ToString(ci));
			logStringBuilder.Append(",");

			// If we have valid gaze data process it
			if (gazeData.GazeHasValue)
			{
				// Append the info about the gaze to our log
				logStringBuilder.Append(gazeData.GazeOrigin.x.ToString("F5", ci));
				logStringBuilder.Append(",");
				logStringBuilder.Append(gazeData.GazeOrigin.y.ToString("F5", ci));
				logStringBuilder.Append(",");
				logStringBuilder.Append(gazeData.GazeOrigin.z.ToString("F5", ci));
				logStringBuilder.Append(",");
				logStringBuilder.Append(gazeData.GazeDirection.x.ToString("F5", ci));
				logStringBuilder.Append(",");
				logStringBuilder.Append(gazeData.GazeDirection.y.ToString("F5", ci));
				logStringBuilder.Append(",");
				logStringBuilder.Append(gazeData.GazeDirection.z.ToString("F5", ci));
				logStringBuilder.Append(",");

				// Did we hit any GameObject?
				logStringBuilder.Append(gazeData.GazePointHit);
				logStringBuilder.Append(",");

				// If we did hit something on the gaze ray, write the hit info to the log, otherwise simply write NA
				if (gazeData.GazePointHit)
				{
					logStringBuilder.Append(gazeData.GazePoint.x.ToString("F5", ci));
					logStringBuilder.Append(",");
					logStringBuilder.Append(gazeData.GazePoint.y.ToString("F5", ci));
					logStringBuilder.Append(",");
					logStringBuilder.Append(gazeData.GazePoint.z.ToString("F5", ci));
					logStringBuilder.Append(",");
					logStringBuilder.Append(gazeData.GazePointName);
					logStringBuilder.Append(",");
					logStringBuilder.Append(gazeData.GazePointOnHit.x.ToString("F5", ci));
					logStringBuilder.Append(",");
					logStringBuilder.Append(gazeData.GazePointOnHit.y.ToString("F5", ci));
					logStringBuilder.Append(",");
					logStringBuilder.Append(gazeData.GazePointOnHit.z.ToString("F5", ci));
					logStringBuilder.Append(",");
					logStringBuilder.Append(gazeData.GazePointHitPosition.x.ToString("F5", ci));
					logStringBuilder.Append(",");
					logStringBuilder.Append(gazeData.GazePointHitPosition.y.ToString("F5", ci));
					logStringBuilder.Append(",");
					logStringBuilder.Append(gazeData.GazePointHitPosition.z.ToString("F5", ci));
					logStringBuilder.Append(",");
					logStringBuilder.Append(gazeData.GazePointHitRotation.x.ToString("F5", ci));
					logStringBuilder.Append(",");
					logStringBuilder.Append(gazeData.GazePointHitRotation.y.ToString("F5", ci));
					logStringBuilder.Append(",");
					logStringBuilder.Append(gazeData.GazePointHitRotation.z.ToString("F5", ci));
					logStringBuilder.Append(",");
					logStringBuilder.Append(gazeData.GazePointHitScale.x.ToString("F5", ci));
					logStringBuilder.Append(",");
					logStringBuilder.Append(gazeData.GazePointHitScale.y.ToString("F5", ci));
					logStringBuilder.Append(",");
					logStringBuilder.Append(gazeData.GazePointHitScale.z.ToString("F5", ci));
					logStringBuilder.Append(",");

					if (gazeData.GazePointLeftDisplay.HasValue)
					{
						logStringBuilder.Append(gazeData.GazePointLeftDisplay.Value.x.ToString("F5", ci));
						logStringBuilder.Append(",");
						logStringBuilder.Append(gazeData.GazePointLeftDisplay.Value.y.ToString("F5", ci));
						logStringBuilder.Append(",");
						logStringBuilder.Append(gazeData.GazePointLeftDisplay.Value.z.ToString("F5", ci));
						logStringBuilder.Append(",");
						logStringBuilder.Append(gazeData.GazePointRightDisplay.Value.x.ToString("F5", ci));
						logStringBuilder.Append(",");
						logStringBuilder.Append(gazeData.GazePointRightDisplay.Value.y.ToString("F5", ci));
						logStringBuilder.Append(",");
						logStringBuilder.Append(gazeData.GazePointRightDisplay.Value.z.ToString("F5", ci));
						logStringBuilder.Append(",");
					}
					else
					{
						logStringBuilder.Append("NA,NA,NA,NA,NA,NA,");
					}

					logStringBuilder.Append(gazeData.GazePointMonoDisplay.x.ToString("F5", ci));
					logStringBuilder.Append(",");
					logStringBuilder.Append(gazeData.GazePointMonoDisplay.y.ToString("F5", ci));
					logStringBuilder.Append(",");
					logStringBuilder.Append(gazeData.GazePointMonoDisplay.z.ToString("F5", ci));
					logStringBuilder.Append(",");

					logStringBuilder.Append(gazeData.GazePointWebcam.x.ToString("F5", ci));
					logStringBuilder.Append(",");
					logStringBuilder.Append(gazeData.GazePointWebcam.y.ToString("F5", ci));
					logStringBuilder.Append(",");
					logStringBuilder.Append(gazeData.GazePointWebcam.z.ToString("F5", ci));
					logStringBuilder.Append(",");
				}
				else
				{
					logStringBuilder.Append("NA,NA,NA,NA,NA,NA,NA,NA,NA,NA,NA,NA,NA,NA,NA,NA,");
					logStringBuilder.Append("NA,NA,NA,NA,NA,NA,NA,NA,NA,NA,NA,NA,");
				}

				// Did we hit an AOI?
				logStringBuilder.Append(gazeData.GazePointAOIHit);
				logStringBuilder.Append(",");

				// If we hit an AOI, write the hit info to the log, otherwise simply write NA
				if (gazeData.GazePointAOIHit)
				{
					logStringBuilder.Append(gazeData.GazePointAOI.x.ToString("F5", ci));
					logStringBuilder.Append(",");
					logStringBuilder.Append(gazeData.GazePointAOI.y.ToString("F5", ci));
					logStringBuilder.Append(",");
					logStringBuilder.Append(gazeData.GazePointAOI.z.ToString("F5", ci));
					logStringBuilder.Append(",");
					logStringBuilder.Append(gazeData.GazePointAOIName);
					logStringBuilder.Append(",");
					logStringBuilder.Append(gazeData.GazePointAOIOnHit.x.ToString("F5", ci));
					logStringBuilder.Append(",");
					logStringBuilder.Append(gazeData.GazePointAOIOnHit.y.ToString("F5", ci));
					logStringBuilder.Append(",");
					logStringBuilder.Append(gazeData.GazePointAOIOnHit.z.ToString("F5", ci));
					logStringBuilder.Append(",");
					logStringBuilder.Append(gazeData.GazePointAOIHitPosition.x.ToString("F5", ci));
					logStringBuilder.Append(",");
					logStringBuilder.Append(gazeData.GazePointAOIHitPosition.y.ToString("F5", ci));
					logStringBuilder.Append(",");
					logStringBuilder.Append(gazeData.GazePointAOIHitPosition.z.ToString("F5", ci));
					logStringBuilder.Append(",");
					logStringBuilder.Append(gazeData.GazePointAOIHitRotation.x.ToString("F5", ci));
					logStringBuilder.Append(",");
					logStringBuilder.Append(gazeData.GazePointAOIHitRotation.y.ToString("F5", ci));
					logStringBuilder.Append(",");
					logStringBuilder.Append(gazeData.GazePointAOIHitRotation.z.ToString("F5", ci));
					logStringBuilder.Append(",");
					logStringBuilder.Append(gazeData.GazePointAOIHitScale.x.ToString("F5", ci));
					logStringBuilder.Append(",");
					logStringBuilder.Append(gazeData.GazePointAOIHitScale.y.ToString("F5", ci));
					logStringBuilder.Append(",");
					logStringBuilder.Append(gazeData.GazePointAOIHitScale.z.ToString("F5", ci));
					logStringBuilder.Append(",");

					logStringBuilder.Append(gazeData.GazePointAOIWebcam.x.ToString("F5", ci));
					logStringBuilder.Append(",");
					logStringBuilder.Append(gazeData.GazePointAOIWebcam.y.ToString("F5", ci));
					logStringBuilder.Append(",");
					logStringBuilder.Append(gazeData.GazePointAOIWebcam.z.ToString("F5", ci));
				}
				else
				{
					logStringBuilder.Append("NA,NA,NA,NA,NA,NA,NA,");
					logStringBuilder.Append("NA,NA,NA,NA,NA,NA,NA,NA,NA,NA,NA,NA");
				}
			}
			else
			{
				// No gaze data
				logStringBuilder.Append("NA,NA,NA,NA,NA,NA,NA,");
				// No gaze hit
				logStringBuilder.Append("NA,NA,NA,NA,NA,NA,NA,NA,NA,NA,NA,NA,NA,NA,NA,NA,");
				logStringBuilder.Append("NA,NA,NA,NA,NA,NA,NA,NA,NA,NA,NA,NA,");
				// No AOI hit
				logStringBuilder.Append("NA,NA,NA,NA,NA,NA,NA,NA,");
				logStringBuilder.Append("NA,NA,NA,NA,NA,NA,NA,NA,NA,NA,NA,NA");
			}

			// If we are supposed to log the position of game objects, log them
			// Note: We log the position even when we have no gaze data!
			for (int i = 0; i < gazeData.positionInfos.Length; i++)
			{
				// Make sure the object does have a valid position
				if (!gazeData.positionInfos[i].positionValid)
				{
					logStringBuilder.Append(",NA,NA,NA,NA,NA,NA,NA,NA,NA");
					continue;
				}

				logStringBuilder.Append(",");
				logStringBuilder.Append(gazeData.positionInfos[i].xPosition.ToString("F5", ci));
				logStringBuilder.Append(",");
				logStringBuilder.Append(gazeData.positionInfos[i].yPosition.ToString("F5", ci));
				logStringBuilder.Append(",");
				logStringBuilder.Append(gazeData.positionInfos[i].zPosition.ToString("F5", ci));
				logStringBuilder.Append(",");
				logStringBuilder.Append(gazeData.positionInfos[i].xRotation.ToString("F5", ci));
				logStringBuilder.Append(",");
				logStringBuilder.Append(gazeData.positionInfos[i].yRotation.ToString("F5", ci));
				logStringBuilder.Append(",");
				logStringBuilder.Append(gazeData.positionInfos[i].zRotation.ToString("F5", ci));
				logStringBuilder.Append(",");
				logStringBuilder.Append(gazeData.positionInfos[i].xScale.ToString("F5", ci));
				logStringBuilder.Append(",");
				logStringBuilder.Append(gazeData.positionInfos[i].yScale.ToString("F5", ci));
				logStringBuilder.Append(",");
				logStringBuilder.Append(gazeData.positionInfos[i].zScale.ToString("F5", ci));
			}

			// Append the separator for the info to the output string
			logStringBuilder.Append(",");

			// If there is info we should log, log it
			if (!infoToLog.IsEmpty)
			{
				while (infoToLog.TryDequeue(out string info))
				{
					// Append the string
					logStringBuilder.Append(info);

					// If there are more strings to append, add a separator between them
					if (infoToLog.Count > 0)
					{
						logStringBuilder.Append(";");
					}
				}
			}
			// If there is nothing to log simply leave this column empty.
			// This saves space and as we are at the end of the columns it doesn't mess up the other columns

			// Write info to file
			FileHandler.WriteData(logStringBuilder.ToString());
		}
	}
}
