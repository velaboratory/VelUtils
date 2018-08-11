using System;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class Logger
{
	private static string dataDirectory = "log";
	private static string fileExtension = ".tsv";
	private static string delimiter = "\t";

	public static void LogRow(string fileName, IEnumerable<string> data)
	{
		// make directory
		if (dataDirectory[0] == '/')
		{
			dataDirectory = dataDirectory.Substring(1);
		}

		if (dataDirectory[dataDirectory.Length - 1] == '/')
		{
			dataDirectory = dataDirectory.Substring(0, dataDirectory.Length - 2);
		}

		string directoryPath = Application.dataPath + "/" + dataDirectory + "/";
		string filePath = Path.Combine(directoryPath, fileName + fileExtension);

		// create the writer
		StreamWriter writer;
		if (File.Exists(filePath))
		{
			writer = new StreamWriter(filePath, true);
		}
		else
		{
			writer = new StreamWriter(filePath);
		}

		// actually log data
		try
		{
			string output = "";
			output += DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff") + delimiter;
			foreach (var elem in data)
			{
				if (elem.Contains(delimiter))
				{
					throw new Exception("Data contains delimiter: " + elem);
				}

				output += elem + delimiter;
			}

			writer.WriteLine(output);
		}
		catch (Exception e)
		{
			Debug.LogWarning(e.Message);
		}

		writer.Close();
	}
}