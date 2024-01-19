using System.Collections.Generic;

namespace VelUtils
{
	/// <summary>
	/// Denotes classes that can be packed to and from a Dictionary of string,object
	/// </summary>
	public interface IDictionaryPackable
	{
		/// <summary>
		/// Packs the data describing the object into a dictionary.
		/// </summary>
		/// <returns>The dictionary with all the data.</returns>
		Dictionary<string, object> PackData();

		/// <summary>
		/// Receives the dictionary of data about the object.
		/// </summary>
		/// <param name="data">The dictionary with all the data.</param>
		void UnpackData(Dictionary<string, object> data);
	}
}