using System.Collections.Generic;
using UnityEngine;

namespace VelUtils
{
	/// <summary>
	/// Define columns that are added to every log file
	/// </summary>
	public abstract class LoggerConstantFields : MonoBehaviour
	{
		/// <summary>
		/// Should return the current values for each of the desired columns
		/// </summary>
		public abstract IEnumerable<string> GetConstantFields();
		
		/// <summary>
		/// Should return the headers for each of the desired columns
		/// </summary>
		public abstract IEnumerable<string> GetConstantFieldHeaders();
	}
}