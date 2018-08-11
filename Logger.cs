using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class Logger : MonoBehaviour
{

	public static string loggingApi = "https://vel.engr.uga.edu/apps/nervedawg/api/";
	public static string tableEvent = "logEvent.php";
	public static string tableBlock = "logBlock.php";
	public static string tableUser = "logUser.php";
	public static string tableTest = "logTest.php";
	public static int userID = 0;
	public static int caseID = -1;
	public static int blockID;
	public static int testID;
	private static Logger instance;
	
	private string dataDirectory = "log";
	public static string fileExtension = ".tsv";
	public static string delimiter = "\t";

	private class listObj
	{
		string label;
		string value;
		listObj(string label, string value)
		{
			this.label = label;
			this.value = value;
		}
	}

	private List<List<string>> list;

	void Start()
	{
		instance = this;
	}
	private IEnumerator doLogEventC(string event_name, string value)
	{
		WWWForm form = new WWWForm();
		form.AddField("user_id", "" + userID);
		form.AddField("event", event_name);
		form.AddField("value", value);
		WWW www = new WWW(loggingApi + tableEvent, form);
		//Debug.Log(form.ToString());
		yield return www;
		//Debug.Log (www.text);
	}
	private IEnumerator doLogBlockC(int block_id, string block_name, float block_time, float block_completion, float block_repetition, int block_num_backtracks, float block_avg_head_angle_dog, float block_avg_head_dist_dog)
	{
		WWWForm form = new WWWForm();
		form.AddField("block_id", "" + block_id);
		form.AddField("block_name", "" + block_name);
		form.AddField("test_method", "" + block_name);
		WWW www = new WWW(loggingApi + tableEvent, form);
		//Debug.Log(form.ToString());
		yield return www;
		//Debug.Log (www.text);
	}

	private IEnumerator doLogGeneralC(List<string[]> list)
	{
		WWWForm form = new WWWForm();
		for (int i = 0; i < list.Count; i++)
		{
			form.AddField(list[i][0], list[i][1]);
		}
		WWW www = new WWW(loggingApi + tableEvent, form);
		//Debug.Log(form.ToString());
		yield return www;
		//Debug.Log(www.text);
	}

	private void doLogEvent(string event_name, string value)
	{
		StartCoroutine(doLogEventC(event_name, value));
	}
	public static void LogEvent(string event_name, string value)
	{
		
		instance.doLogEvent(event_name, value);
	}

	public static void LogEvent2(int event_number, string event_data, int block_number, InteractionSystem test_method, int dogType)
	{
		List<string[]> list = new List<string[]>();
		list.Add(new string[] { "block_number", block_number.ToString() });
		list.Add(new string[] { "block_name", DogTestController.blockNames[block_number].ToString() });
		list.Add(new string[] { "test_method", test_method.ToString() });
		list.Add(new string[] { "dog_type", dogType.ToString() });
		list.Add(new string[] { "user_id", userID.ToString() });
		list.Add(new string[] { "event_number", event_number.ToString() });
		list.Add(new string[] { "event_data", event_data });

		instance.doLogGeneral("event", list);
	}

	//public static void LogTime(string test_name, float duration)
	//{
	//	instance.doLogEvent(test_name, "duration: " + duration);
	//}

	public static void LogBlock(int dogType, int block_number, string block_name, InteractionSystem test_method, float block_time, float block_completion, float block_repetition, int block_num_backtracks, float block_avg_head_angle_dog, float block_avg_head_dist_dog, string event_numbers_in_completion_order)
	{
		List<string[]> list = new List<string[]>();
		list.Add(new string[] { "block_number", block_number.ToString() });
		list.Add(new string[] { "block_name", block_name.ToString() });
		list.Add(new string[] { "test_method", test_method.ToString() });
		list.Add(new string[] { "dog_type", dogType.ToString() });
		list.Add(new string[] { "user_id", userID.ToString() });
		list.Add(new string[] { "block_time", block_time.ToString() });
		list.Add(new string[] { "block_completion", block_completion.ToString() });
		list.Add(new string[] { "block_repetition", block_repetition.ToString() });
		list.Add(new string[] { "block_num_backtracks", block_num_backtracks.ToString() });
		list.Add(new string[] { "block_avg_head_angle_dog", block_avg_head_angle_dog.ToString() });
		list.Add(new string[] { "block_avg_head_dist_dog", block_avg_head_dist_dog.ToString() });
		list.Add(new string[] { "event_numbers_in_completion_order", event_numbers_in_completion_order });

		instance.doLogGeneral("block", list);

		//instance.doLogBlock(block_id, block_name, block_time, block_completion, block_repetition, block_num_backtracks, block_avg_head_angle_dog, block_avg_head_dist_dog);
	}

	/**
	 * Not used
	 */
	private void doLogBlock(int block_id, string block_name, InteractionSystem test_method, float block_time, float block_completion, float block_repetition, int block_num_backtracks, float block_avg_head_angle_dog, float block_avg_head_dist_dog)
	{
		StartCoroutine(doLogBlockC(block_id, block_name, block_time, block_completion, block_repetition, block_num_backtracks, block_avg_head_angle_dog, block_avg_head_dist_dog));
	}

	public static void LogTest(int test_id, InteractionSystem test_method, int dogType, float test_completion_blocks, float test_repetition_blocks, float test_time, float test_num_backtracks, float test_avg_head_angle_dog, float test_avg_head_dist_dog, string block_numbers_in_completion_order)
	{
		List<string[]> list = new List<string[]>();
		list.Add(new string[] { "test_id", test_id.ToString() });
		list.Add(new string[] { "test_method", test_method.ToString() });
		list.Add(new string[] { "dog_type", dogType.ToString() });
		list.Add(new string[] { "user_id", userID.ToString() });
		list.Add(new string[] { "test_completion_blocks", test_completion_blocks.ToString() });
		list.Add(new string[] { "test_repetition_blocks", test_repetition_blocks.ToString() });
		list.Add(new string[] { "test_time", test_time.ToString() });
		list.Add(new string[] { "test_num_backtracks", test_num_backtracks.ToString() });	// what even is this?
		list.Add(new string[] { "test_avg_head_angle_dog", test_avg_head_angle_dog.ToString() });
		list.Add(new string[] { "test_avg_head_dist_dog", test_avg_head_dist_dog.ToString() });
		list.Add(new string[] { "block_numbers_in_completion_order", block_numbers_in_completion_order });

		instance.doLogGeneral("test", list);
	}

	private void doLogGeneral(string type, List<string[]> list)
    {
		StartCoroutine(doLogGeneralC(list));
		logRow(type, list);
	}

	public void logRow(string fileName, List<string[]> data)
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
		string filePath = Path.Combine(directoryPath, fileName+fileExtension);

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
			output += System.DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss.fff") + delimiter;
			for (int i = 0; i < data.Count; i++)
			{
				if (data[i][1].Contains(delimiter))
				{
					throw new Exception("Data contains delimiter: " + data[i][1]);
				}
				output += data[i][1] + delimiter;
			}
			writer.WriteLine(output);
			
		}
		catch (System.Exception e)
		{
			Debug.LogWarning(e.Message);
		}
		writer.Close();
	}
}
