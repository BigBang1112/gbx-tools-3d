import * as THREE from 'three';

let dotNetObjRef;
let mixers = [];
let mixerTimeScale = 1;

export function createPositionTrack(times, values, discrete) {
    return new THREE.VectorKeyframeTrack('.position', times, values, discrete ? THREE.InterpolateDiscrete : THREE.InterpolateLinear);
}

export function createQuaternionTrack(times, values, discrete) {
    return new THREE.QuaternionKeyframeTrack('.quaternion', times, values, discrete ? THREE.InterpolateDiscrete : THREE.InterpolateLinear);
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
    const mixer = new THREE.AnimationMixer(object);
    mixer.timeScale = 0;
    mixers.push(mixer);
    return mixer;
}

export function createAction(mixer, clip, obj) {
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
    for (let i = 0; i < mixers.length; i++) {
        mixers[i].timeScale = mixerTimeScale;
    }
}

export function pauseMixer() {
    for (let i = 0; i < mixers.length; i++) {
        mixers[i].timeScale = 0;
    }
}

export function setMixerTime(time) {
    for (let i = 0; i < mixers.length; i++) {
        const mixer = mixers[i];
        let prevTimeScale = mixer.timeScale;
        mixer.timeScale = 1;
        mixer.setTime(time);
        mixer.timeScale = prevTimeScale;
    }
}

export function setMixerTimeScale(timeScale, isPaused) {
    mixerTimeScale = timeScale;

    if (!isPaused) {
        for (let i = 0; i < mixers.length; i++) {
            const mixer = mixers[i];
            mixer.timeScale = timeScale;
        }
    }
}

let prevMixerTime;

export function updateMixer(delta) {
    for (let i = 0; i < mixers.length; i++) {
        const mixer = mixers[i];
        mixer.update(delta);
        if (mixer.time !== prevMixerTime) {
            dotNetObjRef.invokeMethodAsync("UpdateTimeline", mixer.time);
            prevMixerTime = mixer.time;
        }
    }
}

export function disposeMixers() {
    for (let i = 0; i < mixers.length; i++) {
        mixers[i].stopAllAction();
    }
    mixers = [];
    prevMixerTime = null;
}

export function registerDotNet(reference) {
    dotNetObjRef = reference;
}