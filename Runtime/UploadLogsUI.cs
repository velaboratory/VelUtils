using System;
using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.IO;
using System.Linq;

namespace VelUtils
{
	public class UploadLogsUI : MonoBehaviour
	{
		public Transform optionsObjects;
		public Transform progressObjects;

		[Space] public TMP_Text fileSizeLabel;
		public Toggle uploadAllToggle;
		public TMP_Text progressText;
		private bool wasUploading = false;


		private void OnEnable()
		{
			RefreshSize();
			if (wasUploading && !Logger.uploading)
			{
				HideWhenDone();
				wasUploading = false;
			}
		}

		private void Update()
		{
			if (wasUploading && !Logger.uploading)
			{
				StartCoroutine(HideAfterDelay());
				wasUploading = false;
			}
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
			// Add file sizes.
			FileInfo[] fis = d.GetFiles();
			long size = fis.Sum(fi => fi.Length);
			// Add subdirectory sizes.
			DirectoryInfo[] dis = d.GetDirectories();
			size += dis.Sum(DirSize);
			return size;
		}

		public void Upload()
		{
			Logger.UploadZip(uploadAllToggle.isOn);
			wasUploading = true;
			progressObjects.gameObject.SetActive(true);
			optionsObjects.gameObject.SetActive(false);
			progressText.text = "Uploading...";
		}

		private IEnumerator HideAfterDelay()
		{
			progressText.text = "Done";

			yield return new WaitForSeconds(1);

			HideWhenDone();
		}

		private void HideWhenDone()
		{
			gameObject.SetActive(false);
			progressObjects.gameObject.SetActive(false);
			optionsObjects.gameObject.SetActive(true);
		}
	}
}