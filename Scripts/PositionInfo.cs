// Copyright (c) Sebastian Kapp.
// Licensed under the MIT License.

namespace ARETT
{
	/// <summary>
	/// Information about the position of a game object
	/// Used for logging this information together with the gaze data
	/// </summary>
	public struct PositionInfo
	{
		public bool positionValid;

		public string gameObjectName;

		public float xPosition;
		public float yPosition;
		public float zPosition;
	
		public float xRotation;
		public float yRotation;
		public float zRotation;

		public float xScale;
		public float yScale;
		public float zScale;
	}
}
