using System;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine.Networking;
using System.Collections;

namespace unityutilities {

	/// <summary>
	/// Logs any data to a file.
	/// </summary>
	[AddComponentMenu("unityutilities/Logger")]
	public class Logger : MonoBehaviour {
		private static string logFolder = "Log";
		private const string fileExtension = ".tsv";
		private const string delimiter = "\t";
		private const string dateFormat = "yyyy/MM/dd HH:mm:ss.fff";
		private const string newLineChar = "\n";
		public string webLogURL = "http://";
		private const string passwordField = "password";
		public string webLogPassword;

		/// <summary>
		/// How many lines to wait before actually logging
		/// </summary>
		public int lineLogInterval = 50;
		private static int numLinesLogged;

		public static bool enableLogging = true;
		public static bool enableLoggingLocal = true;
		public static bool enableLoggingRemote = true;
		private static string debugLogFileName = "debug_log";
		// can't change during runtime
		public static bool enableDebugLogLogging = true;

		/// <summary>
		/// Dictionary of filename and list of lines that haven't been logged yet
		/// </summary>
		private static Dictionary<string, List<string>> dataToLog = new Dictionary<string, List<string>>();
		/// <summary>
		/// Dictionary of filename and stream writers
		/// </summary>
		private static Dictionary<string, StreamWriter> streamWriters = new Dictionary<string, StreamWriter>();

		/// <summary>
		/// Logs data to a file
		/// </summary>
		/// <param name="fileName">e.g. "movement"</param>
		/// <param name="data">List of columns to log</param>
		public static void LogRow(string fileName, IEnumerable<string> data) {

			if (!enableLogging) {
				return;
			}

			// add the data to the dictionary
			try {
				StringBuilder strBuilder = new StringBuilder();
				strBuilder.Append(DateTime.Now.ToString(dateFormat));
				strBuilder.Append(delimiter);
				foreach (var elem in data) {
					if (elem.Contains(delimiter)) {
						throw new Exception("Data contains delimiter: " + elem);
					}

					strBuilder.Append(elem);
					strBuilder.Append(delimiter);
				}
				if (!dataToLog.ContainsKey(fileName)) {
					dataToLog.Add(fileName, new List<string>());
				}
				dataToLog[fileName].Add(strBuilder.ToString());
			}
			catch (Exception e) {
				Debug.LogError(e.Message);
			}

			numLinesLogged++;
		}

		/// <summary>
		/// Just references the LogRow overload above
		/// </summary>
		/// <param name="fileName"></param>
		/// <param name="elements"></param>
		public static void LogRow(string fileName, params string[] elements) {
			if (elements is null) {
				return;
			}

			LogRow(fileName, new List<string>(elements));
		}

		private void ActuallyLog() {

			if (!enableLogging) {
				return;
			}

			StringBuilder allOutputData = new StringBuilder();

			foreach (var fileName in dataToLog.Keys) {

				if (enableLoggingLocal) {
					string filePath, directoryPath;
#if UNITY_ANDROID && !UNITY_EDITOR
					filePath = Path.Combine(Application.persistentDataPath, fileName+fileExtension);
#else
					directoryPath = Path.Combine(Application.dataPath, logFolder);

					if (!Directory.Exists(directoryPath)) {
						Directory.CreateDirectory(directoryPath);
					}
#endif

					filePath = Path.Combine(directoryPath, fileName + fileExtension);

					// create writer if it doesn't exist
					if (!streamWriters.ContainsKey(fileName)) {
						streamWriters.Add(fileName, new StreamWriter(filePath, true));
					}
				}

				allOutputData.Clear();

				// actually log data
				try {
					foreach (var row in dataToLog[fileName]) {
						if (enableLoggingLocal) {
							streamWriters[fileName].WriteLine(row);
						}
						if (enableLoggingRemote) {
							allOutputData.Append(row);
							allOutputData.Append(newLineChar);
						}
					}
					dataToLog[fileName].Clear();

					if (enableLoggingRemote) {
						StartCoroutine(Upload(fileName, allOutputData.ToString()));
					}
				}
				catch (Exception e) {
					Debug.LogError(e.Message);
				}
			}

			numLinesLogged = 0;
		}

		IEnumerator Upload(string name, string data) {
			WWWForm form = new WWWForm();
			form.AddField(passwordField, webLogPassword);
			form.AddField("file", name);
			form.AddField("data", data);
			using (UnityWebRequest www = UnityWebRequest.Post(webLogURL, form)) {
				yield return www.SendWebRequest();
				if (www.isNetworkError || www.isHttpError) {
					Debug.Log(www.error);
				}
			}

		}

		private void Update() {
			if (numLinesLogged > lineLogInterval) {
				ActuallyLog();
			}
		}

		private void Awake() {
			if (enableDebugLogLogging)
				Application.logMessageReceived += LogCallback;
		}

		private void LogCallback(string condition, string stackTrace, LogType logType) {
			LogRow(debugLogFileName, condition);
		}

		public void EnableLogging(bool enable) {
			enableLogging = enable;
		}

		//close writers
		private void OnApplicationQuit() {
			if (enableDebugLogLogging)
				Application.logMessageReceived -= LogCallback;

			ActuallyLog();

			foreach (var writer in streamWriters) {
				writer.Value.Close();
			}
		}

	}

}