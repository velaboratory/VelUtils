using System.Collections.Generic;
using UnityEngine;

namespace VelUtils
{
	public abstract class LoggerConstantFields : MonoBehaviour
	{
		public abstract IEnumerable<string> GetConstantFields();
		public abstract IEnumerable<string> GetConstantFieldHeaders();
	}
}