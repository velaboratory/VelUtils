using System;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine.Networking;
using System.Collections;
using System.Globalization;
using System.Security.Cryptography;
using ICSharpCode.SharpZipLib.Zip;
using System.Threading;

namespace VelUtils
{
	/// <summary>
	/// Logs any data to a file. An instance of this class must be in the scene.
	/// </summary>
	[AddComponentMenu("VelUtils/Logger")]
	public class Logger : MonoBehaviour
	{
		private static Logger instance;

		/// <summary>
		/// The local folder within which to save log files
		/// </summary>
		private static string logFolder = "Log";

		private const string fileExtension = ".tsv";
		private const string dateFormat = "yyyy/MM/dd HH:mm:ss.fff";
		private const string Y_M_D = "yyyy_MM_dd";
		private const string fileTimeFormat = "yyyy-MM-dd_HH-mm-ss-ff";
		private const string delimiter = "\t";
		private const string newLineChar = "\n";

		private static readonly string[] baseHeaders =
		{
			"timestamp",
			"hw_id",
			"os",
		};

		private static readonly Dictionary<string, string[]> fileColumns = new Dictionary<string, string[]>();
		// private static readonly Dictionary<string, LogDataSchema> fileSchemas = new Dictionary<string, LogDataSchema>();

		[Header("Web Server Information")] public string webLogURL = "https://";
		public string webLogPassword;
		public string appName;
		public static List<string> subFolders = new List<string>();
		public static bool uploading;
		public static float uploadProgress { get; private set; }

		[Header("Log destinations")] [Tooltip("Enables constant logging to the local filesystem. This is required for uploading zips at the end.")]
		public bool enableLoggingLocal = true;

		[Tooltip("Enables constant uploading to the web server. Leave this off for only uploading zips at the end.")]
		public bool enableLoggingRemote = true;

		/// <summary>
		/// Splits log files into new folders for each day (UTC) 
		/// This is overriden by separateFoldersPerLaunch if that is true.
		/// </summary>
		[Space] public bool separateFoldersPerDay;

		/// <summary>
		/// Splits log files into new folders based on the application version number
		/// </summary>
		public bool separateFoldersPerVersion = true;

		/// <summary>
		/// Splits log files into new folders based on the launch time of the application
		/// </summary>
		public bool separateFoldersPerLaunch = true;

		private DateTime launchTime;


		/// <summary>
		/// How many lines to wait before actually logging
		/// </summary>
		[Header("Log Interval")] [Tooltip("How many lines to put in a cache before actually uploading/saving them.")]
		public int lineLogInterval = 50;

		[Tooltip("How long two wait (in seconds) putting lines in a cache before actually uploading/saving them.")]
		public float timeLogInterval = 5;

		private static int numLinesLogged;
		private float lastLogTime;
		public TimeOrLines useTimeOrLineInterval = TimeOrLines.WhicheverLast;

		public enum TimeOrLines
		{
			Time,
			Lines,
			WhicheverFirst,
			WhicheverLast
		}

		[Header("Debug.Log Logging")] [Tooltip("Whether to log debug output to file and web. Can't change during runtime.")]
		public bool enableDebugLogLogging = true;

		public bool fullStackTraceForErrors = true;
		public bool fullStackTraceForOtherMessages = true;
		private static string debugLogFileName = "debug_log";

		[Header("Extra Info")] public bool logSystemInfoOnStart;


		[Space]
		[Tooltip("Not required. Define a set of fields that get appended to every log line, such as photon user ids etc. by inheriting from the LoggerConstantFields class.")]
		public LoggerConstantFields constantFields;


		/// <summary>
		/// Dictionary of filename and list of lines that haven't been logged yet
		/// </summary>
		private static Dictionary<string, List<string>> dataToLog = new Dictionary<string, List<string>>();

		/// <summary>
		/// Dictionary of filename and stream writers
		/// </summary>
		private static Dictionary<string, StreamWriter> streamWriters = new Dictionary<string, StreamWriter>();


		[Header("Debug")] public bool testMessageWithF6;
		public bool uploadWithF7;
		public bool uploadAllWithF8;

		public static bool lastUploadSucceeded { get; private set; }
		public static UnityWebRequest uploadWWW { get; private set; }

		private void Awake()
		{
			if (instance == null) instance = this;

			launchTime = DateTime.UtcNow;

			if (logSystemInfoOnStart)
			{
				SetHeaders("system_info",
					"deviceUniqueIdentifier",
					"deviceModel",
					"deviceName",
					"deviceType",
					"operatingSystem",
					"operatingSystemFamily",
					"graphicsDeviceName",
					"graphicsDeviceVendor",
					"graphicsDeviceType",
					"graphicsDeviceVersion",
					"graphicsMemorySize",
					"graphicsMultiThreaded",
					"processorType",
					"processorCount",
					"processorFrequency",
					"systemMemorySize",
					"batteryLevel",
					"batteryStatus"
				);
				LogRow(
					"system_info",
					SystemInfo.deviceUniqueIdentifier,
					SystemInfo.deviceModel,
					SystemInfo.deviceName,
					SystemInfo.deviceType.ToString(),

					// OS
					SystemInfo.operatingSystem,
					SystemInfo.operatingSystemFamily.ToString(),

					// GPU
					SystemInfo.graphicsDeviceName,
					SystemInfo.graphicsDeviceVendor,
					SystemInfo.graphicsDeviceType.ToString(),
					SystemInfo.graphicsDeviceVersion,
					SystemInfo.graphicsMemorySize.ToString(),
					SystemInfo.graphicsMultiThreaded.ToString(),

					// CPU
					SystemInfo.processorType,
					SystemInfo.processorCount.ToString(),
					SystemInfo.processorFrequency.ToString(),
					SystemInfo.systemMemorySize.ToString(),

					// battery
					SystemInfo.batteryLevel.ToString(CultureInfo.InvariantCulture),
					SystemInfo.batteryStatus.ToString()
				);
			}
		}

		public static void SetHeaders(string fileName, params string[] headers)
		{
			fileColumns[fileName] = headers;
		}

		private static List<string> GetHeaders(string fileName)
		{
			List<string> headersList = new List<string>();
			headersList.AddRange(baseHeaders);
			if (instance.constantFields != null)
			{
				headersList.AddRange(instance.constantFields.GetConstantFieldHeaders());
			}

			if (fileColumns.TryGetValue(fileName, out string[] dataColumns))
			{
				headersList.AddRange(dataColumns);
			}

			return headersList;
		}

		private static string GetHeaderLine(string fileName)
		{
			return string.Join(delimiter, GetHeaders(fileName));
		}

		/// <summary>
		/// Logs data to a file
		/// </summary>
		/// <param name="fileName">e.g. "movement"</param>
		/// <param name="data">List of columns to log</param>
		public static void LogRow(string fileName, params object[] data)
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

			List<string> columns = GetHeaders(fileName);
			int colCount = 0;

			// add the data to the dictionary
			try
			{
				StringBuilder strBuilder = new StringBuilder();

				// add global constant fields
				strBuilder.Append(DateTime.UtcNow.ToString(dateFormat));
				strBuilder.Append(delimiter);
				strBuilder.Append(CreateDeviceId());
				strBuilder.Append(delimiter);
				if (Application.isEditor)
				{
					strBuilder.Append(SystemInfo.operatingSystem + " (Editor)");
				}
				else
				{
					strBuilder.Append(SystemInfo.operatingSystem);
				}

				colCount += 3;

				strBuilder.Append(delimiter);

				// add custom constant fields
				if (instance.constantFields != null)
				{
					foreach (string elem in instance.constantFields.GetConstantFields())
					{
						strBuilder.Append(elem);
						strBuilder.Append(delimiter);
						colCount++;
					}
				}

				// add actual data
				foreach (string elem in data)
				{
					if (elem == null)
					{
						strBuilder.Append("null");
						strBuilder.Append(delimiter);
					}
					else
					{
						string elem1 = elem;
						if (elem1.Contains(delimiter))
						{
							if (delimiter == "\t")
							{
								elem1 = elem1.Replace("\t", "    ");
							}
							else
							{
								throw new Exception("Data contains delimiter: " + elem1);
							}
						}

						if (elem1.Contains("\n"))
						{
							elem1 = elem1.Replace("\n", "\\n");
						}

						strBuilder.Append(elem1);
						strBuilder.Append(delimiter);
					}

					colCount++;
				}

				if (colCount != columns.Count)
				{
					Debug.LogError("Column count does not match the number of headers specified for this file!", instance);
				}

				// add this data to the cache
				if (!dataToLog.ContainsKey(fileName))
				{
					dataToLog.Add(fileName, new List<string>());
				}

				dataToLog[fileName].Add(strBuilder.ToString());
			}
			catch (Exception e)
			{
				Debug.LogError(e);
			}

			numLinesLogged++;
		}

		/// <summary>
		/// Actually logs the data to disk or remote because the conditions were met (time or lines)
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

						if (!File.Exists(filePath))
						{
							string headers = GetHeaderLine(fileName);
							streamWriters[fileName].WriteLine(headers);
						}
					}
				}

				allOutputData.Clear();

				// actually log data
				try
				{
					foreach (string row in dataToLog[fileName])
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

					foreach (StreamWriter writer in streamWriters.Values)
					{
						writer.Flush();
					}

					dataToLog[fileName].Clear();

					if (enableLoggingRemote)
					{
						string headers = GetHeaderLine(fileName);
						StartCoroutine(Upload(fileName, allOutputData.ToString(), appName, headers));
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
		/// <param name="parentLogFolder">Uses the parent of all date-based folders.</param>
		/// <returns>The full path to the folder that contains the log files</returns>
		public static string GetCurrentLogFolder(bool parentLogFolder = false)
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


			if (!Directory.Exists(directoryPath))
			{
				Directory.CreateDirectory(directoryPath);
			}

			if (parentLogFolder) return directoryPath;


			if (instance.separateFoldersPerDay && !instance.separateFoldersPerLaunch)
			{
				directoryPath = Path.Combine(directoryPath, DateTime.UtcNow.ToString(Y_M_D));
			}
			else if (instance.separateFoldersPerLaunch)
			{
				directoryPath = Path.Combine(directoryPath, instance.launchTime.ToString(fileTimeFormat));
			}

			if (!Directory.Exists(directoryPath))
			{
				Directory.CreateDirectory(directoryPath);
			}

			return directoryPath;
		}

		IEnumerator Upload(string name, string data, string appName, string headers)
		{
			lastUploadSucceeded = false;
			if (string.IsNullOrEmpty(data)) yield break;

			WWWForm form = new WWWForm();
			form.AddField("password", webLogPassword);
			form.AddField("appendfile", name);
			form.AddField("appenddata", data);
			form.AddField("headers", headers);
			form.AddField("app", appName);
			form.AddField("version", Application.version);
			using (UnityWebRequest www = UnityWebRequest.Post(webLogURL, form))
			{
				uploadWWW = www;
				yield return www.SendWebRequest();
				uploadWWW = null;
				if (www.result != UnityWebRequest.Result.Success)
				{
					Debug.Log(www.error);
				}
			}

			lastUploadSucceeded = true;
		}

		public static void UploadZip(bool uploadAll = false)
		{
			if (instance == null)
			{
				Debug.LogError("UploadZip() called, but no Logger.cs is in the scene.");
				return;
			}

			instance.StartCoroutine(instance.UploadZipCo(uploadAll));
		}

		private IEnumerator UploadZipCo(bool uploadAll = false)
		{
			uploading = true;

			string dir = GetCurrentLogFolder(parentLogFolder: uploadAll);

			Thread thread = new Thread(() => CreateZipFromFolder(dir));
			thread.Start();
			// wait for folder to finish zipping
			while (thread.IsAlive)
			{
				yield return null;
			}

			string zipFile = dir + ".zip";
			byte[] data = File.ReadAllBytes(zipFile);

			WWWForm form = new WWWForm();
			form.AddField("password", webLogPassword);
			form.AddField("app", appName);
			form.AddField("version", Application.version);
			form.AddBinaryData("upload", data, $"{DateTime.UtcNow:yyyy-MM-dd_HH-mm-ss-ff}_{SystemInfo.deviceUniqueIdentifier}.zip");
			using (UnityWebRequest www = UnityWebRequest.Post(webLogURL, form))
			{
				uploadWWW = www;
				yield return www.SendWebRequest();
				uploadWWW = null;
				if (www.result != UnityWebRequest.Result.Success)
				{
					Debug.Log(www.error);
				}
				else
				{
					Debug.Log("Uploaded .zip of logs.");
					Debug.Log(www.downloadHandler.text);
				}
			}

			lastUploadSucceeded = true;
			uploading = false;
		}


		public static string CreateZipFromFolder(string folderName)
		{
			// copy the files to a new folder first
			//string tempDir = $"{folderName}_{DateTime.UtcNow:s}";
			string tempDir = $"{folderName}_temp";
			Directory.CreateDirectory(tempDir);
			DirectoryCopy(folderName, tempDir, true);

			// zip them
			FastZip fz = new FastZip();
			fz.CreateZip(folderName + ".zip", tempDir, true, "");

			// delete the temp folder
			Directory.Delete(tempDir, true);

			return folderName + ".zip";
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


			if (uploadWithF7 && Input.GetKey(KeyCode.F7))
			{
				UploadZip();
			}

			if (uploadAllWithF8 && Input.GetKey(KeyCode.F8))
			{
				UploadZip(uploadAll: true);
			}

			if (testMessageWithF6 && Input.GetKey(KeyCode.F6))
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

		/// <summary>
		/// Computes 15-char device id compatibly with pocketbase
		/// This should be kept compatible with the VEL-Connect Unity package
		/// </summary>
		private static string CreateDeviceId()
		{
			MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
			StringBuilder sb = new StringBuilder(SystemInfo.deviceUniqueIdentifier);
			sb.Append(Application.productName);
#if UNITY_EDITOR
			// allows running multiple builds on the same computer
			sb.Append(Application.dataPath);
			sb.Append("EDITOR");
#endif
			string id = Convert.ToBase64String(md5.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString())));
			return id[..15];
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

	/// <summary>
	/// Utility class to make logging different datatypes easier.
	/// Inputs can be floats, Vectors, bools, etc, and the output defines consistent serialization for all of them at once.
	/// </summary>
	public class StringList
	{
		public readonly List<string> List;

		public StringList()
		{
			List = new List<string>();
		}

		public StringList(List<dynamic> l)
		{
			List = new List<string>();
			foreach (object o in l)
			{
				switch (o)
				{
					case string s:
						Add(s);
						break;
					case float f:
						Add(f);
						break;
					case Vector3 v:
						Add(v);
						break;
					case Quaternion q:
						Add(q);
						break;
					case Enum e:
						Add(e);
						break;
					case bool b:
						Add(b);
						break;
				}
			}
		}

		public void Add(string s)
		{
			List.Add(s);
		}

		public void Add(float f)
		{
			List.Add(f.ToString(CultureInfo.InvariantCulture));
		}

		public void Add(Vector3 v)
		{
			List.Add(v.x.ToString(CultureInfo.InvariantCulture));
			List.Add(v.y.ToString(CultureInfo.InvariantCulture));
			List.Add(v.z.ToString(CultureInfo.InvariantCulture));
		}

		public void Add(Quaternion q)
		{
			List.Add(q.x.ToString(CultureInfo.InvariantCulture));
			List.Add(q.y.ToString(CultureInfo.InvariantCulture));
			List.Add(q.z.ToString(CultureInfo.InvariantCulture));
			List.Add(q.w.ToString(CultureInfo.InvariantCulture));
		}

		public void Add(bool b)
		{
			List.Add(b.ToString());
		}

		public void Add(Enum v)
		{
			List.Add(v.ToString());
		}
	}
}