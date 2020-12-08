using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace unityutilities
{
    public interface IDictionaryPackable
    {
        /// <summary>
        /// Packs the data describing the object into a dictionary.
        /// </summary>
        /// <returns>The dictionary with all the data.</returns>
        Dictionary<string,string> PackData();

        /// <summary>
        /// Receives the dictioary of data about the object.
        /// </summary>
        /// <param name="data">The dictionary with all the data.</param>
        void UnpackData(Dictionary<string,string> data);
    }
}