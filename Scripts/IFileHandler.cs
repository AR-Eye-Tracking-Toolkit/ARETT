// Copyright (c) Sebastian Kapp.
// Licensed under the MIT License.

namespace ARETT
{
	/// <summary>
	/// Interface for the file handlers which accept text which is supposed to be written to disk and writes it
	/// </summary>
	public interface IFileHandler
	{
		/// <summary>
		/// Time between writing to the data file
		/// </summary>
		float dataSleep { get; set; }

		/// <summary>
		/// Lock whether we are currently writing data
		/// </summary>
		bool writingData { get; set; }

		/// <summary>
		/// Start a new data log
		/// </summary>
		/// <param name="participantName"></param>
		void StartDataLog(string participantName, string recordingName);

		/// <summary>
		/// Close the currently open data log
		/// </summary>
		void StopDataLog();


		/// <summary>
		/// Write new data to the file
		/// </summary>
		/// <param name="data"></param>
		void WriteData(string data);

		/// <summary>
		/// Write and information file for the current recording
		/// </summary>
		/// <param name="fileContent">Content of the file</param>
		/// <param name="replaceFile">Do we want to replace the current content of the file or append to it?</param>
		void WriteInformation(string fileContent, bool replaceFile);

		/// <summary>
		/// Remove all non alphanumeric chars from the string input
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		string RemoveAllNonAlphanumeric(string input);
	}
}
