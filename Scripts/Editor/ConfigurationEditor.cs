// Copyright (c) Sebastian Kapp.
// Licensed under the MIT License.

using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace ARETT.Editor
{
	/// <summary>
	/// Custom Unity editor to help configure the Unity project and scene for eye tracking
	/// </summary>
	[CustomEditor(typeof(Configuration))]
	public class PlatformSwitcherEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			// Configuration script we are the editor of
			Configuration configutaion = (Configuration)target;

			//----
			// Default inspector
			//----
			DrawDefaultInspector();

			//----
			// Draw line
			//----
			//DrawUILine(Color.gray, 2, 15);

			//----
			// Start custom GUI
			//---


			GUILayout.Label("UWP Network Permissions:");

			GUILayout.BeginHorizontal();

			// Editor button to check if the network permissions are set
			if (GUILayout.Button("Check Network Permissions", GUILayout.Height(50)))
			{
#if UNITY_WSA
				Microsoft.MixedReality.Toolkit.Utilities.Editor.UWPCapabilityUtility.RequireCapability(
						UnityEditor.PlayerSettings.WSACapability.InternetClient,
						this.GetType());
				Microsoft.MixedReality.Toolkit.Utilities.Editor.UWPCapabilityUtility.RequireCapability(
						UnityEditor.PlayerSettings.WSACapability.InternetClientServer,
						this.GetType());
				Microsoft.MixedReality.Toolkit.Utilities.Editor.UWPCapabilityUtility.RequireCapability(
						UnityEditor.PlayerSettings.WSACapability.PrivateNetworkClientServer,
						this.GetType());
				Debug.Log("[Permission Configuration] Checked network permissions!");
#else
				Debug.LogError("[Permission Configuration] Not on UWP build target! Can't check network permission.");
#endif
			}

			GUILayout.EndHorizontal();



			GUILayout.Label("UWP Gaze Permission:");

			GUILayout.BeginHorizontal();

			// Editor button to check if the gaze permission is set
			if (GUILayout.Button("Check Gaze Permission", GUILayout.Height(50)))
			{
#if UNITY_WSA
				Microsoft.MixedReality.Toolkit.Utilities.Editor.UWPCapabilityUtility.RequireCapability(
						UnityEditor.PlayerSettings.WSACapability.GazeInput,
						this.GetType());
				Debug.Log("[Permission Configuration] Checked gaze permission!");
#else
				Debug.LogError("[Permission Configuration] Not on UWP build target! Can't check gaze permission.");
#endif
			}

			GUILayout.EndHorizontal();



			GUILayout.Label("Layer Configuration:");

			GUILayout.BeginHorizontal();

			// Editor button to configure required layers
			if (GUILayout.Button("Configure Layers", GUILayout.Height(50)))
			{
				// Report string
				string report = "";

				// Get the DataProvider and AccuracyGrid for layer configuration
				DataProvider dataProvider = FindObjectOfType<DataProvider>();
				AccuracyGrid accuracyGrid = FindObjectOfType<AccuracyGrid>();

				if (dataProvider == null || accuracyGrid == null)
				{
					Debug.LogError("[Layer configuration] Did not find data provider or accuracy grid in scene, can't update layers!");
				}
				else
				{
					// Make a list of all layers which we need to add
					List<string> layers = new List<string>();
					List<string> newLayers = new List<string>();

					// Get all layers from the data provider and accuracy grid which we want to configure
					foreach (string aoiLayer in dataProvider.eyeTrackingAOILayers) layers.Add(aoiLayer);
					foreach (string visLayer in dataProvider.eyeTrackingVisLayers) layers.Add(visLayer);
					foreach (string checkLayer in dataProvider.eyeTrackingCheckLayers) layers.Add(checkLayer);
					layers.Add(accuracyGrid.gridLayer);

					// Identify which layers need to be added
					foreach (var layer in layers)
					{
						// Try to get the layer number of this layer, if it doesn't exist the number is -1
						if (LayerMask.NameToLayer(layer) > -1)
						{
							report += $"\nLayer {layer} already exists.";
						}
						else
						{
							report += $"\nLayer {layer} is missing!";
							newLayers.Add(layer);
						}
					}

					// If we don't have new layers we are done, otherwise continue
					if (newLayers.Count == 0)
					{
						report += "\n\nDone! No layers added.";
						Debug.Log("[Layer Configuration] Successfully checked layer masks, no layers added.\nDetails:\n" + report);
					}
					else
					{
						// Make a list of all layers which are empty and could be filled with eye tracking layers
						List<int> emptyLayerNumbers = new List<int>();
						for (int i = 8; i <= 31; i++)
						{
							if (LayerMask.LayerToName(i) == "")
							{
								emptyLayerNumbers.Add(i);
							}
						}

						// If we don't have enough empty layers abort, otherwise continue
						if (emptyLayerNumbers.Count < newLayers.Count)
						{
							report += "\n\nError! Not enough empty layers.";
							Debug.LogError("[Layer Configuration] Error! Not enough empty layers.\nDetails:" + report);
						}
						else
						{
							// Assign every missing layer the next possible layer number
							Dictionary<int, string> newLayerAssignments = new Dictionary<int, string>();
							int emptyLayerNumbersIndex = 0;
							foreach (var newLayer in newLayers)
							{
								newLayerAssignments.Add(emptyLayerNumbers[emptyLayerNumbersIndex], newLayer);
								emptyLayerNumbersIndex++;
							}

							// Open the unity tag manager file
							SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
							SerializedProperty layersProp = tagManager.FindProperty("layers");

							// Set the new assignments
							foreach (KeyValuePair<int, string> newLayerAssignment in newLayerAssignments)
							{
								layersProp.GetArrayElementAtIndex(newLayerAssignment.Key).stringValue = newLayerAssignment.Value;

								report += $"\nSet layer {newLayerAssignment.Key} to layer {newLayerAssignment.Value}.";
							}

							// Save the new file
							tagManager.ApplyModifiedProperties();

							report += $"\n\nDone! {newLayerAssignments.Count} layers added.";
							Debug.Log($"[Layer Configuration] Successfully added {newLayerAssignments.Count} layer mask{((newLayerAssignments.Count > 1) ? "s" : "")}.\nDetails:\n" + report);
						}
					}
				}
			}

			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();

			if (GUILayout.Button("Configure Camera", GUILayout.Height(50)))
			{
				// Get the DataProvider and AccuracyGrid for layer configuration
				DataProvider dataProvider = FindObjectOfType<DataProvider>();
				AccuracyGrid accuracyGrid = FindObjectOfType<AccuracyGrid>();

				if (dataProvider == null || accuracyGrid == null)
				{
					Debug.LogError("[Camera configuration] Did not find data provider or accuracy grid in scene, can't update camera!");
				}
				else
				{
					// Calculate Layer Mask without AOIs
					// Note: As Layers are represented by bits, this calculation is also happening on bit level
					// We start with the current mask
					LayerMask cameraLayerMask = Camera.main.cullingMask;
					// Hide all AOI layers
					foreach (string aoiLayer in dataProvider.eyeTrackingAOILayers)
					{
						cameraLayerMask &= ~(1 << LayerMask.NameToLayer(aoiLayer));
					}
					// Also hide the layers which visualizes the eye tracking data
					foreach (string visLayer in dataProvider.eyeTrackingVisLayers)
					{
						cameraLayerMask &= ~(1 << LayerMask.NameToLayer(visLayer));
					}
					// Hide the check layers
					foreach (string checkLayer in dataProvider.eyeTrackingCheckLayers)
					{
						cameraLayerMask &= ~(1 << LayerMask.NameToLayer(checkLayer));
					}
					// And hide the accuracy grid layer
					cameraLayerMask &= ~(1 << LayerMask.NameToLayer(accuracyGrid.gridLayer));

					// Set the new camera mask
					Camera.main.cullingMask = cameraLayerMask;
				}
			}

			GUILayout.EndHorizontal();
		}
	}
}
