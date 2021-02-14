using System;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine.Networking;
using System.Collections;
using System.CodeDom.Compiler;
using System.CodeDom;

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
		public string appName;
		public static List<string> subFolders = new List<string>();

		/// <summary>
		/// How many lines to wait before actually logging
		/// </summary>
		[Header("Log Interval")]
		[Tooltip("How many lines to put in a cache before actually uploading them.")]
		public int lineLogInterval = 50;
		[Tooltip("How long two wait (in seconds) putting lines in a cache before actually uploading them.")]
		public float timeLogInterval = 5;
		private static int numLinesLogged;
		private float lastLogTime = 0;
		public TimeOrLines useTimeOrLineInterval;
		public enum TimeOrLines
		{
			Time, Lines, WhicheverFirst, Both
		}

		public static bool usePerDeviceFolder = true;
		public static bool usePerLaunchFolder = true;
		public static bool enableLogging = true;
		public static bool enableLoggingLocal = true;
		public static bool enableLoggingRemote = true;
		private static string debugLogFileName = "debug_log";

		[Header("Debug.Log Logging")]
		[Tooltip("Whether to log debug output to file and web. Can't change during runtime.")]
		public bool enableDebugLogLogging = true;
		public bool fullStackTraceForErrors = true;
		public bool fullStackTraceForOtherMessages = true;

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

			lastLogTime = Time.time;

			StringBuilder allOutputData = new StringBuilder();

			foreach (var fileName in dataToLog.Keys) {

				if (enableLoggingLocal) {
					string filePath, directoryPath;
#if UNITY_ANDROID && !UNITY_EDITOR
					filePath = Path.Combine(Application.persistentDataPath, fileName+fileExtension);
#else
					directoryPath = Path.Combine(Application.dataPath, Path.Combine(logFolder, Application.version));

					if (!Directory.Exists(directoryPath)) {
						Directory.CreateDirectory(directoryPath);
					}

					filePath = Path.Combine(directoryPath, fileName + fileExtension);
#endif

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
						StartCoroutine(Upload(fileName, allOutputData.ToString(), appName));
					}
				}
				catch (Exception e) {
					Debug.LogError(e.Message);
				}
			}

			numLinesLogged = 0;
		}

		IEnumerator Upload(string name, string data, string appName) {
			WWWForm form = new WWWForm();
			form.AddField(passwordField, webLogPassword);
			form.AddField("file", name);
			form.AddField("data", data);
			form.AddField("app", appName);
			form.AddField("version", Application.version);
			using (UnityWebRequest www = UnityWebRequest.Post(webLogURL, form)) {
				yield return www.SendWebRequest();
				if (www.isNetworkError || www.isHttpError) {
					Debug.Log(www.error);
				}
			}

		}

		/// <summary>
		/// Not used currently
		/// </summary>
		private void UpdateSubFolders() {
			subFolders.Clear();
			subFolders.Add(SystemInfo.deviceUniqueIdentifier);
			subFolders.Add(DateTime.Now.ToString("yyyy-MM-dd_hh-mm"));
		}

		private void Update() {
			switch (useTimeOrLineInterval)
			{
				case TimeOrLines.Time:
					if (Time.time - lastLogTime > timeLogInterval)
					{
						ActuallyLog();
					}
					break;
				case TimeOrLines.Lines:
					if (numLinesLogged > lineLogInterval)
					{
						ActuallyLog();
					}
					break;
				case TimeOrLines.WhicheverFirst:
					if (Time.time - lastLogTime > timeLogInterval || numLinesLogged > lineLogInterval)
					{
						ActuallyLog();
					}
					break;
				case TimeOrLines.Both:
					if (Time.time - lastLogTime > timeLogInterval && numLinesLogged > lineLogInterval)
					{
						ActuallyLog();
					}
					break;
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

		public void EnableLogging(bool enable) {
			enableLogging = enable;
		}

		private static string ToLiteral(string input)
		{
			using (var writer = new StringWriter())
			{
				using (var provider = CodeDomProvider.CreateProvider("CSharp"))
				{
					provider.GenerateCodeFromExpression(new CodePrimitiveExpression(input), writer, new CodeGeneratorOptions { IndentString = "\t" });
					var literal = writer.ToString();
					literal = literal.Replace(string.Format("\" +{0}\t\"", Environment.NewLine), "");
					literal = literal.Replace("\\r\\n", "\\r\\n\"+\r\n\"");
					return literal;
				}
			}
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
		private void OnApplicationQuit() {

			ActuallyLog();

			foreach (var writer in streamWriters) {
				writer.Value.Close();
			}
		}

	}

}