using System;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine.Networking;
using System.Collections;
using System.CodeDom.Compiler;
using System.CodeDom;
using System.IO.Compression;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Core;

namespace unityutilities
{

	/// <summary>
	/// Logs any data to a file.
	/// </summary>
	[AddComponentMenu("unityutilities/Logger")]
	public class Logger : MonoBehaviour
	{

		private static Logger instance;

		/// <summary>
		/// The local folder within which to save log files
		/// </summary>
		private static string logFolder = "Log";
		private const string fileExtension = ".tsv";
		private const string delimiter = "\t";
		private const string dateFormat = "yyyy/MM/dd HH:mm:ss.fff";
		private const string newLineChar = "\n";

		[Header("Web Server Information")]
		public string webLogURL = "http://";
		private const string passwordField = "password";
		public string webLogPassword;
		public string appName;
		public static List<string> subFolders = new List<string>();

		[Header("Log destinations")]
		[Tooltip("Enables constant logging to the local filesystem. This is required for uploading zips at the end.")]
		public bool enableLoggingLocal = true;
		[Tooltip("Enables constant uploading to the web server. Leave this off for only uploading zips at the end.")]
		public bool enableLoggingRemote = true;

		[Space]

		/// <summary>
		/// Splits log files into new folders for each day (UTC) 
		/// </summary>
		public bool separateFoldersPerDay = true;
		/// <summary>
		/// Splits log files into new folders based on the application version number
		/// </summary>
		public bool separateFoldersPerVersion = true;


		/// <summary>
		/// How many lines to wait before actually logging
		/// </summary>
		[Header("Log Interval")]
		[Tooltip("How many lines to put in a cache before actually uploading/saving them.")]
		public int lineLogInterval = 50;
		[Tooltip("How long two wait (in seconds) putting lines in a cache before actually uploading/saving them.")]
		public float timeLogInterval = 5;
		private static int numLinesLogged;
		private float lastLogTime = 0;
		public TimeOrLines useTimeOrLineInterval = TimeOrLines.WhicheverLast;
		public enum TimeOrLines
		{
			Time, Lines, WhicheverFirst, WhicheverLast
		}

		[Header("Debug.Log Logging")]
		[Tooltip("Whether to log debug output to file and web. Can't change during runtime.")]
		public bool enableDebugLogLogging = true;
		public bool fullStackTraceForErrors = true;
		public bool fullStackTraceForOtherMessages = true;
		private static string debugLogFileName = "debug_log";

		/// <summary>
		/// Dictionary of filename and list of lines that haven't been logged yet
		/// </summary>
		private static Dictionary<string, List<string>> dataToLog = new Dictionary<string, List<string>>();
		/// <summary>
		/// Dictionary of filename and stream writers
		/// </summary>
		private static Dictionary<string, StreamWriter> streamWriters = new Dictionary<string, StreamWriter>();

		private void Awake()
		{
			if (instance == null) instance = this;
		}

		/// <summary>
		/// Logs data to a file
		/// </summary>
		/// <param name="fileName">e.g. "movement"</param>
		/// <param name="data">List of columns to log</param>
		public static void LogRow(string fileName, IEnumerable<string> data)
		{
			if (instance == null)
			{
				Debug.LogError("LogRow() called, but no Logger.cs is in the scene.");
				return;
			}
			if (!instance.enableLoggingLocal && !instance.enableLoggingRemote)
			{
				return;
			}

			// add the data to the dictionary
			try
			{
				StringBuilder strBuilder = new StringBuilder();
				strBuilder.Append(DateTime.Now.ToString(dateFormat));
				strBuilder.Append(delimiter);
				strBuilder.Append(SystemInfo.deviceUniqueIdentifier);
				strBuilder.Append(delimiter);
				if (Application.isEditor)
				{
					strBuilder.Append(SystemInfo.operatingSystem + " (Editor)");
				}
				else
				{
					strBuilder.Append(SystemInfo.operatingSystem);
				}
				strBuilder.Append(delimiter);
				foreach (string elem in data)
				{
					if (elem.Contains(delimiter))
					{
						throw new Exception("Data contains delimiter: " + elem);
					}

					strBuilder.Append(elem);
					strBuilder.Append(delimiter);
				}
				if (!dataToLog.ContainsKey(fileName))
				{
					dataToLog.Add(fileName, new List<string>());
				}
				dataToLog[fileName].Add(strBuilder.ToString());
			}
			catch (Exception e)
			{
				Debug.LogError(e.Message);
			}

			numLinesLogged++;
		}

		/// <summary>
		/// Just references the LogRow overload above
		/// </summary>
		/// <param name="fileName"></param>
		/// <param name="elements"></param>
		public static void LogRow(string fileName, params string[] elements)
		{
			if (elements is null)
			{
				return;
			}

			LogRow(fileName, new List<string>(elements));
		}

		/// <summary>
		/// Actuallys logs the data to disk or remote because the conditions were met (time or lines)
		/// </summary>
		private void WriteOutLogCache()
		{
			if (!enableLoggingLocal && !enableLoggingRemote)
			{
				return;
			}

			lastLogTime = Time.time;

			StringBuilder allOutputData = new StringBuilder();

			foreach (string fileName in dataToLog.Keys)
			{

				if (enableLoggingLocal)
				{
					string directoryPath = GetCurrentLogFolder();

					string filePath = Path.Combine(directoryPath, fileName + fileExtension);

					// create writer if it doesn't exist
					if (!streamWriters.ContainsKey(fileName))
					{
						streamWriters.Add(fileName, new StreamWriter(filePath, true));
					}
				}

				allOutputData.Clear();

				// actually log data
				try
				{
					foreach (var row in dataToLog[fileName])
					{
						if (enableLoggingLocal)
						{
							streamWriters[fileName].WriteLine(row);
						}
						if (enableLoggingRemote)
						{
							allOutputData.Append(row);
							allOutputData.Append(newLineChar);
						}
					}
					dataToLog[fileName].Clear();

					if (enableLoggingRemote)
					{
						StartCoroutine(Upload(fileName, allOutputData.ToString(), appName));
					}
				}
				catch (Exception e)
				{
					Debug.LogError(e.Message);
				}
			}

			numLinesLogged = 0;
		}

		/// <summary>
		/// Gets the folder that log files are saved to currently
		/// </summary>
		/// <param name="fileName">The name of the log file e.g. movement</param>
		/// <returns>The full path to the folder that contains the log files</returns>
		private static string GetCurrentLogFolder()
		{
			if (instance == null) return null;

			string directoryPath;
#if UNITY_ANDROID && !UNITY_EDITOR
					directoryPath = Path.Combine(Application.persistentDataPath, logFolder);
#else
			directoryPath = Path.Combine(Application.dataPath, logFolder);
#endif
			if (instance.separateFoldersPerVersion)
			{
				directoryPath = Path.Combine(directoryPath, Application.version);
			}
			if (instance.separateFoldersPerDay)
			{
				directoryPath = Path.Combine(directoryPath, DateTime.UtcNow.ToString("yyyy_MM_dd"));
			}

			if (!Directory.Exists(directoryPath))
			{
				Directory.CreateDirectory(directoryPath);
			}
			return directoryPath;
		}

		IEnumerator Upload(string name, string data, string appName)
		{
			WWWForm form = new WWWForm();
			form.AddField(passwordField, webLogPassword);
			form.AddField("file", name);
			form.AddField("data", data);
			form.AddField("app", appName);
			form.AddField("version", Application.version);
			using (UnityWebRequest www = UnityWebRequest.Post(webLogURL, form))
			{
				yield return www.SendWebRequest();
				if (www.result != UnityWebRequest.Result.Success)
				{
					Debug.Log(www.error);
				}
			}

		}

		public static void UploadZip(bool dailyOnly = true)
		{
			if (instance == null)
			{
				Debug.LogError("UploadZip() called, but no Logger.cs is in the scene.");
				return;
			}

			string dir = GetCurrentLogFolder();

			CreateZipFromFolder(dir);
		}


		public static void CreateZipFromFolder(string folderName)
		{
			// copy the files to a new folder first
			string tempDir = $"{folderName}_{SystemInfo.deviceUniqueIdentifier}";
			Directory.CreateDirectory(tempDir);
			DirectoryCopy(folderName, tempDir, true);

			// zip them
			FastZip fz = new FastZip();
			fz.CreateZip(tempDir + ".zip", tempDir, true, "");

			// delete the temp folder
			Directory.Delete(tempDir, true);
		}

		private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
		{
			// Get the subdirectories for the specified directory.
			DirectoryInfo dir = new DirectoryInfo(sourceDirName);

			if (!dir.Exists)
			{
				throw new DirectoryNotFoundException(
					"Source directory does not exist or could not be found: "
					+ sourceDirName);
			}

			DirectoryInfo[] dirs = dir.GetDirectories();

			// If the destination directory doesn't exist, create it.       
			Directory.CreateDirectory(destDirName);

			// Get the files in the directory and copy them to the new location.
			FileInfo[] files = dir.GetFiles();
			foreach (FileInfo file in files)
			{
				string tempPath = Path.Combine(destDirName, file.Name);
				file.CopyTo(tempPath, false);
			}

			// If copying subdirectories, copy them and their contents to new location.
			if (copySubDirs)
			{
				foreach (DirectoryInfo subdir in dirs)
				{
					string tempPath = Path.Combine(destDirName, subdir.Name);
					DirectoryCopy(subdir.FullName, tempPath, copySubDirs);
				}
			}
		}

		/// <summary>
		/// Not used currently
		/// </summary>
		private void UpdateSubFolders()
		{
			subFolders.Clear();
			subFolders.Add(SystemInfo.deviceUniqueIdentifier);
			subFolders.Add(DateTime.Now.ToString("yyyy-MM-dd_hh-mm"));
		}

		private void Update()
		{
			switch (useTimeOrLineInterval)
			{
				case TimeOrLines.Time:
					if (Time.time - lastLogTime > timeLogInterval)
					{
						WriteOutLogCache();
					}
					break;
				case TimeOrLines.Lines:
					if (numLinesLogged > lineLogInterval)
					{
						WriteOutLogCache();
					}
					break;
				case TimeOrLines.WhicheverFirst:
					if (Time.time - lastLogTime > timeLogInterval || numLinesLogged > lineLogInterval)
					{
						WriteOutLogCache();
					}
					break;
				case TimeOrLines.WhicheverLast:
					if (Time.time - lastLogTime > timeLogInterval && numLinesLogged > lineLogInterval)
					{
						WriteOutLogCache();
					}
					break;
			}


			if (Input.GetKey(KeyCode.BackQuote) && Input.GetKeyDown(KeyCode.S))
			{
				UploadZip();
			}

			if (Input.GetKey(KeyCode.F6))
			{
				Debug.Log("Test Message");
			}
		}

		private void OnEnable()
		{
			if (enableDebugLogLogging)
				Application.logMessageReceived += LogCallback;
		}

		private void OnDisable()
		{
			if (enableDebugLogLogging)
				Application.logMessageReceived -= LogCallback;
		}

		private void LogCallback(string condition, string stackTrace, LogType logType)
		{
			List<string> cols = new List<string>() { logType.ToString(), condition };
			if (logType == LogType.Error && fullStackTraceForErrors)
			{
				cols.Add(ToLiteral2(stackTrace));
			}
			else if (logType != LogType.Error && fullStackTraceForOtherMessages)
			{
				cols.Add(ToLiteral2(stackTrace));
			}
			LogRow(debugLogFileName, cols);
		}

		private static string ToLiteral2(string input)
		{
			input = input.Replace("\n", "\\n ");
			input = input.Replace("\r\n", "\\n ");
			input = input.Replace("\t", "\t ");
			return input;
		}

		/// <summary>
		/// Turn a string into a CSV cell output
		/// </summary>
		/// <param name="str">String to output</param>
		/// <returns>The CSV cell formatted string</returns>
		public static string StringToCSVCell(string str, string delimiter = "\t")
		{
			bool mustQuote = (str.Contains(delimiter) || str.Contains("\"") || str.Contains("\r") || str.Contains("\n"));
			if (mustQuote)
			{
				StringBuilder sb = new StringBuilder();
				sb.Append("\"");
				foreach (char nextChar in str)
				{
					sb.Append(nextChar);
					if (nextChar == '"')
						sb.Append("\"");
				}
				sb.Append("\"");
				return sb.ToString();
			}

			return str;
		}

		//close writers
		private void OnApplicationQuit()
		{

			WriteOutLogCache();

			foreach (KeyValuePair<string, StreamWriter> writer in streamWriters)
			{
				writer.Value.Close();
			}
		}

	}

}