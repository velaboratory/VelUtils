using UnityEngine;

namespace VelUtils.Ar
{
	/// <summary>
	/// This should be overridden by a class that saves the AR alignment with platform features, such as Oculus Spatial Anchors
	/// </summary>
	public interface SaveArAlignment
	{
		public void AddAnchor(Transform t);
		public void RemoveAnchor(Transform t);
		public void Save();
		public void Load();
	}
}