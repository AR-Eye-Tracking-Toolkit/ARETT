// Copyright (c) Sebastian Kapp.
// Licensed under the MIT License.

using System;
using UnityEngine;

namespace ARETT
{
	/// <summary>
	/// Class which controls a Unity camera simulating the integrated camera of the device we are running on
	/// </summary>
	public class WebcamCamera : MonoBehaviour
	{
		[Header("Camera configuration")]
		/// <summary>
		/// Local position of the integrated camera in relation to the virtual main camera position
		/// Note: Currently set for the camera integrated into the HoloLens 2 which was used during development
		/// </summary>
		[SerializeField]
		private Vector3 localPosition = new Vector3(0.0008540161f, 0.04609771f, 0.05014295f);

		[SerializeField]
		/// <summary>
		/// Local rotation of the integrated camera in relation to the virtual main camera position
		/// Note: Currently set for the camera integrated into the HoloLens 2 which was used during development
		/// </summary>
		//private Vector3 localRotation = new Vector3(0.2832f, -0.1305067f, -0.0200118f);
		private Vector3 localRotation = new Vector3(0.2832f, -0.1305067f, -0.6f);

		/// <summary>
		/// On Awake set the camera position and projection matrix to match the integrated camera
		/// </summary>
		private void Awake()
		{
			// Set position
			transform.SetParent(Camera.main.transform, false);
			transform.localPosition = localPosition;
			transform.localRotation = Quaternion.Euler(localRotation);

			// Set projection matrix
			// Note: Currently set for the camera integrated into the HoloLens 2 which was used during development
			Matrix4x4 projectionMatrix = new Matrix4x4
			{
				m00 = BitConverter.ToSingle(new byte[] { 13, 60, 195, 63 }, 0),
				m01 = BitConverter.ToSingle(new byte[] { 0, 0, 0, 0 }, 0),
				m02 = BitConverter.ToSingle(new byte[] { 0, 113, 70, 60 }, 0),
				m03 = BitConverter.ToSingle(new byte[] { 0, 0, 0, 0 }, 0),
				m10 = BitConverter.ToSingle(new byte[] { 0, 0, 0, 0 }, 0),
				m11 = BitConverter.ToSingle(new byte[] { 64, 213, 45, 64 }, 0),
				m12 = BitConverter.ToSingle(new byte[] { 0, 242, 114, 189 }, 0),
				m13 = BitConverter.ToSingle(new byte[] { 0, 0, 0, 0 }, 0),
				m20 = BitConverter.ToSingle(new byte[] { 0, 0, 0, 0 }, 0),
				m21 = BitConverter.ToSingle(new byte[] { 0, 0, 0, 0 }, 0),
				//m22 = BitConverter.ToSingle(new byte[] { 0, 0, 128, 191 }, 0),
				//m23 = BitConverter.ToSingle(new byte[] { 0, 0, 0, 0 }, 0),
				// m22 and m23 modified from calibration value for correct render distance in the Unity camera
				m22 = BitConverter.ToSingle(new byte[] { 141, 6, 128, 191 }, 0),
				m23 = BitConverter.ToSingle(new byte[] { 11, 210, 76, 190 }, 0),
				m30 = BitConverter.ToSingle(new byte[] { 0, 0, 0, 0 }, 0),
				m31 = BitConverter.ToSingle(new byte[] { 0, 0, 0, 0 }, 0),
				m32 = BitConverter.ToSingle(new byte[] { 0, 0, 128, 191 }, 0),
				m33 = BitConverter.ToSingle(new byte[] { 0, 0, 0, 0 }, 0)
			};
			GetComponent<Camera>().projectionMatrix = projectionMatrix;
		}

	}
}
