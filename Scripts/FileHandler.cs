// Copyright (c) Sebastian Kapp.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Timers;
using UnityEngine;

namespace ARETT
{
	/// <summary>
	/// The file handler accepts text which is supposed to be written to disk and writes it to the persistent data folder
	/// </summary>
	public class FileHandler : IFileHandler
	{
		#region Data Log

		/// <summary>
		/// StreamWriter for the data file
		/// </summary>
		private StreamWriter dataWriter;

		/// <summary>
		/// Time between writing to the data file (in ms)
		/// </summary>
		public float dataSleep = 1000f;
		float IFileHandler.dataSleep { get => dataSleep; set => dataSleep = value; }

		/// <summary>
		/// Timer which starts the writing command
		/// </summary>
		private Timer dataWriteTimer;

		/// <summary>
		/// Is the previous timer to write data still busy?
		/// </summary>
		private static int dataWriteTimerIsBusy = 0;

		/// <summary>
		/// Lock Object for data file
		/// </summary>
		private readonly object dataFileLock = new object();

		/// <summary>
		/// Flag whether we are currently writing data
		/// </summary>
		public bool writingData = false;
		bool IFileHandler.writingData { get => writingData; set => writingData = value; }

		/// <summary>
		/// Queue of data which waits to be written to the data file
		/// </summary>
		private ConcurrentQueue<string> dataQueue = new ConcurrentQueue<string>();

		/// <summary>
		/// StringBuilder which creates the new text to append to the data file
		/// </summary>
		private StringBuilder append = new StringBuilder();


		/// <summary>
		/// Name of the current participant
		/// </summary>
		private string currentParticipantName = "";
		/// <summary>
		/// Name of the current recording
		/// </summary>
		private string currentRecordingName = "";
		/// <summary>
		/// Time at which the recording started and which is used in the filename
		/// </summary>
		private string currentRecordingTime = "";
		/// <summary>
		/// Cache of Application.persistentDataPath
		/// </summary>
		private string persistentDataPath;

		/// <summary>
		/// We need the persistent data path on class initialization as we can't get it from Unity inside this class
		/// </summary>
		/// <param name="persistentDataPath"></param>
		public FileHandler(string persistentDataPath)
		{
			this.persistentDataPath = persistentDataPath;
		}

		/// <summary>
		/// Start a new data log
		/// </summary>
		/// <param name="participantName"></param>
		/// <param name="recordingName"></param>
		public void StartDataLog(string participantName, string recordingName)
		{
			// If we are already writing data we can't start a new data log
			if (writingData)
			{
				throw new Exception("[EyeTracking FileHandler] Already writing a data log! Can't start a new one for the participant " + participantName);
			}

			// Set status to currently writing data
			writingData = true;

			// Clear the current queue from all messages by simply replacing it with a new one
			// Note: This ensures that we don't write old data to the new data recording
			dataQueue = new ConcurrentQueue<string>();

			// Save the new participant and recording name
			currentParticipantName = RemoveAllNonAlphanumeric(participantName);
			currentRecordingName = RemoveAllNonAlphanumeric(recordingName);
			currentRecordingTime = DateTime.Now.ToString("yyyy_MM_dd-HH_mm_ss");

			// Create the file path from the persistent path and the current time
			string filePath = getFilePath(FileType.DataFile);

			// Open the file
			dataWriter = File.AppendText(filePath);
			dataWriter.AutoFlush = true;

			// Create a new Timer
			dataWriteTimer = new Timer(dataSleep);

			// Hook up the write function to the timer. 
			dataWriteTimer.Elapsed += flushDataQueue;
			dataWriteTimer.AutoReset = true;

			// Start the timer
			dataWriteTimer.Start();

			// Log
			Debug.Log("[EyeTracking FileHandler] Started new data log for participant " + participantName + " under the path " + filePath);
		}

		/// <summary>
		/// Close the currently open data log
		/// </summary>
		public void StopDataLog()
		{
			// If we currently aren't writing data we don't need to do anything
			if (!writingData)
			{
				Debug.Log("[EyeTracking FileHandler] No data log currently open, we don't need to close one!");
				return;
			}

			// Stop the timer and dispose of it
			dataWriteTimer.Stop();
			dataWriteTimer.Dispose();

			// Lock the data writer
			lock (dataFileLock)
			{
				// Check if there are remaining messages
				if (!dataQueue.IsEmpty)
				{
					// Clear all remaining messages
					// Note: This isn't thread save as we could end in a loop when there is still new data coming in faster than we are writing it.
					//       However, closing the data log should only happen after we stopped adding data and data is only added slowly enough so we shouldn't run into issues.
					while (dataQueue.TryDequeue(out string newData))
						append.AppendLine(newData);

					dataWriter.Write(append);
					append.Length = 0;
				}
			}

			// Close the file writer
			dataWriter.Close();

			// We aren't writing data anymore
			writingData = false;

			// Log
			Debug.Log("[EyeTracking FileHandler] Closed the currently open data log!");
		}


		/// <summary>
		/// Write new data to the file
		/// </summary>
		/// <param name="data"></param>
		public void WriteData(string data)
		{
			// Simply add the data to the queue of data waiting to be written
			dataQueue.Enqueue(data);
		}

		/// <summary>
		/// Function which actually writes the data
		/// </summary>
		/// <param name="source"></param>
		/// <param name="e"></param>
		private void flushDataQueue(object source, ElapsedEventArgs e)
		{
			// Make sure the previous event isn't still running
			if (System.Threading.Interlocked.CompareExchange(ref dataWriteTimerIsBusy, 1, 0) == 1)
			{
				//Debug.LogError("Previous event still running!");
				return;
			}

			try
			{
				// Lock the data writer
				lock (dataFileLock)
				{
					if (!dataQueue.IsEmpty)
					{
						// Append all data currently in the queue to the StringBuilder
						// Note: This isn't 100% thread save as we could end in a loop when there is still new data coming in faster than we are writing it.
						//       However, data is added slowly enough so we shouldn't run into issues.
						while (dataQueue.TryDequeue(out string newData))
						{
							append.AppendLine(newData);
						}

						// Append the StringBuilder to the file
						dataWriter.Write(append);

						// Clear the StringBuilder
						append.Length = 0;
					}
				}
			}
			finally
			{
				dataWriteTimerIsBusy = 0;
			}
		}

		#endregion Data Log

		#region Info File

		/// <summary>
		/// Lock Object for info file
		/// </summary>
		private readonly object infoFileLock = new object();

		/// <summary>
		/// Write an information file for the current recording
		/// </summary>
		/// <param name="fileContent">Content of the file</param>
		/// <param name="replaceFile">Do we want to replace the current content of the file or append to it?</param>
		public void WriteInformation(string fileContent, bool replaceFile)
		{
			// Make sure we are currently writing data and have a recording name
			if (!writingData)
			{
				throw new Exception("[EyeTracking FileHandler] Not writing a data log! Can't write an info file!");
			}

			lock (infoFileLock)
			{
				// Get the file path
				string filePath = getFilePath(FileType.InfoFile);

				// If we want to replace the file simply write the new text to it
				if (replaceFile)
				{
					File.WriteAllText(filePath, fileContent, Encoding.UTF8);
				}
				// Otherwise append the new text
				else
				{
					File.AppendAllText(filePath, fileContent, Encoding.UTF8);
				}
			}
		}

		#endregion Info File


		#region Filename Handling

		/// <summary>
		/// Create a valid file path based on the given participant name
		/// Note: This also creates folders which might be missing!
		/// </summary>
		/// <param name="fileType"></param>
		/// <returns></returns>
		private string getFilePath(FileType fileType)
		{
			// Make sure we have a participant and recording name
			if (currentParticipantName == "" || currentRecordingName == "" || currentRecordingTime == "")
			{
				throw new Exception("[EyeTracking FileHandler] Participant name, recording name or recording time is missing! Can't get file path.");
			}

			// Base path is the persistent data path of the Unity application
			string path = persistentDataPath;

			// Add the current participant name as sub folder
			path += "/" + currentParticipantName + "/";

			// Check if this folder already exists, if it doesn't create it
			if (!Directory.Exists(path))
			{
				Directory.CreateDirectory(path);
			}

			// Build the filename
			path += currentRecordingTime;
			path += "-";
			path += currentParticipantName;
			path += "-";
			path += currentRecordingName;

			// Append the appropriate file extension
			switch (fileType)
			{
				case FileType.DataFile:
					path += ".csv";
					break;

				case FileType.InfoFile:
					path += ".txt";
					break;

				default:
					path += ".tmp";
					break;
			}

			// return the created path
			return path;
		}

		/// <summary>
		/// Type of the file we want to write to
		/// </summary>
		private enum FileType
		{
			DataFile,
			InfoFile
		}

		/// <summary>
		/// Make a valid file name out of the given string (using Path.GetInvalidFileNameChars()) [unused]
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		private static string MakeValidFileName(string name)
		{
			string invalidChars = System.Text.RegularExpressions.Regex.Escape(new string(System.IO.Path.GetInvalidFileNameChars()));
			string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);

			return System.Text.RegularExpressions.Regex.Replace(name, invalidRegStr, "-");
		}

		/// <summary>
		/// Remove all non alphanumeric chars from the string input
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		public static string RemoveAllNonAlphanumeric(string input)
		{
			System.Text.RegularExpressions.Regex rgx = new System.Text.RegularExpressions.Regex("[^a-zA-Z0-9 _-]");
			return rgx.Replace(input, "_");
		}

		string IFileHandler.RemoveAllNonAlphanumeric(string input)
		{
			return RemoveAllNonAlphanumeric(input);
		}

		#endregion Filename Handling
	}
}
