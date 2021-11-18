// Copyright (c) Sebastian Kapp.
// Licensed under the MIT License.

#if (UNITY_WSA && DOTNETWINRT_PRESENT) || WINDOWS_UWP
using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Windows.Storage;
using UnityEngine;

namespace ARETT
{
	/// <summary>
	/// The file handler accepts text which is supposed to be written to disk and writes it
	/// </summary>
	public class FileHandlerUWP : IFileHandler
	{
#region Data Log

		/// <summary>
		/// FileIO for the data file
		/// </summary>
		private IStorageFile dataFile;

		/// <summary>
		/// Time between writing to the data file
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
		private System.Threading.SemaphoreSlim dataFileSemaphore = new System.Threading.SemaphoreSlim(1);

		/// <summary>
		/// Lock whether we are currently writing data
		/// </summary>
		public bool writingData = false;
		bool IFileHandler.writingData { get => writingData; set => writingData = value; }

		/// <summary>
		/// Queue of data which waits to be written to the data file
		/// </summary>
		private ConcurrentQueue<string> dataQueue = new ConcurrentQueue<string>();

		/// <summary>
		/// Queue of info files which wait to be written
		/// Note: Done in a queue so we don't try to write to files while they are still being written to from the previous call
		/// </summary>
		private ConcurrentQueue<(string fileContent, bool replaceFile)> writeInfoQueue = new ConcurrentQueue<(string fileContent, bool replaceFile)>();

		/// <summary>
		/// StringBuilder which creates the new text to append to the data file
		/// </summary>
		private StringBuilder append = new StringBuilder();

		/// <summary>
		/// Name of the folder under which the files should be saved in the documents folder
		/// </summary>
		private string baseFolderName;
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
		/// When creating the file handler we need a folder name under which the files should be saved in the documents folder
		/// Note: This could simply be the application name
		/// </summary>
		/// <param name="baseFolderName"></param>
		public FileHandlerUWP(string baseFolderName)
		{
			// Save base folder name
			this.baseFolderName = baseFolderName;
		}

		/// <summary>
		/// Start a new data log
		/// </summary>
		/// <param name="participantName"></param>
		public void StartDataLog(string participantName, string recordingName)
		{
			// If we are already writing data we can't start a new data log
			if (writingData)
			{
				throw new Exception("[EyeTracking FileHandlerUWP] Already writing a data log! Can't start a new one for the participant " + participantName);
			}

			// Clear the current queue from all messages by simply replacing it with a new one
			// Note: This ensures that we don't write old data to the new data recording
			dataQueue = new ConcurrentQueue<string>();

			// Save the new participant and recording name
			currentParticipantName = RemoveAllNonAlphanumeric(participantName);
			currentRecordingName = RemoveAllNonAlphanumeric(recordingName);
			currentRecordingTime = DateTime.Now.ToString("yyyy_MM_dd-HH_mm_ss");

			// Start the async tasks
			StartDataLogAsync();
		}

		/// <summary>
		/// Do the asynchronous tasks to start a new data log
		/// </summary>
		/// <param name="participantName"></param>
		/// <param name="recordingName"></param>
		private async void StartDataLogAsync()
		{
			// Get the file
			dataFile = await getFile(FileType.DataFile);

			// Create a new Timer
			dataWriteTimer = new Timer(dataSleep);

			// Hook up the write function to the timer. 
			dataWriteTimer.Elapsed += flushQueues;
			dataWriteTimer.AutoReset = true;

			// Start the timer
			dataWriteTimer.Start();

			// We are now writing data
			writingData = true;

			// Log
			Debug.Log("[EyeTracking FileHandlerUWP] Started new data log for participant " + currentParticipantName + " under the path " + dataFile.Path);
		}

		/// <summary>
		/// Close the currently open data log
		/// </summary>
		public void StopDataLog()
		{
			// If we currently aren't writing data we don't need to do anything
			if (!writingData)
			{
				Debug.Log("[EyeTracking FileHandlerUWP] No data log currently open, we don't need to close one!");
				return;
			}

			// Stop the timer and dispose of it
			dataWriteTimer.Stop();
			dataWriteTimer.Dispose();

			// Do the asynchronous tasks
			StopDataLogAsync();
		}

		/// <summary>
		/// Do the asynchronous tasks to close the currently open data log
		/// </summary>
		private async void StopDataLogAsync()
		{
			if (!dataQueue.IsEmpty)
			{
				// Clear all remaining messages
				// Note: This isn't thread save as we could end in a loop when there is still new data coming in.
				//       However, closing the data log should only happen after we stopped adding data and data is only added slowly enough so we shouldn't run into issues.
				while (dataQueue.TryDequeue(out string newData))
					append.AppendLine(newData);

				// Wait until we can lock the data file
				await dataFileSemaphore.WaitAsync();

				// Append the StringBuilder to the file
				try
				{
					await FileIO.AppendTextAsync(dataFile, append.ToString(), Windows.Storage.Streams.UnicodeEncoding.Utf8);
				}

				// Catch errors
				catch (Exception e)
				{
					Debug.LogError("[FileHandlerUWP] Exception on appending text while stopping data log!\n" + e);
				}

				// Release the lock for the data file
				finally
				{
					dataFileSemaphore.Release();
				}

				// Reset the StringBuilder
				append.Length = 0;
			}

			// Reset the current log file
			dataFile = null;

			// Write all remaining info files
			if (!writeInfoQueue.IsEmpty)
			{
				while (writeInfoQueue.TryDequeue(out (string fileContent, bool replaceFile) infoDetails))
				{
					// Wait until we can lock the info file
					await infoFileSemaphore.WaitAsync();

					try
					{
						// Get the file
						IStorageFile file = await getFile(FileType.InfoFile);

						// If we want to replace the file simply write the new text to it
						if (infoDetails.replaceFile)
						{
							await FileIO.WriteTextAsync(file, infoDetails.fileContent, Windows.Storage.Streams.UnicodeEncoding.Utf8);
						}
						// Otherwise append the new text
						else
						{
							await FileIO.AppendTextAsync(file, infoDetails.fileContent, Windows.Storage.Streams.UnicodeEncoding.Utf8);
						}
					}

					// Catch errors
					catch (Exception e)
					{
						Debug.LogError("[FileHandlerUWP] Exception on writing info file after stopping data log!\n" + e);
					}

					// Release the lock for the data file
					finally
					{
						infoFileSemaphore.Release();
					}
				}
			}

			// We aren't writing data anymore
			writingData = false;

			// Log
			Debug.Log("[EyeTracking FileHandlerUWP] Closed the currently open data log!");
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
#endregion Data Log

#region Info File

		/// <summary>
		/// Lock Object for info file
		/// </summary>
		private System.Threading.SemaphoreSlim infoFileSemaphore = new System.Threading.SemaphoreSlim(1);

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
				throw new Exception("[EyeTracking FileHandlerUWP] Not writing a data log! Can't write an info file!");
			}

			// Queue writing the information
			writeInfoQueue.Enqueue((fileContent, replaceFile));
		}

#endregion Info File


#region Queue Flushing

		/// <summary>
		/// Function which actually writes the data
		/// </summary>
		/// <param name="source"></param>
		/// <param name="e"></param>
		private async void flushQueues(object source, ElapsedEventArgs e)
		{
			// Make sure the previous event isn't still running
			if (System.Threading.Interlocked.CompareExchange(ref dataWriteTimerIsBusy, 1, 0) == 1)
			{
				//Debug.LogError("Previous write event for eye tracking data still running!");
				return;
			}

			try
			{
				if (!dataQueue.IsEmpty)
				{
					// Append all data currently in the queue to the StringBuilder
					// Note: This isn't 100% thread save as we could end in a loop when there is still new data coming in.
					//       However, data is added slowly enough so we shouldn't run into issues.
					while (dataQueue.TryDequeue(out string newData))
					{
						append.AppendLine(newData);
					}

					// Wait until we can lock the data file
					await dataFileSemaphore.WaitAsync();
					
					// Append the StringBuilder to the file
					try
					{
						await FileIO.AppendTextAsync(dataFile, append.ToString(), Windows.Storage.Streams.UnicodeEncoding.Utf8);
					}

					// Catch errors
					catch (Exception dataLogException)
					{
						Debug.LogError("[FileHandlerUWP] Exception while writing data log!\n" + dataLogException);
					}

					// Release the lock for the data file
					finally
					{
						dataFileSemaphore.Release();
					}

					// Clear the StringBuilder
					append.Length = 0;
				}

				// Check if a info file is waiting to be written
				if (!writeInfoQueue.IsEmpty)
				{
					while (writeInfoQueue.TryDequeue(out (string fileContent, bool replaceFile) infoDetails))
					{
						// Wait until we can lock the info file
						await infoFileSemaphore.WaitAsync();

						try {
							// Get the file
							IStorageFile file = await getFile(FileType.InfoFile);

							// If we want to replace the file simply write the new text to it
							if (infoDetails.replaceFile)
							{
								await FileIO.WriteTextAsync(file, infoDetails.fileContent, Windows.Storage.Streams.UnicodeEncoding.Utf8);
							}
							// Otherwise append the new text
							else
							{
								await FileIO.AppendTextAsync(file, infoDetails.fileContent, Windows.Storage.Streams.UnicodeEncoding.Utf8);
							}
						}

						// Catch errors
						catch (Exception infoFileException)
						{
							Debug.LogError("[FileHandlerUWP] Exception while writing info file!\n" + infoFileException);
						}

						// Release the lock for the data file
						finally
						{
							infoFileSemaphore.Release();
						}
					}
				}
			}
			finally
			{
				dataWriteTimerIsBusy = 0;
			}
		}

#endregion Queue Flushing

#region Filename Handling

		/// <summary>
		/// Create a valid file object based on the given participant name
		/// Note: This also creates folders which might be missing!
		/// </summary>
		/// <param name="fileType"></param>
		/// <returns></returns>
		private async Task<IStorageFile> getFile(FileType fileType)
		{
			// Make sure we have a participant and recording name
			if (currentParticipantName == "" || currentRecordingName == "" || currentRecordingTime == "")
			{
				throw new Exception("[EyeTracking FileHandlerUWP] Participant name, recording name or recording time is missing! Can't get file path!");
			}

			// Base folder is the Documents folder of the device
			StorageFolder storageFolder = KnownFolders.DocumentsLibrary;

			// Add Folder for specified name or just get the folder if it already exists
			StorageFolder appStorageFolder = await storageFolder.CreateFolderAsync(baseFolderName, CreationCollisionOption.OpenIfExists);

			// Add the current participant name (or get the existing folder)
			StorageFolder participantStorageFolder = await appStorageFolder.CreateFolderAsync(currentParticipantName, CreationCollisionOption.OpenIfExists);

			// Build the filename
			string fileName = currentRecordingTime + "-" + currentParticipantName + "-" + currentRecordingName;

			// Append the appropriate file extension
			switch (fileType)
			{
				case FileType.DataFile:
					fileName += ".csv";
					break;

				case FileType.InfoFile:
					fileName += ".txt";
					break;

				default:
					fileName += ".tmp";
					break;
			}

			// Create the file or open it if it exists and return it
			return await participantStorageFolder.CreateFileAsync(fileName, CreationCollisionOption.OpenIfExists);
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
#endif
