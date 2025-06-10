import * as THREE from 'three';

let mixer, dotNetObjRef;
let mixerTimeScale = 1;

export function createPositionTrack(times, values) {
    return new THREE.VectorKeyframeTrack('.position', times, values);
}

export function createQuaternionTrack(times, values) {
    return new THREE.QuaternionKeyframeTrack('.quaternion', times, values);
}

export function createRotationXTrack(times, values) {
    return new THREE.NumberKeyframeTrack('.rotation[x]', times, values);
}

export function createRotationYTrack(times, values) {
    return new THREE.NumberKeyframeTrack('.rotation[y]', times, values);
}

export function createRelativePositionYTrack(times, values, referenceObj) {
    let refY = referenceObj.position.y;
    for (let i = 0; i < values.length; i++) {
        values[i] = refY - values[i];
    }
    return new THREE.NumberKeyframeTrack('.position[y]', times, values);
}

export function createClip(name, duration, tracks) {
    return new THREE.AnimationClip(name, duration, tracks);
}

export function createMixer(object) {
    mixer = new THREE.AnimationMixer(object);
    mixer.timeScale = 0;
}

export function createAction(clip, obj) {
    return mixer.clipAction(clip, obj);
}

export function playAction(action) {
    action.play();
}

export function pauseAction(action) {
    action.paused = true;
}

export function resumeAction(action) {
    action.paused = false;
}

export function playMixer() {
    if (mixer) {
        mixer.timeScale = mixerTimeScale;
    }
}

export function pauseMixer() {
    if (mixer) {
        mixer.timeScale = 0;
    }
}

export function setMixerTime(time) {
    if (mixer) {
        let prevTimeScale = mixer.timeScale;
        mixer.timeScale = 1;
        mixer.setTime(time);
        mixer.timeScale = prevTimeScale;
    }
}

export function setMixerTimeScale(timeScale, isPaused) {
    mixerTimeScale = timeScale;

    if (mixer && !isPaused) {
        mixer.timeScale = timeScale;
    }
}

let prevMixerTime;

export function updateMixer(delta) {
    if (mixer) {
        mixer.update(delta);
        if (mixer.time !== prevMixerTime) {
            dotNetObjRef.invokeMethodAsync("UpdateTimeline", mixer.time);
            prevMixerTime = mixer.time;
        }
    }
}

export function registerDotNet(reference) {
    dotNetObjRef = reference;
}