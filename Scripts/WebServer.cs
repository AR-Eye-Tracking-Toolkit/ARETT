// Copyright (c) Sebastian Kapp.
// Licensed under the MIT License.

using ARETT.JSON;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using System.IO;
using System;
using System.Collections.Concurrent;

namespace ARETT
{
	/// <summary>
	/// Web server which serves the web interface from the device and handles the API calls from the interface
	/// </summary>
	public class WebServer : MonoBehaviour
	{
		[Header("Framework Components")]
#pragma warning disable CS0649 // unassigned variable
		/// <summary>
		/// Data Provider of the eye tracking data
		/// </summary>
		[SerializeField]
		private DataProvider dataProvider;
		
		/// <summary>
		/// Data Logger for recording
		/// </summary>
		[SerializeField]
		private DataLogger dataLogger;

		/// <summary>
		/// Reference to the accuracy grid to show/hide it
		/// </summary>
		[SerializeField]
		private AccuracyGrid accuracyGrid;
#pragma warning restore CS0649 // unassigned variable

		[Header("Configuration")]
		/// <summary>
		/// URI under which the Server listens and where the path is appended (e.g. http://*:8080/)
		/// </summary>
		[Tooltip("URI under which the Server listens and where the path is appended (e.g. http://*:8080/)")]
		[SerializeField]
		private string listenerBaseURI = "http://*:8080/";


		[Header("Web Files")]
		/// <summary>
		/// Main HTML Page
		/// </summary>
		[Tooltip("Main HTML page")]
		[SerializeField]
		private TextAsset indexHTML;
		private string indexHTMLstring;
		
		/// <summary>
		/// GUI JavaScript File
		/// </summary>
		[Tooltip("GUI JavaScript")]
		[SerializeField]
		private TextAsset guiJS;
		private string guiJSstring;

		/// <summary>
		/// API JavaScript File
		/// </summary>
		[Tooltip("API JavaScript")]
		[SerializeField]
		private TextAsset apiJS;
		private string apiJSstring;

		/// <summary>
		/// Bootstrap CSS File
		/// </summary>
		[Tooltip("Bootstrap CSS File")]
		[SerializeField]
		private TextAsset bootstrapCSS;
		private string bootstrapCSSstring;


		/// <summary>
		/// HTTP Server which processes the requests.
		/// </summary>
		private HTTPServer httpServer;

		/// <summary>
		/// Name of the current device, as provided by Unity
		/// </summary>
		private string deviceName;

		/// <summary>
		/// Queue of commands waiting to be processed inside the main Unity thread
		/// </summary>
		private ConcurrentQueue<Action> actionQueue = new ConcurrentQueue<Action>();

		/// <summary>
		/// On Awake translate the selected files to strings and get the device name
		/// </summary>
		private void Awake()
		{
			indexHTMLstring = indexHTML.text;
			guiJSstring = guiJS.text;
			apiJSstring = apiJS.text;
			bootstrapCSSstring = bootstrapCSS.text;

			deviceName = SystemInfo.deviceName;
		}

		/// <summary>
		/// On Enable start the HTTP Server
		/// </summary>
		private void OnEnable()
		{
			httpServer = new HTTPServer(new string[] { listenerBaseURI }, handleRequest);
			httpServer.Run();
		}

		/// <summary>
		/// On Disable stop the HTTP Server
		/// </summary>
		private void OnDisable()
		{
			httpServer.Stop();
		}

		/// <summary>
		/// Handle an incoming request and respond with the corresponding data
		/// </summary>
		/// <param name="request"></param>
		/// <returns></returns>
		private string handleRequest(HttpListenerRequest request)
		{
			// Which function should be called can be identified using the URL
			switch (request.Url.LocalPath.TrimEnd('/'))
			{
				// Just ignore requests for a favicon for now
				case "/favicon.ico":
					return "";

				case "":
				case "/index.html":
					return indexHTMLstring;

				case "/gui.js":
					return guiJSstring;

				case "/api.js":
					return apiJSstring;

				case "/bootstrap.min.css":
					return bootstrapCSSstring;

				case "/api/status":
					return processStatusRequest(request);

				case "/api/participant_name":
					return processParticipantNameRequest(request);

				case "/api/recording_name":
					return processRecordingNameRequest(request);

				case "/api/recording_start":
					return processRecordingStart(request);

				case "/api/recording_stop":
					return processRecordingStop(request);

				case "/api/accuracy_show":
					return processAccuracyShow(request);

				case "/api/accuracy_hide":
					return processAccuracyHide(request);

				case "/api/accuracy_distance":
					return processAccuracyDistance(request);

				case "/api/check_show":
					return processCheckShow(request);

				case "/api/check_hide":
					return processCheckHide(request);

				case "/api/add_info":
					return processAddInfo(request);

				case "/api/launch_calibration":
					return launchCalibration(request);

				default:
					return "404 - NOT FOUND!";
			}
		}

		/// <summary>
		/// Launch the eye tracking calibration on the HoloLens 2
		/// </summary>
		/// <param name="request"></param>
		/// <returns></returns>
		private string launchCalibration(HttpListenerRequest request)
		{
			// Result
			Success response;

#if (UNITY_WSA && DOTNETWINRT_PRESENT) || WINDOWS_UWP
			UnityEngine.WSA.Application.InvokeOnUIThread(async () =>
			{
				bool result = await global::Windows.System.Launcher.LaunchUriAsync(new System.Uri("ms-hololenssetup://EyeTracking"));
				
				if (!result)
				{
					Debug.LogError("[ARETT WebServer] Launching eye tracking calibration failed.");
				}
			}, false);

			response = new Success()
			{
				success = true,
				message = "Sent command to start eye tracking calibration on device."
			};
#else
			// We can't launch the calibration when we aren't running on the HoloLens 2
			Debug.LogError("[ARETT WebServer] Can't launch eye tracking calibration as we are not running on a HoloLens 2!");

			response = new Success()
			{
				success = false,
				message = "Can't start eye tracking calibration as we aren't running on a HoloLens 2!"
			};
#endif

			// Return result
			return JsonUtility.ToJson(response, true);
		}

		/// <summary>
		/// Add an info string to the log
		/// </summary>
		/// <param name="request"></param>
		/// <returns></returns>
		private string processAddInfo(HttpListenerRequest request)
		{
			Success response;

			// If we get a PUT or POST request add the provided information to the log
			if (request.HttpMethod == "PUT" || request.HttpMethod == "POST")
			{
				// Get parameters from request
				string infoString = request.QueryString.Get("info_string");

				// If we don't get the data using the QueryString, try to read it from the raw data sent
				if (infoString == null)
				{
					// Manually parse the post parameters
					Dictionary<string, string> postParams = parseRequestParams(request);

					// If we now got a value use it
					if (postParams.ContainsKey("info_string"))
					{
						infoString = postParams["info_string"];
					}
				}

				// If a new info was given log it
				if (infoString != null)
				{
					try
					{
						// Log info
						dataLogger.LogInfo(infoString);

						// create response
						response = new Success()
						{
							success = true,
							message = "Info added!"
						};
					}
					catch (Exception e)
					{
						response = new Success()
						{
							success = false,
							message = e.Message
						};
					}
				}
				// If no info was given we can't add it to the log
				else
				{
					response = new Success()
					{
						success = false,
						message = "Error on adding info! No info text was provided!"
					};
				}
			}
			// Otherwise reply with an error as there is nothing to add
			else
			{
				response = new Success()
				{
					success = false,
					message = "Error on adding info! No info text was provided!"
				};
			}

			return JsonUtility.ToJson(response, true);
		}

		/// <summary>
		/// Show the AOI check layer
		/// </summary>
		/// <param name="request"></param>
		/// <returns></returns>
		private string processCheckShow(HttpListenerRequest request)
		{
			// Send the command to show the layers
			// Note: The command has to be executed in the main Unity thread, therefore we use the async command which queues it internally for the next update
			dataProvider.SetAOICheckVisibleAsync(true);

			Success response = new Success()
			{
				success = true,
				message = "Sent command to show AOI check layer."
			};

			return JsonUtility.ToJson(response, true);
		}

		/// <summary>
		/// Hide the AOI check layer
		/// </summary>
		/// <param name="request"></param>
		/// <returns></returns>
		private string processCheckHide(HttpListenerRequest request)
		{
			// Send the command to show the layers
			// Note: The command has to be executed in the main Unity thread, therefore we use the async command which queues it internally for the next update
			dataProvider.SetAOICheckVisibleAsync(false);

			Success response = new Success()
			{
				success = true,
				message = "Sent command to hide AOI check layer."
			};

			return JsonUtility.ToJson(response, true);
		}

		/// <summary>
		/// Hide the accuracy grid
		/// </summary>
		/// <param name="request"></param>
		/// <returns></returns>
		private string processAccuracyHide(HttpListenerRequest request)
		{
			Success response;

			// If we aren't showing the grid we can't hide it
			if (!accuracyGrid.gridVisible)
			{
				response = new Success()
				{
					success = false,
					message = "Can't hide accuracy grid as it isn't visible!"
				};
			}
			else
			{
				// Queue to hide the grid on the next update
				actionQueue.Enqueue(accuracyGrid.HideGrid);

				response = new Success()
				{
					success = true,
					message = "Hidden accuracy grid."
				};
			}

			return JsonUtility.ToJson(response, true);
		}

		/// <summary>
		/// Show the accuracy grid
		/// </summary>
		/// <param name="request"></param>
		/// <returns></returns>
		private string processAccuracyShow(HttpListenerRequest request)
		{
			Success response;

			// If we already are recording we can't start
			if (accuracyGrid.gridVisible)
			{
				response = new Success()
				{
					success = false,
					message = "Can't show accuracy grid as it already is visible!"
				};
			}
			else
			{
				// Queue to show the grid on the next update
				actionQueue.Enqueue(accuracyGrid.ShowGrid);

				response = new Success()
				{
					success = true,
					message = "Showing accuracy grid."
				};
			}

			return JsonUtility.ToJson(response, true);
		}

		/// <summary>
		/// Change the distance in which the accuracy grid is displayed
		/// </summary>
		/// <param name="request"></param>
		/// <returns></returns>
		private string processAccuracyDistance(HttpListenerRequest request)
		{
			Success response;

			// Get the requested distance
			string distanceString = request.QueryString.Get("distance");

			// If we don't get the data using the QueryString, try to read it from the raw data sent
			if (distanceString == null)
			{
				// Manually parse the post parameters
				Dictionary<string, string> postParams = parseRequestParams(request);

				// If we now got a value use it
				if (postParams.ContainsKey("distance"))
				{
					distanceString = postParams["distance"];
				}
			}

			// If we didn't get a distance we can't change it
			if (distanceString == null)
			{
				response = new Success()
				{
					success = false,
					message = "Can't change accuracy grid distance as no distance was specified!"
				};
				return JsonUtility.ToJson(response, true);
			}

			// Parse the distance to an integer
			int distance = -1;
			try
			{
				distance = int.Parse(distanceString);
			}
			catch
			{
				response = new Success()
				{
					success = false,
					message = $"Can't change accuracy grid distance as specified distance {distanceString} is not a number!"
				};
				return JsonUtility.ToJson(response, true);
			}

			// Make sure the distance is valid
			if (distance != 05 && distance != 10 && distance != 20 && distance != 40)
			{
				response = new Success()
				{
					success = false,
					message = $"Can't change accuracy grid distance as specified distance {distance} is invalid!"
				};
				return JsonUtility.ToJson(response, true);
			}

			// Check if the distance is already set
			if (accuracyGrid.gridVisible && accuracyGrid.currentGridDistance == distance)
			{
				response = new Success()
				{
					success = true,
					message = $"Accuracy grid already set to distance {distance}."
				};
			}
			else
			{
				// Otherwise queue to show the grid on the next update
				actionQueue.Enqueue(() => accuracyGrid.ChangeGridDistance(distance));

				response = new Success()
				{
					success = true,
					message = $"Changing accuracy grid distance to {distance}."
				};
			}

			return JsonUtility.ToJson(response, true);
		}

		/// <summary>
		/// Start recording data
		/// </summary>
		/// <param name="request"></param>
		/// <returns></returns>
		private string processRecordingStart(HttpListenerRequest request)
		{
			Success response;

			// If we already are recording we can't start
			if (dataLogger.FileHandler.writingData)
			{
				Debug.LogError("[EyeTracking] Already recording");

				response = new Success()
				{
					success = false,
					message = "Can't start recording as we already are recording!"
				};
			}
			else
			{
				// Queue to start recording
				actionQueue.Enqueue(dataLogger.StartRecording);
				response = new Success()
				{
					success = true,
					message = "Started recording."
				};
			}
			return JsonUtility.ToJson(response, true);
		}

		/// <summary>
		/// Stop recording data
		/// </summary>
		/// <param name="request"></param>
		/// <returns></returns>
		private string processRecordingStop(HttpListenerRequest request)
		{
			Success response;

			// If we aren't recording we can't stop
			if (!dataLogger.FileHandler.writingData)
			{
				response = new Success()
				{
					success = false,
					message = "Can't stop recording as we currently aren't recording!"
				};
			}
			else
			{
				// Queue to stop recording
				actionQueue.Enqueue(dataLogger.StopRecording);

				response = new Success()
				{
					success = true,
					message = "Stopped recording."
				};
			}

			return JsonUtility.ToJson(response, true);
		}


		/// <summary>
		/// Process a recording name request
		/// </summary>
		/// <param name="request"></param>
		/// <returns></returns>
		private string processRecordingNameRequest(HttpListenerRequest request)
		{
			Recording response;

			// If we get a PUT or POST request update the recording name
			if (request.HttpMethod == "PUT" || request.HttpMethod == "POST")
			{
				// Get parameters from request
				string newRecordingName = request.QueryString.Get("recording_name");

				// If we don't get the data using the QueryString, try to read it from the raw data sent
				if (newRecordingName == null)
				{
					// Manually parse the post parameters
					Dictionary<string, string> postParams = parseRequestParams(request);

					// If we now got a value use it
					if (postParams.ContainsKey("recording_name"))
					{
						newRecordingName = postParams["recording_name"];
					}
				}

				// If a new name was given set it
				if (newRecordingName != null)
				{
					// set new name
					dataLogger.RecordingName = newRecordingName;

					// create response
					response = new Recording()
					{
						recordingName = dataLogger.RecordingName,
						wasUpdated = true
					};
				}
				// If no name was given just answer with the current name
				else
				{
					response = new Recording()
					{
						recordingName = dataLogger.RecordingName,
						wasUpdated = false
					};
				}
			}
			// Otherwise simply reply with the current recording name
			else
			{
				response = new Recording()
				{
					recordingName = dataLogger.RecordingName,
					wasUpdated = false
				};
			}

			return JsonUtility.ToJson(response, true);
		}

		/// <summary>
		/// Process a participant name request
		/// </summary>
		/// <param name="request"></param>
		/// <returns></returns>
		private string processParticipantNameRequest(HttpListenerRequest request)
		{
			Participant response;

			// If we get a PUT or POST request update the participant name
			if (request.HttpMethod == "PUT" || request.HttpMethod == "POST")
			{
				// Get parameters from request
				string newParticipantName = request.QueryString.Get("participant_name");

				// If we don't get the data using the QueryString, try to read it from the raw data sent
				if (newParticipantName == null)
				{
					// Manually parse the post parameters
					Dictionary<string, string> postParams = parseRequestParams(request);

					// If we now got a value use it
					if (postParams.ContainsKey("participant_name"))
					{
						newParticipantName = postParams["participant_name"];
					}
				}

				// If a new name was given set it
				if (newParticipantName != null)
				{
					// set new name
					dataLogger.ParticipantName = newParticipantName;

					// create response
					response = new Participant()
					{
						participantName = dataLogger.ParticipantName,
						wasUpdated = true
					};
				}
				// If no name was given just answer with the current name
				else
				{
					Debug.LogError("[EyeTracking] Error on set participant name: No new name provided!");
					
					response = new Participant()
					{
						participantName = dataLogger.ParticipantName,
						wasUpdated = false
					};
				}
			}
			// Otherwise simply reply with the current participant name
			else
			{
				response = new Participant()
				{
					participantName = dataLogger.ParticipantName,
					wasUpdated = false
				};
			}

			return JsonUtility.ToJson(response, true);
		}

		/// <summary>
		/// Manually parse the parameters from a request
		/// </summary>
		/// <param name="request"></param>
		/// <returns></returns>
		private Dictionary<string, string> parseRequestParams(HttpListenerRequest request)
		{
			// Open StreamReader
			using (StreamReader inputStream = new StreamReader(request.InputStream))
			{
				// Read all data
				string input = inputStream.ReadToEnd();

				// Parse the parameters
				Dictionary<string, string> postParams = new Dictionary<string, string>();
				string[] rawParams = input.Split('&');
				foreach (string param in rawParams)
				{
					string[] kvPair = param.Split('=');
					if (kvPair.Length >= 2)
						postParams.Add(kvPair[0], WebUtility.UrlDecode(kvPair[1]));
				}

				// Return parsed parameters
				return postParams;
			}
		}

		/// <summary>
		/// Create the response to a status request
		/// </summary>
		/// <param name="request"></param>
		/// <returns></returns>
		private string processStatusRequest(HttpListenerRequest request)
		{
			Status response = new Status
			{
				deviceName = deviceName,
				eyesApiAvailable = dataProvider.EyesApiAvailable,
				isGazeCalibrationValid = dataProvider.IsGazeCalibrationValid,
				recording = dataLogger.FileHandler.writingData,
				accuracyGridVisible = accuracyGrid.gridVisible,
				accuracyGridDistance = accuracyGrid.currentGridDistance,
				checkVisible = dataProvider.eyeTrackingCheckLayersVisible
			};

			// If a recording exists add the recording information
			lock (dataLogger.CurrentRecording)
			{
				response.participantName = dataLogger.CurrentRecording.participantName;
				response.recordingName = dataLogger.CurrentRecording.recordingName;
				response.recordingStartTime = dataLogger.CurrentRecording.startTime != null ? dataLogger.CurrentRecording.startTime.ToString() : "<None>";
				response.recordingStopTime = dataLogger.CurrentRecording.stopTime != null ? dataLogger.CurrentRecording.stopTime.ToString() : "<None>";
				response.recordingDuration = dataLogger.CurrentRecording.recordingDuration != null ? dataLogger.CurrentRecording.recordingDuration.ToString() : "<None>";
			}

			// Lock the info log object while adding info
			lock (dataLogger.CurrentRecording.infoLogs)
			{
				// If there are info logs add them to the response
				if (dataLogger.CurrentRecording.infoLogs.Count > 0)
				{
					response.infoLogs = new string[dataLogger.CurrentRecording.infoLogs.Count];
					for (int i = 0; i < dataLogger.CurrentRecording.infoLogs.Count; i++)
					{
						response.infoLogs[i] = "[" + dataLogger.CurrentRecording.infoLogs[i].timestamp.ToString("yyyy-MM-dd HH:mm:ss") + "] " + dataLogger.CurrentRecording.infoLogs[i].info;
					}
				}
			}

			// Return the info
			return JsonUtility.ToJson(response, true);
		}


		/// <summary>
		/// Check on every frame if there is a command waiting to be executed inside the Unity thread
		/// </summary>
		private void Update()
		{
			// Check if there is something to process
			if (!actionQueue.IsEmpty)
			{
				// Process all commands which are waiting to be processed
				// Note: This isn't 100% thread save as we could end in a loop when there is still new data coming in.
				//       However, data is added slowly enough so we shouldn't run into issues.
				while (actionQueue.TryDequeue(out Action action))
				{
					// Invoke the waiting action
					action.Invoke();
				}
			}
		}
	}
}
