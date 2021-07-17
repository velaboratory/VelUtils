using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.IO;

namespace unityutilities
{
	public class UploadLogsUI : MonoBehaviour
	{
		public Transform optionsObjects;
		public Transform progressObjects;

		[Space]

		public TMP_Text fileSizeLabel;
		public Toggle uploadAllToggle;
		public TMP_Text progressText;


		private void OnEnable()
		{
			RefreshSize();
		}

		public void RefreshSize()
		{
			string parentDirectory = Logger.GetCurrentLogFolder(parentLogFolder: uploadAllToggle.isOn);

			if (string.IsNullOrEmpty(parentDirectory)) return;

			long size = DirSize(new DirectoryInfo(parentDirectory));

			fileSizeLabel.text = $"{size / 1000:N} KB";
		}

		/// <summary>
		/// https://stackoverflow.com/questions/468119/whats-the-best-way-to-calculate-the-size-of-a-directory-in-net
		/// </summary>
		/// <param name="d">The directory to scan</param>
		/// <returns>The file size in bytes</returns>
		public static long DirSize(DirectoryInfo d)
		{
			long size = 0;
			// Add file sizes.
			FileInfo[] fis = d.GetFiles();
			foreach (FileInfo fi in fis)
			{
				size += fi.Length;
			}
			// Add subdirectory sizes.
			DirectoryInfo[] dis = d.GetDirectories();
			foreach (DirectoryInfo di in dis)
			{
				size += DirSize(di);
			}
			return size;
		}

		public void Upload()
		{
			Logger.UploadZip(uploadAllToggle.isOn);
			StartCoroutine(UploadCo());
		}

		private IEnumerator UploadCo()
		{
			progressObjects.gameObject.SetActive(true);
			optionsObjects.gameObject.SetActive(false);
			progressText.text = "Uploading...";
			while (Logger.uploading)
			{
				yield return null;
			}

			progressText.text = "Done";

			yield return new WaitForSeconds(1);

			gameObject.SetActive(false);
			progressObjects.gameObject.SetActive(false);
			optionsObjects.gameObject.SetActive(true);
		}


	}
}
