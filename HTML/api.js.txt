// Copyright (c) Sebastian Kapp.
// Licensed under the MIT License.

////
// Handle a connection error
////
handleConnectionError = function (error) {
    // Remember that we had an error
    globalThis.error = true;

    // Show error message
    if (error instanceof TypeError) {
        displayError(true, "<b>Connection Error!</b> Unable to connect to device.");
    }
    else {
        displayError(true, "<b>Unknown error on update!</b> " + error);
    }

    // Disable auto update
    clearInterval(globalThis.updateInterval);
}


////
// Update the status display
////
updatingStatus = false;
updateStatus = async () => {
    // Make sure the command can only run once
    if (globalThis.updatingStatus) return;
    globalThis.updatingStatus = true;

    // Set display status to updating
    displayUpdateButton(updateRunning = true);

    // Get and parse the new data
    try {
        response = await fetch('/api/status');
        newStatus = await response.json();

        // Update the display
        displayDeviceName(newStatus["deviceName"]);
        displayParticipantName(newStatus["participantName"]);
        displayCalibrationValid(newStatus["isGazeCalibrationValid"]);
        displayEyesApiAvailable(newStatus["eyesApiAvailable"]);
        displayRecordingName(newStatus["recordingName"]);
        displayRecordingInfo(recordingStartTime = newStatus["recordingStartTime"],
            recordingStop = newStatus["recordingStopTime"],
            recordingDuration = newStatus["recordingDuration"]);

        // Update recording status
        globalThis.currentlyRecording = newStatus["recording"];
        displayRecordingActive(newStatus["recording"]);

        // If an error for logging information exists but we are recording clear it
        if (globalThis.infoError && newStatus["recording"]) {
            displayLogError(false, "");
        }

        // Update accuracy status
        globalThis.accuracyVisible = newStatus["accuracyGridVisible"];
        globalThis.accuracyDistance = newStatus["accuracyGridDistance"];
        displayAccuracyButtons(true, newStatus["accuracyGridVisible"], newStatus["accuracyGridDistance"]);

        // Update the AOI check status
        globalThis.checkVisible = newStatus["checkVisible"];
        displayCheckButton(newStatus["checkVisible"]);

        // Enable the calibration button
        displayCalibrationButton(true);

        // Add logged info
        newInfoString = "";
        for (i = (newStatus["infoLogs"].length - 1); i >= 0; i--) {
            newInfoString += newStatus["infoLogs"][i];
            if (i > 0) newInfoString += "<br />";
        }
        document.getElementById('infoList').innerHTML = newInfoString;

        // If we previously had an error clear it
        if (globalThis.error) {
            // Reset the flag
            globalThis.error = false;

            // Hide the error message
            displayError(false, "")

            // Restart the automatic updates
            globalThis.updateInterval = setInterval(updateStatus, 2000);
        }
    }
    catch (error) {
        handleConnectionError(error);
    }
    finally {
        // Update status
        updateLastUpdateTimestamp();
        displayUpdateButton(updateRunning = false);

        // Reset the running flag
        globalThis.updatingStatus = false;
    }
}


////
// Set the participant name to the name in the form
////
settingParticipantName = false;
setParticipantName = async () => {
    // Make sure the command can only run once
    if (globalThis.settingParticipantName) return;
    globalThis.settingParticipantName = true;

    // Build formData of new name
    formData = "participant_name=";
    formData += encodeURI(document.getElementById('participantNameInput').value);

    // Post the data, wait for the response and parse it
    try {
        response = await fetch("/api/participant_name",
            {
                body: formData,
                method: "put"
            });
        newStatus = await response.json();

        // If there was an error alert the user to it
        if (!newStatus["wasUpdated"]) {
            alert("New participant name was not set successfully!");
        }
        else {
            // Reset the input
            document.getElementById('participantNameInput').value = "";
        }

    }
    catch (error) {
        handleConnectionError(error);
    }
    finally {

        // Update the status display
        updateStatus();

        globalThis.settingParticipantName = false;
    }
}

////
// Set the recording name to the name in the form
////
settingRecordingName = false;
setRecordingName = async () => {
    // Make sure the command can only run once
    if (globalThis.settingRecordingName) return;
    globalThis.settingRecordingName = true;

    // Build formData of new name
    formData = "recording_name=";
    formData += encodeURI(document.getElementById('recordingNameInput').value);

    // Post the data, wait for the response and parse it
    try {
        response = await fetch("/api/recording_name",
            {
                body: formData,
                method: "put"
            });
        newStatus = await response.json();

        // If there was an error alert the user to it
        if (!newStatus["wasUpdated"]) {
            alert("New recording name was not set successfully!");
        }
        else {
            // Reset the input
            document.getElementById('recordingNameInput').value = "";
        }
    }
    catch (error) {
        handleConnectionError(error);
    }
    finally {

        // Update the status display
        updateStatus();

        globalThis.settingRecordingName = false;
    }
}


////
// Toggle recording EyeTracking data
////
togglingRecord = false;
toggleRecord = async () => {
    // Make sure the command can only run once
    if (globalThis.togglingRecord) return;
    globalThis.togglingRecord = true;

    // Disable the button to indicate that we are working
    button = document.getElementById('toggleRecordingButton');
    button.disabled = true;
    button.classList.remove('btn-danger');
    button.classList.remove('btn-success');
    button.classList.add('btn-secondary');


    // If we currently are recording stop recording
    try {
        if (globalThis.currentlyRecording) {
            response = await fetch('/api/recording_stop');
            success = await response.json();

            if (!success["success"]) {
                alert("Recording did not stop!\nError: " + success["message"]);
            }
        }
        // Otherwise start recording
        else {
            response = await fetch('/api/recording_start');
            success = await response.json();

            if (!success["success"]) {
                alert("Recording did not start!\nError: " + success["message"]);
            }
        }
    }
    catch (error) {
        handleConnectionError(error);
    }
    finally {

        // Update the status display after a few ms
        // (The status only changes after the "stopping" is complete which takes some time due to the final writes to the files on the HoloLens)
        displayUpdateButton(updateRunning = true);
        setTimeout(updateStatus, 600);

        globalThis.togglingRecord = false;
    }
}


////
// Toggle the accuracy grid
////
togglingAccuracy = false;
toggleAccuracy = async () => {
    // Make sure the command can only run once
    if (globalThis.togglingAccuracy) return;
    globalThis.togglingAccuracy = true;

    // Disable the buttons to indicate that we are working
    displayAccuracyButtons(false, globalThis.accuracyVisible, globalThis.accuracyDistance);

    // If the accuracy grid is visible, hide it
    try {
        if (globalThis.accuracyVisible) {
            response = await fetch('/api/accuracy_hide');
            success = await response.json();

            if (!success["success"]) {
                alert("Accuracy grid could not be hidden!\nError: " + success["message"]);
            }
        }
        // Otherwise show it
        else {
            response = await fetch('/api/accuracy_show');
            success = await response.json();

            if (!success["success"]) {
                alert("Accuracy grid could not be displayed!\nError: " + success["message"]);
            }
        }
    }
    catch (error) {
        console.log("Error on accuracy");
        handleConnectionError(error);
    }
    finally {
        // Update the status display after a few ms
        // (as the grid is only shown/hidden on the next update instead of immediately)
        displayUpdateButton(updateRunning = true);
        setTimeout(updateStatus, 50);

        globalThis.togglingAccuracy = false;
    }
}


////
// Change the accuracy grid distance
////
updatingAccuracyDistance = false;
updateAccuracyDistance = async (distance) => {
    // Make sure the command can only run once
    if (updatingAccuracyDistance) return;
    updatingAccuracyDistance = true;

    // Disable the buttons to indicate that we are working
    displayAccuracyButtons(false, globalThis.accuracyVisible, globalThis.accuracyDistance);

    // Save the new distance
    globalThis.accuracyDistance = distance;

    // If we are already showing the grid update the distance
    try {
        formData = "distance=";
        formData += encodeURI(globalThis.accuracyDistance);

        response = await fetch("/api/accuracy_distance",
            {
                body: formData,
                method: "put"
            });
        success = await response.json();

        if (!success["success"]) {
            alert("Distance of accuracy grid could not be updated!\nError: " + success["message"]);
        }
    }
    catch (error) {
        console.log("Error on accuracy");
        handleConnectionError(error);
    }
    finally {
        // Update the status display after a few ms
        // (as the distance is only changed on the next update instead of immediately)
        displayUpdateButton(updateRunning = true);
        setTimeout(updateStatus, 50);

        updatingAccuracyDistance = false;
    }
}


////
// Toggle the AOI check
////
togglingCheck = false;
toggleCheck = async () => {
    // Make sure the command can only run once
    if (globalThis.togglingCheck) return;
    globalThis.togglingCheck = true;

    // Disable the button to indicate that we are working
    button = document.getElementById('toggleCheckButton');
    button.disabled = true;
    button.classList.remove('btn-primary');
    button.classList.remove('btn-secondary');
    button.classList.add('btn-light');

    // If the AOI check is visible, hide it
    try {
        if (globalThis.checkVisible) {
            response = await fetch('/api/check_hide');
            success = await response.json();

            if (!success["success"]) {
                alert("AOI check layer could not be hidden!\nError: " + success["message"]);
            }
        }
        // Otherwise show it
        else {
            response = await fetch('/api/check_show');
            success = await response.json();

            if (!success["success"]) {
                alert("AOI check layer could not be displayed!\nError: " + success["message"]);
            }
        }
    }
    catch (error) {
        handleConnectionError(error);
    }
    finally {
        // Update the status display after a few ms
        // (as the highlight layer is only shown on the next update instead of immediately)
        displayUpdateButton(updateRunning = true);
        setTimeout(updateStatus, 50);

        globalThis.togglingCheck = false;
    }
}


////
// Launch the eye tracking calibration on the device
////
launchingCalibration = false;
launchCalibration = async () => {
    // Make sure the command can only run once
    if (globalThis.launchingCalibration) return;
    globalThis.launchingCalibration = true;

    // Disable the button to indicate that we are working
    displayCalibrationButton(false);

    // If the AOI check is visible, hide it
    try {
        response = await fetch('/api/launch_calibration');
        success = await response.json();

        if (!success["success"]) {
            alert("Calibration couldn't be launched on the device!\nError: " + success["message"]);
        }
    }
    catch (error) {
        handleConnectionError(error);
    }
    finally {
        // Update the status display after a few ms
        // (as the highlight layer is only shown on the next update instead of immediately)
        displayUpdateButton(updateRunning = true);
        setTimeout(updateStatus, 50);

        globalThis.launchingCalibration = false;
    }
}


////
// Send an info line to the recording
////
sendingInfo = false;
sendInfo = async () => {
    // Make sure the command can only run once
    if (globalThis.sendingInfo) return;
    globalThis.sendingInfo = true;

    // Disable the send-button
    button = document.getElementById('sendInfoButton');
    button.disabled = true;

    // Build formData of new name
    formData = "info_string=";
    formData += encodeURI(document.getElementById('infoInput').value);

    // Post the data, wait for the response and parse it
    try {
        response = await fetch("/api/add_info",
            {
                body: formData,
                method: "put"
            });
        success = await response.json();

        // If there was an error alert the user to it
        if (!success["success"]) {
            displayLogError(true, "Info text was not sent successfully! " + success["message"])
            globalThis.infoError = true;
        }
        else {
            // Reset the input
            document.getElementById('infoInput').value = "";

            // Clear an error if it currently exists
            if (globalThis.infoError)
                displayLogError(false, "");
        }
    }
    catch (error) {
        handleConnectionError(error);
    }
    finally {
        // Update the status display after a few ms
        // (as the info is only logged on the next update instead of immediately)
        displayUpdateButton(updateRunning = true);
        setTimeout(updateStatus, 50);

        // Re-enable the button as the info was sent
        button.disabled = false;

        globalThis.sendingInfo = false;
    }
}
