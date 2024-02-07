The logging module can be used to log .csv or .tsv files on your local device or on a remote server. Logs can be written continuously to the server (batched for performance) or using an optional manual upload function that lets you provide a UI for direct consent to upload data.

1. Add the `Logger.cs` to your scene. If you switch scenes during runtime, make the object ["DontDestroyOnLoad"](../reference/Runtime/DontDestroyOnLoad.md)
- Configure the Logger component to match your needs. 
- Add your headers with the `Logger.SetHeaders()` method. The first parameter is the filename that these headers will be used for. Be careful to not use the `UnityEngine.Logger` class accidentally.
    ```csharp
    // This will write to a file called events.tsv
    Logger.SetHeaders("events", "event_name", "details", "x", "y", "z");
    ```
- Add your data with the `Logger.LogRow()` method. This could be in `Update()` or in some other function that you want to log. The first parameter is the filename, and the second is either a variadic string parameter, an array of strings, or a `StringList` helper class.
    ```csharp
    // This will write to a file called events.tsv using the headers above
    Logger.LogRow("events", "teleport", "left-hand", pos.x, pos.y, pos.z);
    ```
- Optionally, use the `StringList` helper class to add your data. This lets you add more complex data types, and VelUtils will automatically split them into columns or parse them into strings. For example, you can pass in a `Vector3` or `Quaternion`, and VelUtils will automatically parse the individual x, y, z, (w) values into string columns. 
    ```csharp
    Logger.LogRow("events", new StringList(new List<dynamic> { "teleport", "left-hand", transform.position }).List);
    ```


## Constant Fields
If you want to add the same column to every single log file, it can be repetitive to fetch and add that data to every single `Logger.LogRow()`. Examples of this type of data might by the room name, nickname, and user id for VelNet, input method, or a study-specific id. It is not necessary to add the timestamp or device id because VelUtils will automatically add these fields to every single log file.

1. Create a new class that inherits from the `LoggerConstantFields` class.
- Override the `GetConstantFields()` and `GetConstantFieldHeaders()` methods to return an array of constant fields that you want added. See the example below for a possible implementation:
```cs
using System.Collections.Generic;
using VelUtils;
using VelNet;

public class VelNetConstantFields : LoggerConstantFields
{
	public override IEnumerable<string> GetConstantFields()
	{
		return new[]
		{
			VelNetManager.Room,
			VelNetManager.LocalPlayer?.userid.ToString() ?? "-1",
			VelNetMan.NickName,
			GameManager.instance.player.trackedHandsVisible ? "hands" : "controllers"
		};
	}

	private readonly string[] headers = {
		"room",
		"player_id",
		"nickname",
		"input_method"
	};

	public override IEnumerable<string> GetConstantFieldHeaders()
	{
		return headers;
	}
}
```
- Add your new class as a componenent to the object with the `Logger.cs` component.
- Assign the new class to the "Constant Fields" field in the `Logger.cs` component.