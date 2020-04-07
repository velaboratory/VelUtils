namespace unityutilities
{
	public interface INetworkPack
	{
		/// <summary>
		/// Packs the data describing the object into a byte array.
		/// </summary>
		/// <returns>The byte array with all the data.</returns>
		byte[] PackData();

		/// <summary>
		/// Receives the byte array of data about the object.
		/// </summary>
		/// <param name="data">The byte array with all the data.</param>
		void UnpackData(byte[] data);
	}
}