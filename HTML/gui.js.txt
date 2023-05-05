// Copyright (c) Sebastian Kapp.
// Licensed under the MIT License.

// Set the displayed device name
displayDeviceName = function (newName = "HoloLens 2") {
    document.getElementById('deviceName').innerHTML = newName;
}

// Update the "last update" timestamp
updateLastUpdateTimestamp = function () {
    document.getElementById('timeLastUpdate').innerHTML = (new Date()).toLocaleTimeString();
}

// Update the update button
displayUpdateButton = function (updateRunning = false) {
    updateButton = document.getElementById('updateButton');
    if (updateRunning) {
        // If the update is running, display an info and disable the button
        updateButton.enabled = false;
        updateButton.value = "Updating...";
        updateButton.classList.remove('btn-success');
        updateButton.classList.add('btn-warning');
    }
    else {
        // If no update is running, enable the button
        updateButton.enabled = true;
        updateButton.value = "Update";
        updateButton.classList.remove('btn-warning');
        updateButton.classList.add('btn-success');
    }
}

// Set the current participant name
displayParticipantName = function (newName = "None") {
    document.getElementById('participantName').innerHTML = newName;
}

// Set eyes api 
displayEyesApiAvailable = function (available = true) {
    display = document.getElementById('eyesApiAvailable');
    if (available) {
        display.innerHTML = "Eyes API Available";
        display.classList.remove('bg-danger');
        display.classList.add('bg-success');
    }
    else {
        display.innerHTML = "Eyes API <b>not</b> available";
        display.classList.remove('bg-success');
        display.classList.add('bg-danger');
    }
}

// Set gaze calibration valid
displayCalibrationValid = function (valid = true) {
    display = document.getElementById('gazeCalibrationValid');
    if (valid) {
        display.innerHTML = "Calibration valid";
        display.classList.remove('bg-danger');
        display.classList.add('bg-success');
    }
    else {
        display.innerHTML = "Calibration <b>invalid</b>";
        display.classList.remove('bg-success');
        display.classList.add('bg-danger');
    }
}

// Set Recording Name
displayRecordingName = function (newName = "None") {
    document.getElementById('recordingName').innerHTML = newName;
}

// Display if recording is active
displayRecordingActive = function (running = false) {
    display = document.getElementById('recordingRunning');
    button = document.getElementById('toggleRecordingButton');
    if (running) {
        // Update the status display
        display.innerHTML = "Recording";
        display.classList.remove('bg-danger');
        display.classList.add('bg-success');
        // Update the record button
        button.value = "Stop Recording";
        button.classList.remove('btn-secondary');
        button.classList.remove('btn-success');
        button.classList.add('btn-danger');
    }
    else {
        // Update the status display
        display.innerHTML = "Not Recording";
        display.classList.remove('bg-success');
        display.classList.add('bg-danger');
        // Update the record button
        button.value = "Start Recording";
        button.classList.remove('btn-secondary');
        button.classList.remove('btn-danger');
        button.classList.add('btn-success');
    }
    // Make sure the button is enabled
    // (While the toggle command is running we disable the button)
    button.disabled = false;
}

// Display the info of the current recording
displayRecordingInfo = function (recordingStartTime = "NA", recordingStop = "NA", recordingDuration = "NA") {
    document.getElementById('recordingStartTime').innerHTML = recordingStartTime;
    document.getElementById('recordingStopTime').innerHTML = recordingStop;
    document.getElementById('recordingDuration').innerHTML = recordingDuration;
}

// Toggle Accuracy Buttons
displayAccuracyButtons = function (enabled = true, visible = false, distance = -1) {
    // Update the main button
    button = document.getElementById('toggleAccuracyButton');
    if (!enabled) {
        button.classList.remove('btn-secondary');
        button.classList.remove('btn-primary');
        button.classList.add('btn-light');
    }
    else if (visible) {
        button.value = "Hide Accuracy Grid";
        button.classList.remove('btn-light');
        button.classList.remove('btn-primary');
        button.classList.add('btn-secondary');
    }
    else {
        button.value = "Show Accuracy Grid";
        button.classList.remove('btn-light');
        button.classList.remove('btn-secondary');
        button.classList.add('btn-primary');
    }
    button.disabled = !enabled;

    // Update the distance buttons
    b05m = document.getElementById('d05mButton');
    b05m.disabled = !enabled;
    if (!enabled) {
        b05m.classList.remove('btn-secondary');
        b05m.classList.remove('btn-primary');
        b05m.classList.add('btn-light');
    }
    else if (distance == 05) {
        b05m.classList.remove('btn-secondary');
        b05m.classList.remove('btn-light');
        b05m.classList.add('btn-primary');
    }
    else {
        b05m.classList.remove('btn-primary');
        b05m.classList.remove('btn-light');
        b05m.classList.add('btn-secondary');
    }
    b10m = document.getElementById('d10mButton');
    b10m.disabled = !enabled;
    if (!enabled) {
        b10m.classList.remove('btn-secondary');
        b10m.classList.remove('btn-primary');
        b10m.classList.add('btn-light');
    }
    else if (distance == 10) {
        b10m.classList.remove('btn-secondary');
        b10m.classList.remove('btn-light');
        b10m.classList.add('btn-primary');
    }
    else {
        b10m.classList.remove('btn-primary');
        b10m.classList.remove('btn-light');
        b10m.classList.add('btn-secondary');
    }
    b20m = document.getElementById('d20mButton');
    b20m.disabled = !enabled;
    if (!enabled) {
        b20m.classList.remove('btn-secondary');
        b20m.classList.remove('btn-primary');
        b20m.classList.add('btn-light');
    }
    else if (distance == 20) {
        b20m.classList.remove('btn-secondary');
        b20m.classList.remove('btn-light');
        b20m.classList.add('btn-primary');
    }
    else {
        b20m.classList.remove('btn-primary');
        b20m.classList.remove('btn-light');
        b20m.classList.add('btn-secondary');
    }
    b40m = document.getElementById('d40mButton');
    b40m.disabled = !enabled;
    if (!enabled) {
        b40m.classList.remove('btn-secondary');
        b40m.classList.remove('btn-primary');
        b40m.classList.add('btn-light');
    }
    else if (distance == 40) {
        b40m.classList.remove('btn-secondary');
        b40m.classList.remove('btn-light');
        b40m.classList.add('btn-primary');
    }
    else {
        b40m.classList.remove('btn-primary');
        b40m.classList.remove('btn-light');
        b40m.classList.add('btn-secondary');
    }
}

// Toggle Check Button
displayCheckButton = function (visible = false) {
    button = document.getElementById('toggleCheckButton');
    if (visible) {
        button.value = "Hide AOI Check";
        button.classList.remove('btn-light');
        button.classList.remove('btn-primary');
        button.classList.add('btn-secondary');
    }
    else {
        button.value = "Show AOI Check";
        button.classList.remove('btn-light');
        button.classList.remove('btn-secondary');
        button.classList.add('btn-primary');
    }
    // Make sure the button is enabled
    // (While the toggle command is running we disable the button)
    button.disabled = false;
}

// Launch Calibration Button
displayCalibrationButton = function (active = false) {
    button = document.getElementById('launchCalibrationButton');

    if (active) {
        button.disabled = false;
        button.classList.remove('btn-light');
        button.classList.add('btn-primary');
    }
    else {
        button.disabled = true;
        button.classList.remove('btn-primary');
        button.classList.add('btn-light');
    }
}

// Display a general error (e.g. lost connection)
displayError = function(visible = true, error = "Error!") {
    display = document.getElementById('errorDisplay');
    if (visible) {
        display.style.display = "block";
        display.innerHTML = error;
    }
    else {
        display.style.display = "none";
        display.innerHTML = error;
    }
}

// Display an error when writing logs
displayLogError = function(visible = true, error = "Error!") {
    display = document.getElementById('logErrorDisplay');
    if (visible) {
        display.style.display = "block";
        display.innerHTML = error;
    }
    else {
        display.style.display = "none";
        display.innerHTML = error;
    }
}
